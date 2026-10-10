using System.Reflection;
using McpServer.Support.Mcp.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-016 / TR-MCP-MEMORY-API-002:
/// OpenAPI/swagger lists new routes under /mcpserver/memory.
/// </summary>
public sealed class MemoryOpenApiTests
{
    /// <summary>AC-TR-MCP-MEMORY-API-002-04: New routes are listed under /mcpserver/memory.</summary>
    [Fact]
    public void NewRoutes_Listed()
    {
        var route = typeof(MemoryController).GetCustomAttribute<RouteAttribute>();
        Assert.NotNull(route);
        Assert.Equal("mcpserver/memory", route!.Template);

        var templates = typeof(MemoryController).GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .SelectMany(method => method.GetCustomAttributes<HttpMethodAttribute>(inherit: true))
            .Select(attribute => attribute.Template ?? string.Empty)
            .ToList();

        foreach (var fragment in MemoryS5Catalog.RestRouteFragments)
            Assert.Contains(templates, template => template.Contains(fragment, StringComparison.OrdinalIgnoreCase));
    }
}
