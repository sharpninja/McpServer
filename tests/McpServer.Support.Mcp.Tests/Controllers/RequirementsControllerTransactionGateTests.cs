using McpServer.Support.Mcp.Controllers;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Requirements;
using McpServer.Support.Mcp.Requirements.Models;
using McpServer.Support.Mcp.Services;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;
using MsOptions = Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Tests.Controllers;

/// <summary>
/// TEST-MCP-161: Verifies requirements whole-document ingest fails closed before
/// multi-record repository mutations when turn transactions are required.
/// </summary>
public sealed class RequirementsControllerTransactionGateTests
{
    private const string FunctionalMarkdown = """
        ## FR-MCP-TXNINGEST-001 Transaction-gated ingest

        Requirements ingest should fail closed while turn transactions are required.
        """;

    /// <summary>
    /// TEST-MCP-161: Required transaction mode rejects ingest before reading or
    /// mutating the requirements repository.
    /// </summary>
    [Fact]
    public async Task IngestAsync_WhenTransactionsRequired_ReturnsConflictWithoutCallingRepository()
    {
        var requirements = Substitute.For<IRequirementsDocumentService>();
        var controller = CreateController(requirements, new CapturingCoordinator());

        var result = await controller.IngestAsync(
                new RequirementsIngestRequest { FunctionalMarkdown = FunctionalMarkdown },
                CancellationToken.None)
            .ConfigureAwait(true);

        Assert.IsType<ConflictObjectResult>(result.Result);
        await requirements.DidNotReceive().GetAllFrAsync(Arg.Any<CancellationToken>()).ConfigureAwait(true);
        await requirements.DidNotReceive().AddFrAsync(Arg.Any<FrEntry>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
        await requirements.DidNotReceive().UpdateFrAsync(Arg.Any<FrEntry>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>
    /// TEST-MCP-161: Degraded transaction security rejects ingest with the
    /// coordinator message before repository access.
    /// </summary>
    [Fact]
    public async Task IngestAsync_WhenCoordinatorDegraded_ReturnsConflictWithoutCallingRepository()
    {
        var requirements = Substitute.For<IRequirementsDocumentService>();
        var controller = CreateController(requirements, new CapturingCoordinator(degraded: true, message: "txn degraded"));

        var result = await controller.IngestAsync(
                new RequirementsIngestRequest { FunctionalMarkdown = FunctionalMarkdown },
                CancellationToken.None)
            .ConfigureAwait(true);

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Contains("txn degraded", conflict.Value?.ToString(), StringComparison.Ordinal);
        await requirements.DidNotReceive().GetAllFrAsync(Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>
    /// TEST-MCP-161: Non-required transaction mode preserves the existing
    /// requirements ingest behavior.
    /// </summary>
    [Fact]
    public async Task IngestAsync_WhenTransactionsNotRequired_DelegatesToExistingIngestPath()
    {
        var requirements = Substitute.For<IRequirementsDocumentService>();
        requirements.GetAllFrAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<FrEntry>());
        requirements.GetAllTrAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<TrEntry>());
        requirements.GetAllTestAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<TestEntry>());
        requirements.GetAllMappingsAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<FrTrMapping>());
        var controller = CreateController(
            requirements,
            new CapturingCoordinator(),
            new TurnTransactionOptions { Enabled = true, RequiredForMutations = false });

        var result = await controller.IngestAsync(
                new RequirementsIngestRequest { FunctionalMarkdown = FunctionalMarkdown },
                CancellationToken.None)
            .ConfigureAwait(true);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var ingest = Assert.IsType<RequirementsIngestResult>(ok.Value);
        Assert.Equal(1, ingest.FunctionalAdded);
        await requirements.Received(1).AddFrAsync(
                Arg.Is<FrEntry>(entry => entry != null && entry.Id == "FR-MCP-TXNINGEST-001"),
                Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    private static RequirementsController CreateController(
        IRequirementsDocumentService requirements,
        ITurnTransactionCoordinator coordinator,
        TurnTransactionOptions? transactionOptions = null)
        => new(
            requirements,
            MsOptions.Options.Create(new RequirementsOptions()),
            new WorkspaceContext { WorkspacePath = @"F:\GitHub\McpServer" },
            Substitute.For<ITodoExecutionService>(),
            NullLogger<RequirementsController>.Instance,
            coordinator,
            MsOptions.Options.Create(transactionOptions ?? new TurnTransactionOptions { Enabled = true, RequiredForMutations = true }));

    private sealed class CapturingCoordinator : ITurnTransactionCoordinator
    {
        private readonly bool _degraded;
        private readonly string _message;

        public CapturingCoordinator(bool degraded = false, string message = "ready")
        {
            _degraded = degraded;
            _message = message;
        }

        public TurnTransactionRequest? Request { get; private set; }

        public Task<TurnTransactionResult> ExecuteAsync(
            TurnTransactionRequest request,
            Func<CancellationToken, Task<TurnMutationResult>> mutation,
            CancellationToken cancellationToken = default)
        {
            Request = request;
            return Task.FromResult(new TurnTransactionResult
            {
                TransactionId = request.TransactionId ?? "txn-requirements-ingest-test",
                Status = "committed",
                Reason = TransactionFailureReason.None,
                MutationApplied = false,
            });
        }

        public TurnTransactionStatusResponse GetStatus()
            => new()
            {
                Enabled = true,
                Degraded = _degraded,
                LastReason = TransactionFailureReason.None,
                Message = _message,
            };
    }
}
