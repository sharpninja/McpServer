using System.Diagnostics;
using System.Text.Json;
using McpServer.Support.Mcp.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Tunnel provider using <c>ngrok</c> CLI. Starts <c>ngrok http {port}</c>
/// and reads the public URL from the ngrok local API.
/// </summary>
public sealed class NgrokTunnelProvider : ITunnelProvider, IDisposable
{
    private static readonly TimeSpan s_startupPollInterval = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan s_startupTimeout = TimeSpan.FromSeconds(8);

    /// <inheritdoc />
    public string ProviderName => "ngrok";

    private readonly TunnelOptions _options;
    private readonly IProcessRunner _processRunner;
    private readonly ILogger<NgrokTunnelProvider> _logger;
    private Process? _process;
    private CancellationTokenSource? _outputPumpCts;
    private Task? _stdoutPumpTask;
    private Task? _stderrPumpTask;
    private string? _publicUrl;
    private string? _error;
    private string? _lastStdoutLine;
    private string? _lastStderrLine;
    private string? _lastApiQueryError;
    private bool _startupCompleted;
    private bool _stopRequested;

    /// <summary>Initializes a new instance of the <see cref="NgrokTunnelProvider"/> class.</summary>
    public NgrokTunnelProvider(IOptions<TunnelOptions> options, IProcessRunner processRunner, ILogger<NgrokTunnelProvider> logger)
    {
        _options = options.Value;
        _processRunner = processRunner;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        ResetRuntimeStateForStart();

        var ngrokExe = ResolveExecutablePath();

        // Verify ngrok is installed.
        var check = await _processRunner.RunAsync(ngrokExe, "version", cancellationToken).ConfigureAwait(false);
        if (check.ExitCode != 0)
        {
            _error = "ngrok CLI not found. Install from https://ngrok.com/download";
            _logger.LogError("{Error}", _error);
            return;
        }

        var ngrok = _options.Ngrok;
        var args = $"http {_options.Port} --log stdout --log-format json";
        if (!string.IsNullOrWhiteSpace(ngrok.Subdomain))
            args += $" --subdomain {ngrok.Subdomain}";
        if (!string.IsNullOrWhiteSpace(ngrok.Region))
            args += $" --region {ngrok.Region}";

        var startInfo = new ProcessStartInfo
        {
            FileName = ngrokExe,
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        // Pass auth token via environment variable to avoid exposure in process listing.
        if (!string.IsNullOrWhiteSpace(ngrok.AuthToken))
            startInfo.Environment["NGROK_AUTHTOKEN"] = ngrok.AuthToken;

        _process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        _process.Exited += OnProcessExited;
        _process.Start();
        StartOutputReaders(cancellationToken);
        _logger.LogInformation("ngrok started (PID {Pid}), waiting for tunnel URL...", _process.Id);

        await WaitForPublicUrlOrTimeoutAsync(cancellationToken).ConfigureAwait(false);
        _startupCompleted = true;

        if (_publicUrl is not null)
        {
            _error = null;
            _logger.LogInformation("ngrok tunnel active: {Url}", _publicUrl);
        }
        else if (!string.IsNullOrWhiteSpace(_error))
        {
            _logger.LogWarning("ngrok started without a usable public URL: {Error}", _error);
        }
        else
        {
            _logger.LogWarning("ngrok started but public URL not yet available.");
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
                _logger.LogInformation("ngrok tunnel stopped.");
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
    public async Task<TunnelStatus> GetStatusAsync(CancellationToken ct = default)
    {
        if (_process is null)
            return new TunnelStatus(false, Error: _error ?? "Not started.");

        if (TryUpdateExitedProcessError(_process))
            return new TunnelStatus(false, Error: _error ?? "Not started.");

        if (_publicUrl is null)
            await RefreshPublicUrlAsync(ct).ConfigureAwait(false);

        return new TunnelStatus(true, _publicUrl, _error);
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
        _publicUrl = null;
        _error = null;
        _lastStdoutLine = null;
        _lastStderrLine = null;
        _lastApiQueryError = null;
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
            _logger.LogDebug(ex, "Failed while reading ngrok process output.");
        }
    }

    private async Task WaitForPublicUrlOrTimeoutAsync(CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow + s_startupTimeout;

        while (!cancellationToken.IsCancellationRequested)
        {
            if (_process is null)
            {
                _error = "ngrok process failed to initialize.";
                return;
            }

            if (TryUpdateExitedProcessError(_process))
                return;

            await RefreshPublicUrlAsync(cancellationToken).ConfigureAwait(false);
            if (_publicUrl is not null)
                return;

            if (DateTime.UtcNow >= deadline)
            {
                _error = BuildStartupTimeoutError(
                    (int)s_startupTimeout.TotalSeconds,
                    _lastApiQueryError,
                    _lastStderrLine,
                    _lastStdoutLine);
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

    private async Task RefreshPublicUrlAsync(CancellationToken ct)
    {
        try
        {
            var result = await _processRunner.RunAsync("curl", "-s http://127.0.0.1:4040/api/tunnels", ct).ConfigureAwait(false);
            if (result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.Stdout))
            {
                var doc = JsonDocument.Parse(result.Stdout);
                var tunnels = doc.RootElement.GetProperty("tunnels");
                if (tunnels.GetArrayLength() > 0)
                {
                    string? fallbackUrl = null;
                    foreach (var tunnel in tunnels.EnumerateArray())
                    {
                        if (!tunnel.TryGetProperty("public_url", out var publicUrlElement))
                            continue;

                        var candidate = publicUrlElement.GetString();
                        if (string.IsNullOrWhiteSpace(candidate))
                            continue;

                        fallbackUrl ??= candidate;
                        if (candidate.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                        {
                            _publicUrl = candidate;
                            _lastApiQueryError = null;
                            return;
                        }
                    }

                    _publicUrl = fallbackUrl;
                    if (_publicUrl is not null)
                        _lastApiQueryError = null;
                }
            }
            else if (result.ExitCode != 0)
            {
                _lastApiQueryError = string.IsNullOrWhiteSpace(result.Stderr)
                    ? $"curl exited with code {result.ExitCode}."
                    : $"curl exited with code {result.ExitCode}: {TrimDiagnosticLine(result.Stderr)}";
            }
        }
        catch (Exception ex)
        {
            _lastApiQueryError = ex.Message;
            _logger.LogDebug(ex, "Failed to query ngrok API for public URL.");
        }
    }

    private static string BuildStartupTimeoutError(
        int timeoutSeconds,
        string? apiQueryError,
        string? lastStderrLine,
        string? lastStdoutLine)
    {
        var parts = new List<string>
        {
            $"ngrok startup timed out after {timeoutSeconds}s waiting for a public URL from the ngrok local API (http://127.0.0.1:4040/api/tunnels)."
        };

        if (!string.IsNullOrWhiteSpace(apiQueryError))
            parts.Add($"Last ngrok API query error: {TrimDiagnosticLine(apiQueryError)}");
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
        var parts = new List<string> { $"ngrok process exited {phase} with exit code {codeText}." };

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

    /// <summary>
    /// Returns the ngrok executable path from <see cref="NgrokTunnelOptions.ExecutablePath"/>
    /// or falls back to the bare <c>ngrok</c> command name (resolved via PATH).
    /// </summary>
    private string ResolveExecutablePath()
    {
        var configured = _options.Ngrok.ExecutablePath;
        if (!string.IsNullOrWhiteSpace(configured))
        {
            _logger.LogDebug("Using configured ngrok path: {Path}", configured);
            return configured;
        }

        return "ngrok";
    }
}
