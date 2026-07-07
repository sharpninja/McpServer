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

    /// <summary>
    /// Initializes a new instance of QueryTodoTests.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-002, TEST-MCP-074, FR-MCP-002, TR-MCP-TODO-002.
    /// Test data: Generated TODO IDs and endpoint payloads for create/update/query/error combinations.
    /// Data rationale: These inputs verify TODO endpoint contract stability, mutation behavior, and validation/error handling paths.
    /// </remarks>
    public QueryTodoTests(TodoEndpointFixture fixture) => _fixture = fixture;

    /// <summary>
    /// Validates the <c>Query_NoFilters_Returns200WithValidStructure</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-002, TEST-MCP-074, FR-MCP-002, TR-MCP-TODO-002.
    /// Test data: Generated TODO IDs and endpoint payloads for create/update/query/error combinations.
    /// Data rationale: These inputs verify TODO endpoint contract stability, mutation behavior, and validation/error handling paths.
    /// </remarks>
    [Fact]
    public async Task Query_NoFilters_Returns200WithValidStructure()
    {
        var response = await _fixture.Client.GetAsync(TodoEndpointFixture.TodoRoute, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<TodoQueryResult>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.NotNull(result.Items);
        Assert.True(result.TotalCount >= 0, "TotalCount should be non-negative.");
        Assert.Equal(result.Items.Count, result.TotalCount);
    }

    /// <summary>
    /// Validates the <c>Query_ResponseIsJson</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-002, TEST-MCP-074, FR-MCP-002, TR-MCP-TODO-002.
    /// Test data: Generated TODO IDs and endpoint payloads for create/update/query/error combinations.
    /// Data rationale: These inputs verify TODO endpoint contract stability, mutation behavior, and validation/error handling paths.
    /// </remarks>
    [Fact]
    public async Task Query_ResponseIsJson()
    {
        var response = await _fixture.Client.GetAsync(TodoEndpointFixture.TodoRoute, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    /// <summary>
    /// Validates the <c>Query_ByPriority_Returns200</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-002, TEST-MCP-074, FR-MCP-002, TR-MCP-TODO-002.
    /// Test data: Generated TODO IDs and endpoint payloads for create/update/query/error combinations.
    /// Data rationale: These inputs verify TODO endpoint contract stability, mutation behavior, and validation/error handling paths.
    /// </remarks>
    [Fact]
    public async Task Query_ByPriority_Returns200()
    {
        var response = await _fixture.Client.GetAsync($"{TodoEndpointFixture.TodoRoute}?priority=high", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<TodoQueryResult>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        // All returned items should have priority=high (if any).
        foreach (var item in result.Items)
        {
            Assert.Equal("high", item.Priority, StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Validates the <c>Query_ByDoneStatus_Returns200</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-002, TEST-MCP-074, FR-MCP-002, TR-MCP-TODO-002.
    /// Test data: Generated TODO IDs and endpoint payloads for create/update/query/error combinations.
    /// Data rationale: These inputs verify TODO endpoint contract stability, mutation behavior, and validation/error handling paths.
    /// </remarks>
    [Fact]
    public async Task Query_ByDoneStatus_Returns200()
    {
        var response = await _fixture.Client.GetAsync($"{TodoEndpointFixture.TodoRoute}?done=false", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<TodoQueryResult>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        foreach (var item in result.Items)
        {
            Assert.False(item.Done, $"Item {item.Id} should not be done.");
        }
    }

    /// <summary>
    /// Validates the <c>Query_ByKeyword_Returns200</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-002, TEST-MCP-074, FR-MCP-002, TR-MCP-TODO-002.
    /// Test data: Generated TODO IDs and endpoint payloads for create/update/query/error combinations.
    /// Data rationale: These inputs verify TODO endpoint contract stability, mutation behavior, and validation/error handling paths.
    /// </remarks>
    [Fact]
    public async Task Query_ByKeyword_Returns200()
    {
        var response = await _fixture.Client.GetAsync($"{TodoEndpointFixture.TodoRoute}?keyword=test", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<TodoQueryResult>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        // Should return valid structure regardless of matches.
        Assert.NotNull(result.Items);
    }

    /// <summary>
    /// Validates the <c>Query_BySection_Returns200</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-002, TEST-MCP-074, FR-MCP-002, TR-MCP-TODO-002.
    /// Test data: Generated TODO IDs and endpoint payloads for create/update/query/error combinations.
    /// Data rationale: These inputs verify TODO endpoint contract stability, mutation behavior, and validation/error handling paths.
    /// </remarks>
    [Fact]
    public async Task Query_BySection_Returns200()
    {
        var response = await _fixture.Client.GetAsync($"{TodoEndpointFixture.TodoRoute}?section=mvp-app", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<TodoQueryResult>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        foreach (var item in result.Items)
        {
            Assert.Equal("mvp-app", item.Section, StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Validates the <c>Query_ById_Returns200</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-002, TEST-MCP-074, FR-MCP-002, TR-MCP-TODO-002.
    /// Test data: Generated TODO IDs and endpoint payloads for create/update/query/error combinations.
    /// Data rationale: These inputs verify TODO endpoint contract stability, mutation behavior, and validation/error handling paths.
    /// </remarks>
    [Fact]
    public async Task Query_ById_Returns200()
    {
        // First get any existing item to use its ID.
        var listResponse = await _fixture.Client.GetAsync(TodoEndpointFixture.TodoRoute, cancellationToken: TestContext.Current.CancellationToken);
        var listResult = await listResponse.Content.ReadFromJsonAsync<TodoQueryResult>(cancellationToken: TestContext.Current.CancellationToken);
        if (listResult is null || listResult.TotalCount == 0)
            return; // Skip if no items exist.

        var knownId = listResult.Items[0].Id;
        var response = await _fixture.Client.GetAsync($"{TodoEndpointFixture.TodoRoute}?id={Uri.EscapeDataString(knownId)}", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<TodoQueryResult>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 1);
        Assert.Contains(result.Items, i => i.Id == knownId);
    }

    /// <summary>
    /// Validates the <c>Query_NonMatchingKeyword_ReturnsEmptyList</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-002, TEST-MCP-074, FR-MCP-002, TR-MCP-TODO-002.
    /// Test data: Generated TODO IDs and endpoint payloads for create/update/query/error combinations.
    /// Data rationale: These inputs verify TODO endpoint contract stability, mutation behavior, and validation/error handling paths.
    /// </remarks>
    [Fact]
    public async Task Query_NonMatchingKeyword_ReturnsEmptyList()
    {
        var response = await _fixture.Client.GetAsync(
            $"{TodoEndpointFixture.TodoRoute}?keyword=zzz_nonexistent_keyword_zzz_{Guid.NewGuid():N}", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<TodoQueryResult>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }
}
