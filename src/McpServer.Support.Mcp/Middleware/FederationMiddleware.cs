using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Middleware;

/// <summary>
/// FR-MCP-077: Intercepts incoming requests and proxies them to a configured remote MCP server
/// when server federation is enabled. Runs after <see cref="WorkspaceResolutionMiddleware"/>
/// (so the workspace is already resolved) and before <see cref="WorkspaceAuthMiddleware"/>
/// (federated requests skip local auth and let the remote server handle it).
/// <para>
/// Management endpoints at <c>/mcpserver/federation/*</c> are always served locally regardless
/// of federation state.
/// </para>
/// </summary>
public sealed class FederationMiddleware
{
    private const string FederationManagementPrefix = "/mcpserver/federation";
    private const string AuthPrefix = "/auth";

    private readonly RequestDelegate _next;
    private readonly FederationRegistry _registry;
    private readonly FederationProxyService _proxyService;
    private readonly int _maxHops;

    /// <summary>
    /// Initializes a new instance of the <see cref="FederationMiddleware"/> class.
    /// </summary>
    /// <param name="next">Next middleware delegate.</param>
    /// <param name="registry">Federation target registry.</param>
    /// <param name="proxyService">HTTP proxy service.</param>
    /// <param name="options">Federation configuration.</param>
    public FederationMiddleware(
        RequestDelegate next,
        FederationRegistry registry,
        FederationProxyService proxyService,
        IOptions<FederationOptions> options)
    {
        _next = next;
        _registry = registry;
        _proxyService = proxyService;
        _maxHops = options.Value.MaxHops;
    }

    /// <summary>Processes the request, proxying it when federation is active and a target is resolved.</summary>
    /// <param name="context">Current HTTP context.</param>
    /// <param name="workspaceContext">Resolved workspace identity from the preceding middleware.</param>
    public async Task InvokeAsync(HttpContext context, WorkspaceContext workspaceContext)
    {
        // Management API and auth endpoints are always served locally
        if (context.Request.Path.StartsWithSegments(FederationManagementPrefix, StringComparison.OrdinalIgnoreCase) ||
            context.Request.Path.StartsWithSegments(AuthPrefix, StringComparison.OrdinalIgnoreCase))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        // Fast path: federation globally disabled
        if (!_registry.IsEnabled)
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        // Anti-loop check
        if (TryDetectLoop(context, out var hopCount))
        {
            context.Response.StatusCode = 508; // Loop Detected
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                "{\"error\":\"Federation loop detected — too many forwarding hops.\"}",
                context.RequestAborted)
                .ConfigureAwait(false);
            return;
        }

        // Resolve federation target (workspace-specific → global default)
        var target = _registry.ResolveTarget(workspaceContext.WorkspacePath);
        if (target is null)
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        // Proxy to target
        await _proxyService.ProxyAsync(context, target, hopCount + 1, context.RequestAborted)
            .ConfigureAwait(false);
    }

    private bool TryDetectLoop(HttpContext context, out int hopCount)
    {
        hopCount = 0;
        var headerValue = context.Request.Headers[FederationProxyService.HopCountHeader].FirstOrDefault();

        if (headerValue is null)
            return false;

        if (!int.TryParse(headerValue, out hopCount))
            return true; // Malformed → treat as loop

        return hopCount >= _maxHops;
    }
}
