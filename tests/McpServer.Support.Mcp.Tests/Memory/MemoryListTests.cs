using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Mvc;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-016 / FR-MCP-MEMORY-010: Invalid list scope query returns 400.
/// </summary>
public sealed class MemoryListTests : IDisposable
{
    private readonly MemoryS1Harness _harness = new();

    /// <inheritdoc />
    public void Dispose() => _harness.Dispose();

    /// <summary>AC-FR-MCP-MEMORY-010-47: memory_list with invalid scope query returns 400.</summary>
    [Fact]
    public async Task InvalidScopeQuery_Returns400()
    {
        var controller = MemoryS1Harness.CreateController(
            NSubstitute.Substitute.For<IMemoryService>());
        var action = await controller.ListAsync("1", null, null, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.IsType<BadRequestObjectResult>(action.Result);
        Assert.Equal(400, MemoryS1Harness.StatusOf(action.Result!));
    }
}
