using FWH.Support.Mcp.Services;
using Microsoft.AspNetCore.Mvc;

namespace FWH.Support.Mcp.Controllers;

/// <summary>
/// TR-PLANNED-013: TODO item CRUD and query endpoints for MCP.
/// Agents can search, create, update, and delete TODO items via REST.
/// </summary>
[ApiController]
[Route("mcp/todo")]
public sealed class TodoController : ControllerBase
{
    private readonly ITodoService _todoService;
    private readonly IRequirementsService _requirementsService;

    /// <summary>TR-PLANNED-013: Constructor.</summary>
    public TodoController(ITodoService todoService, IRequirementsService requirementsService)
    {
        _todoService = todoService;
        _requirementsService = requirementsService;
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

        return Created(new Uri($"/mcp/todo/{Uri.EscapeDataString(request.Id)}", UriKind.Relative), result);
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
}
