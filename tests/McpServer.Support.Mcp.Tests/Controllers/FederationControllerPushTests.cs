using McpServer.Support.Mcp.Controllers;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace McpServer.Support.Mcp.Tests.Controllers;

/// <summary>
/// Unit tests for the federation push endpoint on <see cref="FederationController"/>.
/// FR-MCP-085, TEST-MCP-FED-005.
/// </summary>
public sealed class FederationControllerPushTests
{
    private readonly IFederationPushService _pushService = Substitute.For<IFederationPushService>();

    private static FederationRegistry CreateRegistry(bool enabled = false, string? defaultTarget = null)
    {
        var opts = new FederationOptions { Enabled = enabled };
        if (defaultTarget is not null)
        {
            opts.DefaultTarget = defaultTarget;
            opts.Targets.Add(new FederationTargetOptions { Name = defaultTarget, BaseUrl = "http://remote:7147" });
        }
        return new FederationRegistry(Microsoft.Extensions.Options.Options.Create(opts));
    }

    private static TunnelRegistry CreateEmptyTunnelRegistry()
    {
        var opts = Microsoft.Extensions.Options.Options.Create(new TunnelOptions());
        return new TunnelRegistry([], opts, NullLogger<TunnelRegistry>.Instance);
    }

    private FederationController CreateController(
        FederationRegistry? registry = null,
        ITurnTransactionCoordinator? transactionCoordinator = null,
        TurnTransactionOptions? transactionOptions = null)
    {
        registry ??= CreateRegistry();
        return new FederationController(
            registry,
            CreateEmptyTunnelRegistry(),
            _pushService,
            transactionCoordinator: transactionCoordinator,
            transactionOptions: Microsoft.Extensions.Options.Options.Create(
                transactionOptions ?? new TurnTransactionOptions { Enabled = true, RequiredForMutations = true }));
    }

    /// <summary>Valid push request returns 200 with push result.</summary>
    [Fact]
    public async Task Push_ValidRequest_Returns200WithResult()
    {
        var expected = new FederationPushResult(3, 0, []);
        _pushService.PushAllAsync(Arg.Any<CancellationToken>()).Returns(expected);

        var controller = CreateController(CreateRegistry(enabled: true, defaultTarget: "remote"));
        var result = await controller.Push(new FederationPushRequest(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var pushResult = Assert.IsType<FederationPushResult>(ok.Value);
        Assert.Equal(3, pushResult.Succeeded);
        Assert.Equal(0, pushResult.Failed);
    }

    /// <summary>TEST-MCP-161: Federation push fails closed before invoking remote push side effects.</summary>
    [Fact]
    public async Task Push_WhenTransactionsRequired_ReturnsConflictWithoutCallingPushService()
    {
        var controller = CreateController(
            CreateRegistry(enabled: true, defaultTarget: "remote"),
            new CapturingCoordinator(enabled: true));

        var result = await controller.Push(new FederationPushRequest(), CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result.Result);
        await _pushService.DidNotReceiveWithAnyArgs().PushAllAsync(ct: TestContext.Current.CancellationToken);
        await _pushService.DidNotReceiveWithAnyArgs().PushTodosAsync(ct: TestContext.Current.CancellationToken);
        await _pushService.DidNotReceiveWithAnyArgs().PushSessionLogsAsync(ct: TestContext.Current.CancellationToken);
    }

    /// <summary>When federation is disabled, push returns 409 Conflict.</summary>
    [Fact]
    public async Task Push_FederationDisabled_Returns409()
    {
        var controller = CreateController(CreateRegistry(enabled: false));
        var result = await controller.Push(new FederationPushRequest(), CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    /// <summary>When no federation target exists, push returns 404.</summary>
    [Fact]
    public async Task Push_NoTarget_Returns404()
    {
        var controller = CreateController(CreateRegistry(enabled: true)); // no targets
        var result = await controller.Push(new FederationPushRequest(), CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    /// <summary>Push with specific types filters correctly.</summary>
    [Fact]
    public async Task Push_SpecificTypes_FiltersCorrectly()
    {
        var expected = new FederationPushResult(1, 0, []);
        _pushService.PushTodosAsync(Arg.Any<CancellationToken>()).Returns(expected);

        var controller = CreateController(CreateRegistry(enabled: true, defaultTarget: "remote"));
        var result = await controller.Push(new FederationPushRequest { Types = ["todos"] }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var pushResult = Assert.IsType<FederationPushResult>(ok.Value);
        Assert.Equal(1, pushResult.Succeeded);
        await _pushService.DidNotReceiveWithAnyArgs().PushSessionLogsAsync(ct: TestContext.Current.CancellationToken);
    }

    private sealed class CapturingCoordinator : ITurnTransactionCoordinator
    {
        private readonly TurnTransactionStatusResponse _status;

        public CapturingCoordinator(bool enabled, bool degraded = false, string message = "")
        {
            _status = new TurnTransactionStatusResponse
            {
                Enabled = enabled,
                Degraded = degraded,
                Message = message,
            };
        }

        public Task<TurnTransactionResult> ExecuteAsync(
            TurnTransactionRequest request,
            Func<CancellationToken, Task<TurnMutationResult>> mutation,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public TurnTransactionStatusResponse GetStatus() => _status;
    }
}
