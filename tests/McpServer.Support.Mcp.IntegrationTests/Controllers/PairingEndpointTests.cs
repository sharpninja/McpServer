using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace McpServer.Support.Mcp.IntegrationTests.Controllers;

/// <summary>Integration tests for the /pair web login flow.</summary>
public sealed class PairingEndpointTests : IClassFixture<PairingWebApplicationFactory>
{
    private readonly HttpClient _client;

    /// <summary>Initializes a new instance of the <see cref="PairingEndpointTests"/> class.</summary>
    public PairingEndpointTests(PairingWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
    }

    [Fact]
    public async Task PairGet_WhenConfigured_ReturnsLoginPage()
    {
        var response = await _client.GetAsync("/pair").ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
        Assert.Contains("Sign In", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PairPost_WithBadCredentials_ShowsError()
    {
        var form = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("username", "admin"),
            new KeyValuePair<string, string>("password", "wrong"),
        ]);

        var response = await _client.PostAsync("/pair", form).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
        Assert.Contains("Invalid username or password", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PairPost_AfterRepeatedFailures_ReturnsTooManyRequests()
    {
        await using var factory = new PairingWebApplicationFactory();
        using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        for (var i = 0; i < 5; i++)
        {
            using var failedAttempt = new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("username", "admin"),
                new KeyValuePair<string, string>("password", "wrong"),
            ]);

            var failedResponse = await client.PostAsync("/pair", failedAttempt).ConfigureAwait(true);
            Assert.Equal(HttpStatusCode.OK, failedResponse.StatusCode);
        }

        using var lockedAttempt = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("username", "admin"),
            new KeyValuePair<string, string>("password", "testpass"),
        ]);

        var response = await client.PostAsync("/pair", lockedAttempt).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Retry-After", out var retryAfterValues));
        Assert.NotEmpty(retryAfterValues);

        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
        Assert.Contains("Too many failed sign-in attempts", body, StringComparison.Ordinal);
        Assert.False(response.Headers.TryGetValues("Set-Cookie", out _));
    }

    [Fact]
    public async Task PairPost_WithGoodCredentials_RedirectsToKey()
    {
        var form = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("username", "admin"),
            new KeyValuePair<string, string>("password", "testpass"),
        ]);

        var response = await _client.PostAsync("/pair", form).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/pair/key", response.Headers.Location?.OriginalString);
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        Assert.Contains(cookies, c => c.Contains("mcp_pair=", StringComparison.Ordinal));
    }

    [Fact]
    public async Task PairKey_WithoutCookie_RedirectsToLogin()
    {
        var response = await _client.GetAsync("/pair/key").ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/pair", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task PairKey_WithValidSession_ShowsApiKey()
    {
        // First, authenticate to get the session cookie.
        var form = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("username", "admin"),
            new KeyValuePair<string, string>("password", "testpass"),
        ]);

        var loginResponse = await _client.PostAsync("/pair", form).ConfigureAwait(true);
        var setCookie = loginResponse.Headers.GetValues("Set-Cookie").First();

        // Extract cookie value and send with next request.
        using var request = new HttpRequestMessage(HttpMethod.Get, "/pair/key");
        request.Headers.Add("Cookie", setCookie.Split(';')[0]);

        var response = await _client.SendAsync(request).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
        Assert.Contains("test-api-key-12345", body, StringComparison.Ordinal);
        Assert.Contains("X-Api-Key", body, StringComparison.Ordinal);
    }
}

/// <summary>Tests that /pair returns "not configured" when no PairingUsers are set.</summary>
public sealed class PairingNotConfiguredTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    /// <summary>Initializes a new instance of the <see cref="PairingNotConfiguredTests"/> class.</summary>
    public PairingNotConfiguredTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PairGet_WhenNotConfigured_ShowsNotConfiguredPage()
    {
        var response = await _client.GetAsync("/pair").ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
        Assert.Contains("Pairing Not Configured", body, StringComparison.Ordinal);
    }
}

/// <summary>Factory that configures PairingUsers and ApiKey for pairing tests.</summary>
public sealed class PairingWebApplicationFactory : WebApplicationFactory<McpApiEntryPoint>
{
    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        builder.UseContentRoot(CustomWebApplicationFactory.ResolveContentRoot());
        builder.ConfigureAppConfiguration(config =>
        {
            // SHA-256 of "testpass"
            var hash = System.Convert.ToHexStringLower(
                System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes("testpass")));

            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Mcp:DataSource", ":memory:" },
                { "Mcp:ApiKey", "test-api-key-12345" },
                { "Mcp:PairingUsers:0:Username", "admin" },
                { "Mcp:PairingUsers:0:PasswordHash", hash },
            });
        });
    }
}
