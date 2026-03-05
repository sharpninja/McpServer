using McpServer.Support.Mcp.Indexing;
using McpServer.Support.Mcp.Ingestion;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Ingestion;

/// <summary>TR-PLANNED-013: Unit tests for ExternalDocsIngestor file discovery and limits.</summary>
public sealed class ExternalDocsIngestorTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _externalDir;

    public ExternalDocsIngestorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"ext_ingest_{Guid.NewGuid():N}");
        _externalDir = Path.Combine(_tempDir, "docs", "external");
        Directory.CreateDirectory(_externalDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public async Task IngestAsync_ValidDocs_ReturnsDocumentsAndChunks()
    {
        File.WriteAllText(Path.Combine(_externalDir, "api-guide.md"), "# API Guide\nSome content here.");
        File.WriteAllText(Path.Combine(_externalDir, "tutorial.txt"), "Step 1: do something");

        var options = Microsoft.Extensions.Options.Options.Create(new IngestionOptions { RepoRoot = _tempDir, ExternalDocsPath = "docs/external" });
        var sut = new ExternalDocsIngestor(new Chunker(), options, NullLogger<ExternalDocsIngestor>.Instance);

        var results = await sut.IngestAsync().ConfigureAwait(true);

        Assert.True(results.Count >= 2);
        Assert.All(results, r =>
        {
            Assert.Equal("external-doc", r.Doc.SourceType);
            Assert.True(r.Chunks.Count > 0);
        });
    }

    [Fact]
    public async Task IngestAsync_SkipsOversizedFiles()
    {
        File.WriteAllText(Path.Combine(_externalDir, "huge.txt"), new string('x', 2 * 1024 * 1024));
        File.WriteAllText(Path.Combine(_externalDir, "normal.txt"), "normal content");

        var options = Microsoft.Extensions.Options.Options.Create(new IngestionOptions
        {
            RepoRoot = _tempDir,
            ExternalDocsPath = "docs/external",
            MaxFileSizeBytes = 1024 * 1024
        });
        var sut = new ExternalDocsIngestor(new Chunker(), options, NullLogger<ExternalDocsIngestor>.Instance);

        var results = await sut.IngestAsync().ConfigureAwait(true);

        Assert.DoesNotContain(results, r => r.Doc.SourceKey.Contains("huge.txt"));
        Assert.Contains(results, r => r.Doc.SourceKey.Contains("normal.txt"));
    }

    [Fact]
    public async Task IngestAsync_EmptyDirectory_ReturnsEmpty()
    {
        var emptyDir = Path.Combine(_tempDir, "empty_ext");
        Directory.CreateDirectory(emptyDir);

        var options = Microsoft.Extensions.Options.Options.Create(new IngestionOptions { RepoRoot = _tempDir, ExternalDocsPath = "empty_ext" });
        var sut = new ExternalDocsIngestor(new Chunker(), options, NullLogger<ExternalDocsIngestor>.Instance);

        var results = await sut.IngestAsync().ConfigureAwait(true);

        Assert.Empty(results);
    }

    [Fact]
    public async Task IngestAsync_NonexistentDirectory_ReturnsEmpty()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new IngestionOptions { RepoRoot = _tempDir, ExternalDocsPath = "nonexistent" });
        var sut = new ExternalDocsIngestor(new Chunker(), options, NullLogger<ExternalDocsIngestor>.Instance);

        var results = await sut.IngestAsync().ConfigureAwait(true);

        Assert.Empty(results);
    }
}
