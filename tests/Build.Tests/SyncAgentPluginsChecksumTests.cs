namespace NukeBuild.Tests;

/// <summary>
/// FR-MCP-PLUGINCORE-004 AC3: the committed sync path must produce official plugin copies
/// that match canonical plugin-core lib-ps files without relying on sibling working trees.
/// </summary>
public sealed class SyncAgentPluginsChecksumTests
{
    /// <summary>
    /// The official-plugin inventory uses the committed sync path, whose generated lib copies
    /// share SHA256 with plugins/core/lib-ps.
    /// </summary>
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
            if (File.Exists(Path.Combine(directory.FullName, "McpServer.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("McpServer.sln not found.");
    }
}
