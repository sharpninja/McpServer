using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace McpServer.Support.Mcp.IntegrationTests;

/// <summary>TR-PLANNED-013: Health and default endpoint tests.</summary>
public sealed class HealthEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public HealthEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>GET /health returns 200.</summary>
    [Fact]
    public async Task Health_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync(new Uri("/health", UriKind.Relative)).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>GET / returns redirect to Swagger.</summary>
    [Fact]
    public async Task Root_RedirectsToSwagger()
    {
        var options = new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false };
        var client = _factory.CreateClient(options);
        var response = await client.GetAsync(new Uri("/", UriKind.Relative)).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("swagger", response.Headers.Location?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}
