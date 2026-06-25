namespace McpServer.Repl.Core;

/// <summary>
/// TR-MCP-REPL-TRIAGE-001: YAML command shapes for the deprecated workflow.triage namespace.
/// </summary>
public static class TriageCommandShapes
{
    /// <summary>Namespace prefix for triage workflow commands.</summary>
    public const string MethodNamespace = "workflow.triage";

    /// <summary>Submit a triage report. Method: workflow.triage.report.</summary>
    public const string ReportMethod = "workflow.triage.report";

    /// <summary>Get a triage report. Method: workflow.triage.getReport.</summary>
    public const string GetReportMethod = "workflow.triage.getReport";

    /// <summary>Query triage groups. Method: workflow.triage.queryGroups.</summary>
    public const string QueryGroupsMethod = "workflow.triage.queryGroups";

    /// <summary>Get a triage group. Method: workflow.triage.getGroup.</summary>
    public const string GetGroupMethod = "workflow.triage.getGroup";

    /// <summary>Flush a triage group. Method: workflow.triage.flushGroup.</summary>
    public const string FlushGroupMethod = "workflow.triage.flushGroup";

    /// <summary>Retry a failed triage group. Method: workflow.triage.retryGroup.</summary>
    public const string RetryGroupMethod = "workflow.triage.retryGroup";
}
