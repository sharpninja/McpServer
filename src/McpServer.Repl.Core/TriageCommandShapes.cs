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

    /// <summary>Get triage dashboard state. Method: workflow.triage.dashboard.</summary>
    public const string GetDashboardMethod = "workflow.triage.dashboard";

    /// <summary>Get a triage group. Method: workflow.triage.getGroup.</summary>
    public const string GetGroupMethod = "workflow.triage.getGroup";

    /// <summary>Query triage research runs. Method: workflow.triage.queryRuns.</summary>
    public const string QueryRunsMethod = "workflow.triage.queryRuns";

    /// <summary>Get a triage research run. Method: workflow.triage.getRun.</summary>
    public const string GetRunMethod = "workflow.triage.getRun";

    /// <summary>Query TODOs created by triage. Method: workflow.triage.queryCreatedTodos.</summary>
    public const string QueryCreatedTodosMethod = "workflow.triage.queryCreatedTodos";

    /// <summary>Flush a triage group. Method: workflow.triage.flushGroup.</summary>
    public const string FlushGroupMethod = "workflow.triage.flushGroup";

    /// <summary>Retry a failed triage group. Method: workflow.triage.retryGroup.</summary>
    public const string RetryGroupMethod = "workflow.triage.retryGroup";

    /// <summary>Soft-delete a triage group and its reports. Method: workflow.triage.deleteGroup.</summary>
    public const string DeleteGroupMethod = "workflow.triage.deleteGroup";

    /// <summary>Create a triage group from selected reports or groups. Method: workflow.triage.createGroup.</summary>
    public const string CreateGroupMethod = "workflow.triage.createGroup";

    /// <summary>Move selected reports or groups into an existing triage group. Method: workflow.triage.consolidateIntoGroup.</summary>
    public const string ConsolidateIntoGroupMethod = "workflow.triage.consolidateIntoGroup";

    /// <summary>Merge source triage groups into an existing triage group. Method: workflow.triage.mergeGroups.</summary>
    public const string MergeGroupsMethod = "workflow.triage.mergeGroups";
}
