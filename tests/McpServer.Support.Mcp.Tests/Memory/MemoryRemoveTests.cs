using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Mvc;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-016 / FR-MCP-MEMORY-010: Remove unknown id and second-remove stability.
/// </summary>
public sealed class MemoryRemoveTests : IDisposable
{
    private readonly MemoryS1Harness _harness = new();

    /// <inheritdoc />
    public void Dispose() => _harness.Dispose();

    /// <summary>AC-FR-MCP-MEMORY-010-31: Remove unknown id returns 404.</summary>
    [Fact]
    public async Task UnknownId_Returns404()
    {
        await using var db = _harness.CreateContext(_harness.WorkspaceA);
        var controller = MemoryS1Harness.CreateController(MemoryS1Harness.CreateService(db));
        var action = await controller.RemoveAsync("MEMORY-MISSING-001", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.IsType<NotFoundObjectResult>(action.Result);
        Assert.Equal(404, MemoryS1Harness.StatusOf(action.Result!));
    }

    /// <summary>AC-FR-MCP-MEMORY-010-32: Second remove of an already soft-deleted id is 404 or a documented no-op.</summary>
    [Fact]
    public async Task SecondRemove_StableBehavior()
    {
        var created = await _harness.AddCompatAsync(new MemoryAddRequest
        {
            Category = "rm",
            Text = "remove twice",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(created.Success, created.Error);

        var first = await _harness.RemoveCompatAsync(created.Memory!.Id, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        var second = await _harness.RemoveCompatAsync(created.Memory.Id, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.True(first.Success, first.Error);
        Assert.True(
            second.FailureKind == MemoryMutationFailureKind.NotFound || second.Success,
            second.Error ?? "second remove must be 404 or documented no-op success");
        var again = await _harness.RemoveCompatAsync(created.Memory.Id, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.Equal(second.Success, again.Success);
        Assert.Equal(second.FailureKind, again.FailureKind);
    }
}
