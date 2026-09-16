namespace McpServer.Support.Mcp.Models;

/// <summary>FR-MCP-HYGIENE-001: Versioned hygiene rule registry.</summary>
public static class WorkspaceValidationRuleRegistry
{
    /// <summary>Registry version.</summary>
    public const string Version = "hygiene/v1";

    /// <summary>Known rule codes.</summary>
    public static readonly HashSet<string> KnownCodes = new(StringComparer.Ordinal)
    {
        "missing_acceptance_criteria",
        "tr_orphan_no_fr",
        "fr_missing_tr_or_test",
        "test_orphan_no_fr",
        "broken_or_duplicate_mapping",
        "todo_done_incomplete_tasks",
        "todo_open_all_tasks_complete",
        "todo_done_missing_summary",
        "todo_remaining_contradicts_completion",
        "todo_missing_dependency",
        "todo_missing_requirement",
        "session_stale_in_progress",
        "triage_nonterminal",
        "triage_processing_failed",
    };
}

/// <summary>FR-MCP-HYGIENE-001: Validation request.</summary>
public sealed class WorkspaceValidationRequest
{
    /// <summary>Caller is authenticated.</summary>
    public bool Authenticated { get; set; } = true;

    /// <summary>Optional stale-turn threshold in hours. Default 48. Max 168.</summary>
    public double? StaleTurnThresholdHours { get; set; }

    /// <summary>Optional rule-code filter. Unknown codes become diagnostics.</summary>
    public List<string> RuleCodes { get; set; } = [];

    /// <summary>Page offset.</summary>
    public int Offset { get; set; }

    /// <summary>Page size. Default 200.</summary>
    public int Limit { get; set; } = 200;
}

/// <summary>FR-MCP-HYGIENE-001: One finding.</summary>
public sealed class WorkspaceValidationFinding
{
    /// <summary>Rule code.</summary>
    public string RuleCode { get; set; } = string.Empty;

    /// <summary>Rule version.</summary>
    public string RuleVersion { get; set; } = WorkspaceValidationRuleRegistry.Version;

    /// <summary>Severity: Warning, Error, Critical.</summary>
    public string Severity { get; set; } = "Error";

    /// <summary>Entity kind.</summary>
    public string EntityKind { get; set; } = string.Empty;

    /// <summary>Record identity.</summary>
    public string RecordId { get; set; } = string.Empty;

    /// <summary>Evidence.</summary>
    public string Evidence { get; set; } = string.Empty;

    /// <summary>Remediation guidance.</summary>
    public string Remediation { get; set; } = string.Empty;
}

/// <summary>FR-MCP-HYGIENE-001: Validation result contract.</summary>
public sealed class WorkspaceValidationResult
{
    /// <summary>True when the run completed.</summary>
    public bool Success { get; set; }

    /// <summary>HTTP-equivalent status.</summary>
    public int HttpStatus { get; set; } = 200;

    /// <summary>Error code.</summary>
    public string? ErrorCode { get; set; }

    /// <summary>Error text.</summary>
    public string? Error { get; set; }

    /// <summary>Workspace identity.</summary>
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>Registry version.</summary>
    public string RuleRegistryVersion { get; set; } = WorkspaceValidationRuleRegistry.Version;

    /// <summary>Run timestamp UTC.</summary>
    public DateTimeOffset RunUtc { get; set; }

    /// <summary>Duration.</summary>
    public TimeSpan Duration { get; set; }

    /// <summary>Effective stale-turn threshold hours.</summary>
    public double StaleTurnThresholdHours { get; set; }

    /// <summary>True when a threshold override was supplied and accepted.</summary>
    public bool ThresholdOverrideRecorded { get; set; }

    /// <summary>Director-oriented exit code. 1 when any Error/Critical finding exists.</summary>
    public int ExitCode { get; set; }

    /// <summary>Diagnostics for unknown rules and bound failures.</summary>
    public List<string> Diagnostics { get; set; } = [];

    /// <summary>Findings page.</summary>
    public List<WorkspaceValidationFinding> Findings { get; set; } = [];

    /// <summary>Total findings before paging.</summary>
    public int TotalFindings { get; set; }

    /// <summary>Count of Error+Critical findings.</summary>
    public int ErrorCount { get; set; }

    /// <summary>Count of Warning findings.</summary>
    public int WarningCount { get; set; }
}
