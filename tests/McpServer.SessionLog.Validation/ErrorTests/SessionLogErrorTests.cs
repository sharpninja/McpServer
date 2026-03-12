using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using McpServer.SessionLog.Validation.Models;
using Xunit;

namespace McpServer.SessionLog.Validation.ErrorTests;

/// <summary>
/// Validation tests for <c>SessionLogErrorTests</c>.
/// </summary>
/// <remarks>
/// Requirement coverage: TEST-MCP-015, TEST-MCP-074, FR-MCP-003, TR-MCP-LOG-002.
/// Test data: Generated session/request IDs plus submit/query/dialog payloads serialized as endpoint JSON bodies.
/// Data rationale: These inputs verify session-log persistence/query behavior and canonical identifier validation paths.
/// </remarks>
[Collection("SessionLogEndpoint")]
public sealed class SessionLogErrorTests
{
    private readonly SessionLogEndpointFixture _fixture;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Initializes a new instance of SessionLogErrorTests.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-015, TEST-MCP-074, FR-MCP-003, TR-MCP-LOG-002.
    /// Test data: Generated session/request IDs plus submit/query/dialog payloads serialized as endpoint JSON bodies.
    /// Data rationale: These inputs verify session-log persistence/query behavior and canonical identifier validation paths.
    /// </remarks>
    public SessionLogErrorTests(SessionLogEndpointFixture fixture) => _fixture = fixture;

    /// <summary>
    /// Validates the <c>Submit_MissingSourceType_Returns400</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-015, TEST-MCP-074, FR-MCP-003, TR-MCP-LOG-002.
    /// Test data: Generated session/request IDs plus submit/query/dialog payloads serialized as endpoint JSON bodies.
    /// Data rationale: These inputs verify session-log persistence/query behavior and canonical identifier validation paths.
    /// </remarks>
    [Fact]
    public async Task Submit_MissingSourceType_Returns400()
    {
        var payload = new
        {
            sessionId = SessionLogEndpointFixture.GenerateSessionId("AuditTest"),
            title = "Missing source type",
            model = "test",
            status = "completed",
            entryCount = 0
        };

        var response = await _fixture.Client.PostAsJsonAsync(SessionLogEndpointFixture.SessionLogRoute, payload);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var err = await response.Content.ReadFromJsonAsync<ErrorResult>(JsonOpts);
        Assert.Contains("SourceType", err!.Error!, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Validates the <c>Submit_MissingSessionId_Returns400</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-015, TEST-MCP-074, FR-MCP-003, TR-MCP-LOG-002.
    /// Test data: Generated session/request IDs plus submit/query/dialog payloads serialized as endpoint JSON bodies.
    /// Data rationale: These inputs verify session-log persistence/query behavior and canonical identifier validation paths.
    /// </remarks>
    [Fact]
    public async Task Submit_MissingSessionId_Returns400()
    {
        var payload = new
        {
            sourceType = "AuditTest",
            title = "Missing session ID",
            model = "test",
            status = "completed",
            entryCount = 0
        };

        var response = await _fixture.Client.PostAsJsonAsync(SessionLogEndpointFixture.SessionLogRoute, payload);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var err = await response.Content.ReadFromJsonAsync<ErrorResult>(JsonOpts);
        Assert.Contains("SessionId", err!.Error!, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Validates the <c>Submit_EmptySourceType_Returns400</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-015, TEST-MCP-074, FR-MCP-003, TR-MCP-LOG-002.
    /// Test data: Generated session/request IDs plus submit/query/dialog payloads serialized as endpoint JSON bodies.
    /// Data rationale: These inputs verify session-log persistence/query behavior and canonical identifier validation paths.
    /// </remarks>
    [Fact]
    public async Task Submit_EmptySourceType_Returns400()
    {
        var payload = new
        {
            sourceType = "",
            sessionId = SessionLogEndpointFixture.GenerateSessionId("AuditTest"),
            title = "Empty source type",
            entryCount = 0
        };

        var response = await _fixture.Client.PostAsJsonAsync(SessionLogEndpointFixture.SessionLogRoute, payload);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Validates the <c>Submit_EmptySessionId_Returns400</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-015, TEST-MCP-074, FR-MCP-003, TR-MCP-LOG-002.
    /// Test data: Generated session/request IDs plus submit/query/dialog payloads serialized as endpoint JSON bodies.
    /// Data rationale: These inputs verify session-log persistence/query behavior and canonical identifier validation paths.
    /// </remarks>
    [Fact]
    public async Task Submit_EmptySessionId_Returns400()
    {
        var payload = new
        {
            sourceType = "AuditTest",
            sessionId = "",
            title = "Empty session ID",
            entryCount = 0
        };

        var response = await _fixture.Client.PostAsJsonAsync(SessionLogEndpointFixture.SessionLogRoute, payload);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Validates the <c>AppendDialog_EmptyItemsList_Returns400</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-015, TEST-MCP-074, FR-MCP-003, TR-MCP-LOG-002.
    /// Test data: Generated session/request IDs plus submit/query/dialog payloads serialized as endpoint JSON bodies.
    /// Data rationale: These inputs verify session-log persistence/query behavior and canonical identifier validation paths.
    /// </remarks>
    [Fact]
    public async Task AppendDialog_EmptyItemsList_Returns400()
    {
        var dialogRoute = $"{SessionLogEndpointFixture.SessionLogRoute}/AuditTest/some-session/some-request/dialog";
        var emptyItems = Array.Empty<object>();
        var response = await _fixture.Client.PostAsJsonAsync(dialogRoute, emptyItems);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Validates the <c>Submit_InvalidJsonBody_Returns400</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-015, TEST-MCP-074, FR-MCP-003, TR-MCP-LOG-002.
    /// Test data: Generated session/request IDs plus submit/query/dialog payloads serialized as endpoint JSON bodies.
    /// Data rationale: These inputs verify session-log persistence/query behavior and canonical identifier validation paths.
    /// </remarks>
    [Fact]
    public async Task Submit_InvalidJsonBody_Returns400()
    {
        var content = new StringContent("not valid json", Encoding.UTF8, "application/json");
        var response = await _fixture.Client.PostAsync(SessionLogEndpointFixture.SessionLogRoute, content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
