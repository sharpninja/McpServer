using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Logging;

namespace McpServer.Common.Copilot;

/// <summary>
/// Manages a persistent interactive Copilot CLI process launched with <c>-i</c>.
/// Subsequent prompts are written to stdin; responses are read from stdout
/// until the "Esc to cancel" sentinel indicates Copilot is ready for input.
/// </summary>
public sealed class CopilotInteractiveSession : IAsyncDisposable
{
    private const string Sentinel = "Esc to cancel";
    private const int OutputTailLineLimit = 40;
    private const int OutputTailLineMaxChars = 300;

    private readonly ISpawnedProcess _process;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Task _stderrDrainTask;
    private readonly Task _exitMonitorTask;
    private readonly object _outputTailLock = new();
    private readonly Queue<string> _stdoutTail = new();
    private readonly Queue<string> _stderrTail = new();
    private int _shutdownRequested;
    private int _exitDiagnosticsLogged;
    private bool _disposed;

    internal CopilotInteractiveSession(ISpawnedProcess process, ILogger logger)
    {
        _process = process;
        _logger = logger;
        _stderrDrainTask = DrainStderrAsync();
        _exitMonitorTask = MonitorProcessExitAsync();
    }

    /// <summary>Returns <c>true</c> when the underlying process is still running.</summary>
    public bool IsAlive => !_disposed && !_process.HasExited;

    /// <summary>Gets the OS process ID of the Copilot CLI process.</summary>
    public int ProcessId => _process.Id;

    /// <summary>
    /// Reads the initial response produced by the <c>-i</c> prompt.
    /// Call once immediately after creation.
    /// </summary>
    public Task<CopilotResult> ReadInitialResponseAsync(CancellationToken ct = default)
        => ReadUntilSentinelAsync(ct);

    /// <summary>
    /// Streams the initial response line-by-line. Call once immediately after creation.
    /// </summary>
    public async IAsyncEnumerable<string> ReadInitialResponseStreamingAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            while (!ct.IsCancellationRequested)
            {
                string? line;
                string? timestamped;
                try
                {
                    line = await _process.StandardOutput.ReadLineAsync(ct).ConfigureAwait(false) + '\n';
                    timestamped = $"{DateTimeOffset.Now.ToLocalTime():t}: {line}";
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                if (timestamped is null) break;
                if (timestamped.Contains(Sentinel, StringComparison.Ordinal)) break;
                AppendOutputTail(_stdoutTail, timestamped);
                yield return LineSanitizer.Sanitize(timestamped);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Sends three ESC characters (<c>\x1B\x1B\x1B</c>) to the Copilot CLI process stdin
    /// to immediately cancel the current generation without ending the session.
    /// </summary>
    public async Task SendEscapeAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_process.HasExited) return;

        const string EscChars = "\x1B\x1B\x1B";
        try
        {
            await _process.StandardInput!.WriteAsync(EscChars.AsMemory(), ct).ConfigureAwait(false);
            await _process.StandardInput!.FlushAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException or InvalidOperationException)
        {
            LogExitDiagnostics("stdin-escape-write-failed", ex);
            _logger.LogWarning(ex, "Interactive session escape write failed; Copilot process is no longer writable.");
        }
    }

