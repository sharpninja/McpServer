using McpServer.Web.Tests.TestInfrastructure;
using Xunit;

namespace McpServer.Web.Tests.Auth;

/// <summary>
/// Integration tests for the <c>/access-denied</c> Blazor page.
/// Verifies that the page is accessible without authentication and renders correctly.
/// </summary>
public sealed class AccessDeniedPageTests
{
    [Fact]
    public async Task GetAccessDenied_WithoutAuth_Returns200()
    {
        using var factory = WebTestFactory.Create();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/access-denied");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAccessDenied_ResponseContainsAccessDeniedText()
    {
        using var factory = WebTestFactory.Create();
        var client = factory.CreateClient();

        var content = await client.GetStringAsync("/access-denied");

        Assert.Contains("Access Denied", content, StringComparison.OrdinalIgnoreCase);
    }
}
