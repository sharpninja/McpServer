using System.Net;
using System.Net.Http.Json;
using McpServer.Todo.Validation.Models;
using Xunit;

namespace McpServer.Todo.Validation.AtomicTests;

/// <summary>Audit: PUT /mcpserver/todo/{id} — Update an existing TODO item.</summary>
[Collection("TodoEndpoint")]
public sealed class UpdateTodoTests : IAsyncLifetime
{
    private readonly TodoEndpointFixture _fixture;
    private readonly string _testId;

    public UpdateTodoTests(TodoEndpointFixture fixture)
    {
        _fixture = fixture;
        _testId = TodoEndpointFixture.GenerateTestId();
    }

    public async ValueTask InitializeAsync()
    {
        var body = new { Id = _testId, Title = "AuditUpdateOriginal", Section = "mvp-support", Priority = "low" };
        var response = await _fixture.Client.PostAsJsonAsync(TodoEndpointFixture.TodoRoute, body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    public async ValueTask DisposeAsync()
    {
        await _fixture.Client.DeleteAsync($"{TodoEndpointFixture.TodoRoute}/{Uri.EscapeDataString(_testId)}");
    }

    [Fact]
    public async Task Update_ChangeTitle_Returns200()
    {
        var body = new { Title = "AuditUpdateRenamed" };
        var response = await _fixture.Client.PutAsJsonAsync(
            $"{TodoEndpointFixture.TodoRoute}/{Uri.EscapeDataString(_testId)}", body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<TodoMutationResult>();
        Assert.NotNull(result);
        Assert.True(result.Success, $"Expected success but got error: {result.Error}");
        Assert.NotNull(result.Item);
        Assert.Equal("AuditUpdateRenamed", result.Item.Title);
    }

    [Fact]
    public async Task Update_ToggleDone_Returns200()
    {
        var body = new { Done = true, CompletedDate = "2026-02-21", DoneSummary = "Completed by audit" };
        var response = await _fixture.Client.PutAsJsonAsync(
            $"{TodoEndpointFixture.TodoRoute}/{Uri.EscapeDataString(_testId)}", body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<TodoMutationResult>();
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Item);
        Assert.True(result.Item.Done, "Item should now be marked done.");
    }

    [Fact]
    public async Task Update_ChangePriority_Returns200()
    {
        var body = new { Priority = "high" };
        var response = await _fixture.Client.PutAsJsonAsync(
            $"{TodoEndpointFixture.TodoRoute}/{Uri.EscapeDataString(_testId)}", body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<TodoMutationResult>();
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Item);
        Assert.Equal("high", result.Item.Priority);
    }

    [Fact]
    public async Task Update_NonExistentId_Returns404()
    {
        var fakeId = $"NONEXISTENT-{Guid.NewGuid():N}";
        var body = new { Title = "Ghost" };
        var response = await _fixture.Client.PutAsJsonAsync(
            $"{TodoEndpointFixture.TodoRoute}/{Uri.EscapeDataString(fakeId)}", body);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_NullBody_Returns400()
    {
        var response = await _fixture.Client.PutAsync(
            $"{TodoEndpointFixture.TodoRoute}/{Uri.EscapeDataString(_testId)}",
            null);

        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.UnsupportedMediaType,
            $"Expected 400/415 but got {(int)response.StatusCode}.");
    }
}
