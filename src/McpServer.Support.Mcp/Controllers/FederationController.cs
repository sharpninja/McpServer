using System.Globalization;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Mvc;

namespace McpServer.Support.Mcp.Controllers;

/// <summary>
/// FR-MCP-077: Management API for server federation. Provides runtime control of federation
/// state: enable/disable, add/remove targets, configure workspace routing rules, and
/// auto-discover targets from running tunnel providers.
/// </summary>
[ApiController]
[Route("mcpserver/federation")]
public sealed class FederationController : ControllerBase
{
    private readonly FederationRegistry _registry;
    private readonly TunnelRegistry _tunnelRegistry;

    /// <summary>Initializes a new instance of the <see cref="FederationController"/> class.</summary>
    /// <param name="registry">Federation target registry.</param>
    /// <param name="tunnelRegistry">Tunnel registry for auto-discovery.</param>
    public FederationController(FederationRegistry registry, TunnelRegistry tunnelRegistry)
    {
        _registry = registry;
        _tunnelRegistry = tunnelRegistry;
    }

    /// <summary>Get the current federation status including all targets and workspace routes.</summary>
    /// <returns>Full federation status snapshot.</returns>
    [HttpGet("status")]
    public ActionResult<FederationStatusResponse> GetStatus()
        => Ok(BuildStatus());

    /// <summary>Enable federation proxying globally.</summary>
    /// <returns>Updated federation status.</returns>
    [HttpPost("enable")]
    public ActionResult<FederationStatusResponse> Enable()
    {
        _registry.SetEnabled(true);
        return Ok(BuildStatus());
    }

    /// <summary>Disable federation proxying globally. In-flight proxied requests complete normally.</summary>
    /// <returns>Updated federation status.</returns>
    [HttpPost("disable")]
    public ActionResult<FederationStatusResponse> Disable()
    {
        _registry.SetEnabled(false);
        return Ok(BuildStatus());
    }

    /// <summary>List all registered federation targets.</summary>
    /// <returns>Array of federation target info objects.</returns>
    [HttpGet("targets")]
    public ActionResult<IReadOnlyList<FederationTargetInfo>> ListTargets()
        => Ok(_registry.List());

    /// <summary>Add a new named federation target.</summary>
    /// <param name="options">Target configuration.</param>
    /// <returns>201 Created with the new target info, or 409 Conflict if the name is already taken.</returns>
    [HttpPost("targets")]
    public ActionResult<FederationTargetInfo> AddTarget([FromBody] FederationTargetOptions options)
    {
        if (!_registry.TryAddTarget(options, out var error))
            return Conflict(new { error });

        var added = _registry.List().First(t => string.Equals(t.Name, options.Name.Trim(), StringComparison.OrdinalIgnoreCase));
        return CreatedAtAction(nameof(ListTargets), null, added);
    }

    /// <summary>Remove a federation target by name.</summary>
    /// <param name="name">Target name.</param>
    /// <returns>204 No Content on success, 404 if not found.</returns>
    [HttpDelete("targets/{name}")]
    public IActionResult RemoveTarget(string name)
    {
        if (!_registry.TryRemoveTarget(name))
            return NotFound(new { error = $"Federation target '{name}' not found." });

        return NoContent();
    }

    /// <summary>Set a target as the global default for requests with no workspace-specific route.</summary>
    /// <param name="name">Target name to set as default.</param>
    /// <returns>Updated federation status, or 404 if the target is not found.</returns>
    [HttpPost("targets/{name}/set-default")]
    public ActionResult<FederationStatusResponse> SetDefault(string name)
    {
        if (!_registry.SetDefaultTarget(name))
            return NotFound(new { error = $"Federation target '{name}' not found." });

        return Ok(BuildStatus());
    }

    /// <summary>Clear the global default target (requests will only route via workspace-specific rules).</summary>
    /// <returns>Updated federation status.</returns>
    [HttpDelete("targets/default")]
    public ActionResult<FederationStatusResponse> ClearDefault()
    {
        _registry.SetDefaultTarget(null);
        return Ok(BuildStatus());
    }

    /// <summary>Add or update a workspace-specific routing rule.</summary>
    /// <param name="route">Workspace path and target name.</param>
    /// <returns>Updated route list, or 404 if the target does not exist.</returns>
    [HttpPost("routes")]
    public ActionResult<IReadOnlyList<WorkspaceRouteInfo>> AddRoute([FromBody] WorkspaceRouteOptions route)
    {
        if (!_registry.SetWorkspaceRoute(route.WorkspacePath, route.TargetName))
            return NotFound(new { error = $"Federation target '{route.TargetName}' not found." });

        return Ok(_registry.ListRoutes());
    }

    /// <summary>Remove a workspace-specific routing rule.</summary>
    /// <param name="route">Route specifying the workspace path to remove.</param>
    /// <returns>204 No Content on success, 404 if the route did not exist.</returns>
    [HttpDelete("routes")]
    public IActionResult RemoveRoute([FromBody] WorkspaceRouteOptions route)
    {
        if (!_registry.RemoveWorkspaceRoute(route.WorkspacePath))
            return NotFound(new { error = $"No federation route for workspace path '{route.WorkspacePath}'." });

        return NoContent();
    }

