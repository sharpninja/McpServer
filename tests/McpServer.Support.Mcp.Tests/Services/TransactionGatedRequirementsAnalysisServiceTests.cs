using McpServer.Support.Mcp.Services;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-161: TODO requirements analysis fails closed while required turn
/// transactions are active because analyzer side effects are not compensated.
/// </summary>
public sealed class TransactionGatedRequirementsAnalysisServiceTests
{
    /// <summary>
    /// TEST-MCP-161: Required transaction gating blocks TODO requirements
    /// analysis before Copilot or requirements document side effects run.
    /// </summary>
    [Fact]
    public async Task AnalyzeAsync_WhenRequiredTransactionsActive_FailsClosedWithoutCallingInner()
    {
        var inner = Substitute.For<IRequirementsService>();
        var coordinator = new StatusCoordinator
        {
            Status = new TurnTransactionStatusResponse
            {
                Enabled = true,
                Degraded = false,
                Message = "ready",
            },
        };
        var sut = new TransactionGatedRequirementsAnalysisService(
            inner,
            coordinator,
            Microsoft.Extensions.Options.Options.Create(new TurnTransactionOptions { Enabled = true, RequiredForMutations = true }));

        var result = await sut.AnalyzeAsync("PLAN-TXN-001", CancellationToken.None).ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Contains("not transaction compensated", result.Error, StringComparison.OrdinalIgnoreCase);
        await inner.DidNotReceiveWithAnyArgs().AnalyzeAsync(default!, default).ConfigureAwait(true);
    }

    /// <summary>
    /// TEST-MCP-161: Degraded transaction state blocks analyzer side effects even
    /// before checking the required-for-mutations option.
    /// </summary>
    [Fact]
    public async Task AnalyzeAsync_WhenCoordinatorDegraded_FailsClosedWithoutCallingInner()
    {
        var inner = Substitute.For<IRequirementsService>();
        var coordinator = new StatusCoordinator
        {
            Status = new TurnTransactionStatusResponse
            {
                Enabled = true,
                Degraded = true,
                Message = "subscriber unavailable",
            },
        };
        var sut = new TransactionGatedRequirementsAnalysisService(
            inner,
            coordinator,
            Microsoft.Extensions.Options.Options.Create(new TurnTransactionOptions { Enabled = true, RequiredForMutations = true }));

        var result = await sut.AnalyzeAsync("PLAN-TXN-001", CancellationToken.None).ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Contains("subscriber unavailable", result.Error, StringComparison.Ordinal);
        await inner.DidNotReceiveWithAnyArgs().AnalyzeAsync(default!, default).ConfigureAwait(true);
    }

    /// <summary>
    /// TEST-MCP-161: When mutation transactions are not required, analyzer
    /// behavior remains delegated to the existing service.
    /// </summary>
    [Fact]
    public async Task AnalyzeAsync_WhenTransactionsAreNotRequired_DelegatesToInner()
    {
        var expected = new RequirementsAnalysisResult(
            true,
            FunctionalRequirements: ["FR-MCP-120"],
            TechnicalRequirements: ["TR-MCP-TXN-001"]);
        var inner = Substitute.For<IRequirementsService>();
        inner.AnalyzeAsync("PLAN-TXN-001", Arg.Any<CancellationToken>()).Returns(expected);
        var coordinator = new StatusCoordinator
        {
            Status = new TurnTransactionStatusResponse
            {
                Enabled = true,
                Degraded = false,
                Message = "ready",
            },
        };
        var sut = new TransactionGatedRequirementsAnalysisService(
            inner,
            coordinator,
            Microsoft.Extensions.Options.Options.Create(new TurnTransactionOptions { Enabled = true, RequiredForMutations = false }));

        var result = await sut.AnalyzeAsync("PLAN-TXN-001", CancellationToken.None).ConfigureAwait(true);

        Assert.True(result.Success);
        Assert.Equal(["FR-MCP-120"], result.FunctionalRequirements);
        await inner.Received(1).AnalyzeAsync("PLAN-TXN-001", Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    private sealed class StatusCoordinator : ITurnTransactionCoordinator
    {
        public TurnTransactionStatusResponse Status { get; init; } = new();

        public Task<TurnTransactionResult> ExecuteAsync(
            TurnTransactionRequest request,
            Func<CancellationToken, Task<TurnMutationResult>> mutation,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException("Requirements analyzer fail-closed tests should not execute transactions.");

        public TurnTransactionStatusResponse GetStatus() => Status;
    }
}
