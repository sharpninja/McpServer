using System.Net;
using System.Net.Http.Json;
using McpServer.Workspace.Validation.Models;
using Xunit;

namespace McpServer.Workspace.Validation.AtomicTests;

/// <summary>Audit: GET /mcpserver/workspace/{key} — Retrieve a workspace by key (public endpoint).</summary>
[Collection("WorkspaceEndpoint")]
public sealed class GetWorkspaceTests : IAsyncLifetime
{
    private readonly WorkspaceEndpointFixture _fixture;
    private readonly string _testPath;
    private readonly string _testKey;

    /// <summary>Initializes a new instance.</summary>
    public GetWorkspaceTests(WorkspaceEndpointFixture fixture)
    {
        _fixture = fixture;
        _testPath = WorkspaceEndpointFixture.GenerateTestWorkspacePath();
        _testKey = WorkspaceEndpointFixture.EncodeKey(_testPath);
    }

    /// <summary>Initializes resources asynchronously.</summary>
    public async ValueTask InitializeAsync()
    {
        var body = new { WorkspacePath = _testPath, Name = "AuditGetTest" };
        var response = await _fixture.Client.PostAsJsonAsync(WorkspaceEndpointFixture.WorkspaceRoute, body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    /// <summary>Disposes resources asynchronously.</summary>
    public async ValueTask DisposeAsync()
    {
        await _fixture.Client.DeleteAsync($"{WorkspaceEndpointFixture.WorkspaceRoute}/{_testKey}");
    }

    /// <summary>Test method.</summary>
    [Fact]
    public async Task Get_ValidKey_Returns200WithWorkspace()
    {
        var response = await _fixture.Client.GetAsync($"{WorkspaceEndpointFixture.WorkspaceRoute}/{_testKey}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var dto = await response.Content.ReadFromJsonAsync<WorkspaceDto>();
        Assert.NotNull(dto);
        Assert.Equal(_testPath, dto.WorkspacePath);
        Assert.Equal("AuditGetTest", dto.Name);
    }

    /// <summary>Test method.</summary>
    [Fact]
    public async Task Get_NonExistentKey_Returns404()
    {
        var fakeKey = WorkspaceEndpointFixture.EncodeKey(@"C:\NonExistent\Path_" + Guid.NewGuid().ToString("N"));
        var response = await _fixture.Client.GetAsync($"{WorkspaceEndpointFixture.WorkspaceRoute}/{fakeKey}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>Test method.</summary>
    [Fact]
    public async Task Get_InvalidBase64Key_Returns400()
    {
        var response = await _fixture.Client.GetAsync($"{WorkspaceEndpointFixture.WorkspaceRoute}/!!!invalid!!!");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
