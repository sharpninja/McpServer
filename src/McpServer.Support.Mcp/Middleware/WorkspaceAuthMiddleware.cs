using System.Text.Json;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace McpServer.Support.Mcp.Middleware;

/// <summary>
/// Pipeline middleware that enforces per-workspace auth tokens on all <c>/mcp/*</c> routes.
/// Two token tiers are supported:
/// <list type="bullet">
///   <item><description><strong>Full-access</strong> — the per-workspace token from the marker
///     file. Grants unrestricted access to all endpoints.</description></item>
///   <item><description><strong>Default (anonymous)</strong> — the token returned by
///     <c>GET /api-key</c>. Grants read-only access to all endpoints <strong>except</strong>
///     TODO routes (<c>/mcp/todo*</c>) which are read-write.</description></item>
/// </list>
/// Non-<c>/mcp/</c> routes (health, swagger, MCP transport, <c>/api-key</c>) pass through unprotected.
/// </summary>
public sealed class WorkspaceAuthMiddleware
{
    /// <summary>The HTTP header used to supply the workspace auth token.</summary>
    public const string HeaderName = "X-Api-Key";

    /// <summary>The query-string parameter accepted as a fallback.</summary>
    public const string QueryParam = "api_key";

    /// <summary>
    /// Key set in <see cref="HttpContext.Items"/> when the request was authenticated with a
    /// default (anonymous) token. Downstream controllers/middleware can inspect this to
    /// enforce additional restrictions.
    /// </summary>
    public const string IsDefaultKeyItem = "IsDefaultKey";

    private static readonly JsonSerializerOptions s_json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private static readonly HashSet<string> s_readOnlyMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "GET", "HEAD", "OPTIONS"
    };

    private readonly RequestDelegate _next;

    /// <summary>Initializes a new instance of the <see cref="WorkspaceAuthMiddleware"/> class.</summary>
    public WorkspaceAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>Validates the auth token for <c>/mcp/*</c> requests.</summary>
    public async Task InvokeAsync(HttpContext context, WorkspaceTokenService tokenService, IConfiguration configuration)
    {
        var path = context.Request.Path;

        // Only protect /mcp/* API routes.
        if (!path.StartsWithSegments("/mcp", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        // A valid user auth token (e.g., JWT bearer) should satisfy authentication without also
        // requiring a workspace API key.
        // We check both the current principal and (when a Bearer header is present) explicitly
        // authenticate the JWT scheme so this remains correct even if pipeline ordering changes.
        if (await HasAuthenticatedJwtAsync(context).ConfigureAwait(false))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        // When OIDC is enabled, agent mutation routes are JWT-protected via [Authorize(Policy="AgentManager")].
        // Skip API-key enforcement here so ASP.NET authorization can challenge/validate the bearer token.
        var oidcEnabled = !string.IsNullOrWhiteSpace(configuration["Mcp:Auth:Authority"]);
        if (oidcEnabled && IsAgentMutationRoute(path, context.Request.Method))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var workspacePath = configuration["Mcp:RepoRoot"] ?? string.Empty;

        // If no workspace is configured or no token generated yet (startup race), allow through.
        if (string.IsNullOrWhiteSpace(workspacePath))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var expected = tokenService.GetToken(workspacePath);
        if (expected is null)
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var provided = context.Request.Headers[HeaderName].FirstOrDefault()
                       ?? context.Request.Query[QueryParam].FirstOrDefault();

        // Full-access token — unrestricted.
        if (tokenService.ValidateToken(workspacePath, provided))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        // Default (anonymous) token — read-only except for TODO routes.
        if (tokenService.ValidateDefaultToken(workspacePath, provided))
        {
            context.Items[IsDefaultKeyItem] = true;

            var isTodoRoute = path.StartsWithSegments("/mcp/todo", StringComparison.OrdinalIgnoreCase);
            var isReadOnly = s_readOnlyMethods.Contains(context.Request.Method);

            if (isTodoRoute || isReadOnly)
            {
                await _next(context).ConfigureAwait(false);
                return;
            }

            // Write operation on a non-todo route with a default key → forbidden.
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            var forbiddenBody = new
            {
                error = "Default API key grants read-only access to non-todo endpoints. " +
                        "Use the full workspace API key from the AGENTS-README-FIRST.yaml marker file for write operations."
            };
            await context.Response.WriteAsync(
                JsonSerializer.Serialize(forbiddenBody, s_json),
                context.RequestAborted).ConfigureAwait(false);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";
        var body = new
        {
            error = "Invalid or missing API key. Re-read the AGENTS-README-FIRST.yaml marker file in the workspace root to get the current auth token and include it as the X-Api-Key header."
        };
        await context.Response.WriteAsync(
            JsonSerializer.Serialize(body, s_json),
            context.RequestAborted).ConfigureAwait(false);
    }

    private static bool IsAgentMutationRoute(PathString path, string method)
    {
        if (!path.StartsWithSegments("/mcp/agents", StringComparison.OrdinalIgnoreCase))
            return false;

        return !s_readOnlyMethods.Contains(method);
    }

    private static async Task<bool> HasAuthenticatedJwtAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
            return true;

        var authorization = context.Request.Headers.Authorization.ToString();
        if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return false;

        var result = await context.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme).ConfigureAwait(false);
        if (!result.Succeeded || result.Principal?.Identity?.IsAuthenticated != true)
            return false;

        context.User = result.Principal;
        return true;
    }
}
