using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace McpServer.Support.Mcp.Controllers;

/// <summary>
/// FR-MCP-129 and FR-MCP-130: REST endpoints for external brain-slot registry and invocation.
/// </summary>
[ApiController]
[Authorize(Policy = "AgentManager")]
[Route("mcpserver/brain-slots")]
public sealed class BrainSlotsController : ControllerBase
{
    private readonly IBrainSlotRegistryService _registry;
    private readonly IBrainSlotInvocationService _invocation;

    /// <summary>Initializes a new instance of the <see cref="BrainSlotsController"/> class.</summary>
    public BrainSlotsController(IBrainSlotRegistryService registry, IBrainSlotInvocationService invocation)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _invocation = invocation ?? throw new ArgumentNullException(nameof(invocation));
    }

    /// <summary>Lists brain-slot definitions visible to the active workspace.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<BrainSlotDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BrainSlotDto>>> ListAsync(CancellationToken cancellationToken)
        => Ok(await _registry.ListAsync(cancellationToken).ConfigureAwait(false));

    /// <summary>Gets one brain-slot definition by id.</summary>
    [HttpGet("{slotId}")]
    [ProducesResponseType(typeof(BrainSlotDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BrainSlotDto>> GetAsync(string slotId, CancellationToken cancellationToken)
    {
        var slot = await _registry.GetAsync(slotId, cancellationToken).ConfigureAwait(false);
        return slot is null ? NotFound(new { error = $"Brain slot '{slotId}' not found." }) : Ok(slot);
    }

    /// <summary>Creates or updates a brain-slot definition.</summary>
    [HttpPut("{slotId}")]
    [ProducesResponseType(typeof(BrainSlotDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BrainSlotDto>> UpsertAsync(string slotId, [FromBody] UpsertBrainSlotRequest? request, CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest(new { error = "Request body is required." });

        try
        {
            return Ok(await _registry.UpsertAsync(slotId, request, cancellationToken).ConfigureAwait(false));
        }
        catch (BrainSlotValidationException ex)
        {
            return BadRequest(new { error = ex.Message, reason = ex.Reason });
        }
        catch (BrainSlotConflictException ex)
        {
            return Conflict(new { error = ex.Message, reason = BrainSlotReasonCodes.EnabledRoleConflict });
        }
    }

    /// <summary>Soft-deletes and disables a brain-slot definition.</summary>
    [HttpDelete("{slotId}")]
    [ProducesResponseType(typeof(BrainSlotDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BrainSlotDto>> DeleteAsync(string slotId, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _registry.DeleteAsync(slotId, cancellationToken).ConfigureAwait(false));
        }
        catch (BrainSlotNotFoundException ex)
        {
            return NotFound(new { error = ex.Message, reason = BrainSlotReasonCodes.SlotNotFound });
        }
    }

    /// <summary>Enables a brain-slot definition.</summary>
    [HttpPost("{slotId}/enable")]
    [ProducesResponseType(typeof(BrainSlotDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BrainSlotDto>> EnableAsync(string slotId, [FromQuery] bool replaceExisting, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _registry.EnableAsync(slotId, replaceExisting, cancellationToken).ConfigureAwait(false));
        }
        catch (BrainSlotNotFoundException ex)
        {
            return NotFound(new { error = ex.Message, reason = BrainSlotReasonCodes.SlotNotFound });
        }
        catch (BrainSlotConflictException ex)
        {
            return Conflict(new { error = ex.Message, reason = BrainSlotReasonCodes.EnabledRoleConflict });
        }
    }

    /// <summary>Disables a brain-slot definition.</summary>
    [HttpPost("{slotId}/disable")]
    [ProducesResponseType(typeof(BrainSlotDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BrainSlotDto>> DisableAsync(string slotId, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _registry.DisableAsync(slotId, cancellationToken).ConfigureAwait(false));
        }
        catch (BrainSlotNotFoundException ex)
        {
            return NotFound(new { error = ex.Message, reason = BrainSlotReasonCodes.SlotNotFound });
        }
    }

    /// <summary>Gets quad-readiness status for the active workspace.</summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(BrainSlotStatusResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BrainSlotStatusResponse>> GetStatusAsync(CancellationToken cancellationToken)
        => Ok(await _registry.GetStatusAsync(cancellationToken).ConfigureAwait(false));

    /// <summary>Invokes a configured brain slot.</summary>
    [HttpPost("{slotId}/invoke")]
    [ProducesResponseType(typeof(BrainSlotInvokeResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BrainSlotInvokeResponse>> InvokeAsync(
        string slotId,
        [FromBody] BrainSlotInvokeRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest(new { error = "Request body is required." });

        return Ok(await _invocation.InvokeAsync(slotId, request, cancellationToken).ConfigureAwait(false));
    }
}
