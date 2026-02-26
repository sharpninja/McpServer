using System.Diagnostics;
using System.Globalization;
using McpServer.Support.Mcp.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Tunnel provider using <c>frpc</c> (FRP client). Generates a temporary
/// <c>frpc.toml</c> config and starts the FRP client process.
/// Requires a running <c>frps</c> server (see <c>docker-compose.frps.yml</c>).
/// </summary>
public sealed class FrpTunnelProvider : ITunnelProvider, IDisposable
{
    /// <inheritdoc />
    public string ProviderName => "frp";

    private readonly TunnelOptions _options;
    private readonly IProcessRunner _processRunner;
    private readonly ILogger<FrpTunnelProvider> _logger;
    private readonly IServiceScopeFactory? _scopeFactory;
    private readonly IWorkspaceProcessManager? _workspaceProcessManager;
    private readonly object _stateGate = new();
    private readonly SemaphoreSlim _lifecycleGate = new(1, 1);
    private Process? _process;
    private string? _configPath;
    private string? _publicUrl;
    private string? _error;
    private string? _lastStdoutLine;
    private string? _lastStderrLine;
    private string? _lastAppliedConfig;
    private Task? _stdoutPumpTask;
    private Task? _stderrPumpTask;
    private CancellationTokenSource? _reconcileLoopCts;
    private Task? _reconcileLoopTask;
    private bool _stopRequested;

