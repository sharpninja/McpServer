using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Mvc;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-016 / FR-MCP-MEMORY-010: Update unknown id.
/// </summary>
public sealed class MemoryUpdateTests : IDisposable
{
    private readonly MemoryS1Harness _harness = new();

    /// <inheritdoc />
    public void Dispose() => _harness.Dispose();

    /// <summary>AC-FR-MCP-MEMORY-010-30: Update unknown id returns 404.</summary>
    [Fact]
    public async Task UnknownId_Returns404()
    {
        await using var db = _harness.CreateContext(_harness.WorkspaceA);
        var controller = MemoryS1Harness.CreateController(MemoryS1Harness.CreateService(db));
        var action = await controller.UpdateAsync(
            "MEMORY-MISSING-001",
            new MemoryUpdateRequest { Text = "nope" },
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.IsType<NotFoundObjectResult>(action.Result);
        Assert.Equal(404, MemoryS1Harness.StatusOf(action.Result!));
    }
}
