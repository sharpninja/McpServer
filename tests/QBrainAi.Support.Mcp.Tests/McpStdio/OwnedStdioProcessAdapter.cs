// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.Json;

namespace QBrainAi.Support.Mcp.Tests.McpStdio;

/// <summary>
/// TEST-MCP-TRANSCRIPT-008/TEST-QB-069: uncompiled private diagnostic adapter
/// for the original fixture-owned Process. Reuses its existing readers and
/// output predicate; creates no host, lifecycle, transport or second process.
/// </summary>
internal static class OwnedStdioProcessAdapter
{
    /// <summary>
    /// Executes the diagnostic orchestration through the exact owned process,
    /// retaining the first actual exception and all observed reader failures.
    /// Original assertions run only when this returns without a failure.
    /// </summary>
    /// <param name="process">The original test's already-started owned Process.</param>
    /// <param name="messages">The original unmodified JSON-RPC messages.</param>
    /// <param name="capture">The original CaptureAsync method.</param>
    /// <param name="waitForOutput">The original WaitForOutputAsync method.</param>
    /// <param name="cancellationToken">The original test cancellation token.</param>
    /// <returns>Available captured output and actual diagnostic effects.</returns>
    public static async Task<OwnedStdioDiagnosticBinding.Observation> RunAsync(
        Process process,
        IEnumerable<string> messages,
        Func<StreamReader, StringBuilder, CancellationToken, Task> capture,
        Func<StringBuilder, Func<string, bool>, TimeSpan, CancellationToken, Task> waitForOutput,
        CancellationToken cancellationToken)
    {
        var ownedPid = process.Id;
        var ownedCreatedAt = process.StartTime.ToUniversalTime();
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        using var readerCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Task stdoutTask = Task.CompletedTask;
        Task stderrTask = Task.CompletedTask;
        var inputClosed = false;
        Exception? firstFailure = null;
        var readerFailures = new List<Exception>();

        static string Snapshot(StringBuilder source)
        {
            lock (source)
                return source.ToString();
        }

        async Task<bool> StepAsync(string stage, int? seconds)
        {
            try
            {
                switch (stage)
                {
                    case "start":
                        // The original StartStdioHost owns creation. Never look up
                        // another process by a supplied or discovered PID.
                        if (process.Id != ownedPid)
                            throw new InvalidOperationException("Owned process identity changed.");
                        break;
                    case "capture-both":
                        stdoutTask = capture(process.StandardOutput, stdout, readerCancellation.Token);
                        stderrTask = capture(process.StandardError, stderr, readerCancellation.Token);
                        break;
                    case "send":
                        foreach (var message in messages)
                        {
                            await process.StandardInput.WriteLineAsync(message.AsMemory(), cancellationToken).ConfigureAwait(false);
                            await process.StandardInput.FlushAsync(cancellationToken).ConfigureAwait(false);
                        }
                        break;
                    case "wait-output":
                        await waitForOutput(
                            stdout,
                            text => text.Contains("compatibilityArtifactPath", StringComparison.Ordinal),
                            TimeSpan.FromSeconds(seconds ?? throw new InvalidOperationException("Output limit missing.")),
                            cancellationToken).ConfigureAwait(false);
                        break;
                    case "close-input":
                        process.StandardInput.Close();
                        inputClosed = true;
                        break;
                    case "wait-exit":
                        using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(
                            seconds ?? throw new InvalidOperationException("Exit limit missing."))))
                        using (var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token))
                            await process.WaitForExitAsync(linked.Token).ConfigureAwait(false);
                        break;
                    case "inspect-owned-exit":
                        return process.HasExited;
                    case "kill-owned-tree":
                        process.Kill(entireProcessTree: true);
                        break;
                    case "verify-owned-exit":
                        // This bounds failure cleanup only. It cannot rescue a
                        // failed original 45/30 second wait or suppress its error.
                        using (var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                            await process.WaitForExitAsync(cleanup.Token).ConfigureAwait(false);
                        break;
                    case "drain":
                        // A failed kill must not leave readers waiting forever.
                        // Cancel only these two readers; retain captured buffers.
                        await OwnedStdioReaderDrain.DrainAsync(
                            () => !process.HasExited, readerCancellation,
                            () => stdoutTask, () => stderrTask, readerFailures).ConfigureAwait(false);
                        if (readerFailures.Count > 0)
                            throw new AggregateException("Owned STDIO reader failures.", readerFailures);
                        break;
                    default:
                        throw new InvalidOperationException("Unknown owned child operation: " + stage);
                }
                return false;
            }
            catch (Exception exception)
            {
                firstFailure ??= exception;
                throw;
            }
        }

        var observation = await OwnedStdioDiagnosticBinding.RunAsync(
            StepAsync, () => ownedPid, () => Snapshot(stdout), () => Snapshot(stderr),
            () => inputClosed, () => !process.HasExited).ConfigureAwait(false);

        if (firstFailure is not null)
        {
            firstFailure.Data["OwnedStdioDiagnostic"] = JsonSerializer.Serialize(observation);
            firstFailure.Data["OwnedProcessId"] = ownedPid;
            firstFailure.Data["OwnedProcessCreatedAtUtc"] = ownedCreatedAt.ToString("O");
            firstFailure.Data["OwnedReaderFailures"] = readerFailures.Select(exception => exception.ToString()).ToArray();
            ExceptionDispatchInfo.Capture(firstFailure).Throw();
        }

        return observation;
    }
}
