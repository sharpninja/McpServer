using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace McpServer.Support.Mcp.IntegrationTests.Controllers;

/// <summary>
/// TEST-MCP-091: Validates that the admin configuration endpoints stay unavailable when OIDC is not
/// configured, even if callers attempt Bearer-token authentication.
/// The test uses the real HTTP pipeline so workspace auth and ASP.NET authorization behave exactly as they
/// do in the running server.
/// </summary>
public sealed class ConfigurationAuthorizationPolicyTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="ConfigurationAuthorizationPolicyTests"/> class.</summary>
    /// <param name="factory">The shared integration-test application factory.</param>
    public ConfigurationAuthorizationPolicyTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// TEST-MCP-091: Verifies that a Bearer-authenticated request to the configuration endpoint returns
    /// unauthorized when OIDC is disabled.
    /// The test deliberately sends a bearer token without an API key because the middleware must reject the
    /// JWT path before any workspace-token fallback can permit access.
    /// </summary>
    [Fact]
    public async Task ConfigurationEndpoint_WhenOidcDisabledAndBearerProvided_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            JwtBearerDefaults.AuthenticationScheme,
            "fake-admin-token");

        using var response = await client.GetAsync("/mcpserver/configuration")
            .ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
