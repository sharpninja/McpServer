namespace McpServer.Support.Mcp.Options;

/// <summary>
/// TR-MCP-AGENT-004: Configuration for a single pooled agent entry.
/// </summary>
public sealed class AgentPoolDefinitionOptions
{
    /// <summary>
    /// Unique agent name used for routing and endpoint addressing.
    /// </summary>
    public string AgentName { get; set; } = string.Empty;

    /// <summary>
    /// Agent executable path used by Copilot CLI process launch.
    /// </summary>
    public string AgentPath { get; set; } = string.Empty;

    /// <summary>
    /// Model identifier used by this pooled agent.
    /// </summary>
    public string AgentModel { get; set; } = "gpt-5.3-codex";

    /// <summary>
    /// Optional seed prompt injected into the first turn for this agent session.
    /// </summary>
    public string? AgentSeed { get; set; }

    /// <summary>
    /// Optional key-value parameters forwarded as environment variables to the agent process.
    /// </summary>
    public Dictionary<string, string> AgentParameters { get; set; } = [];

    /// <summary>
    /// Indicates this agent is the fallback default for interactive requests.
    /// </summary>
    public bool IsInteractiveDefault { get; set; }

    /// <summary>
    /// Indicates this agent is the fallback default for one-shot <c>Plan</c> context.
    /// </summary>
    public bool IsTodoPlanDefault { get; set; }

    /// <summary>
    /// Indicates this agent is the fallback default for one-shot <c>Status</c> context.
    /// </summary>
    public bool IsTodoStatusDefault { get; set; }

    /// <summary>
    /// Indicates this agent is the fallback default for one-shot <c>Implement</c> context.
    /// </summary>
    public bool IsTodoImplementDefault { get; set; }
}
