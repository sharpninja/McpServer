using QBrainAi.Support.Mcp.Indexing;
using QBrainAi.Support.Mcp.Ingestion;
using QBrainAi.Support.Mcp.Options;
using QBrainAi.Support.Mcp.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Configuration;

/// <summary>
/// Configurable paths, integration half: a relative repository root and a relative vector index path resolve against the process current directory on the real filesystem (TR-MCP-USECASE-018 AC-006).
/// </summary>
/// <remarks>2026-11-07, TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Configuration/ConfigurablePathIntegrationTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 2 Category=Integration cases moved unchanged with the class attributes; the state and helpers both halves use come from the abstract base class ConfigurablePathIntegrationTestsBase in Configuration/ConfigurablePathIntegrationTestsBase.cs of QBrainAi.Support.Mcp.Tests, linked into this project (DRY); only the namespace changed.</remarks>
public sealed class ConfigurablePathIntegrationTests : ConfigurablePathIntegrationTestsBase, IDisposable
{
    /// <remarks>Integration: exercises native filesystem reads or writes for this case instead of only scripted in-memory data (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task RepoIngestor_ResolvesRelativeRepoRoot_AgainstCurrentDirectory()
    {
        var repoFolder = CreateTempDirectory();
        var relativeRepoRoot = Path.GetRelativePath(Directory.GetCurrentDirectory(), repoFolder);
        Directory.CreateDirectory(repoFolder);
        await File.WriteAllTextAsync(Path.Combine(repoFolder, "readme.md"), "# relative root", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var sut = new RepoIngestor(new Chunker(), Microsoft.Extensions.Options.Options.Create(new IngestionOptions { RepoRoot = relativeRepoRoot }), new WorkspaceContext(), NullLogger<RepoIngestor>.Instance);

        var results = await sut.IngestAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Contains(results, r => string.Equals(r.Doc.SourceKey, "readme.md", StringComparison.OrdinalIgnoreCase));
    }

    /// <remarks>Integration: exercises native filesystem reads or writes for this case instead of only scripted in-memory data (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
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
        await sut.SaveAsync(string.Empty, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(File.Exists(expectedPath));
        Assert.True(File.Exists(expectedPath + ".map"));
        Assert.True(File.Exists(expectedPath + ".vectors"));
    }
}
