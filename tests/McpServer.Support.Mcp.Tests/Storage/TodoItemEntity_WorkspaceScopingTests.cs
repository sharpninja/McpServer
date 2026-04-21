using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Storage;

/// <summary>
/// TR-MCP-TODO-008 Phase 1 acceptance: <see cref="TodoItemEntity"/> MUST carry
/// a <c>WorkspaceId</c> column and use the composite primary key
/// <c>(WorkspaceId, Id)</c> so the same canonical TODO id may coexist across
/// workspaces without collision. These tests are deliberately failing stubs
/// under the Byrd Development Process: they document the target contract
/// before the entity + <see cref="McpDbContext"/> Fluent configuration ship in
/// Phase 1 of the workspace-scoped TODO plan.
/// </summary>
public sealed class TodoItemEntity_WorkspaceScopingTests
{
    /// <summary>
    /// The EF model MUST declare the primary key as the composite
    /// <c>(WorkspaceId, Id)</c> rather than <c>Id</c> alone.
    /// </summary>
    [Fact(Skip = "TR-MCP-TODO-008 Phase 1 pending: composite PK not yet configured")]
    public void TodoItem_PrimaryKey_IsCompositeWorkspaceIdAndId()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"todo-pk-{System.Guid.NewGuid():N}")
            .Options;

        using var ctx = new McpDbContext(options);
        var entity = ctx.Model.FindEntityType(typeof(TodoItemEntity));
        Assert.NotNull(entity);

        var pk = entity!.FindPrimaryKey();
        Assert.NotNull(pk);
        Assert.Equal(
            new[] { "WorkspaceId", "Id" },
            pk!.Properties.Select(p => p.Name).ToArray());
    }

    /// <summary>
    /// A <c>WorkspaceId</c> column MUST exist on <see cref="TodoItemEntity"/>
    /// so the TR-MCP-MT-003 global query filter has something to clamp on.
    /// </summary>
    [Fact(Skip = "TR-MCP-TODO-008 Phase 1 pending: WorkspaceId property not yet added")]
    public void TodoItem_HasWorkspaceIdProperty()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"todo-ws-{System.Guid.NewGuid():N}")
            .Options;

        using var ctx = new McpDbContext(options);
        var entity = ctx.Model.FindEntityType(typeof(TodoItemEntity));
        var workspaceId = entity!.FindProperty("WorkspaceId");
        Assert.NotNull(workspaceId);
        Assert.Equal(typeof(string), workspaceId!.ClrType);
    }

    /// <summary>
    /// Audit rows MUST also carry <c>WorkspaceId</c> so history queries
    /// under the global query filter never leak across workspaces.
    /// </summary>
    [Fact(Skip = "TR-MCP-TODO-008 Phase 1 pending: audit WorkspaceId not yet added")]
    public void TodoAuditHistory_HasWorkspaceIdPropertyAndIndex()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"todo-audit-ws-{System.Guid.NewGuid():N}")
            .Options;

        using var ctx = new McpDbContext(options);
        var entity = ctx.Model.FindEntityType(typeof(TodoAuditHistoryEntity));
        var workspaceId = entity!.FindProperty("WorkspaceId");
        Assert.NotNull(workspaceId);

        var hasIndex = entity.GetIndexes()
            .Any(ix => ix.Properties.Count == 1 && ix.Properties[0].Name == "WorkspaceId");
        Assert.True(hasIndex, "TodoAuditHistoryEntity must have an index on WorkspaceId");
    }

    /// <summary>
    /// The global query filter registered on <see cref="TodoItemEntity"/>
    /// MUST reference the ambient workspace id, matching the TR-MCP-MT-003
    /// pattern installed on the other 12+ multi-tenant entities.
    /// </summary>
    [Fact(Skip = "TR-MCP-TODO-008 Phase 1 pending: query filter not yet installed")]
    public void TodoItem_HasGlobalQueryFilter()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"todo-filter-{System.Guid.NewGuid():N}")
            .Options;

        using var ctx = new McpDbContext(options);
        var entity = ctx.Model.FindEntityType(typeof(TodoItemEntity));
        Assert.NotNull(entity);
        Assert.NotEmpty(entity!.GetDeclaredQueryFilters());
    }
}
