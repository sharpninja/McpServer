using System.Text.Json;
using McpServer.Support.Mcp.Services;

namespace McpServer.Support.Mcp.Middleware;

/// <summary>
/// Pipeline middleware that enforces per-workspace auth tokens on all <c>/mcp/*</c> routes.
/// Tokens rotate on every service restart and are published in the
/// <c>AGENTS-README-FIRST.yaml</c> marker file so agents auto-discover them.
/// Non-<c>/mcp/</c> routes (health, swagger, MCP transport, etc.) pass through unprotected.
/// </summary>
public sealed class WorkspaceAuthMiddleware
{
    /// <summary>The HTTP header used to supply the workspace auth token.</summary>
    public const string HeaderName = "X-Api-Key";

    /// <summary>The query-string parameter accepted as a fallback.</summary>
    public const string QueryParam = "api_key";

    private static readonly JsonSerializerOptions s_json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

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

        if (tokenService.ValidateToken(workspacePath, provided))
        {
            await _next(context).ConfigureAwait(false);
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
}
