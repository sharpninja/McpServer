using McpServer.Support.Mcp.Notifications;
using McpServer.Support.Mcp.Services;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-016 / TR-MCP-MEMORY-API-002:
/// SSE subscription for memory.* does not leak events across workspaces.
/// </summary>
public sealed class MemorySseTests
{
    /// <summary>AC-TR-MCP-MEMORY-API-002-08: memory SSE events stay inside the caller workspace.</summary>
    [Fact]
    public void NoCrossWorkspaceEvents()
    {
        var filterType = typeof(MemoryLimits).Assembly.GetTypes()
            .FirstOrDefault(type => type.Name == "MemorySseWorkspaceFilter");
        Assert.NotNull(filterType);
        var allow = filterType!.GetMethod("Allow", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        Assert.NotNull(allow);

        var workspaceProperty = typeof(ChangeEvent).GetProperty("WorkspacePath");
        Assert.NotNull(workspaceProperty);

        var local = new ChangeEvent { Category = "memory", Action = "consolidated", EntityId = "run-1" };
        var foreign = new ChangeEvent { Category = "memory", Action = "consolidated", EntityId = "run-2" };
        var connection = new ChangeEvent
        {
            Category = ChangeEventCategories.Connection,
            Action = ChangeEventActions.Connected,
        };
        workspaceProperty!.SetValue(local, @"C:\ws-a");
        workspaceProperty.SetValue(foreign, @"C:\ws-b");

        Assert.True((bool)allow!.Invoke(null, [local, @"C:\ws-a"])!);
        Assert.False((bool)allow.Invoke(null, [foreign, @"C:\ws-a"])!);
        Assert.True((bool)allow.Invoke(null, [connection, @"C:\ws-a"])!);
    }
}
