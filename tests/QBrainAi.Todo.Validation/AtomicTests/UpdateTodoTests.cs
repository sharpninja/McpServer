using System.Net;
using System.Net.Http.Json;
using QBrainAi.Todo.Validation.Models;
using Xunit;

namespace QBrainAi.Todo.Validation.AtomicTests;

/// <summary>Audit: PUT /qbrainai/todo/{id} — Update an existing TODO item.</summary>
[Collection("TodoEndpoint")]
public sealed class UpdateTodoTests : IAsyncLifetime
{
    private readonly TodoEndpointFixture _fixture;
    private readonly string _testId;

    /// <summary>
    /// Initializes a new instance of UpdateTodoTests.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-002, TEST-MCP-074, FR-MCP-002, TR-MCP-TODO-002.
    /// Test data: Generated TODO IDs and endpoint payloads for create/update/query/error combinations.
    /// Data rationale: These inputs verify TODO endpoint contract stability, mutation behavior, and validation/error handling paths.
    /// </remarks>
    public UpdateTodoTests(TodoEndpointFixture fixture)
    {
        _fixture = fixture;
        _testId = TodoEndpointFixture.GenerateTestId();
    }

    /// <summary>
    /// Initializes test state for validation execution.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-002, TEST-MCP-074, FR-MCP-002, TR-MCP-TODO-002.
    /// Test data: Generated TODO IDs and endpoint payloads for create/update/query/error combinations.
    /// Data rationale: These inputs verify TODO endpoint contract stability, mutation behavior, and validation/error handling paths.
    /// </remarks>
    public async ValueTask InitializeAsync()
    {
        var body = new { Id = _testId, Title = "AuditUpdateOriginal", Section = "mvp-support", Priority = "low" };
        var response = await _fixture.Client.PostAsJsonAsync(TodoEndpointFixture.TodoRoute, body).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    /// <summary>
    /// Releases resources used by validation tests.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-002, TEST-MCP-074, FR-MCP-002, TR-MCP-TODO-002.
    /// Test data: Generated TODO IDs and endpoint payloads for create/update/query/error combinations.
    /// Data rationale: These inputs verify TODO endpoint contract stability, mutation behavior, and validation/error handling paths.
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        await _fixture.Client.DeleteAsync($"{TodoEndpointFixture.TodoRoute}/{Uri.EscapeDataString(_testId)}").ConfigureAwait(true);
    }

    /// <summary>
    /// Validates the <c>UpdateChangeTitleReturns200</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-002, TEST-MCP-074, FR-MCP-002, TR-MCP-TODO-002.
    /// Test data: Generated TODO IDs and endpoint payloads for create/update/query/error combinations.
    /// Data rationale: These inputs verify TODO endpoint contract stability, mutation behavior, and validation/error handling paths.
    /// </remarks>
    [Fact]
    public async Task UpdateChangeTitleReturns200()
    {
        var body = new { Title = "AuditUpdateRenamed" };
        var response = await _fixture.Client.PutAsJsonAsync(
            $"{TodoEndpointFixture.TodoRoute}/{Uri.EscapeDataString(_testId)}", body, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<TodoMutationResult>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.True(result.Success, $"Expected success but got error: {result.Error}");
        Assert.NotNull(result.Item);
        Assert.Equal("AuditUpdateRenamed", result.Item.Title);
    }

    /// <summary>
    /// Validates the <c>UpdateToggleDoneReturns200</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-002, TEST-MCP-074, FR-MCP-002, TR-MCP-TODO-002.
    /// Test data: Generated TODO IDs and endpoint payloads for create/update/query/error combinations.
    /// Data rationale: These inputs verify TODO endpoint contract stability, mutation behavior, and validation/error handling paths.
    /// </remarks>
    [Fact]
    public async Task UpdateToggleDoneReturns200()
    {
        var body = new { Done = true, CompletedDate = "2026-02-21", DoneSummary = "Completed by audit" };
        var response = await _fixture.Client.PutAsJsonAsync(
            $"{TodoEndpointFixture.TodoRoute}/{Uri.EscapeDataString(_testId)}", body, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<TodoMutationResult>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Item);
        Assert.True(result.Item.Done, "Item should now be marked done.");
    }

    /// <summary>
    /// Validates the <c>UpdateChangePriorityReturns200</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-002, TEST-MCP-074, FR-MCP-002, TR-MCP-TODO-002.
    /// Test data: Generated TODO IDs and endpoint payloads for create/update/query/error combinations.
    /// Data rationale: These inputs verify TODO endpoint contract stability, mutation behavior, and validation/error handling paths.
    /// </remarks>
    [Fact]
    public async Task UpdateChangePriorityReturns200()
    {
        var body = new { Priority = "high" };
        var response = await _fixture.Client.PutAsJsonAsync(
            $"{TodoEndpointFixture.TodoRoute}/{Uri.EscapeDataString(_testId)}", body, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<TodoMutationResult>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Item);
        Assert.Equal("high", result.Item.Priority);
    }

    /// <summary>
    /// Validates the <c>UpdateNonExistentIdReturns404</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-002, TEST-MCP-074, FR-MCP-002, TR-MCP-TODO-002.
    /// Test data: Generated TODO IDs and endpoint payloads for create/update/query/error combinations.
    /// Data rationale: These inputs verify TODO endpoint contract stability, mutation behavior, and validation/error handling paths.
    /// </remarks>
    [Fact]
    public async Task UpdateNonExistentIdReturns404()
    {
        var fakeId = $"NONEXISTENT-{Guid.NewGuid():N}";
        var body = new { Title = "Ghost" };
        var response = await _fixture.Client.PutAsJsonAsync(
            $"{TodoEndpointFixture.TodoRoute}/{Uri.EscapeDataString(fakeId)}", body, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Validates the <c>UpdateNullBodyReturns400</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-002, TEST-MCP-074, FR-MCP-002, TR-MCP-TODO-002.
    /// Test data: Generated TODO IDs and endpoint payloads for create/update/query/error combinations.
    /// Data rationale: These inputs verify TODO endpoint contract stability, mutation behavior, and validation/error handling paths.
    /// </remarks>
    [Fact]
    public async Task UpdateNullBodyReturns400()
    {
        var response = await _fixture.Client.PutAsync(
            $"{TodoEndpointFixture.TodoRoute}/{Uri.EscapeDataString(_testId)}",
            null, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.UnsupportedMediaType,
            $"Expected 400/415 but got {(int)response.StatusCode}.");
    }
}
