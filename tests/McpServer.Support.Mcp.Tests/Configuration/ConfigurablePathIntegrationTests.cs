using McpServer.Support.Mcp.Indexing;
using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Configuration;

public sealed class ConfigurablePathIntegrationTests : IDisposable
{
    private readonly List<string> _tempDirectories = [];

    [Fact]
    public async Task RepoIngestor_ResolvesRelativeRepoRoot_AgainstCurrentDirectory()
    {
        var repoFolder = CreateTempDirectory();
        var relativeRepoRoot = Path.GetRelativePath(Directory.GetCurrentDirectory(), repoFolder);
        Directory.CreateDirectory(repoFolder);
        await File.WriteAllTextAsync(Path.Combine(repoFolder, "readme.md"), "# relative root").ConfigureAwait(true);

        var sut = new RepoIngestor(new Chunker(), Microsoft.Extensions.Options.Options.Create(new IngestionOptions { RepoRoot = relativeRepoRoot }), new WorkspaceContext(), NullLogger<RepoIngestor>.Instance);

        var results = await sut.IngestAsync().ConfigureAwait(true);

        Assert.Contains(results, r => string.Equals(r.Doc.SourceKey, "readme.md", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task VectorIndexService_SaveAsync_UsesConfiguredRelativeIndexPath()
    {
        var tempRoot = CreateTempDirectory();
        var relativeRoot = Path.GetRelativePath(Directory.GetCurrentDirectory(), tempRoot);
        var relativePath = Path.Combine(relativeRoot, "indexes", "vector.idx");
        var expectedPath = Path.GetFullPath(relativePath);

        using var sut = new VectorIndexService(
            new VectorIndexOptions { IndexPath = relativePath, MaxElements = 100 },
            NullLogger<VectorIndexService>.Instance);

        var embedding = new float[384];
        embedding[0] = 1f;
        sut.AddVector("chunk-1", embedding);
        await sut.SaveAsync(string.Empty).ConfigureAwait(true);

        Assert.True(File.Exists(expectedPath));
        Assert.True(File.Exists(expectedPath + ".map"));
        Assert.True(File.Exists(expectedPath + ".vectors"));
    }

    [Fact]
    public void McpInstanceResolver_ResolvesIsolatedSettings_ForTwoInstances()
    {
        var tempRoot = CreateTempDirectory();
        var alphaRoot = Path.Combine(tempRoot, "alpha");
        var betaRoot = Path.Combine(tempRoot, "beta");
        var alphaData = Path.Combine(tempRoot, "alpha-data");
        var betaData = Path.Combine(tempRoot, "beta-data");
        Directory.CreateDirectory(alphaRoot);
        Directory.CreateDirectory(betaRoot);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mcp:Instances:alpha:Port"] = "7147",
                ["Mcp:Instances:alpha:RepoRoot"] = alphaRoot,
                ["Mcp:Instances:alpha:DataSource"] = "alpha.db",
                ["Mcp:Instances:alpha:DataDirectory"] = alphaData,
                ["Mcp:Instances:beta:Port"] = "7157",
                ["Mcp:Instances:beta:RepoRoot"] = betaRoot,
                ["Mcp:Instances:beta:DataSource"] = "beta.db",
                ["Mcp:Instances:beta:DataDirectory"] = betaData,
            })
            .Build();

        McpInstanceResolver.ValidateInstances(config);

        var alphaPort = McpInstanceResolver.GetEffectiveMcpInt(config, "alpha", "Port", 0);
        var betaPort = McpInstanceResolver.GetEffectiveMcpInt(config, "beta", "Port", 0);
        var alphaDb = McpInstanceResolver.ResolveSqliteDataSource(config, "alpha");
        var betaDb = McpInstanceResolver.ResolveSqliteDataSource(config, "beta");

        Assert.NotEqual(alphaPort, betaPort);
        Assert.NotEqual(alphaDb, betaDb);
        Assert.EndsWith(Path.Combine("alpha-data", "alpha.db"), alphaDb, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith(Path.Combine("beta-data", "beta.db"), betaDb, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        foreach (var dir in _tempDirectories)
        {
            try
            {
                if (Directory.Exists(dir))
                    Directory.Delete(dir, true);
            }
            catch
            {
                // Best-effort cleanup.
            }
        }
    }

    private string CreateTempDirectory()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"fwh_mcp_cfg_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        _tempDirectories.Add(dir);
        return dir;
    }
}
