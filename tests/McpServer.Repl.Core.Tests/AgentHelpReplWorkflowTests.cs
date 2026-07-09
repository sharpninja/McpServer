using McpServer.Client.Models;
using McpServer.Repl.Core;
using NSubstitute;

namespace McpServer.Repl.Core.Tests;

/// <summary>
/// TEST-MCP-HELP-008: REPL tests for <c>workflow.agenthelp.*</c> typed wrapper dispatch.
/// </summary>
public sealed class AgentHelpReplWorkflowTests
{
    /// <summary>Enumerates every typed workflow Agent Help method exposed to REPL callers.</summary>
    /// <returns>Method names and representative parameters.</returns>
    public static IEnumerable<object[]> AgentHelpRouteCases()
    {
        yield return [AgentHelpCommandShapes.CreateSessionMethod, new Dictionary<string, object?>
        {
            ["workspacePath"] = "F:\\GitHub\\McpServer",
            ["topic"] = "marker trust",
            ["callerAgent"] = "Codex",
            ["issueSummary"] = "Marker signature mismatch after service restart.",
        }];
        yield return [AgentHelpCommandShapes.SubmitTurnMethod, new Dictionary<string, object?>
        {
            ["sessionId"] = "help-001",
            ["userMessage"] = "What should I check first?",
        }];
        yield return [AgentHelpCommandShapes.GetStatusMethod, new Dictionary<string, object?> { ["sessionId"] = "help-001" }];
    }

    /// <summary>TEST-MCP-HELP-008: the route table covers the complete Agent Help wrapper surface.</summary>
    [Fact]
    public void AgentHelpRouteCases_CoverAllCommandShapes()
    {
        Assert.Equal(3, AgentHelpRouteCases().Count());
    }

    /// <summary>
    /// TEST-MCP-HELP-008: workflow.agenthelp.* routes through <see cref="IAgentHelpWorkflow"/> and
    /// returns deprecated metadata consistent with the other workflow namespaces.
    /// </summary>
    /// <param name="method">The workflow Agent Help method.</param>
    /// <param name="parameters">Representative YAML-bound parameters.</param>
    [Theory]
    [MemberData(nameof(AgentHelpRouteCases))]
    public async Task Dispatcher_AgentHelpWorkflowMethod_RoutesToRegisteredWorkflow(
        string method,
        Dictionary<string, object?> parameters)
    {
        var workflow = Substitute.For<IAgentHelpWorkflow>();
        ConfigureWorkflow(workflow);
        var sut = new ReplCommandDispatcher(
            Substitute.For<IGenericClientPassthrough>(),
            agentHelpWorkflow: workflow);

        var response = await sut.DispatchAsync(BuildRequest(method, parameters), CancellationToken.None);

        Assert.Equal("result", response.Type);
        var payload = Assert.IsType<ResultPayload>(response.Payload);
        Assert.True(payload.Deprecated);
        await AssertReceivedAsync(workflow, method);
    }

    /// <summary>TEST-MCP-HELP-008: submit turn requires userMessage before dispatch.</summary>
    [Fact]
    public async Task Dispatcher_AgentHelpSubmitTurnMissingMessage_ReturnsInvocationError()
    {
        var workflow = Substitute.For<IAgentHelpWorkflow>();
        var sut = new ReplCommandDispatcher(
            Substitute.For<IGenericClientPassthrough>(),
            agentHelpWorkflow: workflow);

        var response = await sut.DispatchAsync(BuildRequest(
            AgentHelpCommandShapes.SubmitTurnMethod,
            new Dictionary<string, object?> { ["sessionId"] = "help-001" }),
            CancellationToken.None);

        Assert.Equal("error", response.Type);
        var payload = Assert.IsAssignableFrom<IErrorPayload>(response.Payload);
        Assert.Equal("method_invocation_error", payload.Code);
        await workflow.DidNotReceiveWithAnyArgs().SubmitTurnAsync(default!, default!, cancellationToken: TestContext.Current.CancellationToken);
    }

