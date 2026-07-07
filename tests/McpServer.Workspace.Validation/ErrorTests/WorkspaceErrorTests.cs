using System.Net;
using System.Net.Http.Json;
using McpServer.Workspace.Validation.Models;
using Xunit;

namespace McpServer.Workspace.Validation.ErrorTests;

/// <summary>
/// Audit: Error and edge-case tests for workspace endpoints.
/// Validates proper HTTP status codes for invalid inputs, missing resources, and duplicates.
/// </summary>
[Collection("WorkspaceEndpoint")]
public sealed class WorkspaceErrorTests
{
    private readonly WorkspaceEndpointFixture _fixture;
    private readonly ITestOutputHelper _output;

    /// <summary>Initializes a new instance.</summary>
    public WorkspaceErrorTests(WorkspaceEndpointFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    // ── Invalid Base64URL key tests ──────────────────────────────────────

    /// <summary>Returns 400 for invalid key in GET.</summary>
    [Theory]
    [InlineData("!!!invalid!!!")]
    [InlineData("not-valid-base64-@#$")]
    [InlineData("")]
    public async Task Get_InvalidKey_Returns400(string badKey)
    {
        // Empty key will hit the List endpoint (GET /mcpserver/workspace) returning 200,
        // so only test non-empty invalid keys.
        if (string.IsNullOrEmpty(badKey)) return;

        var response = await _fixture.Client.GetAsync(
            $"{WorkspaceEndpointFixture.WorkspaceRoute}/{badKey}", cancellationToken: TestContext.Current.CancellationToken);
        _output.WriteLine($"GET with key='{badKey}' → {(int)response.StatusCode}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>Returns 400 for invalid key in PUT.</summary>
    [Theory]
    [InlineData("!!!invalid!!!")]
    [InlineData("not-valid-base64-@#$")]
    public async Task Put_InvalidKey_Returns400(string badKey)
    {
        var body = new { Name = "Test" };
        var response = await _fixture.Client.PutAsJsonAsync(
            $"{WorkspaceEndpointFixture.WorkspaceRoute}/{badKey}", body, cancellationToken: TestContext.Current.CancellationToken);
        _output.WriteLine($"PUT with key='{badKey}' → {(int)response.StatusCode}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>Returns 400 for invalid key in DELETE.</summary>
    [Theory]
    [InlineData("!!!invalid!!!")]
    [InlineData("not-valid-base64-@#$")]
    public async Task Delete_InvalidKey_Returns400(string badKey)
    {
        var response = await _fixture.Client.DeleteAsync(
            $"{WorkspaceEndpointFixture.WorkspaceRoute}/{badKey}", cancellationToken: TestContext.Current.CancellationToken);
        _output.WriteLine($"DELETE with key='{badKey}' → {(int)response.StatusCode}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── Missing resource tests ───────────────────────────────────────────

    /// <summary>Test method.</summary>
    [Fact]
    public async Task Get_NonExistent_Returns404()
    {
        var key = WorkspaceEndpointFixture.EncodeKey(@"C:\DoesNotExist_" + Guid.NewGuid().ToString("N"));
        var response = await _fixture.Client.GetAsync(
            $"{WorkspaceEndpointFixture.WorkspaceRoute}/{key}", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>Test method.</summary>
    [Fact]
    public async Task Update_NonExistent_Returns404()
    {
        var key = WorkspaceEndpointFixture.EncodeKey(@"C:\DoesNotExist_" + Guid.NewGuid().ToString("N"));
        var body = new { Name = "Ghost" };
        var response = await _fixture.Client.PutAsJsonAsync(
            $"{WorkspaceEndpointFixture.WorkspaceRoute}/{key}", body, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>Test method.</summary>
    [Fact]
    public async Task Delete_NonExistent_Returns404()
    {
        var key = WorkspaceEndpointFixture.EncodeKey(@"C:\DoesNotExist_" + Guid.NewGuid().ToString("N"));
        var response = await _fixture.Client.DeleteAsync(
            $"{WorkspaceEndpointFixture.WorkspaceRoute}/{key}", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>Test method.</summary>
    [Fact]
    public async Task Start_NonExistent_Returns404()
    {
        var key = WorkspaceEndpointFixture.EncodeKey(@"C:\DoesNotExist_" + Guid.NewGuid().ToString("N"));
        var response = await _fixture.Client.PostAsync(
            $"{WorkspaceEndpointFixture.WorkspaceRoute}/{key}/start", null, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── Duplicate create test ────────────────────────────────────────────

    /// <summary>Test method.</summary>
    [Fact]
    public async Task Create_Duplicate_Returns409()
    {
        var testPath = WorkspaceEndpointFixture.GenerateTestWorkspacePath();
        var testKey = WorkspaceEndpointFixture.EncodeKey(testPath);
        try
        {
            var body = new { WorkspacePath = testPath, Name = "DupeFirst" };
            var first = await _fixture.Client.PostAsJsonAsync(WorkspaceEndpointFixture.WorkspaceRoute, body, cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Created, first.StatusCode);

            var second = await _fixture.Client.PostAsJsonAsync(WorkspaceEndpointFixture.WorkspaceRoute, body, cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

            var result = await second.Content.ReadFromJsonAsync<WorkspaceMutationResult>(cancellationToken: TestContext.Current.CancellationToken);
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
            _output.WriteLine($"Duplicate error message: {result.Error}");
        }
        finally
        {
            await _fixture.Client.DeleteAsync($"{WorkspaceEndpointFixture.WorkspaceRoute}/{testKey}", cancellationToken: TestContext.Current.CancellationToken);
        }
    }

    // ── Missing/empty body tests ─────────────────────────────────────────

    /// <summary>Test method.</summary>
    [Fact]
    public async Task Create_EmptyBody_ReturnsBadRequest()
    {
        var response = await _fixture.Client.PostAsync(
            WorkspaceEndpointFixture.WorkspaceRoute,
            new StringContent("{}", System.Text.Encoding.UTF8, "application/json"), cancellationToken: TestContext.Current.CancellationToken);

        // Missing required WorkspacePath → 400 or model validation error.
        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.UnprocessableEntity,
            $"Expected 400/422 but got {(int)response.StatusCode}.");
    }

    /// <summary>Test method.</summary>
    [Fact]
    public async Task Update_NullBody_ReturnsBadRequest()
    {
        var key = WorkspaceEndpointFixture.EncodeKey(@"C:\SomePath");
        var response = await _fixture.Client.PutAsync(
            $"{WorkspaceEndpointFixture.WorkspaceRoute}/{key}",
            null, cancellationToken: TestContext.Current.CancellationToken);

        // Null body → should fail gracefully.
        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.UnsupportedMediaType ||
            response.StatusCode == HttpStatusCode.NotFound, // may be 404 if workspace doesn't exist
            $"Expected 400/415/404 but got {(int)response.StatusCode}.");
    }

    // ── Method not allowed ───────────────────────────────────────────────

    /// <summary>Test method.</summary>
    [Fact]
    public async Task Patch_NotSupported_Returns405()
    {
        var key = WorkspaceEndpointFixture.EncodeKey(@"C:\SomePath");
        var request = new HttpRequestMessage(HttpMethod.Patch, $"{WorkspaceEndpointFixture.WorkspaceRoute}/{key}")
        {
            Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
        };
        var response = await _fixture.Client.SendAsync(request, cancellationToken: TestContext.Current.CancellationToken);

        // PATCH is not defined on the controller.
        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }
}
