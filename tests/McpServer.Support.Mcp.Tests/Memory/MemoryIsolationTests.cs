using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage.Entities;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-010 / FR-MCP-MEMORY-010: Workspace and product isolation.
/// </summary>
public sealed class MemoryIsolationTests : IDisposable
{
    private readonly MemoryS1Harness _harness = new();

    /// <inheritdoc />
    public void Dispose() => _harness.Dispose();

    /// <summary>AC-FR-MCP-MEMORY-010-25: Workspace-scoped memory is invisible to a different workspace get/list/recall.</summary>
    [Fact]
    public async Task WorkspaceMemory_HiddenFromOtherWorkspace()
    {
        var created = await _harness.AddCompatAsync(new MemoryAddRequest
        {
            Category = "iso",
            Scope = MemoryScope.Workspace,
            Text = "workspace A secret guidance",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(created.Success, created.Error);

        var getB = await _harness.GetCompatAsync(
            created.Memory!.Id,
            _harness.WorkspaceB,
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        var listB = await _harness.ListCompatAsync(
            workspacePath: _harness.WorkspaceB,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Null(getB);
        Assert.DoesNotContain(listB.Items, item => item.Id == created.Memory.Id);
        Assert.DoesNotContain(listB.Items, item => (item.Content ?? item.Text).Contains("workspace A secret", StringComparison.Ordinal));
    }

    /// <summary>AC-FR-MCP-MEMORY-010-46: Product membership does not auto-share memories across member workspaces.</summary>
    [Fact]
    public async Task ProductMembership_DoesNotShareMemories()
    {
        await using (var db = _harness.CreateContext(_harness.WorkspaceA))
        {
            db.Workspaces.Add(new WorkspaceEntity
            {
                WorkspaceId = _harness.WorkspaceA,
                WorkspacePath = _harness.WorkspaceA,
                Name = "A",
            });
            db.Workspaces.Add(new WorkspaceEntity
            {
                WorkspaceId = _harness.WorkspaceB,
                WorkspacePath = _harness.WorkspaceB,
                Name = "B",
            });
            var product = new ProductEntity
            {
                Key = "PROD-S1-ISO",
                Name = "S1 isolation",
                OwnerWorkspaceId = _harness.WorkspaceA,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                UpdatedAtUtc = DateTimeOffset.UtcNow,
            };
            db.Products.Add(product);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
            db.ProductWorkspaceMemberships.Add(new ProductWorkspaceMembershipEntity
            {
                ProductId = product.ProductId,
                WorkspaceId = _harness.WorkspaceA,
                Role = "Owner",
                AddedBy = _harness.WorkspaceA,
                AddedAtUtc = DateTimeOffset.UtcNow,
            });
            db.ProductWorkspaceMemberships.Add(new ProductWorkspaceMembershipEntity
            {
                ProductId = product.ProductId,
                WorkspaceId = _harness.WorkspaceB,
                Role = "Member",
                AddedBy = _harness.WorkspaceA,
                AddedAtUtc = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        }

        var created = await _harness.AddCompatAsync(new MemoryAddRequest
        {
            Category = "product",
            Scope = MemoryScope.Workspace,
            Text = "product-owned workspace A memory",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(created.Success, created.Error);

        var fromB = await _harness.GetCompatAsync(
            created.Memory!.Id,
            _harness.WorkspaceB,
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        var listB = await _harness.ListCompatAsync(
            workspacePath: _harness.WorkspaceB,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Null(fromB);
        Assert.DoesNotContain(listB.Items, item => item.Id == created.Memory.Id);
    }
}
