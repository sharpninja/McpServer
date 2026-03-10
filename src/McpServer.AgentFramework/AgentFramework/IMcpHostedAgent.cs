using McpServer.AgentFramework.SessionLog;
using McpServer.AgentFramework.Todo;
using McpServer.AgentFramework.PowerShellSessions;
using McpServer.Client;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace McpServer.AgentFramework.AgentFramework;

/// <summary>
/// FR-MCP-066/TR-MCP-AGENT-007: Host-resolvable MCP-aware agent contract exposing the built-in
/// session-log, TODO, repository, desktop-launch, and local PowerShell-session integrations.
/// </summary>
public interface IMcpHostedAgent
{
    /// <summary>
    /// Gets the ChatClientAgent-ready registration surface that exposes the built-in MCP workflow
    /// tools through <c>Microsoft.Extensions.AI</c> abstractions.
    /// </summary>
    McpHostedAgentRegistration Registration { get; }

    /// <summary>
    /// Gets the host-facing name assigned to the scaffolded hosted agent.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the canonical source type reserved for hosted-agent session-log workflow integration.
    /// </summary>
    string SourceType { get; }

    /// <summary>
    /// Gets the canonical identifier helper bound to the hosted agent's configured source type.
    /// </summary>
    IMcpSessionIdentifierFactory Identifiers { get; }

    /// <summary>
    /// Gets the resolved <c>Microsoft.Agents.AI</c> metadata that a host can use when constructing a chat-backed agent.
    /// </summary>
    ChatClientAgentOptions AgentOptions { get; }

    /// <summary>
    /// Gets the MCP Server transport client backing the scaffold registration.
    /// </summary>
    McpServerClient Client { get; }

    /// <summary>
    /// Gets the session-log workflow service bound to this agent's transport client and identifier factory.
    /// Use this to bootstrap sessions, create turns, and update turn metadata without managing the
    /// <see cref="ISessionLogWorkflow"/> instance separately.
    /// </summary>
    ISessionLogWorkflow SessionLog { get; }

    /// <summary>
    /// Gets the TODO workflow service bound to this agent's transport client.
    /// Use this to query, update, analyze, and stream TODO workflows without managing the
    /// <see cref="ITodoWorkflow"/> instance separately.
    /// </summary>
    ITodoWorkflow Todo { get; }

    /// <summary>
    /// Gets the host-facing local PowerShell session manager bound to this hosted agent instance.
    /// Use this when the host application needs to execute direct local PowerShell commands without
    /// going through the model-facing tool surface.
    /// </summary>
    IHostedPowerShellSessionManager PowerShellSessions { get; }

    /// <summary>
    /// Creates a <see cref="ChatClientAgent"/> that uses this hosted agent's MCP-aware metadata.
    /// Pair the returned agent with <see cref="CreateRunOptions"/> when running prompts that should
    /// be able to invoke the built-in MCP workflow tools.
    /// </summary>
    /// <param name="chatClient">The chat client that should power the hosted agent.</param>
    /// <returns>A <see cref="ChatClientAgent"/> configured with this hosted agent's metadata.</returns>
    ChatClientAgent CreateChatClientAgent(IChatClient chatClient);

    /// <summary>
     /// Creates <see cref="ChatClientAgentRunOptions"/> that attach the built-in MCP workflow tools
     /// through <see cref="Microsoft.Extensions.AI.ChatOptions.Tools"/> and wrap the supplied chat
     /// client with function invocation support, including the local in-process PowerShell session
     /// tools exposed by the hosted-agent adapter.
     /// </summary>
    /// <param name="baseOptions">
    /// Optional caller-supplied run options to clone and enrich with the hosted MCP capabilities.
    /// </param>
    /// <returns>The run options enriched with the hosted MCP workflow tools.</returns>
    ChatClientAgentRunOptions CreateRunOptions(ChatClientAgentRunOptions? baseOptions = null);
}
