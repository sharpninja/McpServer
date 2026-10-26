using Xunit;

namespace QBrainAi.Support.Mcp.Tests.TestSupport;

/// <summary>
/// The process-environment variables the CLI strategies read to choose their temporary folder, in each strategy's
/// fallback order: <c>CliBrainSlotChatClient</c> reads MCPSERVER_QUADBRAIN_CLI_TEMP then MCPSERVER_ONESHOT_TEMP;
/// <c>CodexCliAgentExecutionStrategy</c> reads MCPSERVER_CODEX_ONESHOT_TEMP then MCPSERVER_CODEX_TRIAGE_TEMP;
/// <c>OneShotCliAgentExecutionStrategy</c> reads MCPSERVER_ONESHOT_TEMP, MCPSERVER_CODEX_ONESHOT_TEMP, then
/// MCPSERVER_CODEX_TRIAGE_TEMP; <c>GrokCliAgentExecutionStrategy</c> reads MCPSERVER_GROK_ONESHOT_TEMP then
/// MCPSERVER_ONESHOT_TEMP. A test that reaches one of them sets every name of the chain and restores it: the name the
/// strategy under test reads first points at the test folder and every other name at a separate test-owned fallback
/// folder, so an assertion on the test folder proves the primary name won over a set fallback (TR-MCP-USECASE-018
/// AC-006 and TR-MCP-USECASE-019 AC-008 as amended 2026-10-26; operator: "Strengthen AC, move 14 (Recommended)" and
/// "Dependence + proof AC (Recommended)"; hostile validation finding R6-F1). Shared by the three strategy test classes.
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
    /// Runs <paramref name="body"/> with <paramref name="primaryName"/> pointing at a new test folder and every other
    /// name of <see cref="Names"/> at a second new fallback folder, both under the official temporary folder, then
    /// restores every name to its earlier value (an unset name stays unset) and deletes both folders, also when the
    /// body throws. Callers belong to <see cref="ProcessEnvironmentCollection"/>.
    /// </summary>
    /// <param name="primaryName">The name the strategy under test reads first; one of <see cref="Names"/>.</param>
    /// <param name="body">The test body; it receives the test folder.</param>
    /// <returns>A task that completes when the body has run and the variables are restored.</returns>
    internal static Task RunWithTestFolderAsync(string primaryName, Func<string, Task> body)
    {
        ArgumentNullException.ThrowIfNull(body);
        return RunWithTestFoldersAsync(primaryName, (folder, _) => body(folder));
    }

    /// <summary>
    /// The same as <see cref="RunWithTestFolderAsync"/>, with the body receiving the fallback folder too.
    /// </summary>
    /// <param name="primaryName">The name the strategy under test reads first; one of <see cref="Names"/>.</param>
    /// <param name="body">The test body; it receives the test folder and the fallback folder.</param>
    /// <returns>A task that completes when the body has run and the variables are restored.</returns>
    internal static async Task RunWithTestFoldersAsync(string primaryName, Func<string, string, Task> body)
    {
        ArgumentNullException.ThrowIfNull(body);
        if (!Names.Contains(primaryName, StringComparer.Ordinal))
            throw new ArgumentException($"{primaryName} is not a CLI temp variable name.", nameof(primaryName));

        var previous = Snapshot();
        var stem = Path.Combine(Path.GetTempPath(), "qbt-cli-temp-" + Guid.NewGuid().ToString("N"));
        var folder = stem + "-primary";
        var fallbackFolder = stem + "-fallback";
        Directory.CreateDirectory(folder);
        Directory.CreateDirectory(fallbackFolder);
        try
        {
            foreach (var name in Names)
                Environment.SetEnvironmentVariable(name, name == primaryName ? folder : fallbackFolder);
            await body(folder, fallbackFolder).ConfigureAwait(true);
        }
        finally
        {
            foreach (var (name, value) in previous)
                Environment.SetEnvironmentVariable(name, value);
            foreach (var created in new[] { folder, fallbackFolder })
            {
                try
                {
                    Directory.Delete(created, recursive: true);
                }
                catch (IOException)
                {
                }
            }
        }
    }

    /// <summary>Reads the current value of every name of <see cref="Names"/> from the process environment.</summary>
    /// <returns>The values by name, null for an unset name.</returns>
    internal static Dictionary<string, string?> Snapshot() =>
        Names.ToDictionary(name => name, name => Environment.GetEnvironmentVariable(name), StringComparer.Ordinal);

    /// <summary>
    /// Asserts that a snapshot of the variables points the name a strategy reads first, <paramref name="primaryName"/>,
    /// at <paramref name="folder"/> and every other name of <see cref="Names"/> at a different folder,
    /// <paramref name="fallbackFolder"/>, so a test that asserts on <paramref name="folder"/> proves the primary name
    /// won over a set fallback.
    /// </summary>
    /// <param name="observed">The variable values seen, by name.</param>
    /// <param name="primaryName">The name the strategy under test reads first; one of <see cref="Names"/>.</param>
    /// <param name="folder">The folder the primary name must point at.</param>
    /// <param name="fallbackFolder">The folder every other name must point at; not <paramref name="folder"/>.</param>
    internal static void AssertChainAt(IReadOnlyDictionary<string, string?> observed, string primaryName, string folder, string fallbackFolder)
    {
        ArgumentNullException.ThrowIfNull(observed);
        Assert.Contains(primaryName, Names);
        Assert.NotEqual(folder, fallbackFolder);
        foreach (var name in Names)
        {
            Assert.True(observed.TryGetValue(name, out var value), $"{name} was not observed.");
            Assert.Equal(name == primaryName ? folder : fallbackFolder, value);
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
