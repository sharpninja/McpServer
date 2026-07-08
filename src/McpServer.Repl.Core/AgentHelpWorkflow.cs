using McpServer.Client;
using McpServer.Client.Models;

namespace McpServer.Repl.Core;

/// <summary>
/// TR-MCP-HELP-009: Production <c>workflow.agenthelp.*</c> wrapper that delegates to <see cref="AgentHelpClient"/>.
/// </summary>
public sealed class AgentHelpWorkflow : IAgentHelpWorkflow
{
    private readonly AgentHelpClient _client;

    /// <summary>Initializes a new instance of the <see cref="AgentHelpWorkflow"/> class.</summary>
    /// <param name="client">The typed Agent Help client used for transport.</param>
    public AgentHelpWorkflow(AgentHelpClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    /// <inheritdoc />
    public Task<AgentHelpSessionCreateResponse> CreateSessionAsync(
        AgentHelpSessionCreateRequest? request = null,
        CancellationToken cancellationToken = default)
        => _client.CreateSessionAsync(request, cancellationToken);

    /// <inheritdoc />
    public Task<AgentHelpTurnResponse> SubmitTurnAsync(
        string sessionId,
        AgentHelpTurnRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session id is required.", nameof(sessionId));

        ArgumentNullException.ThrowIfNull(request);
        return _client.SubmitTurnAsync(sessionId, request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<AgentHelpSessionStatusDto> GetStatusAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session id is required.", nameof(sessionId));

        return _client.GetStatusAsync(sessionId, cancellationToken);
    }
}