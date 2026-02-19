using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Indexing;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Ingestion;

/// <summary>TR-PLANNED-013: Tests verifying MD normalization produces structured text suitable for chunking.</summary>
public sealed class SessionLogIngestorChunkingTests : IDisposable
{
    private readonly McpDbContext _db;
    private readonly SessionLogService _service;
    private readonly string _tempDir;

    public SessionLogIngestorChunkingTests()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"ChunkingTests_{Guid.NewGuid()}")
            .Options;
        _db = new McpDbContext(options);
        _db.Database.EnsureCreated();
        _service = new SessionLogService(_db, NullLogger<SessionLogService>.Instance);
        _tempDir = Path.Combine(Path.GetTempPath(), $"fwh-chunking-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(_tempDir, "docs", "sessions"));
    }

    public void Dispose()
    {
        _db.Dispose();
        try { Directory.Delete(_tempDir, true); } catch { /* best-effort cleanup */ }
    }

    [Fact]
    public async Task WhenIngestingMarkdownThenChunksContainStructuredText()
    {
        var md = """
            # Copilot Session Log - Chunking Test

            **Date:** 2026-02-16
            **Duration:** ~2 hours
            **Model:** gpt-4o
            **Status:** ✅ Complete

            ## 1. Session Overview
            Implemented chunking improvements for MD session logs.

            ## 2. Changes Made
            - Updated NormalizeMarkdownSessionLog
            - Added structured extraction

            ## 4. Testing
            All 10 tests passing.
            """;
        File.WriteAllText(Path.Combine(_tempDir, "docs", "sessions", "copilot-SESSION-LOG-2026-02-16.md"), md);

        var ingestor = CreateIngestor();
        var results = await ingestor.IngestAsync().ConfigureAwait(true);

        Assert.NotEmpty(results);
        var (doc, chunks) = results[0];
        Assert.Equal("session-log", doc.SourceType);
        Assert.NotEmpty(chunks);

        // The normalized text should contain structured output, not raw markdown
        var fullText = string.Join("\n", chunks.Select(c => c.Content));
        Assert.Contains("Session:", fullText, StringComparison.Ordinal);
        Assert.Contains("Date:", fullText, StringComparison.Ordinal);
        Assert.Contains("Model:", fullText, StringComparison.Ordinal);
        Assert.Contains("Duration:", fullText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WhenIngestingMarkdownWithSectionsThenChunksContainSectionHeaders()
    {
        var md = """
            # Session Log - Sections Chunking Test

            **Date:** 2026-02-16
            **Status:** Complete

            ## 1. Session Overview
            Overview of the session.

            ## 3. Technical Requirements
            - TR-001: Added new requirement

            ## 7. Files Summary
            - Source: 5 files
            - Tests: 3 files
            """;
        File.WriteAllText(Path.Combine(_tempDir, "docs", "sessions", "cursor-SESSION-LOG-2026-02-16.md"), md);

        var ingestor = CreateIngestor();
        var results = await ingestor.IngestAsync().ConfigureAwait(true);

        Assert.NotEmpty(results);
        var fullText = string.Join("\n", results[0].Chunks.Select(c => c.Content));
        Assert.Contains("Section: Session Overview", fullText, StringComparison.Ordinal);
        Assert.Contains("Section: Technical Requirements", fullText, StringComparison.Ordinal);
        Assert.Contains("Section: Files Summary", fullText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WhenIngestingMarkdownThenNormalizationDiffersFromRawContent()
    {
        var md = """
            # Session Log - Normalization Diff Test

            **Date:** 2026-02-16
            **Status:** Complete

            ## Session Overview
            Simple overview.
            """;
        File.WriteAllText(Path.Combine(_tempDir, "docs", "sessions", "copilot-SESSION-LOG-2026-02-16-diff.md"), md);

        var ingestor = CreateIngestor();
        var results = await ingestor.IngestAsync().ConfigureAwait(true);

        Assert.NotEmpty(results);
        var fullText = string.Join("\n", results[0].Chunks.Select(c => c.Content));

        // Should NOT contain raw markdown headers (## prefixes)
        Assert.DoesNotContain("## Session Overview", fullText, StringComparison.Ordinal);
        // Should contain structured "Section:" prefix instead
        Assert.Contains("Section: Session Overview", fullText, StringComparison.Ordinal);
    }

    private SessionLogIngestor CreateIngestor()
    {
        var opts = Microsoft.Extensions.Options.Options.Create(new IngestionOptions
        {
            RepoRoot = _tempDir,
            SessionsPath = "docs/sessions"
        });
        return new SessionLogIngestor(new Chunker(), opts, _service, NullLogger<SessionLogIngestor>.Instance);
    }
}
