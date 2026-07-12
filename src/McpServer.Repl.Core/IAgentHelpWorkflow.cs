using McpServer.Client.Models;

namespace McpServer.Repl.Core;

/// <summary>
/// Defines the canonical Agent Help workflow operations exposed through <c>workflow.agenthelp.*</c>.
/// </summary>
public interface IAgentHelpWorkflow
{
    /// <summary>Creates a new Agent Help session.</summary>
    Task<AgentHelpSessionCreateResponse> CreateSessionAsync(
        AgentHelpSessionCreateRequest? request = null,
        CancellationToken cancellationToken = default);

    /// <summary>Submits one synchronous Agent Help turn.</summary>
    Task<AgentHelpTurnResponse> SubmitTurnAsync(
        string sessionId,
        AgentHelpTurnRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Gets the current status for an Agent Help session.</summary>
    Task<AgentHelpSessionStatusDto> GetStatusAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>Gets transcript entries for an Agent Help session.</summary>
    Task<AgentHelpTranscriptResponse> GetTranscriptAsync(
        string sessionId,
        CancellationToken cancellationToken = default);
}