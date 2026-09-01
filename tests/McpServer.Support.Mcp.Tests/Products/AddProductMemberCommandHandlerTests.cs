using McpServer.Support.Mcp.Products.Commands;
using McpServer.Support.Mcp.Products.Queries;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Products;

/// <summary>
/// TEST-MCP-PRODUCT-001 / FR-MCP-PRODUCT-002: AddProductMemberCommandHandler acceptance.
/// </summary>
public sealed class AddProductMemberCommandHandlerTests : IDisposable
{
    private readonly ProductHandlerTestContext _fx = new();

    /// <inheritdoc />
    public void Dispose() => _fx.Dispose();

    /// <summary>Owner can add a registered enabled workspace as a member.</summary>
    [Fact]
    public async Task HandleAsync_OwnerAddsRegisteredWorkspace_IncludesMember()
    {
        await using var db = _fx.CreateDb();
        await _fx.CreateDefaultProductAsync(db);
        var result = await ProductHandlerTestContext.AddMember(db).HandleAsync(
            new AddProductMemberCommand(ProductHandlerTestContext.Owner, "PROD-MCPSERVER", ProductHandlerTestContext.Member),
            _fx.CallContext);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Contains(ProductHandlerTestContext.Member, result.Value!.MemberWorkspaceIds);
        Assert.Contains(ProductHandlerTestContext.Owner, result.Value.MemberWorkspaceIds);
    }

    /// <summary>Unknown workspace ids are rejected.</summary>
    [Fact]
    public async Task HandleAsync_UnknownWorkspace_Fails()
    {
        await using var db = _fx.CreateDb();
        await _fx.CreateDefaultProductAsync(db);
        var result = await ProductHandlerTestContext.AddMember(db).HandleAsync(
            new AddProductMemberCommand(ProductHandlerTestContext.Owner, "PROD-MCPSERVER", @"F:\does-not-exist"),
            _fx.CallContext);

        Assert.False(result.IsSuccess);
        Assert.Contains("400", result.Error, StringComparison.Ordinal);
    }

    /// <summary>Non-owner cannot add members (403).</summary>
    [Fact]
    public async Task HandleAsync_NonOwner_FailsForbidden()
    {
        await using var db = _fx.CreateDb();
        await _fx.CreateDefaultProductAsync(db);
        await _fx.AddDefaultMemberAsync(db);
        var result = await ProductHandlerTestContext.AddMember(db).HandleAsync(
            new AddProductMemberCommand(ProductHandlerTestContext.Member, "PROD-MCPSERVER", ProductHandlerTestContext.Other),
            _fx.CallContext);

        Assert.False(result.IsSuccess);
        Assert.Contains("403", result.Error, StringComparison.Ordinal);
    }

    /// <summary>Owner can remove a member.</summary>
    [Fact]
    public async Task HandleAsync_OwnerRemovesMember_DropsMember()
    {
        await using var db = _fx.CreateDb();
        await _fx.CreateDefaultProductAsync(db);
        await _fx.AddDefaultMemberAsync(db);
        var result = await ProductHandlerTestContext.RemoveMember(db).HandleAsync(
            new RemoveProductMemberCommand(ProductHandlerTestContext.Owner, "PROD-MCPSERVER", ProductHandlerTestContext.Member),
            _fx.CallContext);

        Assert.True(result.IsSuccess, result.Error);
        Assert.DoesNotContain(ProductHandlerTestContext.Member, result.Value!.MemberWorkspaceIds);
        Assert.Contains(ProductHandlerTestContext.Owner, result.Value.MemberWorkspaceIds);
    }

    /// <summary>A member may leave itself.</summary>
    [Fact]
    public async Task HandleAsync_MemberLeavesSelf_RemovesOnlyCaller()
    {
        await using var db = _fx.CreateDb();
        await _fx.CreateDefaultProductAsync(db);
        await _fx.AddDefaultMemberAsync(db);
        var result = await ProductHandlerTestContext.RemoveMember(db).HandleAsync(
            new RemoveProductMemberCommand(ProductHandlerTestContext.Member, "PROD-MCPSERVER", ProductHandlerTestContext.Member),
            _fx.CallContext);

        Assert.True(result.IsSuccess, result.Error);
        Assert.DoesNotContain(ProductHandlerTestContext.Member, result.Value!.MemberWorkspaceIds);
    }

    /// <summary>A member cannot remove another workspace.</summary>
    [Fact]
    public async Task HandleAsync_MemberRemovesOther_FailsForbidden()
    {
        await using var db = _fx.CreateDb();
        await _fx.CreateDefaultProductAsync(db);
        await _fx.AddDefaultMemberAsync(db);
        var result = await ProductHandlerTestContext.RemoveMember(db).HandleAsync(
            new RemoveProductMemberCommand(ProductHandlerTestContext.Member, "PROD-MCPSERVER", ProductHandlerTestContext.Owner),
            _fx.CallContext);

        Assert.False(result.IsSuccess);
        Assert.Contains("403", result.Error, StringComparison.Ordinal);
    }

    /// <summary>Removed member loses product reads (get is 404).</summary>
    [Fact]
    public async Task HandleAsync_RemovedMember_LosesProductReads()
    {
        await using var db = _fx.CreateDb();
        await _fx.CreateDefaultProductAsync(db);
        await _fx.AddDefaultMemberAsync(db);
        var removed = await ProductHandlerTestContext.RemoveMember(db).HandleAsync(
            new RemoveProductMemberCommand(ProductHandlerTestContext.Owner, "PROD-MCPSERVER", ProductHandlerTestContext.Member),
            _fx.CallContext);
        var result = await ProductHandlerTestContext.Get(db).HandleAsync(
            new GetProductQuery(ProductHandlerTestContext.Member, "PROD-MCPSERVER"),
            _fx.CallContext);

        Assert.True(removed.IsSuccess, removed.Error);
        Assert.False(result.IsSuccess);
        Assert.Contains("404", result.Error, StringComparison.Ordinal);
    }
}
