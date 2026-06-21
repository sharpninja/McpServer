using System.Net;
using System.Text.Json;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace McpServer.Support.Mcp.IntegrationTests;

/// <summary>TEST-MCP-HEALTH-003: agent-flow auth semantics and <c>/ready</c> readiness coverage.</summary>
public sealed class ReadinessAndAuthIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="ReadinessAndAuthIntegrationTests"/> class.</summary>
    /// <param name="factory">Integration test application factory.</param>
    public ReadinessAndAuthIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>Pure X-Api-Key agent flow without X-Workspace-Path succeeds with the current full token.</summary>
    [Fact]
    public async Task Todo_ValidToken_NoWorkspaceHeader_Returns200()
    {
        using var scope = _factory.Services.CreateScope();
        var tokens = scope.ServiceProvider.GetRequiredService<WorkspaceTokenService>();
        var token = tokens.GetToken(_factory.WorkspacePath)!;

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", token);

        var response = await client.GetAsync(new Uri("/mcpserver/todo", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>Unknown API key on the agent flow returns 401, not blanket 503.</summary>
    [Fact]
    public async Task Todo_UnknownKey_NoWorkspaceHeader_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "stale-or-wrong-key");

        var response = await client.GetAsync(new Uri("/mcpserver/todo", UriKind.Relative));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>Missing API key on the agent flow returns 401 after token initialization.</summary>
    [Fact]
    public async Task Todo_MissingKey_NoWorkspaceHeader_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/mcpserver/todo", UriKind.Relative));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary><c>/ready</c> is healthy and lists the workspace readiness check when the data layer is up.</summary>
    [Fact]
    public async Task Ready_WhenUp_Healthy_IncludesWorkspaceReadinessCheck()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/ready", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var payload = await JsonDocument.ParseAsync(stream);
        Assert.Equal("Healthy", payload.RootElement.GetProperty("status").GetString());

        var workspaceReady = payload.RootElement.GetProperty("checks")
            .EnumerateArray()
            .FirstOrDefault(check => string.Equals(
                check.GetProperty("name").GetString(),
                "workspace-ready",
                StringComparison.OrdinalIgnoreCase));
        Assert.Equal(JsonValueKind.Object, workspaceReady.ValueKind);
        Assert.Equal("Healthy", workspaceReady.GetProperty("status").GetString());
    }
}
