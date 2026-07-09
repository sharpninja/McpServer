using McpServer.Client.Models;
using McpServer.Repl.Core;
using NSubstitute;

namespace McpServer.Repl.Core.Tests;

/// <summary>
/// TEST-MCP-REPL-025: validates the REPL-native session-log persistence command contract.
/// </summary>
public sealed class SessionLogPersistenceDispatcherTests
{
    /// <summary>
    /// A degraded persistence result is returned as structured terminal-notification data.
    /// </summary>
    [Fact]
    public async Task DispatchAsync_PersistTurnDegraded_ReturnsFailsafeDetails()
    {
        var passthrough = Substitute.For<IGenericClientPassthrough>();
        var persistence = Substitute.For<ISessionLogPersistenceStrategy>();
        var path = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "degraded-turn.yaml"));
        persistence.Name.Returns("mcp-service-with-failsafe");
        persistence.PersistAsync(
                Arg.Any<UnifiedSessionLogDto>(),
                Arg.Any<CancellationToken>())
            .Returns(new SessionLogPersistenceResult(
                Persisted: true,
                Degraded: true,
                Strategy: "filesystem-failsafe",
                FailsafePath: path,
                Message: "MCP Session Log persistence is degraded."));
        var sut = new ReplCommandDispatcher(
            passthrough,
            sessionLogPersistenceStrategy: persistence);
        var snapshot = CreateSessionLog();
        var envelope = new YamlEnvelope
        {
            Type = "request",
            Payload = new RequestPayload
            {
                RequestId = "req-20260709T214000Z-persist-turn",
                Method = SessionLogCommandShapes.PersistTurnMethod,
                Params = new Dictionary<string, object?>
                {
                    ["sessionLog"] = snapshot
                }
            }
        };

        var response = await sut.DispatchAsync(envelope, TestContext.Current.CancellationToken);

        Assert.Equal("result", response.Type);
        var payload = Assert.IsType<ResultPayload>(response.Payload);
        var result = Assert.IsType<Dictionary<string, object?>>(payload.Result);
        Assert.Equal(true, result["persisted"]);
        Assert.Equal(true, result["degraded"]);
        Assert.Equal("filesystem-failsafe", result["persistenceStrategy"]);
        Assert.Equal(path, result["failsafePath"]);
        Assert.Equal("MCP Session Log persistence is degraded.", result["message"]);
        await persistence.Received(1).PersistAsync(
            Arg.Is<UnifiedSessionLogDto>(value =>
                value != null &&
                value.SessionId == snapshot.SessionId &&
                value.Turns != null &&
                value.Turns.Count == 1 &&
                value.Turns[0].RequestId == snapshot.Turns![0].RequestId),
            Arg.Any<CancellationToken>());
        await passthrough.DidNotReceiveWithAnyArgs().InvokeAsync(
            default!,
            default!,
            default!,
            TestContext.Current.CancellationToken);
    }

    private static UnifiedSessionLogDto CreateSessionLog() =>
        new()
        {
            SourceType = "Codex",
            SessionId = "Codex-20260709T214000Z-dispatcher-test",
            Title = "Dispatcher persistence test",
            Model = "gpt-5.3-codex",
            Started = "2026-07-09T21:40:00Z",
            LastUpdated = "2026-07-09T21:40:01Z",
            Status = "in_progress",
            TurnCount = 1,
            Turns =
            [
                new UnifiedRequestEntryDto
                {
                    RequestId = "req-20260709T214001Z-dispatcher-turn",
                    Timestamp = "2026-07-09T21:40:01Z",
                    QueryTitle = "Persist through REPL",
                    QueryText = "Persist this turn.",
                    Response = "Done.",
                    Status = "completed",
                    Model = "gpt-5.3-codex"
                }
            ]
        };
}

