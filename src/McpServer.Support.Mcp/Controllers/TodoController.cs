using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace McpServer.Support.Mcp.Controllers;

/// <summary>
/// TR-PLANNED-013: TODO item CRUD and query endpoints for MCP.
/// Agents can search, create, update, and delete TODO items via REST.
/// </summary>
[ApiController]
[Route("mcpserver/todo")]
public sealed class TodoController : ControllerBase
{
    private readonly ITodoService _todoService;
    private readonly TodoServiceResolver _todoServiceResolver;
    private readonly IWorkspaceService _workspaceService;
    private readonly IRequirementsService _requirementsService;
    private readonly ITodoPromptService _todoPromptService;
    private readonly TodoCreationService _todoCreationService;
    private readonly TodoUpdateService _todoUpdateService;
    private readonly IAgentPoolService? _agentPoolService;

    /// <summary>TR-PLANNED-013, TR-MCP-MT-001: Constructor. Resolves workspace-specific TODO service.</summary>
    public TodoController(
        TodoServiceResolver todoServiceResolver,
        WorkspaceContext workspaceContext,
        IWorkspaceService workspaceService,
        IRequirementsService requirementsService,
        ITodoPromptService todoPromptService,
        TodoCreationService todoCreationService,
        TodoUpdateService todoUpdateService,
        IAgentPoolService? agentPoolService = null)
    {
        _todoServiceResolver = todoServiceResolver;
        _workspaceService = workspaceService;
        _todoService = todoServiceResolver.Resolve(workspaceContext);
        _requirementsService = requirementsService;
        _todoPromptService = todoPromptService;
        _todoCreationService = todoCreationService ?? throw new ArgumentNullException(nameof(todoCreationService));
        _todoUpdateService = todoUpdateService ?? throw new ArgumentNullException(nameof(todoUpdateService));
        _agentPoolService = agentPoolService;
    }

    /// <summary>TR-PLANNED-013: Query TODO items by keyword, priority, section, id, or done status.</summary>
    [HttpGet]
    public async Task<ActionResult<TodoQueryResult>> QueryAsync(
        [FromQuery] string? keyword,
        [FromQuery] string? priority,
        [FromQuery] string? section,
        [FromQuery] string? id,
        [FromQuery] bool? done,
        CancellationToken cancellationToken)
    {
        var request = new TodoQueryRequest
        {
            Keyword = keyword,
            Priority = priority,
            Section = section,
            Id = id,
            Done = done
        };
        var result = await _todoService.QueryAsync(request, cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>TR-PLANNED-013: Get a single TODO item by id.</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<TodoFlatItem>> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        var item = await _todoService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (item is null)
            return NotFound(new { error = $"Item with id '{id}' not found." });
        return Ok(item);
    }

    /// <summary>TR-MCP-TODO-005: Get append-only audit history for a single TODO item.</summary>
    [HttpGet("{id}/audit")]
    public async Task<ActionResult<TodoAuditQueryResult>> GetAuditAsync(
        string id,
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _todoService.GetAuditAsync(id, limit, offset, cancellationToken).ConfigureAwait(false);
            if (result.TotalCount == 0)
                return NotFound(new { error = $"Audit history for TODO '{id}' not found." });

            return Ok(result);
        }
        catch (NotSupportedException ex)
        {
            return StatusCode(StatusCodes.Status501NotImplemented, new { error = ex.Message });
        }
    }