    /// <summary>Initializes a new instance of the <see cref="FrpTunnelProvider"/> class.</summary>
    public FrpTunnelProvider(
        IOptions<TunnelOptions> options,
        IProcessRunner processRunner,
        ILogger<FrpTunnelProvider> logger,
        IServiceScopeFactory? scopeFactory = null,
        IWorkspaceProcessManager? workspaceProcessManager = null)
    {
        _options = options.Value;
        _processRunner = processRunner;
        _logger = logger;
        _scopeFactory = scopeFactory;
        _workspaceProcessManager = workspaceProcessManager;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _stopRequested = false;
        _error = null;
        _publicUrl = null;
        _lastStdoutLine = null;
        _lastStderrLine = null;
        _lastAppliedConfig = null;

        var frp = _options.Frp;
        if (!TryValidateOptions(frp, out var validationError))
        {
            _error = validationError;
            _logger.LogError("{Error}", _error);
            return;
        }

        var proxyType = NormalizeProxyType(frp.ProxyType);

        // frpc verify may fail without a config; only --version is used for an existence check.
        var whichCheck = await _processRunner.RunAsync("frpc", "--version", cancellationToken).ConfigureAwait(false);
        if (whichCheck.ExitCode != 0)
        {
            _error = "frpc CLI not found. Install from https://github.com/fatedier/frp/releases";
            _logger.LogError("{Error}", _error);
            return;
        }

        var runtimeConfig = await BuildRuntimeConfigAsync(frp, proxyType, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation(
            "Starting FRP tunnel provider: LocalPorts={LocalPorts}; ProxyType={ProxyType}; Server={ServerAddress}:{ServerPort}; StartupTimeoutSeconds={StartupTimeoutSeconds}; PublicBaseUrlConfigured={HasPublicBaseUrl}; AutoMapWorkspacePorts={AutoMapWorkspacePorts}",
            runtimeConfig.LocalPortSummary,
            proxyType,
            frp.ServerAddress,
            frp.ServerPort,
            frp.StartupTimeoutSeconds,
            !string.IsNullOrWhiteSpace(frp.PublicBaseUrl),
            ShouldUseWorkspaceTcpPortAutoMap(frp, proxyType));

        var started = await StartOrRestartTunnelProcessAsync(
            runtimeConfig,
            proxyType,
            startupReason: "initial startup",
            isRestart: false,
            cancellationToken).ConfigureAwait(false);

        if (!started)
            return;

        StartReconcileLoopIfEnabled(frp, proxyType);
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _stopRequested = true;
        await StopReconcileLoopAsync().ConfigureAwait(false);

        await _lifecycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            StopProcessCore(logStopped: true);
            CleanupConfig();
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    /// <inheritdoc />
    public Task<TunnelStatus> GetStatusAsync(CancellationToken ct = default)
    {
        if (_process is null)
            return Task.FromResult(new TunnelStatus(false, Error: _error ?? "Not started."));

        if (_process.HasExited)
        {
            if (string.IsNullOrWhiteSpace(_error))
            {
                _error = BuildExitError(_process, "frpc process exited");
            }

            return Task.FromResult(new TunnelStatus(false, Error: _error ?? "Not started."));
        }

        return Task.FromResult(new TunnelStatus(true, _publicUrl, _error));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _stopRequested = true;
        try
        {
            _reconcileLoopCts?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Best effort during disposal.
        }

        try
        {
            StopProcessCore(logStopped: false);
        }
        catch (InvalidOperationException) { /* process already exited */ }
        CleanupConfig();
        _reconcileLoopCts?.Dispose();
        _lifecycleGate.Dispose();
    }

    private async Task<bool> StartOrRestartTunnelProcessAsync(
        FrpRuntimeConfig runtimeConfig,
        string proxyType,
        string startupReason,
        bool isRestart,
        CancellationToken cancellationToken)
    {
        await _lifecycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_stopRequested)
                return false;

            _error = null;
            _publicUrl = runtimeConfig.PublicEndpoint;
            _lastStdoutLine = null;
            _lastStderrLine = null;

            _configPath ??= Path.Combine(Path.GetTempPath(), $"frpc_{Guid.NewGuid():N}.toml");
            await File.WriteAllTextAsync(_configPath, runtimeConfig.ConfigText, cancellationToken).ConfigureAwait(false);

            if (isRestart)
            {
                StopProcessCore(logStopped: false);
                _logger.LogInformation(
                    "Restarting FRP tunnel process after config change: Reason={Reason}; LocalPorts={LocalPorts}",
                    startupReason,
                    runtimeConfig.LocalPortSummary);
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "frpc",
                Arguments = $"-c \"{_configPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
            process.Exited += OnProcessExited;
            process.Start();
            StartOutputPumps(process);
            _process = process;
            _lastAppliedConfig = runtimeConfig.ConfigText;

            _logger.LogInformation("frpc started (PID {Pid}) with config {Config}", process.Id, _configPath);

            if (_publicUrl is not null)
                _logger.LogInformation("FRP tunnel endpoint summary: {Url}", _publicUrl);

            var startupDelay = TimeSpan.FromSeconds(Math.Clamp(_options.Frp.StartupTimeoutSeconds, 1, 120));
            await Task.Delay(startupDelay, cancellationToken).ConfigureAwait(false);

            if (_process is null || _process.HasExited)
            {
                if (_process is not null)
                {
                    _error = BuildExitError(_process, "frpc exited during startup");
                    _logger.LogError("{Error}", _error);
                }
                else
                {
                    _error = "frpc process missing after startup.";
                    _logger.LogError("{Error}", _error);
                }

                return false;
            }

            _logger.LogInformation(
                "FRP tunnel process is running after startup wait ({Seconds}s).",
                (int)startupDelay.TotalSeconds);
            return true;
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    private void StopProcessCore(bool logStopped)
    {
        var process = _process;
        _process = null;
        if (process is null)
            return;

        try
        {
            process.Exited -= OnProcessExited;
        }
        catch
        {
            // Best effort; event detachment can fail if process object is disposed.
        }

        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(5000);
                if (logStopped)
                    _logger.LogInformation("FRP tunnel stopped.");
            }
        }
        catch (InvalidOperationException)
        {
            // Process exited between checks.
        }
        finally
        {
            process.Dispose();
        }
    }

    private void StartReconcileLoopIfEnabled(FrpTunnelOptions frp, string proxyType)
    {
        if (!ShouldUseWorkspaceTcpPortAutoMap(frp, proxyType))
            return;

        if (_scopeFactory is null)
        {
            _logger.LogWarning(
                "FRP tcp auto-mapping requested but IServiceScopeFactory is unavailable. Falling back to static tcp mapping.");
            return;
        }

        if (_reconcileLoopTask is not null)
            return;

        var intervalSeconds = Math.Clamp(frp.ReconcileIntervalSeconds, 1, 300);
        _reconcileLoopCts = new CancellationTokenSource();
        _reconcileLoopTask = Task.Run(
            () => RunReconcileLoopAsync(proxyType, TimeSpan.FromSeconds(intervalSeconds), _reconcileLoopCts.Token));

        _logger.LogInformation(
            "FRP tcp auto-mapping reconcile loop enabled: IntervalSeconds={IntervalSeconds}",
            intervalSeconds);
    }

    private async Task StopReconcileLoopAsync()
    {
        var cts = _reconcileLoopCts;
        var loopTask = _reconcileLoopTask;
        _reconcileLoopCts = null;
        _reconcileLoopTask = null;

        if (cts is not null)
        {
            try { cts.Cancel(); }
            catch (ObjectDisposedException) { }
        }

        if (loopTask is null)
            return;

        try
        {
            await loopTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown.
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "FRP reconcile loop exited with an exception.");
        }
        finally
        {
            cts?.Dispose();
        }
    }

