namespace McpServer.Repl.Core;

/// <summary>FR-MCP-HOSTILEREVIEW-006: workflow.hostileReview command names. Submit/status/get/query only.</summary>
public static class HostileReviewCommandShapes
{
    /// <summary>Workflow namespace.</summary>
    public const string MethodNamespace = "workflow.hostileReview";

    /// <summary>Submit.</summary>
    public const string SubmitMethod = "workflow.hostileReview.submit";

    /// <summary>Status.</summary>
    public const string StatusMethod = "workflow.hostileReview.status";

    /// <summary>Get.</summary>
    public const string GetMethod = "workflow.hostileReview.get";

    /// <summary>Query.</summary>
    public const string QueryMethod = "workflow.hostileReview.query";
}
