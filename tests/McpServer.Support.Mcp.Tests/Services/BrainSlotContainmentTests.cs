using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Indexing;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

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
            }, "output", "txn-1", cancellationToken: TestContext.Current.CancellationToken)).ConfigureAwait(true);

        Assert.Equal(BrainSlotReasonCodes.DeferredFeatureDisabled, ex.Reason);
    }
}
