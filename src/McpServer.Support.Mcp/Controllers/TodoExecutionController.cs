using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Mvc;

namespace McpServer.Support.Mcp.Controllers;

/// <summary>
/// Byrd execution endpoints layered on top of the existing TODO API.
/// </summary>
[ApiController]
[Route("mcpserver/todo-execution")]
public sealed class TodoExecutionController : ControllerBase
{
    private readonly ITodoExecutionService _todoExecutionService;
    private readonly WorkspaceContext _workspaceContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="TodoExecutionController"/> class.
    /// </summary>
    public TodoExecutionController(ITodoExecutionService todoExecutionService, WorkspaceContext workspaceContext)
    {
        _todoExecutionService = todoExecutionService ?? throw new ArgumentNullException(nameof(todoExecutionService));
        _workspaceContext = workspaceContext ?? throw new ArgumentNullException(nameof(workspaceContext));
    }

    /// <summary>Create a Byrd iteration phase.</summary>
    [HttpPost("phases")]
    public async Task<ActionResult<CreateIterationPhaseResult>> CreateIterationPhaseAsync(
        [FromBody] CreateIterationPhaseRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest(new { error = "Request body is required." });
        if (!TryGetWorkspacePath(out var workspacePath, out var errorResult))
            return errorResult!;

        try
        {
            var result = await _todoExecutionService.CreateIterationPhaseAsync(workspacePath, request, cancellationToken).ConfigureAwait(false);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Create Byrd execution TODOs from a plan within an iteration phase.</summary>
    [HttpPost("phases/{phaseId}/todos")]
    public async Task<ActionResult<CreateTodosFromPlanResult>> CreateTodosFromPlanAsync(
        string phaseId,
        [FromBody] CreateTodosFromPlanRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest(new { error = "Request body is required." });
        if (!TryGetWorkspacePath(out var workspacePath, out var errorResult))
            return errorResult!;

        try
        {
            request.PhaseId = phaseId;
            var result = await _todoExecutionService.CreateTodosFromPlanAsync(workspacePath, request, cancellationToken).ConfigureAwait(false);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { error = ex.Message });
        }
    }

    /// <summary>Return the active Byrd execution TODO.</summary>
    [HttpGet("active")]
    public async Task<ActionResult<ActiveTodoResult>> GetActiveTodoAsync(CancellationToken cancellationToken)
    {
        if (!TryGetWorkspacePath(out var workspacePath, out var errorResult))
            return errorResult!;

        var result = await _todoExecutionService.GetActiveTodoAsync(workspacePath, cancellationToken).ConfigureAwait(false);
        return result is null ? NotFound(new { error = "No active TODO was found." }) : Ok(result);
    }

    /// <summary>Return the next ready Byrd execution TODO.</summary>
    [HttpGet("next-ready")]
    public async Task<ActionResult<ActiveTodoResult>> GetNextReadyTodoAsync(CancellationToken cancellationToken)
    {
        if (!TryGetWorkspacePath(out var workspacePath, out var errorResult))
            return errorResult!;

        var result = await _todoExecutionService.GetNextReadyTodoAsync(workspacePath, cancellationToken).ConfigureAwait(false);
        return result is null ? NotFound(new { error = "No ready TODO was found." }) : Ok(result);
    }

    /// <summary>Return the bounded execution context for a TODO.</summary>
    [HttpGet("todos/{todoId}")]
    public async Task<ActionResult<ActiveTodoContext>> GetExecutionContextAsync(
        string todoId,
        [FromQuery] int requirementSnippetLimit = 5,
        [FromQuery] int sessionTurnSummaryLimit = 5,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetWorkspacePath(out var workspacePath, out var errorResult))
            return errorResult!;

        var result = await _todoExecutionService
            .GetExecutionContextAsync(workspacePath, todoId, requirementSnippetLimit, sessionTurnSummaryLimit, cancellationToken)
            .ConfigureAwait(false);
        return result is null ? NotFound(new { error = $"Execution TODO '{todoId}' was not found." }) : Ok(result);
    }

    /// <summary>Return the delta context for a TODO since a checkpoint.</summary>
    [HttpGet("todos/{todoId}/delta")]
    public async Task<ActionResult<TodoDeltaContext>> GetDeltaContextAsync(
        string todoId,
        [FromQuery] string? sinceCheckpointId,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetWorkspacePath(out var workspacePath, out var errorResult))
            return errorResult!;

