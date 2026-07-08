namespace McpServer.Support.Mcp.Services.AgentHelp;

/// <summary>
/// FR-MCP-HELP-001: Agent Help conversation orchestration contract.
/// TR-MCP-HELP-007: Session registry, inbound guard evaluation, and helper turn execution.
/// </summary>
public interface IAgentHelpConversationService
{
    /// <summary>
    /// Creates a new Agent Help session.
    /// </summary>
    Task<AgentHelpSessionCreateResponse> CreateSessionAsync(
        AgentHelpSessionCreateRequest? request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes a help turn for an existing session.
    /// </summary>
    Task<AgentHelpTurnResponse?> SubmitTurnAsync(
        string sessionId,
        AgentHelpTurnRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams help turn output for an existing session.
    /// </summary>
    IAsyncEnumerable<AgentHelpStreamEvent> SubmitTurnStreamingAsync(
        string sessionId,
        AgentHelpTurnRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets session status details.
    /// </summary>
    Task<AgentHelpSessionStatusDto?> GetStatusAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets transcript entries for the session.
    /// </summary>
    Task<AgentHelpTranscriptResponse?> GetTranscriptAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a help session and releases any in-memory state.
    /// </summary>
    Task<bool> DeleteSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default);
}