namespace McpServer.AgentFramework.AgentFramework;

/// <summary>
/// FR-MCP-066/TR-MCP-AGENT-006: Factory for creating fresh hosted-agent instances and
/// ChatClientAgent-ready MCP capability registrations from dependency injection.
/// </summary>
public interface IMcpHostedAgentFactory
{
    /// <summary>
    /// Creates a new hosted-agent instance backed by a fresh set of transient workflow services.
    /// Hosts should use this when each conversation or run needs its own in-memory session-log
    /// continuation state.
    /// </summary>
    /// <returns>A newly created hosted-agent instance.</returns>
    IMcpHostedAgent CreateHostedAgent();

    /// <summary>
    /// Creates a ChatClientAgent-ready registration surface for a fresh hosted-agent instance.
    /// </summary>
    /// <returns>The MCP capability registration associated with a newly created hosted agent.</returns>
    McpHostedAgentRegistration CreateRegistration();
}
