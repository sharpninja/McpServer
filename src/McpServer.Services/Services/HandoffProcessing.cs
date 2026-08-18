namespace McpServer.Support.Mcp.Services;

/// <summary>TR-HANDOFF-AUDIT-001: Durable processing state for a reserved handoff run.</summary>
public enum HandoffProcessingState
{
    /// <summary>No processing lease is active.</summary>
    None = 0,

    /// <summary>A service instance holds a live processing or approval lease.</summary>
    Processing = 1,

    /// <summary>The run reached a terminal success, failure, rejection, or cancellation.</summary>
    Terminal = 2,
}

/// <summary>TR-HANDOFF-AUDIT-001: Stable machine-readable handoff outcome codes.</summary>
public static class HandoffErrorCodes
{
    /// <summary>Another instance is still processing the same replay identity.</summary>
    public const string InProgress = "handoff_in_progress";

    /// <summary>The caller cancelled after a durable reservation.</summary>
    public const string Cancelled = "handoff_cancelled";

    /// <summary>Extraction or later pipeline work failed.</summary>
    public const string ProcessingFailed = "handoff_processing_failed";

    /// <summary>A caller-owned TODO id already exists.</summary>
    public const string TodoCollision = "todo_collision";

    /// <summary>The TODO service rejected create for a reason other than this run's heal.</summary>
    public const string TodoCreateFailed = "todo_create_failed";

    /// <summary>The run cannot be approved in its current state.</summary>
    public const string RunNotApprovable = "run_not_approvable";

    /// <summary>The requested run does not exist.</summary>
    public const string RunNotFound = "run_not_found";

    /// <summary>This instance lost the processing or approval fence.</summary>
    public const string LostOwnership = "handoff_lost_ownership";

    /// <summary>A concurrent claimant holds a newer state version.</summary>
    public const string ConcurrencyConflict = "handoff_concurrency";

    /// <summary>Decoded source exceeded the 8 MiB bound.</summary>
    public const string SourceOversized = "source_oversized";

    /// <summary>TODO was created but the durable run receipt could not be confirmed.</summary>
    public const string CompensationFailed = "handoff_compensation_failed";

    /// <summary>Mode was missing, numeric, or not a defined enum value.</summary>
    public const string InvalidMode = "invalid_mode";

    /// <summary>Caller selected a non-canonical prompt template.</summary>
    public const string InvalidPromptTemplate = "invalid_prompt_template";
}

/// <summary>TR-HANDOFF-AUDIT-001: Lease, heartbeat, and compensation timeouts.</summary>
public sealed class HandoffLeaseOptions
{
    /// <summary>How long an ingest or approval lease remains exclusive without renewal.</summary>
    public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>How often a live owner renews the lease.</summary>
    public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>Bounded non-cancelled token used to persist compensation after caller cancellation.</summary>
    public TimeSpan CompensationTimeout { get; set; } = TimeSpan.FromSeconds(15);
}

/// <summary>TR-HANDOFF-AUDIT-001: Default lease values used when options are not injected.</summary>
public static class HandoffLeaseDefaults
{
    /// <summary>Default exclusive lease duration.</summary>
    public static readonly TimeSpan Duration = TimeSpan.FromSeconds(30);

    /// <summary>Default compensation timeout.</summary>
    public static readonly TimeSpan CompensationTimeout = TimeSpan.FromSeconds(15);
}
