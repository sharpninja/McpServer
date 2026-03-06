using System.Net;
using System.Net.Http.Json;
using McpServer.Workspace.Validation.Models;
using Xunit;

namespace McpServer.Workspace.Validation.SequenceTests;

/// <summary>
/// Audit: Full workspace lifecycle sequence test.
/// Exercises all endpoints in the correct operational order as a single scripted flow.
/// This validates that the entire CRUD + process-lifecycle pipeline works end-to-end.
/// </summary>
[Collection("WorkspaceEndpoint")]
public sealed class WorkspaceLifecycleSequenceTests
{
    private readonly WorkspaceEndpointFixture _fixture;
    private readonly ITestOutputHelper _output;

    /// <summary>Initializes a new instance.</summary>
    public WorkspaceLifecycleSequenceTests(WorkspaceEndpointFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    /// <summary>Test method.</summary>
    [Fact]
    public async Task FullLifecycle_CreateThroughDelete()
    {
        var testPath = WorkspaceEndpointFixture.GenerateTestWorkspacePath();
        var testKey = WorkspaceEndpointFixture.EncodeKey(testPath);
        var client = _fixture.Client;
        var route = WorkspaceEndpointFixture.WorkspaceRoute;

        try
        {
            // ── Step 1: List (baseline) ───────────────────────────────────
            _output.WriteLine("Step 1: GET /mcpserver/workspace — List (baseline)");
            var listResponse = await client.GetAsync(route);
            Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
            var listResult = await listResponse.Content.ReadFromJsonAsync<WorkspaceListResult>();
            Assert.NotNull(listResult);
            var baselineCount = listResult.TotalCount;
            _output.WriteLine($"  Baseline workspace count: {baselineCount}");

            // ── Step 2: Create ────────────────────────────────────────────
            _output.WriteLine("Step 2: POST /mcpserver/workspace — Create");
            var createBody = new { WorkspacePath = testPath, Name = "LifecycleAudit" };
            var createResponse = await client.PostAsJsonAsync(route, createBody);
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
            var createResult = await createResponse.Content.ReadFromJsonAsync<WorkspaceMutationResult>();
            Assert.NotNull(createResult);
            Assert.True(createResult.Success, $"Create failed: {createResult.Error}");
            Assert.NotNull(createResult.Workspace);
            _output.WriteLine($"  Created workspace: {createResult.Workspace.Name}");

            // ── Step 3: List (verify count increased) ─────────────────────
            _output.WriteLine("Step 3: GET /mcpserver/workspace — List (verify +1)");
            var list2 = await client.GetAsync(route);
            var list2Result = await list2.Content.ReadFromJsonAsync<WorkspaceListResult>();
            Assert.NotNull(list2Result);
            Assert.Equal(baselineCount + 1, list2Result.TotalCount);

            // ── Step 4: Get ───────────────────────────────────────────────
            _output.WriteLine("Step 4: GET /mcpserver/workspace/{key} — Get");
            var getResponse = await client.GetAsync($"{route}/{testKey}");
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            var dto = await getResponse.Content.ReadFromJsonAsync<WorkspaceDto>();
            Assert.NotNull(dto);
            Assert.Equal(testPath, dto.WorkspacePath);
            Assert.Equal("LifecycleAudit", dto.Name);
            Assert.Equal("docs/todo.yaml", dto.TodoPath);
            _output.WriteLine($"  Retrieved workspace: {dto.Name}");

            // ── Step 5: Update (rename) ───────────────────────────────────
            _output.WriteLine("Step 5: PUT /mcpserver/workspace/{key} — Update name");
            var updateBody = new { Name = "LifecycleAuditRenamed" };
            var updateResponse = await client.PutAsJsonAsync($"{route}/{testKey}", updateBody);
            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
            var updateResult = await updateResponse.Content.ReadFromJsonAsync<WorkspaceMutationResult>();
            Assert.NotNull(updateResult);
            Assert.True(updateResult.Success, $"Update failed: {updateResult.Error}");
            Assert.NotNull(updateResult.Workspace);
            Assert.Equal("LifecycleAuditRenamed", updateResult.Workspace.Name);
            _output.WriteLine("  Renamed to: LifecycleAuditRenamed");

            // ── Step 6: Get (verify update) ───────────────────────────────
            _output.WriteLine("Step 6: GET /mcpserver/workspace/{key} — Verify update");
            var get2 = await client.GetAsync($"{route}/{testKey}");
            var dto2 = await get2.Content.ReadFromJsonAsync<WorkspaceDto>();
            Assert.NotNull(dto2);
            Assert.Equal("LifecycleAuditRenamed", dto2.Name);

            // ── Step 7: Init ──────────────────────────────────────────────
            _output.WriteLine("Step 7: POST /mcpserver/workspace/{key}/init — Initialize");
            var initResponse = await client.PostAsync($"{route}/{testKey}/init", null);
            // May be 200 (success) or 422 (directory doesn't physically exist).
            Assert.True(
                initResponse.StatusCode == HttpStatusCode.OK ||
                initResponse.StatusCode == HttpStatusCode.UnprocessableEntity,
                $"Init returned unexpected {(int)initResponse.StatusCode}.");
            var initResult = await initResponse.Content.ReadFromJsonAsync<WorkspaceInitResult>();
            Assert.NotNull(initResult);
            _output.WriteLine($"  Init success={initResult.Success}, files={initResult.FilesCreated?.Count ?? 0}");

            // ── Step 8: Status (before start) ─────────────────────────────
            _output.WriteLine("Step 8: GET /mcpserver/workspace/{key}/status — Before start");
            var statusBefore = await client.GetAsync($"{route}/{testKey}/status");
            Assert.Equal(HttpStatusCode.OK, statusBefore.StatusCode);
            var statusBeforeDto = await statusBefore.Content.ReadFromJsonAsync<WorkspaceProcessStatus>();
            Assert.NotNull(statusBeforeDto);
            Assert.False(statusBeforeDto.IsRunning, "Should not be running before start.");
            _output.WriteLine($"  IsRunning={statusBeforeDto.IsRunning}");

            // ── Step 9: Start ─────────────────────────────────────────────
            _output.WriteLine("Step 9: POST /mcpserver/workspace/{key}/start — Start");
            var startResponse = await client.PostAsync($"{route}/{testKey}/start", null);
            Assert.Equal(HttpStatusCode.OK, startResponse.StatusCode);
            var startStatus = await startResponse.Content.ReadFromJsonAsync<WorkspaceProcessStatus>();
            Assert.NotNull(startStatus);
            _output.WriteLine($"  Start result: IsRunning={startStatus.IsRunning}, Port={startStatus.Port}, Error={startStatus.Error}");

            // ── Step 10: Status (after start) ─────────────────────────────
            _output.WriteLine("Step 10: GET /mcpserver/workspace/{key}/status — After start");
            var statusAfter = await client.GetAsync($"{route}/{testKey}/status");
            Assert.Equal(HttpStatusCode.OK, statusAfter.StatusCode);
            var statusAfterDto = await statusAfter.Content.ReadFromJsonAsync<WorkspaceProcessStatus>();
            Assert.NotNull(statusAfterDto);
            _output.WriteLine($"  IsRunning={statusAfterDto.IsRunning}, Port={statusAfterDto.Port}");

            // ── Step 11: Stop ─────────────────────────────────────────────
            _output.WriteLine("Step 11: POST /mcpserver/workspace/{key}/stop — Stop");
            var stopResponse = await client.PostAsync($"{route}/{testKey}/stop", null);
            Assert.Equal(HttpStatusCode.OK, stopResponse.StatusCode);
            var stopStatus = await stopResponse.Content.ReadFromJsonAsync<WorkspaceProcessStatus>();
            Assert.NotNull(stopStatus);
            _output.WriteLine($"  Stop result: IsRunning={stopStatus.IsRunning}");

            // ── Step 12: Status (after stop) ──────────────────────────────
            _output.WriteLine("Step 12: GET /mcpserver/workspace/{key}/status — After stop");
            var statusStopped = await client.GetAsync($"{route}/{testKey}/status");
            Assert.Equal(HttpStatusCode.OK, statusStopped.StatusCode);
            var statusStoppedDto = await statusStopped.Content.ReadFromJsonAsync<WorkspaceProcessStatus>();
            Assert.NotNull(statusStoppedDto);
            Assert.False(statusStoppedDto.IsRunning, "Should not be running after stop.");
            _output.WriteLine($"  IsRunning={statusStoppedDto.IsRunning}");

            // ── Step 13: Delete ───────────────────────────────────────────
            _output.WriteLine("Step 13: DELETE /mcpserver/workspace/{key} — Delete");
            var deleteResponse = await client.DeleteAsync($"{route}/{testKey}");
            Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
            var deleteResult = await deleteResponse.Content.ReadFromJsonAsync<WorkspaceMutationResult>();
            Assert.NotNull(deleteResult);
            Assert.True(deleteResult.Success, $"Delete failed: {deleteResult.Error}");
            _output.WriteLine("  Deleted successfully.");

            // ── Step 14: Get (verify 404) ─────────────────────────────────
            _output.WriteLine("Step 14: GET /mcpserver/workspace/{key} — Verify gone (404)");
            var getGone = await client.GetAsync($"{route}/{testKey}");
            Assert.Equal(HttpStatusCode.NotFound, getGone.StatusCode);
            _output.WriteLine("  Confirmed: workspace returns 404 after deletion.");

            // ── Step 15: List (verify count restored) ─────────────────────
            _output.WriteLine("Step 15: GET /mcpserver/workspace — Verify count restored");
            var listFinal = await client.GetAsync(route);
            var listFinalResult = await listFinal.Content.ReadFromJsonAsync<WorkspaceListResult>();
            Assert.NotNull(listFinalResult);
            Assert.Equal(baselineCount, listFinalResult.TotalCount);
            _output.WriteLine($"  Final count: {listFinalResult.TotalCount} (matches baseline).");

            _output.WriteLine("✅ Full lifecycle sequence completed successfully!");
        }
        catch
        {
            // Best-effort cleanup on failure.
            try
            {
                await client.PostAsync($"{route}/{testKey}/stop", null);
                await client.DeleteAsync($"{route}/{testKey}");
            }
            catch { /* swallow cleanup errors */ }
            throw;
        }
    }
}
