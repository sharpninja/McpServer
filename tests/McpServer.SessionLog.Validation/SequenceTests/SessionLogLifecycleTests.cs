using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using McpServer.SessionLog.Validation.Models;
using Xunit;

namespace McpServer.SessionLog.Validation.SequenceTests;

/// <summary>
/// Validation tests for <c>SessionLogLifecycleTests</c>.
/// </summary>
/// <remarks>
/// Requirement coverage: TEST-MCP-015, TEST-MCP-074, FR-MCP-003, TR-MCP-LOG-002.
/// Test data: Generated session/request IDs plus submit/query/dialog payloads serialized as endpoint JSON bodies.
/// Data rationale: These inputs verify session-log persistence/query behavior and canonical identifier validation paths.
/// </remarks>
[Collection("SessionLogEndpoint")]
public sealed class SessionLogLifecycleTests
{
    private readonly SessionLogEndpointFixture _fixture;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Initializes a new instance of SessionLogLifecycleTests.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-015, TEST-MCP-074, FR-MCP-003, TR-MCP-LOG-002.
    /// Test data: Generated session/request IDs plus submit/query/dialog payloads serialized as endpoint JSON bodies.
    /// Data rationale: These inputs verify session-log persistence/query behavior and canonical identifier validation paths.
    /// </remarks>
    public SessionLogLifecycleTests(SessionLogEndpointFixture fixture) => _fixture = fixture;

    /// <summary>
    /// Validates the <c>FullLifecycle_Submit_Query_AppendDialog_Requery</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-015, TEST-MCP-074, FR-MCP-003, TR-MCP-LOG-002.
    /// Test data: Generated session/request IDs plus submit/query/dialog payloads serialized as endpoint JSON bodies.
    /// Data rationale: These inputs verify session-log persistence/query behavior and canonical identifier validation paths.
    /// </remarks>
    [Fact]
    public async Task FullLifecycle_Submit_Query_AppendDialog_Requery()
    {
        // Step 1: Submit a session log with one entry
        var sourceType = "LifecycleTest";
        var sessionId = SessionLogEndpointFixture.GenerateSessionId(sourceType);
        var requestId = SessionLogEndpointFixture.GenerateRequestId("full-lifecycle");

        var submitPayload = new
        {
            sourceType,
            sessionId,
            title = "Lifecycle audit test",
            model = "test-lifecycle-model",
            started = DateTimeOffset.UtcNow.AddMinutes(-5).ToString("o"),
            lastUpdated = DateTimeOffset.UtcNow.ToString("o"),
            status = "in_progress",
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
                    queryText = "Full lifecycle test query",
                    queryTitle = "Lifecycle test",
                    response = "Processing...",
                    status = "in_progress",
                    tags = new[] { "lifecycle", "audit" },
                    actions = new[]
                    {
                        new { order = 1, description = "Step 1", type = "create", status = "completed", filePath = "lifecycle.cs" }
                    }
                }
            }
        };

        var submitResponse = await _fixture.Client.PostAsJsonAsync(SessionLogEndpointFixture.SessionLogRoute, submitPayload, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, submitResponse.StatusCode);
        var submitResult = await submitResponse.Content.ReadFromJsonAsync<SubmitResult>(JsonOpts, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(submitResult);
        var sessionDbId = submitResult!.Id;
        Assert.True(sessionDbId > 0);

        // Step 2: Query by agent to find our session
        var queryResponse = await _fixture.Client.GetAsync(
            $"{SessionLogEndpointFixture.SessionLogRoute}?agent={sourceType}", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, queryResponse.StatusCode);
        var queryResult = await queryResponse.Content.ReadFromJsonAsync<QueryResult>(JsonOpts, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(queryResult);
        Assert.True(queryResult!.TotalCount >= 1);
        var ourSession = queryResult.Items!.FirstOrDefault(s => s.SessionId == sessionId);
        Assert.NotNull(ourSession);
        Assert.Equal("Lifecycle audit test", ourSession!.Title);
        Assert.Equal("test-lifecycle-model", ourSession.Model);

        // Step 3: Append dialog items to the entry
        var dialogItems = new[]
        {
            new { timestamp = DateTimeOffset.UtcNow.ToString("o"), role = "model", content = "Reasoning about the approach...", category = "reasoning" },
            new { timestamp = DateTimeOffset.UtcNow.ToString("o"), role = "tool", content = "Executed read_file on lifecycle.cs", category = "tool_call" },
            new { timestamp = DateTimeOffset.UtcNow.ToString("o"), role = "model", content = "Decision: proceed with edit", category = "decision" }
        };

        var dialogRoute = $"{SessionLogEndpointFixture.SessionLogRoute}/{sourceType}/{sessionId}/{requestId}/dialog";
        var dialogResponse = await _fixture.Client.PostAsJsonAsync(dialogRoute, dialogItems, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, dialogResponse.StatusCode);
        var dialogResult = await dialogResponse.Content.ReadFromJsonAsync<DialogAppendResult>(JsonOpts, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(dialogResult);
        Assert.Equal(3, dialogResult!.TotalDialogCount);

        // Step 4: Upsert the session as completed
        var updatePayload = new
        {
            sourceType,
            sessionId,
            title = "Lifecycle audit test",
            model = "test-lifecycle-model",
            started = DateTimeOffset.UtcNow.AddMinutes(-5).ToString("o"),
            lastUpdated = DateTimeOffset.UtcNow.ToString("o"),
            status = "completed",
            turnCount = 1,
            turns = new[]
            {
                new
                {
                    requestId,
                    timestamp = DateTimeOffset.UtcNow.ToString("o"),
                    queryText = "Full lifecycle test query",
                    queryTitle = "Lifecycle test",
                    response = "Completed successfully",
                    status = "completed",
                    tags = new[] { "lifecycle", "audit" },
                    actions = new[]
                    {
                        new { order = 1, description = "Step 1", type = "create", status = "completed", filePath = "lifecycle.cs" },
                        new { order = 2, description = "Step 2 - edit", type = "edit", status = "completed", filePath = "lifecycle.cs" }
                    }
                }
            }
        };

        var updateResponse = await _fixture.Client.PostAsJsonAsync(SessionLogEndpointFixture.SessionLogRoute, updatePayload, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, updateResponse.StatusCode);
        var updateResult = await updateResponse.Content.ReadFromJsonAsync<SubmitResult>(JsonOpts, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(sessionDbId, updateResult!.Id); // Same ID = upsert

        // Step 5: Re-query to verify final state
        var finalQuery = await _fixture.Client.GetAsync(
            $"{SessionLogEndpointFixture.SessionLogRoute}?agent={sourceType}", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, finalQuery.StatusCode);
        var finalResult = await finalQuery.Content.ReadFromJsonAsync<QueryResult>(JsonOpts, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(finalResult);
        var finalSession = finalResult!.Items!.FirstOrDefault(s => s.SessionId == sessionId);
        Assert.NotNull(finalSession);
        Assert.Equal("completed", finalSession!.Status);
    }
}
