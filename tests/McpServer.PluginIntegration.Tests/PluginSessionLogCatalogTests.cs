using Xunit;

namespace McpServer.PluginIntegration.Tests;

/// <summary>
/// PLAN-PLUGINHANDOFF-001 C-red-P4: catalog uniqueness, roots, host kinds, entrypoints, versions, and eight enabled rows.
/// </summary>
[Collection("PluginSessionLog")]
[Trait("PluginInt", "Deterministic")]
public sealed class PluginSessionLogCatalogTests
{
    /// <summary>P4 red: each enabled row has a unique agent-source plus cache identity.</summary>
    [Fact]
    public void Catalog_UniqueAgentSourceCache()
    {
        var rows = PluginSessionLogCatalog.LoadAndValidate(FindRepositoryRoot());
        Assert.All(rows, row =>
        {
            Assert.False(string.IsNullOrWhiteSpace(row.AgentSourceType));
            Assert.False(string.IsNullOrWhiteSpace(row.CacheFolder));
        });
        var keys = rows.Select(row => row.AgentSourceType + ":" + row.CacheFolder).ToList();
        Assert.Equal(keys.Distinct(StringComparer.OrdinalIgnoreCase).Count(), keys.Count);
    }

    /// <summary>P4 red: each catalog repository root exists under the sibling GitHub directory.</summary>
    [Fact]
    public void Catalog_RepositoryRootsExist()
    {
        var repoRoot = FindRepositoryRoot();
        var parent = Directory.GetParent(repoRoot)?.FullName;
        Assert.False(string.IsNullOrWhiteSpace(parent));
        foreach (var row in PluginSessionLogCatalog.LoadAndValidate(repoRoot))
        {
            Assert.True(Directory.Exists(Path.Combine(parent!, row.RepositoryName)), row.RepositoryName + " is missing.");
        }
    }

    /// <summary>P4 red: every enabled row uses a defined PluginHostKind.</summary>
    [Fact]
    public void Catalog_SupportedHostKinds()
    {
        var rows = PluginSessionLogCatalog.LoadAndValidate(FindRepositoryRoot());
        Assert.All(rows, row => Assert.True(Enum.IsDefined(row.HostKind)));
    }

    /// <summary>P4 red: required entrypoint files exist for each enabled host.</summary>
    [Fact]
    public void Catalog_RequiredEntrypointFilesExist()
    {
        var repoRoot = FindRepositoryRoot();
        var parent = Directory.GetParent(repoRoot)!.FullName;
        foreach (var row in PluginSessionLogCatalog.LoadAndValidate(repoRoot))
        {
            Assert.False(string.IsNullOrWhiteSpace(row.Entrypoint));
            Assert.False(row.Entrypoint.Contains("..", StringComparison.Ordinal));
            var pluginRoot = Path.Combine(parent, row.RepositoryName);
            var entrypointPath = Path.Combine(pluginRoot, row.Entrypoint.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(entrypointPath), row.Name + " entrypoint missing: " + row.Entrypoint);
            Assert.NotNull(row.RequiredEnvironmentVariables);
            Assert.NotEmpty(row.RequiredEnvironmentVariables);
            Assert.All(row.RequiredEnvironmentVariables, name => Assert.False(string.IsNullOrWhiteSpace(name)));
        }
    }

    /// <summary>P4 red: version metadata is present for each enabled plugin.</summary>
    [Fact]
    public void Catalog_VersionMetadataPresent()
    {
        var repoRoot = FindRepositoryRoot();
        var parent = Directory.GetParent(repoRoot)!.FullName;
        foreach (var row in PluginSessionLogCatalog.LoadAndValidate(repoRoot))
        {
            var pluginRoot = Path.Combine(parent, row.RepositoryName);
            Assert.True(
                File.Exists(Path.Combine(pluginRoot, ".version")) ||
                File.Exists(Path.Combine(pluginRoot, "package.json")),
                row.Name + " is missing version metadata.");
        }
    }

    /// <summary>P4 red: exactly eight enabled catalog rows.</summary>
    [Fact]
    public void Catalog_ExactlyEightEnabled()
    {
        var rows = PluginSessionLogCatalog.LoadAndValidate(FindRepositoryRoot());
        Assert.Equal(8, rows.Count);
        Assert.All(rows, row => Assert.True(row.Enabled));
        Assert.Contains(rows, row => string.Equals(row.Name, "Cline v2", StringComparison.Ordinal));
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
