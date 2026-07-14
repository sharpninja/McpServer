using McpServer.SessionLog.Transcripts;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
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
}