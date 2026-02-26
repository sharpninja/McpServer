using McpServer.Support.Mcp.Services;

namespace McpServer.Support.Mcp.Middleware;

/// <summary>
/// TR-MCP-MT-002: Resolves workspace identity per-request using a three-tier chain:
/// <list type="number">
///   <item><description><c>X-Workspace-Path</c> header — explicit workspace path (highest priority).</description></item>
///   <item><description><c>X-Api-Key</c> reverse lookup via <see cref="WorkspaceTokenService"/>.</description></item>
///   <item><description>Default (primary) workspace from configuration (lowest priority).</description></item>
/// </list>
/// Populates the scoped <see cref="WorkspaceContext"/> for downstream services.
/// Non-<c>/mcp/</c> and non-<c>/mcp-transport</c> routes skip resolution.
/// </summary>
public sealed class WorkspaceResolutionMiddleware
{
    /// <summary>HTTP header for explicit workspace path targeting.</summary>
    public const string WorkspacePathHeader = "X-Workspace-Path";

    private readonly RequestDelegate _next;

    /// <summary>Initializes a new instance of the <see cref="WorkspaceResolutionMiddleware"/> class.</summary>
    public WorkspaceResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>Resolves workspace identity and populates <see cref="WorkspaceContext"/>.</summary>
    public async Task InvokeAsync(
        HttpContext context,
        WorkspaceContext workspaceContext,
        WorkspaceTokenService tokenService,
        IWorkspaceService workspaceService,
        IConfiguration configuration)
    {
        var path = context.Request.Path;

        // Only resolve for /mcp/* and /mcp-transport routes.
        if (!path.StartsWithSegments("/mcp", StringComparison.OrdinalIgnoreCase)
            && !path.StartsWithSegments("/mcp-transport", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        // Tier 1: X-Workspace-Path header
        var headerValue = context.Request.Headers[WorkspacePathHeader].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(headerValue))
        {
            var ws = await workspaceService.GetAsync(headerValue, context.RequestAborted).ConfigureAwait(false);
            if (ws is null)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(
                    $$"""{"error":"Unknown workspace path in {{WorkspacePathHeader}} header: '{{headerValue}}'."}""",
                    context.RequestAborted).ConfigureAwait(false);
                return;
            }

            PopulateContext(workspaceContext, ws, isDefault: false);
            await _next(context).ConfigureAwait(false);
            return;
        }

        // Tier 2: API key reverse lookup
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
                    PopulateContext(workspaceContext, ws, isDefault);
                    await _next(context).ConfigureAwait(false);
                    return;
                }
            }
        }

        // Tier 3: Default (primary) workspace
        var defaultPath = configuration["Mcp:RepoRoot"];
        if (!string.IsNullOrWhiteSpace(defaultPath))
        {
            var ws = await workspaceService.GetAsync(defaultPath, context.RequestAborted).ConfigureAwait(false);
            if (ws is not null)
            {
                PopulateContext(workspaceContext, ws, isDefault: false);
                await _next(context).ConfigureAwait(false);
                return;
            }
        }

        // Fallback: find primary workspace from registered list
        var list = await workspaceService.ListAsync(context.RequestAborted).ConfigureAwait(false);
        var primary = list.Items.FirstOrDefault(w => w.IsPrimary) ?? list.Items.FirstOrDefault();
        if (primary is not null)
        {
            PopulateContext(workspaceContext, primary, isDefault: false);
        }

        await _next(context).ConfigureAwait(false);
    }

    private static void PopulateContext(WorkspaceContext ctx, WorkspaceDto ws, bool isDefault)
    {
        ctx.WorkspacePath = ws.WorkspacePath;
        ctx.WorkspaceName = ws.Name;
        ctx.DataDirectory = ws.DataDirectory;
        ctx.TodoFilePath = ws.TodoPath;
        ctx.IsDefaultKey = isDefault;

        // Derive session and external docs paths from workspace path
        ctx.SessionsPath = Path.Combine(ws.WorkspacePath, "docs", "sessions");
        ctx.ExternalDocsPath = Path.Combine(ws.WorkspacePath, "docs", "external");
    }
}
