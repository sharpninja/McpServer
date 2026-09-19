using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using NSubstitute;
using Xunit;
using MsOptions = Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-221 / FR-MCP-173: Agent-pool runtime mutations skip coordinator/keyserver.
/// </summary>
public sealed class TransactionGatedAgentPoolServiceTests
{
    /// <summary>start-agent delegates to the inner pool while required transactions are active.</summary>
    [Fact]
    public async Task StartAgentAsync_WhenTransactionsRequired_DelegatesToInner()
    {
        var inner = Substitute.For<IAgentPoolService>();
        inner.StartAgentAsync("planner", @"F:\GitHub\McpServer", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new AgentPoolMutationResult { Success = true }));
        var sut = CreateSut(inner, new CapturingCoordinator(enabled: true));

        var result = await sut.StartAgentAsync("planner", @"F:\GitHub\McpServer", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Success);
        await inner.Received(1)
            .StartAgentAsync("planner", @"F:\GitHub\McpServer", Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>enqueue delegates when the coordinator is degraded.</summary>
    [Fact]
    public async Task EnqueueOneShotAsync_WhenCoordinatorDegraded_DelegatesToInner()
    {
        var inner = Substitute.For<IAgentPoolService>();
        inner.EnqueueOneShotAsync(Arg.Any<AgentPoolOneShotRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new AgentPoolEnqueueResult { Success = true }));
        var sut = CreateSut(inner, new CapturingCoordinator(enabled: true, degraded: true, message: "txn degraded"));

        var result = await sut.EnqueueOneShotAsync(new AgentPoolOneShotRequest { PromptText = "plan" }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Success);
        await inner.Received(1)
            .EnqueueOneShotAsync(Arg.Any<AgentPoolOneShotRequest>(), Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>connect delegates to the inner pool while required transactions are active.</summary>
    [Fact]
    public async Task ConnectInteractiveAsync_WhenTransactionsRequired_DelegatesToInner()
    {
        var inner = Substitute.For<IAgentPoolService>();
        inner.ConnectInteractiveAsync("planner", @"F:\GitHub\McpServer", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new AgentPoolConnectResult { Success = true }));
        var sut = CreateSut(inner, new CapturingCoordinator(enabled: true));

        var result = await sut.ConnectInteractiveAsync("planner", @"F:\GitHub\McpServer", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Success);
        await inner.Received(1)
            .ConnectInteractiveAsync("planner", @"F:\GitHub\McpServer", Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>read-only agent status queries delegate while mutation transactions are required.</summary>
    [Fact]
    public async Task GetAgentsAsync_WhenTransactionsRequired_Delegates()
    {
        var inner = Substitute.For<IAgentPoolService>();
        inner.GetAgentsAsync(@"F:\GitHub\McpServer", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<AgentPoolAgentStatusDto>>([CreateAgentStatus()]));
        var sut = CreateSut(inner, new CapturingCoordinator(enabled: true));

        var result = await sut.GetAgentsAsync(@"F:\GitHub\McpServer", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Single(result);
        await inner.Received(1)
            .GetAgentsAsync(@"F:\GitHub\McpServer", Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>workspace seed delegates because it only materializes configured in-memory pool slots.</summary>
    [Fact]
    public async Task SeedWorkspaceAgentsAsync_WhenTransactionsRequired_Delegates()
    {
        var inner = Substitute.For<IAgentPoolService>();
        var sut = CreateSut(inner, new CapturingCoordinator(enabled: true));

        await sut.SeedWorkspaceAgentsAsync(@"F:\GitHub\McpServer", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        await inner.Received(1)
            .SeedWorkspaceAgentsAsync(@"F:\GitHub\McpServer", Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>start-agent delegates when mutation transactions are not required.</summary>
    [Fact]
    public async Task StartAgentAsync_WhenTransactionsNotRequired_Delegates()
    {
        var inner = Substitute.For<IAgentPoolService>();
        inner.StartAgentAsync("planner", @"F:\GitHub\McpServer", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new AgentPoolMutationResult { Success = true }));
        var sut = CreateSut(
            inner,
            new CapturingCoordinator(enabled: true),
            new TurnTransactionOptions { Enabled = true, RequiredForMutations = false });

        var result = await sut.StartAgentAsync("planner", @"F:\GitHub\McpServer", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Success);
        await inner.Received(1)
            .StartAgentAsync("planner", @"F:\GitHub\McpServer", Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    private static TransactionGatedAgentPoolService CreateSut(
        IAgentPoolService inner,
        ITurnTransactionCoordinator coordinator,
        TurnTransactionOptions? options = null)
        => new(
            inner,
            coordinator,
            MsOptions.Options.Create(options ?? new TurnTransactionOptions { Enabled = true, RequiredForMutations = true }));

    private static AgentPoolAgentStatusDto CreateAgentStatus()
        => new()
        {
            AgentName = "planner",
            WorkspacePath = @"F:\GitHub\McpServer",
            Lifecycle = "stopped",
        };

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
