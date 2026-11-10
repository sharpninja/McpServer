using System.Text.Json;
using QBrainAi.Support.Mcp.Ingestion;
using QBrainAi.Support.Mcp.Indexing;
using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Ingestion;

/// <summary>TR-PLANNED-CORE-013 (historical): Tests for SessionLogIngestor.ImportToSessionLogTablesAsync (MVP-SUPPORT-011).</summary>
/// <remarks>2026-11-07, TR-PLANNED-CORE-013; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Ingestion/SessionLogIngestorImportTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 5 Category=Integration cases moved unchanged with the class attributes; the state and helpers both halves use come from the abstract base class SessionLogIngestorImportTestsBase in Ingestion/SessionLogIngestorImportTestsBase.cs of QBrainAi.Support.Mcp.Tests, linked into this project (DRY); only the namespace changed.</remarks>
public sealed class SessionLogIngestorImportTests : SessionLogIngestorImportTestsBase, IDisposable
{
    /// <remarks>Integration: imports physical session files through the real extractor, whose profile scan is read-only and not asserted for content (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
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

    /// <summary>
    /// AC-FR-MCP-SESSIONLOGCTX-001-007 / TEST-MCP-SESSIONLOG-006:
    /// ingest of omitted planFile/todoId persists extractor result or None, never null.
    /// </summary>
    /// <remarks>Integration: imports physical session files through the real extractor, whose profile scan is read-only and not asserted for content (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
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

    // --- Phase 4a: Markdown import tests ---

    /// <remarks>Integration: imports physical session files through the real extractor, whose profile scan is read-only and not asserted for content (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
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

    /// <remarks>Integration: imports physical session files through the real extractor, whose profile scan is read-only and not asserted for content (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
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

    /// <remarks>Integration: imports physical session files through the real extractor, whose profile scan is read-only and not asserted for content (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
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
