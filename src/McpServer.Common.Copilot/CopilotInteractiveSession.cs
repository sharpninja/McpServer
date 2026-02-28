using System.Diagnostics;
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

    private readonly Process _process;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Task _stderrDrainTask;
    private bool _disposed;

    internal CopilotInteractiveSession(Process process, ILogger logger)
    {
        _process = process;
        _logger = logger;
        _stderrDrainTask = DrainStderrAsync();
    }

    /// <summary>Returns <c>true</c> when the underlying process is still running.</summary>
    public bool IsAlive => !_disposed && !_process.HasExited;

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
                try
                {
                    line = await _process.StandardOutput.ReadLineAsync(ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                if (line is null) break;
                if (line.Contains(Sentinel, StringComparison.Ordinal)) break;
                yield return line;
            }
        }
        finally
        {
            _gate.Release();
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
            await _process.StandardInput.WriteLineAsync(prompt.AsMemory(), ct).ConfigureAwait(false);
            await _process.StandardInput.FlushAsync(ct).ConfigureAwait(false);
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
            await _process.StandardInput.WriteLineAsync(prompt.AsMemory(), ct).ConfigureAwait(false);
            await _process.StandardInput.FlushAsync(ct).ConfigureAwait(false);

            while (!ct.IsCancellationRequested)
            {
                string? line;
                try
                {
                    line = await _process.StandardOutput.ReadLineAsync(ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                if (line is null) break;
                if (line.Contains(Sentinel, StringComparison.Ordinal)) break;
                yield return line;
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

        try
        {
            await _process.StandardInput.WriteLineAsync("End Session");
            await _process.StandardInput.FlushAsync();

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

        try { await _stderrDrainTask.ConfigureAwait(false); }
        catch { /* drain complete or process killed */ }

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
            try
            {
                line = await _process.StandardOutput.ReadLineAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (line is null)
            {
                // Process exited
                if (_process.HasExited && _process.ExitCode != 0)
                {
                    return new CopilotResult
                    {
                        State = CopilotResultState.Error,
                        Body = sb.ToString().Trim(),
                        ExitCode = _process.ExitCode,
                    };
                }
                break;
            }

            if (line.Contains(Sentinel, StringComparison.Ordinal))
                break;

            sb.AppendLine(line);
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
                    _logger.LogWarning("Copilot stderr: {Line}", line);
            }
        }
        catch
        {
            // Process exited or disposed
        }
    }

    private void TryKillProcess()
    {
        try
        {
            if (!_process.HasExited)
                _process.Kill(entireProcessTree: true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to kill interactive Copilot process.");
        }
    }
}
