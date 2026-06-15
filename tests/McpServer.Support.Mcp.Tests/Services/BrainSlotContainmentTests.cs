using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Indexing;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>Tests for deferred quad branch containment. TEST-MCP-180.</summary>
public sealed class BrainSlotContainmentTests
{
    /// <summary>AoT reconciliation execution remains explicitly disabled in this slice.</summary>
    [Fact]
    public void ExecuteAotReconciliation_ReturnsDeferredFeatureDisabled()
    {
        var service = new BrainSlotContainmentService();

        var response = service.ExecuteAotReconciliation(new BrainSlotDeferredRequest { TurnId = "turn-1" });

        Assert.Equal("rejected", response.Status);
        Assert.Equal(BrainSlotReasonCodes.DeferredFeatureDisabled, response.Reason);
        Assert.Null(response.Output);
    }

    /// <summary>Weight updates remain explicitly disabled in this slice.</summary>
    [Fact]
    public void ExecuteWeightUpdate_ReturnsDeferredFeatureDisabled()
    {
        var service = new BrainSlotContainmentService();

        var response = service.ExecuteWeightUpdate(new BrainSlotDeferredRequest { TurnId = "turn-1" });

        Assert.Equal("rejected", response.Status);
        Assert.Equal(BrainSlotReasonCodes.DeferredFeatureDisabled, response.Reason);
        Assert.Null(response.Output);
    }

    /// <summary>Full automatic quad orchestration remains explicitly disabled in this slice.</summary>
    [Fact]
    public void ExecuteFullOrchestration_ReturnsDeferredFeatureDisabled()
    {
        var service = new BrainSlotContainmentService();

        var response = service.ExecuteFullOrchestration(new BrainSlotDeferredRequest { TurnId = "turn-1" });

        Assert.Equal("rejected", response.Status);
        Assert.Equal(BrainSlotReasonCodes.DeferredFeatureDisabled, response.Reason);
        Assert.Null(response.Output);
    }

    /// <summary>Direct context admission also rejects non-Curiosity slots.</summary>
    [Fact]
    public async Task ContextAdmission_WhenRoleIsNotCuriosity_ThrowsDeferredFeatureDisabled()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase("brain-slot-admission-" + Guid.NewGuid().ToString("N"))
            .Options;
        using var db = new McpDbContext(options, new WorkspaceContext { WorkspacePath = @"F:\GitHub\McpServer" });
        var service = new BrainSlotContextAdmissionService(
            db,
            new Chunker(),
            Substitute.For<IEmbeddingService>(),
            Substitute.For<IVectorIndexService>(),
            NullLogger<BrainSlotContextAdmissionService>.Instance);

        var ex = await Assert.ThrowsAsync<BrainSlotValidationException>(() =>
            service.AdmitAsync(new BrainSlotDefinitionEntity
            {
                SlotId = "left-main",
                Role = BrainSlotRoles.LeftHemisphere,
            }, "output", "txn-1")).ConfigureAwait(true);

        Assert.Equal(BrainSlotReasonCodes.DeferredFeatureDisabled, ex.Reason);
    }
}
