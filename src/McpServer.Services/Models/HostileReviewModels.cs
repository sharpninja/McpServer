namespace McpServer.Support.Mcp.Models;

/// <summary>FR-MCP-HOSTILEREVIEW-001: Artifact link on a submit request. Identity only.</summary>
public sealed class HostileReviewArtifactLink
{
    /// <summary>Artifact type.</summary>
    public string ArtifactType { get; set; } = string.Empty;

    /// <summary>Locator or id.</summary>
    public string ArtifactId { get; set; } = string.Empty;
}

/// <summary>FR-MCP-HOSTILEREVIEW-001: Submit request. Bodies are never persisted.</summary>
public sealed class HostileReviewSubmitRequest
{
    /// <summary>Target type.</summary>
    public string TargetType { get; set; } = "code";

    /// <summary>Review mode.</summary>
    public string Mode { get; set; } = "adversarial";

    /// <summary>Scope statement.</summary>
    public string ScopeStatement { get; set; } = string.Empty;

    /// <summary>Requester identity.</summary>
    public string RequestingAgent { get; set; } = string.Empty;

    /// <summary>Explicit workspace path. Foreign workspace is 403.</summary>
    public string? WorkspacePath { get; set; }

    /// <summary>Artifact links.</summary>
    public List<HostileReviewArtifactLink> Links { get; set; } = [];

    /// <summary>Optional serialized payload used to enforce the 1 MiB bound in tests.</summary>
    public string? SerializedPayload { get; set; }
}

/// <summary>FR-MCP-HOSTILEREVIEW-004: Request-quality scores.</summary>
public sealed class HostileReviewRequestQuality
{
    /// <summary>Disclosure adequacy 0-1.</summary>
    public double Disclosure { get; set; }

    /// <summary>Scope clarity 0-1.</summary>
    public double ScopeClarity { get; set; }

    /// <summary>Objective clarity 0-1.</summary>
    public double ObjectiveClarity { get; set; }

    /// <summary>Reviewer confidence 0-1.</summary>
    public double Confidence { get; set; }

    /// <summary>Enough context for a defensible review.</summary>
    public bool EnoughContext { get; set; }
}

/// <summary>FR-MCP-HOSTILEREVIEW-004: Normalized finding.</summary>
public sealed class HostileReviewFinding
{
    /// <summary>Taxonomy category.</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Severity.</summary>
    public string Severity { get; set; } = "medium";

    /// <summary>Artifact identity.</summary>
    public string? ArtifactId { get; set; }

    /// <summary>Location.</summary>
    public string? Location { get; set; }

    /// <summary>Requirement id.</summary>
    public string? RequirementId { get; set; }

    /// <summary>Sanitized evidence.</summary>
    public string? EvidenceSummary { get; set; }

    /// <summary>Recommendation.</summary>
    public string? Recommendation { get; set; }
}

/// <summary>TR-MCP-HOSTILEREVIEW-003: Execution metadata.</summary>
public sealed class HostileReviewExecution
{
    /// <summary>Execution id.</summary>
    public string ExecutionId { get; set; } = string.Empty;

    /// <summary>Reviewer agent.</summary>
    public string ReviewerAgent { get; set; } = string.Empty;

    /// <summary>Model.</summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>Effort.</summary>
    public string Effort { get; set; } = string.Empty;

    /// <summary>Source surface.</summary>
    public string? SourceSurface { get; set; }

    /// <summary>Prompt template id.</summary>
    public string? PromptTemplateId { get; set; }

    /// <summary>Prompt version.</summary>
    public string? PromptVersion { get; set; }

    /// <summary>Start UTC.</summary>
    public DateTimeOffset StartedUtc { get; set; }

    /// <summary>End UTC.</summary>
    public DateTimeOffset? EndedUtc { get; set; }

    /// <summary>Input tokens when supplied.</summary>
    public int? InputTokens { get; set; }

    /// <summary>Output tokens when supplied.</summary>
    public int? OutputTokens { get; set; }
}

