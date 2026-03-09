namespace McpServer.AgentFramework;

/// <summary>
/// FR-MCP-066/TR-MCP-AGENT-007: Shared defaults for the scaffolded hosted agent surface.
/// </summary>
public static class McpHostedAgentDefaults
{
    /// <summary>
    /// Default stable identifier assigned to the scaffolded hosted agent metadata.
    /// </summary>
    public const string DefaultAgentId = "mcpserver-hosted-agent";

    /// <summary>
    /// Default host-facing name assigned to the scaffolded hosted agent.
    /// </summary>
    public const string DefaultAgentName = "McpServerHostedAgent";

    /// <summary>
    /// Default source type reserved for hosted-agent session-log workflows.
    /// </summary>
    public const string DefaultSourceType = "AgentFramework";

    /// <summary>
    /// Default description surfaced through <c>Microsoft.Agents.AI</c> metadata.
    /// </summary>
    public const string DefaultAgentDescription = "Hosted MCP Server agent registration scaffold.";

    /// <summary>
    /// Canonical UTC timestamp format shared by hosted-agent session and request identifiers.
    /// </summary>
    public const string IdentifierTimestampFormat = "yyyyMMddTHHmmssZ";

    /// <summary>
    /// Canonical prefix reserved for hosted-agent request identifiers.
    /// </summary>
    public const string RequestIdPrefix = "req";

    /// <summary>
    /// Canonical regex for hosted-agent session identifiers.
    /// </summary>
    public const string SessionIdPattern = "^[A-Z][A-Za-z0-9]*-\\d{8}T\\d{6}Z-[a-z0-9]+(?:-[a-z0-9]+)*$";

    /// <summary>
    /// Canonical regex for hosted-agent request identifiers.
    /// </summary>
    public const string RequestIdPattern = "^req-\\d{8}T\\d{6}Z-[a-z0-9]+(?:-[a-z0-9]+)*$";

    /// <summary>
    /// Named <see cref="HttpClient"/> identifier used by the scaffolded transport registration.
    /// </summary>
    public const string HttpClientName = "McpServer.AgentFramework";
}
