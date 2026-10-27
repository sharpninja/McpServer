// SPDX-License-Identifier: Apache-2.0
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;

namespace QBrainAi.Support.Mcp.Controllers;

public sealed partial class SessionLogController
{
    /// <summary>FR-QB-003/005: Commits one immutable event to an existing turn and echoes its actual receipt.</summary>
    [HttpPost("event")]
    public async Task<IActionResult> AppendAuditEventAsync(
        [FromBody] SessionAuditEventRequest auditEvent, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.AppendAuditEventAsync(auditEvent, cancellationToken).ConfigureAwait(false));
        }
        catch (Exception error) when (error is DbUpdateException or ArgumentException or InvalidOperationException or StorageCommandBudgetExceededException)
        {
            return ClassifiedError(error);
        }
    }

    /// <summary>TR-QB-AUDIT-001: Independently reads the exact addressed event receipt.</summary>
    [HttpGet("{agent}/{sessionId}/{requestId}/event/{eventId}")]
    public async Task<IActionResult> ReadAuditEventAsync(
        string agent, string sessionId, string requestId, string eventId, CancellationToken cancellationToken)
    {
        var receipt = await _service.ReadAuditEventAsync(agent, sessionId, requestId, eventId, cancellationToken).ConfigureAwait(false);
        return receipt is null ? NotFound() : Ok(receipt);
    }

    /// <summary>AC-QB-009/069: Retained rejected events block successful completion.</summary>
    [HttpGet("{agent}/{sessionId}/{requestId}/audit-completion")]
    public async Task<IActionResult> CheckAuditCompletionAsync(
        string agent, string sessionId, string requestId, CancellationToken cancellationToken)
        => Ok(await _service.CheckAuditCompletionAsync(agent, sessionId, requestId, cancellationToken).ConfigureAwait(false));
}
