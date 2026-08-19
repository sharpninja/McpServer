using System.Diagnostics.CodeAnalysis;
using McpServer.Cqrs;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.UseCases;
using McpServer.Support.Mcp.UseCases.Commands;
using McpServer.Support.Mcp.UseCases.Models;
using McpServer.Support.Mcp.UseCases.Queries;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace McpServer.Support.Mcp.Controllers;

/// <summary>
/// TR-MCP-USECASE-003 / FR-MCP-USECASE-001..006: Thin REST controller for use cases.
/// Builds CQRS messages and maps <see cref="Result{T}"/> to HTTP status codes only.
/// </summary>
[ApiController]
[Route("mcpserver/usecases")]
[Produces("application/json")]
public sealed class UseCasesController : ControllerBase
{
    private readonly IDispatcher _dispatcher;
    private readonly WorkspaceContext _workspaceContext;

    /// <summary>TR-MCP-USECASE-003: Initializes the controller with CQRS dispatcher and workspace context.</summary>
    public UseCasesController(IDispatcher dispatcher, WorkspaceContext workspaceContext)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _workspaceContext = workspaceContext ?? throw new ArgumentNullException(nameof(workspaceContext));
    }

    /// <summary>FR-MCP-USECASE-001: Creates a use case.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(UseCaseDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<ActionResult<UseCaseDetailDto>> CreateAsync(
        [FromBody] CreateUseCaseRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest(ClassifiedPayload(McpErrorClassifier.ValidationError, "Request body is required.", 400));

        var result = await _dispatcher.SendAsync(
            new CreateUseCaseCommand(GetWorkspacePath(), request),
            cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
            return MapFailure(result);

        return Created(
            new Uri($"/mcpserver/usecases/{result.Value!.UseCaseId}", UriKind.Relative),
            result.Value);
    }

    /// <summary>FR-MCP-USECASE-001: Lists use cases, optional title filter.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UseCaseSummaryDto>), StatusCodes.Status200OK)]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<ActionResult<IReadOnlyList<UseCaseSummaryDto>>> ListAsync(
        [FromQuery] string? title = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _dispatcher.QueryAsync(
            new ListUseCasesQuery(GetWorkspacePath(), title),
            cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
            return MapFailure(result);
        return Ok(result.Value);
    }

    /// <summary>FR-MCP-USECASE-006: Reports Realizes UC↔FR coverage gaps.</summary>
    [HttpGet("coverage")]
    [ProducesResponseType(typeof(UseCaseFrCoverageDto), StatusCodes.Status200OK)]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<ActionResult<UseCaseFrCoverageDto>> CoverageAsync(CancellationToken cancellationToken)
    {
        var result = await _dispatcher.QueryAsync(
            new GetUseCaseFrCoverageQuery(GetWorkspacePath()),
            cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
            return MapFailure(result);
        return Ok(result.Value);
    }

    /// <summary>FR-MCP-USECASE-009: Lists use cases sharing a product key.</summary>
    [HttpGet("by-product/{productKey}")]
    [ProducesResponseType(typeof(IReadOnlyList<UseCaseSummaryDto>), StatusCodes.Status200OK)]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<ActionResult<IReadOnlyList<UseCaseSummaryDto>>> ListByProductAsync(
        string productKey,
        CancellationToken cancellationToken)
    {
        var result = await _dispatcher.QueryAsync(
            new ListUseCasesByProductQuery(GetWorkspacePath(), productKey),
            cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
            return MapFailure(result);
        return Ok(result.Value);
    }

    /// <summary>FR-MCP-USECASE-008: Sets approval status for a use case.</summary>
    [HttpPost("{id:long}/approval")]
    [ProducesResponseType(typeof(UseCaseDetailDto), StatusCodes.Status200OK)]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<ActionResult<UseCaseDetailDto>> SetApprovalAsync(
        long id,
        [FromBody] SetApprovalRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Status))
            return BadRequest(ClassifiedPayload(McpErrorClassifier.ValidationError, "Status is required.", 400));

        var result = await _dispatcher.SendAsync(
            new SetUseCaseApprovalStatusCommand(GetWorkspacePath(), id, request.Status),
            cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
            return MapFailure(result);
        return Ok(result.Value);
    }

    /// <summary>FR-MCP-USECASE-009: Sets product membership key.</summary>
    [HttpPost("{id:long}/product")]
    [ProducesResponseType(typeof(UseCaseDetailDto), StatusCodes.Status200OK)]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<ActionResult<UseCaseDetailDto>> SetProductAsync(
        long id,
        [FromBody] SetProductRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _dispatcher.SendAsync(
            new SetUseCaseProductKeyCommand(GetWorkspacePath(), id, request?.ProductKey),
            cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
            return MapFailure(result);
        return Ok(result.Value);
    }

    /// <summary>FR-MCP-USECASE-004: Creates a shell use case from a functional requirement.</summary>
    [HttpPost("from-fr/{frId}")]
    [ProducesResponseType(typeof(UseCaseDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<ActionResult<UseCaseDetailDto>> CreateFromFrAsync(
        string frId,
        CancellationToken cancellationToken)
    {
        var result = await _dispatcher.SendAsync(
            new CreateUseCaseFromFrCommand(GetWorkspacePath(), frId),
            cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
            return MapFailure(result);

        return Created(
            new Uri($"/mcpserver/usecases/{result.Value!.UseCaseId}", UriKind.Relative),
            result.Value);
    }

    /// <summary>FR-MCP-USECASE-001: Gets a use case by id.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(UseCaseDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<ActionResult<UseCaseDetailDto>> GetAsync(long id, CancellationToken cancellationToken)
    {
        var result = await _dispatcher.QueryAsync(
            new GetUseCaseQuery(GetWorkspacePath(), id),
            cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
            return MapFailure(result);
        return Ok(result.Value);
    }

    /// <summary>FR-MCP-USECASE-001: Updates use case header fields.</summary>
    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(UseCaseDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<ActionResult<UseCaseDetailDto>> UpdateAsync(
        long id,
        [FromBody] UpdateUseCaseRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest(ClassifiedPayload(McpErrorClassifier.ValidationError, "Request body is required.", 400));

        var result = await _dispatcher.SendAsync(
            new UpdateUseCaseCommand(GetWorkspacePath(), id, request),
            cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
            return MapFailure(result);
        return Ok(result.Value);
    }

    /// <summary>FR-MCP-USECASE-001: Soft-deletes a use case.</summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<IActionResult> DeleteAsync(long id, CancellationToken cancellationToken)
    {
        var result = await _dispatcher.SendAsync(
            new DeleteUseCaseCommand(GetWorkspacePath(), id),
            cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
            return MapFailure(result);
        return NoContent();
    }

    /// <summary>FR-MCP-USECASE-002: Adds a flow to a use case.</summary>
    [HttpPost("{id:long}/flows")]
    [ProducesResponseType(typeof(UseCaseFlowDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<ActionResult<UseCaseFlowDto>> AddFlowAsync(
        long id,
        [FromBody] AddUseCaseFlowRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest(ClassifiedPayload(McpErrorClassifier.ValidationError, "Request body is required.", 400));

        var result = await _dispatcher.SendAsync(
            new AddUseCaseFlowCommand(GetWorkspacePath(), id, request),
            cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
            return MapFailure(result);

        return Created(
            new Uri($"/mcpserver/usecases/{id}/flows/{result.Value!.FlowId}", UriKind.Relative),
            result.Value);
    }

    /// <summary>FR-MCP-USECASE-002: Adds a step to a flow.</summary>
    [HttpPost("{id:long}/flows/{flowId:long}/steps")]
    [ProducesResponseType(typeof(UseCaseStepDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<ActionResult<UseCaseStepDto>> AddStepAsync(
        long id,
        long flowId,
        [FromBody] CreateUseCaseStepRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest(ClassifiedPayload(McpErrorClassifier.ValidationError, "Request body is required.", 400));

        var result = await _dispatcher.SendAsync(
            new AddUseCaseStepCommand(GetWorkspacePath(), id, flowId, request),
            cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
            return MapFailure(result);

        return Created(
            new Uri($"/mcpserver/usecases/{id}/flows/{flowId}/steps/{result.Value!.StepId}", UriKind.Relative),
            result.Value);
    }

    /// <summary>FR-MCP-USECASE-002: Attaches an actor to a use case.</summary>
    [HttpPost("{id:long}/actors")]
    [ProducesResponseType(typeof(UseCaseActorDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<ActionResult<UseCaseActorDto>> AttachActorAsync(
        long id,
        [FromBody] AttachUseCaseActorRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest(ClassifiedPayload(McpErrorClassifier.ValidationError, "Request body is required.", 400));

        var result = await _dispatcher.SendAsync(
            new AttachUseCaseActorCommand(GetWorkspacePath(), id, request),
            cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
            return MapFailure(result);

        return Created(
            new Uri($"/mcpserver/usecases/{id}/actors/{result.Value!.ActorId}", UriKind.Relative),
            result.Value);
    }

    /// <summary>FR-MCP-USECASE-003: Links a use case to a functional requirement.</summary>
    [HttpPost("{id:long}/links")]
    [ProducesResponseType(typeof(UseCaseFrLinkDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<ActionResult<UseCaseFrLinkDto>> LinkFrAsync(
        long id,
        [FromBody] LinkUseCaseToFrRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest(ClassifiedPayload(McpErrorClassifier.ValidationError, "Request body is required.", 400));

        var result = await _dispatcher.SendAsync(
            new LinkUseCaseToFrCommand(
                GetWorkspacePath(),
                id,
                request.FrId,
                request.LinkType,
                request.LinkOrder,
                request.Notes),
            cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
            return MapFailure(result);

        return Created(
            new Uri($"/mcpserver/usecases/{id}/links/{Uri.EscapeDataString(result.Value!.FrId)}", UriKind.Relative),
            result.Value);
    }

    /// <summary>FR-MCP-USECASE-003: Unlinks a use case from a functional requirement.</summary>
    [HttpDelete("{id:long}/links/{frId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<IActionResult> UnlinkFrAsync(long id, string frId, CancellationToken cancellationToken)
    {
        var result = await _dispatcher.SendAsync(
            new UnlinkUseCaseFromFrCommand(GetWorkspacePath(), id, frId),
            cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
            return MapFailure(result);
        return NoContent();
    }

    /// <summary>FR-MCP-USECASE-005: Returns a diagram for the use case (sequence default or UML graph export).</summary>
    [HttpGet("{id:long}/diagram")]
    [ProducesResponseType(typeof(UseCaseDiagramDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<ActionResult<UseCaseDiagramDto>> DiagramAsync(
        long id,
        [FromQuery] string format = "mermaid",
        [FromQuery] string kind = "sequence",
        CancellationToken cancellationToken = default)
    {
        if (string.Equals(kind, "usecase", StringComparison.OrdinalIgnoreCase)
            || string.Equals(kind, "uml", StringComparison.OrdinalIgnoreCase))
        {
            var graphResult = await _dispatcher.QueryAsync(
                new GetUseCaseDiagramGraphQuery(GetWorkspacePath(), id),
                cancellationToken).ConfigureAwait(false);
            if (graphResult.IsFailure)
                return MapFailure(graphResult);

            var serializer = HttpContext.RequestServices.GetRequiredService<IUseCaseUmlSerializationService>();
            string content;
            try
            {
                content = format.Trim().ToLowerInvariant() switch
                {
                    "plantuml" => serializer.ToPlantUml(graphResult.Value!),
                    _ => serializer.ToMermaid(graphResult.Value!),
                };
            }
            catch (Exception ex)
            {
                var classified = McpErrorClassifier.Classify(ex);
                return StatusCode(
                    classified.StatusCode,
                    ClassifiedPayload(classified.Code, classified.Message, classified.StatusCode, classified.Retryable, classified.Details));
            }

            return Ok(new UseCaseDiagramDto
            {
                UseCaseId = id,
                Format = format,
                Content = content,
            });
        }

        var result = await _dispatcher.QueryAsync(
            new GetUseCaseDiagramQuery(GetWorkspacePath(), id, format),
            cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
            return MapFailure(result);
        return Ok(result.Value);
    }

    /// <summary>FR-MCP-USECASE-012: Gets the UML diagram graph for canvas load.</summary>
    [HttpGet("{id:long}/diagram-graph")]
    [ProducesResponseType(typeof(UseCaseDiagramGraphDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<ActionResult<UseCaseDiagramGraphDto>> GetDiagramGraphAsync(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await _dispatcher.QueryAsync(
            new GetUseCaseDiagramGraphQuery(GetWorkspacePath(), id),
            cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
            return MapFailure(result);
        return Ok(result.Value);
    }

    /// <summary>FR-MCP-USECASE-012: Replaces the UML diagram graph from the canvas editor.</summary>
    [HttpPut("{id:long}/diagram-graph")]
    [ProducesResponseType(typeof(UseCaseDiagramGraphDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<ActionResult<UseCaseDiagramGraphDto>> PutDiagramGraphAsync(
        long id,
        [FromBody] UseCaseDiagramGraphDto? graph,
        CancellationToken cancellationToken)
    {
        if (graph is null)
            return BadRequest(ClassifiedPayload(McpErrorClassifier.ValidationError, "Graph body is required.", 400));

        var result = await _dispatcher.SendAsync(
            new PutUseCaseDiagramGraphCommand(GetWorkspacePath(), id, graph),
            cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
            return MapFailure(result);
        return Ok(result.Value);
    }

    private string GetWorkspacePath()
        => _workspaceContext.WorkspacePath ?? string.Empty;

    private ActionResult MapFailure(Result result)
        => MapFailureCore(result.Error, result.Exception);

    private ActionResult MapFailure<T>(Result<T> result)
        => MapFailureCore(result.Error, result.Exception);

    private ActionResult MapFailureCore(string? error, Exception? exception)
    {
        var message = string.IsNullOrWhiteSpace(error) ? "Unexpected use case operation failure." : error;
        if (message.StartsWith(UseCaseResultCodes.NotFound, StringComparison.Ordinal))
            return NotFound(ClassifiedPayload(McpErrorClassifier.NotFound, StripPrefix(message, UseCaseResultCodes.NotFound), 404));
        if (message.StartsWith(UseCaseResultCodes.Validation, StringComparison.Ordinal))
            return BadRequest(ClassifiedPayload(McpErrorClassifier.ValidationError, StripPrefix(message, UseCaseResultCodes.Validation), 400));
        if (message.StartsWith(UseCaseResultCodes.Conflict, StringComparison.Ordinal))
            return Conflict(ClassifiedPayload(McpErrorClassifier.Conflict, StripPrefix(message, UseCaseResultCodes.Conflict), 409));

        if (exception is not null)
        {
            var classifiedFromException = McpErrorClassifier.Classify(exception);
            return StatusCode(
                classifiedFromException.StatusCode,
                ClassifiedPayload(
                    classifiedFromException.Code,
                    classifiedFromException.Message,
                    classifiedFromException.StatusCode,
                    classifiedFromException.Retryable,
                    classifiedFromException.Details));
        }

        if (message.Contains("not found", StringComparison.OrdinalIgnoreCase))
            return NotFound(ClassifiedPayload(McpErrorClassifier.NotFound, message, 404));
        if (message.Contains("already exists", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("conflict", StringComparison.OrdinalIgnoreCase))
            return Conflict(ClassifiedPayload(McpErrorClassifier.Conflict, message, 409));
        if (message.Contains("required", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("invalid", StringComparison.OrdinalIgnoreCase))
            return BadRequest(ClassifiedPayload(McpErrorClassifier.ValidationError, message, 400));

        var classified = McpErrorClassifier.Classify(new InvalidOperationException(message));
        return StatusCode(classified.StatusCode, ClassifiedPayload(classified.Code, classified.Message, classified.StatusCode, classified.Retryable, classified.Details));
    }

    private static object ClassifiedPayload(
        string code,
        string message,
        int status,
        bool retryable = false,
        IReadOnlyDictionary<string, object?>? details = null)
        => new
        {
            type = "https://httpstatuses.io/" + status,
            title = code,
            status,
            detail = message,
            code,
            message,
            retryable,
            details,
            error = code,
        };

    private static string StripPrefix(string message, string prefix)
        => message.Length > prefix.Length ? message[prefix.Length..].TrimStart() : message;
}

/// <summary>FR-MCP-USECASE-008: Request body for approval status transition.</summary>
public sealed class SetApprovalRequest
{
    /// <summary>Target status: Draft, Submitted, Approved, or Rejected.</summary>
    public string Status { get; set; } = string.Empty;
}

/// <summary>FR-MCP-USECASE-009: Request body for product key assignment.</summary>
public sealed class SetProductRequest
{
    /// <summary>Product key, or null/empty to clear.</summary>
    public string? ProductKey { get; set; }
}
