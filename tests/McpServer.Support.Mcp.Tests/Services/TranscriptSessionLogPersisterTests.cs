using McpServer.SessionLog.Transcripts;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>Tests for transcript persistence through the existing session-log service.</summary>
public sealed class TranscriptSessionLogPersisterTests
{
    /// <summary>Verifies normalized transcript sessions are submitted through ISessionLogService with source artifact evidence.</summary>
    [Fact]
    public async Task PersistAsync_SubmitsUnifiedSessionLogThroughSessionLogService()
    {
        var sessionLogService = Substitute.For<ISessionLogService>();
        UnifiedSessionLogDto? capturedDto = null;
        string? capturedSourceFilePath = null;
        string? capturedContentHash = null;
        sessionLogService.SubmitAsync(
                Arg.Do<UnifiedSessionLogDto>(dto => capturedDto = dto),
                Arg.Do<string?>(path => capturedSourceFilePath = path),
                Arg.Do<string?>(hash => capturedContentHash = hash),
                Arg.Any<CancellationToken>())
            .Returns(42L);
        var persister = new TranscriptSessionLogPersister(sessionLogService);
        var sourcePath = Path.Combine(Path.GetTempPath(), "session.jsonl");
        var yamlPath = Path.Combine(Path.GetTempPath(), "session.hash.sessionlog.yaml");
        var recoveryPath = Path.Combine(Path.GetTempPath(), "native-1.hash.importRecovery.yaml");
        var request = new TranscriptIngestionRequest(sourcePath)
        {
            Agent = "Codex",
            WorkspacePath = "F:\\GitHub\\McpServer",
            Persist = true
        };
        var session = new TranscriptSession(
            TranscriptSourceKind.Codex,
            "Codex-20260710T010203Z-import",
            [
                new TranscriptEvent(
                    "event-user",
                    1,
                    "user",
                    "response_item",
                    [new TranscriptContentBlock("text", "Run the tests")],
                    DateTimeOffset.Parse("2026-07-10T01:02:03Z")),
                new TranscriptEvent(
                    "event-assistant",
                    2,
                    "assistant",
                    "response_item",
                    [new TranscriptContentBlock("text", "Tests passed")],
                    DateTimeOffset.Parse("2026-07-10T01:03:03Z"))
            ],
            "sourceType: Codex\n",
            nativeSessionId: "native-1",
            model: "gpt-5",
            workspacePath: "F:\\GitHub\\McpServer",
            sourceFiles: [sourcePath]);
        var receipt = new TranscriptSessionReceipt(
            TranscriptSourceKind.Codex,
            "native-1",
            session.SessionId,
            "hash",
            "pending",
            yamlPath,
            recoveryPath);

        var persistenceReceipt = await persister.PersistAsync(request, session, receipt, TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal("sessionLogId:42", persistenceReceipt);
        Assert.NotNull(capturedDto);
        Assert.Equal("Codex", capturedDto.SourceType);
        Assert.Equal(session.SessionId, capturedDto.SessionId);
        Assert.Equal("gpt-5", capturedDto.Model);
        Assert.Equal("completed", capturedDto.Status);
        Assert.Equal(2, capturedDto.TurnCount);
        Assert.Equal("F:\\GitHub\\McpServer", capturedDto.Workspace?.Repository);
        Assert.Equal(yamlPath, capturedSourceFilePath);
        Assert.Equal("hash", capturedContentHash);
        Assert.NotNull(capturedDto.Turns);
        Assert.Equal("Run the tests", capturedDto.Turns!.ElementAt(0).QueryText);
        Assert.Equal("Tests passed", capturedDto.Turns!.ElementAt(1).Response);
        Assert.Contains("transcript-import", capturedDto.Turns!.ElementAt(0).Tags ?? []);
    }

    /// <summary>
    /// AC-FR-MCP-SESSIONLOGCTX-001-007: omitted import fields persist extractor result or None
    /// through the real SessionLogService import path.
    /// </summary>
    [Fact]
    public async Task Import_OmittedFields_PersistsExtractorResultOrNone()
    {
        var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<McpServer.Support.Mcp.Storage.McpDbContext>()
            .UseInMemoryDatabase("import-omit-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new McpServer.Support.Mcp.Storage.McpDbContext(options);
        db.Database.EnsureCreated();
        var workspace = Path.Combine(Path.GetTempPath(), "import-omit-" + Guid.NewGuid().ToString("N"));
        db.OverrideWorkspaceId(workspace);
        var service = new SessionLogService(db, Microsoft.Extensions.Logging.Abstractions.NullLogger<SessionLogService>.Instance, workspaceContext: new WorkspaceContext { WorkspacePath = workspace });
        var persister = new TranscriptSessionLogPersister(service);
        var sourcePath = Path.Combine(Path.GetTempPath(), "omit.jsonl");
        var yamlPath = Path.Combine(Path.GetTempPath(), "omit.hash.sessionlog.yaml");
        var request = new TranscriptIngestionRequest(sourcePath)
        {
            Agent = "Codex",
            WorkspacePath = workspace,
            Persist = true
        };
        var session = new TranscriptSession(
            TranscriptSourceKind.Codex,
            "Codex-20260710T010203Z-omit",
            [
                new TranscriptEvent(
                    "event-user",
                    1,
                    "user",
                    "response_item",
                    [new TranscriptContentBlock("text", "working MCP-IMPORT-001 on docs/plans/imported.md")],
                    DateTimeOffset.Parse("2026-07-10T01:02:03Z")),
            ],
            "sourceType: Codex\n",
            nativeSessionId: "native-omit",
            model: "gpt-5",
            workspacePath: workspace,
            sourceFiles: [sourcePath]);
        var receipt = new TranscriptSessionReceipt(
            TranscriptSourceKind.Codex,
            "native-omit",
            session.SessionId,
            "hash-omit",
            "pending",
            yamlPath,
            Path.Combine(Path.GetTempPath(), "omit.recovery.yaml"));

        await persister.PersistAsync(request, session, receipt, TestContext.Current.CancellationToken).ConfigureAwait(true);

        var stored = Assert.Single(db.SessionLogTurns);
        Assert.False(string.IsNullOrWhiteSpace(stored.PlanFile));
        Assert.False(string.IsNullOrWhiteSpace(stored.TodoId));
        Assert.NotEqual((string?)null, stored.PlanFile);
        Assert.True(stored.PlanFile == "None" || stored.PlanFile.Contains("docs/plans/imported.md", StringComparison.Ordinal));
        Assert.True(stored.TodoId == "None" || stored.TodoId == "MCP-IMPORT-001");
    }
}