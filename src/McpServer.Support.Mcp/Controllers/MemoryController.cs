using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace McpServer.Support.Mcp.Controllers;

/// <summary>
/// TR-MCP-MEMORY-004: REST endpoints for MCP memory CRUD and effective memory
/// listing.
/// </summary>
[ApiController]
[Route("mcpserver/memory")]
public sealed class MemoryController : ControllerBase
{
    private readonly IMemoryService _memoryService;

    /// <summary>Initializes a new instance of the <see cref="MemoryController"/> class.</summary>
    public MemoryController(IMemoryService memoryService)
    {
        _memoryService = memoryService ?? throw new ArgumentNullException(nameof(memoryService));
    }

    /// <summary>Lists effective memories visible to the active workspace.</summary>
    [HttpGet]
    public async Task<ActionResult<MemoryQueryResult>> ListAsync(
        [FromQuery] string? scope,
        [FromQuery] string? category,
        [FromQuery] string? keyword,
        CancellationToken cancellationToken)
    {
        if (!TryParseListScope(scope, out var parsedScope, out var error))
            return BadRequest(new { error });

        var result = await _memoryService.ListAsync(new MemoryListRequest
        {
            Scope = parsedScope,
            Category = category,
            Keyword = keyword,
        }, cancellationToken).ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>Gets one visible memory by id.</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<MemoryItem>> GetAsync(string id, CancellationToken cancellationToken)
    {
        var memory = await _memoryService.GetAsync(id, cancellationToken).ConfigureAwait(false);
        return memory is null
            ? NotFound(new { error = $"Memory '{id}' not found." })
            : Ok(memory);
    }

    /// <summary>Adds a new memory.</summary>
    [HttpPost]
    public async Task<ActionResult<MemoryMutationResult>> AddAsync(
        [FromBody] MemoryAddRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest(new MemoryMutationResult(false, "Request body is required.", FailureKind: MemoryMutationFailureKind.Validation));

        var result = await _memoryService.AddAsync(request, cancellationToken).ConfigureAwait(false);
        if (!result.Success)
            return ToMutationFailureResult(result);

        var createdId = result.Memory?.Id ?? request.Id ?? string.Empty;
        return Created(new Uri($"/mcpserver/memory/{Uri.EscapeDataString(createdId)}", UriKind.Relative), result);
    }

    /// <summary>Updates one visible memory by id.</summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<MemoryMutationResult>> UpdateAsync(
        string id,
        [FromBody] MemoryUpdateRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest(new MemoryMutationResult(false, "Request body is required.", FailureKind: MemoryMutationFailureKind.Validation));

        var result = await _memoryService.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false);
        if (!result.Success)
            return ToMutationFailureResult(result);

        return Ok(result);
    }

    /// <summary>Removes one visible memory by id.</summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<MemoryMutationResult>> RemoveAsync(string id, CancellationToken cancellationToken)
    {
        var result = await _memoryService.RemoveAsync(id, cancellationToken).ConfigureAwait(false);
        if (!result.Success)
            return ToMutationFailureResult(result);

        return Ok(result);
    }

    private ActionResult<MemoryMutationResult> ToMutationFailureResult(MemoryMutationResult result)
        => result.FailureKind switch
        {
            MemoryMutationFailureKind.Validation => BadRequest(result),
            MemoryMutationFailureKind.NotFound => NotFound(result),
            MemoryMutationFailureKind.Conflict => Conflict(result),
            _ => StatusCode(StatusCodes.Status500InternalServerError, result),
        };

    private static bool TryParseListScope(string? value, out MemoryScope? scope, out string? error)
    {
        scope = null;
        error = null;

        if (string.IsNullOrWhiteSpace(value)
            || string.Equals(value.Trim(), "Effective", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var trimmed = value.Trim();
        if (int.TryParse(trimmed, out _)
            || !Enum.TryParse(trimmed, ignoreCase: true, out MemoryScope parsed)
            || !Enum.IsDefined(parsed))
        {
            error = "scope must be Effective, Global, or Workspace.";
            return false;
        }

        scope = parsed;
        return true;
    }
}
