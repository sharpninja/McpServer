using Xunit;
using static QBrainAi.Support.Mcp.Tests.TestSupport.CliTempVariables;

namespace QBrainAi.Support.Mcp.Tests.TestSupport;

/// <summary>
/// Tests the shared CLI temporary-folder variable support of <see cref="CliTempVariables"/>: its name list matches the
/// four strategies' fallback chains, and its snapshot assertions accept a run that pointed every name at the test
/// folder and restored every name, and reject one that missed or mis-set a name.
/// </summary>
/// <remarks>
/// Requirement coverage: TR-MCP-USECASE-018 AC-006 and TR-MCP-USECASE-019 AC-008 as amended 2026-10-26 ("sets and
/// restores every variable its assertions or the code it calls depend on, every name of a fallback chain the code
/// reads"; operator: "Strengthen AC, move 14 (Recommended)"; hostile validation finding R6-F1). Test data: scripted
/// snapshots (dictionaries of name to value); these mock gate controls read and write no process environment, so
/// they are unit tests. The class runs in <see cref="ProcessEnvironmentCollection"/> because its binding tests write
/// the process environment; those are integration tests.
/// </remarks>
[Collection(ProcessEnvironmentCollection.Name)]
public sealed class CliTempVariablesTests
{
    private const string Folder = "/scripted/cli-temp-folder";

    /// <summary>
    /// The name list holds the five chain names once each: CliBrainSlotChatClient (QUADBRAIN_CLI, ONESHOT), Codex
    /// (CODEX_ONESHOT, CODEX_TRIAGE), one-shot (ONESHOT, CODEX_ONESHOT, CODEX_TRIAGE) and Grok (GROK_ONESHOT, ONESHOT).
    /// </summary>
    [Fact]
    public void Names_HoldEveryChainNameOnce()
    {
        string[] expected =
        [
            "MCPSERVER_CODEX_ONESHOT_TEMP",
            "MCPSERVER_CODEX_TRIAGE_TEMP",
            "MCPSERVER_GROK_ONESHOT_TEMP",
            "MCPSERVER_ONESHOT_TEMP",
            "MCPSERVER_QUADBRAIN_CLI_TEMP",
        ];

        Assert.Equal(expected, Names.Order(StringComparer.Ordinal));
    }

    /// <summary>Mock gate positive control: a scripted snapshot with every name at the folder passes.</summary>
    [Fact]
    public void ScriptedSnapshot_EveryNameAtTheFolder_Passes() =>
        AssertAllAt(Scripted(_ => Folder), Folder);

    /// <summary>
    /// Mock gate negative controls: a snapshot missing one name, one with a name at another folder, and one with a
    /// name unset are each rejected, inside a passing harness.
    /// </summary>
    [Fact]
    public void ScriptedSnapshot_MissingOrWrongName_IsRejectedInsidePassingHarness()
    {
        var missing = Scripted(_ => Folder);
        missing.Remove("MCPSERVER_CODEX_TRIAGE_TEMP");
        var elsewhere = Scripted(name => name == "MCPSERVER_GROK_ONESHOT_TEMP" ? "/scripted/other-folder" : Folder);
        var unset = Scripted(name => name == "MCPSERVER_ONESHOT_TEMP" ? null : Folder);

        Assert.NotNull(Record.Exception(() => AssertAllAt(missing, Folder)));
        Assert.NotNull(Record.Exception(() => AssertAllAt(elsewhere, Folder)));
        Assert.NotNull(Record.Exception(() => AssertAllAt(unset, Folder)));
    }

    /// <summary>Mock gate positive control: equal before and after snapshots, an unset name included, pass.</summary>
    [Fact]
    public void ScriptedRestore_EqualSnapshots_Pass() =>
        AssertRestored(Scripted(Prior), Scripted(Prior));

    /// <summary>
    /// Mock gate negative controls: an after snapshot that left one name at the folder, one that set a previously
    /// unset name, and one missing a name are each rejected, inside a passing harness.
    /// </summary>
    [Fact]
    public void ScriptedRestore_UnrestoredName_IsRejectedInsidePassingHarness()
    {
        var before = Scripted(Prior);
        var leftSet = Scripted(name => name == "MCPSERVER_QUADBRAIN_CLI_TEMP" ? Folder : Prior(name));
        var newlySet = Scripted(name => name == "MCPSERVER_GROK_ONESHOT_TEMP" ? Folder : Prior(name));
        var missing = Scripted(Prior);
        missing.Remove("MCPSERVER_ONESHOT_TEMP");

        Assert.NotNull(Record.Exception(() => AssertRestored(before, leftSet)));
        Assert.NotNull(Record.Exception(() => AssertRestored(before, newlySet)));
        Assert.NotNull(Record.Exception(() => AssertRestored(before, missing)));
    }

    /// <summary>
    /// Binding: the helper points every name at one new folder under the official temporary folder while the body
    /// runs, then restores every scripted earlier value (an unset name included) and deletes the folder. The test
    /// writes the process environment and restores the ambient values, so it is an integration test.
    /// </summary>
    /// <returns>A task that completes when the assertions have run.</returns>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task RunWithTestFolderAsync_PointsEveryNameAtItsFolderThenRestoresThem()
    {
        var ambient = Snapshot();
        try
        {
            SetAll(Prior);
            var before = Snapshot();
            string? seenFolder = null;
            Dictionary<string, string?>? inside = null;
            var existed = false;

            await RunWithTestFolderAsync(folder =>
            {
                seenFolder = folder;
                inside = Snapshot();
                existed = Directory.Exists(folder);
                return Task.CompletedTask;
            }).ConfigureAwait(true);

            Assert.NotNull(seenFolder);
            Assert.NotNull(inside);
            AssertAllAt(inside, seenFolder);
            Assert.True(existed);
            Assert.StartsWith(Path.GetFullPath(Path.GetTempPath()), Path.GetFullPath(seenFolder), StringComparison.OrdinalIgnoreCase);
            Assert.False(Directory.Exists(seenFolder));
            AssertRestored(before, Snapshot());
        }
        finally
        {
            SetAll(name => ambient[name]);
        }
    }

    /// <summary>
    /// Binding: when the body throws, the exception reaches the caller and the helper still restores every scripted
    /// earlier value and deletes the folder. The test writes the process environment and restores the ambient
    /// values, so it is an integration test.
    /// </summary>
    /// <returns>A task that completes when the assertions have run.</returns>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task RunWithTestFolderAsync_BodyThrows_StillRestoresAndDeletes()
    {
        var ambient = Snapshot();
        try
        {
            SetAll(Prior);
            var before = Snapshot();
            string? seenFolder = null;

            await Assert.ThrowsAsync<InvalidOperationException>(() => RunWithTestFolderAsync(folder =>
            {
                seenFolder = folder;
                throw new InvalidOperationException("scripted body failure");
            })).ConfigureAwait(true);

            Assert.NotNull(seenFolder);
            Assert.False(Directory.Exists(seenFolder));
            AssertRestored(before, Snapshot());
        }
        finally
        {
            SetAll(name => ambient[name]);
        }
    }

    private static void SetAll(Func<string, string?> value)
    {
        foreach (var name in Names)
            Environment.SetEnvironmentVariable(name, value(name));
    }

    private static string? Prior(string name) =>
        name == "MCPSERVER_GROK_ONESHOT_TEMP" ? null : "/scripted/prior/" + name;

    private static Dictionary<string, string?> Scripted(Func<string, string?> value) =>
        Names.ToDictionary(name => name, value, StringComparer.Ordinal);
}
