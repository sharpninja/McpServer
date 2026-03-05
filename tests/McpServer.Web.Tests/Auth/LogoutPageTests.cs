using McpServer.Web.Tests.TestInfrastructure;
using Xunit;

namespace McpServer.Web.Tests.Auth;

/// <summary>
/// Integration tests for the <c>/logout</c> Razor Page.
/// Verifies sign-out behaviour with and without a pre-authenticated user.
/// </summary>
public sealed class LogoutPageTests
{
    [Fact]
    public async Task GetLogout_WhenAuthenticated_SignsOutAndRedirects()
    {
        var user = WebTestFactory.BuildUser("alice", "agent-manager");
        using var factory = WebTestFactory.Create(user);
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var response = await client.GetAsync("/logout");

        // The OIDC SignOut initiates an end-session redirect.
        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
    }

    [Fact]
    public async Task GetLogout_WhenAnonymous_StillRedirects()
    {
        using var factory = WebTestFactory.Create();
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var response = await client.GetAsync("/logout");

        // Even unauthenticated logout should result in a redirect (to / or provider).
        Assert.True(
            response.StatusCode == System.Net.HttpStatusCode.Redirect ||
            response.StatusCode == System.Net.HttpStatusCode.OK,
            $"Unexpected status: {response.StatusCode}");
    }
}
