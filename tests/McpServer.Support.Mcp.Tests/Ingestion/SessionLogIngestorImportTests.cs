using System.Text.Json;
using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Indexing;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Ingestion;

/// <summary>TR-PLANNED-CORE-013: Tests for SessionLogIngestor.ImportToSessionLogTablesAsync (MVP-SUPPORT-011).</summary>
public sealed class SessionLogIngestorImportTests : IDisposable
{
    private readonly McpDbContext _db;
    private readonly SessionLogService _service;
    private readonly string _tempDir;

    public SessionLogIngestorImportTests()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"IngestorImportTests_{Guid.NewGuid()}")
            .Options;
        _db = new McpDbContext(options);
        _db.Database.EnsureCreated();
        _service = new SessionLogService(_db, NullLogger<SessionLogService>.Instance);
        _tempDir = Path.Combine(Path.GetTempPath(), $"fwh-ingestor-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(_tempDir, "docs", "sessions"));
        _db.OverrideWorkspaceId(_tempDir);
    }

    public void Dispose()
    {
        _db.Dispose();
        try { Directory.Delete(_tempDir, true); } catch { /* best-effort cleanup */ }
    }

    [Fact]
    public async Task WhenImportingJsonSessionLogThenSessionIsPersisted()
    {
        WriteSessionFile("copilot-test.json", new UnifiedSessionLogDto
        {
            SourceType = "Copilot",
            SessionId = "import-1",
            Title = "Imported Session",
            Model = "gpt-4",
            Started = "2026-02-12T10:00:00Z",
            TurnCount = 1,
            Turns =
            [
                new UnifiedRequestEntryDto
                {
                    RequestId = "req-1",
                    QueryText = "test query",
                    Response = "test response",
                    Status = "completed"
                }
            ]
        });

        var ingestor = CreateIngestor();
        var result = await ingestor.ImportToSessionLogTablesAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(1, result.Imported);
        var stored = await _db.SessionLogs.Include(s => s.Turns).FirstOrDefaultAsync(s => s.SessionId == "import-1", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.NotNull(stored);
        Assert.Equal("Copilot", stored!.SourceType);
        Assert.Equal("Imported Session", stored.Title);
        Assert.Single(stored.Turns);
        Assert.NotNull(stored.SourceFilePath);
        Assert.EndsWith("copilot-test.json", stored.SourceFilePath!);
        Assert.NotNull(stored.ContentHash);
        Assert.Equal(64, stored.ContentHash!.Length); // SHA-256 hex
    }

    [Fact]
    public async Task WhenImportingWithStringWorkspaceThenWorkspaceIsHandled()
    {
        var json = """
        {
            "sourceType": "Cursor",
            "sessionId": "ws-string",
            "title": "String Workspace",
            "workspace": "E:\\github\\FunWasHad",
            "turnCount": 0
        }
        """;
        File.WriteAllText(Path.Combine(_tempDir, "docs", "sessions", "cursor-ws.json"), json);

        var ingestor = CreateIngestor();
        var result = await ingestor.ImportToSessionLogTablesAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(1, result.Imported);
        var stored = await _db.SessionLogs.FirstAsync(s => s.SessionId == "ws-string", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(@"E:\github\FunWasHad", stored.Repository);
    }

    [Fact]
    public async Task WhenImportingWithObjectWorkspaceThenWorkspaceFieldsArePersisted()
    {
        var json = """
        {
            "sourceType": "Copilot",
            "sessionId": "ws-object",
            "title": "Object Workspace",
            "workspace": {
                "project": "FunWasHad",
                "targetFramework": ".NET 9",
                "repository": "sharpninja/FunWasHad",
                "branch": "develop"
            },
            "turnCount": 0
        }
        """;
        File.WriteAllText(Path.Combine(_tempDir, "docs", "sessions", "copilot-ws.json"), json);

        var ingestor = CreateIngestor();
        await ingestor.ImportToSessionLogTablesAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var stored = await _db.SessionLogs.FirstAsync(s => s.SessionId == "ws-object", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal("FunWasHad", stored.Project);
        Assert.Equal(".NET 9", stored.TargetFramework);
        Assert.Equal("sharpninja/FunWasHad", stored.Repository);
        Assert.Equal("develop", stored.Branch);
    }

    [Fact]
    public async Task WhenImportingMissingSourceTypeThenFileIsSkipped()
    {
        var json = """{ "sessionId": "no-source", "turnCount": 0 }""";
        File.WriteAllText(Path.Combine(_tempDir, "docs", "sessions", "bad.json"), json);

        var ingestor = CreateIngestor();
        var result = await ingestor.ImportToSessionLogTablesAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(0, result.Imported);
        Assert.Equal(1, result.Failed);
    }

    [Fact]
    public async Task WhenImportingMultipleFilesThenAllAreImported()
    {
        for (var i = 0; i < 3; i++)
        {
            WriteSessionFile($"multi-{i}.json", new UnifiedSessionLogDto
            {
                SourceType = "Cursor",
                SessionId = $"multi-{i}",
                Title = $"Multi {i}",
                TurnCount = 0
            });
        }

        var ingestor = CreateIngestor();
        var result = await ingestor.ImportToSessionLogTablesAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(3, result.Imported);
        Assert.Equal(3, result.FilesScanned);
        Assert.Equal(3, await _db.SessionLogs.CountAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true));
    }

    [Fact]
    public async Task WhenReimportingSameFileThenSessionIsUpserted()
    {
        WriteSessionFile("upsert.json", new UnifiedSessionLogDto
        {
            SourceType = "Copilot",
            SessionId = "upsert-1",
            Title = "Original",
            TurnCount = 0
        });

        var ingestor = CreateIngestor();
        await ingestor.ImportToSessionLogTablesAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Overwrite with updated title
        WriteSessionFile("upsert.json", new UnifiedSessionLogDto
        {
            SourceType = "Copilot",
            SessionId = "upsert-1",
            Title = "Updated",
            TurnCount = 0
        });

        var result = await ingestor.ImportToSessionLogTablesAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(1, result.Imported);
        Assert.Equal(1, await _db.SessionLogs.CountAsync(s => s.SessionId == "upsert-1", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true));
        var stored = await _db.SessionLogs.FirstAsync(s => s.SessionId == "upsert-1", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal("Updated", stored.Title);
    }

    [Fact]
    public async Task WhenFileIsUnchangedThenImportSkipsIt()
    {
        WriteSessionFile("unchanged.json", new UnifiedSessionLogDto
        {
            SourceType = "Cursor",
            SessionId = "unchanged-1",
            Title = "Stable",
            TurnCount = 0
        });

        var ingestor = CreateIngestor();

        // First import
        var first = await ingestor.ImportToSessionLogTablesAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(1, first.Imported);

        // Second import — file has not changed, should be skipped
        var second = await ingestor.ImportToSessionLogTablesAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(0, second.Imported);
        Assert.Equal(1, second.Skipped);
    }

    /// <summary>
    /// AC-FR-MCP-SESSIONLOGCTX-001-007 / TEST-MCP-SESSIONLOG-006:
    /// ingest of omitted planFile/todoId persists extractor result or None, never null.
    /// </summary>
    [Fact]
    public async Task Ingest_OmittedFields_PersistsExtractorResultOrNone()
    {
        WriteSessionFile("omit-fields.json", new UnifiedSessionLogDto
        {
            SourceType = "Copilot",
            SessionId = "Copilot-20260304T113901Z-omit",
            Title = "Omitted fields",
            TurnCount = 1,
            Turns =
            [
                new UnifiedRequestEntryDto
                {
                    RequestId = "req-20260304T113901Z-omit",
                    QueryText = "working MCP-IMPORT-001 on docs/plans/imported.md",
                    Status = "completed",
                },
            ],
        });

        var ingestor = CreateIngestor();
        var result = await ingestor.ImportToSessionLogTablesAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(1, result.Imported);
        var stored = await _db.SessionLogTurns.SingleAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.False(string.IsNullOrWhiteSpace(stored.PlanFile));
        Assert.False(string.IsNullOrWhiteSpace(stored.TodoId));
        Assert.True(stored.PlanFile == "None" || stored.PlanFile.Contains("docs/plans/imported.md", StringComparison.Ordinal));
        Assert.True(stored.TodoId == "None" || stored.TodoId == "MCP-IMPORT-001");
    }

    [Fact]
    public async Task WhenFileChangedThenImportUpdatesIt()
    {
        WriteSessionFile("changing.json", new UnifiedSessionLogDto
        {
            SourceType = "Copilot",
            SessionId = "changing-1",
            Title = "V1",
            TurnCount = 0
        });

        var ingestor = CreateIngestor();
        await ingestor.ImportToSessionLogTablesAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Change file content
        WriteSessionFile("changing.json", new UnifiedSessionLogDto
        {
            SourceType = "Copilot",
            SessionId = "changing-1",
            Title = "V2",
            TurnCount = 0
        });

        var result = await ingestor.ImportToSessionLogTablesAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(1, result.Imported);

        var stored = await _db.SessionLogs.FirstAsync(s => s.SessionId == "changing-1", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal("V2", stored.Title);
    }

    [Fact]
    public async Task WhenMixOfChangedAndUnchangedThenOnlyChangedAreImported()
    {
        WriteSessionFile("stable.json", new UnifiedSessionLogDto
        {
            SourceType = "Cursor",
            SessionId = "stable-1",
            Title = "Stable",
            TurnCount = 0
        });
        WriteSessionFile("evolving.json", new UnifiedSessionLogDto
        {
            SourceType = "Cursor",
            SessionId = "evolving-1",
            Title = "V1",
            TurnCount = 0
        });

        var ingestor = CreateIngestor();
        var first = await ingestor.ImportToSessionLogTablesAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(2, first.Imported);

        // Only change one file
        WriteSessionFile("evolving.json", new UnifiedSessionLogDto
        {
            SourceType = "Cursor",
            SessionId = "evolving-1",
            Title = "V2",
            TurnCount = 0
        });

        var second = await ingestor.ImportToSessionLogTablesAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(1, second.Imported);
        Assert.Equal(1, second.Skipped);
    }

    private SessionLogIngestor CreateIngestor()
    {
        var opts = Microsoft.Extensions.Options.Options.Create(new IngestionOptions
        {
            RepoRoot = _tempDir,
            SessionsPath = "docs/sessions"
        });
        return new SessionLogIngestor(new Chunker(), opts, new WorkspaceContext(), _service, NullLogger<SessionLogIngestor>.Instance);
    }

    private void WriteSessionFile(string filename, UnifiedSessionLogDto dto)
    {
        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(_tempDir, "docs", "sessions", filename), json);
    }

    private void WriteMdSessionFile(string filename, string content) =>
        File.WriteAllText(Path.Combine(_tempDir, "docs", "sessions", filename), content);

    // --- Phase 4a: Markdown import tests ---

    [Fact]
    public async Task WhenImportingValidMarkdownThenSessionIsPersisted()
    {
        var md = """
            # Copilot Session Log - MD Import Test

            **Date:** 2026-02-16
            **Duration:** ~2 hours
            **Branch:** feature/md-import
            **Status:** ✅ Complete
            **Model:** gpt-4o

            ## 1. Session Overview
            Tested Markdown import pipeline.

            ## 2. Changes Made
            - Added MD parser support
            - Updated ingestor
            """;
        WriteMdSessionFile("copilot-SESSION-LOG-2026-02-16.md", md);

        var ingestor = CreateIngestor();
        var result = await ingestor.ImportToSessionLogTablesAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Imported >= 1);
        var stored = await _db.SessionLogs
            .Include(s => s.Turns)
            .FirstOrDefaultAsync(s => s.SourceType == "copilot", cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.NotNull(stored);
        Assert.Contains("MD Import Test", stored!.Title, StringComparison.Ordinal);
        Assert.NotEmpty(stored.Turns);
    }

    [Fact]
    public async Task WhenImportingInvalidMarkdownThenFileIsSkipped()
    {
        WriteMdSessionFile("not-a-session-log.md", "# Regular Document\n\nThis is not a session log.");

        var ingestor = CreateIngestor();
        var result = await ingestor.ImportToSessionLogTablesAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(0, result.Imported);
    }

    [Fact]
    public async Task WhenUpsertingMarkdownThenSessionIsUpdated()
    {
        var md1 = """
            # Session Log - Upsert MD V1

            **Date:** 2026-02-16
            **Status:** 🚧 In Progress

            ## Session Overview
            Version 1.
            """;
        WriteMdSessionFile("copilot-SESSION-LOG-2026-02-16-upsert.md", md1);

        var ingestor = CreateIngestor();
        await ingestor.ImportToSessionLogTablesAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var md2 = """
            # Session Log - Upsert MD V2

            **Date:** 2026-02-16
            **Status:** ✅ Complete

            ## Session Overview
            Version 2 with updates.
            """;
        WriteMdSessionFile("copilot-SESSION-LOG-2026-02-16-upsert.md", md2);

        var result = await ingestor.ImportToSessionLogTablesAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Imported >= 1);
        var stored = await _db.SessionLogs
            .FirstOrDefaultAsync(s => s.SourceType == "copilot" &&
                s.SessionId == "copilot-session-log-2026-02-16-upsert", cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.NotNull(stored);
        Assert.Contains("V2", stored!.Title, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WhenImportingJsonAndMarkdownThenBothAreProcessed()
    {
        WriteSessionFile("cursor-test.json", new UnifiedSessionLogDto
        {
            SourceType = "Cursor",
            SessionId = "json-coexist-1",
            Title = "JSON Session",
            TurnCount = 0
        });

        var md = """
            # Session Log - MD Coexistence

            **Date:** 2026-02-16
            **Status:** Complete

            ## Session Overview
            Markdown session alongside JSON.
            """;
        WriteMdSessionFile("copilot-SESSION-LOG-2026-02-16-coexist.md", md);

        var ingestor = CreateIngestor();
        var result = await ingestor.ImportToSessionLogTablesAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Imported >= 2);
        Assert.True(result.FilesScanned >= 2);
    }
}
