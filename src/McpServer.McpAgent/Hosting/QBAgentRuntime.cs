using Microsoft.Agents.AI;

namespace McpServer.McpAgent.Hosting;

/// <summary>
/// FR-MCP-136/TR-MCP-AGENT-015: Bundles an ACID tightly coupled Agent Framework agent with
/// the sealed run options that expose only the approved MCP tool surface.
/// </summary>
public sealed class QBAgentRuntime
{
    internal QBAgentRuntime(
        ChatClientAgent agent,
        ChatClientAgentRunOptions runOptions,
        QBAgentDefinition definition,
        IReadOnlyList<string> toolNames)
    {
        Agent = agent ?? throw new ArgumentNullException(nameof(agent));
        RunOptions = runOptions ?? throw new ArgumentNullException(nameof(runOptions));
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        ToolNames = toolNames ?? throw new ArgumentNullException(nameof(toolNames));
    }

    /// <summary>
    /// Gets the configured Microsoft Agent Framework chat-backed agent.
    /// </summary>
    public ChatClientAgent Agent { get; }

    /// <summary>
    /// Gets the sealed run options that must be used with <see cref="Agent"/> for ACID execution.
    /// </summary>
    public ChatClientAgentRunOptions RunOptions { get; }

    /// <summary>
    /// Gets the public ACID hosted-agent definition represented by this runtime bundle.
    /// </summary>
    public QBAgentDefinition Definition { get; }

    /// <summary>
    /// Gets the model-visible tool names attached to <see cref="RunOptions"/>.
    /// </summary>
    public IReadOnlyList<string> ToolNames { get; }
}
