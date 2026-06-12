// FR-MCP-REPL-006: workflow.* namespace deprecation - workflow.sessionlog lifecycle
// verbs route statelessly through the SessionLog client when explicit identifiers
// are supplied, and every workflow.* result is marked deprecated so callers migrate
// to the client.* passthrough surface.
// TEST-MCP-REPL-006A: stateless routing + deprecation markers.

using McpServer.Repl.Core;
using NSubstitute;

namespace McpServer.Repl.Core.Tests;

/// <summary>
/// Phase 1c tests: <c>workflow.sessionlog.openSession/beginTurn/completeTurn/failTurn</c>
/// with explicit (agent, sessionId, requestId) parameters bypass the legacy in-process
/// <c>SessionLogState</c> entirely and forward to the stateless SessionLog client
/// lifecycle methods; all <c>workflow.*</c> results carry <c>deprecated: true</c>.
/// </summary>
public class SessionLogDeprecationTests
{
    private const string Agent = "ClaudeCode";
    private const string SessionId = "ClaudeCode-20260612T120000Z-deprecation";
    private const string RequestId = "req-20260612T120001Z-stateless-turn";

    /// <summary>
    /// beginTurn with explicit agent + sessionId works with NO prior openSession in
    /// this process and never touches the legacy workflow state.
    /// </summary>
    [Fact]
    public async Task Dispatcher_BeginTurnWithExplicitIds_RoutesStatelesslyToClient()
    {
        var (sut, passthrough, legacyWorkflow) = BuildSut();

        var response = await sut.DispatchAsync(BuildRequest(
            "workflow.sessionlog.beginTurn",
            new Dictionary<string, object?>
            {
                ["agent"] = Agent,
                ["sessionId"] = SessionId,
                ["requestId"] = RequestId,
                ["queryTitle"] = "Stateless begin",
                ["queryText"] = "begin without in-process state",
            }), CancellationToken.None);

        Assert.Equal("result", response.Type);
        await passthrough.Received(1).InvokeAsync(
            "SessionLog",
            "BeginTurnAsync",
            Arg.Is<Dictionary<string, object?>>(d =>
                d != null && Equals(d["agent"], Agent) && Equals(d["sessionId"], SessionId) && Equals(d["requestId"], RequestId)),
            Arg.Any<CancellationToken>());
        await legacyWorkflow.DidNotReceive().BeginTurnAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        AssertDeprecated(response);
    }

    /// <summary>
    /// completeTurn with explicit ids forwards to the stateless client lifecycle
    /// method, carrying the turn payload (response + evidence) through.
    /// </summary>
    [Fact]
    public async Task Dispatcher_CompleteTurnWithExplicitIds_RoutesStatelesslyToClient()
    {
        var (sut, passthrough, legacyWorkflow) = BuildSut();

        var response = await sut.DispatchAsync(BuildRequest(
            "workflow.sessionlog.completeTurn",
            new Dictionary<string, object?>
            {
                ["agent"] = Agent,
                ["sessionId"] = SessionId,
                ["requestId"] = RequestId,
                ["response"] = "done",
                ["designDecisions"] = new List<object?> { "Decision: stateless verbs." },
            }), CancellationToken.None);

        Assert.Equal("result", response.Type);
        await passthrough.Received(1).InvokeAsync(
            "SessionLog",
            "CompleteTurnAsync",
            Arg.Is<Dictionary<string, object?>>(d =>
                d != null && Equals(d["agent"], Agent) && Equals(d["sessionId"], SessionId) && Equals(d["requestId"], RequestId) && d.ContainsKey("payload")),
            Arg.Any<CancellationToken>());
        await legacyWorkflow.DidNotReceive().CompleteTurnAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        AssertDeprecated(response);
    }

    /// <summary>failTurn with explicit ids routes statelessly and maps errorMessage to the failure note.</summary>
    [Fact]
    public async Task Dispatcher_FailTurnWithExplicitIds_RoutesStatelesslyToClient()
    {
        var (sut, passthrough, legacyWorkflow) = BuildSut();

        var response = await sut.DispatchAsync(BuildRequest(
            "workflow.sessionlog.failTurn",
            new Dictionary<string, object?>
            {
                ["agent"] = Agent,
                ["sessionId"] = SessionId,
                ["requestId"] = RequestId,
                ["errorMessage"] = "dependency missing",
                ["designDecisions"] = new List<object?> { "Decision: abort." },
            }), CancellationToken.None);

        Assert.Equal("result", response.Type);
        await passthrough.Received(1).InvokeAsync(
            "SessionLog",
            "FailTurnAsync",
            Arg.Is<Dictionary<string, object?>>(d => d != null && d.ContainsKey("payload")),
            Arg.Any<CancellationToken>());
        await legacyWorkflow.DidNotReceive().FailTurnAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
        AssertDeprecated(response);
    }

