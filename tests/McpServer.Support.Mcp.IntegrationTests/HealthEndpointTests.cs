using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace McpServer.Support.Mcp.IntegrationTests;

/// <summary>TR-PLANNED-CORE-013: Health and default endpoint tests.</summary>
[Trait("Category", "Integration")]
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
        var response = await client.GetAsync(new Uri("/health", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>GET / returns redirect to Swagger.</summary>
    [Fact]
    public async Task Root_RedirectsToSwagger()
    {
        var options = new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false };
        var client = _factory.CreateClient(options);
        var response = await client.GetAsync(new Uri("/", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("swagger", response.Headers.Location?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>GET /health echoes a caller nonce when one is supplied.</summary>
    [Fact]
    public async Task Health_WithNonce_EchoesNonce()
    {
        var client = _factory.CreateClient();
        const string nonce = "test-nonce-123";

        var response = await client.GetAsync(new Uri($"/health?nonce={nonce}", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(nonce, payload.RootElement.GetProperty("nonce").GetString());
    }
}
