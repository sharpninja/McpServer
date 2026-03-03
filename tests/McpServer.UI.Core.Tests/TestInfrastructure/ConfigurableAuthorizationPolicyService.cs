using McpServer.UI.Core.Authorization;

namespace McpServer.UI.Core.Tests.TestInfrastructure;

/// <summary>
/// Test authorization policy that can allow/deny specific areas and actions.
/// </summary>
internal sealed class ConfigurableAuthorizationPolicyService : IAuthorizationPolicyService
{
    private readonly bool _defaultAllow;
    private readonly Dictionary<McpArea, bool> _areaAllow = [];
    private readonly Dictionary<string, bool> _actionAllow = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<McpArea, string?> _areaRoles = [];
    private readonly Dictionary<string, string?> _actionRoles = new(StringComparer.OrdinalIgnoreCase);

    public ConfigurableAuthorizationPolicyService(bool defaultAllow = true)
    {
        _defaultAllow = defaultAllow;
    }

    public ConfigurableAuthorizationPolicyService SetArea(McpArea area, bool allowed, string? requiredRole = null)
    {
        _areaAllow[area] = allowed;
        _areaRoles[area] = requiredRole;
        return this;
    }

    public ConfigurableAuthorizationPolicyService SetAction(string actionKey, bool allowed, string? requiredRole = null)
    {
        _actionAllow[actionKey] = allowed;
        _actionRoles[actionKey] = requiredRole;
        return this;
    }

    public bool CanViewArea(McpArea area)
        => _areaAllow.TryGetValue(area, out var allowed) ? allowed : _defaultAllow;

    public bool CanExecuteAction(string actionKey)
        => _actionAllow.TryGetValue(actionKey, out var allowed) ? allowed : _defaultAllow;

    public string? GetRequiredRole(McpArea area)
        => _areaRoles.TryGetValue(area, out var role) ? role : McpRoles.Viewer;

    public string? GetRequiredRole(string actionKey)
        => _actionRoles.TryGetValue(actionKey, out var role) ? role : McpRoles.Viewer;
}
