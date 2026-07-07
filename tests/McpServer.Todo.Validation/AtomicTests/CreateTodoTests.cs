using System.Net;
using System.Net.Http.Json;
using McpServer.Todo.Validation.Models;
using Xunit;

namespace McpServer.Todo.Validation.AtomicTests;

/// <summary>Audit: POST /mcpserver/todo — Create a new TODO item.</summary>
[Collection("TodoEndpoint")]
public sealed class CreateTodoTests : IAsyncLifetime
{
    private readonly TodoEndpointFixture _fixture;
    private readonly string _testId;

    /// <summary>
    /// Initializes a new instance of CreateTodoTests.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-002, TEST-MCP-074, FR-MCP-002, TR-MCP-TODO-002.
    /// Test data: Generated TODO IDs and endpoint payloads for create/update/query/error combinations.
    /// Data rationale: These inputs verify TODO endpoint contract stability, mutation behavior, and validation/error handling paths.
    /// </remarks>
    public CreateTodoTests(TodoEndpointFixture fixture)
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
    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

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
        await _fixture.Client.DeleteAsync($"{TodoEndpointFixture.TodoRoute}/{Uri.EscapeDataString(_testId)}");
    }

    /// <summary>
    /// Validates the <c>Create_ValidRequest_Returns201</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-002, TEST-MCP-074, FR-MCP-002, TR-MCP-TODO-002.
    /// Test data: Generated TODO IDs and endpoint payloads for create/update/query/error combinations.
    /// Data rationale: These inputs verify TODO endpoint contract stability, mutation behavior, and validation/error handling paths.
    /// </remarks>
    [Fact]
    public async Task Create_ValidRequest_Returns201()
    {
        var body = new
        {
            Id = _testId,
            Title = "AuditCreateTest",
            Section = "mvp-support",
            Priority = "high",
            Estimate = "2-4 hours",
            Description = new[] { "Audit test item" },
            TechnicalDetails = new[] { "Created by validation suite" }
        };

        var response = await _fixture.Client.PostAsJsonAsync(TodoEndpointFixture.TodoRoute, body, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<TodoMutationResult>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.True(result.Success, $"Expected success but got error: {result.Error}");
        Assert.NotNull(result.Item);
        Assert.Equal(_testId, result.Item.Id);
        Assert.Equal("AuditCreateTest", result.Item.Title);
        Assert.Equal("mvp-support", result.Item.Section);
        Assert.Equal("high", result.Item.Priority);
        Assert.False(result.Item.Done);

        // Location header should be set.
        Assert.NotNull(response.Headers.Location);
        Assert.Contains(Uri.EscapeDataString(_testId), response.Headers.Location.ToString());
    }

    /// <summary>
    /// Validates the <c>Create_DuplicateId_Returns409</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-002, TEST-MCP-074, FR-MCP-002, TR-MCP-TODO-002.
    /// Test data: Generated TODO IDs and endpoint payloads for create/update/query/error combinations.
    /// Data rationale: These inputs verify TODO endpoint contract stability, mutation behavior, and validation/error handling paths.
    /// </remarks>
    [Fact]
    public async Task Create_DuplicateId_Returns409()
    {
        var body = new { Id = _testId, Title = "First", Section = "mvp-support", Priority = "low" };
        var first = await _fixture.Client.PostAsJsonAsync(TodoEndpointFixture.TodoRoute, body, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await _fixture.Client.PostAsJsonAsync(TodoEndpointFixture.TodoRoute, body, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

        var result = await second.Content.ReadFromJsonAsync<TodoMutationResult>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.False(result.Success);
    }

    /// <summary>
    /// Validates the <c>Create_NullBody_Returns400</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-002, TEST-MCP-074, FR-MCP-002, TR-MCP-TODO-002.
    /// Test data: Generated TODO IDs and endpoint payloads for create/update/query/error combinations.
    /// Data rationale: These inputs verify TODO endpoint contract stability, mutation behavior, and validation/error handling paths.
    /// </remarks>
    [Fact]
    public async Task Create_NullBody_Returns400()
    {
        var response = await _fixture.Client.PostAsync(
            TodoEndpointFixture.TodoRoute,
            new StringContent("{}", System.Text.Encoding.UTF8, "application/json"), cancellationToken: TestContext.Current.CancellationToken);

        // Empty object missing required fields → 400.
        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.UnprocessableEntity,
            $"Expected 400/422 but got {(int)response.StatusCode}.");
    }

    /// <summary>
    /// Validates the <c>Create_WithImplementationTasks_Returns201</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-002, TEST-MCP-074, FR-MCP-002, TR-MCP-TODO-002.
    /// Test data: Generated TODO IDs and endpoint payloads for create/update/query/error combinations.
    /// Data rationale: These inputs verify TODO endpoint contract stability, mutation behavior, and validation/error handling paths.
    /// </remarks>
    [Fact]
    public async Task Create_WithImplementationTasks_Returns201()
    {
        var body = new
        {
            Id = _testId,
            Title = "AuditTasksTest",
            Section = "mvp-support",
            Priority = "medium",
            ImplementationTasks = new[]
            {
                new { Task = "Step 1", Done = false },
                new { Task = "Step 2", Done = true }
            }
        };

        var response = await _fixture.Client.PostAsJsonAsync(TodoEndpointFixture.TodoRoute, body, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<TodoMutationResult>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Item);
        Assert.NotNull(result.Item.ImplementationTasks);
        Assert.Equal(2, result.Item.ImplementationTasks.Count);
    }
}
