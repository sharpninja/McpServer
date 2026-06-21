// TR-MCP-REPL-005 / Phase 1d: TODO/Byrd execution MCP tools partial of FwhMcpTools.

using System.ComponentModel;
using System.Text.Json;
using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Requirements;
using McpServer.Support.Mcp.Requirements.Models;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.McpStdio;

public sealed partial class FwhMcpTools
{
    // ── GROUP A: TODO tools ──────────────────────────────────────────────

    /// <summary>TR-PLANNED-CORE-013: List/search TODO items.</summary>
    [McpServerTool(Name = "todo_list"), Description("Query TODO items. Optional filters: section, priority, done.")]
    public async Task<string> TodoList(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Section filter (e.g. mvp-app)")] string? section = null,
        [Description("Priority filter (high/medium/low)")] string? priority = null,
        [Description("Done filter (true/false)")] bool? done = null,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var result = await _workspaceAccessor.GetTodoService().QueryAsync(new TodoQueryRequest { Section = section, Priority = priority, Done = done }, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Serialize(new { items = result.Items, totalCount = result.TotalCount });
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>TR-PLANNED-CORE-013: Get a single TODO by id.</summary>
    [McpServerTool(Name = "todo_get"), Description("Get a single TODO item by its id (e.g. MVP-APP-001).")]
    public async Task<string> TodoGet(
        [Description("TODO item id")] string id,
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var item = await _workspaceAccessor.GetTodoService().GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (item == null) return JsonSerializer.Serialize(new { error = $"TODO '{id}' not found" });
            return JsonSerializer.Serialize(item);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>TR-MCP-TODO-005: Get append-only audit history for a TODO item.</summary>
    [McpServerTool(Name = "todo_audit"), Description("Get append-only audit history for a TODO item by id.")]
    public async Task<string> TodoAudit(
        [Description("TODO item id")] string id,
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Maximum entries to return (default 50)")] int limit = 50,
        [Description("Entries to skip before returning results (default 0)")] int offset = 0,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var result = await _workspaceAccessor.GetTodoService().GetAuditAsync(id, limit, offset, cancellationToken).ConfigureAwait(false);
            if (result.TotalCount == 0)
                return JsonSerializer.Serialize(new { error = $"TODO audit '{id}' not found" });

            return JsonSerializer.Serialize(new { entries = result.Entries, totalCount = result.TotalCount });
        }
        catch (NotSupportedException ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>TR-MCP-TODO-006: Get database-authoritative TODO projection status.</summary>
    [McpServerTool(Name = "todo_projection_status"), Description("Get projection status for database-backed TODO storage.")]
    public async Task<string> TodoProjectionStatus(
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var result = await _workspaceAccessor.GetTodoService().GetProjectionStatusAsync(cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Serialize(result);
        }
        catch (NotSupportedException ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>TR-MCP-TODO-006: Repair TODO.yaml projection from database-authoritative TODO storage.</summary>
    [McpServerTool(Name = "todo_projection_repair"), Description("Repair TODO.yaml projection from authoritative database-backed TODO storage.")]
    public async Task<string> TodoProjectionRepair(
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var result = _todoMutations is null
                ? await _workspaceAccessor.GetTodoService().RepairProjectionAsync(cancellationToken).ConfigureAwait(false)
                : await _todoMutations.RepairProjectionAsync(cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Serialize(result);
        }
        catch (NotSupportedException ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>TR-PLANNED-CORE-013: Create a new TODO item.</summary>
    [McpServerTool(Name = "todo_create"), Description("Create a new TODO item. Requires id, title, section, priority.")]
    public async Task<string> TodoCreate(
        [Description("Item id (e.g. MVP-APP-006 or ISSUE-NEW)")] string id,
        [Description("Item title")] string title,
        [Description("Section (e.g. mvp-app)")] string section,
        [Description("Priority (high/medium/low)")] string priority,
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Estimate string")] string? estimate = null,
        [Description("Description text")] string? description = null,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var req = new TodoCreateRequest
            {
                Id = id,
                Title = title,
                Section = section,
                Priority = priority,
                Estimate = estimate,
                Description = description != null ? new[] { description } : null
            };
            var result = _todoMutations is null
                ? await _todoCreationService.CreateAsync(req, cancellationToken).ConfigureAwait(false)
                : await _todoMutations.CreateAsync(req, cancellationToken).ConfigureAwait(false);
            if (!result.Success) return JsonSerializer.Serialize(new { error = result.Error });
            return JsonSerializer.Serialize(new { success = true, item = result.Item });
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>TR-PLANNED-CORE-013: Update an existing TODO item.</summary>
    [McpServerTool(Name = "todo_update"), Description("Update a TODO item by id. Only provided fields are changed.")]
    public async Task<string> TodoUpdate(
        [Description("TODO item id")] string id,
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Updated title")] string? title = null,
        [Description("Updated priority")] string? priority = null,
        [Description("Mark as done")] bool? done = null,
        [Description("Updated note")] string? note = null,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var req = new TodoUpdateRequest { Title = title, Priority = priority, Done = done, Note = note };
            var result = _todoMutations is null
                ? await _todoUpdateService.UpdateAsync(id, req, cancellationToken).ConfigureAwait(false)
                : await _todoMutations.UpdateAsync(id, req, cancellationToken).ConfigureAwait(false);
            if (!result.Success) return JsonSerializer.Serialize(new { error = result.Error });
            return JsonSerializer.Serialize(new { success = true, item = result.Item });
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>TR-PLANNED-CORE-013: Delete a TODO item by id.</summary>
    [McpServerTool(Name = "todo_delete"), Description("Delete a TODO item by id.")]
    public async Task<string> TodoDelete(
        [Description("TODO item id")] string id,
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var result = _todoMutations is null
                ? await _workspaceAccessor.GetTodoService().DeleteAsync(id, cancellationToken).ConfigureAwait(false)
                : await _todoMutations.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
            if (!result.Success) return JsonSerializer.Serialize(new { error = result.Error });
            return JsonSerializer.Serialize(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>Move a TODO item from the source workspace to a different target workspace.</summary>
    [McpServerTool(Name = "todo_move"), Description("Move a TODO item from one workspace to another by its ID.")]
    public async Task<string> TodoMove(
        [Description("TODO item id")] string id,
        [Description("Source workspace path (required)")] string workspacePath,
        [Description("Target workspace path to move the item to")] string targetWorkspacePath,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            if (_todoMutations is not null)
            {
                var gated = await _todoMutations.MoveAsync(
                    id,
                    new TodoMoveRequest { TargetWorkspacePath = targetWorkspacePath },
                    cancellationToken).ConfigureAwait(false);
                if (!gated.Success) return JsonSerializer.Serialize(new { error = gated.Error });
                return JsonSerializer.Serialize(new { success = true, item = gated.Item });
            }

            var sourceService = _workspaceAccessor.GetTodoService();
            var item = await sourceService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (item is null) return JsonSerializer.Serialize(new { error = $"Item '{id}' not found in source workspace." });

            var targetWs = await _workspaceService.GetAsync(targetWorkspacePath, cancellationToken).ConfigureAwait(false);
            if (targetWs is null) return JsonSerializer.Serialize(new { error = $"Target workspace '{targetWorkspacePath}' not found." });

            var targetContext = new WorkspaceContext
            {
                WorkspacePath = targetWs.WorkspacePath,
                WorkspaceName = targetWs.Name,
                DataDirectory = targetWs.DataDirectory,
                TodoFilePath = targetWs.TodoPath,
            };
            var targetService = _todoServiceResolver.Resolve(targetContext);

            var createReq = new TodoCreateRequest
            {
                Id = item.Id, Title = item.Title, Section = item.Section, Priority = item.Priority,
                Estimate = item.Estimate, Description = item.Description, TechnicalDetails = item.TechnicalDetails,
                ImplementationTasks = item.ImplementationTasks, Note = item.Note, Remaining = item.Remaining,
                DependsOn = item.DependsOn, FunctionalRequirements = item.FunctionalRequirements,
                TechnicalRequirements = item.TechnicalRequirements,
            };

            var createResult = await targetService.CreateAsync(createReq, cancellationToken).ConfigureAwait(false);
            if (!createResult.Success) return JsonSerializer.Serialize(new { error = $"Failed to create in target: {createResult.Error}" });

            var deleteResult = await sourceService.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
            if (!deleteResult.Success) return JsonSerializer.Serialize(new { error = $"Created in target but failed to delete from source: {deleteResult.Error}" });

            return JsonSerializer.Serialize(new { success = true, movedTo = targetWs.WorkspacePath });
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>MVP-MCP-002: Invoke Copilot to generate a status report for a TODO item.</summary>
    [McpServerTool(Name = "todo_status"), Description("Invoke Copilot to generate a status report for a TODO item in the workspace.")]
    public async Task<string> TodoStatus(
        [Description("TODO item id")] string id,
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            return await CollectStreamAsync(_todoPromptService.StreamStatusAsync(id, cancellationToken)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>MVP-MCP-002: Invoke Copilot to implement a TODO item in the workspace.</summary>
    [McpServerTool(Name = "todo_implement"), Description("Invoke Copilot to implement a TODO item, working through each task in the workspace.")]
    public async Task<string> TodoImplement(
        [Description("TODO item id")] string id,
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            return await CollectStreamAsync(_todoPromptService.StreamImplementAsync(id, cancellationToken)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>MVP-MCP-002: Invoke Copilot to create a detailed implementation plan for a TODO item.</summary>
    [McpServerTool(Name = "todo_plan"), Description("Invoke Copilot to create a detailed implementation plan for a TODO item in the workspace.")]
    public async Task<string> TodoPlan(
        [Description("TODO item id")] string id,
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            return await CollectStreamAsync(_todoPromptService.StreamPlanAsync(id, null, cancellationToken)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>Create a bounded Byrd iteration phase.</summary>
    [McpServerTool(Name = "create_iteration_phase"), Description("Create a bounded Byrd iteration phase aligned to requirements and scope.")]
    public async Task<string> CreateIterationPhase(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Phase name")] string name,
        [Description("Phase summary")] string summary,
        [Description("Linked requirement IDs")] string[]? requirementIds = null,
        [Description("Entry criteria")] string[]? entryCriteria = null,
        [Description("Exit criteria")] string[]? exitCriteria = null,
        [Description("Originating plan ID")] string? createdFromPlanId = null,
        [Description("Branch associated with the phase")] string? branch = null,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var result = await _todoExecutionService.CreateIterationPhaseAsync(workspacePath, new CreateIterationPhaseRequest
            {
                Name = name,
                Summary = summary,
                RequirementIds = requirementIds,
                EntryCriteria = entryCriteria,
                ExitCriteria = exitCriteria,
                CreatedFromPlanId = createdFromPlanId,
                Branch = branch,
            }, cancellationToken).ConfigureAwait(false);
            return SerializeJson(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return SerializeJson(new { error = ex.Message });
        }
    }

    /// <summary>Create Byrd execution TODOs from a plan.</summary>
    [McpServerTool(Name = "create_todos_from_plan"), Description("Decompose an approved plan into executable TODO items inside an iteration phase.")]
    public async Task<string> CreateTodosFromPlan(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Iteration phase ID")] string phaseId,
        [Description("Plan ID")] string planId,
        [Description("Planned TODO definitions")] PlanTodoInput[]? todos = null,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var result = await _todoExecutionService.CreateTodosFromPlanAsync(workspacePath, new CreateTodosFromPlanRequest
            {
                PhaseId = phaseId,
                PlanId = planId,
                Todos = todos,
            }, cancellationToken).ConfigureAwait(false);
            return SerializeJson(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return SerializeJson(new { error = ex.Message });
        }
    }

    /// <summary>Return the active Byrd execution TODO.</summary>
    [McpServerTool(Name = "get_active_todo"), Description("Return the single TODO Codex should work on next.")]
    public async Task<string> GetActiveTodo(
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var result = await _todoExecutionService.GetActiveTodoAsync(workspacePath, cancellationToken).ConfigureAwait(false);
            return result is null
                ? SerializeJson(new { error = "No active TODO was found." })
                : SerializeJson(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return SerializeJson(new { error = ex.Message });
        }
    }

    /// <summary>Return the bounded execution context for a Byrd TODO.</summary>
    [McpServerTool(Name = "get_todo_execution_context"), Description("Hydrate a single bounded working set for a Byrd execution TODO.")]
    public async Task<string> GetTodoExecutionContext(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Execution TODO ID")] string todoId,
        [Description("Maximum requirement snippets to return (default 5)")] int requirementSnippetLimit = 5,
        [Description("Maximum recent turn summaries to return (default 5)")] int sessionTurnSummaryLimit = 5,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var result = await _todoExecutionService.GetExecutionContextAsync(
                workspacePath,
                todoId,
                requirementSnippetLimit,
                sessionTurnSummaryLimit,
                cancellationToken).ConfigureAwait(false);
            return result is null
                ? SerializeJson(new { error = $"Execution TODO '{todoId}' was not found." })
                : SerializeJson(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return SerializeJson(new { error = ex.Message });
        }
    }

    /// <summary>Return the execution delta for a Byrd TODO since a checkpoint.</summary>
    [McpServerTool(Name = "get_todo_delta_context"), Description("Fetch only what changed since the last checkpoint for a Byrd TODO.")]
    public async Task<string> GetTodoDeltaContext(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Execution TODO ID")] string todoId,
        [Description("Checkpoint ID to diff from")] string? sinceCheckpointId = null,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var result = await _todoExecutionService.GetDeltaContextAsync(workspacePath, todoId, sinceCheckpointId, cancellationToken).ConfigureAwait(false);
            return result is null
                ? SerializeJson(new { error = $"Execution TODO '{todoId}' was not found." })
                : SerializeJson(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return SerializeJson(new { error = ex.Message });
        }
    }

    /// <summary>Store the test plan for a Byrd TODO.</summary>
    [McpServerTool(Name = "set_todo_test_plan"), Description("Store test files and commands before implementation begins.")]
    public async Task<string> SetTodoTestPlan(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Execution TODO ID")] string todoId,
        [Description("Whether unit tests are defined")] bool unitTestsDefined,
        [Description("Whether integration tests are defined")] bool integrationTestsDefined = false,
        [Description("Test file paths")] string[]? testFilePaths = null,
        [Description("Test commands")] string[]? testCommands = null,
        [Description("Whether unit tests are already passing")] bool? unitTestsPassing = null,
        [Description("Whether integration tests are already passing")] bool? integrationTestsPassing = null,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var result = await _todoExecutionService.SetTestPlanAsync(workspacePath, todoId, new SetTodoTestPlanRequest
            {
                UnitTestsDefined = unitTestsDefined,
                IntegrationTestsDefined = integrationTestsDefined,
                TestFilePaths = testFilePaths,
                TestCommands = testCommands,
                UnitTestsPassing = unitTestsPassing,
                IntegrationTestsPassing = integrationTestsPassing,
            }, cancellationToken).ConfigureAwait(false);
            return SerializeJson(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return SerializeJson(new { error = ex.Message });
        }
    }

    /// <summary>Move a Byrd TODO through its execution states.</summary>
    [McpServerTool(Name = "update_todo_status"), Description("Move a Byrd TODO through its execution states with process enforcement.")]
    public async Task<string> UpdateTodoStatus(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Execution TODO ID")] string todoId,
        [Description("Target execution status")] TodoExecutionStatus targetStatus,
        [Description("Optional transition reason")] string? reason = null,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var result = await _todoExecutionService.UpdateStatusAsync(workspacePath, todoId, new UpdateTodoStatusRequest
            {
                TargetStatus = targetStatus,
                Reason = reason,
            }, cancellationToken).ConfigureAwait(false);
            return SerializeJson(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return SerializeJson(new { error = ex.Message });
        }
    }

    /// <summary>Append a checkpoint to a Byrd TODO.</summary>
    [McpServerTool(Name = "append_todo_checkpoint"), Description("Record progress, decisions, failures, or validation results for a Byrd TODO.")]
    public async Task<string> AppendTodoCheckpoint(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Execution TODO ID")] string todoId,
        [Description("Checkpoint kind")] TodoCheckpointKind kind,
        [Description("Checkpoint summary")] string summary,
        [Description("Suggested next action")] string? nextAction = null,
        [Description("Requirement IDs")] string[]? requirementIds = null,
        [Description("Session turn IDs")] string[]? sessionTurnIds = null,
        [Description("Artifact IDs")] string[]? artifactIds = null,
        [Description("Commit SHAs")] string[]? commitShas = null,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var result = await _todoExecutionService.AppendCheckpointAsync(workspacePath, todoId, new AppendTodoCheckpointRequest
            {
                Kind = kind,
                Summary = summary,
                NextAction = nextAction,
                RequirementIds = requirementIds,
                SessionTurnIds = sessionTurnIds,
                ArtifactIds = artifactIds,
                CommitShas = commitShas,
            }, cancellationToken).ConfigureAwait(false);
            return SerializeJson(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return SerializeJson(new { error = ex.Message });
        }
    }

    /// <summary>Record the validation result for a Byrd TODO.</summary>
    [McpServerTool(Name = "record_todo_validation_result"), Description("Persist validation state, including device validation artifacts, for a Byrd TODO.")]
    public async Task<string> RecordTodoValidationResult(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Execution TODO ID")] string todoId,
        [Description("Validation result string")] string result,
        [Description("Validation summary")] string? summary = null,
        [Description("Artifact IDs")] string[]? artifactIds = null,
        [Description("Session turn IDs")] string[]? sessionTurnIds = null,
        [Description("Whether unit tests are passing")] bool? unitTestsPassing = null,
        [Description("Whether integration tests are passing")] bool? integrationTestsPassing = null,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var payload = await _todoExecutionService.RecordValidationResultAsync(workspacePath, todoId, new RecordTodoValidationResultRequest
            {
                Result = result,
                Summary = summary,
                ArtifactIds = artifactIds,
                SessionTurnIds = sessionTurnIds,
                UnitTestsPassing = unitTestsPassing,
                IntegrationTestsPassing = integrationTestsPassing,
            }, cancellationToken).ConfigureAwait(false);
            return SerializeJson(payload);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return SerializeJson(new { error = ex.Message });
        }
    }

    /// <summary>Return the next ready Byrd TODO.</summary>
    [McpServerTool(Name = "get_next_ready_todo"), Description("Advance work without rereading the whole plan.")]
    public async Task<string> GetNextReadyTodo(
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var result = await _todoExecutionService.GetNextReadyTodoAsync(workspacePath, cancellationToken).ConfigureAwait(false);
            return result is null
                ? SerializeJson(new { error = "No ready TODO was found." })
                : SerializeJson(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return SerializeJson(new { error = ex.Message });
        }
    }

    /// <summary>Link historical session turns to a Byrd TODO.</summary>
    [McpServerTool(Name = "link_todo_to_session_turns"), Description("Attach historical evidence to a Byrd TODO without duplicating log content.")]
    public async Task<string> LinkTodoToSessionTurns(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Execution TODO ID")] string todoId,
        [Description("Session turn IDs")] string[]? sessionTurnIds = null,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var result = await _todoExecutionService.LinkTodoToSessionTurnsAsync(workspacePath, todoId, new LinkTodoToSessionTurnsRequest
            {
                SessionTurnIds = sessionTurnIds,
            }, cancellationToken).ConfigureAwait(false);
            return SerializeJson(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return SerializeJson(new { error = ex.Message });
        }
    }

    /// <summary>Perform a safe Android ADB step.</summary>
    [McpServerTool(Name = "adb_step"), Description("Perform a fixed safe ADB action such as screenshot, tap, swipe, text, keyevent, wait, launch_app, or get_focus.")]
    public async Task<string> AdbStep(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("ADB action")] AdbStepAction action,
        [Description("Optional device serial")] string? deviceSerial = null,
        [Description("Capture a screenshot after the action")] bool captureScreenshot = false,
        [Description("Optional user-facing instruction")] string? instruction = null,
        [Description("Tap X coordinate")] int? x = null,
        [Description("Tap Y coordinate")] int? y = null,
        [Description("Swipe start X coordinate")] int? startX = null,
        [Description("Swipe start Y coordinate")] int? startY = null,
        [Description("Swipe end X coordinate")] int? endX = null,
        [Description("Swipe end Y coordinate")] int? endY = null,
        [Description("Optional duration in milliseconds")] int? durationMs = null,
        [Description("Text payload")] string? text = null,
        [Description("Key event value")] string? keyEvent = null,
        [Description("Package name to launch")] string? packageName = null,
        [Description("Activity name for explicit launches")] string? activityName = null,
        [Description("Wait duration in milliseconds")] int? waitMilliseconds = null,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var result = await _todoExecutionService.AdbStepAsync(workspacePath, new AdbStepRequest
            {
                DeviceSerial = deviceSerial,
                Action = action,
                CaptureScreenshot = captureScreenshot,
                Instruction = instruction,
                X = x,
                Y = y,
                StartX = startX,
                StartY = startY,
                EndX = endX,
                EndY = endY,
                DurationMs = durationMs,
                Text = text,
                KeyEvent = keyEvent,
                PackageName = packageName,
                ActivityName = activityName,
                WaitMilliseconds = waitMilliseconds,
            }, cancellationToken).ConfigureAwait(false);
            return SerializeJson(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return SerializeJson(new { error = ex.Message });
        }
    }

    private static async Task<string> CollectStreamAsync(IAsyncEnumerable<string> lines)
    {
        var sb = new System.Text.StringBuilder();
        await foreach (var line in lines.ConfigureAwait(false))
        {
            if (sb.Length > 0) sb.AppendLine();
            sb.Append(line);
        }
        return sb.ToString();
    }
}
