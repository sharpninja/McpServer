namespace McpServer.UI.Core.Authorization;

/// <summary>
/// Evaluates UI authorization rules for areas and actions using the current role context.
/// </summary>
public interface IAuthorizationPolicyService
{
    /// <summary>
    /// Returns whether the current user can view a given UI area/tab.
    /// </summary>
    /// <param name="area">Area being evaluated.</param>
    /// <returns><c>true</c> if visible; otherwise <c>false</c>.</returns>
    bool CanViewArea(McpArea area);

    /// <summary>
    /// Returns whether the current user can execute a named action.
    /// </summary>
    /// <param name="actionKey">Action key (for example, <c>workspace.update-policy</c>).</param>
    /// <returns><c>true</c> if allowed; otherwise <c>false</c>.</returns>
    bool CanExecuteAction(string actionKey);

    /// <summary>
    /// Gets the minimum role required to view the specified area, or <c>null</c> if unrestricted.
    /// </summary>
    /// <param name="area">Area to inspect.</param>
    /// <returns>Required role name, or <c>null</c>.</returns>
    string? GetRequiredRole(McpArea area);

    /// <summary>
    /// Gets the minimum role required to execute a named action, or <c>null</c> if unrestricted.
    /// </summary>
    /// <param name="actionKey">Action key to inspect.</param>
    /// <returns>Required role name, or <c>null</c>.</returns>
    string? GetRequiredRole(string actionKey);
}
