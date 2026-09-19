using System.Text.Json;
using McpServer.Support.Mcp.Controllers;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-016 / TR-MCP-MEMORY-API-002:
/// New memory endpoints use the existing type/title/status/detail envelope.
/// </summary>
public sealed class MemoryErrorEnvelopeTests
{
    /// <summary>AC-TR-MCP-MEMORY-API-002-02: Error responses use type/title/status/detail.</summary>
    [Fact]
    public async Task UsesStandardEnvelope()
    {
        var method = typeof(MemoryController).GetMethod("RememberAsync");
        Assert.NotNull(method);
        var service = Substitute.For<IMemoryService>();
        var controller = new MemoryController(service);
        var invoked = method!.Invoke(controller, [null, TestContext.Current.CancellationToken]);
        Assert.NotNull(invoked);
        var action = await ((Task<ActionResult<MemoryRememberResult>>)invoked!).ConfigureAwait(true);
        var objectResult = Assert.IsAssignableFrom<ObjectResult>(action.Result);
        Assert.Equal(400, objectResult.StatusCode ?? (objectResult as BadRequestObjectResult is not null ? 400 : objectResult.StatusCode));
        var json = JsonSerializer.Serialize(objectResult.Value);
        using var document = JsonDocument.Parse(json);
        Assert.True(document.RootElement.TryGetProperty("type", out var type));
        Assert.True(document.RootElement.TryGetProperty("title", out var title));
        Assert.True(document.RootElement.TryGetProperty("status", out var status));
        Assert.True(document.RootElement.TryGetProperty("detail", out var detail));
        Assert.False(string.IsNullOrWhiteSpace(type.GetString()));
        Assert.False(string.IsNullOrWhiteSpace(title.GetString()));
        Assert.Equal(400, status.GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(detail.GetString()));
    }
}
