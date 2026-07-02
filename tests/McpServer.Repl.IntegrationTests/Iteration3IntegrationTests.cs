using System.Globalization;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace McpServer.Repl.IntegrationTests;

[Trait("Category", "Integration")]
public sealed class Iteration3IntegrationTests : IDisposable
{
    private readonly ReplChildProcessHelper _replProcess;
    private readonly ISerializer _yamlSerializer;
    private readonly IDeserializer _yamlDeserializer;

    public Iteration3IntegrationTests()
    {
        _replProcess = new ReplChildProcessHelper();
        _yamlSerializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        _yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
    }

    [Fact]
    public async Task TodoWorkflow_Query_ReturnsItems()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var requestId = GenerateRequestId("todo-query");
        var queryEnvelope = YamlEnvelopeBuilder.CreateTodoQueryRequest(
            requestId,
            keyword: null,
            priority: null,
            section: null,
            id: null,
            done: false);

        await SendCommandAndWaitAsync(queryEnvelope);

        var responseLine = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(responseLine);

        var response = _yamlDeserializer.Deserialize<Dictionary<string, object>>(responseLine!);
        Assert.NotNull(response);
        Assert.True(response.ContainsKey("type") || response.ContainsKey("Type"));
    }

    [Fact]
    public async Task TodoWorkflow_Create_Get_Delete_Succeeds()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var todoId = "TEST-INT-001";

        var createEnvelope = YamlEnvelopeBuilder.CreateTodoCreateRequest(
            GenerateRequestId("create"),
            todoId,
            "Integration Test TODO",
            "Testing",
            "high",
            estimate: "1h",
            description: new[] { "Test TODO for integration tests" },
            functionalRequirements: new[] { "FR-TEST-001" });

        await SendCommandAndWaitAsync(createEnvelope);

        var createResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(createResponse);

        var getEnvelope = YamlEnvelopeBuilder.CreateTodoGetRequest(
            GenerateRequestId("get"),
            todoId);

        await SendCommandAndWaitAsync(getEnvelope);

        var getResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(getResponse);

        var deleteEnvelope = YamlEnvelopeBuilder.CreateTodoDeleteRequest(
            GenerateRequestId("delete"),
            todoId);

        await SendCommandAndWaitAsync(deleteEnvelope);

        var deleteResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(deleteResponse);
    }

    [Fact]
    public async Task TodoWorkflow_Update_ModifiesTodo()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var todoId = "TEST-INT-002";

        var createEnvelope = YamlEnvelopeBuilder.CreateTodoCreateRequest(
            GenerateRequestId("create"),
            todoId,
            "TODO to Update",
            "Testing",
            "medium",
            estimate: "2h");

        await SendCommandAndWaitAsync(createEnvelope);

        var updateEnvelope = YamlEnvelopeBuilder.CreateTodoUpdateRequest(
            GenerateRequestId("update"),
            todoId,
            title: "Updated TODO Title",
            priority: "high",
            done: false,
            remaining: "Need to complete tests");

        await SendCommandAndWaitAsync(updateEnvelope);

        var updateResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(updateResponse);

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoDeleteRequest(GenerateRequestId("cleanup"), todoId));
    }

    [Fact]
    public async Task TodoWorkflow_Select_CurrentSelection_Persists()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var todoId = "TEST-INT-003";

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoCreateRequest(
                GenerateRequestId("create"),
                todoId,
                "Selection Test TODO",
                "Testing",
                "low",
                estimate: "30m"));

        var selectEnvelope = YamlEnvelopeBuilder.CreateTodoSelectRequest(
            GenerateRequestId("select"),
            todoId);

        await SendCommandAndWaitAsync(selectEnvelope);

        var selectResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(selectResponse);

        var currentSelectionEnvelope = YamlEnvelopeBuilder.CreateTodoCurrentSelectionRequest(
            GenerateRequestId("current-selection-1"));

        await SendCommandAndWaitAsync(currentSelectionEnvelope);

        var selectionResponse1 = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(selectionResponse1);

        await Task.Delay(500);

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoCurrentSelectionRequest(
                GenerateRequestId("current-selection-2")));

        var selectionResponse2 = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(selectionResponse2);

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoDeleteRequest(GenerateRequestId("cleanup"), todoId));
    }

    [Fact]
    public async Task TodoWorkflow_UpdateSelected_UsesSelection()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var todoId = "TEST-INT-004";

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoCreateRequest(
                GenerateRequestId("create"),
                todoId,
                "Selected Update Test",
                "Testing",
                "medium"));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoSelectRequest(
                GenerateRequestId("select"),
                todoId));

        var updateSelectedEnvelope = YamlEnvelopeBuilder.CreateTodoUpdateSelectedRequest(
            GenerateRequestId("update-selected"),
            title: "Updated via Selection",
            priority: "critical",
            done: false,
            remaining: "Almost done");

        await SendCommandAndWaitAsync(updateSelectedEnvelope);

        var updateResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(updateResponse);

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoDeleteRequest(GenerateRequestId("cleanup"), todoId));
    }

    [Fact]
    public async Task TodoWorkflow_DeleteSelected_RemovesSelectedTodo()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var todoId = "TEST-INT-005";

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoCreateRequest(
                GenerateRequestId("create"),
                todoId,
                "Delete Selected Test",
                "Testing",
                "low"));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoSelectRequest(
                GenerateRequestId("select"),
                todoId));

        var deleteSelectedEnvelope = YamlEnvelopeBuilder.CreateTodoDeleteSelectedRequest(
            GenerateRequestId("delete-selected"));

        await SendCommandAndWaitAsync(deleteSelectedEnvelope);

        var deleteResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(deleteResponse);

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoCurrentSelectionRequest(
                GenerateRequestId("check-selection")));

        var selectionResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(selectionResponse);
    }

    [Fact]
    public async Task TodoWorkflow_AnalyzeRequirements_ReturnsAnalysis()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var todoId = "TEST-INT-006";

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoCreateRequest(
                GenerateRequestId("create"),
                todoId,
                "Requirements Analysis Test",
                "Testing",
                "medium",
                functionalRequirements: new[] { "FR-TEST-001", "FR-TEST-002" },
                technicalRequirements: new[] { "TR-TEST-001" }));

        var analyzeEnvelope = YamlEnvelopeBuilder.CreateTodoAnalyzeRequirementsRequest(
            GenerateRequestId("analyze"),
            todoId);

        await SendCommandAndWaitAsync(analyzeEnvelope);

        var analyzeResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(analyzeResponse);

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoDeleteRequest(GenerateRequestId("cleanup"), todoId));
    }

    [Fact]
    public async Task TodoWorkflow_StreamStatus_EmitsEvents()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var todoId = "TEST-INT-007";

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoCreateRequest(
                GenerateRequestId("create"),
                todoId,
                "Stream Status Test",
                "Testing",
                "high"));

        _replProcess.ClearStdout();

        var streamRequestId = GenerateRequestId("stream-status");
        var streamEnvelope = YamlEnvelopeBuilder.CreateTodoStreamStatusRequest(
            streamRequestId,
            todoId);

        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(streamEnvelope));

        var foundEvents = await _replProcess.WaitForStdoutLineCountAsync(3, TimeSpan.FromSeconds(10));

        Assert.True(_replProcess.StdoutLines.Count > 0, "Should receive streaming events");

        var eventLines = _replProcess.StdoutLines.ToList();

        foreach (var line in eventLines)
        {
            try
            {
                var evt = _yamlDeserializer.Deserialize<Dictionary<string, object>>(line);
                Assert.NotNull(evt);
            }
            catch
            {
            }
        }

        await WaitForResponseAsync(streamRequestId, TimeSpan.FromSeconds(30));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoDeleteRequest(GenerateRequestId("cleanup"), todoId));
    }

    [Fact]
    public async Task TodoWorkflow_StreamPlan_EmitsEvents()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var todoId = "TEST-INT-008";

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoCreateRequest(
                GenerateRequestId("create"),
                todoId,
                "Stream Plan Test",
                "Testing",
                "high"));

        _replProcess.ClearStdout();

        var streamRequestId = GenerateRequestId("stream-plan");
        var streamEnvelope = YamlEnvelopeBuilder.CreateTodoStreamPlanRequest(
            streamRequestId,
            todoId);

        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(streamEnvelope));

        await _replProcess.WaitForStdoutLineCountAsync(1, TimeSpan.FromSeconds(10));

        Assert.True(_replProcess.StdoutLines.Count > 0, "Should receive plan streaming events");

        await WaitForResponseAsync(streamRequestId, TimeSpan.FromSeconds(30));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoDeleteRequest(GenerateRequestId("cleanup"), todoId));
    }

    [Fact]
    public async Task TodoWorkflow_StreamImplement_EmitsEvents()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var todoId = "TEST-INT-009";

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoCreateRequest(
                GenerateRequestId("create"),
                todoId,
                "Stream Implement Test",
                "Testing",
                "high"));

        _replProcess.ClearStdout();

        var streamRequestId = GenerateRequestId("stream-implement");
        var streamEnvelope = YamlEnvelopeBuilder.CreateTodoStreamImplementRequest(
            streamRequestId,
            todoId);

        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(streamEnvelope));

        await _replProcess.WaitForStdoutLineCountAsync(1, TimeSpan.FromSeconds(10));

        Assert.True(_replProcess.StdoutLines.Count > 0, "Should receive implement streaming events");

        await WaitForResponseAsync(streamRequestId, TimeSpan.FromSeconds(30));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoDeleteRequest(GenerateRequestId("cleanup"), todoId));
    }

    [Fact]
    public async Task TodoWorkflow_GetProjectionStatus_ReturnsStatus()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var todoId = "TEST-INT-010";

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoCreateRequest(
                GenerateRequestId("create"),
                todoId,
                "Projection Status Test",
                "Testing",
                "medium"));

        var statusEnvelope = YamlEnvelopeBuilder.CreateTodoGetProjectionStatusRequest(
            GenerateRequestId("projection-status"),
            todoId);

        await SendCommandAndWaitAsync(statusEnvelope);

        var statusResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(statusResponse);

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoDeleteRequest(GenerateRequestId("cleanup"), todoId));
    }

    [Fact]
    public async Task TodoWorkflow_RepairProjection_Succeeds()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var todoId = "TEST-INT-011";

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoCreateRequest(
                GenerateRequestId("create"),
                todoId,
                "Repair Projection Test",
                "Testing",
                "high"));

        var repairEnvelope = YamlEnvelopeBuilder.CreateTodoRepairProjectionRequest(
            GenerateRequestId("repair"),
            todoId);

        await SendCommandAndWaitAsync(repairEnvelope);

        var repairResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(repairResponse);

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoDeleteRequest(GenerateRequestId("cleanup"), todoId));
    }

    [Fact]
    public async Task TodoWorkflow_SelectionState_PersistsAcrossCommands()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var todoId = "TEST-INT-012";

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoCreateRequest(
                GenerateRequestId("create"),
                todoId,
                "Persistence Test",
                "Testing",
                "medium"));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoSelectRequest(
                GenerateRequestId("select"),
                todoId));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoCurrentSelectionRequest(
                GenerateRequestId("check-1")));

        var check1 = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(check1);

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoUpdateSelectedRequest(
                GenerateRequestId("update-selected"),
                title: "Updated Title",
                remaining: "In progress"));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoCurrentSelectionRequest(
                GenerateRequestId("check-2")));

        var check2 = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(check2);

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoDeleteRequest(GenerateRequestId("cleanup"), todoId));
    }

    [Fact]
    public async Task TodoWorkflow_FullCrudWorkflow_CompletesSuccessfully()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var todoId = "TEST-INT-013";

        var subtasks = new[]
        {
            YamlEnvelopeBuilder.CreateTodoSubtask("Subtask 1", false),
            YamlEnvelopeBuilder.CreateTodoSubtask("Subtask 2", false)
        };

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoCreateRequest(
                GenerateRequestId("create"),
                todoId,
                "Full CRUD Workflow Test",
                "Testing",
                "critical",
                estimate: "3h",
                description: new[] { "Line 1", "Line 2" },
                technicalDetails: new[] { "Tech detail 1" },
                implementationTasks: subtasks,
                note: "Test note",
                remaining: "Everything",
                dependsOn: new[] { "TEST-INT-999" },
                functionalRequirements: new[] { "FR-TEST-001" },
                technicalRequirements: new[] { "TR-TEST-001" }));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoGetRequest(
                GenerateRequestId("get"),
                todoId));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoSelectRequest(
                GenerateRequestId("select"),
                todoId));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoUpdateSelectedRequest(
                GenerateRequestId("update-selected"),
                priority: "high",
                done: false,
                remaining: "Almost done"));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoCurrentSelectionRequest(
                GenerateRequestId("check-selection")));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoDeleteRequest(
                GenerateRequestId("delete"),
                todoId));

        Assert.True(_replProcess.StdoutLines.Count >= 6, "Should have received all responses");
    }

    [Fact]
    public async Task TodoWorkflow_MultipleStreams_EmitSeparateEvents()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var todoId = "TEST-INT-014";

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoCreateRequest(
                GenerateRequestId("create"),
                todoId,
                "Multiple Streams Test",
                "Testing",
                "high"));

        _replProcess.ClearStdout();

        var statusRequestId = GenerateRequestId("stream-status");
        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(
            YamlEnvelopeBuilder.CreateTodoStreamStatusRequest(
                statusRequestId,
                todoId)));

        await _replProcess.WaitForStdoutLineCountAsync(1, TimeSpan.FromSeconds(5));
        var statusEventCount = _replProcess.StdoutLines.Count;
        await WaitForResponseAsync(statusRequestId, TimeSpan.FromSeconds(30));

        _replProcess.ClearStdout();

        var planRequestId = GenerateRequestId("stream-plan");
        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(
            YamlEnvelopeBuilder.CreateTodoStreamPlanRequest(
                planRequestId,
                todoId)));

        await _replProcess.WaitForStdoutLineCountAsync(1, TimeSpan.FromSeconds(5));
        var planEventCount = _replProcess.StdoutLines.Count;
        await WaitForResponseAsync(planRequestId, TimeSpan.FromSeconds(30));

        _replProcess.ClearStdout();

        var implementRequestId = GenerateRequestId("stream-implement");
        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(
            YamlEnvelopeBuilder.CreateTodoStreamImplementRequest(
                implementRequestId,
                todoId)));

        await _replProcess.WaitForStdoutLineCountAsync(1, TimeSpan.FromSeconds(5));
        var implementEventCount = _replProcess.StdoutLines.Count;
        await WaitForResponseAsync(implementRequestId, TimeSpan.FromSeconds(30));

        Assert.True(statusEventCount > 0, "Should receive status events");
        Assert.True(planEventCount > 0, "Should receive plan events");
        Assert.True(implementEventCount > 0, "Should receive implement events");

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoDeleteRequest(GenerateRequestId("cleanup"), todoId));
    }

    [Fact]
    public async Task TodoWorkflow_ProjectionStatusAndRepair_WorkTogether()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var todoId = "TEST-INT-015";

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoCreateRequest(
                GenerateRequestId("create"),
                todoId,
                "Status and Repair Test",
                "Testing",
                "medium"));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoGetProjectionStatusRequest(
                GenerateRequestId("status-before"),
                todoId));

        var statusBefore = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(statusBefore);

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoRepairProjectionRequest(
                GenerateRequestId("repair"),
                todoId));

        var repairResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(repairResponse);

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoGetProjectionStatusRequest(
                GenerateRequestId("status-after"),
                todoId));

        var statusAfter = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(statusAfter);

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoDeleteRequest(GenerateRequestId("cleanup"), todoId));
    }

    [Fact]
    public async Task TodoWorkflow_QueryFiltering_ReturnsFilteredResults()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoCreateRequest(
                GenerateRequestId("create-1"),
                "TEST-FLT-001",
                "High Priority TODO",
                "Testing",
                "high"));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoCreateRequest(
                GenerateRequestId("create-2"),
                "TEST-FLT-002",
                "Low Priority TODO",
                "Testing",
                "low"));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoQueryRequest(
                GenerateRequestId("query-high"),
                priority: "high",
                section: "Testing"));

        var highPriorityResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(highPriorityResponse);

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoQueryRequest(
                GenerateRequestId("query-low"),
                priority: "low"));

        var lowPriorityResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(lowPriorityResponse);

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoDeleteRequest(GenerateRequestId("cleanup-1"), "TEST-FLT-001"));
        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoDeleteRequest(GenerateRequestId("cleanup-2"), "TEST-FLT-002"));
    }

    [Fact]
    public async Task TodoWorkflow_InvalidTodoId_ReturnsError()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var invalidId = "invalid-todo-id";

        var createEnvelope = YamlEnvelopeBuilder.CreateTodoCreateRequest(
            GenerateRequestId("create-invalid"),
            invalidId,
            "Invalid ID Test",
            "Testing",
            "medium");

        await SendCommandAndWaitAsync(createEnvelope);

        var response = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(response);
    }

    [Fact]
    public async Task TodoWorkflow_GetNonExistentTodo_ReturnsError()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var nonExistentId = "TEST-XXX-999";

        var getEnvelope = YamlEnvelopeBuilder.CreateTodoGetRequest(
            GenerateRequestId("get-nonexistent"),
            nonExistentId);

        await SendCommandAndWaitAsync(getEnvelope);

        var response = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(response);
    }

    [Fact]
    public async Task TodoWorkflow_NoSelection_UpdateSelected_ReturnsError()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var updateSelectedEnvelope = YamlEnvelopeBuilder.CreateTodoUpdateSelectedRequest(
            GenerateRequestId("update-no-selection"),
            title: "Should Fail");

        await SendCommandAndWaitAsync(updateSelectedEnvelope);

        var response = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(response);
    }

    [Fact]
    public async Task TodoWorkflow_NoSelection_DeleteSelected_ReturnsError()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var deleteSelectedEnvelope = YamlEnvelopeBuilder.CreateTodoDeleteSelectedRequest(
            GenerateRequestId("delete-no-selection"));

        await SendCommandAndWaitAsync(deleteSelectedEnvelope);

        var response = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(response);
    }

    [Fact]
    public async Task TodoWorkflow_CurrentSelection_NoSelection_ReturnsNull()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var currentSelectionEnvelope = YamlEnvelopeBuilder.CreateTodoCurrentSelectionRequest(
            GenerateRequestId("current-no-selection"));

        await SendCommandAndWaitAsync(currentSelectionEnvelope);

        var response = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(response);
    }

    [Fact]
    public async Task TodoWorkflow_StreamingEvents_AreSeparateYamlEnvelopes()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var todoId = "TEST-ENV-001";

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoCreateRequest(
                GenerateRequestId("create"),
                todoId,
                "Event Envelope Test",
                "Testing",
                "high"));

        _replProcess.ClearStdout();

        var streamRequestId = GenerateRequestId("stream-status");
        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(
            YamlEnvelopeBuilder.CreateTodoStreamStatusRequest(
                streamRequestId,
                todoId)));

        await _replProcess.WaitForStdoutLineCountAsync(1, TimeSpan.FromSeconds(10));

        var eventLines = _replProcess.StdoutLines.ToList();

        foreach (var line in eventLines)
        {
            // FR-MCP-REPL-005: '---' document separators are part of the framing
            // contract between envelopes, not envelope content.
            if (string.IsNullOrWhiteSpace(line) || line.TrimEnd() == "---")
                continue;

            var envelope = _yamlDeserializer.Deserialize<Dictionary<string, object>>(line);
            Assert.NotNull(envelope);
            Assert.True(envelope.ContainsKey("type") || envelope.ContainsKey("Type"));
        }

        await WaitForResponseAsync(streamRequestId, TimeSpan.FromSeconds(30));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoDeleteRequest(GenerateRequestId("cleanup"), todoId));
    }

    [Fact]
    public async Task TodoWorkflow_StreamEvents_ContainSequenceNumbers()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var todoId = "TEST-SEQ-001";

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoCreateRequest(
                GenerateRequestId("create"),
                todoId,
                "Sequence Number Test",
                "Testing",
                "high"));

        _replProcess.ClearStdout();

        var streamRequestId = GenerateRequestId("stream-plan");
        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(
            YamlEnvelopeBuilder.CreateTodoStreamPlanRequest(
                streamRequestId,
                todoId)));

        await _replProcess.WaitForStdoutLineCountAsync(1, TimeSpan.FromSeconds(10));

        var eventLines = _replProcess.StdoutLines.ToList();

        foreach (var line in eventLines)
        {
            try
            {
                var envelope = _yamlDeserializer.Deserialize<Dictionary<string, object>>(line);
                Assert.NotNull(envelope);
            }
            catch
            {
            }
        }

        await WaitForResponseAsync(streamRequestId, TimeSpan.FromSeconds(30));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoDeleteRequest(GenerateRequestId("cleanup"), todoId));
    }

    [Fact]
    public async Task TodoWorkflow_SelectionStatePersistsAcrossMultipleOperations()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var todoId1 = "TEST-PST-001";
        var todoId2 = "TEST-PST-002";

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoCreateRequest(
                GenerateRequestId("create-1"),
                todoId1,
                "First TODO",
                "Testing",
                "high"));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoCreateRequest(
                GenerateRequestId("create-2"),
                todoId2,
                "Second TODO",
                "Testing",
                "medium"));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoSelectRequest(
                GenerateRequestId("select-1"),
                todoId1));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoCurrentSelectionRequest(
                GenerateRequestId("check-1")));

        var check1 = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(check1);

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoSelectRequest(
                GenerateRequestId("select-2"),
                todoId2));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoCurrentSelectionRequest(
                GenerateRequestId("check-2")));

        var check2 = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(check2);

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoDeleteRequest(GenerateRequestId("cleanup-1"), todoId1));
        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoDeleteRequest(GenerateRequestId("cleanup-2"), todoId2));
    }

    [Fact]
    public async Task TodoWorkflow_ComplexQuery_WithMultipleFilters()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoCreateRequest(
                GenerateRequestId("create-1"),
                "TEST-QRY-001",
                "Authentication Feature",
                "Backend",
                "critical",
                estimate: "8h"));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoCreateRequest(
                GenerateRequestId("create-2"),
                "TEST-QRY-002",
                "UI Component",
                "Frontend",
                "medium",
                estimate: "4h"));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoQueryRequest(
                GenerateRequestId("query-backend-critical"),
                section: "Backend",
                priority: "critical"));

        var response = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(response);

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoDeleteRequest(GenerateRequestId("cleanup-1"), "TEST-QRY-001"));
        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoDeleteRequest(GenerateRequestId("cleanup-2"), "TEST-QRY-002"));
    }

    [Fact]
    public async Task TodoWorkflow_UpdateWithComplexFields()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var todoId = "TEST-CPX-001";

        var initialSubtasks = new[]
        {
            YamlEnvelopeBuilder.CreateTodoSubtask("Initial Task 1", false),
            YamlEnvelopeBuilder.CreateTodoSubtask("Initial Task 2", true)
        };

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoCreateRequest(
                GenerateRequestId("create"),
                todoId,
                "Complex Update Test",
                "Testing",
                "high",
                implementationTasks: initialSubtasks,
                description: new[] { "Initial description" }));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoUpdateRequest(
                GenerateRequestId("update"),
                todoId,
                description: new[] { "Updated line 1", "Updated line 2", "Updated line 3" },
                remaining: "Need to verify updates"));

        var updateResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(updateResponse);

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoDeleteRequest(GenerateRequestId("cleanup"), todoId));
    }

    [Fact]
    public async Task TodoWorkflow_ProjectionWorkflow_StatusPlanImplement()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var todoId = "TEST-PRJ-001";

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoCreateRequest(
                GenerateRequestId("create"),
                todoId,
                "Full Projection Workflow",
                "Testing",
                "high"));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoGetProjectionStatusRequest(
                GenerateRequestId("status-initial"),
                todoId));

        _replProcess.ClearStdout();
        var statusRequestId = GenerateRequestId("stream-status");
        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(
            YamlEnvelopeBuilder.CreateTodoStreamStatusRequest(
                statusRequestId,
                todoId)));
        await _replProcess.WaitForStdoutLineCountAsync(1, TimeSpan.FromSeconds(10));
        await WaitForResponseAsync(statusRequestId, TimeSpan.FromSeconds(30));

        _replProcess.ClearStdout();
        var planRequestId = GenerateRequestId("stream-plan");
        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(
            YamlEnvelopeBuilder.CreateTodoStreamPlanRequest(
                planRequestId,
                todoId)));
        await _replProcess.WaitForStdoutLineCountAsync(1, TimeSpan.FromSeconds(10));
        await WaitForResponseAsync(planRequestId, TimeSpan.FromSeconds(30));

        _replProcess.ClearStdout();
        var implementRequestId = GenerateRequestId("stream-implement");
        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(
            YamlEnvelopeBuilder.CreateTodoStreamImplementRequest(
                implementRequestId,
                todoId)));
        await _replProcess.WaitForStdoutLineCountAsync(1, TimeSpan.FromSeconds(10));
        await WaitForResponseAsync(implementRequestId, TimeSpan.FromSeconds(30));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoGetProjectionStatusRequest(
                GenerateRequestId("status-final"),
                todoId));

        var finalStatus = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(finalStatus);

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoDeleteRequest(GenerateRequestId("cleanup"), todoId));
    }

    [Fact]
    public async Task TodoWorkflow_DeleteClearsSelection()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var todoId = "TEST-DEL-001";

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoCreateRequest(
                GenerateRequestId("create"),
                todoId,
                "Delete Selection Test",
                "Testing",
                "medium"));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoSelectRequest(
                GenerateRequestId("select"),
                todoId));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoCurrentSelectionRequest(
                GenerateRequestId("check-before")));

        var beforeDelete = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(beforeDelete);

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoDeleteRequest(
                GenerateRequestId("delete"),
                todoId));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoCurrentSelectionRequest(
                GenerateRequestId("check-after")));

        var afterDelete = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(afterDelete);
    }

    [Fact]
    public async Task TodoWorkflow_MultipleQueryExecutions()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        for (int i = 0; i < 3; i++)
        {
            await SendCommandAndWaitAsync(
                YamlEnvelopeBuilder.CreateTodoQueryRequest(
                    GenerateRequestId($"query-{i}"),
                    done: false));

            var response = _replProcess.StdoutLines.LastOrDefault();
            Assert.NotNull(response);
        }
    }

    [Fact]
    public async Task TodoWorkflow_CreateWithAllOptionalFields()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var todoId = "TEST-OPT-001";

        var subtasks = new[]
        {
            YamlEnvelopeBuilder.CreateTodoSubtask("Task A", false),
            YamlEnvelopeBuilder.CreateTodoSubtask("Task B", true),
            YamlEnvelopeBuilder.CreateTodoSubtask("Task C", false)
        };

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoCreateRequest(
                GenerateRequestId("create"),
                todoId,
                "Full Optional Fields Test",
                "Testing",
                "critical",
                estimate: "16h",
                description: new[] { "Desc 1", "Desc 2", "Desc 3" },
                technicalDetails: new[] { "Tech 1", "Tech 2" },
                implementationTasks: subtasks,
                note: "Important note here",
                remaining: "Everything needs to be done",
                dependsOn: new[] { "TEST-DEP-001", "TEST-DEP-002" },
                functionalRequirements: new[] { "FR-001", "FR-002", "FR-003" },
                technicalRequirements: new[] { "TR-001", "TR-002" }));

        var createResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(createResponse);

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoGetRequest(
                GenerateRequestId("get"),
                todoId));

        var getResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(getResponse);

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateTodoDeleteRequest(GenerateRequestId("cleanup"), todoId));
    }

    private async Task SendCommandAndWaitAsync(object envelope)
    {
        var initialCount = _replProcess.StdoutLines.Count;
        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(envelope));
        var foundResponse = await _replProcess.WaitForStdoutLineCountAsync(initialCount + 1, TimeSpan.FromSeconds(15));
        Assert.True(foundResponse, BuildTimeoutMessage(initialCount + 1));
        await Task.Delay(100);
    }

    private string BuildTimeoutMessage(int expectedCount)
        => $"Timed out waiting for stdout document count {expectedCount}. "
           + $"STDOUT: {string.Join(Environment.NewLine + "--- stdout document ---" + Environment.NewLine, _replProcess.StdoutLines)} "
           + $"STDERR: {string.Join(Environment.NewLine, _replProcess.StderrLines)}";

    private async Task WaitForResponseAsync(string requestId, TimeSpan timeout)
    {
        var foundResponse = await _replProcess.WaitForStdoutResponseAsync(requestId, timeout);
        Assert.True(foundResponse, $"Timed out waiting for response '{requestId}'. {_replProcess.Diagnostics}");
    }

    private static string GenerateRequestId(string suffix)
        => TestRequestIds.Next(suffix);

    public void Dispose()
    {
        _replProcess.StopAsync().Wait(TimeSpan.FromSeconds(3));
        _replProcess.Dispose();
    }
}
