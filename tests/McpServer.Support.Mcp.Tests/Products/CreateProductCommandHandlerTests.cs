using McpServer.Support.Mcp.Products.Commands;
using McpServer.Support.Mcp.Products.Models;
using McpServer.Support.Mcp.Products.Queries;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Products;

/// <summary>
/// TEST-MCP-PRODUCT-001 / FR-MCP-PRODUCT-001: CreateProductCommandHandler acceptance.
/// </summary>
public sealed class CreateProductCommandHandlerTests : IDisposable
{
    private readonly ProductHandlerTestContext _fx = new();

    /// <inheritdoc />
    public void Dispose() => _fx.Dispose();

    /// <summary>Valid key PROD-MCPSERVER succeeds and returns owner workspace.</summary>
    [Fact]
    public async Task HandleAsync_ValidProdMcpserverKey_ReturnsOwnerAndKey()
    {
        await using var db = _fx.CreateDb();
        var result = await ProductHandlerTestContext.Create(db).HandleAsync(
            new CreateProductCommand(ProductHandlerTestContext.Owner, new CreateProductRequest
            {
                Key = "PROD-MCPSERVER",
                Name = "McpServer",
            }),
            _fx.CallContext);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal("PROD-MCPSERVER", result.Value!.Key);
        Assert.Equal(ProductHandlerTestContext.Owner, result.Value.OwnerWorkspaceId);
        Assert.Contains(ProductHandlerTestContext.Owner, result.Value.MemberWorkspaceIds);
    }

    /// <summary>Invalid keys are rejected (400 semantics in the result error).</summary>
    /// <param name="key">Invalid product key.</param>
    [Theory]
    [InlineData("")]
    [InlineData("mcpserver")]
    [InlineData("prod-mcpserver")]
    [InlineData("MCP-SERVER")]
    public async Task HandleAsync_InvalidKey_Fails(string key)
    {
        await using var db = _fx.CreateDb();
        var result = await ProductHandlerTestContext.Create(db).HandleAsync(
            new CreateProductCommand(ProductHandlerTestContext.Owner, new CreateProductRequest
            {
                Key = key,
                Name = "Bad",
            }),
            _fx.CallContext);

        Assert.False(result.IsSuccess);
        Assert.Contains("400", result.Error, StringComparison.Ordinal);
    }

    /// <summary>Soft-delete hides the product from a subsequent get.</summary>
    [Fact]
    public async Task HandleAsync_SoftDelete_HidesFromGet()
    {
        await using var db = _fx.CreateDb();
        await _fx.CreateDefaultProductAsync(db);
        var deleted = await ProductHandlerTestContext.Delete(db).HandleAsync(
            new DeleteProductCommand(ProductHandlerTestContext.Owner, "PROD-MCPSERVER"),
            _fx.CallContext);
        var visible = await ProductHandlerTestContext.Get(db).HandleAsync(
            new GetProductQuery(ProductHandlerTestContext.Owner, "PROD-MCPSERVER"),
            _fx.CallContext);

        Assert.True(deleted.IsSuccess, deleted.Error);
        Assert.False(visible.IsSuccess);
        Assert.Contains("404", visible.Error, StringComparison.Ordinal);
    }

    /// <summary>Outsider get does not leak product existence.</summary>
    [Fact]
    public async Task HandleAsync_OutsiderGet_IsNotFound()
    {
        await using var db = _fx.CreateDb();
        await _fx.CreateDefaultProductAsync(db);
        var result = await ProductHandlerTestContext.Get(db).HandleAsync(
            new GetProductQuery(ProductHandlerTestContext.Outsider, "PROD-MCPSERVER"),
            _fx.CallContext);

        Assert.False(result.IsSuccess);
        Assert.Contains("404", result.Error, StringComparison.Ordinal);
    }

