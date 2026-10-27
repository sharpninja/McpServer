// SPDX-License-Identifier: Apache-2.0
using System;
using System.Threading;
using System.Threading.Tasks;
using QBrainAi.Client.Models;

namespace QBrainAi.Client;

public sealed partial class SessionLogClient
{
    /// <summary>FR-QB-003/005: Commits an immutable captured event through the existing session-log endpoint.</summary>
    public Task<SessionAuditEventReceipt> AppendAuditEventAsync(
        SessionAuditEventRequest auditEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);
        return PostAsync<SessionAuditEventReceipt>("qbrainai/sessionlog/event", auditEvent, cancellationToken);
    }

    /// <summary>TR-QB-AUDIT-001: Reads a committed receipt independently; an absent receipt returns null.</summary>
    public async Task<SessionAuditEventReceipt?> ReadAuditEventAsync(
        string agent, string sessionId, string requestId, string eventId, CancellationToken cancellationToken = default)
    {
        var path = $"qbrainai/sessionlog/{Uri.EscapeDataString(agent)}/{Uri.EscapeDataString(sessionId)}/{Uri.EscapeDataString(requestId)}/event/{Uri.EscapeDataString(eventId)}";
        try
        {
            return await GetAsync<SessionAuditEventReceipt>(path, cancellationToken).ConfigureAwait(false);
        }
        catch (McpNotFoundException)
        {
            return null;
        }
    }

    /// <summary>AC-QB-009/069: Checks retained event rejection evidence before successful completion.</summary>
    public Task<SessionAuditCompletionResult> CheckAuditCompletionAsync(
        string agent, string sessionId, string requestId, CancellationToken cancellationToken = default)
    {
        var path = $"qbrainai/sessionlog/{Uri.EscapeDataString(agent)}/{Uri.EscapeDataString(sessionId)}/{Uri.EscapeDataString(requestId)}/audit-completion";
        return GetAsync<SessionAuditCompletionResult>(path, cancellationToken);
    }
}