    /// <summary>
    /// Returns connection credentials for this server so a federated peer can generate a
    /// marker file that points to this server's public URL.
    /// The caller must supply a full-access workspace token as <c>X-Api-Key</c>.
    /// The workspace is looked up by <paramref name="workspaceName"/>; if found, a token is
    /// issued for that workspace's local path. Returns <c>404</c> when no enabled workspace
    /// with the given name is registered, so the caller can fall back to local credentials.
    /// </summary>
    /// <param name="workspaceName">Display name of the workspace to look up.</param>
    /// <param name="tokenService">Workspace token service (injected).</param>
    /// <param name="workspaceService">Workspace service for name lookup (injected).</param>
    /// <param name="serverRuntimeInfo">Server runtime info (injected).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Connection info including this server's local base URL and the workspace token, or 404.</returns>
    [HttpGet("connection")]
    public async Task<ActionResult<FederationConnectionInfo>> GetConnection(
        [FromQuery] string workspaceName,
        [FromServices] WorkspaceTokenService tokenService,
        [FromServices] IWorkspaceService workspaceService,
        [FromServices] ServerRuntimeInfo serverRuntimeInfo,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(workspaceName))
            return BadRequest(new { error = "workspaceName query parameter is required." });

        // Resolve the workspace by name — paths differ across machines.
        var workspaces = await workspaceService.ListAsync(ct).ConfigureAwait(false);
        var match = workspaces.Items.FirstOrDefault(w =>
            w.IsEnabled &&
            string.Equals(w.Name, workspaceName.Trim(), StringComparison.OrdinalIgnoreCase));

        if (match is null)
            return NotFound(new { error = $"No enabled workspace named '{workspaceName}' is registered on this server." });

        var token = tokenService.GetToken(match.WorkspacePath) ?? tokenService.GenerateToken(match.WorkspacePath);
        _ = tokenService.GetDefaultToken(match.WorkspacePath) ?? tokenService.GenerateDefaultToken(match.WorkspacePath);

        var port = serverRuntimeInfo.ListenPort;
        var baseUrl = $"http://{System.Net.Dns.GetHostName()}:{port.ToString(CultureInfo.InvariantCulture)}";
        return Ok(new FederationConnectionInfo(baseUrl, port, token));
    }

    /// <summary>
    /// Auto-discover federation targets from running tunnel providers.
    /// For each tunnel provider that is currently running and has a public URL, a corresponding
    /// federation target is registered (name = provider name, baseUrl = public URL).
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Discovery result with count of newly registered targets.</returns>
    [HttpPost("targets/discover-from-tunnels")]
    public async Task<ActionResult<TunnelDiscoveryResult>> DiscoverFromTunnels(CancellationToken ct)
    {
        var tunnels = await _tunnelRegistry.ListAsync(ct).ConfigureAwait(false);
        var discovered = new List<FederationTargetInfo>();

        foreach (var tunnel in tunnels.Where(t => t.IsRunning && t.PublicUrl is not null))
        {
            var opts = new FederationTargetOptions { Name = tunnel.Provider, BaseUrl = tunnel.PublicUrl! };
            if (_registry.TryAddTarget(opts, out _))
            {
                var info = _registry.List().FirstOrDefault(t =>
                    string.Equals(t.Name, tunnel.Provider, StringComparison.OrdinalIgnoreCase));
                if (info is not null)
                    discovered.Add(info);
            }
        }

        return Ok(new TunnelDiscoveryResult(discovered.Count, discovered));
    }

    private FederationStatusResponse BuildStatus()
        => new(_registry.IsEnabled, _registry.List(), _registry.ListRoutes());
}

/// <summary>FR-MCP-077: Full federation status snapshot returned by the management API.</summary>
/// <param name="Enabled">Whether federation is globally enabled.</param>
/// <param name="Targets">Registered federation targets.</param>
/// <param name="WorkspaceRoutes">Per-workspace routing rules.</param>
public sealed record FederationStatusResponse(
    bool Enabled,
    IReadOnlyList<FederationTargetInfo> Targets,
    IReadOnlyList<WorkspaceRouteInfo> WorkspaceRoutes);

/// <summary>FR-MCP-077: Result of a tunnel-based target auto-discovery operation.</summary>
/// <param name="Discovered">Number of new targets registered in this call.</param>
/// <param name="Targets">The newly registered target info objects.</param>
public sealed record TunnelDiscoveryResult(int Discovered, IReadOnlyList<FederationTargetInfo> Targets);

/// <summary>
/// FR-MCP-077: Connection credentials returned by the federation connection endpoint so a
/// federated peer can generate a marker file pointing to this server.
/// </summary>
/// <param name="BaseUrl">This server's local base URL (e.g. <c>http://hostname:7147</c>).</param>
/// <param name="Port">The TCP port the server is listening on.</param>
/// <param name="ApiKey">Full-access workspace token for the requested workspace path.</param>
public sealed record FederationConnectionInfo(string BaseUrl, int Port, string ApiKey);
