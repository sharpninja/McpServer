// SPDX-License-Identifier: Apache-2.0
using System.Text.Json;

namespace QBrainAi.Support.Mcp.Tests.McpStdio;

/// <summary>
/// TEST-MCP-TRANSCRIPT-008/TEST-QB-069: private draft of the diagnostic-only
/// orchestration shared by scripted fixture dependencies and the original owned
/// STDIO child. This owns no MCP lifecycle or transport and is not compiled yet.
/// </summary>
public static class OwnedStdioDiagnosticBinding
{
    /// <summary>Captured effects of calls to the actual dependency boundary.</summary>
    public sealed record Effect(string Stage, int Pid, int? Seconds = null);

    /// <summary>Original failure and complete available captured streams, with honest exit evidence.</summary>
    public sealed record Observation(
        IReadOnlyList<Effect> Effects, string? Failure, IReadOnlyList<string> CleanupFailures,
        string Stdout, string Stderr, bool ExitVerified, bool Alive, bool InputClosed,
        int OutputSeconds = 45, int ExitSeconds = 30);

    /// <summary>
    /// Executes fixture IO through one captured task-owned child boundary. A phase
    /// callback performs the actual operation, including waits and stream drain;
    /// it does not return case-name observations. Exit confirmation is only set
    /// after a successful actual wait. Every original exception is retained.
    /// </summary>
    /// <param name="step">Actual owned-child callback; inspect-owned-exit returns HasExited.</param>
    /// <param name="getOwnedPid">The identity captured from that exact started Process object.</param>
    /// <param name="getStdout">Complete available stdout captured by that child's reader.</param>
    /// <param name="getStderr">Complete available stderr captured by that child's reader.</param>
    /// <param name="getInputClosed">Independently observed state of that exact child input writer.</param>
    /// <param name="getAlive">Independently observed state of that exact owned child.</param>
    /// <returns>Diagnostic evidence; callers preserve the original failing test outcome.</returns>
    public static async Task<Observation> RunAsync(
        Func<string, int?, Task<bool>> step, Func<int> getOwnedPid,
        Func<string> getStdout, Func<string> getStderr, Func<bool> getInputClosed, Func<bool> getAlive)
    {
        var effects = new List<Effect>();
        var cleanupFailures = new List<string>();
        Exception? originalFailure = null;
        var started = false;
        var inputClosed = false;
        var exitVerified = false;
        var alive = true;
        var pid = 0;

        async Task<bool> InvokeAsync(string stage, int? seconds = null)
        {
            effects.Add(new Effect(stage, pid, seconds));
            return await step(stage, seconds).ConfigureAwait(false);
        }

        static string Describe(Exception exception) => exception.GetType().Name + ":" + exception.Message;

        try
        {
            // Start establishes the real identity before recording or touching it.
            await step("start", null).ConfigureAwait(false);
            pid = getOwnedPid();
            started = true;
            effects.Add(new Effect("start", pid));
            await InvokeAsync("capture-both").ConfigureAwait(false);
            await InvokeAsync("send").ConfigureAwait(false);
            await InvokeAsync("wait-output", 45).ConfigureAwait(false);
            await InvokeAsync("close-input").ConfigureAwait(false);
            inputClosed = true;
            await InvokeAsync("wait-exit", 30).ConfigureAwait(false);
            exitVerified = true;
            alive = false;
        }
        catch (Exception exception)
        {
            originalFailure = exception;
        }

        if (originalFailure is not null && started)
        {
            if (!inputClosed)
            {
                try
                {
                    await InvokeAsync("close-input").ConfigureAwait(false);
                    inputClosed = true;
                }
                catch (Exception exception) { cleanupFailures.Add(Describe(exception)); }
            }

            try
            {
                alive = !await InvokeAsync("inspect-owned-exit").ConfigureAwait(false);
                if (alive)
                    await InvokeAsync("kill-owned-tree").ConfigureAwait(false);
                await InvokeAsync("verify-owned-exit").ConfigureAwait(false);
                exitVerified = true;
                alive = false;
            }
            catch (Exception exception)
            {
                // Kill or verification failure must never be presented as exit proof.
                exitVerified = false;
                cleanupFailures.Add(Describe(exception));
            }
        }

        if (started)
        {
            try { await InvokeAsync("drain").ConfigureAwait(false); }
            catch (Exception exception) { cleanupFailures.Add(Describe(exception)); }
        }

        return new Observation(effects, originalFailure is null ? null : Describe(originalFailure),
            cleanupFailures, getStdout(), getStderr(), exitVerified, started && getAlive(), getInputClosed());
    }
}
