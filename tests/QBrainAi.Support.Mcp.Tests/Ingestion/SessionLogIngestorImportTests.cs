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

namespace QBrainAi.Support.Mcp.Tests.Ingestion;

/// <summary>TR-PLANNED-CORE-013 (historical): Tests for SessionLogIngestor.ImportToSessionLogTablesAsync (MVP-SUPPORT-011).</summary>
/// <remarks>2026-11-07, TR-PLANNED-CORE-013; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the unit half of this class: its 5 Category=Integration cases moved unchanged to the class of the same name in the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S7, hostile validation finding R14-F4), so this unit-owned project holds only unit cases; the state and helpers both halves use (_db, _service, _tempDir, .ctor, Dispose, CreateIngestor, WriteSessionFile and WriteMdSessionFile) moved once into the abstract base class SessionLogIngestorImportTestsBase in Ingestion/SessionLogIngestorImportTestsBase.cs beside this file, which both halves derive from and the integration project links (DRY); the unit cases and the class attributes are unchanged.</remarks>
public sealed class SessionLogIngestorImportTests : SessionLogIngestorImportTestsBase, IDisposable
{
    [Fact]
    public async Task WhenImportingWithStringWorkspaceThenWorkspaceIsHandled()
    {
        var json = """
        {
            "sourceType": "Cursor",
            "sessionId": "ws-string",
            "title": "String Workspace",
            "workspace": "Q:\\fixture\\ExampleApp",
            "turnCount": 0
        }
        """;
        File.WriteAllText(Path.Combine(_tempDir, "docs", "sessions", "cursor-ws.json"), json);

        var ingestor = CreateIngestor();
        var result = await ingestor.ImportToSessionLogTablesAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(1, result.Imported);
        var stored = await _db.SessionLogs.FirstAsync(s => s.SessionId == "ws-string", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(@"Q:\fixture\ExampleApp", stored.Repository);
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
                "project": "ExampleApp",
                "targetFramework": ".NET 9",
                "repository": "example-org/ExampleApp",
                "branch": "develop"
            },
            "turnCount": 0
        }
        """;
        File.WriteAllText(Path.Combine(_tempDir, "docs", "sessions", "copilot-ws.json"), json);

        var ingestor = CreateIngestor();
        await ingestor.ImportToSessionLogTablesAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var stored = await _db.SessionLogs.FirstAsync(s => s.SessionId == "ws-object", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal("ExampleApp", stored.Project);
        Assert.Equal(".NET 9", stored.TargetFramework);
        Assert.Equal("example-org/ExampleApp", stored.Repository);
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

    [Fact]
    public async Task WhenImportingInvalidMarkdownThenFileIsSkipped()
    {
        WriteMdSessionFile("not-a-session-log.md", "# Regular Document\n\nThis is not a session log.");

        var ingestor = CreateIngestor();
        var result = await ingestor.ImportToSessionLogTablesAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(0, result.Imported);
    }
}
