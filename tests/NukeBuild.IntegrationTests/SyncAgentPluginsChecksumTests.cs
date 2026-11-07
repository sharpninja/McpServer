using NukeBuild.Tests;

namespace NukeBuild.IntegrationTests;

/// <summary>
/// FR-MCP-PLUGINCORE-004 AC3: the committed sync path must produce official plugin copies
/// that match canonical plugin-core lib-ps files without relying on sibling working trees.
/// </summary>
/// <remarks>
/// Requirement coverage named by requirement evidence: TR-MCP-NUKE-006 AC-002.
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-06</term><description>TR-MCP-NUKE-006 AC-002: cites the criterion whose evidence names this class, so the evidence no longer names the test (operator: "Must be fixed.").</description></item>
/// <item><term>2026-11-07</term><description>FR-MCP-PLUGINCORE-004 AC3; TR-MCP-NUKE-006 AC-002; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: moved whole and unchanged from Build.Tests into the integration-owned Build.IntegrationTests (root namespace NukeBuild.IntegrationTests), so the infrastructure-owned Build.Tests holds no Category=Integration case (slice S4, hostile validation finding R14-F4; operator 2026-11-06: "Move them (Recommended)"); OfficialPluginCoreSyncTestSupport is linked from Build.Tests, not copied; only the namespace changed.</description></item>
/// <item><term>2026-11-07</term><description>FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: Build.IntegrationTests, the project the entry above names, is now NukeBuild.IntegrationTests, renamed to its root namespace (operator 2026-11-07: "Project names and root namespaces for the project should match, so fix the project name."); the class and its cases are unchanged.</description></item>
/// </list>
/// </remarks>
public sealed class SyncAgentPluginsChecksumTests
{
    /// <summary>
    /// The official-plugin inventory uses the committed sync path, whose generated lib copies
    /// share SHA256 with plugins/core/lib-ps.
    /// </summary>
    /// <remarks>TR-MCP-NUKE-019: integration because this case resolves or reads the physical checkout.</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task SyncAgentPlugins_OfficialPluginLibChecksumsMatchCanonicalCore()
    {
        var repoRoot = FindRepositoryRoot();
        await OfficialPluginCoreSyncTestSupport.AssertCanonicalCorePropagatesAsync(
            repoRoot,
            TestContext.Current.CancellationToken).ConfigureAwait(true);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "QBrainAi.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("QBrainAi.sln not found.");
    }
}
