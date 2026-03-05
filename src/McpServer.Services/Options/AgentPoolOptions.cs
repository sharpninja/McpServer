namespace McpServer.Support.Mcp.Options;

/// <summary>
/// TR-MCP-AGENT-004: Agent pool options bound from configuration.
/// </summary>
public sealed class AgentPoolOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "AgentPool";

    /// <summary>
    /// Whether the pool runtime and endpoints are enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Maximum queued one-shot items before enqueue is rejected.
    /// </summary>
    public int MaxQueueSize { get; set; } = 200;

    /// <summary>
    /// Configured pooled agents.
    /// </summary>
    public List<AgentPoolDefinitionOptions> Agents { get; set; } = [];
}
