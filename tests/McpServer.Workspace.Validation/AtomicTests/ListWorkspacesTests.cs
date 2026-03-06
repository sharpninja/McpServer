using System.Net;
using System.Net.Http.Json;
using McpServer.Workspace.Validation.Models;
using Xunit;

namespace McpServer.Workspace.Validation.AtomicTests;

/// <summary>Audit: GET /mcpserver/workspace — List all registered workspaces (public endpoint).</summary>
[Collection("WorkspaceEndpoint")]
public sealed class ListWorkspacesTests
{
    private readonly WorkspaceEndpointFixture _fixture;

    /// <summary>Initializes a new instance.</summary>
    public ListWorkspacesTests(WorkspaceEndpointFixture fixture) => _fixture = fixture;

    /// <summary>Test method.</summary>
    [Fact]
    public async Task List_Returns200_WithValidStructure()
    {
        var response = await _fixture.Client.GetAsync(WorkspaceEndpointFixture.WorkspaceRoute);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<WorkspaceListResult>();
        Assert.NotNull(result);
        Assert.NotNull(result.Items);
        Assert.True(result.TotalCount >= 0, "TotalCount should be non-negative.");
        Assert.Equal(result.Items.Count, result.TotalCount);
    }

    /// <summary>Test method.</summary>
    [Fact]
    public async Task List_ResponseIsJson()
    {
        var response = await _fixture.Client.GetAsync(WorkspaceEndpointFixture.WorkspaceRoute);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }
}
