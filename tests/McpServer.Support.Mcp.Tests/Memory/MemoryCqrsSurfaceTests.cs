using McpServer.Cqrs;
using McpServer.Support.Mcp.Controllers;
using McpServer.Support.Mcp.McpStdio;
using McpServer.Support.Mcp.Services;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-016 / TR-MCP-MEMORY-API-002:
/// Controllers, MCP, REPL, plugins, and MemoryClient do not add a parallel domain service API.
/// </summary>
public sealed class MemoryCqrsSurfaceTests
{
    /// <summary>AC-TR-MCP-MEMORY-API-002-01: Adapters dispatch CQRS handlers only.</summary>
    [Fact]
    public void Adapters_DispatchHandlersOnly()
    {
        var serviceMethods = typeof(IMemoryService).GetMethods().Select(method => method.Name).ToHashSet(StringComparer.Ordinal);
        foreach (var forbidden in new[] { "RememberAsync", "RecallAsync", "ExploreAsync", "ConsolidateAsync", "PromoteAsync", "RevertAsync" })
            Assert.DoesNotContain(forbidden, serviceMethods);

        var controllerCtor = typeof(MemoryController).GetConstructors().Single();
        Assert.Contains(controllerCtor.GetParameters(), parameter => parameter.ParameterType == typeof(IDispatcher));

        var toolsCtor = typeof(FwhMcpTools).GetConstructors().Single();
        Assert.Contains(toolsCtor.GetParameters(), parameter => parameter.ParameterType == typeof(IDispatcher));

        var controllerMethods = typeof(MemoryController).GetMethods()
            .Select(method => method.Name)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("RememberAsync", controllerMethods);
        Assert.Contains("RecallAsync", controllerMethods);
        Assert.Contains("ExploreAsync", controllerMethods);
        Assert.Contains("ConsolidateAsync", controllerMethods);
        Assert.Contains("PromoteAsync", controllerMethods);
        Assert.Contains("RevertAsync", controllerMethods);
        Assert.Contains("ListVersionsAsync", controllerMethods);
    }
}
