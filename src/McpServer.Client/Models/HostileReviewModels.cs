using System.Text.Json.Serialization;

namespace McpServer.Client.Models;

/// <summary>FR-MCP-HOSTILEREVIEW-001: Artifact identity on a submit request. Bodies are never sent.</summary>
public sealed class HostileReviewArtifactLink
{
    /// <summary>Artifact type (todo, requirement, file, session, plan).</summary>
    [JsonPropertyName("artifactType")]
    public string ArtifactType { get; set; } = string.Empty;

    /// <summary>Locator or canonical id.</summary>
    [JsonPropertyName("artifactId")]
    public string ArtifactId { get; set; } = string.Empty;
}

/// <summary>FR-MCP-HOSTILEREVIEW-001: Bounded hostile-review submit contract.</summary>
public sealed class HostileReviewSubmitRequest
{
    /// <summary>Target type.</summary>
    [JsonPropertyName("targetType")]
    public string TargetType { get; set; } = "code";

    /// <summary>Review mode.</summary>
    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "adversarial";

    /// <summary>Scope statement.</summary>
    [JsonPropertyName("scopeStatement")]
    public string ScopeStatement { get; set; } = string.Empty;

    /// <summary>Requester identity.</summary>
    [JsonPropertyName("requestingAgent")]
    public string RequestingAgent { get; set; } = string.Empty;

    /// <summary>Explicit workspace path. Foreign workspace is 403.</summary>
    [JsonPropertyName("workspacePath")]
    public string? WorkspacePath { get; set; }

    /// <summary>Artifact identities only.</summary>
    [JsonPropertyName("links")]
    public List<HostileReviewArtifactLink> Links { get; set; } = [];
}

/// <summary>FR-MCP-HOSTILEREVIEW-005: Query filters combine with AND.</summary>
public sealed class HostileReviewQueryRequest
{
    /// <summary>Model filter.</summary>
    [JsonPropertyName("model")]
    public string? Model { get; set; }

    /// <summary>Effort filter.</summary>
    [JsonPropertyName("effort")]
    public string? Effort { get; set; }

    /// <summary>Requesting agent filter.</summary>
    [JsonPropertyName("requestingAgent")]
    public string? RequestingAgent { get; set; }

    /// <summary>Reviewer agent filter.</summary>
    [JsonPropertyName("reviewerAgent")]
    public string? ReviewerAgent { get; set; }

    /// <summary>Target type filter.</summary>
    [JsonPropertyName("targetType")]
    public string? TargetType { get; set; }

    /// <summary>Artifact type filter.</summary>
    [JsonPropertyName("artifactType")]
    public string? ArtifactType { get; set; }

    /// <summary>Finding severity filter.</summary>
    [JsonPropertyName("severity")]
    public string? Severity { get; set; }

    /// <summary>Finding category filter.</summary>
    [JsonPropertyName("category")]
    public string? Category { get; set; }

    /// <summary>Offset.</summary>
    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    /// <summary>Bounded limit.</summary>
    [JsonPropertyName("limit")]
    public int Limit { get; set; } = 50;
}

/// <summary>FR-MCP-HOSTILEREVIEW-004: Request-quality scores.</summary>
public sealed class HostileReviewRequestQuality
{
    /// <summary>Disclosure adequacy 0-1.</summary>
    [JsonPropertyName("disclosure")]
    public double Disclosure { get; set; }

    /// <summary>Scope clarity 0-1.</summary>
    [JsonPropertyName("scopeClarity")]
    public double ScopeClarity { get; set; }

    /// <summary>Objective clarity 0-1.</summary>
    [JsonPropertyName("objectiveClarity")]
    public double ObjectiveClarity { get; set; }

    /// <summary>Reviewer confidence 0-1.</summary>
    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }

    /// <summary>Enough context for a defensible review.</summary>
    [JsonPropertyName("enoughContext")]
    public bool EnoughContext { get; set; }
}

/// <summary>FR-MCP-HOSTILEREVIEW-004: Normalized finding.</summary>
public sealed class HostileReviewFinding
{
    /// <summary>Taxonomy category.</summary>
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    /// <summary>Severity.</summary>
    [JsonPropertyName("severity")]
    public string Severity { get; set; } = "medium";

