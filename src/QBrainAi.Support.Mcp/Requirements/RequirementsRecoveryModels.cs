namespace QBrainAi.Support.Mcp.Requirements;

/// <summary>
/// FR-MCP-REQRECOVERY-001: recovery request shared by dry-run and apply.
/// </summary>
public sealed class RequirementsRecoveryRequest
{
    /// <summary>Operation mode: <c>dry-run</c> or <c>apply</c>. REST sets this; the service methods ignore it.</summary>
    public string? Mode { get; set; }

    /// <summary>Caller idempotency key, unique per workspace.</summary>
    public string? IdempotencyKey { get; set; }

    /// <summary>Requirement rows to plan or apply.</summary>
    public List<RequirementsRecoveryItemRequest>? Items { get; set; }
}

/// <summary>FR-MCP-REQRECOVERY-001: one FR, TR, or TEST row in a recovery payload.</summary>
public sealed class RequirementsRecoveryItemRequest
{
    /// <summary>Requirement kind: <c>fr</c>, <c>tr</c>, or <c>test</c>.</summary>
    public string? Kind { get; set; }

    /// <summary>Canonical requirement identifier.</summary>
    public string? Id { get; set; }

    /// <summary>Requirement title.</summary>
    public string? Title { get; set; }

    /// <summary>Requirement body or testing condition.</summary>
    public string? Body { get; set; }

    /// <summary>Optional priority. Omitted values default to <c>medium</c>.</summary>
    public string? Priority { get; set; }

    /// <summary>Optional lifecycle status. Omitted values default to <c>pending</c>.</summary>
    public string? Status { get; set; }
}

/// <summary>FR-MCP-REQRECOVERY-001: planned or applied recovery outcome.</summary>
public sealed class RequirementsRecoveryResult
{
    /// <summary>Idempotency key the outcome belongs to.</summary>
    public string IdempotencyKey { get; set; } = string.Empty;

    /// <summary>SHA-256 hex of the canonical payload.</summary>
    public string PayloadHash { get; set; } = string.Empty;

    /// <summary><c>planned</c> for dry-run, <c>applied</c> after a committed apply.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>True when the payload was committed or replayed from a committed run.</summary>
    public bool Applied { get; set; }

    /// <summary>True when apply returned a previously stored result without writing again.</summary>
    public bool Replay { get; set; }

    /// <summary>UTC timestamp of the committed run. Empty for a dry-run.</summary>
    public string CreatedAtUtc { get; set; } = string.Empty;

    /// <summary>Per-item create or update actions.</summary>
    public List<RequirementsRecoveryItemResult> Items { get; set; } = [];
}

/// <summary>FR-MCP-REQRECOVERY-001: planned action for one requirement row.</summary>
public sealed class RequirementsRecoveryItemResult
{
    /// <summary>Requirement kind.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Requirement identifier.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary><c>create</c> when the row is absent, <c>update</c> when it already exists.</summary>
    public string Action { get; set; } = string.Empty;
}
