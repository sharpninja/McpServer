using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace McpServer.Client.Models;

/// <summary>FR-MCP-REQRECOVERY-001: recovery payload for planRecovery and applyRecovery.</summary>
public sealed class RequirementsRecoveryRequest
{
    /// <summary>REST mode. The client sets <c>dry-run</c> or <c>apply</c>.</summary>
    [JsonPropertyName("mode")]
    public string? Mode { get; set; }

    /// <summary>Caller idempotency key.</summary>
    [JsonPropertyName("idempotencyKey")]
    public string? IdempotencyKey { get; set; }

    /// <summary>Requirement rows.</summary>
    [JsonPropertyName("items")]
    public IReadOnlyList<RequirementsRecoveryItem> Items { get; set; } = [];
}

/// <summary>FR-MCP-REQRECOVERY-001: one FR, TR, or TEST row in a recovery payload.</summary>
public sealed class RequirementsRecoveryItem
{
    /// <summary>Requirement kind: fr, tr, or test.</summary>
    [JsonPropertyName("kind")]
    public string? Kind { get; set; }

    /// <summary>Canonical requirement identifier.</summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>Requirement title.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>Requirement body or testing condition.</summary>
    [JsonPropertyName("body")]
    public string? Body { get; set; }

    /// <summary>Optional priority.</summary>
    [JsonPropertyName("priority")]
    public string? Priority { get; set; }

    /// <summary>Optional status.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

/// <summary>FR-MCP-REQRECOVERY-001: planned, applied, or stored recovery result.</summary>
public sealed class RequirementsRecoveryResult
{
    /// <summary>Idempotency key.</summary>
    [JsonPropertyName("idempotencyKey")]
    public string IdempotencyKey { get; set; } = string.Empty;

    /// <summary>Canonical payload hash.</summary>
    [JsonPropertyName("payloadHash")]
    public string PayloadHash { get; set; } = string.Empty;

    /// <summary>planned or applied.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>True when the payload was committed or replayed.</summary>
    [JsonPropertyName("applied")]
    public bool Applied { get; set; }

    /// <summary>True when apply returned a stored result without writing.</summary>
    [JsonPropertyName("replay")]
    public bool Replay { get; set; }

    /// <summary>UTC timestamp of the committed run.</summary>
    [JsonPropertyName("createdAtUtc")]
    public string CreatedAtUtc { get; set; } = string.Empty;

    /// <summary>Per-item actions.</summary>
    [JsonPropertyName("items")]
    public IReadOnlyList<RequirementsRecoveryItemResult> Items { get; set; } = [];
}

/// <summary>FR-MCP-REQRECOVERY-001: create or update action for one requirement.</summary>
public sealed class RequirementsRecoveryItemResult
{
    /// <summary>Requirement kind.</summary>
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;

    /// <summary>Requirement identifier.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>create or update.</summary>
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;
}
