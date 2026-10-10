using McpServer.Client.Models;
using McpServer.Repl.Core;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace McpServer.Repl.Core.Tests;

/// <summary>
/// FR-MCP-REPL-009: non-terminal session-log methods stay successful when primary
/// MCP persistence fails and failsafe persistence succeeds. Terminal close reports
/// the degraded strategy name and absolute failsafe path.
/// </summary>
public sealed class SessionLogPersistenceCoordinatorIsolationTests
{
    /// <summary>FR-MCP-REPL-009 AC1: open remains successful on primary fail + failsafe ok.</summary>
    [Fact]
    public async Task Coordinator_Open_PrimaryFailFailsafeOk_PluginSeesSuccess()
    {
        await AssertNonTerminalSucceedsWhenPrimaryFailsAsync(SessionLogCommandShapes.OpenSessionMethod).ConfigureAwait(true);
    }

    /// <summary>FR-MCP-REPL-009 AC1: begin remains successful on primary fail + failsafe ok.</summary>
    [Fact]
    public async Task Coordinator_Begin_PrimaryFailFailsafeOk_PluginSeesSuccess()
    {
        await AssertNonTerminalSucceedsWhenPrimaryFailsAsync(SessionLogCommandShapes.BeginTurnMethod).ConfigureAwait(true);
    }

    /// <summary>FR-MCP-REPL-009 AC1: update remains successful on primary fail + failsafe ok.</summary>
    [Fact]
    public async Task Coordinator_Update_PrimaryFailFailsafeOk_PluginSeesSuccess()
    {
        await AssertNonTerminalSucceedsWhenPrimaryFailsAsync(SessionLogCommandShapes.UpdateTurnMethod).ConfigureAwait(true);
    }

    /// <summary>FR-MCP-REPL-009 AC1: appendDialog remains successful on primary fail + failsafe ok.</summary>
    [Fact]
    public async Task Coordinator_AppendDialog_PrimaryFailFailsafeOk_PluginSeesSuccess()
    {
        await AssertNonTerminalSucceedsWhenPrimaryFailsAsync(SessionLogCommandShapes.AppendDialogMethod).ConfigureAwait(true);
    }

    /// <summary>FR-MCP-REPL-009 AC1: appendActions remains successful on primary fail + failsafe ok.</summary>
    [Fact]
    public async Task Coordinator_AppendActions_PrimaryFailFailsafeOk_PluginSeesSuccess()
    {
        await AssertNonTerminalSucceedsWhenPrimaryFailsAsync(SessionLogCommandShapes.AppendActionsMethod).ConfigureAwait(true);
    }

