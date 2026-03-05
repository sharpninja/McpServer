namespace McpServer.UI.Core.Authorization;

/// <summary>
/// Provides the current user's authentication and role information for UI authorization checks.
/// Implemented by the host shell (e.g. Director) and consumed by UI.Core.
/// </summary>
public interface IRoleContext
{
    /// <summary>Whether a user is currently authenticated.</summary>
    bool IsAuthenticated { get; }

    /// <summary>Current role names.</summary>
    IReadOnlyList<string> Roles { get; }

    /// <summary>
    /// Returns <c>true</c> if the current user has the specified role.
    /// Implementations should use case-insensitive comparison.
    /// </summary>
    /// <param name="role">Role name to check.</param>
    /// <returns><c>true</c> if present; otherwise <c>false</c>.</returns>
    bool HasRole(string role);
}
