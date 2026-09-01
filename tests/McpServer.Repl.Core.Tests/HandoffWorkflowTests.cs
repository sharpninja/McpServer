using McpServer.Client.Models;
using McpServer.Repl.Core;
using NSubstitute;
using Xunit;

namespace McpServer.Repl.Core.Tests;

/// <summary>
/// TEST-HANDOFF-006: REPL workflow.handoff methods delegate to the typed client contract.
/// </summary>
public sealed class HandoffWorkflowTests
{
    /// <summary>TEST-HANDOFF-006: ingest, get, and approve are routed on the dispatcher.</summary>
    [Fact]
    public async Task Dispatcher_RoutesHandoffWorkflowMethods()
    {
        var workflow = Substitute.For<IHandoffWorkflow>();
        workflow.IngestAsync(Arg.Any<HandoffIngestionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new HandoffIngestionResult { Success = true });
        workflow.GetAsync("handoff-run-001", Arg.Any<CancellationToken>())
            .Returns(new HandoffIngestionResult { Success = true });
        workflow.ApproveAsync("handoff-run-001", Arg.Any<HandoffApprovalRequest>(), Arg.Any<CancellationToken>())
            .Returns(new HandoffIngestionResult { Success = true, Created = true });

        var sut = new ReplCommandDispatcher(Substitute.For<IGenericClientPassthrough>(), handoffWorkflow: workflow);

        var ingest = await sut.DispatchAsync(Request(HandoffCommandShapes.IngestMethod, new Dictionary<string, object?>
        {
            ["sourceKind"] = "Content",
            ["content"] = "handoff",
            ["mode"] = "DraftOnly",
        }), TestContext.Current.CancellationToken);
        var get = await sut.DispatchAsync(Request(HandoffCommandShapes.GetMethod, new Dictionary<string, object?>
        {
            ["runId"] = "handoff-run-001",
        }), TestContext.Current.CancellationToken);
        var approve = await sut.DispatchAsync(Request(HandoffCommandShapes.ApproveMethod, new Dictionary<string, object?>
        {
            ["runId"] = "handoff-run-001",
            ["approved"] = true,
        }), TestContext.Current.CancellationToken);

        Assert.Equal("result", ingest.Type);
        Assert.Equal("result", get.Type);
        Assert.Equal("result", approve.Type);
        await workflow.Received(1).IngestAsync(Arg.Any<HandoffIngestionRequest>(), Arg.Any<CancellationToken>());
        await workflow.Received(1).GetAsync("handoff-run-001", Arg.Any<CancellationToken>());
        await workflow.Received(1).ApproveAsync("handoff-run-001", Arg.Any<HandoffApprovalRequest>(), Arg.Any<CancellationToken>());
    }

    /// <summary>P1-7: REPL rejects numeric mode 999 before calling the workflow.</summary>
    [Fact]
    public async Task Dispatcher_NumericMode999_ReturnsInvocationError()
    {
        var workflow = Substitute.For<IHandoffWorkflow>();
        var sut = new ReplCommandDispatcher(Substitute.For<IGenericClientPassthrough>(), handoffWorkflow: workflow);
        var result = await sut.DispatchAsync(Request(HandoffCommandShapes.IngestMethod, new Dictionary<string, object?>
        {
            ["sourceKind"] = "Content",
            ["content"] = "handoff",
            ["mode"] = "999",
        }), TestContext.Current.CancellationToken);

        Assert.Equal("error", result.Type);
        await workflow.DidNotReceiveWithAnyArgs().IngestAsync(default!, cancellationToken: TestContext.Current.CancellationToken);
    }

    private static IYamlEnvelope Request(string method, Dictionary<string, object?> args)
        => new YamlEnvelope
        {
            Type = "request",
            Payload = new RequestPayload
            {
                RequestId = "req-20260816T180000Z-handoff",
                Method = method,
                Params = args,
            },
        };
}
