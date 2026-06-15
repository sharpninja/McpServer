using System.Globalization;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

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
    private const string DeferredFederationMutationMessage =
        "Federation control-plane mutations are not transaction compensated while required turn transactions are active.";

    private readonly FederationRegistry _registry;
    private readonly TunnelRegistry _tunnelRegistry;
    private readonly IFederationPushService? _pushService;
    private readonly IFederationTopologyService? _topologyService;
    private readonly FederationStateAdapterRegistry? _adapterRegistry;
    private readonly IFederationEnvelopeSigner? _envelopeSigner;
    private readonly IFederationOperationApplyService? _operationApplyService;
    private readonly ITurnTransactionCoordinator? _transactionCoordinator;
    private readonly IOptions<TurnTransactionOptions>? _transactionOptions;

    /// <summary>Initializes a new instance of the <see cref="FederationController"/> class.</summary>
    /// <param name="registry">Federation target registry.</param>
    /// <param name="tunnelRegistry">Tunnel registry for auto-discovery.</param>
    /// <param name="pushService">Optional push service for federated data push operations.</param>
    /// <param name="topologyService">Optional hub/proxy topology service.</param>
    /// <param name="adapterRegistry">Optional state adapter registry.</param>
    /// <param name="envelopeSigner">Optional signed envelope verifier.</param>
    /// <param name="operationApplyService">Optional operation apply service used by signed hub intake.</param>
    /// <param name="transactionCoordinator">Optional turn transaction coordinator used to fail closed uncompensated federation control-plane mutations.</param>
    /// <param name="transactionOptions">Optional turn transaction options.</param>
    public FederationController(
        FederationRegistry registry,
        TunnelRegistry tunnelRegistry,
        IFederationPushService? pushService = null,
        IFederationTopologyService? topologyService = null,
        FederationStateAdapterRegistry? adapterRegistry = null,
        IFederationEnvelopeSigner? envelopeSigner = null,
        IFederationOperationApplyService? operationApplyService = null,
        ITurnTransactionCoordinator? transactionCoordinator = null,
        IOptions<TurnTransactionOptions>? transactionOptions = null)
    {
        _registry = registry;
        _tunnelRegistry = tunnelRegistry;
        _pushService = pushService;
        _topologyService = topologyService;
        _adapterRegistry = adapterRegistry;
        _envelopeSigner = envelopeSigner;
        _operationApplyService = operationApplyService;
        _transactionCoordinator = transactionCoordinator;
        _transactionOptions = transactionOptions;
    }

    /// <summary>Get the current federation status including all targets and workspace routes.</summary>
    /// <returns>Full federation status snapshot.</returns>
    [HttpGet("status")]
    public ActionResult<FederationStatusResponse> GetStatus()
        => Ok(BuildStatus());

    /// <summary>Enroll a LocalProxy with this hub.</summary>
    /// <param name="request">Enrollment payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Accepted enrollment response.</returns>
    [HttpPost("proxies/enroll")]
    public async Task<ActionResult<FederationEnrollmentResponse>> EnrollProxy(
        [FromBody] FederationEnrollmentRequest request,
        CancellationToken ct)
    {
        if (ShouldDeferFederationControlMutation(out var transactionError))
            return Conflict(new { error = transactionError });

        if (_topologyService is null)
            return StatusCode(501, new { error = "Federation topology service is not configured." });

        var response = await _topologyService.EnrollAsync(request, ct).ConfigureAwait(false);
        return Ok(response);
    }

    /// <summary>Record a heartbeat from an enrolled LocalProxy.</summary>
    /// <param name="proxyId">Proxy identifier.</param>
    /// <param name="request">Heartbeat payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Heartbeat result with queue and conflict counts.</returns>
    [HttpPost("proxies/{proxyId}/heartbeat")]
    public async Task<ActionResult<FederationHeartbeatResponse>> Heartbeat(
        string proxyId,
        [FromBody] FederationHeartbeatRequest request,
        CancellationToken ct)
    {
        if (ShouldDeferFederationControlMutation(out var transactionError))
            return Conflict(new { error = transactionError });

        if (_topologyService is null)
            return StatusCode(501, new { error = "Federation topology service is not configured." });

        var response = await _topologyService.HeartbeatAsync(proxyId, request, ct).ConfigureAwait(false);
        return Ok(response);
    }

    /// <summary>List enrolled proxies known by this hub.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Proxy inventory.</returns>
    [HttpGet("proxies")]
    public async Task<ActionResult<IReadOnlyList<FederationProxyInfo>>> ListProxies(CancellationToken ct)
    {
        if (_topologyService is null)
            return StatusCode(501, new { error = "Federation topology service is not configured." });

        return Ok(await _topologyService.ListProxiesAsync(ct).ConfigureAwait(false));
    }

    /// <summary>Register or update one workspace hosted by a proxy.</summary>
    /// <param name="proxyId">Proxy identifier.</param>
    /// <param name="request">Workspace registration payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Registered workspace info.</returns>
    [HttpPost("proxies/{proxyId}/workspaces")]
    public async Task<ActionResult<FederationWorkspaceInfo>> RegisterWorkspace(
        string proxyId,
        [FromBody] FederationWorkspaceRegistrationRequest request,
        CancellationToken ct)
    {
        if (ShouldDeferFederationControlMutation(out var transactionError))
            return Conflict(new { error = transactionError });

        if (_topologyService is null)
            return StatusCode(501, new { error = "Federation topology service is not configured." });

        var response = await _topologyService.RegisterWorkspaceAsync(proxyId, request, ct).ConfigureAwait(false);
        return Ok(response);
    }

    /// <summary>List global proxy-hosted workspaces, optionally scoped to one proxy.</summary>
    /// <param name="proxyId">Optional proxy filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Workspace inventory.</returns>
    [HttpGet("workspaces")]
    public async Task<ActionResult<IReadOnlyList<FederationWorkspaceInfo>>> ListWorkspaces(
        [FromQuery] string? proxyId,
        CancellationToken ct)
    {
        if (_topologyService is null)
            return StatusCode(501, new { error = "Federation topology service is not configured." });

        return Ok(await _topologyService.ListWorkspacesAsync(proxyId, ct).ConfigureAwait(false));
    }

    /// <summary>Accept or idempotently replay one federation operation.</summary>
    /// <param name="request">Operation payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Operation status.</returns>
    [HttpPost("operations")]
    public async Task<ActionResult<FederationOperationResponse>> RecordOperation(
        [FromBody] FederationOperationRequest request,
        CancellationToken ct)
    {
        if (ShouldDeferFederationControlMutation(out var transactionError))
            return Conflict(new { error = transactionError });

        if (_topologyService is null)
            return StatusCode(501, new { error = "Federation topology service is not configured." });

        var response = await _topologyService.RecordOperationAsync(request, ct).ConfigureAwait(false);
        return Ok(response);
    }

    /// <summary>Accept or idempotently replay one signed federation operation envelope.</summary>
    /// <param name="envelope">Signed operation envelope.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Operation status.</returns>
    [HttpPost("envelopes")]
    public async Task<ActionResult<FederationOperationResponse>> RecordEnvelope(
        [FromBody] FederationExecutionEnvelope envelope,
        CancellationToken ct)
    {
        if (_topologyService is null)
            return StatusCode(501, new { error = "Federation topology service is not configured." });
        if (_operationApplyService is null)
            return StatusCode(501, new { error = "Federation operation apply service is not configured." });
        if (_envelopeSigner is null || !_envelopeSigner.IsConfigured)
            return StatusCode(501, new { error = "Federation envelope signer is not configured." });

        var verification = _envelopeSigner.Verify(envelope);
        if (!verification.IsValid)
            return BadRequest(new { error = verification.Error });

        var response = await _topologyService.RecordOperationAsync(envelope.Operation, ct).ConfigureAwait(false);
        if (!ShouldApplySignedEnvelope(response.Status))
            return Ok(response);

        var apply = await _operationApplyService.ApplyAsync(envelope.Operation, ct).ConfigureAwait(false);
        if (apply.Conflict || (!apply.Applied && !apply.AlreadyApplied))
        {
            response = await _topologyService.AcknowledgeOperationAsync(
                    response.OperationId,
                    new FederationOperationAckRequest
                    {
                        Status = "conflict",
                        HubVersion = apply.Version,
                        Error = apply.Message ?? "Federation operation apply did not complete.",
                    },
                    ct)
                .ConfigureAwait(false);
        }
        else
        {
            response = await _topologyService.AcknowledgeOperationAsync(
                    response.OperationId,
                    new FederationOperationAckRequest
                    {
                        Status = apply.AlreadyApplied ? "already_applied" : "applied",
                        HubVersion = apply.Version,
                        Error = apply.Message,
                    },
                    ct)
                .ConfigureAwait(false);
        }

        return Ok(response);
    }

    /// <summary>Acknowledge one replayed or fanned-out operation.</summary>
    /// <param name="operationId">Operation identifier.</param>
    /// <param name="request">Acknowledgement payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Updated operation status.</returns>
    [HttpPost("operations/{operationId}/ack")]
    public async Task<ActionResult<FederationOperationResponse>> AcknowledgeOperation(
        string operationId,
        [FromBody] FederationOperationAckRequest request,
        CancellationToken ct)
    {
        if (ShouldDeferFederationControlMutation(out var transactionError))
            return Conflict(new { error = transactionError });

        if (_topologyService is null)
            return StatusCode(501, new { error = "Federation topology service is not configured." });

        var response = await _topologyService.AcknowledgeOperationAsync(operationId, request, ct).ConfigureAwait(false);
        return response.Status == "not_found" ? NotFound(response) : Ok(response);
    }

    /// <summary>Inspect queued operations, fanout depth, and conflicts.</summary>
    /// <param name="proxyId">Optional proxy filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Queue status.</returns>
    [HttpGet("queue")]
    public async Task<ActionResult<FederationQueueStatusResponse>> GetQueueStatus(
        [FromQuery] string? proxyId,
        CancellationToken ct)
    {
        if (_topologyService is null)
            return StatusCode(501, new { error = "Federation topology service is not configured." });

        return Ok(await _topologyService.GetQueueStatusAsync(proxyId, ct).ConfigureAwait(false));
    }

    /// <summary>List federation conflicts.</summary>
    /// <param name="proxyId">Optional proxy filter.</param>
    /// <param name="openOnly">Whether to return only open conflicts.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Conflict inventory.</returns>
    [HttpGet("conflicts")]
    public async Task<ActionResult<IReadOnlyList<FederationConflictInfo>>> ListConflicts(
        [FromQuery] string? proxyId,
        [FromQuery] bool openOnly = true,
        CancellationToken ct = default)
    {
        if (_topologyService is null)
            return StatusCode(501, new { error = "Federation topology service is not configured." });

        return Ok(await _topologyService.ListConflictsAsync(proxyId, openOnly, ct).ConfigureAwait(false));
    }

    /// <summary>Resolve a federation conflict.</summary>
    /// <param name="conflictId">Conflict identifier.</param>
    /// <param name="request">Resolution payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Resolved conflict info.</returns>
    [HttpPost("conflicts/{conflictId}/resolve")]
    public async Task<ActionResult<FederationConflictInfo>> ResolveConflict(
        string conflictId,
        [FromBody] FederationConflictResolutionRequest request,
        CancellationToken ct)
    {
        if (ShouldDeferFederationControlMutation(out var transactionError))
            return Conflict(new { error = transactionError });

        if (_topologyService is null)
            return StatusCode(501, new { error = "Federation topology service is not configured." });

        var response = await _topologyService.ResolveConflictAsync(conflictId, request, ct).ConfigureAwait(false);
        return response is null ? NotFound(new { error = $"Conflict '{conflictId}' not found." }) : Ok(response);
    }

    /// <summary>Return hub fanout rows for a proxy after a sequence.</summary>
    /// <param name="proxyId">Proxy identifier.</param>
    /// <param name="afterSequence">Exclusive sequence cursor.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Sync rows waiting for proxy acknowledgement.</returns>
    [HttpGet("sync")]
    public async Task<ActionResult<IReadOnlyList<FederationSyncItem>>> Sync(
        [FromQuery] string proxyId,
        [FromQuery] long afterSequence,
        CancellationToken ct)
    {
        if (_topologyService is null)
            return StatusCode(501, new { error = "Federation topology service is not configured." });

        var items = await _topologyService.GetSyncItemsAsync(proxyId, afterSequence, ct).ConfigureAwait(false);
        if (_envelopeSigner is { IsConfigured: true })
        {
            foreach (var item in items)
                item.Envelope = _envelopeSigner.Sign(item.ToRequest(), "hub", proxyId, ResolveSyncApplyMode(item));
        }

        return Ok(items);
    }

    /// <summary>Acknowledge one recipient-specific sync row.</summary>
    /// <param name="sequence">Sync sequence number.</param>
    /// <param name="request">Acknowledgement payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Updated operation status.</returns>
    [HttpPost("sync/{sequence:long}/ack")]
    public async Task<ActionResult<FederationOperationResponse>> AcknowledgeSync(
        long sequence,
        [FromBody] FederationSyncAckRequest request,
        CancellationToken ct)
    {
        if (ShouldDeferFederationControlMutation(out var transactionError))
            return Conflict(new { error = transactionError });

        if (_topologyService is null)
            return StatusCode(501, new { error = "Federation topology service is not configured." });

        var proxyId = !string.IsNullOrWhiteSpace(request.ProxyId)
            ? request.ProxyId
            : Request.Headers[FederationHeaders.ProxyId].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(proxyId))
            return BadRequest(new { error = $"ProxyId is required in body or {FederationHeaders.ProxyId} header." });

        var response = await _topologyService.AcknowledgeSyncItemAsync(proxyId, sequence, request, ct).ConfigureAwait(false);
        return response.Status == "not_found" ? NotFound(response) : Ok(response);
    }

    /// <summary>Return mutable state adapter coverage diagnostics.</summary>
    /// <returns>Adapter coverage rows.</returns>
    [HttpGet("adapters")]
    public ActionResult<IReadOnlyList<FederationStateAdapterCoverage>> GetAdapterCoverage()
        => _adapterRegistry is null
            ? StatusCode(501, new { error = "Federation adapter registry is not configured." })
            : Ok(_adapterRegistry.GetCoverage());

    /// <summary>Enable federation proxying globally.</summary>
    /// <returns>Updated federation status.</returns>
    [HttpPost("enable")]
    public ActionResult<FederationStatusResponse> Enable()
    {
        if (ShouldDeferFederationControlMutation(out var transactionError))
            return Conflict(new { error = transactionError });

        _registry.SetEnabled(true);
        return Ok(BuildStatus());
    }

    /// <summary>Disable federation proxying globally. In-flight proxied requests complete normally.</summary>
    /// <returns>Updated federation status.</returns>
    [HttpPost("disable")]
    public ActionResult<FederationStatusResponse> Disable()
    {
        if (ShouldDeferFederationControlMutation(out var transactionError))
            return Conflict(new { error = transactionError });

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
        if (ShouldDeferFederationControlMutation(out var transactionError))
            return Conflict(new { error = transactionError });

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
        if (ShouldDeferFederationControlMutation(out var transactionError))
            return Conflict(new { error = transactionError });

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
        if (ShouldDeferFederationControlMutation(out var transactionError))
            return Conflict(new { error = transactionError });

        if (!_registry.SetDefaultTarget(name))
            return NotFound(new { error = $"Federation target '{name}' not found." });

        return Ok(BuildStatus());
    }

    /// <summary>Clear the global default target (requests will only route via workspace-specific rules).</summary>
    /// <returns>Updated federation status.</returns>
    [HttpDelete("targets/default")]
    public ActionResult<FederationStatusResponse> ClearDefault()
    {
        if (ShouldDeferFederationControlMutation(out var transactionError))
            return Conflict(new { error = transactionError });

        _registry.SetDefaultTarget(null);
        return Ok(BuildStatus());
    }

    /// <summary>Add or update a workspace-specific routing rule.</summary>
    /// <param name="route">Workspace path and target name.</param>
    /// <returns>Updated route list, or 404 if the target does not exist.</returns>
    [HttpPost("routes")]
    public ActionResult<IReadOnlyList<WorkspaceRouteInfo>> AddRoute([FromBody] WorkspaceRouteOptions route)
    {
        if (ShouldDeferFederationControlMutation(out var transactionError))
            return Conflict(new { error = transactionError });

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
        if (ShouldDeferFederationControlMutation(out var transactionError))
            return Conflict(new { error = transactionError });

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
        if (ShouldDeferFederationControlMutation(out var transactionError))
            return Conflict(new { error = transactionError });

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
        if (ShouldDeferFederationControlMutation(out var transactionError))
            return Conflict(new { error = transactionError });

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

    /// <summary>
    /// FR-MCP-085: Push local data (TODOs, session logs) to the resolved federation target.
    /// Optionally filter by type using the <paramref name="request"/> body.
    /// </summary>
    /// <param name="request">Push request with optional type filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Push result with success/failure counts, 409 if federation is disabled, 404 if no target.</returns>
    [HttpPost("push")]
    public async Task<ActionResult<FederationPushResult>> Push([FromBody] FederationPushRequest request, CancellationToken ct)
    {
        if (ShouldDeferFederationControlMutation(out var transactionError))
            return Conflict(new { error = transactionError });

        if (!_registry.IsEnabled)
            return Conflict(new { error = "Federation is disabled." });

        if (_registry.ResolveTarget(null) is null)
            return NotFound(new { error = "No federation target resolved." });

        if (_pushService is null)
            return StatusCode(501, new { error = "Push service is not configured." });

        var types = request.Types ?? [];
        var pushAll = types.Count == 0;

        if (pushAll)
        {
            var result = await _pushService.PushAllAsync(ct).ConfigureAwait(false);
            return Ok(result);
        }

        var succeeded = 0;
        var failed = 0;
        var errors = new List<string>();

        if (types.Contains("todos", StringComparer.OrdinalIgnoreCase))
        {
            var r = await _pushService.PushTodosAsync(ct).ConfigureAwait(false);
            succeeded += r.Succeeded;
            failed += r.Failed;
            errors.AddRange(r.Errors);
        }

        if (types.Contains("sessionlogs", StringComparer.OrdinalIgnoreCase))
        {
            var r = await _pushService.PushSessionLogsAsync(ct).ConfigureAwait(false);
            succeeded += r.Succeeded;
            failed += r.Failed;
            errors.AddRange(r.Errors);
        }

        return Ok(new FederationPushResult(succeeded, failed, errors));
    }

    private bool ShouldDeferFederationControlMutation(out string error)
    {
        error = string.Empty;
        if (_transactionCoordinator is null)
            return false;

        var status = _transactionCoordinator.GetStatus();
        if (status.Degraded)
        {
            error = string.IsNullOrWhiteSpace(status.Message)
                ? "Turn transaction coordinator is degraded."
                : status.Message;
            return true;
        }

        if (!RequiresMutationTransactions(status))
            return false;

        error = DeferredFederationMutationMessage;
        return true;
    }

    private bool RequiresMutationTransactions(TurnTransactionStatusResponse status)
        => status.Enabled && (_transactionOptions?.Value.RequiredForMutations ?? true);

    private FederationStatusResponse BuildStatus()
    {
        var snapshot = _topologyService?.GetSnapshot() ?? new FederationTopologySnapshot();
        return new FederationStatusResponse
        {
            Enabled = _registry.IsEnabled,
            Role = _registry.EffectiveRole.ToString(),
            ConfiguredRole = _registry.ConfiguredRole.ToString(),
            HubBaseUrl = _registry.HubBaseUrl,
            ProxyId = _registry.ProxyId,
            HasEnrollmentToken = _registry.HasEnrollmentToken,
            Targets = _registry.List(),
            WorkspaceRoutes = _registry.ListRoutes(),
            ProxyCount = snapshot.ProxyCount,
            HostedWorkspaceCount = snapshot.WorkspaceCount,
            QueueDepth = snapshot.QueueDepth,
            ConflictCount = snapshot.ConflictCount,
            FanoutDepth = snapshot.FanoutDepth,
            StaleReadStatus = snapshot.QueueDepth > 0 || snapshot.FanoutDepth > 0 ? "stale" : "none",
        };
    }

    private static bool ShouldApplySignedEnvelope(string? status)
        => string.IsNullOrWhiteSpace(status) ||
           status.Equals("accepted", StringComparison.OrdinalIgnoreCase) ||
           status.Equals("queued", StringComparison.OrdinalIgnoreCase) ||
           status.Equals("replay_failed", StringComparison.OrdinalIgnoreCase);

    private static string ResolveSyncApplyMode(FederationSyncItem item)
        => string.Equals(item.Domain, "local_execution", StringComparison.OrdinalIgnoreCase)
            ? "local_execution"
            : "state";
}

