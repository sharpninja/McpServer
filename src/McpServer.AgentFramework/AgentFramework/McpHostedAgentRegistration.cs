using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace McpServer.AgentFramework.AgentFramework;

/// <summary>
/// FR-MCP-066/TR-MCP-AGENT-006/TR-MCP-AGENT-007: ChatClientAgent-ready registration surface that
/// exposes the built-in MCP session-log and TODO workflow tools as Microsoft Agent Framework
/// capabilities.
/// <para>
/// Use <see cref="CreateChatClientAgent"/> to build a <see cref="ChatClientAgent"/> with the hosted
/// agent metadata, then call <see cref="CreateRunOptions"/> to attach the built-in MCP tools through
/// <see cref="ChatClientAgentRunOptions.ChatOptions"/> for a specific run.
/// </para>
/// </summary>
public sealed class McpHostedAgentRegistration
{
    private readonly ChatClientAgentOptions _agentOptions;
    private readonly Func<IChatClient, ChatClientAgent> _chatClientAgentFactory;
    private readonly IReadOnlyList<AIFunction> _functions;
    private readonly Func<ChatClientAgentRunOptions?, ChatClientAgentRunOptions> _runOptionsFactory;
    private readonly IReadOnlyList<AITool> _tools;

    internal McpHostedAgentRegistration(
        ChatClientAgentOptions agentOptions,
        IReadOnlyList<AIFunction> functions,
        Func<IChatClient, ChatClientAgent> chatClientAgentFactory,
        Func<ChatClientAgentRunOptions?, ChatClientAgentRunOptions> runOptionsFactory)
    {
        ArgumentNullException.ThrowIfNull(agentOptions);
        ArgumentNullException.ThrowIfNull(functions);
        _chatClientAgentFactory = chatClientAgentFactory ?? throw new ArgumentNullException(nameof(chatClientAgentFactory));
        _runOptionsFactory = runOptionsFactory ?? throw new ArgumentNullException(nameof(runOptionsFactory));

        _agentOptions = agentOptions.Clone();
        _functions = functions.ToArray();
        _tools = _functions.Cast<AITool>().ToArray();
    }

    /// <summary>
    /// Gets the cloned metadata used when constructing a <see cref="ChatClientAgent"/> for this
    /// registration.
    /// </summary>
    public ChatClientAgentOptions AgentOptions => _agentOptions.Clone();

    /// <summary>
    /// Gets the built-in MCP workflow functions exposed by this registration.
    /// </summary>
    public IReadOnlyList<AIFunction> Functions => _functions;

    /// <summary>
    /// Gets the built-in MCP workflow tools as a general <see cref="AITool"/> list suitable for
    /// <see cref="Microsoft.Extensions.AI.ChatOptions.Tools"/>.
    /// </summary>
    public IReadOnlyList<AITool> Tools => _tools;

    /// <summary>
    /// Creates a <see cref="ChatClientAgent"/> that uses the hosted MCP metadata represented by this
    /// registration.
    /// </summary>
    /// <param name="chatClient">The chat client that should power the agent.</param>
    /// <returns>A configured <see cref="ChatClientAgent"/>.</returns>
    public ChatClientAgent CreateChatClientAgent(IChatClient chatClient)
    {
        ArgumentNullException.ThrowIfNull(chatClient);
        return _chatClientAgentFactory(chatClient);
    }

    /// <summary>
    /// Creates <see cref="ChatClientAgentRunOptions"/> enriched with the built-in MCP workflow tools
    /// and the hosted function-invocation adapter.
    /// </summary>
    /// <param name="baseOptions">
    /// Optional caller-supplied run options to clone before the MCP capabilities are attached.
    /// </param>
    /// <returns>The enriched run options.</returns>
    public ChatClientAgentRunOptions CreateRunOptions(ChatClientAgentRunOptions? baseOptions = null) =>
        _runOptionsFactory(baseOptions);
}