    private async Task RunReconcileLoopAsync(string proxyType, TimeSpan interval, CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(interval);
        while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
        {
            try
            {
                if (_stopRequested)
                    return;

                var runtimeConfig = await BuildRuntimeConfigAsync(_options.Frp, proxyType, cancellationToken).ConfigureAwait(false);
                var processIsRunning = IsProcessRunning();
                var configChanged = !string.Equals(_lastAppliedConfig, runtimeConfig.ConfigText, StringComparison.Ordinal);

                if (!configChanged && processIsRunning)
                    continue;

                var reason = configChanged ? "workspace port mapping changed" : "frpc not running";
                _logger.LogInformation(
                    "FRP reconcile action: Reason={Reason}; LocalPorts={LocalPorts}; ProcessRunning={ProcessRunning}",
                    reason,
                    runtimeConfig.LocalPortSummary,
                    processIsRunning);

                _ = await StartOrRestartTunnelProcessAsync(
                    runtimeConfig,
                    proxyType,
                    reason,
                    isRestart: processIsRunning,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FRP reconcile loop iteration failed.");
            }
        }
    }

    private bool IsProcessRunning()
    {
        try
        {
            return _process is { HasExited: false };
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private void StartOutputPumps(Process process)
    {
        _stdoutPumpTask = Task.Run(() => PumpOutputAsync(process.StandardOutput, isError: false));
        _stderrPumpTask = Task.Run(() => PumpOutputAsync(process.StandardError, isError: true));
    }

    private async Task PumpOutputAsync(System.IO.StreamReader reader, bool isError)
    {
        try
        {
            while (true)
            {
                var line = await reader.ReadLineAsync().ConfigureAwait(false);
                if (line is null)
                    break;

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                lock (_stateGate)
                {
                    if (isError)
                        _lastStderrLine = line;
                    else
                        _lastStdoutLine = line;
                }

                if (isError)
                    _logger.LogWarning("frpc stderr: {Line}", line);
                else
                    _logger.LogDebug("frpc stdout: {Line}", line);
            }
        }
        catch (ObjectDisposedException)
        {
            // Process/stream disposed during shutdown.
        }
        catch (InvalidOperationException)
        {
            // Stream unavailable if process exits very early.
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed while reading frpc {StreamName}.", isError ? "stderr" : "stdout");
        }
    }

    private void OnProcessExited(object? sender, EventArgs e)
    {
        if (sender is not Process process)
            return;

        if (_stopRequested)
        {
            _logger.LogInformation("FRP tunnel process exited after stop request (ExitCode {ExitCode}).", process.ExitCode);
            return;
        }

        var exitError = BuildExitError(process, "frpc process exited unexpectedly");
        _error = exitError;
        _logger.LogError("{Error}", exitError);
    }

    private async Task<FrpRuntimeConfig> BuildRuntimeConfigAsync(
        FrpTunnelOptions frp,
        string proxyType,
        CancellationToken cancellationToken)
    {
        var tcpMappings = await ResolveTcpMappedPortsAsync(frp, proxyType, cancellationToken).ConfigureAwait(false);
        var configText = GenerateConfigWithMappings(frp, proxyType, tcpMappings);
        var publicEndpoint = string.Equals(proxyType, "http", StringComparison.Ordinal)
            ? BuildPublicUrl(frp)
            : BuildTcpEndpointSummary(frp, tcpMappings);
        var localPortSummary = BuildLocalPortSummary(frp, proxyType, tcpMappings);
        return new FrpRuntimeConfig(configText, publicEndpoint, localPortSummary);
    }

    private Task<IReadOnlyList<FrpTcpPortMapping>> ResolveTcpMappedPortsAsync(
        FrpTunnelOptions frp,
        string proxyType,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken; // reserved for future use
        if (!string.Equals(proxyType, "tcp", StringComparison.Ordinal))
            return Task.FromResult<IReadOnlyList<FrpTcpPortMapping>>([]);

        if (!ShouldUseWorkspaceTcpPortAutoMap(frp, proxyType))
            return Task.FromResult<IReadOnlyList<FrpTcpPortMapping>>([.. GetTcpMappedPorts(frp)]);

        // All workspaces share a single port; just map the server listen port.
        return Task.FromResult<IReadOnlyList<FrpTcpPortMapping>>([new FrpTcpPortMapping(_options.Port, _options.Port)]);
    }

    private static bool ShouldUseWorkspaceTcpPortAutoMap(FrpTunnelOptions frp, string proxyType)
        => string.Equals(proxyType, "tcp", StringComparison.Ordinal)
           && frp.AutoMapWorkspacePorts
           && frp.RemotePort is null
           && frp.TcpPortRangeStart is null
           && frp.TcpPortRangeEnd is null;

    private string GenerateConfig(FrpTunnelOptions frp, string proxyType)
        => GenerateConfigWithMappings(frp, proxyType, null);

    private string GenerateConfigWithMappings(
        FrpTunnelOptions frp,
        string proxyType,
        IReadOnlyList<FrpTcpPortMapping>? tcpMappings)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("[common]");
        sb.AppendLine(CultureInfo.InvariantCulture, $"serverAddr = \"{frp.ServerAddress}\"");
        sb.AppendLine(CultureInfo.InvariantCulture, $"serverPort = {frp.ServerPort}");

        if (!string.IsNullOrWhiteSpace(frp.Token))
            sb.AppendLine(CultureInfo.InvariantCulture, $"auth.token = \"{frp.Token}\"");

        sb.AppendLine();
        if (string.Equals(proxyType, "http", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine("[[proxies]]");
            sb.AppendLine(CultureInfo.InvariantCulture, $"name = \"mcp-{proxyType}\"");
            sb.AppendLine(CultureInfo.InvariantCulture, $"type = \"{proxyType}\"");
            sb.AppendLine(CultureInfo.InvariantCulture, $"localPort = {_options.Port}");

            if (!string.IsNullOrWhiteSpace(frp.Subdomain))
                sb.AppendLine(CultureInfo.InvariantCulture, $"subdomain = \"{frp.Subdomain}\"");
            else if (!string.IsNullOrWhiteSpace(frp.CustomDomain))
                sb.AppendLine(CultureInfo.InvariantCulture, $"customDomains = [\"{frp.CustomDomain}\"]");

            return sb.ToString();
        }

        foreach (var mapping in tcpMappings ?? [.. GetTcpMappedPorts(frp)])
        {
            sb.AppendLine("[[proxies]]");
            sb.AppendLine(CultureInfo.InvariantCulture, $"name = \"mcp-tcp-{mapping.RemotePort}\"");
            sb.AppendLine("type = \"tcp\"");
            sb.AppendLine(CultureInfo.InvariantCulture, $"localPort = {mapping.LocalPort}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"remotePort = {mapping.RemotePort}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string? BuildPublicUrl(FrpTunnelOptions frp)
    {
        if (!string.IsNullOrWhiteSpace(frp.PublicBaseUrl))
            return frp.PublicBaseUrl.Trim().TrimEnd('/');
        if (!string.IsNullOrWhiteSpace(frp.CustomDomain))
            return $"http://{frp.CustomDomain}";
        if (!string.IsNullOrWhiteSpace(frp.Subdomain))
            return $"http://{frp.Subdomain}.{frp.ServerAddress}";
        return null;
    }

    private bool TryValidateOptions(FrpTunnelOptions frp, out string? error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(frp.ServerAddress))
        {
            error = "FRP tunnel configuration error: Mcp:Tunnel:Frp:ServerAddress is required.";
            return false;
        }

        if (frp.ServerPort is <= 0 or > 65535)
        {
            error = $"FRP tunnel configuration error: Mcp:Tunnel:Frp:ServerPort '{frp.ServerPort}' is invalid.";
            return false;
        }

        if (_options.Port is <= 0 or > 65535)
        {
            error = $"FRP tunnel configuration error: Mcp:Tunnel:Port '{_options.Port}' is invalid.";
            return false;
        }

        if (frp.StartupTimeoutSeconds <= 0)
        {
            error = $"FRP tunnel configuration error: Mcp:Tunnel:Frp:StartupTimeoutSeconds '{frp.StartupTimeoutSeconds}' must be > 0.";
            return false;
        }

        if (frp.ReconcileIntervalSeconds <= 0)
        {
            error = $"FRP tunnel configuration error: Mcp:Tunnel:Frp:ReconcileIntervalSeconds '{frp.ReconcileIntervalSeconds}' must be > 0.";
            return false;
        }

        var proxyType = NormalizeProxyType(frp.ProxyType);
        if (!string.Equals(proxyType, "http", StringComparison.Ordinal)
            && !string.Equals(proxyType, "tcp", StringComparison.Ordinal))
        {
            error = $"FRP tunnel configuration error: ProxyType '{frp.ProxyType}' is not supported yet. Supported values: http, tcp.";
            return false;
        }

        if (string.Equals(proxyType, "http", StringComparison.Ordinal))
        {
            if (!string.IsNullOrWhiteSpace(frp.Subdomain) && !string.IsNullOrWhiteSpace(frp.CustomDomain))
            {
                error = "FRP tunnel configuration error: Configure either Subdomain or CustomDomain, not both.";
                return false;
            }

            return true;
        }

        if (!string.IsNullOrWhiteSpace(frp.Subdomain) || !string.IsNullOrWhiteSpace(frp.CustomDomain))
        {
            error = "FRP tunnel configuration error: Subdomain/CustomDomain are only valid for ProxyType=http.";
            return false;
        }

        if (frp.RemotePort is not null && !IsValidPort(frp.RemotePort.Value))
        {
            error = $"FRP tunnel configuration error: Mcp:Tunnel:Frp:RemotePort '{frp.RemotePort.Value}' is invalid.";
            return false;
        }

        var hasRangeStart = frp.TcpPortRangeStart is not null;
        var hasRangeEnd = frp.TcpPortRangeEnd is not null;
        if (hasRangeStart != hasRangeEnd)
        {
            error = "FRP tunnel configuration error: TcpPortRangeStart and TcpPortRangeEnd must be set together.";
            return false;
        }

        if (hasRangeStart && hasRangeEnd)
        {
            var start = frp.TcpPortRangeStart!.Value;
            var end = frp.TcpPortRangeEnd!.Value;
            if (!IsValidPort(start) || !IsValidPort(end))
            {
                error = $"FRP tunnel configuration error: TcpPortRangeStart/End '{start}-{end}' contains an invalid port.";
                return false;
            }

            if (end < start)
            {
                error = $"FRP tunnel configuration error: TcpPortRangeEnd '{end}' must be >= TcpPortRangeStart '{start}'.";
                return false;
            }

            if (frp.RemotePort is not null)
            {
                error = "FRP tunnel configuration error: Configure either RemotePort or TcpPortRangeStart/End for ProxyType=tcp, not both.";
                return false;
            }
        }

        return true;
    }

    private static string NormalizeProxyType(string? proxyType)
        => string.IsNullOrWhiteSpace(proxyType) ? "http" : proxyType.Trim().ToLowerInvariant();

    private IEnumerable<FrpTcpPortMapping> GetTcpMappedPorts(FrpTunnelOptions frp)
    {
        if (frp.TcpPortRangeStart is int start && frp.TcpPortRangeEnd is int end)
        {
            for (var port = start; port <= end; port++)
                yield return new FrpTcpPortMapping(port, port);

            yield break;
        }

        yield return new FrpTcpPortMapping(_options.Port, frp.RemotePort ?? _options.Port);
    }

    private static string? BuildTcpEndpointSummary(FrpTunnelOptions frp, IReadOnlyList<FrpTcpPortMapping>? tcpMappings = null)
    {
        if (string.IsNullOrWhiteSpace(frp.ServerAddress))
            return null;

        List<int> remotePorts = tcpMappings is { Count: > 0 }
            ? [.. tcpMappings.Select(m => m.RemotePort).Distinct().OrderBy(p => p)]
            : [.. GetConfiguredRemotePorts(frp)];

        if (remotePorts.Count == 0)
            return null;

        return $"tcp://{frp.ServerAddress}:{FormatPortList(remotePorts)}";
    }

    private string BuildLocalPortSummary(FrpTunnelOptions frp, string proxyType, IReadOnlyList<FrpTcpPortMapping>? tcpMappings = null)
    {
        if (!string.Equals(proxyType, "tcp", StringComparison.Ordinal))
            return _options.Port.ToString(CultureInfo.InvariantCulture);

        List<int> localPorts = tcpMappings is { Count: > 0 }
            ? [.. tcpMappings.Select(m => m.LocalPort).Distinct().OrderBy(p => p)]
            : [.. GetConfiguredLocalPorts(frp)];

        if (localPorts.Count == 0)
            return _options.Port.ToString(CultureInfo.InvariantCulture);

        if (tcpMappings is { Count: 1 })
        {
            var only = tcpMappings[0];
            if (only.LocalPort != only.RemotePort)
                return $"{only.LocalPort}->{only.RemotePort}";
        }

        return FormatPortList(localPorts);
    }

    private static bool IsValidPort(int port)
        => port is > 0 and <= 65535;

    private static IEnumerable<int> GetConfiguredRemotePorts(FrpTunnelOptions frp)
    {
        if (frp.TcpPortRangeStart is int start && frp.TcpPortRangeEnd is int end)
        {
            for (var port = start; port <= end; port++)
                yield return port;
            yield break;
        }

        if (frp.RemotePort is int remotePort && remotePort > 0)
            yield return remotePort;
    }

    private IEnumerable<int> GetConfiguredLocalPorts(FrpTunnelOptions frp)
    {
        if (frp.TcpPortRangeStart is int start && frp.TcpPortRangeEnd is int end)
        {
            for (var port = start; port <= end; port++)
                yield return port;
            yield break;
        }

        yield return _options.Port;
    }

    private static string FormatPortList(IReadOnlyList<int> ports)
    {
        if (ports.Count == 0)
            return string.Empty;

        var ranges = new List<string>();
        var start = ports[0];
        var prev = ports[0];

        for (var i = 1; i < ports.Count; i++)
        {
            var current = ports[i];
            if (current == prev + 1)
            {
                prev = current;
                continue;
            }

            ranges.Add(start == prev ? start.ToString(CultureInfo.InvariantCulture) : $"{start}-{prev}");
            start = prev = current;
        }

        ranges.Add(start == prev ? start.ToString(CultureInfo.InvariantCulture) : $"{start}-{prev}");
        return string.Join(",", ranges);
    }

    private readonly record struct FrpTcpPortMapping(int LocalPort, int RemotePort);
    private readonly record struct FrpRuntimeConfig(string ConfigText, string? PublicEndpoint, string LocalPortSummary);

    private string BuildExitError(Process process, string prefix)
    {
        string? lastStdout;
        string? lastStderr;
        lock (_stateGate)
        {
            lastStdout = _lastStdoutLine;
            lastStderr = _lastStderrLine;
        }

        var message = $"{prefix} (exit code {process.ExitCode}).";
        if (!string.IsNullOrWhiteSpace(lastStderr))
            message += $" stderr: {TruncateForError(lastStderr)}";
        else if (!string.IsNullOrWhiteSpace(lastStdout))
            message += $" stdout: {TruncateForError(lastStdout)}";

        return message;
    }

    private static string TruncateForError(string value, int maxLength = 400)
    {
        var singleLine = value.ReplaceLineEndings(" ").Trim();
        if (singleLine.Length <= maxLength)
            return singleLine;

        return singleLine[..maxLength] + "...";
    }

    private void CleanupConfig()
    {
        if (_configPath is not null && File.Exists(_configPath))
        {
            try { File.Delete(_configPath); }
            catch { /* best-effort */ }
            _configPath = null;
        }
    }
}
