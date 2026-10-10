using System.Diagnostics.CodeAnalysis;
using McpServer.Cqrs;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace McpServer.Support.Mcp.Controllers;

/// <summary>
/// TR-MCP-MEMORY-004 / TR-MCP-MEMORY-API-002: REST endpoints for MCP memory CRUD
/// and additive CQRS verbs. New verbs dispatch handlers only.
/// </summary>
[ApiController]
[Route("mcpserver/memory")]
public sealed class MemoryController : ControllerBase
{
    private readonly IMemoryService _memoryService;
    private readonly ITransactionGatedMemoryService? _memoryMutations;
    private readonly IDispatcher? _dispatcher;
    private readonly WorkspaceContext? _workspaceContext;

    /// <summary>Initializes a new instance of the <see cref="MemoryController"/> class.</summary>
    public MemoryController(
        IMemoryService memoryService,
        ITransactionGatedMemoryService? memoryMutations = null,
        IDispatcher? dispatcher = null,
        WorkspaceContext? workspaceContext = null)
    {
        _memoryService = memoryService ?? throw new ArgumentNullException(nameof(memoryService));
        _memoryMutations = memoryMutations;
        _dispatcher = dispatcher;
        _workspaceContext = workspaceContext;
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

        var result = _memoryMutations is null
            ? await _memoryService.AddAsync(request, cancellationToken).ConfigureAwait(false)
            : await _memoryMutations.AddAsync(request, cancellationToken).ConfigureAwait(false);
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

        var result = _memoryMutations is null
            ? await _memoryService.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false)
            : await _memoryMutations.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false);
        if (!result.Success)
            return ToMutationFailureResult(result);

        return Ok(result);
    }

    /// <summary>FR-MCP-MEMORY-010 / TR-MCP-MEMORY-API-002: Remember a multi-layer memory.</summary>
    [HttpPost("remember")]
    [ProducesResponseType(typeof(MemoryRememberResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<ActionResult<MemoryRememberResult>> RememberAsync(
        [FromBody] MemoryRememberRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest(MemoryErrorEnvelope.Create(McpErrorClassifier.ValidationError, "Request body is required.", 400));

        if (_memoryMutations is not null)
        {
            var gated = await _memoryMutations.RememberAsync(request, cancellationToken).ConfigureAwait(false);
            return MapStatus(gated);
        }

        return await DispatchNewAsync(
            new RememberMemoryCommand(GetWorkspacePath(), request),
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>FR-MCP-MEMORY-011: Recall memories by meaning and keyword.</summary>
    [HttpPost("recall")]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<ActionResult<MemoryRecallResult>> RecallAsync(
        [FromBody] MemoryRecallRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest(MemoryErrorEnvelope.Create(McpErrorClassifier.ValidationError, "Request body is required.", 400));

        var dispatched = await RequireDispatcher().QueryAsync(
            new RecallMemoryQuery(
                GetWorkspacePath(),
                request.Query,
                request.MinScore,
                request.TopN,
                request.Tags,
                request.Type,
                request.Scope),
            cancellationToken).ConfigureAwait(false);
        return dispatched.IsSuccess && dispatched.Value is not null
            ? MapStatus(dispatched.Value)
            : StatusCode(500, MemoryErrorEnvelope.Create(McpErrorClassifier.InternalError, dispatched.Error ?? "Recall failed.", 500));
    }

    /// <summary>FR-MCP-MEMORY-012: Explore a memory neighborhood.</summary>
    [HttpPost("explore")]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<ActionResult<MemoryExploreResult>> ExploreAsync(
        [FromBody] MemoryExploreRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest(MemoryErrorEnvelope.Create(McpErrorClassifier.ValidationError, "Request body is required.", 400));

        var dispatched = await RequireDispatcher().QueryAsync(
            new ExploreMemoryQuery(
                GetWorkspacePath(),
                request.SeedId,
                request.Query,
                request.Depth,
                request.MaxNeighbors,
                request.HebbianEnabled),
            cancellationToken).ConfigureAwait(false);
        return dispatched.IsSuccess && dispatched.Value is not null
            ? MapStatus(dispatched.Value)
            : StatusCode(500, MemoryErrorEnvelope.Create(McpErrorClassifier.InternalError, dispatched.Error ?? "Explore failed.", 500));
    }

    /// <summary>FR-MCP-MEMORY-013: Plan or apply consolidate/sleep merge.</summary>
    [HttpPost("consolidate")]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<ActionResult<MemoryConsolidateResult>> ConsolidateAsync(
        [FromBody] MemoryConsolidateRequest? request,
        CancellationToken cancellationToken)
    {
        if (_memoryMutations is not null && request?.DryRun == false)
        {
            var gated = await _memoryMutations.ConsolidateAsync(request, cancellationToken).ConfigureAwait(false);
            return MapStatus(gated);
        }

        return await DispatchNewAsync(
            new ConsolidateMemoryCommand(GetWorkspacePath(), request),
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>FR-MCP-MEMORY-015: Promote a session-log or context source into memory.</summary>
    [HttpPost("promote")]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<ActionResult<MemoryPromoteResult>> PromoteAsync(
        [FromBody] MemoryPromoteRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest(MemoryErrorEnvelope.Create(McpErrorClassifier.ValidationError, "Request body is required.", 400));

        if (_memoryMutations is not null)
        {
            var gated = await _memoryMutations.PromoteAsync(request, cancellationToken).ConfigureAwait(false);
            return MapStatus(gated);
        }

        return await DispatchNewAsync(
            new PromoteMemoryCommand(GetWorkspacePath(), request),
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>FR-MCP-MEMORY-014: List versions for one memory.</summary>
    [HttpGet("{id}/versions")]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<ActionResult<MemoryVersionListResult>> ListVersionsAsync(
        string id,
        CancellationToken cancellationToken)
    {
        var dispatched = await RequireDispatcher().QueryAsync(
            new ListMemoryVersionsQuery(GetWorkspacePath(), id),
            cancellationToken).ConfigureAwait(false);
        return dispatched.IsSuccess && dispatched.Value is not null
            ? MapStatus(dispatched.Value)
            : StatusCode(500, MemoryErrorEnvelope.Create(McpErrorClassifier.InternalError, dispatched.Error ?? "Versions failed.", 500));
    }

    /// <summary>FR-MCP-MEMORY-014: Revert a memory to snapshot N.</summary>
    [HttpPost("{id}/revert")]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<ActionResult<MemoryRevertResult>> RevertAsync(
        string id,
        [FromBody] MemoryRevertRequest? request,
        CancellationToken cancellationToken)
    {
        var version = request?.VersionNumber ?? 0;
        if (_memoryMutations is not null)
        {
            var gated = await _memoryMutations.RevertAsync(id, version, cancellationToken).ConfigureAwait(false);
            return MapStatus(gated);
        }

        return await DispatchNewAsync(
            new RevertMemoryCommand(GetWorkspacePath(), id, version),
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Removes one visible memory by id.</summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<MemoryMutationResult>> RemoveAsync(string id, CancellationToken cancellationToken)
    {
        var result = _memoryMutations is null
            ? await _memoryService.RemoveAsync(id, cancellationToken).ConfigureAwait(false)
            : await _memoryMutations.RemoveAsync(id, cancellationToken).ConfigureAwait(false);
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

    private string GetWorkspacePath()
        => _workspaceContext?.WorkspacePath ?? string.Empty;

    private IDispatcher RequireDispatcher()
        => _dispatcher ?? throw new InvalidOperationException("CQRS dispatcher is not registered for memory surfaces.");

    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    private async Task<ActionResult<T>> DispatchNewAsync<T>(ICommand<T> command, CancellationToken cancellationToken)
    {
        var dispatched = await RequireDispatcher().SendAsync(command, cancellationToken).ConfigureAwait(false);
        if (dispatched.IsSuccess && dispatched.Value is not null)
            return MapNewStatus(dispatched.Value);

        return StatusCode(
            500,
            MemoryErrorEnvelope.Create(McpErrorClassifier.InternalError, dispatched.Error ?? "Memory command failed.", 500));
    }

    private ActionResult<T> MapNewStatus<T>(T result)
    {
        var status = result switch
        {
            MemoryRememberResult remember => remember.StatusCode,
            MemoryConsolidateResult consolidate => consolidate.StatusCode,
            MemoryPromoteResult promote => promote.StatusCode,
            MemoryRevertResult revert => revert.StatusCode,
            _ => 200,
        };
        var error = result switch
        {
            MemoryRememberResult remember => remember.Error,
            MemoryConsolidateResult consolidate => consolidate.Error,
            MemoryPromoteResult promote => promote.Error,
            MemoryRevertResult revert => revert.Error,
            _ => null,
        };
        return StatusCode(status, status is >= 400 ? EnvelopeOrResult(status, error, result!) : result);
    }

    private ActionResult<MemoryRememberResult> MapStatus(MemoryRememberResult result)
        => StatusCode(result.StatusCode, result.StatusCode is >= 400 ? EnvelopeOrResult(result.StatusCode, result.Error, result) : result);

    private ActionResult<MemoryRecallResult> MapStatus(MemoryRecallResult result)
        => StatusCode(result.StatusCode, result.StatusCode is >= 400 ? EnvelopeOrResult(result.StatusCode, result.Error, result) : result);

    private ActionResult<MemoryExploreResult> MapStatus(MemoryExploreResult result)
        => StatusCode(result.StatusCode, result.StatusCode is >= 400 ? EnvelopeOrResult(result.StatusCode, result.Error, result) : result);

    private ActionResult<MemoryConsolidateResult> MapStatus(MemoryConsolidateResult result)
        => StatusCode(result.StatusCode, result.StatusCode is >= 400 ? EnvelopeOrResult(result.StatusCode, result.Error, result) : result);

    private ActionResult<MemoryPromoteResult> MapStatus(MemoryPromoteResult result)
        => StatusCode(result.StatusCode, result.StatusCode is >= 400 ? EnvelopeOrResult(result.StatusCode, result.Error, result) : result);

    private ActionResult<MemoryVersionListResult> MapStatus(MemoryVersionListResult result)
        => StatusCode(result.StatusCode, result.StatusCode is >= 400 ? EnvelopeOrResult(result.StatusCode, result.Error, result) : result);

    private ActionResult<MemoryRevertResult> MapStatus(MemoryRevertResult result)
        => StatusCode(result.StatusCode, result.StatusCode is >= 400 ? EnvelopeOrResult(result.StatusCode, result.Error, result) : result);

    private static object EnvelopeOrResult(int status, string? error, object result)
        => string.IsNullOrWhiteSpace(error)
            ? result
            : MemoryErrorEnvelope.Create(
                status == 404 ? McpErrorClassifier.NotFound : status == 409 ? McpErrorClassifier.Conflict : McpErrorClassifier.ValidationError,
                error,
                status);
}
