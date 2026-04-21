using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Storage;

/// <summary>
/// TR-MCP-TODO-008 Phase 1 acceptance: <see cref="TodoDocumentMetadataEntity"/>
/// MUST use the composite primary key <c>(WorkspaceId, SingletonId = 1)</c> so
/// every workspace owns exactly one metadata singleton without colliding with
/// other workspaces' singletons. <see cref="TodoDocumentMetadataEntity.SingletonId"/>
/// MUST remain caller-assigned (<see cref="ValueGenerated.Never"/>) under the
/// new composite key to preserve the TR-MCP-TODO-007 SQL Server
/// <c>IDENTITY_INSERT</c> guarantee.
/// </summary>
public sealed class TodoDocumentMetadata_CompositePkTests
{
    /// <summary>
    /// The primary key MUST be the composite <c>(WorkspaceId, SingletonId)</c>.
    /// </summary>
    [Fact(Skip = "TR-MCP-TODO-008 Phase 1 pending: composite PK not yet configured")]
    public void TodoDocumentMetadata_PrimaryKey_IsCompositeWorkspaceIdAndSingletonId()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"todo-meta-pk-{System.Guid.NewGuid():N}")
            .Options;

        using var ctx = new McpDbContext(options);
        var entity = ctx.Model.FindEntityType(typeof(TodoDocumentMetadataEntity));
        Assert.NotNull(entity);

        var pk = entity!.FindPrimaryKey();
        Assert.NotNull(pk);
        Assert.Equal(
            new[] { "WorkspaceId", "SingletonId" },
            pk!.Properties.Select(p => p.Name).ToArray());
    }

    /// <summary>
    /// <c>SingletonId</c> MUST remain <see cref="ValueGenerated.Never"/> even
    /// after the composite-PK change: SQL Server <c>IDENTITY_INSERT OFF</c>
    /// rejects value assignment on identity columns with error 544, so the
    /// per-workspace singleton seed must stay explicitly caller-assigned.
    /// </summary>
    [Fact(Skip = "TR-MCP-TODO-008 Phase 1 pending: composite PK not yet configured")]
    public void TodoDocumentMetadata_SingletonId_StaysValueGeneratedNever_UnderCompositeKey()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"todo-meta-valuegen-{System.Guid.NewGuid():N}")
            .Options;

        using var ctx = new McpDbContext(options);
        var entity = ctx.Model.FindEntityType(typeof(TodoDocumentMetadataEntity));
        var singletonId = entity!.FindProperty(nameof(TodoDocumentMetadataEntity.SingletonId));
        Assert.NotNull(singletonId);
        Assert.Equal(ValueGenerated.Never, singletonId!.ValueGenerated);
    }

    /// <summary>
    /// Two workspaces MUST be able to own their own singleton row with
    /// <c>SingletonId = 1</c> without violating the primary key constraint.
    /// </summary>
    [Fact(Skip = "TR-MCP-TODO-008 Phase 1 pending: composite PK not yet configured")]
    public void TwoWorkspaces_CanEachOwnSingletonRow()
        => Assert.Fail("TR-MCP-TODO-008 Phase 1 not implemented: dual-workspace singleton insert path exercised once the WorkspaceId property lands");
}
