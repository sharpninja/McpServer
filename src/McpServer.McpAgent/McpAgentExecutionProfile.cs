namespace McpServer.McpAgent;

/// <summary>
/// FR-MCP-136/TR-MCP-AGENT-015: Selects the hosted-agent execution profile applied to
/// Microsoft Agent Framework run options.
/// </summary>
public enum McpAgentExecutionProfile
{
    /// <summary>
    /// Uses the backward-compatible hosted-agent tool surface and option behavior.
    /// </summary>
    Default = 0,

    /// <summary>
    /// Uses the fail-closed ACID profile with authenticated workspace binding, serialized
    /// function invocation, durable audit expectations, and a restricted model-visible tool surface.
    /// </summary>
    AcidTightlyCoupled = 1,
}
