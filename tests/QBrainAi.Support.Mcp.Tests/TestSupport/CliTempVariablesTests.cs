using Xunit;
using static QBrainAi.Support.Mcp.Tests.TestSupport.CliTempVariables;

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
/// </remarks>
[Collection(ProcessEnvironmentCollection.Name)]
public sealed class CliTempVariablesTests
{
    private const string Folder = "/scripted/cli-temp-folder";

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

    /// <summary>
    /// Binding: for each name as the primary, the helper points that name at a new test folder and every other name at
    /// a separate new fallback folder, both under the official temporary folder, while the body runs, then restores
    /// every scripted earlier value (an unset name included) and deletes both folders. The test writes the process
    /// environment and restores the ambient values, so it is an integration test.
    /// </summary>
    /// <param name="primaryName">The name read first.</param>
    /// <returns>A task that completes when the assertions have run.</returns>
    [Theory]
    [Trait("Category", "Integration")]
    [InlineData("MCPSERVER_QUADBRAIN_CLI_TEMP")]
    [InlineData("MCPSERVER_ONESHOT_TEMP")]
    [InlineData("MCPSERVER_CODEX_ONESHOT_TEMP")]
    [InlineData("MCPSERVER_CODEX_TRIAGE_TEMP")]
    [InlineData("MCPSERVER_GROK_ONESHOT_TEMP")]
    public async Task RunWithTestFoldersAsync_PointsPrimaryAndFallbackThenRestoresThem(string primaryName)
    {
        var ambient = Snapshot();
        try
        {
            SetAll(Prior);
            var before = Snapshot();
            string? seenFolder = null;
            string? seenFallback = null;
            Dictionary<string, string?>? inside = null;
            var existed = false;

            await RunWithTestFoldersAsync(primaryName, (folder, fallbackFolder) =>
            {
                seenFolder = folder;
                seenFallback = fallbackFolder;
                inside = Snapshot();
                existed = Directory.Exists(folder) && Directory.Exists(fallbackFolder);
                return Task.CompletedTask;
            }).ConfigureAwait(true);

            Assert.NotNull(seenFolder);
            Assert.NotNull(seenFallback);
            Assert.NotNull(inside);
            AssertChainAt(inside, primaryName, seenFolder, seenFallback);
            Assert.True(existed);
            var temp = Path.GetFullPath(Path.GetTempPath());
            Assert.StartsWith(temp, Path.GetFullPath(seenFolder), StringComparison.OrdinalIgnoreCase);
            Assert.StartsWith(temp, Path.GetFullPath(seenFallback), StringComparison.OrdinalIgnoreCase);
            Assert.False(Directory.Exists(seenFolder));
            Assert.False(Directory.Exists(seenFallback));
            AssertRestored(before, Snapshot());
        }
        finally
        {
            SetAll(name => ambient[name]);
        }
    }

    /// <summary>
    /// Binding: when the body throws, the exception reaches the caller and the helper still restores every scripted
    /// earlier value and deletes both folders. The test writes the process environment and restores the ambient
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
            string? seenFallback = null;

            await Assert.ThrowsAsync<InvalidOperationException>(() => RunWithTestFoldersAsync("MCPSERVER_CODEX_ONESHOT_TEMP", (folder, fallbackFolder) =>
            {
                seenFolder = folder;
                seenFallback = fallbackFolder;
                throw new InvalidOperationException("scripted body failure");
            })).ConfigureAwait(true);

            Assert.NotNull(seenFolder);
            Assert.NotNull(seenFallback);
            Assert.False(Directory.Exists(seenFolder));
            Assert.False(Directory.Exists(seenFallback));
            AssertRestored(before, Snapshot());
        }
        finally
        {
            SetAll(name => ambient[name]);
        }
    }

    /// <summary>
    /// Binding: a primary name outside the chain is refused before any variable is written or any folder is made.
    /// The test reads the process environment to prove nothing changed, so it is an integration test.
    /// </summary>
    /// <returns>A task that completes when the assertions have run.</returns>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task RunWithTestFolderAsync_UnknownPrimary_IsRefusedWithoutChanges()
    {
        var before = Snapshot();
        var ran = false;

        await Assert.ThrowsAsync<ArgumentException>(() => RunWithTestFolderAsync("MCPSERVER_NOT_A_CHAIN_NAME", _ =>
        {
            ran = true;
            return Task.CompletedTask;
        })).ConfigureAwait(true);

        Assert.False(ran);
        AssertRestored(before, Snapshot());
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
