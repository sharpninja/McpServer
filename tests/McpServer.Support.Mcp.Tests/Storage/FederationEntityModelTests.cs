using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.Tests.Storage;

/// <summary>Model-shape tests for durable hub-and-spoke federation entities.</summary>
public sealed class FederationEntityModelTests
{
    /// <summary>Federation proxy records use ProxyId as the durable key.</summary>
    [Fact]
    public void FederationProxy_PrimaryKey_IsProxyId()
    {
        using var ctx = CreateContext();
        var entity = ctx.Model.FindEntityType(typeof(FederationProxyEntity));
        Assert.NotNull(entity);

        var pk = entity!.FindPrimaryKey();
        Assert.NotNull(pk);
        Assert.Equal(["ProxyId"], pk!.Properties.Select(p => p.Name).ToArray());
    }

    /// <summary>Federation operation records use OperationId for idempotent replay.</summary>
    [Fact]
    public void FederationOperation_PrimaryKey_IsOperationId()
    {
        using var ctx = CreateContext();
        var entity = ctx.Model.FindEntityType(typeof(FederationOperationEntity));
        Assert.NotNull(entity);

        var pk = entity!.FindPrimaryKey();
        Assert.NotNull(pk);
        Assert.Equal(["OperationId"], pk!.Properties.Select(p => p.Name).ToArray());
    }

    /// <summary>Federation entities are global hub state and are not workspace-filtered.</summary>
    [Fact]
    public void FederationEntities_DoNotHaveWorkspaceQueryFilters()
    {
        using var ctx = CreateContext();

        Assert.Empty(ctx.Model.FindEntityType(typeof(FederationProxyEntity))!.GetDeclaredQueryFilters());
        Assert.Empty(ctx.Model.FindEntityType(typeof(FederationWorkspaceEntity))!.GetDeclaredQueryFilters());
        Assert.Empty(ctx.Model.FindEntityType(typeof(FederationOperationEntity))!.GetDeclaredQueryFilters());
        Assert.Empty(ctx.Model.FindEntityType(typeof(FederationOutboxEntity))!.GetDeclaredQueryFilters());
        Assert.Empty(ctx.Model.FindEntityType(typeof(FederationConflictEntity))!.GetDeclaredQueryFilters());
    }

    private static McpDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"fed-model-{Guid.NewGuid():N}")
            .Options;
        return new McpDbContext(options);
    }
}
