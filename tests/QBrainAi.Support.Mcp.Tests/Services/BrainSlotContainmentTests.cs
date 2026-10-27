using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Indexing;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>Tests for remaining brain-slot containment boundaries. TEST-MCP-179.</summary>
public sealed class BrainSlotContainmentTests
{
    /// <summary>Direct context admission also rejects non-Curiosity slots.</summary>
    [Fact]
    public async Task ContextAdmission_WhenRoleIsNotCuriosity_ThrowsDeferredFeatureDisabled()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase("brain-slot-admission-" + Guid.NewGuid().ToString("N"))
            .Options;
        using var db = new McpDbContext(options, new WorkspaceContext { WorkspacePath = @"Q:\__mcp_unit_test__\QBrainAi" });
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
                Role = BrainSlotRoles.Creativity,
            }, "output", "txn-1", cancellationToken: TestContext.Current.CancellationToken)).ConfigureAwait(true);

        Assert.Equal(BrainSlotReasonCodes.DeferredFeatureDisabled, ex.Reason);
    }
}