    /// <summary>openSession with explicit ids routes to the idempotent stateless client open.</summary>
    [Fact]
    public async Task Dispatcher_OpenSessionWithExplicitIds_RoutesStatelesslyToClient()
    {
        var (sut, passthrough, legacyWorkflow) = BuildSut();

        var response = await sut.DispatchAsync(BuildRequest(
            "workflow.sessionlog.openSession",
            new Dictionary<string, object?>
            {
                ["agent"] = Agent,
                ["sessionId"] = SessionId,
                ["title"] = "Deprecation test session",
                ["model"] = "claude-fable-5",
            }), CancellationToken.None);

        Assert.Equal("result", response.Type);
        await passthrough.Received(1).InvokeAsync(
            "SessionLog",
            "OpenSessionAsync",
            Arg.Is<Dictionary<string, object?>>(d =>
                d != null && Equals(d["agent"], Agent) && Equals(d["sessionId"], SessionId)),
            Arg.Any<CancellationToken>());
        await legacyWorkflow.DidNotReceive().OpenSessionAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        AssertDeprecated(response);
    }

    /// <summary>Other workflow namespaces (todo) are marked deprecated too.</summary>
    [Fact]
    public async Task Dispatcher_WorkflowTodoResponse_IsMarkedDeprecated()
    {
        var passthrough = Substitute.For<IGenericClientPassthrough>();
        var todo = Substitute.For<ITodoWorkflow>();
        todo.GetProjectionStatusAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Substitute.For<ITodoProjectionStatus>()));
        var sut = new ReplCommandDispatcher(passthrough, todoWorkflow: todo);

        var response = await sut.DispatchAsync(BuildRequest(
            "workflow.todo.getProjectionStatus",
            new Dictionary<string, object?> { ["id"] = "PLAN-X-001" }), CancellationToken.None);

        Assert.Equal("result", response.Type);
        AssertDeprecated(response);
    }

    /// <summary>client.* passthrough responses are NOT marked deprecated.</summary>
    [Fact]
    public async Task Dispatcher_ClientPassthroughResponse_IsNotMarkedDeprecated()
    {
        var passthrough = Substitute.For<IGenericClientPassthrough>();
        passthrough.InvokeAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Dictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(new { ok = true }));
        var sut = new ReplCommandDispatcher(passthrough);

        var response = await sut.DispatchAsync(BuildRequest(
            "client.todo.QueryAsync",
            new Dictionary<string, object?>()), CancellationToken.None);

        Assert.Equal("result", response.Type);
        var payload = Assert.IsType<ResultPayload>(response.Payload);
        Assert.Null(payload.Deprecated);
    }

    /// <summary>The serializer emits the deprecated flag on the wire and omits it when null.</summary>
    [Fact]
    public void YamlSerializer_ResultPayloadDeprecated_EmitsDeprecatedField()
    {
        var serializer = new YamlSerializer();

        var deprecated = serializer.Serialize(new YamlEnvelope
        {
            Type = "result",
            Payload = new ResultPayload { RequestId = "req-x", Result = new { ok = true }, Deprecated = true },
        });
        var current = serializer.Serialize(new YamlEnvelope
        {
            Type = "result",
            Payload = new ResultPayload { RequestId = "req-y", Result = new { ok = true } },
        });

        Assert.Contains("deprecated: true", deprecated, StringComparison.Ordinal);
        Assert.DoesNotContain("deprecated", current, StringComparison.Ordinal);
    }

    private static (ReplCommandDispatcher Sut, IGenericClientPassthrough Passthrough, ISessionLogWorkflow LegacyWorkflow) BuildSut()
    {
        var passthrough = Substitute.For<IGenericClientPassthrough>();
        passthrough.InvokeAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Dictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(new { ok = true }));
        var legacyWorkflow = Substitute.For<ISessionLogWorkflow>();
        var sut = new ReplCommandDispatcher(passthrough, sessionLogWorkflow: legacyWorkflow);
        return (sut, passthrough, legacyWorkflow);
    }

    private static YamlEnvelope BuildRequest(string method, Dictionary<string, object?> parameters) => new()
    {
        Type = "request",
        Payload = new RequestPayload
        {
            RequestId = $"req-20260612T120000Z-dispatch-{Guid.NewGuid().ToString("N")[..8]}",
            Method = method,
            Params = parameters,
        },
    };

    private static void AssertDeprecated(IYamlEnvelope response)
    {
        var payload = Assert.IsType<ResultPayload>(response.Payload);
        Assert.True(payload.Deprecated, "workflow.* results must be marked deprecated");
    }
}
