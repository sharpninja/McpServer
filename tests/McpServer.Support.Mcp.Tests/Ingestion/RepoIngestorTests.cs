using McpServer.Support.Mcp.Indexing;
using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Ingestion;

/// <summary>TR-PLANNED-013: Unit tests for RepoIngestor file discovery and chunking.</summary>
public sealed class RepoIngestorTests : IDisposable
{
    private readonly string _tempDir;

    public RepoIngestorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"repo_ingest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public async Task IngestAsync_ValidFiles_ReturnsDocumentsAndChunks()
    {
        File.WriteAllText(Path.Combine(_tempDir, "readme.md"), "# Hello World\nThis is a test.");
        File.WriteAllText(Path.Combine(_tempDir, "code.cs"), "class Foo { }");

        var options = Microsoft.Extensions.Options.Options.Create(new IngestionOptions { RepoRoot = _tempDir });
        var sut = new RepoIngestor(new Chunker(), options, new WorkspaceContext(), NullLogger<RepoIngestor>.Instance);

        var results = await sut.IngestAsync().ConfigureAwait(true);

        Assert.True(results.Count >= 2);
        Assert.All(results, r =>
        {
            Assert.NotEmpty(r.Doc.Id);
            Assert.Equal("repo", r.Doc.SourceType);
            Assert.True(r.Chunks.Count > 0);
        });
    }

    [Fact]
    public async Task IngestAsync_SkipsBinObjDotfiles()
    {
        Directory.CreateDirectory(Path.Combine(_tempDir, "bin"));
        File.WriteAllText(Path.Combine(_tempDir, "bin", "app.dll"), "binary");
        Directory.CreateDirectory(Path.Combine(_tempDir, "obj"));
        File.WriteAllText(Path.Combine(_tempDir, "obj", "project.assets.json"), "{}");
        File.WriteAllText(Path.Combine(_tempDir, ".gitignore"), "*.dll");
        File.WriteAllText(Path.Combine(_tempDir, "keep.md"), "# Keep");

        var options = Microsoft.Extensions.Options.Options.Create(new IngestionOptions { RepoRoot = _tempDir });
        var sut = new RepoIngestor(new Chunker(), options, new WorkspaceContext(), NullLogger<RepoIngestor>.Instance);

        var results = await sut.IngestAsync().ConfigureAwait(true);

        Assert.DoesNotContain(results, r => r.Doc.SourceKey.Contains("bin/"));
        Assert.DoesNotContain(results, r => r.Doc.SourceKey.Contains("obj/"));
        Assert.DoesNotContain(results, r => r.Doc.SourceKey.Contains(".gitignore"));
    }

    [Fact]
    public async Task IngestAsync_EmptyDirectory_ReturnsEmpty()
    {
        var emptyDir = Path.Combine(_tempDir, "empty");
        Directory.CreateDirectory(emptyDir);

        var options = Microsoft.Extensions.Options.Options.Create(new IngestionOptions { RepoRoot = emptyDir });
        var sut = new RepoIngestor(new Chunker(), options, new WorkspaceContext(), NullLogger<RepoIngestor>.Instance);

        var results = await sut.IngestAsync().ConfigureAwait(true);

        Assert.Empty(results);
    }

    [Fact]
    public async Task IngestAsync_ContentHash_DeterministicForSameContent()
    {
        File.WriteAllText(Path.Combine(_tempDir, "file.txt"), "deterministic content");

        var options = Microsoft.Extensions.Options.Options.Create(new IngestionOptions { RepoRoot = _tempDir });
        var sut = new RepoIngestor(new Chunker(), options, new WorkspaceContext(), NullLogger<RepoIngestor>.Instance);

        var results1 = await sut.IngestAsync().ConfigureAwait(true);
        var results2 = await sut.IngestAsync().ConfigureAwait(true);

        Assert.Equal(
            results1.First(r => r.Doc.SourceKey.Contains("file.txt")).Doc.ContentHash,
            results2.First(r => r.Doc.SourceKey.Contains("file.txt")).Doc.ContentHash);
    }

    [Fact]
    public async Task IngestAsync_SkipsLargeFiles()
    {
        var largeContent = new string('x', 2 * 1024 * 1024); // 2MB
        File.WriteAllText(Path.Combine(_tempDir, "large.txt"), largeContent);
        File.WriteAllText(Path.Combine(_tempDir, "small.txt"), "small");

        var options = Microsoft.Extensions.Options.Options.Create(new IngestionOptions { RepoRoot = _tempDir, MaxFileSizeBytes = 1024 * 1024 });
        var sut = new RepoIngestor(new Chunker(), options, new WorkspaceContext(), NullLogger<RepoIngestor>.Instance);

        var results = await sut.IngestAsync().ConfigureAwait(true);

        Assert.DoesNotContain(results, r => r.Doc.SourceKey.Contains("large.txt"));
        Assert.Contains(results, r => r.Doc.SourceKey.Contains("small.txt"));
    }

    [Fact]
    public async Task IngestAsync_DoesNotSkipPathsThatContainBinOrObjAsSubstring()
    {
        var folderWithSubstring = Path.Combine(_tempDir, "binary-assets");
        Directory.CreateDirectory(folderWithSubstring);
        File.WriteAllText(Path.Combine(folderWithSubstring, "keep.md"), "# keep");

        var options = Microsoft.Extensions.Options.Options.Create(new IngestionOptions { RepoRoot = _tempDir });
        var sut = new RepoIngestor(new Chunker(), options, new WorkspaceContext(), NullLogger<RepoIngestor>.Instance);

        var results = await sut.IngestAsync().ConfigureAwait(true);

        Assert.Contains(results, r => r.Doc.SourceKey.Equals("binary-assets/keep.md", StringComparison.Ordinal));
    }
}