    /// <summary>TEST-MCP-HELP-008: unknown workflow.agenthelp methods return method_not_found.</summary>
    [Fact]
    public async Task Dispatcher_UnknownAgentHelpMethod_ReturnsMethodNotFound()
    {
        var workflow = Substitute.For<IAgentHelpWorkflow>();
        var sut = new ReplCommandDispatcher(
            Substitute.For<IGenericClientPassthrough>(),
            agentHelpWorkflow: workflow);

        var response = await sut.DispatchAsync(BuildRequest(
            "workflow.agenthelp.unknown",
            new Dictionary<string, object?>()),
            CancellationToken.None);

        Assert.Equal("error", response.Type);
        var payload = Assert.IsAssignableFrom<IErrorPayload>(response.Payload);
        Assert.Equal("method_not_found", payload.Code);
        await workflow.DidNotReceiveWithAnyArgs().CreateSessionAsync(cancellationToken: TestContext.Current.CancellationToken);
    }

    private static YamlEnvelope BuildRequest(string method, Dictionary<string, object?> parameters) => new()
    {
        Type = "request",
        Payload = new RequestPayload
        {
            RequestId = $"req-20260708T000000Z-agenthelp-{Guid.NewGuid().ToString("N")[..8]}",
            Method = method,
            Params = parameters,
        },
    };

    private static void ConfigureWorkflow(IAgentHelpWorkflow workflow)
    {
        workflow.CreateSessionAsync(Arg.Any<AgentHelpSessionCreateRequest?>(), Arg.Any<CancellationToken>())
            .Returns(new AgentHelpSessionCreateResponse
            {
                SessionId = "help-001",
                Status = "created",
                ExecutionStrategy = "stub",
            });
        workflow.SubmitTurnAsync(Arg.Any<string>(), Arg.Any<AgentHelpTurnRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AgentHelpTurnResponse
            {
                SessionId = "help-001",
                TurnId = "turn-001",
                Status = "completed",
                LatencyMs = 1,
            });
        workflow.GetStatusAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new AgentHelpSessionStatusDto
            {
                SessionId = "help-001",
                Status = "created",
                CreatedUtc = "2026-07-08T00:00:00Z",
                LastUpdatedUtc = "2026-07-08T00:00:00Z",
                ExecutionStrategy = "stub",
            });
    }

    private static async Task AssertReceivedAsync(IAgentHelpWorkflow workflow, string method)
    {
        switch (method)
        {
            case AgentHelpCommandShapes.CreateSessionMethod:
                await workflow.Received(1).CreateSessionAsync(
                    Arg.Is<AgentHelpSessionCreateRequest?>(request => MatchesCreateSessionRequest(request)),
                    Arg.Any<CancellationToken>());
                break;
            case AgentHelpCommandShapes.SubmitTurnMethod:
                await workflow.Received(1).SubmitTurnAsync(
                    "help-001",
                    Arg.Is<AgentHelpTurnRequest>(request => MatchesSubmitTurnRequest(request)),
                    Arg.Any<CancellationToken>());
                break;
            case AgentHelpCommandShapes.GetStatusMethod:
                await workflow.Received(1).GetStatusAsync("help-001", Arg.Any<CancellationToken>());
                break;
        }
    }

    private static bool MatchesCreateSessionRequest(AgentHelpSessionCreateRequest? request)
        => request is not null
           && request.WorkspacePath == "F:\\GitHub\\McpServer"
           && request.Topic == "marker trust"
           && request.CallerAgent == "Codex"
           && request.IssueSummary == "Marker signature mismatch after service restart."
           && request.AgentSeed is null;

    private static bool MatchesSubmitTurnRequest(AgentHelpTurnRequest? request)
        => request is not null && request.UserMessage == "What should I check first?";
}