/// <summary>FR-MCP-077: Full federation status snapshot returned by the management API.</summary>
public sealed class FederationStatusResponse
{
    /// <summary>Whether federation is globally enabled.</summary>
    public bool Enabled { get; set; }

    /// <summary>Effective federation role after compatibility inference.</summary>
    public string Role { get; set; } = FederationRole.Standalone.ToString();

    /// <summary>Configured federation role before compatibility inference.</summary>
    public string ConfiguredRole { get; set; } = FederationRole.Standalone.ToString();

    /// <summary>Hub base URL configured for LocalProxy mode.</summary>
    public string? HubBaseUrl { get; set; }

    /// <summary>Stable local proxy identifier.</summary>
    public string? ProxyId { get; set; }

    /// <summary>Whether an enrollment token is configured. The token value is never returned.</summary>
    public bool HasEnrollmentToken { get; set; }

    /// <summary>Registered federation targets.</summary>
    public IReadOnlyList<FederationTargetInfo> Targets { get; set; } = [];

    /// <summary>Per-workspace routing rules.</summary>
    public IReadOnlyList<WorkspaceRouteInfo> WorkspaceRoutes { get; set; } = [];

    /// <summary>Number of enrolled proxies known by the hub.</summary>
    public int ProxyCount { get; set; }

    /// <summary>Number of proxy-hosted workspaces known by the hub.</summary>
    public int HostedWorkspaceCount { get; set; }

    /// <summary>Number of queued operations waiting for replay or acknowledgement.</summary>
    public int QueueDepth { get; set; }

    /// <summary>Number of open conflicts.</summary>
    public int ConflictCount { get; set; }

    /// <summary>Number of unacknowledged fanout rows.</summary>
    public int FanoutDepth { get; set; }

    /// <summary>Current stale-read status. <c>none</c> means no stale read is currently reported.</summary>
    public string StaleReadStatus { get; set; } = "none";
}

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

/// <summary>
/// FR-MCP-085: Request body for the federation push endpoint.
/// When <see cref="Types"/> is empty or null, all data types are pushed.
/// Valid type values: <c>"todos"</c>, <c>"sessionlogs"</c>.
/// </summary>
public sealed class FederationPushRequest
{
    /// <summary>Optional filter for which data types to push. Empty means push all.</summary>
    public IReadOnlyList<string>? Types { get; set; }
}
