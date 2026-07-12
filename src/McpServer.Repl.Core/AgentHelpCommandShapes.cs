namespace McpServer.Repl.Core;

/// <summary>
/// TR-MCP-HELP-009: YAML command shapes for the <c>workflow.agenthelp.*</c> namespace.
/// </summary>
public static class AgentHelpCommandShapes
{
    /// <summary>Namespace prefix for Agent Help workflow commands.</summary>
    public const string MethodNamespace = "workflow.agenthelp";

    /// <summary>Method: <c>workflow.agenthelp.createSession</c>.</summary>
    public const string CreateSessionMethod = "workflow.agenthelp.createSession";

    /// <summary>Method: <c>workflow.agenthelp.submitTurn</c>.</summary>
    public const string SubmitTurnMethod = "workflow.agenthelp.submitTurn";

    /// <summary>Method: <c>workflow.agenthelp.getStatus</c>.</summary>
    public const string GetStatusMethod = "workflow.agenthelp.getStatus";

    /// <summary>Method: <c>workflow.agenthelp.getTranscript</c>.</summary>
    public const string GetTranscriptMethod = "workflow.agenthelp.getTranscript";
}