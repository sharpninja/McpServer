using System.Net;
using System.Net.Http.Json;
using McpServer.Todo.Validation.Models;
using Xunit;

namespace McpServer.Todo.Validation.ErrorTests;

/// <summary>
/// Audit: Error and edge-case tests for TODO endpoints.
/// Validates proper HTTP status codes for invalid inputs, missing resources, and duplicates.
/// </summary>
[Collection("TodoEndpoint")]
public sealed class TodoErrorTests
{
    private readonly TodoEndpointFixture _fixture;
    private readonly ITestOutputHelper _output;

    /// <summary>
    /// Initializes a new instance of TodoErrorTests.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-002, TEST-MCP-074, FR-MCP-002, TR-MCP-TODO-002.
    /// Test data: Generated TODO IDs and endpoint payloads for create/update/query/error combinations.
    /// Data rationale: These inputs verify TODO endpoint contract stability, mutation behavior, and validation/error handling paths.
    /// </remarks>
    public TodoErrorTests(TodoEndpointFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    // ── Missing resource tests ───────────────────────────────────────────

    /// <summary>
    /// Validates the <c>Get_NonExistent_Returns404</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-002, TEST-MCP-074, FR-MCP-002, TR-MCP-TODO-002.
    /// Test data: Generated TODO IDs and endpoint payloads for create/update/query/error combinations.
    /// Data rationale: These inputs verify TODO endpoint contract stability, mutation behavior, and validation/error handling paths.
    /// </remarks>
    [Fact]
    public async Task Get_NonExistent_Returns404()
    {
        var fakeId = $"NONEXISTENT-{Guid.NewGuid():N}";
        var response = await _fixture.Client.GetAsync(
            $"{TodoEndpointFixture.TodoRoute}/{Uri.EscapeDataString(fakeId)}", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Validates the <c>Update_NonExistent_Returns404</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-002, TEST-MCP-074, FR-MCP-002, TR-MCP-TODO-002.
    /// Test data: Generated TODO IDs and endpoint payloads for create/update/query/error combinations.
    /// Data rationale: These inputs verify TODO endpoint contract stability, mutation behavior, and validation/error handling paths.
    /// </remarks>
    [Fact]
    public async Task Update_NonExistent_Returns404()
    {
        var fakeId = $"NONEXISTENT-{Guid.NewGuid():N}";
        var body = new { Title = "Ghost" };
        var response = await _fixture.Client.PutAsJsonAsync(
            $"{TodoEndpointFixture.TodoRoute}/{Uri.EscapeDataString(fakeId)}", body, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Validates the <c>Delete_NonExistent_Returns404</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-002, TEST-MCP-074, FR-MCP-002, TR-MCP-TODO-002.
    /// Test data: Generated TODO IDs and endpoint payloads for create/update/query/error combinations.
    /// Data rationale: These inputs verify TODO endpoint contract stability, mutation behavior, and validation/error handling paths.
    /// </remarks>
    [Fact]
    public async Task Delete_NonExistent_Returns404()
    {
        var fakeId = $"NONEXISTENT-{Guid.NewGuid():N}";
        var response = await _fixture.Client.DeleteAsync(
            $"{TodoEndpointFixture.TodoRoute}/{Uri.EscapeDataString(fakeId)}", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── Duplicate create test ────────────────────────────────────────────

    /// <summary>
    /// Validates the <c>Create_Duplicate_Returns409</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-002, TEST-MCP-074, FR-MCP-002, TR-MCP-TODO-002.
    /// Test data: Generated TODO IDs and endpoint payloads for create/update/query/error combinations.
    /// Data rationale: These inputs verify TODO endpoint contract stability, mutation behavior, and validation/error handling paths.
    /// </remarks>
    [Fact]
    public async Task Create_Duplicate_Returns409()
    {
        var testId = TodoEndpointFixture.GenerateTestId();
        try
        {
            var body = new { Id = testId, Title = "DupeFirst", Section = "mvp-support", Priority = "low" };
            var first = await _fixture.Client.PostAsJsonAsync(TodoEndpointFixture.TodoRoute, body, cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Created, first.StatusCode);

            var second = await _fixture.Client.PostAsJsonAsync(TodoEndpointFixture.TodoRoute, body, cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

            var result = await second.Content.ReadFromJsonAsync<TodoMutationResult>(cancellationToken: TestContext.Current.CancellationToken);
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
            _output.WriteLine($"Duplicate error: {result.Error}");
        }
        finally
        {
            await _fixture.Client.DeleteAsync($"{TodoEndpointFixture.TodoRoute}/{Uri.EscapeDataString(testId)}", cancellationToken: TestContext.Current.CancellationToken);
        }
    }

    // ── Missing/empty body tests ─────────────────────────────────────────

    /// <summary>
    /// Validates the <c>Create_EmptyBody_ReturnsBadRequest</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-002, TEST-MCP-074, FR-MCP-002, TR-MCP-TODO-002.
    /// Test data: Generated TODO IDs and endpoint payloads for create/update/query/error combinations.
    /// Data rationale: These inputs verify TODO endpoint contract stability, mutation behavior, and validation/error handling paths.
    /// </remarks>
    [Fact]
    public async Task Create_EmptyBody_ReturnsBadRequest()
    {
        var response = await _fixture.Client.PostAsync(
            TodoEndpointFixture.TodoRoute,
            new StringContent("{}", System.Text.Encoding.UTF8, "application/json"), cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.UnprocessableEntity,
            $"Expected 400/422 but got {(int)response.StatusCode}.");
    }

    /// <summary>
    /// Validates the <c>Update_NullBody_Returns400OrUnsupportedMedia</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-002, TEST-MCP-074, FR-MCP-002, TR-MCP-TODO-002.
    /// Test data: Generated TODO IDs and endpoint payloads for create/update/query/error combinations.
    /// Data rationale: These inputs verify TODO endpoint contract stability, mutation behavior, and validation/error handling paths.
    /// </remarks>
    [Fact]
    public async Task Update_NullBody_Returns400OrUnsupportedMedia()
    {
        var fakeId = $"FAKE-{Guid.NewGuid():N}";
        var response = await _fixture.Client.PutAsync(
            $"{TodoEndpointFixture.TodoRoute}/{Uri.EscapeDataString(fakeId)}",
            null, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.UnsupportedMediaType ||
            response.StatusCode == HttpStatusCode.NotFound,
            $"Expected 400/415/404 but got {(int)response.StatusCode}.");
    }

    // ── Method not allowed ───────────────────────────────────────────────

    /// <summary>
    /// Validates the <c>Patch_NotSupported_Returns405</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-002, TEST-MCP-074, FR-MCP-002, TR-MCP-TODO-002.
    /// Test data: Generated TODO IDs and endpoint payloads for create/update/query/error combinations.
    /// Data rationale: These inputs verify TODO endpoint contract stability, mutation behavior, and validation/error handling paths.
    /// </remarks>
    [Fact]
    public async Task Patch_NotSupported_Returns405()
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, $"{TodoEndpointFixture.TodoRoute}/some-id")
        {
            Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
        };
        var response = await _fixture.Client.SendAsync(request, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    // ── Query with invalid filter combinations (should still return 200) ─

    /// <summary>
    /// Validates the <c>Query_InvalidPriority_Returns200EmptyOrAll</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-002, TEST-MCP-074, FR-MCP-002, TR-MCP-TODO-002.
    /// Test data: Generated TODO IDs and endpoint payloads for create/update/query/error combinations.
    /// Data rationale: These inputs verify TODO endpoint contract stability, mutation behavior, and validation/error handling paths.
    /// </remarks>
    [Fact]
    public async Task Query_InvalidPriority_Returns200EmptyOrAll()
    {
        var response = await _fixture.Client.GetAsync(
            $"{TodoEndpointFixture.TodoRoute}?priority=nonexistent_priority", cancellationToken: TestContext.Current.CancellationToken);

        // The service should handle gracefully — either return empty or all items.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<TodoQueryResult>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
    }

    /// <summary>
    /// Validates the <c>Query_MultipleFilters_Returns200</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-002, TEST-MCP-074, FR-MCP-002, TR-MCP-TODO-002.
    /// Test data: Generated TODO IDs and endpoint payloads for create/update/query/error combinations.
    /// Data rationale: These inputs verify TODO endpoint contract stability, mutation behavior, and validation/error handling paths.
    /// </remarks>
    [Fact]
    public async Task Query_MultipleFilters_Returns200()
    {
        var response = await _fixture.Client.GetAsync(
            $"{TodoEndpointFixture.TodoRoute}?priority=high&done=false&section=audit", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<TodoQueryResult>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.NotNull(result.Items);
    }
}
