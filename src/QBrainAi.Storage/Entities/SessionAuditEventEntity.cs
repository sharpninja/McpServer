// SPDX-License-Identifier: Apache-2.0
using System.ComponentModel.DataAnnotations;

namespace QBrainAi.Support.Mcp.Storage.Entities;

/// <summary>FR-QB-003/005: Exact immutable event evidence and committed receipt in the existing session-log store.</summary>
public sealed class SessionAuditEventEntity
{
    /// <summary>The generated committed revision for an accepted event.</summary>
    [Key]
    public long Id { get; set; }
    /// <summary>TR-MCP-MT-003: Canonical tenancy discriminator.</summary>
    public string WorkspaceId { get; set; } = string.Empty;
    /// <summary>The addressed existing turn; this row never creates another lifecycle.</summary>
    public long SessionLogTurnId { get; set; }
    /// <summary>The exact captured workspace spelling; separate from canonical tenant-key conversion.</summary>
    public string CapturedWorkspacePath { get; set; } = string.Empty;
    /// <summary>The captured actual agent identity.</summary>
    [MaxLength(64)]
    public string AgentIdentity { get; set; } = string.Empty;
    /// <summary>The original session identifier.</summary>
    public string McpSessionId { get; set; } = string.Empty;
    /// <summary>The original request identifier.</summary>
    public string RequestId { get; set; } = string.Empty;
    /// <summary>The globally unique immutable event UUID.</summary>
    [MaxLength(36)]
    public string EventId { get; set; } = string.Empty;
    /// <summary>The original positive safe captured order.</summary>
    public long CapturedOrder { get; set; }
    /// <summary>Accepted positions are unique per turn; retained rejections reserve no position.</summary>
    public long? PositionKey { get; set; }
    /// <summary>The explicit captured classification.</summary>
    [MaxLength(16)]
    public string Kind { get; set; } = string.Empty;
    /// <summary>Exact captured UTF-8 JSON text; generic sanitized audit snapshots are not the evidence.</summary>
    public string PayloadJson { get; set; } = string.Empty;
    /// <summary>The lowercase SHA-256 of PayloadJson.</summary>
    [MaxLength(64)]
    public string PayloadHash { get; set; } = string.Empty;
    /// <summary>True only when the event and all its projections committed atomically.</summary>
    public bool Accepted { get; set; }
    /// <summary>The retained classified rejection reason.</summary>
    public string? Reason { get; set; }
    /// <summary>The observed terminal turn status for retained rejection evidence.</summary>
    public string? StoredStatus { get; set; }
}
