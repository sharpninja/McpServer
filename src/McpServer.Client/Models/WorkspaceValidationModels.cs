using System.Text.Json.Serialization;

namespace McpServer.Client.Models;

/// <summary>FR-MCP-HYGIENE-001: Validation request.</summary>
public sealed class WorkspaceValidationRequest
{
    /// <summary>Caller is authenticated.</summary>
    [JsonPropertyName("authenticated")]
    public bool Authenticated { get; set; } = true;

    /// <summary>Optional stale-turn threshold hours.</summary>
    [JsonPropertyName("staleTurnThresholdHours")]
    public double? StaleTurnThresholdHours { get; set; }

    /// <summary>Optional rule-code filter.</summary>
    [JsonPropertyName("ruleCodes")]
    public List<string> RuleCodes { get; set; } = [];

    /// <summary>Page offset.</summary>
    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    /// <summary>Page size.</summary>
    [JsonPropertyName("limit")]
    public int Limit { get; set; } = 200;
}

/// <summary>FR-MCP-HYGIENE-001: Finding.</summary>
public sealed class WorkspaceValidationFinding
{
    /// <summary>Rule code.</summary>
    [JsonPropertyName("ruleCode")]
    public string RuleCode { get; set; } = string.Empty;

    /// <summary>Rule version.</summary>
    [JsonPropertyName("ruleVersion")]
    public string RuleVersion { get; set; } = string.Empty;

    /// <summary>Severity.</summary>
    [JsonPropertyName("severity")]
    public string Severity { get; set; } = "Error";

    /// <summary>Entity kind.</summary>
    [JsonPropertyName("entityKind")]
    public string EntityKind { get; set; } = string.Empty;

    /// <summary>Record identity.</summary>
    [JsonPropertyName("recordId")]
    public string RecordId { get; set; } = string.Empty;

    /// <summary>Evidence.</summary>
    [JsonPropertyName("evidence")]
    public string Evidence { get; set; } = string.Empty;

    /// <summary>Remediation.</summary>
    [JsonPropertyName("remediation")]
    public string Remediation { get; set; } = string.Empty;
}

/// <summary>FR-MCP-HYGIENE-001: Validation result.</summary>
public sealed class WorkspaceValidationResult
{
    /// <summary>True when the run completed.</summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>HTTP-equivalent status.</summary>
    [JsonPropertyName("httpStatus")]
    public int HttpStatus { get; set; } = 200;

    /// <summary>Error code.</summary>
    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; set; }

    /// <summary>Error text.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>Workspace identity.</summary>
    [JsonPropertyName("workspaceId")]
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>Registry version.</summary>
    [JsonPropertyName("ruleRegistryVersion")]
    public string RuleRegistryVersion { get; set; } = string.Empty;

    /// <summary>Run timestamp UTC.</summary>
    [JsonPropertyName("runUtc")]
    public DateTimeOffset RunUtc { get; set; }

    /// <summary>Duration.</summary>
    [JsonPropertyName("duration")]
    public TimeSpan Duration { get; set; }

    /// <summary>Effective stale-turn threshold hours.</summary>
    [JsonPropertyName("staleTurnThresholdHours")]
    public double StaleTurnThresholdHours { get; set; }

    /// <summary>True when a threshold override was supplied and accepted.</summary>
    [JsonPropertyName("thresholdOverrideRecorded")]
    public bool ThresholdOverrideRecorded { get; set; }

    /// <summary>Findings page.</summary>
    [JsonPropertyName("findings")]
    public List<WorkspaceValidationFinding> Findings { get; set; } = [];

    /// <summary>Director exit code.</summary>
    [JsonPropertyName("exitCode")]
    public int ExitCode { get; set; }

    /// <summary>Diagnostics.</summary>
    [JsonPropertyName("diagnostics")]
    public List<string> Diagnostics { get; set; } = [];

    /// <summary>Total findings.</summary>
    [JsonPropertyName("totalFindings")]
    public int TotalFindings { get; set; }

    /// <summary>Count of Error+Critical findings.</summary>
    [JsonPropertyName("errorCount")]
    public int ErrorCount { get; set; }

    /// <summary>Count of Warning findings.</summary>
    [JsonPropertyName("warningCount")]
    public int WarningCount { get; set; }
}
