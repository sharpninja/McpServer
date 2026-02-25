namespace McpServer.UI.Core.Authorization;

/// <summary>
/// Default permissive authorization policy used when a host shell does not register a custom policy.
/// </summary>
internal sealed class AllowAllAuthorizationPolicyService : IAuthorizationPolicyService
{
    /// <inheritdoc />
    public bool CanViewArea(McpArea area) => true;

    /// <inheritdoc />
    public bool CanExecuteAction(string actionKey) => true;

    /// <inheritdoc />
    public string? GetRequiredRole(McpArea area) => null;

    /// <inheritdoc />
    public string? GetRequiredRole(string actionKey) => null;
}
