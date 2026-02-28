using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.Middleware;

/// <summary>
/// TR-MCP-MT-002: Resolves workspace identity per-request using a two-tier chain:
/// <list type="number">
///   <item><description><c>X-Workspace-Path</c> header — explicit workspace path (highest priority).</description></item>
///   <item><description><c>X-Api-Key</c> reverse lookup via <see cref="WorkspaceTokenService"/>.</description></item>
/// </list>
/// If neither tier resolves a workspace, workspace-independent routes pass through with an
/// empty <see cref="WorkspaceContext"/>; workspace-required routes receive a <c>404</c>.
/// Populates the scoped <see cref="WorkspaceContext"/> for downstream services.
/// Non-<c>/mcp/</c> and non-<c>/mcp-transport</c> routes skip resolution.
/// </summary>
public sealed class WorkspaceResolutionMiddleware
{
    /// <summary>HTTP header for explicit workspace path targeting.</summary>
    public const string WorkspacePathHeader = "X-Workspace-Path";

    /// <summary>
    /// Route prefixes (under <c>/mcp</c>) that do NOT require a resolved workspace.
    /// Requests to these routes pass through with an empty <see cref="WorkspaceContext"/>
    /// when no header or API key identifies a workspace.
    /// </summary>
    private static readonly HashSet<string> WorkspaceIndependentPrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "/mcp/workspace",
        "/mcp/todo",
        "/mcp/sessionlog",
        "/mcp/requirements",
        "/mcp/repo",
        "/mcp/tools",
        "/mcp/tunnel",
        "/mcp/diagnostic",
        "/mcp/events",
        "/mcp/gh",
        "/mcp/context",
        "/mcp/voice",
        "/mcp-transport",
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<WorkspaceResolutionMiddleware> _logger;

    /// <summary>Initializes a new instance of the <see cref="WorkspaceResolutionMiddleware"/> class.</summary>
    public WorkspaceResolutionMiddleware(RequestDelegate next, ILogger<WorkspaceResolutionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>Resolves workspace identity and populates <see cref="WorkspaceContext"/>.</summary>
    public async Task InvokeAsync(
        HttpContext context,
        WorkspaceContext workspaceContext,
        WorkspaceTokenService tokenService,
        IWorkspaceService workspaceService)
    {
        var path = context.Request.Path;

        // Only resolve for /mcp/* and /mcp-transport routes.
        if (!path.StartsWithSegments("/mcp", StringComparison.OrdinalIgnoreCase)
            && !path.StartsWithSegments("/mcp-transport", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var method = context.Request.Method;
        var hasBearerToken = context.Request.Headers.Authorization.ToString()
            .StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase);

        // Tier 1: X-Workspace-Path header
        var headerValue = context.Request.Headers[WorkspacePathHeader].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(headerValue))
        {
            var ws = await workspaceService.GetAsync(headerValue, context.RequestAborted).ConfigureAwait(false);
            if (ws is null)
            {
                _logger.LogWarning("[WS-Resolve] {Method} {Path} | Tier1 FAILED: X-Workspace-Path='{HeaderValue}' not found in registered workspaces",
                    method, path, headerValue);
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(
                    $$"""{"error":"Unknown workspace path in {{WorkspacePathHeader}} header: '{{headerValue}}'."}""",
                    context.RequestAborted).ConfigureAwait(false);
                return;
            }

            _logger.LogInformation("[WS-Resolve] {Method} {Path} | Tier1 OK: X-Workspace-Path='{HeaderValue}' → {WorkspaceName}",
                method, path, headerValue, ws.Name);
            PopulateContext(workspaceContext, ws, isDefault: false, context);
            await _next(context).ConfigureAwait(false);
            return;
        }

        // Tier 2: API key reverse lookup — only for agent callers (no Bearer token).
        if (!hasBearerToken)
        {
            var apiKey = context.Request.Headers[WorkspaceAuthMiddleware.HeaderName].FirstOrDefault()
                         ?? context.Request.Query[WorkspaceAuthMiddleware.QueryParam].FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                var resolvedPath = tokenService.ResolveWorkspaceByToken(apiKey, out var isDefault);
                if (resolvedPath is not null)
                {
                    var ws = await workspaceService.GetAsync(resolvedPath, context.RequestAborted).ConfigureAwait(false);
                    if (ws is not null)
                    {
                        _logger.LogInformation("[WS-Resolve] {Method} {Path} | Tier2 OK: ApiKey reverse lookup → {WorkspaceName} (isDefault={IsDefault})",
                            method, path, ws.Name, isDefault);
                        PopulateContext(workspaceContext, ws, isDefault, context);
                        await _next(context).ConfigureAwait(false);
                        return;
                    }

                    _logger.LogWarning("[WS-Resolve] {Method} {Path} | Tier2 PARTIAL: ApiKey resolved path '{ResolvedPath}' but workspace not found in service",
                        method, path, resolvedPath);
                }
                else
                {
                    _logger.LogWarning("[WS-Resolve] {Method} {Path} | Tier2 FAILED: ApiKey not recognized by token service",
                        method, path);
                }
            }
        }
        else
        {
            _logger.LogDebug("[WS-Resolve] {Method} {Path} | Tier2 SKIPPED: Bearer token present, no X-Workspace-Path header",
                method, path);
        }

        // No workspace resolved — check whether this route requires one.
        if (IsWorkspaceIndependent(path))
        {
            _logger.LogDebug("[WS-Resolve] {Method} {Path} | SKIP: workspace-independent route, proceeding without workspace",
                method, path);
            await _next(context).ConfigureAwait(false);
            return;
        }

        // Workspace-required route with no workspace resolved — reject.
        _logger.LogWarning("[WS-Resolve] {Method} {Path} | REJECTED: workspace-required route but no workspace resolved (HasBearer={HasBearer})",
            method, path, hasBearerToken);
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(
            """{"error":"Workspace required. Send X-Workspace-Path header."}""",
            context.RequestAborted).ConfigureAwait(false);
    }

    private static bool IsWorkspaceIndependent(PathString path)
    {
        foreach (var prefix in WorkspaceIndependentPrefixes)
        {
            if (path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static void PopulateContext(WorkspaceContext ctx, WorkspaceDto ws, bool isDefault, HttpContext httpContext)
    {
        ctx.WorkspacePath = ws.WorkspacePath;
        ctx.WorkspaceName = ws.Name;
        ctx.DataDirectory = ws.DataDirectory;
        ctx.TodoFilePath = ws.TodoPath;

        // A JWT-authenticated user is never treated as a "default key" user, even when the
        // workspace was resolved via the anonymous API key.
        ctx.IsDefaultKey = isDefault && httpContext.User.Identity?.IsAuthenticated != true;

        // Derive session and external docs paths from workspace path
        ctx.SessionsPath = Path.Combine(ws.WorkspacePath, "docs", "sessions");
        ctx.ExternalDocsPath = Path.Combine(ws.WorkspacePath, "docs", "external");
    }
}
