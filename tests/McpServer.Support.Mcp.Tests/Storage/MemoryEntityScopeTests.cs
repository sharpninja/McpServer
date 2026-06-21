using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Storage;

/// <summary>
/// TR-MCP-MEMORY-001 acceptance: memory rows use globally unique ids and support
/// both Global rows with no workspace owner and Workspace rows clamped by the
/// active workspace query filter.
/// </summary>
public sealed class MemoryEntityScopeTests
{
    /// <summary>
    /// Memory ids are globally unique across scopes, so the EF primary key is
    /// <c>Id</c> alone rather than the workspace-composite key used by TODOs.
    /// </summary>
    [Fact]
    public void Memory_PrimaryKey_IsIdOnly()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"memory-pk-{Guid.NewGuid():N}")
            .Options;

        using var ctx = new McpDbContext(options);
        var entity = ctx.Model.FindEntityType(typeof(MemoryEntity));
        Assert.NotNull(entity);

        var pk = entity!.FindPrimaryKey();
        Assert.NotNull(pk);
        Assert.Equal(["Id"], pk!.Properties.Select(p => p.Name).ToArray());
    }

    /// <summary>
    /// Global memories must not be assigned to the synthetic global workspace
    /// row; their workspace owner remains null by contract.
    /// </summary>
    [Fact]
    public void Memory_WorkspaceId_IsNullable()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"memory-null-workspace-{Guid.NewGuid():N}")
            .Options;

        using var ctx = new McpDbContext(options);
        var entity = ctx.Model.FindEntityType(typeof(MemoryEntity));
        Assert.NotNull(entity);

        var workspaceId = entity!.FindProperty(nameof(MemoryEntity.WorkspaceId));
        Assert.NotNull(workspaceId);
        Assert.True(workspaceId!.IsNullable);
    }

    /// <summary>
    /// The memory entity must be protected by a named workspace query filter so
    /// workspace-scoped rows cannot leak between tenants while Global rows
    /// remain visible.
    /// </summary>
    [Fact]
    public void Memory_HasWorkspaceAndSoftDeleteQueryFilters()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"memory-filter-{Guid.NewGuid():N}")
            .Options;

        using var ctx = new McpDbContext(options);
        var entity = ctx.Model.FindEntityType(typeof(MemoryEntity));
        Assert.NotNull(entity);

        var filters = entity!.GetDeclaredQueryFilters().Select(filter => filter.Key).ToArray();
        Assert.Contains("Workspace", filters);
        Assert.Contains("SoftDelete", filters);
    }
}
