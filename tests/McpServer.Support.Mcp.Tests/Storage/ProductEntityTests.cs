using McpServer.Support.Mcp.Products.Commands;
using McpServer.Support.Mcp.Products.Models;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using McpServer.Support.Mcp.Tests.Products;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Storage;

/// <summary>
/// TEST-MCP-PRODUCT-001 / TR-MCP-PRODUCT-MODEL-001: Product key and uniqueness acceptance.
/// </summary>
public sealed class ProductEntityTests : IDisposable
{
    private readonly ProductHandlerTestContext _fx = new();

    /// <inheritdoc />
    public void Dispose() => _fx.Dispose();

    /// <summary>PROD-MCPSERVER is a valid key and must persist uniquely.</summary>
    [Fact]
    public async Task Create_DuplicateProdMcpserverKey_FailsConflict()
    {
        await using var db = _fx.CreateDb();
        var request = new CreateProductRequest { Key = "PROD-MCPSERVER", Name = "McpServer" };

        var first = await new CreateProductCommandHandler(db)
            .HandleAsync(new CreateProductCommand(ProductHandlerTestContext.Owner, request), _fx.CallContext);
        var second = await new CreateProductCommandHandler(db)
            .HandleAsync(new CreateProductCommand(ProductHandlerTestContext.Owner, request), _fx.CallContext);

        Assert.True(first.IsSuccess, first.Error);
        Assert.False(second.IsSuccess);
        Assert.Contains("409", second.Error, StringComparison.Ordinal);
    }

    /// <summary>
    /// TEST-MCP-PRODUCT-001 isolation: ProductEntity and ProductWorkspaceMembershipEntity
    /// are host-global and must not have a Workspace query filter.
    /// </summary>
    [Fact]
    public void ProductEntities_DoNotHaveWorkspaceQueryFilters()
    {
        using var ctx = new McpDbContext(new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"product-model-{Guid.NewGuid():N}")
            .Options);

        AssertNoWorkspaceFilter(ctx.Model.FindEntityType(typeof(ProductEntity))!);
        AssertNoWorkspaceFilter(ctx.Model.FindEntityType(typeof(ProductWorkspaceMembershipEntity))!);
    }

    private static void AssertNoWorkspaceFilter(Microsoft.EntityFrameworkCore.Metadata.IEntityType entityType)
    {
        var filterKeys = entityType.GetDeclaredQueryFilters()
            .Select(filter => filter.Key)
            .ToArray();

        Assert.DoesNotContain("Workspace", filterKeys);
    }
}
