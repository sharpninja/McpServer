using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Mvc;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-016 / FR-MCP-MEMORY-010: Get unknown and soft-deleted ids.
/// </summary>
public sealed class MemoryGetTests : IDisposable
{
    private readonly MemoryS1Harness _harness = new();

    /// <inheritdoc />
    public void Dispose() => _harness.Dispose();

    /// <summary>AC-FR-MCP-MEMORY-010-28: Get unknown id returns 404.</summary>
    [Fact]
    public async Task UnknownId_Returns404()
    {
        await using var db = _harness.CreateContext(_harness.WorkspaceA);
        var controller = MemoryS1Harness.CreateController(MemoryS1Harness.CreateService(db));
        var action = await controller.GetAsync("MEMORY-MISSING-001", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.IsType<NotFoundObjectResult>(action.Result);
        Assert.Equal(404, MemoryS1Harness.StatusOf(action.Result!));
    }

    /// <summary>AC-FR-MCP-MEMORY-010-29: Get soft-deleted id returns 404 (not the tombstone payload).</summary>
    [Fact]
    public async Task SoftDeleted_Returns404()
    {
        var created = await _harness.AddCompatAsync(new MemoryAddRequest
        {
            Category = "gone",
            Text = "soft delete me",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(created.Success, created.Error);
        await _harness.RemoveCompatAsync(created.Memory!.Id, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        await using var db = _harness.CreateContext(_harness.WorkspaceA);
        var controller = MemoryS1Harness.CreateController(MemoryS1Harness.CreateService(db));
        var action = await controller.GetAsync(created.Memory.Id, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.IsType<NotFoundObjectResult>(action.Result);
        if (action.Result is ObjectResult obj && obj.Value is MemoryItem item)
            Assert.Fail("Soft-deleted get must not return the tombstone payload, found " + item.Id);
    }
}
