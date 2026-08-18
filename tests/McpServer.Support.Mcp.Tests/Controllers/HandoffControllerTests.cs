using McpServer.Support.Mcp.Controllers;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Controllers;

/// <summary>
/// TEST-HANDOFF-006: REST controller delegates ingest, inspect, and approve to the shared service.
/// </summary>
public sealed class HandoffControllerTests
{
    /// <summary>TEST-HANDOFF-006: ingest posts through IHandoffIngestionService.</summary>
    [Fact]
    public async Task IngestAsync_DelegatesToSharedService()
    {
        var service = Substitute.For<IHandoffIngestionService>();
        service.IngestAsync(Arg.Any<HandoffIngestionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new HandoffIngestionResult { Success = true });
        var sut = new HandoffController(service);

        var result = await sut.IngestAsync(
            new HandoffIngestionRequest { SourceKind = HandoffSourceKind.Content, Content = "handoff" },
            TestContext.Current.CancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(((HandoffIngestionResult)ok.Value!).Success);
        await service.Received(1).IngestAsync(Arg.Any<HandoffIngestionRequest>(), Arg.Any<CancellationToken>());
    }

    /// <summary>TEST-HANDOFF-006: get and approve use the same service contract.</summary>
    [Fact]
    public async Task GetAndApprove_DelegateToSharedService()
    {
        var service = Substitute.For<IHandoffIngestionService>();
        service.GetRunAsync("handoff-run-001", Arg.Any<CancellationToken>())
            .Returns(new HandoffIngestionResult { Success = true });
        service.ApproveAsync("handoff-run-001", Arg.Any<HandoffApprovalRequest>(), Arg.Any<CancellationToken>())
            .Returns(new HandoffIngestionResult { Success = true, Created = true });
        var sut = new HandoffController(service);

        var get = await sut.GetRunAsync("handoff-run-001", TestContext.Current.CancellationToken);
        var approve = await sut.ApproveAsync(
            "handoff-run-001",
            new HandoffApprovalRequest { Approved = true, Reviewer = "operator" },
            TestContext.Current.CancellationToken);

        Assert.IsType<OkObjectResult>(get.Result);
        Assert.IsType<OkObjectResult>(approve.Result);
        await service.Received(1).GetRunAsync("handoff-run-001", Arg.Any<CancellationToken>());
        await service.Received(1).ApproveAsync("handoff-run-001", Arg.Any<HandoffApprovalRequest>(), Arg.Any<CancellationToken>());
    }

    /// <summary>TEST-HANDOFF-006: 404 mapping uses ErrorCode, not English text.</summary>
    [Fact]
    public async Task GetRunAsync_MissingRun_UsesErrorCodeForNotFound()
    {
        var service = Substitute.For<IHandoffIngestionService>();
        service.GetRunAsync("missing", Arg.Any<CancellationToken>())
            .Returns(new HandoffIngestionResult
            {
                Success = false,
                Error = "localized or rewritten text",
                ErrorCode = "run_not_found",
            });
        var sut = new HandoffController(service);

        var result = await sut.GetRunAsync("missing", TestContext.Current.CancellationToken);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }
}
