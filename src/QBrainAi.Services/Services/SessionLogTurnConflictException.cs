// SPDX-License-Identifier: Apache-2.0
namespace QBrainAi.Support.Mcp.Services;

/// <summary>
/// TR-MCP-DB-003 / FR-SUPPORT-010G (triage-report-1af9a7de; operator decision 2026-10-13 "Refuse with clear conflict"): a
/// session-log submit or turn upsert named a turn whose row is deleted. The deleted row keeps the unique (session, request id)
/// key, so that request id is retired and the turn is not recreated; the shared classifier maps this error to a 409 conflict.
/// </summary>
public sealed class SessionLogTurnConflictException : InvalidOperationException
{
    /// <summary>Initializes the conflict for one retired request id.</summary>
    /// <param name="sourceType">The session's source type (agent).</param>
    /// <param name="sessionId">The session id.</param>
    /// <param name="requestId">The retired request id.</param>
    public SessionLogTurnConflictException(string sourceType, string sessionId, string requestId)
        : base($"Turn {requestId} of session {sourceType}/{sessionId} was deleted, so its request id is retired; use a new request id.")
    {
        SourceType = sourceType;
        SessionId = sessionId;
        RequestId = requestId;
    }

    /// <summary>The session's source type (agent).</summary>
    public string SourceType { get; }

    /// <summary>The session id.</summary>
    public string SessionId { get; }

    /// <summary>The retired request id.</summary>
    public string RequestId { get; }
}
