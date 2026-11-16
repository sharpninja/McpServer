using System.Security.Claims;
using System.Text.Json;
using QBrainAi.Support.Mcp.Identity;

namespace QBrainAi.Support.Mcp.Tests.Identity;

/// <summary>
/// Proves the provider-neutral AgentManager role-claim contract.
/// </summary>
/// <remarks>
/// 2026-11-16, FR-MCP-026 and TR-MCP-AUTH-001: added a reference-model gate
/// for the role-claim forms emitted by embedded IdentityServer and supported
/// external OIDC providers. No separate acceptance-criterion identifier exists
/// in the current requirement text.
/// </remarks>
public sealed class AgentManagerRoleAuthorizationTests
{
    /// <summary>
    /// Gets valid provider claim representations and their expected decision.
    /// </summary>
    public static TheoryData<string, string, bool> SupportedRoleClaims => new()
    {
        { "role", "agent-manager", true },
        { "role", "[\"viewer\",\"admin\"]", true },
        { "roles", "admin", true },
        { "realm_roles", "[\"viewer\",\"agent-manager\"]", true },
        { ClaimTypes.Role, "viewer, admin", true },
    };

    /// <summary>
    /// Gets invalid or unauthorized claim representations used as negative controls.
    /// </summary>
    public static TheoryData<string, string, bool> RejectedRoleClaims => new()
    {
        { "role", "viewer", false },
        { "groups", "admin", false },
        { "realm_roles", "[\"viewer\"]", false },
        { ClaimTypes.Role, "", false },
    };

    /// <summary>
    /// Confirms the independent reference evaluator accepts every authorized representation.
    /// </summary>
    /// <param name="claimType">OIDC claim type.</param>
    /// <param name="claimValue">OIDC claim value.</param>
    /// <param name="expected">Expected authorization decision.</param>
    [Theory]
    [MemberData(nameof(SupportedRoleClaims))]
    public void ReferenceEvaluator_AcceptsSupportedRoleClaims(string claimType, string claimValue, bool expected)
    {
        var principal = CreatePrincipal(claimType, claimValue);

        var actual = ReferenceHasAnyRole(principal, "agent-manager", "admin");

        Assert.Equal(expected, actual);
    }

    /// <summary>
    /// Confirms the independent reference evaluator rejects unsupported and unauthorized representations.
    /// </summary>
    /// <param name="claimType">OIDC claim type.</param>
    /// <param name="claimValue">OIDC claim value.</param>
    /// <param name="expected">Expected authorization decision.</param>
    [Theory]
    [MemberData(nameof(RejectedRoleClaims))]
    public void ReferenceEvaluator_RejectsInvalidRoleClaims(string claimType, string claimValue, bool expected)
    {
        var principal = CreatePrincipal(claimType, claimValue);

        var actual = ReferenceHasAnyRole(principal, "agent-manager", "admin");

        Assert.Equal(expected, actual);
    }

    /// <summary>
    /// Confirms an unauthenticated identity is never authorized by role claims.
    /// </summary>
    [Fact]
    public void ReferenceEvaluator_RejectsUnauthenticatedIdentity()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("role", "admin")]));

        var actual = ReferenceHasAnyRole(principal, "agent-manager", "admin");

        Assert.False(actual);
    }

    /// <summary>
    /// Binds the authorized reference vectors to the production policy evaluator.
    /// </summary>
    /// <param name="claimType">OIDC claim type.</param>
    /// <param name="claimValue">OIDC claim value.</param>
    /// <param name="expected">Expected authorization decision.</param>
    [Theory]
    [MemberData(nameof(SupportedRoleClaims))]
    public void ProductionEvaluator_AcceptsSupportedRoleClaims(string claimType, string claimValue, bool expected)
    {
        var principal = CreatePrincipal(claimType, claimValue);

        var actual = AgentManagerRoleAuthorization.HasAnyRole(principal, "agent-manager", "admin");

        Assert.Equal(expected, actual);
    }

    /// <summary>
    /// Binds the negative-control reference vectors to the production policy evaluator.
    /// </summary>
    /// <param name="claimType">OIDC claim type.</param>
    /// <param name="claimValue">OIDC claim value.</param>
    /// <param name="expected">Expected authorization decision.</param>
    [Theory]
    [MemberData(nameof(RejectedRoleClaims))]
    public void ProductionEvaluator_RejectsInvalidRoleClaims(string claimType, string claimValue, bool expected)
    {
        var principal = CreatePrincipal(claimType, claimValue);

        var actual = AgentManagerRoleAuthorization.HasAnyRole(principal, "agent-manager", "admin");

        Assert.Equal(expected, actual);
    }

    /// <summary>
    /// Confirms the production evaluator rejects an unauthenticated identity.
    /// </summary>
    [Fact]
    public void ProductionEvaluator_RejectsUnauthenticatedIdentity()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("role", "admin")]));

        var actual = AgentManagerRoleAuthorization.HasAnyRole(principal, "agent-manager", "admin");

        Assert.False(actual);
    }

    private static ClaimsPrincipal CreatePrincipal(string claimType, string claimValue)
        => new(new ClaimsIdentity([new Claim(claimType, claimValue)], "test"));

    private static bool ReferenceHasAnyRole(ClaimsPrincipal user, params string[] requiredRoles)
    {
        if (user.Identity?.IsAuthenticated != true)
            return false;

        var required = new HashSet<string>(requiredRoles, StringComparer.OrdinalIgnoreCase);
        var supportedTypes = new HashSet<string>(
            ["role", "roles", "realm_roles", ClaimTypes.Role],
            StringComparer.OrdinalIgnoreCase);

        foreach (var claim in user.Claims.Where(claim => supportedTypes.Contains(claim.Type)))
        {
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
            using var document = JsonDocument.Parse(trimmed);
            if (document.RootElement.ValueKind == JsonValueKind.Array &&
                document.RootElement.EnumerateArray().Any(element =>
                    !string.IsNullOrWhiteSpace(element.GetString()) && requiredRoles.Contains(element.GetString()!)))
            {
                return true;
            }
        }

        return trimmed
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(token => token.Trim('"'))
            .Any(requiredRoles.Contains);
    }
}
