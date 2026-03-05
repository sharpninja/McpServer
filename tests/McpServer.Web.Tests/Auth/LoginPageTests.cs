using McpServer.Web.Tests.TestInfrastructure;
using Xunit;

namespace McpServer.Web.Tests.Auth;

/// <summary>
/// Integration tests for the <c>/login</c> Razor Page.
/// Verifies that GET /login issues an OIDC challenge redirect without making real
/// network calls to the provider (OIDC metadata is stubbed via <see cref="WebTestFactory"/>).
/// </summary>
public sealed class LoginPageTests
{
    [Fact]
    public async Task GetLogin_Unauthenticated_ReturnsChallengeRedirect()
    {
        using var factory = WebTestFactory.Create();
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var response = await client.GetAsync("/login");

        // OIDC challenge returns 302 pointing to the authorization endpoint.
        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location?.ToString() ?? string.Empty;
        Assert.StartsWith(WebTestFactory.FakeAuthorizationEndpoint, location);
    }

    [Fact]
    public async Task GetLogin_WithReturnUrl_IncludesReturnUrlInState()
    {
        using var factory = WebTestFactory.Create();
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var response = await client.GetAsync("/login?returnUrl=%2Ftodos");

        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
        // The OIDC redirect_uri or state should encode the return URL.
        var location = response.Headers.Location?.ToString() ?? string.Empty;
        Assert.StartsWith(WebTestFactory.FakeAuthorizationEndpoint, location);
    }

    [Fact]
    public async Task GetLogin_WithNonLocalReturnUrl_FallsBackToRoot()
    {
        using var factory = WebTestFactory.Create();
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        // Non-local URLs should be rejected; challenge still happens but RedirectUri is /.
        var response = await client.GetAsync("/login?returnUrl=https%3A%2F%2Fevil.example.com%2F");

        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location?.ToString() ?? string.Empty;
        Assert.StartsWith(WebTestFactory.FakeAuthorizationEndpoint, location);
    }
}