    /// <summary>TR-MCP-TODO-006: Get SQLite-authoritative TODO projection status and repair guidance.</summary>
    [HttpGet("projection/status")]
    public async Task<ActionResult<TodoProjectionStatusResult>> GetProjectionStatusAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _todoService.GetProjectionStatusAsync(cancellationToken).ConfigureAwait(false);
            return Ok(result);
        }
        catch (NotSupportedException ex)
        {
            return StatusCode(StatusCodes.Status501NotImplemented, new { error = ex.Message });
        }
    }

    /// <summary>TR-MCP-TODO-006: Repair TODO.yaml projection from SQLite-authoritative TODO storage.</summary>
    [HttpPost("projection/repair")]
    public async Task<ActionResult<TodoProjectionRepairResult>> RepairProjectionAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _todoService.RepairProjectionAsync(cancellationToken).ConfigureAwait(false);
            return result.Success
                ? Ok(result)
                : StatusCode(StatusCodes.Status500InternalServerError, result);
        }
        catch (NotSupportedException ex)
        {
            return StatusCode(StatusCodes.Status501NotImplemented, new { error = ex.Message });
        }
    }

    /// <summary>TR-PLANNED-013: Create a new TODO item.</summary>
    [HttpPost]
    public async Task<ActionResult<TodoMutationResult>> CreateAsync(
        [FromBody] TodoCreateRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest(new TodoMutationResult(false, "Request body is required."));

        var result = await _todoCreationService.CreateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!result.Success)
            return ToMutationFailureResult(result);

        var createdId = result.Item?.Id ?? request.Id;
        return Created(new Uri($"/mcpserver/todo/{Uri.EscapeDataString(createdId)}", UriKind.Relative), result);
    }

    /// <summary>TR-PLANNED-013: Update an existing TODO item by id.</summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<TodoMutationResult>> UpdateAsync(
        string id,
        [FromBody] TodoUpdateRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest(new TodoMutationResult(false, "Request body is required."));

        var result = await _todoUpdateService.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false);
        if (!result.Success)
            return ToMutationFailureResult(result);

        return Ok(result);
    }

    /// <summary>TR-PLANNED-013: Delete a TODO item by id.</summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<TodoMutationResult>> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        var result = await _todoService.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        if (!result.Success)
            return ToMutationFailureResult(result);

        return Ok(result);
    }

    /// <summary>Move a TODO item from the current workspace to a different workspace.</summary>
    [HttpPost("{id}/move")]
    public async Task<ActionResult<TodoMutationResult>> MoveAsync(
        string id,
        [FromBody] TodoMoveRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.TargetWorkspacePath))
            return BadRequest(new TodoMutationResult(false, "Request body with targetWorkspacePath is required."));

        var item = await _todoService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (item is null)
            return NotFound(new TodoMutationResult(false, $"Item with id '{id}' not found in source workspace."));

        var targetWorkspace = await _workspaceService.GetAsync(request.TargetWorkspacePath, cancellationToken).ConfigureAwait(false);
        if (targetWorkspace is null)
            return BadRequest(new TodoMutationResult(false, $"Target workspace '{request.TargetWorkspacePath}' not found."));

        var targetContext = new WorkspaceContext
        {
            WorkspacePath = targetWorkspace.WorkspacePath,
            WorkspaceName = targetWorkspace.Name,
            DataDirectory = targetWorkspace.DataDirectory,
            TodoFilePath = targetWorkspace.TodoPath,
        };

        var targetService = _todoServiceResolver.Resolve(targetContext);

        var createRequest = new TodoCreateRequest
        {
            Id = item.Id,
            Title = item.Title,
            Section = item.Section,
            Priority = item.Priority,
            Estimate = item.Estimate,
            Description = item.Description,
            TechnicalDetails = item.TechnicalDetails,
            ImplementationTasks = item.ImplementationTasks,
            Note = item.Note,
            Remaining = item.Remaining,
            DependsOn = item.DependsOn,
            FunctionalRequirements = item.FunctionalRequirements,
            TechnicalRequirements = item.TechnicalRequirements,
        };

        var createResult = await targetService.CreateAsync(createRequest, cancellationToken).ConfigureAwait(false);
        if (!createResult.Success)
            return Conflict(new TodoMutationResult(false, $"Failed to create in target workspace: {createResult.Error}"));

        var deleteResult = await _todoService.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        if (!deleteResult.Success)
            throw new InvalidOperationException($"TODO move failed after target creation succeeded. Target workspace '{request.TargetWorkspacePath}' contains item '{id}', but source deletion failed: {deleteResult.Error}");

        return Ok(new TodoMutationResult(true, null, createResult.Item));
    }

    /// <summary>
    /// Invoke Copilot to analyze a TODO item and update project docs with new FR/TR entries.
    /// </summary>
    [HttpPost("{id}/requirements")]
    public async Task<ActionResult<RequirementsAnalysisResult>> AnalyzeRequirementsAsync(
        string id,
        CancellationToken cancellationToken)
    {
        var result = await _requirementsService.AnalyzeAsync(id, cancellationToken).ConfigureAwait(false);
        if (!result.Success)
            return UnprocessableEntity(result);

        return Ok(result);
    }

    /// <summary>MVP-MCP-002: Stream a Copilot-generated status report for a TODO item via SSE.</summary>
    [HttpGet("{id}/prompt/status")]
    [Produces("text/event-stream")]
    public async Task StreamStatusPromptAsync(string id, CancellationToken cancellationToken)
    {
        if (!await EnsureTodoExistsAsync(id, cancellationToken).ConfigureAwait(false)) return;
        await StreamCopilotResponseAsync(_todoPromptService.StreamStatusAsync(id, cancellationToken), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>MVP-MCP-002: Stream a Copilot-driven implementation session for a TODO item via SSE.</summary>
    [HttpGet("{id}/prompt/implement")]
    [Produces("text/event-stream")]
    public async Task StreamImplementPromptAsync(string id, CancellationToken cancellationToken)
    {
        if (!await EnsureTodoExistsAsync(id, cancellationToken).ConfigureAwait(false)) return;
        await StreamCopilotResponseAsync(_todoPromptService.StreamImplementAsync(id, cancellationToken), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>MVP-MCP-002: Stream a Copilot-driven planning session for a TODO item via SSE.</summary>
    [HttpGet("{id}/prompt/plan")]
    [Produces("text/event-stream")]
    public async Task StreamPlanPromptAsync(string id, [FromQuery] string? prompt, CancellationToken cancellationToken)
    {
        if (!await EnsureTodoExistsAsync(id, cancellationToken).ConfigureAwait(false)) return;
        await StreamCopilotResponseAsync(_todoPromptService.StreamPlanAsync(id, prompt, cancellationToken), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>FR-MCP-053: Enqueue a TODO status one-shot request through the Agent Pool queue.</summary>
    [HttpPost("{id}/prompt/status/queue")]
    public async Task<ActionResult<AgentPoolEnqueueResult>> QueueStatusPromptAsync(
        string id,
        [FromBody] AgentPoolOneShotRequest? request,
        CancellationToken cancellationToken)
        => await QueueTodoPromptAsync(id, AgentPoolOneShotContext.Status, request, cancellationToken).ConfigureAwait(false);

    /// <summary>FR-MCP-053: Enqueue a TODO implement one-shot request through the Agent Pool queue.</summary>
    [HttpPost("{id}/prompt/implement/queue")]
    public async Task<ActionResult<AgentPoolEnqueueResult>> QueueImplementPromptAsync(
        string id,
        [FromBody] AgentPoolOneShotRequest? request,
        CancellationToken cancellationToken)
        => await QueueTodoPromptAsync(id, AgentPoolOneShotContext.Implement, request, cancellationToken).ConfigureAwait(false);

    /// <summary>FR-MCP-053: Enqueue a TODO plan one-shot request through the Agent Pool queue.</summary>
    [HttpPost("{id}/prompt/plan/queue")]
    public async Task<ActionResult<AgentPoolEnqueueResult>> QueuePlanPromptAsync(
        string id,
        [FromBody] AgentPoolOneShotRequest? request,
        CancellationToken cancellationToken)
        => await QueueTodoPromptAsync(id, AgentPoolOneShotContext.Plan, request, cancellationToken).ConfigureAwait(false);

    private async Task<ActionResult<AgentPoolEnqueueResult>> QueueTodoPromptAsync(
        string id,
        AgentPoolOneShotContext context,
        AgentPoolOneShotRequest? request,
        CancellationToken cancellationToken)
    {
        if (_agentPoolService is null)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new AgentPoolEnqueueResult { Success = false, Error = "Agent pool service unavailable." });

        var item = await _todoService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (item is null)
            return NotFound(new AgentPoolEnqueueResult { Success = false, Error = $"TODO '{id}' not found." });

        var enqueueRequest = new AgentPoolOneShotRequest
        {
            AgentName = request?.AgentName,
            Context = context,
            PromptTemplateId = request?.PromptTemplateId,
            PromptText = request?.PromptText,
            Id = id,
            Values = request?.Values,
            UseWorkspaceContext = request?.UseWorkspaceContext ?? true,
        };

        var result = await _agentPoolService.EnqueueOneShotAsync(enqueueRequest, cancellationToken).ConfigureAwait(false);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    private async Task<bool> EnsureTodoExistsAsync(string id, CancellationToken cancellationToken)
    {
        var item = await _todoService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (item is not null) return true;

        Response.StatusCode = 404;
        Response.ContentType = "application/json";
        await Response.WriteAsync($"{{\"error\":\"TODO '{id}' not found.\"}}", cancellationToken).ConfigureAwait(false);
        return false;
    }

    private ActionResult<TodoMutationResult> ToMutationFailureResult(TodoMutationResult result)
        => result.FailureKind switch
        {
            TodoMutationFailureKind.Validation => BadRequest(result),
            TodoMutationFailureKind.NotFound => NotFound(result),
            TodoMutationFailureKind.ProjectionFailed => StatusCode(StatusCodes.Status500InternalServerError, result),
            TodoMutationFailureKind.ExternalSyncFailed => StatusCode(StatusCodes.Status502BadGateway, result),
            _ => Conflict(result)
        };

    private async Task StreamCopilotResponseAsync(IAsyncEnumerable<string> lines, CancellationToken cancellationToken)
    {
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["Connection"] = "keep-alive";
        Response.ContentType = "text/event-stream";

        await Response.WriteAsync("event: thinking\ndata: Processing…\n\n", cancellationToken).ConfigureAwait(false);
        await Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);

        var heartbeatInterval = TimeSpan.FromSeconds(5);
        var enumerator = lines.GetAsyncEnumerator(cancellationToken);
        try
        {
            var hasData = false;
            while (true)
            {
                var moveVt = enumerator.MoveNextAsync();
                Task<bool> moveTask;
                if (moveVt.IsCompleted)
                {
                    moveTask = Task.FromResult(moveVt.Result);
                }
                else
                {
                    moveTask = moveVt.AsTask();
                    while (!moveTask.IsCompleted)
                    {
                        var completed = await Task.WhenAny(moveTask, Task.Delay(heartbeatInterval, cancellationToken)).ConfigureAwait(false);
                        if (completed != moveTask)
                        {
                            await Response.WriteAsync("event: thinking\ndata: …\n\n", cancellationToken).ConfigureAwait(false);
                            await Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
                        }
                    }
                }

                if (!await moveTask.ConfigureAwait(false))
                    break;

                hasData = true;
                var line = enumerator.Current;
                await Response.WriteAsync($"data: {line}\n\n", cancellationToken).ConfigureAwait(false);
                await Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            if (!hasData)
            {
                await Response.WriteAsync("data: (no output)\n\n", cancellationToken).ConfigureAwait(false);
                await Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await Response.WriteAsync($"event: error\ndata: Stream failed: {ex.Message}\n\n", cancellationToken).ConfigureAwait(false);
            await Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await enumerator.DisposeAsync();
        }

        await Response.WriteAsync("event: done\ndata: \n\n", cancellationToken).ConfigureAwait(false);
        await Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
}
