// SPDX-License-Identifier: Apache-2.0
namespace QBrainAi.Support.Mcp.Tests.McpStdio;

/// <summary>TEST-MCP-TRANSCRIPT-008/TEST-QB-069: drains only the fixture-owned readers without losing unrelated failures.</summary>
public static class OwnedStdioReaderDrain
{
    /// <summary>Cancels owned readers only for a still-live owned child, then observes both readers in order.</summary>
    /// <param name="isOwnedChildAlive">Actual state of the original captured child object.</param>
    /// <param name="readerCancellation">Cancellation source owned exclusively by these readers.</param>
    /// <param name="stdout">The already-started stdout reader task.</param>
    /// <param name="stderr">The already-started stderr reader task.</param>
    /// <param name="failures">Every uncontrolled reader exception, in observed order.</param>
    /// <param name="observe">Optional test-only interaction observer.</param>
    /// <returns>Completion after both readers have been observed.</returns>
    public static async Task DrainAsync(
        Func<bool> isOwnedChildAlive, CancellationTokenSource readerCancellation,
        Func<Task> stdout, Func<Task> stderr, ICollection<Exception> failures,
        Action<string>? observe = null)
    {
        observe?.Invoke("inspect-owned-exit");
        if (isOwnedChildAlive())
        {
            observe?.Invoke("cancel-owned-readers");
            await readerCancellation.CancelAsync().ConfigureAwait(false);
        }
        foreach (var reader in new[] { (Name: "await-stdout", Read: stdout), (Name: "await-stderr", Read: stderr) })
        {
            observe?.Invoke(reader.Name);
            try { await reader.Read().ConfigureAwait(false); }
            catch (OperationCanceledException exception) when (
                readerCancellation.IsCancellationRequested && exception.CancellationToken == readerCancellation.Token)
            {
                // Only cancellation requested for these exact owned readers is cleanup.
            }
            catch (Exception exception) { failures.Add(exception); }
        }
    }
}
