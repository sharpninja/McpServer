using McpServer.UI.Core.Authorization;

namespace McpServer.Director.Auth;

/// <summary>
/// Director implementation of <see cref="IRoleContext"/>, sourced from the cached OIDC token.
/// </summary>
internal sealed class DirectorRoleContext : IRoleContext
{
    /// <inheritdoc />
    public bool IsAuthenticated => GetActiveUser() is not null;

    /// <inheritdoc />
    public IReadOnlyList<string> Roles
        => GetActiveUser()?.Roles
            ?.Where(static r => !string.IsNullOrWhiteSpace(r))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray()
            ?? [];

    /// <inheritdoc />
    public bool HasRole(string role)
        => Roles.Any(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase));

    private static TokenInfo? GetActiveUser()
    {
        var user = OidcAuthService.GetCurrentUser();
        if (user is null)
            return null;

        if (!user.IsExpired)
            return user;

        // RBAC checks happen before some request paths that would otherwise refresh
        // the token. Attempt refresh here so permissions reflect the current login.
        if (!McpHttpClient.TryRefreshCachedToken())
            return null;

        var refreshed = OidcAuthService.GetCurrentUser();
        return refreshed is { IsExpired: false } ? refreshed : null;
    }
}
