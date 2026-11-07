using Xunit;
using static QBrainAi.Support.Mcp.Tests.TestSupport.CliTempVariables;
using static QBrainAi.Support.Mcp.Tests.TestSupport.CliTempVariablesTestsSupport;

namespace QBrainAi.Support.Mcp.Tests.TestSupport;

/// <summary>
/// Tests the shared CLI temporary-folder variable support of <see cref="CliTempVariables"/>: its name list matches the
/// four strategies' fallback chains, and its snapshot assertions accept a run that pointed the primary name at the test
/// folder, every other name at a separate fallback folder, and restored every name, and reject one that missed or
/// mis-set a name.
/// </summary>
/// <remarks>
/// Requirement coverage: FR-MCP-TESTRESOURCE-001 ac-10; TR-MCP-USECASE-018 AC-006 as amended 2026-10-26 ("set and
/// restore every variable their assertions or the code they call depend on, every name of a fallback chain the code
/// reads"; operator: "Strengthen AC, move 14 (Recommended)" and "Dependence + proof AC (Recommended)"; hostile
/// validation finding R6-F1). Test data: scripted snapshots (dictionaries of name to value); these mock gate controls
/// read and write no process environment, so they are unit tests. The class runs in
/// <see cref="ProcessEnvironmentCollection"/> because its binding tests write the process environment; those are
/// integration tests.
/// <para>2026-11-07, FR-MCP-TESTRESOURCE-001 ac-10; TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the unit half of this class: its 3 Category=Integration cases (RunWithTestFoldersAsync_PointsPrimaryAndFallbackThenRestoresThem, RunWithTestFolderAsync_BodyThrows_StillRestoresAndDeletes and RunWithTestFolderAsync_UnknownPrimary_IsRefusedWithoutChanges) and the helpers only they use (SetAll) moved unchanged to the class of the same name in the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S7, hostile validation finding R14-F4), so this unit-owned project holds only unit cases; the helpers both halves use (Folder and Prior) moved once into TestSupport/CliTempVariablesTestsSupport.cs beside this file, which the integration project links (DRY); the unit cases and the class attributes are unchanged.</para>
/// </remarks>
[Collection(ProcessEnvironmentCollection.Name)]
public sealed class CliTempVariablesTests
{
    private const string Fallback = "/scripted/cli-temp-fallback";

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

    /// <summary>
    /// Mock gate positive control: a scripted snapshot with the primary name at the folder and every other name at the
    /// fallback folder passes, for each name as the primary.
    /// </summary>
    /// <param name="primaryName">The name read first.</param>
    [Theory]
    [InlineData("MCPSERVER_QUADBRAIN_CLI_TEMP")]
    [InlineData("MCPSERVER_ONESHOT_TEMP")]
    [InlineData("MCPSERVER_CODEX_ONESHOT_TEMP")]
    public void ScriptedChain_PrimaryAtFolderOthersAtFallback_Passes(string primaryName) =>
        AssertChainAt(Scripted(name => name == primaryName ? Folder : Fallback), primaryName, Folder, Fallback);

    /// <summary>
    /// Mock gate negative controls: the primary name at the fallback folder, another name at the primary folder, a
    /// missing name, an unset name, a fallback folder equal to the primary folder, and a primary name outside the
    /// chain are each rejected, inside a passing harness.
    /// </summary>
    [Fact]
    public void ScriptedChain_WrongSnapshot_IsRejectedInsidePassingHarness()
    {
        const string primary = "MCPSERVER_QUADBRAIN_CLI_TEMP";
        var good = Scripted(name => name == primary ? Folder : Fallback);
        var primaryAtFallback = Scripted(_ => Fallback);
        var otherAtFolder = Scripted(name => name == primary || name == "MCPSERVER_ONESHOT_TEMP" ? Folder : Fallback);
        var missing = Scripted(name => name == primary ? Folder : Fallback);
        missing.Remove("MCPSERVER_GROK_ONESHOT_TEMP");
        var unset = Scripted(name => name == primary ? Folder : name == "MCPSERVER_CODEX_TRIAGE_TEMP" ? null : Fallback);
        var sameFolders = Scripted(_ => Folder);

        Assert.NotNull(Record.Exception(() => AssertChainAt(primaryAtFallback, primary, Folder, Fallback)));
        Assert.NotNull(Record.Exception(() => AssertChainAt(otherAtFolder, primary, Folder, Fallback)));
        Assert.NotNull(Record.Exception(() => AssertChainAt(missing, primary, Folder, Fallback)));
        Assert.NotNull(Record.Exception(() => AssertChainAt(unset, primary, Folder, Fallback)));
        Assert.NotNull(Record.Exception(() => AssertChainAt(sameFolders, primary, Folder, Folder)));
        Assert.NotNull(Record.Exception(() => AssertChainAt(good, "MCPSERVER_NOT_A_CHAIN_NAME", Folder, Fallback)));
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

    private static Dictionary<string, string?> Scripted(Func<string, string?> value) =>
        Names.ToDictionary(name => name, value, StringComparer.Ordinal);
}
