using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using McpServer.SessionLog.Validation.Models;
using Xunit;

namespace McpServer.SessionLog.Validation.AtomicTests;

/// <summary>
/// Validation tests for <c>SubmitSessionLogTests</c>.
/// </summary>
/// <remarks>
/// Requirement coverage: TEST-MCP-015, TEST-MCP-074, FR-MCP-003, TR-MCP-LOG-002.
/// Test data: Generated session/request IDs plus submit/query/dialog payloads serialized as endpoint JSON bodies.
/// Data rationale: These inputs verify session-log persistence/query behavior and canonical identifier validation paths.
/// </remarks>
[Collection("SessionLogEndpoint")]
public sealed class SubmitSessionLogTests
{
    private readonly SessionLogEndpointFixture _fixture;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Initializes a new instance of SubmitSessionLogTests.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-015, TEST-MCP-074, FR-MCP-003, TR-MCP-LOG-002.
    /// Test data: Generated session/request IDs plus submit/query/dialog payloads serialized as endpoint JSON bodies.
    /// Data rationale: These inputs verify session-log persistence/query behavior and canonical identifier validation paths.
    /// </remarks>
    public SubmitSessionLogTests(SessionLogEndpointFixture fixture) => _fixture = fixture;

    /// <summary>
    /// Validates the <c>Submit_MinimalSessionLog_Returns201</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-015, TEST-MCP-074, FR-MCP-003, TR-MCP-LOG-002.
    /// Test data: Generated session/request IDs plus submit/query/dialog payloads serialized as endpoint JSON bodies.
    /// Data rationale: These inputs verify session-log persistence/query behavior and canonical identifier validation paths.
    /// </remarks>
    [Fact]
    public async Task Submit_MinimalSessionLog_Returns201()
    {
        var sessionId = SessionLogEndpointFixture.GenerateSessionId("AuditTest");
        var payload = new
        {
            sourceType = "AuditTest",
            sessionId,
            title = "Minimal audit test",
            model = "test-model",
            started = DateTimeOffset.UtcNow.ToString("o"),
            lastUpdated = DateTimeOffset.UtcNow.ToString("o"),
            status = "completed",
            turnCount = 0
        };

        var response = await _fixture.Client.PostAsJsonAsync(SessionLogEndpointFixture.SessionLogRoute, payload, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<SubmitResult>(JsonOpts, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.True(result!.Id > 0);
        Assert.Equal("AuditTest", result.SourceType);
        Assert.Equal(sessionId, result.SessionId);
    }

    /// <summary>
    /// Validates the <c>Submit_FullSessionLogWithTurns_Returns201</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-015, TEST-MCP-074, FR-MCP-003, TR-MCP-LOG-002.
    /// Test data: Generated session/request IDs plus submit/query/dialog payloads serialized as endpoint JSON bodies.
    /// Data rationale: These inputs verify session-log persistence/query behavior and canonical identifier validation paths.
    /// </remarks>
    [Fact]
    public async Task Submit_FullSessionLogWithTurns_Returns201()
    {
        var sessionId = SessionLogEndpointFixture.GenerateSessionId("AuditTest");
        var requestId = SessionLogEndpointFixture.GenerateRequestId("submit-full-session-log");
        var payload = new
        {
            sourceType = "AuditTest",
            sessionId,
            title = "Full audit test with turns",
            model = "claude-sonnet-4-20250514",
            started = DateTimeOffset.UtcNow.AddMinutes(-10).ToString("o"),
            lastUpdated = DateTimeOffset.UtcNow.ToString("o"),
            status = "completed",
            turnCount = 1,
            workspace = new
            {
                project = "McpServer",
                targetFramework = ".NET 9",
                repository = "https://github.com/sharpninja/McpServer.git",
                branch = "develop"
            },
            turns = new[]
            {
                new
                {
                    requestId,
                    timestamp = DateTimeOffset.UtcNow.ToString("o"),
                    queryText = "Submit full session log audit test",
                    queryTitle = "Full audit turn",
                    response = "Session log submitted successfully",
                    status = "completed",
                    score = 1.0,
                    tags = new[] { "audit", "test" },
                    actions = new[]
                    {
                        new { order = 1, description = "Created test turn", type = "create", status = "completed", filePath = "test.cs" }
                    }
                }
            }
        };

        var response = await _fixture.Client.PostAsJsonAsync(SessionLogEndpointFixture.SessionLogRoute, payload, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<SubmitResult>(JsonOpts, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.True(result!.Id > 0);
    }

    /// <summary>
    /// Validates the <c>Submit_UpsertSameSession_Returns201WithUpdatedData</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-015, TEST-MCP-074, FR-MCP-003, TR-MCP-LOG-002.
    /// Test data: Generated session/request IDs plus submit/query/dialog payloads serialized as endpoint JSON bodies.
    /// Data rationale: These inputs verify session-log persistence/query behavior and canonical identifier validation paths.
    /// </remarks>
    [Fact]
    public async Task Submit_UpsertSameSession_Returns201WithUpdatedData()
    {
        var sessionId = SessionLogEndpointFixture.GenerateSessionId("AuditTest");
        var payload1 = new
        {
            sourceType = "AuditTest",
            sessionId,
            title = "Upsert test v1",
            model = "test-model",
            started = DateTimeOffset.UtcNow.ToString("o"),
            lastUpdated = DateTimeOffset.UtcNow.ToString("o"),
            status = "in_progress",
            turnCount = 0
        };

        var response1 = await _fixture.Client.PostAsJsonAsync(SessionLogEndpointFixture.SessionLogRoute, payload1, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);
        var result1 = await response1.Content.ReadFromJsonAsync<SubmitResult>(JsonOpts, cancellationToken: TestContext.Current.CancellationToken);

        // Upsert with same SourceType + SessionId
        var payload2 = new
        {
            sourceType = "AuditTest",
            sessionId,
            title = "Upsert test v2",
            model = "test-model-v2",
            started = DateTimeOffset.UtcNow.ToString("o"),
            lastUpdated = DateTimeOffset.UtcNow.ToString("o"),
            status = "completed",
            turnCount = 1
        };

        var response2 = await _fixture.Client.PostAsJsonAsync(SessionLogEndpointFixture.SessionLogRoute, payload2, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, response2.StatusCode);
        var result2 = await response2.Content.ReadFromJsonAsync<SubmitResult>(JsonOpts, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result2);
        // Should reuse same ID (upsert)
        Assert.Equal(result1!.Id, result2!.Id);
    }

    /// <summary>
    /// Validates the <c>Submit_WithProcessingDialog_Returns201</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-015, TEST-MCP-074, FR-MCP-003, TR-MCP-LOG-002.
    /// Test data: Generated session/request IDs plus submit/query/dialog payloads serialized as endpoint JSON bodies.
    /// Data rationale: These inputs verify session-log persistence/query behavior and canonical identifier validation paths.
    /// </remarks>
    [Fact]
    public async Task Submit_WithProcessingDialog_Returns201()
    {
        var sessionId = SessionLogEndpointFixture.GenerateSessionId("AuditTest");
        var payload = new
        {
            sourceType = "AuditTest",
            sessionId,
            title = "Dialog test",
            model = "test-model",
            started = DateTimeOffset.UtcNow.ToString("o"),
            lastUpdated = DateTimeOffset.UtcNow.ToString("o"),
            status = "completed",
            turnCount = 1,
            turns = new[]
            {
                new
                {
                    requestId = SessionLogEndpointFixture.GenerateRequestId("submit-with-processing-dialog"),
                    timestamp = DateTimeOffset.UtcNow.ToString("o"),
                    queryText = "Test with dialog",
                    queryTitle = "Dialog test turn",
                    response = "Done",
                    status = "completed",
                    processingDialog = new[]
                    {
                        new { timestamp = DateTimeOffset.UtcNow.ToString("o"), role = "model", content = "Thinking about the problem...", category = "reasoning" },
                        new { timestamp = DateTimeOffset.UtcNow.ToString("o"), role = "tool", content = "read_file result", category = "tool_result" }
                    }
                }
            }
        };

        var response = await _fixture.Client.PostAsJsonAsync(SessionLogEndpointFixture.SessionLogRoute, payload, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
