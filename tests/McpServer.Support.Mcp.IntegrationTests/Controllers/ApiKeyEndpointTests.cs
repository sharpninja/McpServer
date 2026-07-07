using System.Net;
using Xunit;

namespace McpServer.Support.Mcp.IntegrationTests.Controllers;

/// <summary>TR-PLANNED-CORE-013: Integration tests for the default API-key issuance endpoint.</summary>
[Trait("Category", "Integration")]
public sealed class ApiKeyEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ApiKeyEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>GET /api-key returns 429 after the fixed window issuance limit is exhausted.</summary>
    [Fact]
    public async Task GetApiKey_AfterPermitLimit_ReturnsTooManyRequests()
    {
        using var client = _factory.CreateClient();

        for (var i = 0; i < 30; i++)
        {
            using var response = await client.GetAsync(new Uri("/api-key", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        using var throttled = await client.GetAsync(new Uri("/api-key", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.TooManyRequests, throttled.StatusCode);
        Assert.True(throttled.Headers.TryGetValues("Retry-After", out var values));
        Assert.NotEmpty(values);
    }
}
