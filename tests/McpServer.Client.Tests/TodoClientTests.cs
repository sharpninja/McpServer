using System;
using System.Net;
using System.Net.Http;
using Xunit;

namespace McpServer.Client.Tests;

public sealed class TodoClientTests
{
    private static readonly McpServerClientOptions DefaultOptions = new()
    {
        BaseUrl = new Uri("http://localhost:7147"),
        ApiKey = "test-key"
    };

    [Fact]
    public async System.Threading.Tasks.Task QueryAsync_SendsCorrectRequest()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"items":[],"totalCount":0}""");
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, DefaultOptions);

        var result = await client.QueryAsync(keyword: "auth", priority: "high");

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Get, handler.LastRequest.Method);
        Assert.Contains("keyword=auth", handler.LastRequest.RequestUri!.Query);
        Assert.Contains("priority=high", handler.LastRequest.RequestUri.Query);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetAsync_SendsCorrectUrl()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"id":"MVP-001","title":"test","section":"s","priority":"high","done":false}""");
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, DefaultOptions);

        var result = await client.GetAsync("MVP-001");

        Assert.Equal("MVP-001", result.Id);
        Assert.Contains("/mcpserver/todo/MVP-001", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetAuditAsync_SendsCorrectUrl()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """
            {
              "entries": [
                {
                  "auditId": 7,
                  "todoId": "MVP-001",
                  "version": 2,
                  "action": "updated",
                  "recordedAtUtc": "2026-03-20T16:00:00Z",
                  "snapshot": {
                    "id": "MVP-001",
                    "title": "After",
                    "section": "mvp-app",
                    "priority": "high",
                    "done": false
                  },
                  "previousSnapshot": {
                    "id": "MVP-001",
                    "title": "Before",
                    "section": "mvp-app",
                    "priority": "high",
                    "done": false
                  },
                  "source": "api"
                }
              ],
              "totalCount": 1
            }
            """);
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, DefaultOptions);

        var result = await client.GetAuditAsync("MVP-001", limit: 25, offset: 5);

        Assert.Equal(1, result.TotalCount);
        Assert.Contains("/mcpserver/todo/MVP-001/audit", handler.LastRequest!.RequestUri!.AbsolutePath);
        Assert.Contains("limit=25", handler.LastRequest.RequestUri.Query);
        Assert.Contains("offset=5", handler.LastRequest.RequestUri.Query);
        Assert.Single(result.Entries);
        Assert.Equal(7, result.Entries[0].AuditId);
        Assert.Equal("After", result.Entries[0].Snapshot?.Title);
        Assert.Equal("Before", result.Entries[0].PreviousSnapshot?.Title);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetProjectionStatusAsync_SendsCorrectUrl()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """
            {
              "authoritativeStore": "sqlite",
              "authoritativeDataSource": "E:\\todo.db",
              "projectionTargetPath": "E:\\docs\\Project\\TODO.yaml",
              "projectionTargetExists": true,
              "projectionConsistent": true,
              "repairRequired": false,
              "verifiedAtUtc": "2026-03-21T00:00:00Z",
              "lastProjectedToYamlUtc": "2026-03-21T00:00:00Z",
              "message": "TODO.yaml matches authoritative database state."
            }
            """);
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, DefaultOptions);

        var result = await client.GetProjectionStatusAsync();

        Assert.Equal("sqlite", result.AuthoritativeStore);
        Assert.True(result.ProjectionConsistent);
        Assert.False(result.RepairRequired);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/todo/projection/status", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async System.Threading.Tasks.Task RepairProjectionAsync_PostsCorrectUrl()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """
            {
              "success": true,
              "error": null,
              "status": {
                "authoritativeStore": "sqlite",
                "authoritativeDataSource": "E:\\todo.db",
                "projectionTargetPath": "E:\\docs\\Project\\TODO.yaml",
                "projectionTargetExists": true,
                "projectionConsistent": true,
                "repairRequired": false,
                "verifiedAtUtc": "2026-03-21T00:01:00Z",
                "message": "TODO.yaml matches authoritative database state."
              }
            }
            """);
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, DefaultOptions);

        var result = await client.RepairProjectionAsync();

        Assert.True(result.Success);
        Assert.NotNull(result.Status);
        Assert.True(result.Status.ProjectionConsistent);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/todo/projection/repair", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async System.Threading.Tasks.Task CreateAsync_PostsJsonBody()
    {
        var handler = new MockHttpHandler(HttpStatusCode.Created, """{"success":true}""");
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, DefaultOptions);

        var result = await client.CreateAsync(new Models.TodoCreateRequest
        {
            Id = "NEW-001",
            Title = "New item",
            Section = "test",
            Priority = "high",
            Note = "client note",
            Remaining = "client remaining"
        });

        Assert.True(result.Success);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("NEW-001", handler.LastRequestBody!);
        Assert.Contains("\"note\":\"client note\"", handler.LastRequestBody!, StringComparison.Ordinal);
        Assert.Contains("\"remaining\":\"client remaining\"", handler.LastRequestBody!, StringComparison.Ordinal);
    }

    [Fact]
    public async System.Threading.Tasks.Task UpdateAsync_PutsJsonBody()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"success":true}""");
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, DefaultOptions);

        var result = await client.UpdateAsync("MVP-001", new Models.TodoUpdateRequest { Done = true });

        Assert.True(result.Success);
        Assert.Equal(HttpMethod.Put, handler.LastRequest!.Method);
    }

    [Fact]
    public async System.Threading.Tasks.Task DeleteAsync_SendsDeleteRequest()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"success":true}""");
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, DefaultOptions);

        var result = await client.DeleteAsync("MVP-001");

        Assert.True(result.Success);
        Assert.Equal(HttpMethod.Delete, handler.LastRequest!.Method);
    }

    [Fact]
    public async System.Threading.Tasks.Task ApiKeyHeader_IsSent()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"items":[],"totalCount":0}""");
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, DefaultOptions);

        await client.QueryAsync();

        Assert.NotNull(handler.LastRequest);
        Assert.True(handler.LastRequest.Headers.TryGetValues("X-Api-Key", out var values));
        Assert.Contains("test-key", values!);
    }

    [Fact]
    public async System.Threading.Tasks.Task AnalyzeRequirementsAsync_PostsCorrectUrl()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"success":true,"copilotResponse":"analysis"}""");
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, DefaultOptions);

        var result = await client.AnalyzeRequirementsAsync("MVP-001");

        Assert.True(result.Success);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/todo/MVP-001/requirements", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async System.Threading.Tasks.Task CreateIterationPhaseAsync_PostsCorrectUrl()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"phaseId":"PHASE-001","status":"Planning"}""");
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, DefaultOptions);

        var result = await client.CreateIterationPhaseAsync(new Models.CreateIterationPhaseRequest
        {
            Name = "Execution phase",
            Summary = "Bounded Byrd execution"
        });

        Assert.Equal("PHASE-001", result.PhaseId);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/todo-execution/phases", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async System.Threading.Tasks.Task CreateTodosFromPlanAsync_PostsCorrectUrl()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """{"phaseId":"PHASE-001","planId":"PLAN-001","todoIds":["TODO-201","TODO-202"]}""");
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, DefaultOptions);

        var result = await client.CreateTodosFromPlanAsync(
            "PHASE-001",
            new Models.CreateTodosFromPlanRequest
            {
                PhaseId = "PHASE-001",
                PlanId = "PLAN-001",
                Todos =
                [
                    new Models.PlanTodoInput
                    {
                        Title = "Execution todo",
                        Goal = "Bound context",
                        Summary = "Use active TODO only."
                    }
                ]
            });

        Assert.Equal("PHASE-001", result.PhaseId);
        Assert.Equal(2, result.TodoIds.Count);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/todo-execution/phases/PHASE-001/todos", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetActiveTodoAsync_SendsCorrectUrl()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """{"todoId":"TODO-201","title":"Execution todo","status":"TestDesign","nextAction":"Define unit tests"}""");
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, DefaultOptions);

        var result = await client.GetActiveTodoAsync();

        Assert.Equal("TODO-201", result.TodoId);
        Assert.Equal(Models.TodoExecutionStatus.TestDesign, result.Status);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/todo-execution/active", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetNextReadyTodoAsync_SendsCorrectUrl()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """{"todoId":"TODO-202","title":"Validation todo","status":"Validating","nextAction":"Run validation"}""");
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, DefaultOptions);

        var result = await client.GetNextReadyTodoAsync();

        Assert.Equal("TODO-202", result.TodoId);
        Assert.Equal(Models.TodoExecutionStatus.Validating, result.Status);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/todo-execution/next-ready", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetExecutionContextAsync_SendsCorrectQuery()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """
            {
              "todoId":"TODO-201",
              "workspacePath":"F:\\GitHub\\McpServer",
              "title":"Execution todo",
              "goal":"Implement bounded execution",
              "summary":"Hydrate the active TODO context.",
              "status":"TestDesign",
              "iterationPhaseId":"PHASE-001",
              "nextAction":"Define unit tests",
              "requirementIds":["FR-BYRD-001"],
              "recentRequirementSnippets":["FR-BYRD-001: Planning remains bounded."],
              "recentTurnSummaries":["Defined the TODO execution boundaries."],
              "relevantFiles":["src/McpServer.Services/Services/TodoExecutionService.cs"],
              "artifactIds":[],
              "acceptanceCriteria":["Hydrates requirement snippets"],
              "constraints":["Do not return the full plan"],
              "testPlan":{"unitTestsDefined":false,"unitTestsPassing":false,"integrationTestsDefined":false,"integrationTestsPassing":false,"testFilePaths":[],"testCommands":[]},
              "validation":{"lastResult":"not_run","lastValidatedAtUtc":null,"validationArtifactIds":[],"summary":null},
              "pointers":{"lastRelevantTurnId":"req-001","lastSuccessfulTurnId":null,"lastFailedTurnId":null,"lastCheckpointId":"CHK-001","lastCommitSha":null,"lastScreenshotArtifactId":null}
            }
            """);
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, DefaultOptions);

        var result = await client.GetExecutionContextAsync("TODO-201", 3, 2);

        Assert.Equal("TODO-201", result.TodoId);
        Assert.Contains("/mcpserver/todo-execution/todos/TODO-201", handler.LastRequest!.RequestUri!.AbsolutePath);
        Assert.Contains("requirementSnippetLimit=3", handler.LastRequest.RequestUri.Query);
        Assert.Contains("sessionTurnSummaryLimit=2", handler.LastRequest.RequestUri.Query);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetDeltaContextAsync_SendsCorrectQuery()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """
            {
              "todoId":"TODO-201",
              "sinceCheckpointId":"CHK-001",
              "newRequirementIds":["FR-BYRD-001"],
              "newTurnIds":["req-002"],
              "newArtifactIds":["artifacts/diff.patch"],
              "newCommitShas":["abc1234"],
              "updatedNextAction":"Run validation"
            }
            """);
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, DefaultOptions);

        var result = await client.GetDeltaContextAsync("TODO-201", "CHK-001");

        Assert.Equal("TODO-201", result.TodoId);
        Assert.Equal("CHK-001", result.SinceCheckpointId);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/todo-execution/todos/TODO-201/delta", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("sinceCheckpointId=CHK-001", handler.LastRequest.RequestUri.Query);
    }

    [Fact]
    public async System.Threading.Tasks.Task SetTestPlanAsync_PutsCorrectUrl()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """{"todoId":"TODO-201","status":"TestReady"}""");
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, DefaultOptions);

        var result = await client.SetTestPlanAsync(
            "TODO-201",
            new Models.SetTodoTestPlanRequest
            {
                UnitTestsDefined = true,
                TestFilePaths = ["tests/TodoExecutionServiceTests.cs"],
                TestCommands = ["dotnet test tests/McpServer.Support.Mcp.Tests"]
            });

        Assert.Equal("TODO-201", result.TodoId);
        Assert.Equal(Models.TodoExecutionStatus.TestReady, result.Status);
        Assert.Equal(HttpMethod.Put, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/todo-execution/todos/TODO-201/test-plan", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async System.Threading.Tasks.Task UpdateExecutionStatusAsync_PostsCorrectUrl()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """{"todoId":"TODO-201","previousStatus":"TestReady","currentStatus":"Implementing"}""");
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, DefaultOptions);

        var result = await client.UpdateExecutionStatusAsync(
            "TODO-201",
            new Models.UpdateTodoStatusRequest
            {
                TargetStatus = Models.TodoExecutionStatus.Implementing,
                Reason = "Unit tests are defined"
            });

        Assert.Equal(Models.TodoExecutionStatus.Implementing, result.CurrentStatus);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/todo-execution/todos/TODO-201/status", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async System.Threading.Tasks.Task AppendCheckpointAsync_PostsCorrectUrl()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """{"todoId":"TODO-201","checkpointId":"CHK-001"}""");
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, DefaultOptions);

        var result = await client.AppendCheckpointAsync(
            "TODO-201",
            new Models.AppendTodoCheckpointRequest
            {
                Kind = Models.TodoCheckpointKind.ImplementationProgress,
                Summary = "Implemented execution gating.",
                NextAction = "Run validation"
            });

        Assert.Equal("CHK-001", result.CheckpointId);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/todo-execution/todos/TODO-201/checkpoints", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async System.Threading.Tasks.Task RecordValidationResultAsync_PostsCorrectUrl()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """{"todoId":"TODO-201","validationState":{"lastResult":"pass","summary":"Validation succeeded."}}""");
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, DefaultOptions);

        var result = await client.RecordValidationResultAsync(
            "TODO-201",
            new Models.RecordTodoValidationResultRequest
            {
                Result = "pass",
                Summary = "Validation succeeded.",
                UnitTestsPassing = true
            });

        Assert.Equal("TODO-201", result.TodoId);
        Assert.Equal("Validation succeeded.", result.ValidationState.Summary);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/todo-execution/todos/TODO-201/validation", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async System.Threading.Tasks.Task LinkSessionTurnsAsync_PostsCorrectUrl()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """{"todoId":"TODO-201","sessionTurnIds":["req-001","req-002"]}""");
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, DefaultOptions);

        var result = await client.LinkSessionTurnsAsync(
            "TODO-201",
            new Models.LinkTodoToSessionTurnsRequest
            {
                SessionTurnIds = ["req-001", "req-002"]
            });

        Assert.Equal("TODO-201", result.TodoId);
        Assert.Equal(2, result.SessionTurnIds.Count);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/todo-execution/todos/TODO-201/session-turns", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async System.Threading.Tasks.Task AdbStepAsync_PostsCorrectUrl()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """{"success":true,"action":"Screenshot","deviceSerial":"emulator-5554","commandSummary":"adb -s emulator-5554 exec-out screencap -p","screenshotPath":"artifacts/device/test.png","screenshotBase64":null,"currentFocus":"com.example/.MainActivity","observationHints":[],"error":null,"timestampUtc":"2026-04-23T22:01:01Z"}""");
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, DefaultOptions);

        var result = await client.AdbStepAsync(new Models.AdbStepRequest
        {
            Action = Models.AdbStepAction.Screenshot,
            CaptureScreenshot = true,
            Instruction = "Capture the current UI state."
        });

        Assert.True(result.Success);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/todo-execution/adb/step", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async System.Threading.Tasks.Task StreamStatusAsync_YieldsDataLines()
    {
        var sse = "data: Line one\n\ndata: Line two\n\nevent: done\ndata: \n\n";
        var handler = new MockHttpHandler(HttpStatusCode.OK, sse, "text/event-stream");
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, DefaultOptions);

        var lines = new System.Collections.Generic.List<string>();
        await foreach (var line in client.StreamStatusAsync("MVP-001"))
            lines.Add(line);

        Assert.Equal(2, lines.Count);
        Assert.Equal("Line one", lines[0]);
        Assert.Equal("Line two", lines[1]);
        Assert.Contains("/mcpserver/todo/MVP-001/prompt/status", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async System.Threading.Tasks.Task StreamImplementAsync_YieldsDataLines()
    {
        var sse = "data: impl line\n\nevent: done\ndata: \n\n";
        var handler = new MockHttpHandler(HttpStatusCode.OK, sse, "text/event-stream");
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, DefaultOptions);

        var lines = new System.Collections.Generic.List<string>();
        await foreach (var line in client.StreamImplementAsync("MVP-001"))
            lines.Add(line);

        Assert.Single(lines);
        Assert.Equal("impl line", lines[0]);
        Assert.Contains("/mcpserver/todo/MVP-001/prompt/implement", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async System.Threading.Tasks.Task StreamPlanAsync_YieldsDataLines()
    {
        var sse = "data: plan step 1\n\ndata: plan step 2\n\ndata: plan step 3\n\nevent: done\ndata: \n\n";
        var handler = new MockHttpHandler(HttpStatusCode.OK, sse, "text/event-stream");
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, DefaultOptions);

        var lines = new System.Collections.Generic.List<string>();
        await foreach (var line in client.StreamPlanAsync("MVP-001"))
            lines.Add(line);

        Assert.Equal(3, lines.Count);
        Assert.Equal("plan step 1", lines[0]);
        Assert.Contains("/mcpserver/todo/MVP-001/prompt/plan", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async System.Threading.Tasks.Task QueueStatusPromptAsync_PostsQueueEndpoint()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"success":true,"jobId":"job-status","agentName":"triage","renderedPrompt":"status"}""");
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, DefaultOptions);

        var result = await client.QueueStatusPromptAsync(
            "MVP-001",
            new Models.AgentPoolOneShotRequest { AgentName = "triage", Context = Models.AgentPoolOneShotContext.Status });

        Assert.True(result.Success);
        Assert.Equal("job-status", result.JobId);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/todo/MVP-001/prompt/status/queue", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("\"agentName\":\"triage\"", handler.LastRequestBody!, StringComparison.Ordinal);
    }

    [Fact]
    public async System.Threading.Tasks.Task QueueImplementPromptAsync_PostsQueueEndpoint()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"success":true,"jobId":"job-implement","agentName":"builder","renderedPrompt":"implement"}""");
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, DefaultOptions);

        var result = await client.QueueImplementPromptAsync("MVP-001");

        Assert.True(result.Success);
        Assert.Equal("job-implement", result.JobId);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/todo/MVP-001/prompt/implement/queue", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async System.Threading.Tasks.Task QueuePlanPromptAsync_PostsQueueEndpoint()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"success":true,"jobId":"job-plan","agentName":"planner","renderedPrompt":"plan"}""");
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, DefaultOptions);

        var result = await client.QueuePlanPromptAsync("MVP-001");

        Assert.True(result.Success);
        Assert.Equal("job-plan", result.JobId);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/todo/MVP-001/prompt/plan/queue", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async System.Threading.Tasks.Task StreamSse_WithoutApiKey_ThrowsInvalidOperation()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, "", "text/event-stream");
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, new McpServerClientOptions { BaseUrl = new System.Uri("http://localhost:7147") });

        await Assert.ThrowsAsync<System.InvalidOperationException>(async () =>
        {
            await foreach (var _ in client.StreamStatusAsync("MVP-001")) { }
        });
    }

    [Fact]
    public async System.Threading.Tasks.Task StreamSse_ServerError_ThrowsMcpServerException()
    {
        var handler = new MockHttpHandler(HttpStatusCode.InternalServerError, """{"error":"fail"}""", "text/event-stream");
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, DefaultOptions);

        await Assert.ThrowsAsync<McpServerException>(async () =>
        {
            await foreach (var _ in client.StreamStatusAsync("MVP-001")) { }
        });
    }

    [Fact]
    public async System.Threading.Tasks.Task StreamSse_ApiKeyHeader_IsSent()
    {
        var sse = "event: done\ndata: \n\n";
        var handler = new MockHttpHandler(HttpStatusCode.OK, sse, "text/event-stream");
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, DefaultOptions);

        await foreach (var _ in client.StreamStatusAsync("MVP-001")) { }

        Assert.True(handler.LastRequest!.Headers.TryGetValues("X-Api-Key", out var values));
        Assert.Contains("test-key", values!);
    }
}
