using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Storage;

/// <summary>
/// Verifies requirements storage follows the same workspace discriminator
/// contract as TODO/session/context storage.
/// </summary>
public sealed class RequirementEntity_WorkspaceScopingTests
{
    /// <summary>Requirement rows use a composite key so ids can repeat per workspace.</summary>
    [Fact]
    public void Requirement_PrimaryKey_IsWorkspaceKindAndId()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"req-pk-{Guid.NewGuid():N}")
            .Options;

        using var ctx = new McpDbContext(options);
        var entity = ctx.Model.FindEntityType(typeof(RequirementEntity));
        Assert.NotNull(entity);

        var pk = entity!.FindPrimaryKey();
        Assert.NotNull(pk);
        Assert.Equal(
            new[] { "WorkspaceId", "Kind", "Id" },
            pk!.Properties.Select(p => p.Name).ToArray());
    }

    /// <summary>Traceability rows are also workspace-scoped and multi-link capable.</summary>
    [Fact]
    public void RequirementTraceability_PrimaryKey_IsWorkspaceFrTargetKindAndTargetId()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"req-link-pk-{Guid.NewGuid():N}")
            .Options;

        using var ctx = new McpDbContext(options);
        var entity = ctx.Model.FindEntityType(typeof(RequirementTraceabilityLinkEntity));
        Assert.NotNull(entity);

        var pk = entity!.FindPrimaryKey();
        Assert.NotNull(pk);
        Assert.Equal(
            new[] { "WorkspaceId", "FrId", "TargetKind", "TargetId" },
            pk!.Properties.Select(p => p.Name).ToArray());
    }

    /// <summary>Both requirement tables must be clamped by global query filters.</summary>
    [Fact]
    public void RequirementEntities_HaveGlobalQueryFilters()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"req-filter-{Guid.NewGuid():N}")
            .Options;

        using var ctx = new McpDbContext(options);

        Assert.NotEmpty(ctx.Model.FindEntityType(typeof(RequirementEntity))!.GetDeclaredQueryFilters());
        Assert.NotEmpty(ctx.Model.FindEntityType(typeof(RequirementTraceabilityLinkEntity))!.GetDeclaredQueryFilters());
    }
}
