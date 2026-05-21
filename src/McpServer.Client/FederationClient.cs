using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Client.Models;

namespace McpServer.Client;

/// <summary>
/// Client for federation management endpoints (<c>/mcpserver/federation</c>).
/// Provides runtime control of federation state: enable/disable, add/remove targets,
/// configure workspace routing rules, auto-discover targets from tunnels, get connection
/// credentials, and push local data to remote federation targets.
/// FR-MCP-077, FR-MCP-085.
/// </summary>
/// <seealso cref="McpServerClient.Federation"/>
public sealed class FederationClient : McpClientBase
{
    /// <inheritdoc />
    public FederationClient(HttpClient http, McpServerClientOptions options)
        : base(http, options) { }

    internal FederationClient(HttpClient http, McpServerClientOptions options, WorkspacePathHolder holder)
        : base(http, options, holder) { }

    /// <summary>Get the current federation status including all targets and workspace routes.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Full federation status snapshot.</returns>
    public async Task<FederationStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default)
        => await GetAsync<FederationStatusResponse>("mcpserver/federation/status", cancellationToken);

    /// <summary>List local proxies enrolled with the hub.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Proxy inventory.</returns>
    public async Task<List<FederationProxyInfo>> ListProxiesAsync(CancellationToken cancellationToken = default)
        => await GetAsync<List<FederationProxyInfo>>("mcpserver/federation/proxies", cancellationToken);

    /// <summary>List proxy-hosted workspaces known by the hub.</summary>
    /// <param name="proxyId">Optional proxy filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Workspace inventory.</returns>
    public async Task<List<FederationWorkspaceInfo>> ListWorkspacesAsync(string? proxyId = null, CancellationToken cancellationToken = default)
    {
        var path = string.IsNullOrWhiteSpace(proxyId)
            ? "mcpserver/federation/workspaces"
            : $"mcpserver/federation/workspaces?proxyId={Encode(proxyId)}";
        return await GetAsync<List<FederationWorkspaceInfo>>(path, cancellationToken);
    }

    /// <summary>Return queued operation and conflict counts.</summary>
    /// <param name="proxyId">Optional proxy filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Queue status.</returns>
    public async Task<FederationQueueStatusResponse> GetQueueStatusAsync(string? proxyId = null, CancellationToken cancellationToken = default)
    {
        var path = string.IsNullOrWhiteSpace(proxyId)
            ? "mcpserver/federation/queue"
            : $"mcpserver/federation/queue?proxyId={Encode(proxyId)}";
        return await GetAsync<FederationQueueStatusResponse>(path, cancellationToken);
    }

    /// <summary>List federation conflicts.</summary>
    /// <param name="proxyId">Optional proxy filter.</param>
    /// <param name="openOnly">Whether to return only open conflicts.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Conflict inventory.</returns>
    public async Task<List<FederationConflictInfo>> ListConflictsAsync(
        string? proxyId = null,
        bool openOnly = true,
        CancellationToken cancellationToken = default)
    {
        var query = string.IsNullOrWhiteSpace(proxyId)
            ? $"?openOnly={openOnly}"
            : $"?proxyId={Encode(proxyId)}&openOnly={openOnly}";
        return await GetAsync<List<FederationConflictInfo>>($"mcpserver/federation/conflicts{query}", cancellationToken);
    }

    /// <summary>Return mutable state adapter coverage diagnostics.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Adapter coverage rows.</returns>
    public async Task<List<FederationStateAdapterCoverage>> GetAdapterCoverageAsync(CancellationToken cancellationToken = default)
        => await GetAsync<List<FederationStateAdapterCoverage>>("mcpserver/federation/adapters", cancellationToken);

    /// <summary>Enable federation proxying globally.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated federation status.</returns>
    public async Task<FederationStatusResponse> EnableAsync(CancellationToken cancellationToken = default)
        => await PostAsync<FederationStatusResponse>("mcpserver/federation/enable", null, cancellationToken);

    /// <summary>Disable federation proxying globally.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated federation status.</returns>
    public async Task<FederationStatusResponse> DisableAsync(CancellationToken cancellationToken = default)
        => await PostAsync<FederationStatusResponse>("mcpserver/federation/disable", null, cancellationToken);

    /// <summary>List all registered federation targets.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Array of federation target info objects.</returns>
    public async Task<List<FederationTargetInfo>> ListTargetsAsync(CancellationToken cancellationToken = default)
        => await GetAsync<List<FederationTargetInfo>>("mcpserver/federation/targets", cancellationToken);

    /// <summary>Add a new named federation target.</summary>
    /// <param name="request">Target configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The newly created target info.</returns>
    /// <exception cref="McpConflictException">A target with the same name already exists.</exception>
    public async Task<FederationTargetInfo> AddTargetAsync(FederationTargetAddRequest request, CancellationToken cancellationToken = default)
        => await PostAsync<FederationTargetInfo>("mcpserver/federation/targets", request, cancellationToken);

    /// <summary>Remove a federation target by name.</summary>
    /// <param name="name">Target name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The HTTP status code (204 on success).</returns>
    /// <exception cref="McpNotFoundException">No target with the given name exists.</exception>
    public async Task<HttpStatusCode> RemoveTargetAsync(string name, CancellationToken cancellationToken = default)
        => await SendForStatusAsync(HttpMethod.Delete, $"mcpserver/federation/targets/{Encode(name)}", null, cancellationToken);

    /// <summary>Set a target as the global default for requests with no workspace-specific route.</summary>
    /// <param name="name">Target name to set as default.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated federation status.</returns>
    /// <exception cref="McpNotFoundException">No target with the given name exists.</exception>
    public async Task<FederationStatusResponse> SetDefaultTargetAsync(string name, CancellationToken cancellationToken = default)
        => await PostAsync<FederationStatusResponse>($"mcpserver/federation/targets/{Encode(name)}/set-default", null, cancellationToken);

    /// <summary>Clear the global default target.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated federation status.</returns>
    public async Task<FederationStatusResponse> ClearDefaultTargetAsync(CancellationToken cancellationToken = default)
        => await DeleteAsync<FederationStatusResponse>("mcpserver/federation/targets/default", cancellationToken);

    /// <summary>Add or update a workspace-specific routing rule.</summary>
    /// <param name="request">Workspace path and target name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated route list.</returns>
    /// <exception cref="McpNotFoundException">The specified target does not exist.</exception>
    public async Task<List<WorkspaceRouteInfo>> AddRouteAsync(WorkspaceRouteRequest request, CancellationToken cancellationToken = default)
        => await PostAsync<List<WorkspaceRouteInfo>>("mcpserver/federation/routes", request, cancellationToken);

    /// <summary>Remove a workspace-specific routing rule.</summary>
    /// <param name="request">Route specifying the workspace path to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The HTTP status code (204 on success).</returns>
    /// <exception cref="McpNotFoundException">No route for the specified workspace path exists.</exception>
    public async Task<HttpStatusCode> RemoveRouteAsync(WorkspaceRouteRequest request, CancellationToken cancellationToken = default)
        => await SendForStatusAsync(HttpMethod.Delete, "mcpserver/federation/routes", request, cancellationToken);

    /// <summary>Get connection credentials so a federated peer can connect to this server.</summary>
    /// <param name="workspaceName">Display name of the workspace to look up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Connection info including base URL, port, and API key.</returns>
    /// <exception cref="McpNotFoundException">No enabled workspace with the given name exists.</exception>
    public async Task<FederationConnectionInfo> GetConnectionAsync(string workspaceName, CancellationToken cancellationToken = default)
        => await GetAsync<FederationConnectionInfo>($"mcpserver/federation/connection?workspaceName={Encode(workspaceName)}", cancellationToken);

    /// <summary>Auto-discover federation targets from running tunnel providers.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Discovery result with count and details of newly registered targets.</returns>
    public async Task<TunnelDiscoveryResult> DiscoverFromTunnelsAsync(CancellationToken cancellationToken = default)
        => await PostAsync<TunnelDiscoveryResult>("mcpserver/federation/targets/discover-from-tunnels", null, cancellationToken);

    /// <summary>Push local data (TODOs, session logs) to the resolved federation target.</summary>
    /// <param name="types">
    /// Optional filter for which data types to push. Valid values: <c>"todos"</c>, <c>"sessionlogs"</c>.
    /// Pass <see langword="null"/> or an empty list to push all types.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Push result with success/failure counts.</returns>
    /// <exception cref="McpConflictException">Federation is disabled.</exception>
    /// <exception cref="McpNotFoundException">No federation target resolved.</exception>
    public async Task<FederationPushResult> PushAsync(IReadOnlyList<string>? types = null, CancellationToken cancellationToken = default)
        => await PostAsync<FederationPushResult>("mcpserver/federation/push", new FederationPushRequest { Types = types }, cancellationToken);

    private static string Encode(string value) => Uri.EscapeDataString(value);
}
