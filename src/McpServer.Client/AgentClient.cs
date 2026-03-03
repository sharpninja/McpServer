using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Client.Models;

namespace McpServer.Client;

/// <summary>
/// Client for agent-management endpoints (<c>/mcpserver/agents</c>).
/// </summary>
/// <seealso cref="McpServerClient.Agent"/>
public sealed class AgentClient : McpClientBase
{
    /// <inheritdoc />
    public AgentClient(HttpClient http, McpServerClientOptions options)
        : base(http, options)
    {
    }

    internal AgentClient(HttpClient http, McpServerClientOptions options, WorkspacePathHolder holder)
        : base(http, options, holder)
    {
    }

    /// <summary>
    /// Seeds built-in agent definitions.
    /// </summary>
    public async Task<AgentSeedDefaultsResult> SeedDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        return await PostAsync<AgentSeedDefaultsResult>("mcpserver/agents/definitions/seed", null, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Logs an agent lifecycle event.
    /// </summary>
    /// <param name="agentId">Agent identifier in the route.</param>
    /// <param name="request">Lifecycle event payload.</param>
    /// <param name="workspacePath">Optional workspace path query parameter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<AgentMutationResult> LogEventAsync(
        string agentId,
        AgentEventRequest request,
        string? workspacePath = null,
        CancellationToken cancellationToken = default)
    {
        var path = $"mcpserver/agents/{Uri.EscapeDataString(agentId)}/events";
        if (!string.IsNullOrWhiteSpace(workspacePath))
        {
            path += $"?workspace={Uri.EscapeDataString(workspacePath)}";
        }

        return await PostAsync<AgentMutationResult>(path, request, cancellationToken).ConfigureAwait(false);
    }
}
