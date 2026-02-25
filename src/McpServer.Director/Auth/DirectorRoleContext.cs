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
        return user is { IsExpired: false } ? user : null;
    }
}