    /// <summary>Artifact identity.</summary>
    [JsonPropertyName("artifactId")]
    public string? ArtifactId { get; set; }

    /// <summary>Location.</summary>
    [JsonPropertyName("location")]
    public string? Location { get; set; }

    /// <summary>Requirement id.</summary>
    [JsonPropertyName("requirementId")]
    public string? RequirementId { get; set; }

    /// <summary>Sanitized evidence.</summary>
    [JsonPropertyName("evidenceSummary")]
    public string? EvidenceSummary { get; set; }

    /// <summary>Recommendation.</summary>
    [JsonPropertyName("recommendation")]
    public string? Recommendation { get; set; }
}

/// <summary>TR-MCP-HOSTILEREVIEW-003: Execution metadata. Never includes prompt text.</summary>
public sealed class HostileReviewExecution
{
    /// <summary>Execution id.</summary>
    [JsonPropertyName("executionId")]
    public string ExecutionId { get; set; } = string.Empty;

    /// <summary>Reviewer agent.</summary>
    [JsonPropertyName("reviewerAgent")]
    public string ReviewerAgent { get; set; } = string.Empty;

    /// <summary>Model.</summary>
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    /// <summary>Effort.</summary>
    [JsonPropertyName("effort")]
    public string Effort { get; set; } = string.Empty;

    /// <summary>Source surface.</summary>
    [JsonPropertyName("sourceSurface")]
    public string? SourceSurface { get; set; }

    /// <summary>Prompt template id.</summary>
    [JsonPropertyName("promptTemplateId")]
    public string? PromptTemplateId { get; set; }

    /// <summary>Prompt version.</summary>
    [JsonPropertyName("promptVersion")]
    public string? PromptVersion { get; set; }

    /// <summary>Start UTC.</summary>
    [JsonPropertyName("startedUtc")]
    public DateTimeOffset StartedUtc { get; set; }

    /// <summary>End UTC.</summary>
    [JsonPropertyName("endedUtc")]
    public DateTimeOffset? EndedUtc { get; set; }

    /// <summary>Input tokens when supplied.</summary>
    [JsonPropertyName("inputTokens")]
    public int? InputTokens { get; set; }

    /// <summary>Output tokens when supplied.</summary>
    [JsonPropertyName("outputTokens")]
    public int? OutputTokens { get; set; }
}

/// <summary>FR-MCP-HOSTILEREVIEW-001: Submit/status/get/query result. Never contains assembled prompts or raw bodies.</summary>
public sealed class HostileReviewResult
{
    /// <summary>True when the operation succeeded.</summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>HTTP-equivalent status. 403 for foreign workspace.</summary>
    [JsonPropertyName("httpStatus")]
    public int HttpStatus { get; set; } = 200;

    /// <summary>Error text.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>Error code.</summary>
    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; set; }

    /// <summary>Request id.</summary>
    [JsonPropertyName("requestId")]
    public string? RequestId { get; set; }

    /// <summary>Queue status.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>Verdict AGREE/DISAGREE/UNKNOWN.</summary>
    [JsonPropertyName("verdict")]
    public string? Verdict { get; set; }

    /// <summary>Target type.</summary>
    [JsonPropertyName("targetType")]
    public string? TargetType { get; set; }

    /// <summary>Requesting agent.</summary>
    [JsonPropertyName("requestingAgent")]
    public string? RequestingAgent { get; set; }

    /// <summary>Diagnostics.</summary>
    [JsonPropertyName("diagnostics")]
    public List<string> Diagnostics { get; set; } = [];

    /// <summary>Resolved artifact identities.</summary>
    [JsonPropertyName("links")]
    public List<HostileReviewArtifactLink> Links { get; set; } = [];

    /// <summary>Executions.</summary>
    [JsonPropertyName("executions")]
    public List<HostileReviewExecution> Executions { get; set; } = [];

    /// <summary>Findings.</summary>
    [JsonPropertyName("findings")]
    public List<HostileReviewFinding> Findings { get; set; } = [];

    /// <summary>Request quality.</summary>
    [JsonPropertyName("requestQuality")]
    public HostileReviewRequestQuality? RequestQuality { get; set; }
}