/// <summary>FR-MCP-HOSTILEREVIEW-001: Submit/get/query result. Never contains assembled prompts or raw bodies.</summary>
public sealed class HostileReviewResult
{
    /// <summary>True when the operation succeeded.</summary>
    public bool Success { get; set; }

    /// <summary>HTTP-equivalent status. 403 for foreign workspace.</summary>
    public int HttpStatus { get; set; } = 200;

    /// <summary>Error text.</summary>
    public string? Error { get; set; }

    /// <summary>Error code.</summary>
    public string? ErrorCode { get; set; }

    /// <summary>Request id.</summary>
    public string? RequestId { get; set; }

    /// <summary>Queue status.</summary>
    public string? Status { get; set; }

    /// <summary>Verdict AGREE/DISAGREE/UNKNOWN.</summary>
    public string? Verdict { get; set; }

    /// <summary>Target type.</summary>
    public string? TargetType { get; set; }

    /// <summary>Requesting agent.</summary>
    public string? RequestingAgent { get; set; }

    /// <summary>Diagnostics.</summary>
    public List<string> Diagnostics { get; set; } = [];

    /// <summary>Resolved artifact identities.</summary>
    public List<HostileReviewArtifactLink> Links { get; set; } = [];

    /// <summary>Executions.</summary>
    public List<HostileReviewExecution> Executions { get; set; } = [];

    /// <summary>Findings.</summary>
    public List<HostileReviewFinding> Findings { get; set; } = [];

    /// <summary>Request quality.</summary>
    public HostileReviewRequestQuality? RequestQuality { get; set; }
}

/// <summary>FR-MCP-HOSTILEREVIEW-005: Query filters combine with AND.</summary>
public sealed class HostileReviewQueryRequest
{
    /// <summary>Model filter.</summary>
    public string? Model { get; set; }

    /// <summary>Effort filter.</summary>
    public string? Effort { get; set; }

    /// <summary>Requesting agent filter.</summary>
    public string? RequestingAgent { get; set; }

    /// <summary>Reviewer agent filter.</summary>
    public string? ReviewerAgent { get; set; }

    /// <summary>Target type filter.</summary>
    public string? TargetType { get; set; }

    /// <summary>Artifact type filter.</summary>
    public string? ArtifactType { get; set; }

    /// <summary>Finding severity filter.</summary>
    public string? Severity { get; set; }

    /// <summary>Finding category filter.</summary>
    public string? Category { get; set; }

    /// <summary>Offset.</summary>
    public int Offset { get; set; }

    /// <summary>Bounded limit.</summary>
    public int Limit { get; set; } = 50;
}

/// <summary>TR-MCP-HOSTILEREVIEW-003: Reviewer output accepted into the normalized store.</summary>
public sealed class HostileReviewerOutput
{
    /// <summary>Reviewer agent.</summary>
    public string ReviewerAgent { get; set; } = "Astra";

    /// <summary>Model.</summary>
    public string Model { get; set; } = "gpt-6-astra";

    /// <summary>Effort.</summary>
    public string Effort { get; set; } = "xhigh";

    /// <summary>Source surface.</summary>
    public string? SourceSurface { get; set; } = "mcp";

    /// <summary>Prompt template id.</summary>
    public string? PromptTemplateId { get; set; } = "hostile-review";

    /// <summary>Prompt version.</summary>
    public string? PromptVersion { get; set; } = "hostile-review/v1";

    /// <summary>Verdict.</summary>
    public string Verdict { get; set; } = "UNKNOWN";

    /// <summary>Findings.</summary>
    public List<HostileReviewFinding> Findings { get; set; } = [];

    /// <summary>Request quality.</summary>
    public HostileReviewRequestQuality RequestQuality { get; set; } = new();

    /// <summary>Input tokens when actually supplied.</summary>
    public int? InputTokens { get; set; }

    /// <summary>Output tokens when actually supplied.</summary>
    public int? OutputTokens { get; set; }
}
