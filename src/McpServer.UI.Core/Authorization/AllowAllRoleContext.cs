namespace McpServer.UI.Core.Authorization;

/// <summary>
/// Default permissive role context used when a host shell does not provide one.
/// This preserves backward compatibility for non-RBAC-aware hosts.
/// </summary>
internal sealed class AllowAllRoleContext : IRoleContext
{
    /// <inheritdoc />
    public bool IsAuthenticated => false;

    /// <inheritdoc />
    public IReadOnlyList<string> Roles { get; } = [];

    /// <inheritdoc />
    public bool HasRole(string role) => true;
}
