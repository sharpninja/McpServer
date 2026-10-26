using Xunit;

namespace QBrainAi.Support.Mcp.Tests.TestSupport;

/// <summary>
/// The process-environment variables the CLI strategies read to choose their temporary folder, in each strategy's
/// fallback order: <c>CliBrainSlotChatClient</c> reads MCPSERVER_QUADBRAIN_CLI_TEMP then MCPSERVER_ONESHOT_TEMP;
/// <c>CodexCliAgentExecutionStrategy</c> reads MCPSERVER_CODEX_ONESHOT_TEMP then MCPSERVER_CODEX_TRIAGE_TEMP;
/// <c>OneShotCliAgentExecutionStrategy</c> reads MCPSERVER_ONESHOT_TEMP, MCPSERVER_CODEX_ONESHOT_TEMP, then
/// MCPSERVER_CODEX_TRIAGE_TEMP; <c>GrokCliAgentExecutionStrategy</c> reads MCPSERVER_GROK_ONESHOT_TEMP then
/// MCPSERVER_ONESHOT_TEMP. A test that reaches one of them sets every name of the chain and restores it
/// (TR-MCP-USECASE-018 AC-006 and TR-MCP-USECASE-019 AC-008 as amended 2026-10-26; operator: "Strengthen AC, move 14
/// (Recommended)"; hostile validation finding R6-F1). Shared by the three strategy test classes.
/// </summary>
internal static class CliTempVariables
{
    /// <summary>Every name of the four fallback chains, once each.</summary>
    internal static IReadOnlyList<string> Names { get; } =
    [
        "MCPSERVER_QUADBRAIN_CLI_TEMP",
        "MCPSERVER_ONESHOT_TEMP",
        "MCPSERVER_CODEX_ONESHOT_TEMP",
        "MCPSERVER_CODEX_TRIAGE_TEMP",
        "MCPSERVER_GROK_ONESHOT_TEMP",
    ];

    /// <summary>
    /// Runs <paramref name="body"/> with every name of <see cref="Names"/> pointing at one new folder under the
    /// official temporary folder, then restores every name to its earlier value (an unset name stays unset) and
    /// deletes the folder, also when the body throws. Callers belong to <see cref="ProcessEnvironmentCollection"/>.
    /// </summary>
    /// <param name="body">The test body; it receives the folder.</param>
    /// <returns>A task that completes when the body has run and the variables are restored.</returns>
    internal static async Task RunWithTestFolderAsync(Func<string, Task> body)
    {
        ArgumentNullException.ThrowIfNull(body);
        var previous = Snapshot();
        var folder = Path.Combine(Path.GetTempPath(), "qbt-cli-temp-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            foreach (var name in Names)
                Environment.SetEnvironmentVariable(name, folder);
            await body(folder).ConfigureAwait(true);
        }
        finally
        {
            foreach (var (name, value) in previous)
                Environment.SetEnvironmentVariable(name, value);
            try
            {
                Directory.Delete(folder, recursive: true);
            }
            catch (IOException)
            {
            }
        }
    }

    /// <summary>Reads the current value of every name of <see cref="Names"/> from the process environment.</summary>
    /// <returns>The values by name, null for an unset name.</returns>
    internal static Dictionary<string, string?> Snapshot() =>
        Names.ToDictionary(name => name, name => Environment.GetEnvironmentVariable(name), StringComparer.Ordinal);

    /// <summary>
    /// Asserts that a snapshot of the variables holds every name of <see cref="Names"/> and that each one points at
    /// <paramref name="folder"/>.
    /// </summary>
    /// <param name="observed">The variable values seen, by name.</param>
    /// <param name="folder">The folder every name must point at.</param>
    internal static void AssertAllAt(IReadOnlyDictionary<string, string?> observed, string folder)
    {
        ArgumentNullException.ThrowIfNull(observed);
        foreach (var name in Names)
        {
            Assert.True(observed.TryGetValue(name, out var value), $"{name} was not observed.");
            Assert.Equal(folder, value);
        }
    }

    /// <summary>
    /// Asserts that every name of <see cref="Names"/> holds the same value after a run as before it, an unset name
    /// included.
    /// </summary>
    /// <param name="before">The values before the run, by name.</param>
    /// <param name="after">The values after the run, by name.</param>
    internal static void AssertRestored(IReadOnlyDictionary<string, string?> before, IReadOnlyDictionary<string, string?> after)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);
        foreach (var name in Names)
        {
            Assert.True(before.TryGetValue(name, out var expected), $"{name} was not recorded before the run.");
            Assert.True(after.TryGetValue(name, out var actual), $"{name} was not recorded after the run.");
            Assert.Equal(expected, actual);
        }
    }
}
