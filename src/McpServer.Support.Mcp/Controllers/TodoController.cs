using McpServer.Support.Mcp.Services;
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

    /// <summary>TR-PLANNED-013, TR-MCP-MT-001: Constructor. Resolves workspace-specific TODO service.</summary>
    public TodoController(TodoServiceResolver todoServiceResolver, WorkspaceContext workspaceContext, IWorkspaceService workspaceService, IRequirementsService requirementsService, ITodoPromptService todoPromptService)
    {
        _todoServiceResolver = todoServiceResolver;
        _workspaceService = workspaceService;
        _todoService = todoServiceResolver.Resolve(workspaceContext);
        _requirementsService = requirementsService;
        _todoPromptService = todoPromptService;
    }

    /// <summary>TR-PLANNED-013: Query TODO items by keyword, priority, section, id, or done status.</summary>
    /// <param name="keyword">Free-text keyword to match across all fields.</param>
    /// <param name="priority">Filter by priority: high, medium, or low.</param>
    /// <param name="section">Filter by section key (e.g. mvp-app, mvp-support).</param>
    /// <param name="id">Filter by item id.</param>
    /// <param name="done">Filter by done status.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
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
    /// <param name="id">The TODO item id (e.g. MVP-APP-001).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{id}")]
    public async Task<ActionResult<TodoFlatItem>> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        var item = await _todoService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (item is null)
            return NotFound(new { error = $"Item with id '{id}' not found." });
        return Ok(item);
    }

    /// <summary>TR-PLANNED-013: Create a new TODO item.</summary>
    /// <param name="request">Create request body with id, title, section, priority.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost]
    public async Task<ActionResult<TodoMutationResult>> CreateAsync(
        [FromBody] TodoCreateRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest(new TodoMutationResult(false, "Request body is required."));

        var result = await _todoService.CreateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!result.Success)
            return Conflict(result);

        return Created(new Uri($"/mcpserver/todo/{Uri.EscapeDataString(request.Id)}", UriKind.Relative), result);
    }

    /// <summary>TR-PLANNED-013: Update an existing TODO item by id.</summary>
    /// <param name="id">The TODO item id.</param>
    /// <param name="request">Update request body with fields to change.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPut("{id}")]
    public async Task<ActionResult<TodoMutationResult>> UpdateAsync(
        string id,
        [FromBody] TodoUpdateRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest(new TodoMutationResult(false, "Request body is required."));

        var result = await _todoService.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>TR-PLANNED-013: Delete a TODO item by id.</summary>
    /// <param name="id">The TODO item id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpDelete("{id}")]
    public async Task<ActionResult<TodoMutationResult>> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        var result = await _todoService.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>Move a TODO item from the current workspace to a different workspace.</summary>
    /// <param name="id">The TODO item id to move.</param>
    /// <param name="request">Request body containing the target workspace path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("{id}/move")]
    public async Task<ActionResult<TodoMutationResult>> MoveAsync(
        string id,
        [FromBody] TodoMoveRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.TargetWorkspacePath))
            return BadRequest(new TodoMutationResult(false, "Request body with targetWorkspacePath is required."));

        // 1. Get the item from the source workspace.
        var item = await _todoService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (item is null)
            return NotFound(new TodoMutationResult(false, $"Item with id '{id}' not found in source workspace."));

        // 2. Resolve the target workspace.
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

        // 3. Create in the target workspace.
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

        // 4. Delete from the source workspace.
        var deleteResult = await _todoService.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        if (!deleteResult.Success)
            return StatusCode(500, new TodoMutationResult(false, $"Created in target but failed to delete from source: {deleteResult.Error}"));

        return Ok(new TodoMutationResult(true, null, createResult.Item));
    }

    /// <summary>
    /// Invoke Copilot to analyze a TODO item and update project docs with
    /// new FR/TR entries. Updates the TODO item with all associated FR/TR IDs.
    /// </summary>
    /// <param name="id">The TODO item id to analyze.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
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

    /// <summary>
    /// MVP-MCP-002: Stream a Copilot-generated status report for a TODO item via SSE.
    /// The Copilot CLI is invoked in the workspace directory and output is streamed line by line.
    /// </summary>
    /// <param name="id">The TODO item id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{id}/prompt/status")]
    [Produces("text/event-stream")]
    public async Task StreamStatusPromptAsync(string id, CancellationToken cancellationToken)
    {
        if (!await EnsureTodoExistsAsync(id, cancellationToken).ConfigureAwait(false)) return;
        await StreamCopilotResponseAsync(_todoPromptService.StreamStatusAsync(id, cancellationToken), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// MVP-MCP-002: Stream a Copilot-driven implementation session for a TODO item via SSE.
    /// The Copilot CLI is invoked in the workspace directory with full item context and
    /// step-by-step implementation instructions. Output is streamed line by line.
    /// </summary>
    /// <param name="id">The TODO item id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{id}/prompt/implement")]
    [Produces("text/event-stream")]
    public async Task StreamImplementPromptAsync(string id, CancellationToken cancellationToken)
    {
        if (!await EnsureTodoExistsAsync(id, cancellationToken).ConfigureAwait(false)) return;
        await StreamCopilotResponseAsync(_todoPromptService.StreamImplementAsync(id, cancellationToken), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// MVP-MCP-002: Stream a Copilot-driven planning session for a TODO item via SSE.
    /// The Copilot CLI is invoked in the workspace directory with full item context and
    /// instructions to create a detailed implementation plan. Output is streamed line by line.
    /// </summary>
    /// <param name="id">The TODO item id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{id}/prompt/plan")]
    [Produces("text/event-stream")]
    public async Task StreamPlanPromptAsync(string id, CancellationToken cancellationToken)
    {
        if (!await EnsureTodoExistsAsync(id, cancellationToken).ConfigureAwait(false)) return;
        await StreamCopilotResponseAsync(_todoPromptService.StreamPlanAsync(id, cancellationToken), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Returns <c>true</c> if the item exists; writes a 404 JSON response and returns <c>false</c> otherwise.</summary>
    private async Task<bool> EnsureTodoExistsAsync(string id, CancellationToken cancellationToken)
    {
        var item = await _todoService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (item is not null) return true;

        Response.StatusCode = 404;
        Response.ContentType = "application/json";
        await Response.WriteAsync($"{{\"error\":\"TODO '{id}' not found.\"}}", cancellationToken).ConfigureAwait(false);
        return false;
    }

    private async Task StreamCopilotResponseAsync(IAsyncEnumerable<string> lines, CancellationToken cancellationToken)
    {
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["Connection"] = "keep-alive";
        Response.ContentType = "text/event-stream";

        // Flush headers immediately so clients see the connection is alive.
        await Response.WriteAsync("event: thinking\ndata: Processing…\n\n", cancellationToken).ConfigureAwait(false);
        await Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);

        // Send periodic heartbeat events while waiting for data lines.
        // The enumerator is consumed concurrently with a heartbeat timer.
        var heartbeatInterval = TimeSpan.FromSeconds(5);
        var enumerator = lines.GetAsyncEnumerator(cancellationToken);
        try
        {
            var hasData = false;
            while (true)
            {
                // Race: next data line vs heartbeat timer.
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
        finally
        {
            await enumerator.DisposeAsync();
        }

        await Response.WriteAsync("event: done\ndata: \n\n", cancellationToken).ConfigureAwait(false);
        await Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
}
