using Xunit;
using static QBrainAi.Support.Mcp.Tests.TestSupport.CliTempVariables;
using static QBrainAi.Support.Mcp.Tests.TestSupport.CliTempVariablesTestsSupport;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.TestSupport;

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
/// <para>2026-11-07, FR-MCP-TESTRESOURCE-001 ac-10; TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (TestSupport/CliTempVariablesTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 3 Category=Integration cases and the helpers only they use (SetAll) moved unchanged with the class attributes; the helpers both halves use come from TestSupport/CliTempVariablesTestsSupport.cs of QBrainAi.Support.Mcp.Tests, linked into this project (DRY); only the namespace changed.</para>
/// </remarks>
[Collection(ProcessEnvironmentCollection.Name)]
public sealed class CliTempVariablesTests
{
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
}
