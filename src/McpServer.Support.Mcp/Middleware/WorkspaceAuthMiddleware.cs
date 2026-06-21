using System.Text.Json;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Middleware;

/// <summary>
/// Pipeline middleware that enforces authentication on all <c>/mcpserver/*</c> routes.
/// Two authentication mechanisms are supported, evaluated in this order:
/// <list type="number">
///   <item><description><strong>JWT Bearer</strong> — a valid OIDC token grants full access.
///     When a <c>Authorization: Bearer</c> header is present, API keys are completely
///     ignored. If the JWT fails validation, the request is rejected immediately —
///     no fallthrough to API-key auth occurs.</description></item>
///   <item><description><strong>API key</strong> — for agents that cannot perform OIDC.
///     Full-access keys (from marker files) grant unrestricted access. Default keys
///     (from <c>GET /api-key</c>) grant read-only access only.</description></item>
/// </list>
/// Non-<c>/mcpserver/</c> routes (health, swagger, MCP transport, <c>/api-key</c>) pass through unprotected.
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

    /// <summary>Validates the auth token for <c>/mcpserver/*</c> requests.</summary>
    public async Task InvokeAsync(
        HttpContext context,
        WorkspaceTokenService tokenService,
        IConfiguration configuration,
        WorkspaceContext workspaceContext,
        IOptions<FederationOptions> federationOptions)
    {
        var path = context.Request.Path;

        // Only protect /mcpserver/* API routes; /mcp-transport and other non-/mcpserver/ routes pass through.
        if (!path.HasValue || !path.Value.StartsWith("/mcpserver/", StringComparison.OrdinalIgnoreCase))
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

        var provided = context.Request.Headers[HeaderName].FirstOrDefault()
                       ?? context.Request.Query[QueryParam].FirstOrDefault();

        if (IsValidHubAccessToken(provided, federationOptions.Value))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        // ── API key path (agents only) ────────────────────────────────────────
        var workspacePath = workspaceContext.WorkspacePath ?? configuration["Mcp:RepoRoot"] ?? string.Empty;
        var expected = string.IsNullOrWhiteSpace(workspacePath) ? null : tokenService.GetToken(workspacePath);

        if (expected is not null)
        {
            // Full-access token — unrestricted.
            if (tokenService.ValidateToken(workspacePath, provided))
            {
                await _next(context).ConfigureAwait(false);
                return;
            }

            // Default (anonymous) token — read-only only.
            if (tokenService.ValidateDefaultToken(workspacePath, provided))
            {
                context.Items[IsDefaultKeyItem] = true;
                if (s_readOnlyMethods.Contains(context.Request.Method))
                {
                    await _next(context).ConfigureAwait(false);
                    return;
                }

                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                var forbiddenBody = new
                {
                    error = "Default API key grants read-only access only. " +
                            "Use the full workspace API key from the AGENTS-README-FIRST.yaml marker file or a valid JWT Bearer token for write operations."
                };
                await context.Response.WriteAsync(
                    JsonSerializer.Serialize(forbiddenBody, s_json),
                    context.RequestAborted).ConfigureAwait(false);
                return;
            }

            // Known workspace, wrong key → 401.
            await WriteUnauthorizedAsync(context).ConfigureAwait(false);
            return;
        }

        // `expected` is null: no full token for the effective workspace path.
        // TR-MCP-AUTH-010: 503 is reserved strictly for genuine startup readiness — no full token
        // has been seeded for any workspace yet, so we cannot authenticate anyone.
        if (!tokenService.IsInitialized)
        {
            _logger.LogWarning("[WS-Auth] {Method} {Path} | Auth-token subsystem not initialized → 503 (startup)",
                method, path);
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.Response.Headers.RetryAfter = "5";
            context.Response.ContentType = "application/json";
            var startupBody = new
            {
                error = "Workspace authentication is starting up: the per-workspace token subsystem has not been initialized yet. Retry shortly."
            };
            await context.Response.WriteAsync(
                JsonSerializer.Serialize(startupBody, s_json),
                context.RequestAborted).ConfigureAwait(false);
            return;
        }

        // Subsystem is initialized: a valid full/default token would have reverse-resolved a workspace
        // in WorkspaceResolutionMiddleware and produced a non-null expected token here.
        _logger.LogWarning("[WS-Auth] {Method} {Path} | Unresolved workspace / unknown API key (initialized) → 401",
            method, path);
        await WriteUnauthorizedAsync(context).ConfigureAwait(false);
    }

    private static async Task WriteUnauthorizedAsync(HttpContext context)
    {
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

    private static bool IsValidHubAccessToken(string? provided, FederationOptions options)
    {
        if (!options.Enabled ||
            options.Role != FederationRole.Hub ||
            string.IsNullOrWhiteSpace(options.HubAccessToken) ||
            string.IsNullOrWhiteSpace(provided))
        {
            return false;
        }

        return string.Equals(options.HubAccessToken.Trim(), provided, StringComparison.Ordinal);
    }

    private static bool IsAgentMutationRoute(PathString path, string method)
    {
        if (!path.StartsWithSegments("/mcpserver/agents", StringComparison.OrdinalIgnoreCase))
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
