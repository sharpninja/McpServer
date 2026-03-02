using System.Net;
using System.Net.Http.Json;
using McpServer.Todo.Validation.Models;
using Xunit;

namespace McpServer.Todo.Validation.AtomicTests;

/// <summary>Audit: GET /mcpserver/todo/{id} — Get a single TODO item by id.</summary>
[Collection("TodoEndpoint")]
public sealed class GetTodoTests : IAsyncLifetime
{
    private readonly TodoEndpointFixture _fixture;
    private readonly string _testId;

    public GetTodoTests(TodoEndpointFixture fixture)
    {
        _fixture = fixture;
        _testId = TodoEndpointFixture.GenerateTestId();
    }

    public async ValueTask InitializeAsync()
    {
        var body = new { Id = _testId, Title = "AuditGetTest", Section = "mvp-support", Priority = "medium" };
        var response = await _fixture.Client.PostAsJsonAsync(TodoEndpointFixture.TodoRoute, body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    public async ValueTask DisposeAsync()
    {
        await _fixture.Client.DeleteAsync($"{TodoEndpointFixture.TodoRoute}/{Uri.EscapeDataString(_testId)}");
    }

    [Fact]
    public async Task Get_ValidId_Returns200WithItem()
    {
        var response = await _fixture.Client.GetAsync(
            $"{TodoEndpointFixture.TodoRoute}/{Uri.EscapeDataString(_testId)}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var item = await response.Content.ReadFromJsonAsync<TodoFlatItem>();
        Assert.NotNull(item);
        Assert.Equal(_testId, item.Id);
        Assert.Equal("AuditGetTest", item.Title);
        Assert.Equal("mvp-support", item.Section);
        Assert.Equal("medium", item.Priority);
        Assert.False(item.Done);
    }

    [Fact]
    public async Task Get_NonExistentId_Returns404()
    {
        var fakeId = $"NONEXISTENT-{Guid.NewGuid():N}";
        var response = await _fixture.Client.GetAsync(
            $"{TodoEndpointFixture.TodoRoute}/{Uri.EscapeDataString(fakeId)}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
