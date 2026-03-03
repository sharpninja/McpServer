using System;
using System.Collections.Generic;
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
    /// Lists all global agent definitions.
    /// </summary>
    public async Task<AgentDefinitionListResult> ListDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<AgentDefinitionListResult>("mcpserver/agents/definitions", cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Gets a specific global agent definition.
    /// </summary>
    /// <param name="agentType">Agent definition identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<AgentDefinition> GetDefinitionAsync(string agentType, CancellationToken cancellationToken = default)
    {
        return await GetAsync<AgentDefinition>($"mcpserver/agents/definitions/{Encode(agentType)}", cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Creates or updates a global agent definition.
    /// </summary>
    public async Task<AgentMutationResult> UpsertDefinitionAsync(
        AgentDefinitionRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PostAsync<AgentMutationResult>("mcpserver/agents/definitions", request, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes a global agent definition.
    /// </summary>
    /// <param name="agentType">Agent definition identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<AgentMutationResult> DeleteDefinitionAsync(string agentType, CancellationToken cancellationToken = default)
    {
        return await DeleteAsync<AgentMutationResult>($"mcpserver/agents/definitions/{Encode(agentType)}", cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Seeds built-in agent definitions.
    /// </summary>
    public async Task<AgentSeedDefaultsResult> SeedDefaultsAsync(CancellationToken cancellationToken = default)
    {
        return await PostAsync<AgentSeedDefaultsResult>("mcpserver/agents/definitions/seed", null, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Seeds built-in agent definitions.
    /// Maintained as a compatibility alias for older callers.
    /// </summary>
    public async Task<AgentSeedDefaultsResult> SeedDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        return await SeedDefaultsAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Lists all workspace agent configurations.
    /// </summary>
    /// <param name="workspacePath">Optional workspace query parameter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<AgentWorkspaceListResult> ListWorkspaceAgentsAsync(
        string? workspacePath = null,
        CancellationToken cancellationToken = default)
    {
        return await GetAsync<AgentWorkspaceListResult>($"mcpserver/agents{BuildWorkspaceQuery(workspacePath)}", cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Gets a workspace agent configuration.
    /// </summary>
    /// <param name="agentId">Agent identifier.</param>
    /// <param name="workspacePath">Optional workspace query parameter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<AgentWorkspaceConfig> GetWorkspaceAgentAsync(
        string agentId,
        string? workspacePath = null,
        CancellationToken cancellationToken = default)
    {
        return await GetAsync<AgentWorkspaceConfig>(
                $"mcpserver/agents/{Encode(agentId)}{BuildWorkspaceQuery(workspacePath)}",
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Creates or updates a workspace agent configuration.
    /// </summary>
    /// <param name="agentId">Agent identifier.</param>
    /// <param name="request">Workspace configuration payload.</param>
    /// <param name="workspacePath">Optional workspace query parameter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<AgentMutationResult> UpsertWorkspaceAgentAsync(
        string agentId,
        AgentWorkspaceRequest request,
        string? workspacePath = null,
        CancellationToken cancellationToken = default)
    {
        return await PostAsync<AgentMutationResult>(
                $"mcpserver/agents/{Encode(agentId)}{BuildWorkspaceQuery(workspacePath)}",
                request,
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes a workspace agent configuration.
    /// </summary>
    /// <param name="agentId">Agent identifier.</param>
    /// <param name="workspacePath">Optional workspace query parameter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<AgentMutationResult> DeleteWorkspaceAgentAsync(
        string agentId,
        string? workspacePath = null,
        CancellationToken cancellationToken = default)
    {
        return await DeleteAsync<AgentMutationResult>(
                $"mcpserver/agents/{Encode(agentId)}{BuildWorkspaceQuery(workspacePath)}",
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Bans an agent within a workspace or globally.
    /// </summary>
    /// <param name="agentId">Agent identifier.</param>
    /// <param name="request">Ban request payload.</param>
    /// <param name="workspacePath">Optional workspace query parameter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<AgentMutationResult> BanAgentAsync(
        string agentId,
        AgentBanRequest request,
        string? workspacePath = null,
        CancellationToken cancellationToken = default)
    {
        return await PostAsync<AgentMutationResult>(
                $"mcpserver/agents/{Encode(agentId)}/ban{BuildWorkspaceQuery(workspacePath)}",
                request,
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Unbans an agent within a workspace or globally.
    /// </summary>
    /// <param name="agentId">Agent identifier.</param>
    /// <param name="workspacePath">Optional workspace query parameter.</param>
    /// <param name="global">When true, unban globally.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<AgentMutationResult> UnbanAgentAsync(
        string agentId,
        string? workspacePath = null,
        bool global = false,
        CancellationToken cancellationToken = default)
    {
        return await PostAsync<AgentMutationResult>(
                $"mcpserver/agents/{Encode(agentId)}/unban{BuildWorkspaceAndGlobalQuery(workspacePath, global)}",
                null,
                cancellationToken)
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
        var path = $"mcpserver/agents/{Encode(agentId)}/events{BuildWorkspaceQuery(workspacePath)}";
        return await PostAsync<AgentMutationResult>(path, request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets agent event history.
    /// </summary>
    /// <param name="agentId">Agent identifier.</param>
    /// <param name="workspacePath">Optional workspace query parameter.</param>
    /// <param name="limit">Maximum events to return. Defaults to 50.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<AgentEventListResult> GetEventsAsync(
        string agentId,
        string? workspacePath = null,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        return await GetAsync<AgentEventListResult>(
                $"mcpserver/agents/{Encode(agentId)}/events{BuildWorkspaceAndLimitQuery(workspacePath, limit)}",
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Validates an <c>agents.yaml</c> file for a workspace.
    /// </summary>
    /// <param name="workspacePath">Optional workspace query parameter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<AgentValidateResult> ValidateAsync(
        string? workspacePath = null,
        CancellationToken cancellationToken = default)
    {
        return await GetAsync<AgentValidateResult>($"mcpserver/agents/validate{BuildWorkspaceQuery(workspacePath)}", cancellationToken)
            .ConfigureAwait(false);
    }

    private static string Encode(string value) => Uri.EscapeDataString(value);

    private static string BuildWorkspaceQuery(string? workspacePath)
    {
        if (string.IsNullOrWhiteSpace(workspacePath))
            return string.Empty;
        return $"?workspace={Encode(workspacePath!)}";
    }

    private static string BuildWorkspaceAndGlobalQuery(string? workspacePath, bool global)
    {
        var parts = new List<string>(2);
        if (!string.IsNullOrWhiteSpace(workspacePath))
            parts.Add($"workspace={Encode(workspacePath!)}");
        parts.Add($"global={(global ? "true" : "false")}");
        return parts.Count == 0 ? string.Empty : "?" + string.Join("&", parts);
    }

    private static string BuildWorkspaceAndLimitQuery(string? workspacePath, int limit)
    {
        var parts = new List<string>(2);
        if (!string.IsNullOrWhiteSpace(workspacePath))
            parts.Add($"workspace={Encode(workspacePath!)}");
        if (limit > 0)
            parts.Add($"limit={limit}");
        return parts.Count == 0 ? string.Empty : "?" + string.Join("&", parts);
    }
}
