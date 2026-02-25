using System.Diagnostics;
using System.Globalization;
using McpServer.Support.Mcp.Options;
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
    private readonly object _stateGate = new();
    private Process? _process;
    private string? _configPath;
    private string? _publicUrl;
    private string? _error;
    private string? _lastStdoutLine;
    private string? _lastStderrLine;
    private Task? _stdoutPumpTask;
    private Task? _stderrPumpTask;
    private bool _stopRequested;

    /// <summary>Initializes a new instance of the <see cref="FrpTunnelProvider"/> class.</summary>
    public FrpTunnelProvider(IOptions<TunnelOptions> options, IProcessRunner processRunner, ILogger<FrpTunnelProvider> logger)
    {
        _options = options.Value;
        _processRunner = processRunner;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _stopRequested = false;
        _error = null;
        _publicUrl = null;
        _lastStdoutLine = null;
        _lastStderrLine = null;

        var frp = _options.Frp;
        if (!TryValidateOptions(frp, out var validationError))
        {
            _error = validationError;
            _logger.LogError("{Error}", _error);
            return;
        }

        var proxyType = NormalizeProxyType(frp.ProxyType);
        _logger.LogInformation(
            "Starting FRP tunnel provider: LocalPort={LocalPort}; ProxyType={ProxyType}; Server={ServerAddress}:{ServerPort}; StartupTimeoutSeconds={StartupTimeoutSeconds}; PublicBaseUrlConfigured={HasPublicBaseUrl}",
            _options.Port,
            proxyType,
            frp.ServerAddress,
            frp.ServerPort,
            frp.StartupTimeoutSeconds,
            !string.IsNullOrWhiteSpace(frp.PublicBaseUrl));

        // frpc verify may fail without a config; only --version is used for an existence check.
        var whichCheck = await _processRunner.RunAsync("frpc", "--version", cancellationToken).ConfigureAwait(false);
        if (whichCheck.ExitCode != 0)
        {
            _error = "frpc CLI not found. Install from https://github.com/fatedier/frp/releases";
            _logger.LogError("{Error}", _error);
            return;
        }

        _configPath = Path.Combine(Path.GetTempPath(), $"frpc_{Guid.NewGuid():N}.toml");
        var config = GenerateConfig(frp, proxyType);
        await File.WriteAllTextAsync(_configPath, config, cancellationToken).ConfigureAwait(false);

        var startInfo = new ProcessStartInfo
        {
            FileName = "frpc",
            Arguments = $"-c \"{_configPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        _process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        _process.Exited += OnProcessExited;
        _process.Start();
        StartOutputPumps(_process);
        _logger.LogInformation("frpc started (PID {Pid}) with config {Config}", _process.Id, _configPath);

        // Construct the expected public URL.
        _publicUrl = BuildPublicUrl(frp);
        if (_publicUrl is not null)
            _logger.LogInformation("FRP tunnel expected URL: {Url}", _publicUrl);

        // Give frpc time to connect to frps and detect early startup exits.
        var startupDelay = TimeSpan.FromSeconds(Math.Clamp(frp.StartupTimeoutSeconds, 1, 120));
        await Task.Delay(startupDelay, cancellationToken).ConfigureAwait(false);

        if (_process.HasExited)
        {
            _error = BuildExitError(_process, "frpc exited during startup");
            _logger.LogError("{Error}", _error);
            return;
        }

        _logger.LogInformation("FRP tunnel process is running after startup wait ({Seconds}s).", (int)startupDelay.TotalSeconds);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _stopRequested = true;
        try
        {
            if (_process is not null && !_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
                _process.WaitForExit(5000);
                _logger.LogInformation("FRP tunnel stopped.");
            }
        }
        catch (InvalidOperationException) { /* process exited between check and kill */ }

        CleanupConfig();
        return Task.CompletedTask;
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
            if (_process is not null && !_process.HasExited)
                _process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException) { /* process already exited */ }
        _process?.Dispose();
        CleanupConfig();
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

    private string GenerateConfig(FrpTunnelOptions frp, string proxyType)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("[common]");
        sb.AppendLine(CultureInfo.InvariantCulture, $"serverAddr = \"{frp.ServerAddress}\"");
        sb.AppendLine(CultureInfo.InvariantCulture, $"serverPort = {frp.ServerPort}");

        if (!string.IsNullOrWhiteSpace(frp.Token))
            sb.AppendLine(CultureInfo.InvariantCulture, $"auth.token = \"{frp.Token}\"");

        sb.AppendLine();
        sb.AppendLine("[[proxies]]");
        sb.AppendLine(CultureInfo.InvariantCulture, $"name = \"mcp-{proxyType}\"");
        sb.AppendLine(CultureInfo.InvariantCulture, $"type = \"{proxyType}\"");
        sb.AppendLine(CultureInfo.InvariantCulture, $"localPort = {_options.Port}");

        if (string.Equals(proxyType, "http", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(frp.Subdomain))
                sb.AppendLine(CultureInfo.InvariantCulture, $"subdomain = \"{frp.Subdomain}\"");
            else if (!string.IsNullOrWhiteSpace(frp.CustomDomain))
                sb.AppendLine(CultureInfo.InvariantCulture, $"customDomains = [\"{frp.CustomDomain}\"]");
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

        var proxyType = NormalizeProxyType(frp.ProxyType);
        if (!string.Equals(proxyType, "http", StringComparison.Ordinal))
        {
            error = $"FRP tunnel configuration error: ProxyType '{frp.ProxyType}' is not supported yet. Supported values: http.";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(frp.Subdomain) && !string.IsNullOrWhiteSpace(frp.CustomDomain))
        {
            error = "FRP tunnel configuration error: Configure either Subdomain or CustomDomain, not both.";
            return false;
        }

        return true;
    }

    private static string NormalizeProxyType(string? proxyType)
        => string.IsNullOrWhiteSpace(proxyType) ? "http" : proxyType.Trim().ToLowerInvariant();

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