        var result = await _todoExecutionService
            .GetDeltaContextAsync(workspacePath, todoId, sinceCheckpointId, cancellationToken)
            .ConfigureAwait(false);
        return result is null ? NotFound(new { error = $"Execution TODO '{todoId}' was not found." }) : Ok(result);
    }

    /// <summary>Store a test plan for a TODO.</summary>
    [HttpPut("todos/{todoId}/test-plan")]
    public async Task<ActionResult<SetTodoTestPlanResult>> SetTestPlanAsync(
        string todoId,
        [FromBody] SetTodoTestPlanRequest? request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
            return BadRequest(new { error = "Request body is required." });
        if (!TryGetWorkspacePath(out var workspacePath, out var errorResult))
            return errorResult!;

        try
        {
            var result = await _todoExecutionService.SetTestPlanAsync(workspacePath, todoId, request, cancellationToken).ConfigureAwait(false);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>Update the execution status for a TODO.</summary>
    [HttpPost("todos/{todoId}/status")]
    public async Task<ActionResult<UpdateTodoStatusResult>> UpdateStatusAsync(
        string todoId,
        [FromBody] UpdateTodoStatusRequest? request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
            return BadRequest(new { error = "Request body is required." });
        if (!TryGetWorkspacePath(out var workspacePath, out var errorResult))
            return errorResult!;

        try
        {
            var result = await _todoExecutionService.UpdateStatusAsync(workspacePath, todoId, request, cancellationToken).ConfigureAwait(false);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { error = ex.Message });
        }
    }

    /// <summary>Append a checkpoint to a TODO.</summary>
    [HttpPost("todos/{todoId}/checkpoints")]
    public async Task<ActionResult<AppendTodoCheckpointResult>> AppendCheckpointAsync(
        string todoId,
        [FromBody] AppendTodoCheckpointRequest? request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
            return BadRequest(new { error = "Request body is required." });
        if (!TryGetWorkspacePath(out var workspacePath, out var errorResult))
            return errorResult!;

        try
        {
            var result = await _todoExecutionService.AppendCheckpointAsync(workspacePath, todoId, request, cancellationToken).ConfigureAwait(false);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>Record a validation result for a TODO.</summary>
    [HttpPost("todos/{todoId}/validation")]
    public async Task<ActionResult<RecordTodoValidationResultResult>> RecordValidationResultAsync(
        string todoId,
        [FromBody] RecordTodoValidationResultRequest? request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
            return BadRequest(new { error = "Request body is required." });
        if (!TryGetWorkspacePath(out var workspacePath, out var errorResult))
            return errorResult!;

        try
        {
            var result = await _todoExecutionService.RecordValidationResultAsync(workspacePath, todoId, request, cancellationToken).ConfigureAwait(false);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>Link historical session turns to a TODO.</summary>
    [HttpPost("todos/{todoId}/session-turns")]
    public async Task<ActionResult<LinkTodoToSessionTurnsResult>> LinkTodoToSessionTurnsAsync(
        string todoId,
        [FromBody] LinkTodoToSessionTurnsRequest? request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
            return BadRequest(new { error = "Request body is required." });
        if (!TryGetWorkspacePath(out var workspacePath, out var errorResult))
            return errorResult!;

        try
        {
            var result = await _todoExecutionService.LinkTodoToSessionTurnsAsync(workspacePath, todoId, request, cancellationToken).ConfigureAwait(false);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>Perform a safe Android ADB step.</summary>
    [HttpPost("adb/step")]
    public async Task<ActionResult<AdbStepResult>> AdbStepAsync(
        [FromBody] AdbStepRequest? request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
            return BadRequest(new { error = "Request body is required." });
        if (!TryGetWorkspacePath(out var workspacePath, out var errorResult))
            return errorResult!;

        var result = await _todoExecutionService.AdbStepAsync(workspacePath, request, cancellationToken).ConfigureAwait(false);
        return result.Success ? Ok(result) : UnprocessableEntity(result);
    }

    private bool TryGetWorkspacePath(out string workspacePath, out ActionResult? errorResult)
    {
        if (string.IsNullOrWhiteSpace(_workspaceContext.WorkspacePath))
        {
            workspacePath = string.Empty;
            errorResult = BadRequest(new { error = "Workspace path was not resolved for the current request." });
            return false;
        }

        workspacePath = _workspaceContext.WorkspacePath!;
        errorResult = null;
        return true;
    }
}
