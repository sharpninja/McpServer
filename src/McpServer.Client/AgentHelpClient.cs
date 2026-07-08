using McpServer.Client.Models;

namespace McpServer.Client;

/// <summary>
/// FR-MCP-HELP-007: Typed client for Agent Help conversation endpoints.
/// </summary>
public sealed class AgentHelpClient : McpClientBase
{
    /// <inheritdoc />
    public AgentHelpClient(HttpClient http, McpServerClientOptions options)
        : base(http, options) { }

    internal AgentHelpClient(HttpClient http, McpServerClientOptions options, WorkspacePathHolder holder)
        : base(http, options, holder) { }

    /// <summary>Creates a new Agent Help session.</summary>
    public Task<AgentHelpSessionCreateResponse> CreateSessionAsync(
        AgentHelpSessionCreateRequest? request = null,
        CancellationToken cancellationToken = default)
        => PostAsync<AgentHelpSessionCreateResponse>("mcpserver/agent-help/session", request, cancellationToken);

    /// <summary>Gets the current status for an Agent Help session.</summary>
    public Task<AgentHelpSessionStatusDto> GetStatusAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
        => GetAsync<AgentHelpSessionStatusDto>(
            $"mcpserver/agent-help/session/{Encode(sessionId)}",
            cancellationToken);

    /// <summary>Submits a single help turn for synchronous processing.</summary>
    public Task<AgentHelpTurnResponse> SubmitTurnAsync(
        string sessionId,
        AgentHelpTurnRequest request,
        CancellationToken cancellationToken = default)
        => PostAsync<AgentHelpTurnResponse>(
            $"mcpserver/agent-help/session/{Encode(sessionId)}/turn",
            request,
            cancellationToken);

    /// <summary>Returns transcript entries captured for an Agent Help session.</summary>
    public Task<AgentHelpTranscriptResponse> GetTranscriptAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
        => GetAsync<AgentHelpTranscriptResponse>(
            $"mcpserver/agent-help/session/{Encode(sessionId)}/transcript",
            cancellationToken);

    private static string Encode(string value) => Uri.EscapeDataString(value);
}