    /// <summary>FR-MCP-PRODUCT-001 ac-4: non-owner update is 403.</summary>
    [Fact]
    public async Task HandleAsync_NonOwnerUpdate_FailsForbidden()
    {
        await using var db = _fx.CreateDb();
        await _fx.CreateDefaultProductAsync(db);
        await _fx.AddDefaultMemberAsync(db);
        var result = await ProductHandlerTestContext.Update(db).HandleAsync(
            new UpdateProductCommand(ProductHandlerTestContext.Member, "PROD-MCPSERVER", new UpdateProductRequest
            {
                Name = "Hijack",
            }),
            _fx.CallContext);

        Assert.False(result.IsSuccess);
        Assert.Contains("403", result.Error, StringComparison.Ordinal);
    }

    /// <summary>FR-MCP-PRODUCT-001 ac-4: non-owner delete is 403.</summary>
    [Fact]
    public async Task HandleAsync_NonOwnerDelete_FailsForbidden()
    {
        await using var db = _fx.CreateDb();
        await _fx.CreateDefaultProductAsync(db);
        await _fx.AddDefaultMemberAsync(db);
        var result = await ProductHandlerTestContext.Delete(db).HandleAsync(
            new DeleteProductCommand(ProductHandlerTestContext.Member, "PROD-MCPSERVER"),
            _fx.CallContext);

        Assert.False(result.IsSuccess);
        Assert.Contains("403", result.Error, StringComparison.Ordinal);
    }

    /// <summary>Owner update changes name.</summary>
    [Fact]
    public async Task HandleAsync_OwnerUpdate_ChangesName()
    {
        await using var db = _fx.CreateDb();
        await _fx.CreateDefaultProductAsync(db);
        var result = await ProductHandlerTestContext.Update(db).HandleAsync(
            new UpdateProductCommand(ProductHandlerTestContext.Owner, "PROD-MCPSERVER", new UpdateProductRequest
            {
                Name = "McpServer Product",
                Description = "Shared catalog",
            }),
            _fx.CallContext);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal("McpServer Product", result.Value!.Name);
        Assert.Equal("Shared catalog", result.Value.Description);
    }

    /// <summary>FR-MCP-PRODUCT-001 ac-5: soft-delete hides the product from the default list.</summary>
    [Fact]
    public async Task HandleAsync_SoftDelete_HidesFromDefaultList()
    {
        await using var db = _fx.CreateDb();
        await _fx.CreateDefaultProductAsync(db);
        var deleted = await ProductHandlerTestContext.Delete(db).HandleAsync(
            new DeleteProductCommand(ProductHandlerTestContext.Owner, "PROD-MCPSERVER"),
            _fx.CallContext);
        var listed = await ProductHandlerTestContext.List(db).HandleAsync(
            new ListProductsQuery(ProductHandlerTestContext.Owner),
            _fx.CallContext);

        Assert.True(deleted.IsSuccess, deleted.Error);
        Assert.True(listed.IsSuccess, listed.Error);
        Assert.DoesNotContain(listed.Value!, p => p.Key == "PROD-MCPSERVER");
    }

    /// <summary>Soft-delete stops membership reads.</summary>
    [Fact]
    public async Task HandleAsync_SoftDelete_StopsMembershipReads()
    {
        await using var db = _fx.CreateDb();
        await _fx.CreateDefaultProductAsync(db);
        var deleted = await ProductHandlerTestContext.Delete(db).HandleAsync(
            new DeleteProductCommand(ProductHandlerTestContext.Owner, "PROD-MCPSERVER"),
            _fx.CallContext);
        var members = await ProductHandlerTestContext.ListMembers(db).HandleAsync(
            new ListProductMembersQuery(ProductHandlerTestContext.Owner, "PROD-MCPSERVER"),
            _fx.CallContext);

        Assert.True(deleted.IsSuccess, deleted.Error);
        Assert.False(members.IsSuccess);
        Assert.Contains("404", members.Error, StringComparison.Ordinal);
    }
}
