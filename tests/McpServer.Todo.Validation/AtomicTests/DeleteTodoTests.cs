using System.Net;
using System.Net.Http.Json;
using McpServer.Todo.Validation.Models;
using Xunit;

namespace McpServer.Todo.Validation.AtomicTests;

/// <summary>Audit: DELETE /mcpserver/todo/{id} — Delete a TODO item.</summary>
[Collection("TodoEndpoint")]
public sealed class DeleteTodoTests
{
    private readonly TodoEndpointFixture _fixture;

    /// <summary>
    /// Initializes a new instance of DeleteTodoTests.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-002, TEST-MCP-074, FR-MCP-002, TR-MCP-TODO-002.
    /// Test data: Generated TODO IDs and endpoint payloads for create/update/query/error combinations.
    /// Data rationale: These inputs verify TODO endpoint contract stability, mutation behavior, and validation/error handling paths.
    /// </remarks>
    public DeleteTodoTests(TodoEndpointFixture fixture) => _fixture = fixture;

    /// <summary>
    /// Validates the <c>Delete_ExistingItem_Returns200</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-002, TEST-MCP-074, FR-MCP-002, TR-MCP-TODO-002.
    /// Test data: Generated TODO IDs and endpoint payloads for create/update/query/error combinations.
    /// Data rationale: These inputs verify TODO endpoint contract stability, mutation behavior, and validation/error handling paths.
    /// </remarks>
    [Fact]
    public async Task Delete_ExistingItem_Returns200()
    {
        var testId = TodoEndpointFixture.GenerateTestId();
        var createBody = new { Id = testId, Title = "AuditDeleteTest", Section = "mvp-support", Priority = "low" };
        var createResponse = await _fixture.Client.PostAsJsonAsync(TodoEndpointFixture.TodoRoute, createBody);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var deleteResponse = await _fixture.Client.DeleteAsync(
            $"{TodoEndpointFixture.TodoRoute}/{Uri.EscapeDataString(testId)}");

        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        var result = await deleteResponse.Content.ReadFromJsonAsync<TodoMutationResult>();
        Assert.NotNull(result);
        Assert.True(result.Success, $"Expected success but got error: {result.Error}");

        // Verify it's gone.
        var getResponse = await _fixture.Client.GetAsync(
            $"{TodoEndpointFixture.TodoRoute}/{Uri.EscapeDataString(testId)}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    /// <summary>
    /// Validates the <c>Delete_NonExistentItem_Returns404</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-002, TEST-MCP-074, FR-MCP-002, TR-MCP-TODO-002.
    /// Test data: Generated TODO IDs and endpoint payloads for create/update/query/error combinations.
    /// Data rationale: These inputs verify TODO endpoint contract stability, mutation behavior, and validation/error handling paths.
    /// </remarks>
    [Fact]
    public async Task Delete_NonExistentItem_Returns404()
    {
        var fakeId = TodoEndpointFixture.GenerateMissingId();
        var response = await _fixture.Client.DeleteAsync(
            $"{TodoEndpointFixture.TodoRoute}/{Uri.EscapeDataString(fakeId)}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<TodoMutationResult>();
        Assert.NotNull(result);
        Assert.False(result.Success);
    }
}
