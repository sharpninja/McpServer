namespace McpServer.UI.Core.Authorization;

/// <summary>
/// Standard role names used by Director/UI RBAC checks.
/// </summary>
public static class McpRoles
{
    /// <summary>Read-only/view-level access.</summary>
    public const string Viewer = "viewer";

    /// <summary>Agent management access.</summary>
    public const string AgentManager = "agent-manager";

    /// <summary>Administrative access.</summary>
    public const string Admin = "admin";

    /// <summary>
    /// Normalizes a role name for comparisons.
    /// </summary>
    /// <param name="role">Role name.</param>
    /// <returns>Trimmed lower-case role name, or empty string.</returns>
    public static string Normalize(string? role)
        => string.IsNullOrWhiteSpace(role) ? string.Empty : role.Trim().ToLowerInvariant();
}
