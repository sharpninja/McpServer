// SPDX-License-Identifier: Apache-2.0
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QBrainAi.Client.Models;

/// <summary>FR-QB-003/005: Immutable captured event addressed to an existing session-log turn.</summary>
public sealed class SessionAuditEventRequest
{
    /// <summary>The exact captured workspace path; tenancy is verified independently.</summary>
    [JsonPropertyName("workspacePath")]
    public string WorkspacePath { get; set; } = string.Empty;
    /// <summary>The actual agent identity that owns the session.</summary>
    [JsonPropertyName("agentIdentity")]
    public string AgentIdentity { get; set; } = string.Empty;
    /// <summary>The existing MCP session identifier.</summary>
    [JsonPropertyName("mcpSessionId")]
    public string McpSessionId { get; set; } = string.Empty;
    /// <summary>The existing request identifier; events cannot mint turns.</summary>
    [JsonPropertyName("requestId")]
    public string RequestId { get; set; } = string.Empty;
    /// <summary>The immutable nonempty UUID assigned at capture.</summary>
    [JsonPropertyName("eventId")]
    public string EventId { get; set; } = string.Empty;
    /// <summary>The captured positive safe integer; raw JSON permits classified malformed-input rejection.</summary>
    [JsonPropertyName("order")]
    public JsonElement Order { get; set; }
    /// <summary>Explicit action, decision, dialog or metadata classification.</summary>
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;
    /// <summary>The exact captured JSON object text, retained without reserialization.</summary>
    [JsonPropertyName("payloadJson")]
    public string PayloadJson { get; set; } = string.Empty;
    /// <summary>The lowercase SHA-256 of the exact UTF-8 payload text.</summary>
    [JsonPropertyName("payloadHash")]
    public string PayloadHash { get; set; } = string.Empty;
}

/// <summary>TR-QB-AUDIT-001: A committed event receipt, distinct from a turn-write outcome.</summary>
public sealed class SessionAuditEventReceipt
{
    /// <summary>The exact captured workspace path.</summary>
    [JsonPropertyName("workspacePath")]
    public string WorkspacePath { get; set; } = string.Empty;
    /// <summary>The actual owning agent identity.</summary>
    [JsonPropertyName("agentIdentity")]
    public string AgentIdentity { get; set; } = string.Empty;
    /// <summary>The owning session identifier.</summary>
    [JsonPropertyName("mcpSessionId")]
    public string McpSessionId { get; set; } = string.Empty;
    /// <summary>The owning request identifier.</summary>
    [JsonPropertyName("requestId")]
    public string RequestId { get; set; } = string.Empty;
    /// <summary>The captured event UUID.</summary>
    [JsonPropertyName("eventId")]
    public string EventId { get; set; } = string.Empty;
    /// <summary>The unchanged captured position.</summary>
    [JsonPropertyName("order")]
    public long Order { get; set; }
    /// <summary>The unchanged captured payload digest.</summary>
    [JsonPropertyName("payloadHash")]
    public string PayloadHash { get; set; } = string.Empty;
    /// <summary>Persisted, unchanged or rejected; no synthetic success is returned.</summary>
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;
    /// <summary>The original committed positive revision; absent for a rejected event.</summary>
    [JsonPropertyName("revision")]
    public long? Revision { get; set; }
    /// <summary>The classified rejection reason, when applicable.</summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
    /// <summary>The observed terminal turn status retained with a terminal rejection.</summary>
    [JsonPropertyName("storedStatus")]
    public string? StoredStatus { get; set; }
}

/// <summary>AC-QB-009/069: Retained rejected events block successful completion.</summary>
public sealed class SessionAuditCompletionResult
{
    /// <summary>Whether no retained event rejection prevents completion.</summary>
    [JsonPropertyName("allowed")]
    public bool Allowed { get; set; }
    /// <summary>The number of retained rejected events for the addressed turn.</summary>
    [JsonPropertyName("rejectedCount")]
    public int RejectedCount { get; set; }
}