    /// <summary>FR-MCP-REPL-009 AC3: completeTurn and failTurn report degraded strategy and absolute path.</summary>
    [Fact]
    public async Task Coordinator_CompleteTurnAndFailTurn_PrimaryFail_ReportsDegradedStrategyNameAndAbsolutePath()
    {
        var path = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "coord-complete-failsafe.yaml"));
        foreach (var method in new[] { SessionLogCommandShapes.CompleteTurnMethod, SessionLogCommandShapes.FailTurnMethod })
        {
            var harness = CreateCoordinator(path, primaryThrows: true);
            var response = await harness.Dispatcher.DispatchAsync(CreateEnvelope(method), TestContext.Current.CancellationToken).ConfigureAwait(true);
            Assert.Equal("result", response.Type);
            var payload = Assert.IsType<ResultPayload>(response.Payload);
            var result = Assert.IsType<Dictionary<string, object?>>(payload.Result);
            Assert.Equal(true, result["degraded"]);
            Assert.Equal("filesystem-failsafe", result["persistenceStrategy"]);
            Assert.Equal(path, result["failsafePath"]);
            Assert.True(Path.IsPathFullyQualified(Assert.IsType<string>(result["failsafePath"])));
            await harness.Failsafe.Received(1).PersistAsync(
                Arg.Any<UnifiedSessionLogDto>(),
                Arg.Any<CancellationToken>()).ConfigureAwait(true);
        }
    }

    /// <summary>FR-MCP-REPL-009 AC4: primary success produces no pending failsafe artifact.</summary>
    [Fact]
    public async Task Coordinator_PrimarySuccess_NoPendingFailsafeArtifact()
    {
        var path = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "coord-should-not-write.yaml"));
        var harness = CreateCoordinator(path, primaryThrows: false);
        var response = await harness.Dispatcher.DispatchAsync(
            CreateEnvelope(SessionLogCommandShapes.CompleteTurnMethod),
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal("result", response.Type);
        var payload = Assert.IsType<ResultPayload>(response.Payload);
        var result = Assert.IsType<Dictionary<string, object?>>(payload.Result);
        Assert.NotEqual(true, result.GetValueOrDefault("degraded"));
        Assert.False(File.Exists(path));
        await harness.Failsafe.DidNotReceive().PersistAsync(
            Arg.Any<UnifiedSessionLogDto>(),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    private static async Task AssertNonTerminalSucceedsWhenPrimaryFailsAsync(string method)
    {
        var path = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "coord-nonterminal.yaml"));
        var harness = CreateCoordinator(path, primaryThrows: true);
        var response = await harness.Dispatcher.DispatchAsync(CreateEnvelope(method), TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal("result", response.Type);
        Assert.IsType<ResultPayload>(response.Payload);
        await harness.Failsafe.Received(1).PersistAsync(
            Arg.Any<UnifiedSessionLogDto>(),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    private static CoordinatorHarness CreateCoordinator(string failsafePath, bool primaryThrows)
    {
        var passthrough = Substitute.For<IGenericClientPassthrough>();
        passthrough.InvokeAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Dictionary<string, object?>>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("service unavailable"));

        var primary = Substitute.For<ISessionLogPersistenceStrategy>();
        var failsafe = Substitute.For<ISessionLogPersistenceStrategy>();
        primary.Name.Returns("mcp-service");
        failsafe.Name.Returns("filesystem-failsafe");
        if (primaryThrows)
        {
            primary.PersistAsync(Arg.Any<UnifiedSessionLogDto>(), Arg.Any<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("service unavailable"));
        }
        else
        {
            primary.PersistAsync(Arg.Any<UnifiedSessionLogDto>(), Arg.Any<CancellationToken>())
                .Returns(new SessionLogPersistenceResult(
                    Persisted: true,
                    Degraded: false,
                    Strategy: "mcp-service",
                    FailsafePath: null,
                    Message: null));
        }

        failsafe.PersistAsync(Arg.Any<UnifiedSessionLogDto>(), Arg.Any<CancellationToken>())
            .Returns(new SessionLogPersistenceResult(
                Persisted: true,
                Degraded: true,
                Strategy: "filesystem-failsafe",
                FailsafePath: failsafePath,
                Message: "MCP Session Log persistence is degraded."));

        var coordinator = new FailoverSessionLogPersistenceStrategy(primary, failsafe);
        return new CoordinatorHarness(
            new ReplCommandDispatcher(passthrough, sessionLogPersistenceStrategy: coordinator),
            failsafe);
    }

    private sealed record CoordinatorHarness(
        ReplCommandDispatcher Dispatcher,
        ISessionLogPersistenceStrategy Failsafe);

    private static YamlEnvelope CreateEnvelope(string method)
    {
        return new YamlEnvelope
        {
            Type = "request",
            Payload = new RequestPayload
            {
                RequestId = "req-20260821T220000Z-001-coord",
                Method = method,
                Params = new Dictionary<string, object?>
                {
                    ["agent"] = "GrokCode",
                    ["sessionId"] = "GrokCode-20260821T220000Z-coord",
                    ["title"] = "Coordinator isolation",
                    ["model"] = "grok-4.6",
                    ["requestId"] = "req-20260821T220000Z-001-coord",
                    ["queryTitle"] = "Coordinator isolation",
                    ["queryText"] = "Prove non-terminal isolation.",
                    ["response"] = "ok",
                    ["errorMessage"] = "failed",
                    ["planFile"] = "None",
                    ["todoId"] = "None",
                    ["dialogItems"] = new[]
                    {
                        new Dictionary<string, object?>
                        {
                            ["role"] = "model",
                            ["content"] = "dialog",
                            ["category"] = "observation"
                        }
                    },
                    ["actions"] = new[]
                    {
                        new Dictionary<string, object?>
                        {
                            ["order"] = 1,
                            ["description"] = "action",
                            ["type"] = "read",
                            ["status"] = "completed"
                        }
                    }
                }
            }
        };
    }
}
