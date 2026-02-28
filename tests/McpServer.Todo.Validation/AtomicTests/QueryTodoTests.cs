using System.Net;
using System.Net.Http.Json;
using McpServer.Todo.Validation.Models;
using Xunit;

namespace McpServer.Todo.Validation.AtomicTests;

/// <summary>Audit: GET /mcpserver/todo — Query TODO items with various filters.</summary>
[Collection("TodoEndpoint")]
public sealed class QueryTodoTests
{
    private readonly TodoEndpointFixture _fixture;

    public QueryTodoTests(TodoEndpointFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Query_NoFilters_Returns200WithValidStructure()
    {
        var response = await _fixture.Client.GetAsync(TodoEndpointFixture.TodoRoute);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<TodoQueryResult>();
        Assert.NotNull(result);
        Assert.NotNull(result.Items);
        Assert.True(result.TotalCount >= 0, "TotalCount should be non-negative.");
        Assert.Equal(result.Items.Count, result.TotalCount);
    }

    [Fact]
    public async Task Query_ResponseIsJson()
    {
        var response = await _fixture.Client.GetAsync(TodoEndpointFixture.TodoRoute);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Query_ByPriority_Returns200()
    {
        var response = await _fixture.Client.GetAsync($"{TodoEndpointFixture.TodoRoute}?priority=high");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<TodoQueryResult>();
        Assert.NotNull(result);
        // All returned items should have priority=high (if any).
        foreach (var item in result.Items)
        {
            Assert.Equal("high", item.Priority, StringComparer.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Query_ByDoneStatus_Returns200()
    {
        var response = await _fixture.Client.GetAsync($"{TodoEndpointFixture.TodoRoute}?done=false");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<TodoQueryResult>();
        Assert.NotNull(result);
        foreach (var item in result.Items)
        {
            Assert.False(item.Done, $"Item {item.Id} should not be done.");
        }
    }

    [Fact]
    public async Task Query_ByKeyword_Returns200()
    {
        var response = await _fixture.Client.GetAsync($"{TodoEndpointFixture.TodoRoute}?keyword=test");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<TodoQueryResult>();
        Assert.NotNull(result);
        // Should return valid structure regardless of matches.
        Assert.NotNull(result.Items);
    }

    [Fact]
    public async Task Query_BySection_Returns200()
    {
        var response = await _fixture.Client.GetAsync($"{TodoEndpointFixture.TodoRoute}?section=mvp-app");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<TodoQueryResult>();
        Assert.NotNull(result);
        foreach (var item in result.Items)
        {
            Assert.Equal("mvp-app", item.Section, StringComparer.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Query_ById_Returns200()
    {
        // First get any existing item to use its ID.
        var listResponse = await _fixture.Client.GetAsync(TodoEndpointFixture.TodoRoute);
        var listResult = await listResponse.Content.ReadFromJsonAsync<TodoQueryResult>();
        if (listResult is null || listResult.TotalCount == 0)
            return; // Skip if no items exist.

        var knownId = listResult.Items[0].Id;
        var response = await _fixture.Client.GetAsync($"{TodoEndpointFixture.TodoRoute}?id={Uri.EscapeDataString(knownId)}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<TodoQueryResult>();
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 1);
        Assert.Contains(result.Items, i => i.Id == knownId);
    }

    [Fact]
    public async Task Query_NonMatchingKeyword_ReturnsEmptyList()
    {
        var response = await _fixture.Client.GetAsync(
            $"{TodoEndpointFixture.TodoRoute}?keyword=zzz_nonexistent_keyword_zzz_{Guid.NewGuid():N}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<TodoQueryResult>();
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }
}
