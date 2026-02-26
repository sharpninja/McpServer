using System.Text.Json;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.Middleware;

/// <summary>
/// Pipeline middleware that enforces authentication on all <c>/mcp/*</c> routes.
/// Two authentication mechanisms are supported, evaluated in this order:
/// <list type="number">
///   <item><description><strong>JWT Bearer</strong> — a valid OIDC token grants full access.
///     When a <c>Authorization: Bearer</c> header is present, API keys are completely
///     ignored. If the JWT fails validation, the request is rejected immediately —
///     no fallthrough to API-key auth occurs.</description></item>
///   <item><description><strong>API key</strong> — for agents that cannot perform OIDC.
///     Full-access keys (from marker files) grant unrestricted access. Default keys
///     (from <c>GET /api-key</c>) grant read-only access except for TODO routes.</description></item>
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
    private readonly ILogger<WorkspaceAuthMiddleware> _logger;

    /// <summary>Initializes a new instance of the <see cref="WorkspaceAuthMiddleware"/> class.</summary>
    public WorkspaceAuthMiddleware(RequestDelegate next,
        ILogger<WorkspaceAuthMiddleware> logger)
    {
        _logger = logger;
        _next = next;
    }

    /// <summary>Validates the auth token for <c>/mcp/*</c> requests.</summary>
    public async Task InvokeAsync(HttpContext context, WorkspaceTokenService tokenService, IConfiguration configuration, WorkspaceContext workspaceContext)
    {
        var path = context.Request.Path;

        // Only protect /mcp/* API routes.
        if (!path.StartsWithSegments("/mcp", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var method = context.Request.Method;

        // ── JWT path ──────────────────────────────────────────────────────────
        // When a Bearer header is present, API keys are IGNORED entirely.
        // JWT is the sole auth mechanism for that request.
        var hasBearerHeader = context.Request.Headers.Authorization.ToString()
            .StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase);

        _logger.LogDebug("[WS-Auth] {Method} {Path} | HasBearer={HasBearer} | ResolvedWorkspace={Workspace}",
            method, path, hasBearerHeader, workspaceContext.WorkspaceName ?? "(none)");

        if (hasBearerHeader)
        {
            var (authenticated, failureReason) = await TryAuthenticateJwtAsync(context).ConfigureAwait(false);
            if (authenticated)
            {
                _logger.LogInformation("[WS-Auth] {Method} {Path} | JWT OK → workspace={Workspace}",
                    method, path, workspaceContext.WorkspaceName ?? "(none)");
                await _next(context).ConfigureAwait(false);
                return;
            }

            // JWT was present but invalid — reject immediately, do NOT fall through to API key.
            _logger.LogWarning("[WS-Auth] {Method} {Path} | JWT FAILED: {Reason} → 401",
                method, path, failureReason ?? "unknown");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            var jwtError = new
            {
                error = $"JWT Bearer token could not be validated ({failureReason ?? "unknown reason"}). " +
                        "Re-authenticate with OIDC to obtain a fresh token."
            };
            await context.Response.WriteAsync(
                JsonSerializer.Serialize(jwtError, s_json),
                context.RequestAborted).ConfigureAwait(false);
            return;
        }

        // Also check if the user was already authenticated by prior middleware (e.g. cookie).
        if (context.User.Identity?.IsAuthenticated == true
            || context.User.Identities.Any(i => i.IsAuthenticated))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        // ── Agent mutation routes (OIDC-only) ─────────────────────────────────
        // When OIDC is enabled, agent mutation routes are JWT-protected via [Authorize(Policy="AgentManager")].
        // Skip API-key enforcement here so ASP.NET authorization can challenge/validate the bearer token.
        var oidcEnabled = !string.IsNullOrWhiteSpace(configuration["Mcp:Auth:Authority"]);
        if (oidcEnabled && IsAgentMutationRoute(path, context.Request.Method))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        // ── API key path (agents only) ────────────────────────────────────────
        var workspacePath = workspaceContext.WorkspacePath ?? configuration["Mcp:RepoRoot"] ?? string.Empty;

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

            // Write operation on a non-todo route with only a default key — reject.
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            var forbiddenBody = new
            {
                error = "Default API key grants read-only access to non-todo endpoints. " +
                        "Use the full workspace API key from the AGENTS-README-FIRST.yaml marker file for write operations, " +
                        "or authenticate with a valid JWT Bearer token."
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

    /// <summary>
    /// Attempts JWT Bearer authentication. Returns a tuple of (success, failureReason).
    /// Only called when a Bearer header is known to be present.
    /// </summary>
    private async Task<(bool Authenticated, string? FailureReason)> TryAuthenticateJwtAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
            return (true, null);

        if (context.User.Identities.Any(i => i.IsAuthenticated))
            return (true, null);

        try
        {
            var result = await context.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme).ConfigureAwait(false);
            if (result.Succeeded && result.Principal?.Identity?.IsAuthenticated == true)
            {
                context.User = result.Principal;
                return (true, null);
            }

            return (false, result.Failure?.Message ?? "token validation failed");
        }
        catch (InvalidOperationException ex)
        {
            // JWT Bearer scheme not registered (OIDC disabled).
            return (false, $"JWT scheme not available: {ex.Message}");
        }
    }
}
