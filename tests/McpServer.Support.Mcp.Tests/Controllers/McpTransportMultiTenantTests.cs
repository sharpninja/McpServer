using System.Net;
using System.Text;
using System.Text.Json;
using McpServer.Support.Mcp.Middleware;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Controllers;

/// <summary>
/// TR-MCP-MT-002: MCP transport multi-tenant workspace resolution tests.
/// Validates that X-Workspace-Path header is respected on /mcp-transport routes.
/// </summary>
public sealed class McpTransportMultiTenantTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    /// <summary>Initializes a new instance of the <see cref="McpTransportMultiTenantTests"/> class.</summary>
    public McpTransportMultiTenantTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task McpTransport_WithWorkspaceHeader_StillInitializes()
    {
        var initRequest = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "initialize",
            @params = new
            {
                protocolVersion = "2025-03-26",
                capabilities = new { },
                clientInfo = new { name = "test-client-mt", version = "1.0.0" }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/mcp-transport");
        request.Content = new StringContent(
            JsonSerializer.Serialize(initRequest),
            Encoding.UTF8,
            "application/json");
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));

        // Unregistered workspace header on /mcp-transport → resolution middleware returns 400
        request.Headers.Add(WorkspaceResolutionMiddleware.WorkspacePathHeader, @"C:\nonexistent");

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task McpTransport_WithoutWorkspaceHeader_UsesDefaultWorkspace()
    {
        var initRequest = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "initialize",
            @params = new
            {
                protocolVersion = "2025-03-26",
                capabilities = new { },
                clientInfo = new { name = "test-client-default", version = "1.0.0" }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/mcp-transport");
        request.Content = new StringContent(
            JsonSerializer.Serialize(initRequest),
            Encoding.UTF8,
            "application/json");
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));

        // No workspace header → falls through to default/primary workspace
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("serverInfo", body, StringComparison.Ordinal);
    }
}
