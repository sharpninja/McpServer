namespace McpServer.Repl.Core;

/// <summary>TR-HANDOFF-SURFACE-001: workflow.handoff command names.</summary>
public static class HandoffCommandShapes
{
    /// <summary>Namespace prefix.</summary>
    public const string MethodNamespace = "workflow.handoff";

    /// <summary>Ingest a handoff document.</summary>
    public const string IngestMethod = "workflow.handoff.ingest";

    /// <summary>Get a persisted run.</summary>
    public const string GetMethod = "workflow.handoff.get";

    /// <summary>Approve or reject a persisted run.</summary>
    public const string ApproveMethod = "workflow.handoff.approve";
}
