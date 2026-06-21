using System.Net;
using McpServer.Support.Mcp.Middleware;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace McpServer.Support.Mcp.IntegrationTests.Controllers;

/// <summary>
/// TR-MCP-MT-001, FR-MCP-043, FR-MCP-044: Integration tests for multi-tenant workspace resolution.
/// Validates the full HTTP pipeline: WorkspaceResolutionMiddleware → WorkspaceAuthMiddleware → Controller.
/// </summary>
public sealed class MultiTenantIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    /// <summary>Initializes test with the shared factory.</summary>
    public MultiTenantIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UnregisteredWorkspacePath_Returns400()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(WorkspaceResolutionMiddleware.WorkspacePathHeader, @"C:\nonexistent\workspace");

        var response = await client.GetAsync(new Uri("/mcpserver/todo", UriKind.Relative));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task InvalidApiKey_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(WorkspaceResolutionMiddleware.WorkspacePathHeader, _factory.WorkspacePath);
        client.DefaultRequestHeaders.Add("X-Api-Key", "invalid-token-value");

        var response = await client.GetAsync(new Uri("/mcpserver/todo", UriKind.Relative));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task NonMcpRoute_SkipsWorkspaceResolution()
    {
        var client = _factory.CreateClient();

        // Health endpoint should work without any auth or workspace headers
        var response = await client.GetAsync(new Uri("/health", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task WorkspacePathHeader_UnregisteredPath_TakesPriorityOverApiKey()
    {
        using var scope = _factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<WorkspaceTokenService>();

        var pathA = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "ws-priority-a"));
        tokenService.GenerateToken(pathA);
        var token = tokenService.GetToken(pathA)!;

        var client = _factory.CreateClient();
        // X-Workspace-Path of unregistered path should return 400 even with valid token
        client.DefaultRequestHeaders.Add(WorkspaceResolutionMiddleware.WorkspacePathHeader, @"C:\not\registered");
        client.DefaultRequestHeaders.Add("X-Api-Key", token);

        var response = await client.GetAsync(new Uri("/mcpserver/todo", UriKind.Relative));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task WorkspacePathHeaderWithoutApiKey_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(WorkspaceResolutionMiddleware.WorkspacePathHeader, _factory.WorkspacePath);

        var response = await client.GetAsync(new Uri("/mcpserver/todo", UriKind.Relative));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task BearerWithoutWorkspaceHeader_OnTenantRoute_Returns404()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "synthetic-jwt");

        var response = await client.GetAsync(new Uri("/mcpserver/sessionlog/query", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task WorkspaceResolutionMiddleware_OnlyRunsForMcpRoutes()
    {
        var client = _factory.CreateClient();
        // Send an X-Workspace-Path header with unregistered path on a non-mcp route
        client.DefaultRequestHeaders.Add(WorkspaceResolutionMiddleware.WorkspacePathHeader, @"C:\nonexistent");

        // Non-mcp route should not trigger workspace validation
        var response = await client.GetAsync(new Uri("/health", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
