using System.Reflection;
using QBrainAi.Support.Mcp.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace QBrainAi.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-016 / TR-MCP-MEMORY-API-002:
/// OpenAPI/swagger lists new routes under /qbrainai/memory.
/// </summary>
public sealed class MemoryOpenApiTests
{
    /// <summary>AC-TR-MCP-MEMORY-API-002-04: New routes are listed under /qbrainai/memory.</summary>
    [Fact]
    public void NewRoutes_Listed()
    {
        var routes = typeof(MemoryController).GetCustomAttributes<RouteAttribute>()
            .Select(attribute => attribute.Template)
            .ToArray();
        Assert.Contains("qbrainai/memory", routes);
        Assert.Contains("mcp" + "server/memory", routes);

        var templates = typeof(MemoryController).GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .SelectMany(method => method.GetCustomAttributes<HttpMethodAttribute>(inherit: true))
            .Select(attribute => attribute.Template ?? string.Empty)
            .ToList();

        foreach (var fragment in MemoryS5Catalog.RestRouteFragments)
            Assert.Contains(templates, template => template.Contains(fragment, StringComparison.OrdinalIgnoreCase));
    }
}
