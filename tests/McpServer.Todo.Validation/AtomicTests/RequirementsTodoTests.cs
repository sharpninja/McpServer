using System.Net;
using System.Net.Http.Json;
using McpServer.Todo.Validation.Models;
using Xunit;

namespace McpServer.Todo.Validation.AtomicTests;

/// <summary>Audit: POST /mcpserver/todo/{id}/requirements — Analyze requirements via Copilot.</summary>
[Collection("TodoEndpoint")]
public sealed class RequirementsTodoTests : IAsyncLifetime
{
    private readonly TodoEndpointFixture _fixture;
    private readonly string _testId;

    public RequirementsTodoTests(TodoEndpointFixture fixture)
    {
        _fixture = fixture;
        _testId = TodoEndpointFixture.GenerateTestId();
    }

    public async ValueTask InitializeAsync()
    {
        var body = new { Id = _testId, Title = "AuditRequirementsTest", Section = "mvp-support", Priority = "medium" };
        var response = await _fixture.Client.PostAsJsonAsync(TodoEndpointFixture.TodoRoute, body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    public async ValueTask DisposeAsync()
    {
        await _fixture.Client.DeleteAsync($"{TodoEndpointFixture.TodoRoute}/{Uri.EscapeDataString(_testId)}");
    }

    [Fact]
    public async Task Requirements_RegisteredItem_ReturnsResult()
    {
        var response = await _fixture.Client.PostAsync(
            $"{TodoEndpointFixture.TodoRoute}/{Uri.EscapeDataString(_testId)}/requirements", null);

        // Copilot may or may not be available; accept 200 (success) or 422 (failure).
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.UnprocessableEntity,
            $"Expected 200 or 422 but got {(int)response.StatusCode}.");

        var result = await response.Content.ReadFromJsonAsync<RequirementsAnalysisResult>();
        Assert.NotNull(result);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            Assert.True(result.Success);
        }
        else
        {
            Assert.False(result.Success);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
        }
    }

    [Fact]
    public async Task Requirements_NonExistentItem_Returns422()
    {
        var fakeId = $"NONEXISTENT-{Guid.NewGuid():N}";
        var response = await _fixture.Client.PostAsync(
            $"{TodoEndpointFixture.TodoRoute}/{Uri.EscapeDataString(fakeId)}/requirements", null);

        // Should fail gracefully — either 404 or 422 depending on whether
        // the service checks existence first or the Copilot call fails.
        Assert.True(
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.UnprocessableEntity,
            $"Expected 404 or 422 but got {(int)response.StatusCode}.");
    }
}
