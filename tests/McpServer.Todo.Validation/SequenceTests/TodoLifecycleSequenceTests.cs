using System.Net;
using System.Net.Http.Json;
using McpServer.Todo.Validation.Models;
using Xunit;

namespace McpServer.Todo.Validation.SequenceTests;

/// <summary>
/// Audit: Full TODO lifecycle sequence test.
/// Exercises all endpoints in the correct operational order as a single scripted flow.
/// </summary>
[Collection("TodoEndpoint")]
public sealed class TodoLifecycleSequenceTests
{
    private readonly TodoEndpointFixture _fixture;
    private readonly ITestOutputHelper _output;

    /// <summary>
    /// Initializes a new instance of TodoLifecycleSequenceTests.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-002, TEST-MCP-074, FR-MCP-002, TR-MCP-TODO-002.
    /// Test data: Generated TODO IDs and endpoint payloads for create/update/query/error combinations.
    /// Data rationale: These inputs verify TODO endpoint contract stability, mutation behavior, and validation/error handling paths.
    /// </remarks>
    public TodoLifecycleSequenceTests(TodoEndpointFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    /// <summary>
    /// Validates the <c>FullLifecycle_CreateThroughDelete</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-002, TEST-MCP-074, FR-MCP-002, TR-MCP-TODO-002.
    /// Test data: Generated TODO IDs and endpoint payloads for create/update/query/error combinations.
    /// Data rationale: These inputs verify TODO endpoint contract stability, mutation behavior, and validation/error handling paths.
    /// </remarks>
    [Fact]
    public async Task FullLifecycle_CreateThroughDelete()
    {
        var testId = TodoEndpointFixture.GenerateTestId();
        var client = _fixture.Client;
        var route = TodoEndpointFixture.TodoRoute;

        try
        {
            // ── Step 1: Query (baseline) ──────────────────────────────────
            _output.WriteLine("Step 1: GET /mcpserver/todo — Query (baseline)");
            var listResponse = await client.GetAsync(route);
            Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
            var listResult = await listResponse.Content.ReadFromJsonAsync<TodoQueryResult>();
            Assert.NotNull(listResult);
            var baselineCount = listResult.TotalCount;
            _output.WriteLine($"  Baseline TODO count: {baselineCount}");

            // ── Step 2: Create ────────────────────────────────────────────
            _output.WriteLine("Step 2: POST /mcpserver/todo — Create");
            var createBody = new
            {
                Id = testId,
                Title = "LifecycleAuditTodo",
                Section = "mvp-support",
                Priority = "high",
                Estimate = "1-2 hours",
                Description = new[] { "Lifecycle audit test item" },
                TechnicalDetails = new[] { "Created by TodoLifecycleSequenceTests" },
                ImplementationTasks = new[]
                {
                    new { Task = "Task A", Done = false },
                    new { Task = "Task B", Done = false }
                }
            };
            var createResponse = await client.PostAsJsonAsync(route, createBody);
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
            var createResult = await createResponse.Content.ReadFromJsonAsync<TodoMutationResult>();
            Assert.NotNull(createResult);
            Assert.True(createResult.Success, $"Create failed: {createResult.Error}");
            Assert.NotNull(createResult.Item);
            _output.WriteLine($"  Created TODO: {createResult.Item.Id}");

            // ── Step 3: Query (verify count increased) ────────────────────
            _output.WriteLine("Step 3: GET /mcpserver/todo — Query (verify +1)");
            var list2 = await client.GetAsync(route);
            var list2Result = await list2.Content.ReadFromJsonAsync<TodoQueryResult>();
            Assert.NotNull(list2Result);
            Assert.Equal(baselineCount + 1, list2Result.TotalCount);

            // ── Step 4: Get by ID ─────────────────────────────────────────
            _output.WriteLine("Step 4: GET /mcpserver/todo/{id} — Get");
            var getResponse = await client.GetAsync($"{route}/{Uri.EscapeDataString(testId)}");
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            var item = await getResponse.Content.ReadFromJsonAsync<TodoFlatItem>();
            Assert.NotNull(item);
            Assert.Equal(testId, item.Id);
            Assert.Equal("LifecycleAuditTodo", item.Title);
            Assert.Equal("mvp-support", item.Section);
            Assert.Equal("high", item.Priority);
            Assert.False(item.Done);
            Assert.NotNull(item.ImplementationTasks);
            Assert.Equal(2, item.ImplementationTasks.Count);
            _output.WriteLine($"  Retrieved: {item.Title} ({item.Priority})");

            // ── Step 5: Query by ID filter ────────────────────────────────
            _output.WriteLine("Step 5: GET /mcpserver/todo?id={id} — Query by ID filter");
            var queryById = await client.GetAsync($"{route}?id={Uri.EscapeDataString(testId)}");
            var queryByIdResult = await queryById.Content.ReadFromJsonAsync<TodoQueryResult>();
            Assert.NotNull(queryByIdResult);
            Assert.True(queryByIdResult.TotalCount >= 1);
            Assert.Contains(queryByIdResult.Items, i => i.Id == testId);

            // ── Step 6: Update (rename) ───────────────────────────────────
            _output.WriteLine("Step 6: PUT /mcpserver/todo/{id} — Update title");
            var updateBody = new { Title = "LifecycleAuditRenamed" };
            var updateResponse = await client.PutAsJsonAsync($"{route}/{Uri.EscapeDataString(testId)}", updateBody);
            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
            var updateResult = await updateResponse.Content.ReadFromJsonAsync<TodoMutationResult>();
            Assert.NotNull(updateResult);
            Assert.True(updateResult.Success, $"Update failed: {updateResult.Error}");
            Assert.NotNull(updateResult.Item);
            Assert.Equal("LifecycleAuditRenamed", updateResult.Item.Title);
            _output.WriteLine("  Renamed to: LifecycleAuditRenamed");

            // ── Step 7: Get (verify update) ───────────────────────────────
            _output.WriteLine("Step 7: GET /mcpserver/todo/{id} — Verify update");
            var get2 = await client.GetAsync($"{route}/{Uri.EscapeDataString(testId)}");
            var item2 = await get2.Content.ReadFromJsonAsync<TodoFlatItem>();
            Assert.NotNull(item2);
            Assert.Equal("LifecycleAuditRenamed", item2.Title);

            // ── Step 8: Update (mark done) ────────────────────────────────
            _output.WriteLine("Step 8: PUT /mcpserver/todo/{id} — Mark done");
            var doneBody = new { Done = true, CompletedDate = "2026-02-21", DoneSummary = "Audit complete" };
            var doneResponse = await client.PutAsJsonAsync($"{route}/{Uri.EscapeDataString(testId)}", doneBody);
            Assert.Equal(HttpStatusCode.OK, doneResponse.StatusCode);
            var doneResult = await doneResponse.Content.ReadFromJsonAsync<TodoMutationResult>();
            Assert.NotNull(doneResult);
            Assert.True(doneResult.Success);
            Assert.NotNull(doneResult.Item);
            Assert.True(doneResult.Item.Done);
            _output.WriteLine("  Marked done.");

            // ── Step 9: Query (done=true filter) ──────────────────────────
            _output.WriteLine("Step 9: GET /mcpserver/todo?done=true — Verify in done list");
            var doneQuery = await client.GetAsync($"{route}?done=true");
            var doneQueryResult = await doneQuery.Content.ReadFromJsonAsync<TodoQueryResult>();
            Assert.NotNull(doneQueryResult);
            Assert.Contains(doneQueryResult.Items, i => i.Id == testId);

            // ── Step 10: Requirements (if Copilot available) ──────────────
            _output.WriteLine("Step 10: POST /mcpserver/todo/{id}/requirements — Analyze");
            var reqResponse = await client.PostAsync(
                $"{route}/{Uri.EscapeDataString(testId)}/requirements", null);
            Assert.True(
                reqResponse.StatusCode == HttpStatusCode.OK ||
                reqResponse.StatusCode == HttpStatusCode.UnprocessableEntity,
                $"Requirements returned unexpected {(int)reqResponse.StatusCode}.");
            var reqResult = await reqResponse.Content.ReadFromJsonAsync<RequirementsAnalysisResult>();
            Assert.NotNull(reqResult);
            _output.WriteLine($"  Requirements success={reqResult.Success}, error={reqResult.Error}");

            // ── Step 11: Delete ───────────────────────────────────────────
            _output.WriteLine("Step 11: DELETE /mcpserver/todo/{id} — Delete");
            var deleteResponse = await client.DeleteAsync($"{route}/{Uri.EscapeDataString(testId)}");
            Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
            var deleteResult = await deleteResponse.Content.ReadFromJsonAsync<TodoMutationResult>();
            Assert.NotNull(deleteResult);
            Assert.True(deleteResult.Success, $"Delete failed: {deleteResult.Error}");
            _output.WriteLine("  Deleted successfully.");

            // ── Step 12: Get (verify 404) ─────────────────────────────────
            _output.WriteLine("Step 12: GET /mcpserver/todo/{id} — Verify gone (404)");
            var getGone = await client.GetAsync($"{route}/{Uri.EscapeDataString(testId)}");
            Assert.Equal(HttpStatusCode.NotFound, getGone.StatusCode);
            _output.WriteLine("  Confirmed: TODO returns 404 after deletion.");

            // ── Step 13: Query (verify count restored) ────────────────────
            _output.WriteLine("Step 13: GET /mcpserver/todo — Verify count restored");
            var listFinal = await client.GetAsync(route);
            var listFinalResult = await listFinal.Content.ReadFromJsonAsync<TodoQueryResult>();
            Assert.NotNull(listFinalResult);
            Assert.Equal(baselineCount, listFinalResult.TotalCount);
            _output.WriteLine($"  Final count: {listFinalResult.TotalCount} (matches baseline).");

            _output.WriteLine("✅ Full TODO lifecycle sequence completed successfully!");
        }
        catch
        {
            // Best-effort cleanup on failure.
            try { await client.DeleteAsync($"{route}/{Uri.EscapeDataString(testId)}"); }
            catch { /* swallow */ }
            throw;
        }
    }
}
