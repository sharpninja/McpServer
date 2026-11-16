using System.Security.Claims;
using System.Text.Json;

namespace QBrainAi.Support.Mcp.Identity;

/// <summary>
/// Evaluates provider-neutral role claims for the AgentManager authorization policy.
/// </summary>
/// <remarks>
/// 2026-11-16, FR-MCP-026 and TR-MCP-AUTH-001: extracted the existing policy
/// evaluator and added the singular <c>role</c> claim emitted by the embedded
/// IdentityServer. The current requirement text has no separate acceptance-
/// criterion identifier for the provider claim representation.
/// </remarks>
internal static class AgentManagerRoleAuthorization
{
    private static readonly HashSet<string> s_supportedClaimTypes = new(
        ["role", "roles", "realm_roles", ClaimTypes.Role],
        StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Determines whether an authenticated principal has any required role in a supported claim form.
    /// </summary>
    /// <param name="user">Authenticated principal to inspect.</param>
    /// <param name="requiredRoles">Roles that authorize the operation.</param>
    /// <returns><see langword="true"/> when at least one supported claim grants a required role.</returns>
    internal static bool HasAnyRole(ClaimsPrincipal user, params string[] requiredRoles)
    {
        if (user.Identity?.IsAuthenticated != true)
            return false;

        var required = new HashSet<string>(requiredRoles, StringComparer.OrdinalIgnoreCase);
        foreach (var claim in user.Claims)
        {
            if (!s_supportedClaimTypes.Contains(claim.Type))
                continue;

            if (ContainsRequiredRole(claim.Value, required))
                return true;
        }

        return false;
    }

    private static bool ContainsRequiredRole(string? claimValue, ISet<string> requiredRoles)
    {
        if (string.IsNullOrWhiteSpace(claimValue))
            return false;

        var trimmed = claimValue.Trim();
        if (requiredRoles.Contains(trimmed))
            return true;

        if (trimmed.StartsWith("[", StringComparison.Ordinal))
        {
            try
            {
                using var document = JsonDocument.Parse(trimmed);
                if (document.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var element in document.RootElement.EnumerateArray())
                    {
                        var role = element.GetString();
                        if (!string.IsNullOrWhiteSpace(role) && requiredRoles.Contains(role))
                            return true;
                    }
                }
            }
            catch (JsonException exception)
            {
                System.Diagnostics.Trace.TraceWarning(exception.ToString());
            }
        }

        foreach (var token in trimmed.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var normalized = token.Trim('"');
            if (requiredRoles.Contains(normalized))
                return true;
        }

        return false;
    }
}
