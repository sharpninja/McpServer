using System.Diagnostics;
using McpServer.Support.Mcp.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Tunnel provider using <c>cloudflared</c> CLI. Starts a quick tunnel or
/// named tunnel and parses the public URL from stdout.
/// </summary>
public sealed class CloudflareTunnelProvider : ITunnelProvider, IDisposable
{
    private static readonly TimeSpan s_startupPollInterval = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan s_startupTimeout = TimeSpan.FromSeconds(8);

    /// <inheritdoc />
    public string ProviderName => "cloudflare";

    private readonly TunnelOptions _options;
    private readonly IProcessRunner _processRunner;
    private readonly ILogger<CloudflareTunnelProvider> _logger;
    private Process? _process;
    private CancellationTokenSource? _outputPumpCts;
    private Task? _stdoutPumpTask;
    private Task? _stderrPumpTask;
    private string? _publicUrl;
    private string? _error;
    private string? _lastStdoutLine;
    private string? _lastStderrLine;
    private bool _startupCompleted;
    private bool _stopRequested;
    private bool _isNamedTunnelMode;

    /// <summary>Initializes a new instance of the <see cref="CloudflareTunnelProvider"/> class.</summary>
    public CloudflareTunnelProvider(IOptions<TunnelOptions> options, IProcessRunner processRunner, ILogger<CloudflareTunnelProvider> logger)
    {
        _options = options.Value;
        _processRunner = processRunner;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        ResetRuntimeStateForStart();

        var check = await _processRunner.RunAsync("cloudflared", "version", cancellationToken).ConfigureAwait(false);
        if (check.ExitCode != 0)
        {
            _error = "cloudflared CLI not found. Install from https://developers.cloudflare.com/cloudflare-one/connections/connect-networks/downloads/";
            _logger.LogError("{Error}", _error);
            return;
        }

        var cf = _options.Cloudflare;
        string args;

        if (!string.IsNullOrWhiteSpace(cf.TunnelName))
        {
            // Named tunnel (requires prior `cloudflared tunnel create`).
            _isNamedTunnelMode = true;
            args = $"tunnel run {cf.TunnelName}";
        }
        else
        {
            // Quick tunnel — cloudflared assigns a random *.trycloudflare.com URL.
            args = $"tunnel --url http://localhost:{_options.Port}";
            if (!string.IsNullOrWhiteSpace(cf.Hostname))
                args += $" --hostname {cf.Hostname}";
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "cloudflared",
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        _process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        _process.Exited += OnProcessExited;
        _process.Start();
        StartOutputReaders(cancellationToken);
        _logger.LogInformation("cloudflared started (PID {Pid}), waiting for tunnel URL...", _process.Id);

        await WaitForStartupAsync(expectPublicUrl: !_isNamedTunnelMode, cancellationToken).ConfigureAwait(false);
        _startupCompleted = true;

        if (_publicUrl is not null)
        {
            _error = null;
            _logger.LogInformation("Cloudflare tunnel active: {Url}", _publicUrl);
        }
        else if (_isNamedTunnelMode)
        {
            _logger.LogInformation(
                "cloudflared named tunnel started (PID {Pid}). Public URL is not auto-detected in named tunnel mode; use the configured hostname.",
                _process.Id);
        }
        else if (!string.IsNullOrWhiteSpace(_error))
        {
            _logger.LogWarning("cloudflared started without a usable public URL: {Error}", _error);
        }
        else
        {
            _logger.LogWarning("cloudflared started but public URL not yet captured.");
        }
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
                _logger.LogInformation("Cloudflare tunnel stopped.");
            }
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            /* process exited between check and kill */
        }
        finally
        {
            StopOutputReaders();
            _publicUrl = null;
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<TunnelStatus> GetStatusAsync(CancellationToken ct = default)
    {
        if (_process is null)
            return Task.FromResult(new TunnelStatus(false, Error: _error ?? "Not started."));

        if (TryUpdateExitedProcessError(_process))
            return Task.FromResult(new TunnelStatus(false, Error: _error ?? "Not started."));

        return Task.FromResult(new TunnelStatus(true, _publicUrl, _error));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _stopRequested = true;
        StopOutputReaders();
        try
        {
            if (_process is not null && !_process.HasExited)
                _process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            /* process already exited */
        }
        if (_process is not null)
            _process.Exited -= OnProcessExited;
        _process?.Dispose();
    }

    private void ResetRuntimeStateForStart()
    {
        _stopRequested = false;
        _startupCompleted = false;
        _isNamedTunnelMode = false;
        _publicUrl = null;
        _error = null;
        _lastStdoutLine = null;
        _lastStderrLine = null;
    }

    private void StartOutputReaders(CancellationToken startupCancellationToken)
    {
        if (_process is null)
            return;

        StopOutputReaders();

        _outputPumpCts = CancellationTokenSource.CreateLinkedTokenSource(startupCancellationToken);
        var outputToken = _outputPumpCts.Token;
        _stdoutPumpTask = Task.Run(() => PumpOutputAsync(_process.StandardOutput, isStdErr: false, outputToken), CancellationToken.None);
        _stderrPumpTask = Task.Run(() => PumpOutputAsync(_process.StandardError, isStdErr: true, outputToken), CancellationToken.None);
    }

    private void StopOutputReaders()
    {
        if (_outputPumpCts is null)
            return;

        try
        {
            _outputPumpCts.Cancel();
        }
        catch (ObjectDisposedException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            // ignored
        }

        _outputPumpCts.Dispose();
        _outputPumpCts = null;
        _stdoutPumpTask = null;
        _stderrPumpTask = null;
    }

    private async Task PumpOutputAsync(StreamReader reader, bool isStdErr, CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(ct).ConfigureAwait(false);
                if (line is null)
                    break;

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (isStdErr)
                    _lastStderrLine = line;
                else
                    _lastStdoutLine = line;

                if (_publicUrl is null && TryExtractUrl(line, out var candidateUrl))
                {
                    _publicUrl = candidateUrl;
                    _logger.LogDebug("Captured Cloudflare tunnel URL: {Url}", _publicUrl);
                }
            }
        }
        catch (OperationCanceledException ex) when (ct.IsCancellationRequested)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            // expected during shutdown
        }
        catch (ObjectDisposedException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            // process stream disposed during shutdown
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            // process exited and stream became unavailable
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error reading cloudflared process output.");
        }
    }

    private async Task WaitForStartupAsync(bool expectPublicUrl, CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow + s_startupTimeout;

        while (!cancellationToken.IsCancellationRequested)
        {
            if (_process is null)
            {
                _error = "cloudflared process failed to initialize.";
                return;
            }

            if (TryUpdateExitedProcessError(_process))
                return;

            if (_publicUrl is not null)
                return;

            if (DateTime.UtcNow >= deadline)
            {
                if (expectPublicUrl)
                {
                    _error = BuildStartupTimeoutError(
                        (int)s_startupTimeout.TotalSeconds,
                        _lastStderrLine,
                        _lastStdoutLine);
                }

                return;
            }

            await Task.Delay(s_startupPollInterval, cancellationToken).ConfigureAwait(false);
        }
    }

    private void OnProcessExited(object? sender, EventArgs e)
    {
        if (_stopRequested || sender is not Process process)
            return;

        if (TryUpdateExitedProcessError(process))
            _logger.LogWarning("{Error}", _error);
    }

    private bool TryUpdateExitedProcessError(Process process)
    {
        bool hasExited;
        try
        {
            hasExited = process.HasExited;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            hasExited = true;
        }

        if (!hasExited)
            return false;

        if (_stopRequested)
            return true;

        _error ??= BuildProcessExitError(
            TryGetExitCode(process),
            _startupCompleted,
            _lastStderrLine,
            _lastStdoutLine);
        return true;
    }

    private int? TryGetExitCode(Process process)
    {
        try
        {
            return process.ExitCode;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            return null;
        }
    }

    private static bool TryExtractUrl(string line, out string? url)
    {
        url = null;

        var idx = line.IndexOf("https://", StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
            return false;

        var end = idx;
        while (end < line.Length &&
               !char.IsWhiteSpace(line[end]) &&
               line[end] != '"' &&
               line[end] != '\'' &&
               line[end] != ')' &&
               line[end] != ',')
        {
            end++;
        }

        if (end <= idx)
            return false;

        var candidate = line[idx..end].Trim();
        if (!Uri.TryCreate(candidate, UriKind.Absolute, out _))
            return false;

        url = candidate;
        return true;
    }

    private static string BuildStartupTimeoutError(
        int timeoutSeconds,
        string? lastStderrLine,
        string? lastStdoutLine)
    {
        var parts = new List<string>
        {
            $"cloudflared startup timed out after {timeoutSeconds}s waiting for a public URL to be emitted by cloudflared."
        };

        if (!string.IsNullOrWhiteSpace(lastStderrLine))
            parts.Add($"Last stderr: {TrimDiagnosticLine(lastStderrLine)}");
        if (!string.IsNullOrWhiteSpace(lastStdoutLine))
            parts.Add($"Last stdout: {TrimDiagnosticLine(lastStdoutLine)}");

        return string.Join(" ", parts);
    }

    private static string BuildProcessExitError(
        int? exitCode,
        bool startupCompleted,
        string? lastStderrLine,
        string? lastStdoutLine)
    {
        var phase = startupCompleted ? "after startup" : "during startup";
        var codeText = exitCode?.ToString() ?? "unknown";
        var parts = new List<string> { $"cloudflared process exited {phase} with exit code {codeText}." };

        if (!string.IsNullOrWhiteSpace(lastStderrLine))
            parts.Add($"Last stderr: {TrimDiagnosticLine(lastStderrLine)}");
        if (!string.IsNullOrWhiteSpace(lastStdoutLine))
            parts.Add($"Last stdout: {TrimDiagnosticLine(lastStdoutLine)}");

        return string.Join(" ", parts);
    }

    private static string TrimDiagnosticLine(string value)
    {
        var singleLine = value.Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Trim();

        const int maxLength = 240;
        return singleLine.Length <= maxLength ? singleLine : singleLine[..maxLength] + "...";
    }
}