    /// <summary>Sends a prompt via stdin and reads the response until the sentinel.</summary>
    public async Task<CopilotResult> SendAsync(string prompt, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_process.HasExited)
            return new CopilotResult { State = CopilotResultState.Error, Body = "Copilot process has exited." };

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            try
            {
                await _process.StandardInput!.WriteLineAsync(prompt.AsMemory(), ct).ConfigureAwait(false);
                await _process.StandardInput!.FlushAsync(ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is IOException or ObjectDisposedException or InvalidOperationException)
            {
                await CaptureRemainingStdoutTailUnsafeAsync(ct).ConfigureAwait(false);
                LogExitDiagnostics("stdin-write-failed", ex);
                _logger.LogWarning(ex, "Interactive session stdin write failed; Copilot process is no longer writable.");
                return new CopilotResult
                {
                    State = CopilotResultState.Error,
                    Body = "Copilot interactive session is no longer writable.",
                };
            }

            return await ReadUntilSentinelAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Sends a prompt and streams response lines until the sentinel.</summary>
    public async IAsyncEnumerable<string> SendStreamingAsync(
        string prompt,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_process.HasExited) yield break;

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            try
            {
                await _process.StandardInput!.WriteLineAsync(prompt.AsMemory(), ct).ConfigureAwait(false);
                await _process.StandardInput!.FlushAsync(ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is IOException or ObjectDisposedException or InvalidOperationException)
            {
                await CaptureRemainingStdoutTailUnsafeAsync(ct).ConfigureAwait(false);
                LogExitDiagnostics("stdin-streaming-write-failed", ex);
                _logger.LogWarning(ex, "Interactive session streaming write failed; Copilot process is no longer writable.");
                yield break;
            }

            while (!ct.IsCancellationRequested)
            {
                string? line;
                string? timestamped;
                try
                {
                    line = await _process.StandardOutput.ReadLineAsync(ct).ConfigureAwait(false) + '\n';
                    timestamped = $"{DateTimeOffset.Now.ToLocalTime():t}: {line}";
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                if (timestamped is null) break;
                if (timestamped.Contains(Sentinel, StringComparison.Ordinal)) break;
                AppendOutputTail(_stdoutTail, timestamped);
                yield return LineSanitizer.Sanitize(timestamped);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Sends "End Session" and waits for the acknowledgement sentinel
    /// before terminating the process.
    /// </summary>
    public async Task EndAsync(TimeSpan timeout)
    {
        if (_disposed || _process.HasExited) return;

        MarkShutdownRequested();
        try
        {
            await _process.StandardInput!.WriteLineAsync("End Session");
            await _process.StandardInput!.FlushAsync();

            using var cts = new CancellationTokenSource(timeout);
            await ReadUntilSentinelAsync(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("End-session acknowledgement timed out, killing process.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during interactive session shutdown.");
        }

        TryKillProcess();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        MarkShutdownRequested();

        TryKillProcess();

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            if (!_process.HasExited)
                await _process.WaitForExitAsync(cts.Token).ConfigureAwait(false);
        }
        catch
        {
            // Best-effort wait
        }

        try
        {
            await _stderrDrainTask.ConfigureAwait(false);
        }
        catch (ObjectDisposedException)
        {
            // Stream already disposed.
        }

        try
        {
            await _exitMonitorTask.ConfigureAwait(false);
        }
        catch (ObjectDisposedException)
        {
            // Process already disposed.
        }

        _process.Dispose();
        _gate.Dispose();
    }

    // ── Internals ──────────────────────────────────────────────────────

    private async Task<CopilotResult> ReadUntilSentinelAsync(CancellationToken ct)
    {
        var sb = new StringBuilder();

        while (!ct.IsCancellationRequested)
        {
            string? line;
            string? timestamped;
            try
            {
                line = await _process.StandardOutput.ReadLineAsync(ct).ConfigureAwait(false) + '\n';
                timestamped = $"{DateTimeOffset.Now.ToLocalTime():t}: {line}";
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (timestamped is null)
            {
                // Process exited
                if (_process.HasExited && _process.ExitCode != 0)
                {
                    LogExitDiagnostics("stdout-closed-nonzero-exit", exception: null);
                    return new CopilotResult
                    {
                        State = CopilotResultState.Error,
                        Body = sb.ToString().Trim(),
                        ExitCode = _process.ExitCode,
                    };
                }
                break;
            }

            if (timestamped.Contains(Sentinel, StringComparison.Ordinal))
                break;

            AppendOutputTail(_stdoutTail, timestamped);
            sb.AppendLine(timestamped);
        }

        var body = sb.ToString().Trim();
        var (contentType, parsed) = ContentParser.DetectAndParse(body);

        return new CopilotResult
        {
            State = CopilotResultState.Success,
            Body = body,
            Parsed = parsed,
            ContentType = contentType,
        };
    }

    private async Task DrainStderrAsync()
    {
        try
        {
            while (true)
            {
                var line = await _process.StandardError.ReadLineAsync().ConfigureAwait(false);
                if (line is null) break;
                if (!string.IsNullOrWhiteSpace(line))
                {
                    AppendOutputTail(_stderrTail, line);
                    _logger.LogWarning("Copilot stderr: {Line}", line);
                }
            }
        }
        catch (ObjectDisposedException)
        {
            // Process exited/disposed while reading stderr.
        }
        catch
        {
            // Process exited or disposed
        }
    }

    private async Task MonitorProcessExitAsync()
    {
        try
        {
            await _process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            await CaptureRemainingStdoutTailAsync(CancellationToken.None).ConfigureAwait(false);
            LogExitDiagnostics("process-exited", exception: null);
        }
        catch (ObjectDisposedException)
        {
            // Process already disposed.
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed while monitoring interactive Copilot process exit.");
        }
    }

    private void MarkShutdownRequested()
        => Interlocked.Exchange(ref _shutdownRequested, 1);

    private async Task CaptureRemainingStdoutTailAsync(CancellationToken cancellationToken)
    {
        var acquired = false;
        try
        {
            acquired = await _gate.WaitAsync(TimeSpan.FromMilliseconds(500), cancellationToken).ConfigureAwait(false);
            if (!acquired)
                return;

            await CaptureRemainingStdoutTailUnsafeAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (ObjectDisposedException)
        {
            // Gate/process already disposed.
        }
        finally
        {
            if (acquired)
                _gate.Release();
        }
    }

    private async Task CaptureRemainingStdoutTailUnsafeAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(500));
            var remaining = await _process.StandardOutput.ReadToEndAsync(timeoutCts.Token).ConfigureAwait(false);
            AppendOutputTailBlock(_stdoutTail, remaining);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Timed out reading remaining stdout; keep existing tail.
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException or InvalidOperationException)
        {
            _logger.LogDebug(ex, "Could not read remaining stdout while capturing exit diagnostics.");
        }
    }

    private void LogExitDiagnostics(string reason, Exception? exception)
    {
        if (Interlocked.Exchange(ref _exitDiagnosticsLogged, 1) != 0)
            return;

        var expectedShutdown = Volatile.Read(ref _shutdownRequested) == 1 || _disposed;
        var exitCode = GetExitCodeSnapshot();
        var stdoutTail = SnapshotOutputTail(_stdoutTail);
        var stderrTail = SnapshotOutputTail(_stderrTail);

        if (expectedShutdown)
        {
            _logger.LogInformation(
                "Interactive Copilot process closed ({Reason}): PID={ProcessId}; ExitCode={ExitCode}; StdoutTail={StdoutTail}; StderrTail={StderrTail}",
                reason,
                ProcessId,
                exitCode,
                stdoutTail,
                stderrTail);
            return;
        }

        if (exception is null)
        {
            _logger.LogWarning(
                "Interactive Copilot process closed unexpectedly ({Reason}): PID={ProcessId}; ExitCode={ExitCode}; StdoutTail={StdoutTail}; StderrTail={StderrTail}",
                reason,
                ProcessId,
                exitCode,
                stdoutTail,
                stderrTail);
            return;
        }

        _logger.LogWarning(
            exception,
            "Interactive Copilot process closed unexpectedly ({Reason}): PID={ProcessId}; ExitCode={ExitCode}; StdoutTail={StdoutTail}; StderrTail={StderrTail}",
            reason,
            ProcessId,
            exitCode,
            stdoutTail,
            stderrTail);
    }

    private string GetExitCodeSnapshot()
    {
        try
        {
            if (!_process.HasExited)
                return "(running)";

            return _process.ExitCode.ToString();
        }
        catch (Exception ex) when (ex is InvalidOperationException or ObjectDisposedException)
        {
            _logger.LogDebug(ex, "Unable to read interactive process exit code for diagnostics.");
            return "(unavailable)";
        }
    }

    private void AppendOutputTail(Queue<string> target, string line)
    {
        var sanitized = LineSanitizer.Sanitize(line);
        if (string.IsNullOrWhiteSpace(sanitized))
            return;

        if (sanitized.Length > OutputTailLineMaxChars)
            sanitized = sanitized[..OutputTailLineMaxChars] + "...(truncated)";

        lock (_outputTailLock)
        {
            target.Enqueue(sanitized);
            while (target.Count > OutputTailLineLimit)
                target.Dequeue();
        }
    }

    private void AppendOutputTailBlock(Queue<string> target, string? block)
    {
        if (string.IsNullOrWhiteSpace(block))
            return;

        using var reader = new StringReader(block);
        while (reader.ReadLine() is { } line)
            AppendOutputTail(target, line);
    }

    private string SnapshotOutputTail(Queue<string> source)
    {
        lock (_outputTailLock)
        {
            return source.Count == 0
                ? "(none)"
                : string.Join(Environment.NewLine, source);
        }
    }

    private void TryKillProcess()
    {
        try
        {
            if (!_process.HasExited)
                _process.Kill();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to kill interactive Copilot process.");
        }
    }
}
