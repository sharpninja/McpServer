using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using McpServer.Support.Mcp.Options;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-077: Singleton registry that tracks federation targets and resolves which target
/// a request should be proxied to. State is seeded from <see cref="FederationOptions"/> at
/// construction; runtime mutations (via the management API) are in-memory only and do not
/// persist across restarts.
/// </summary>
public sealed class FederationRegistry
{
    private readonly ConcurrentDictionary<string, FederationTarget> _targets =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, string> _workspaceRoutes =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly FederationRole _configuredRole;
    private readonly string? _hubBaseUrl;
    private readonly string? _hubAccessToken;
    private readonly string? _proxyId;
    private readonly string? _enrollmentToken;
    private volatile bool _enabled;
    private volatile string? _defaultTarget;

    /// <summary>
    /// Initializes a new instance of the <see cref="FederationRegistry"/> class
    /// and seeds state from configuration.
    /// </summary>
    /// <param name="options">Federation configuration options.</param>
    public FederationRegistry(IOptions<FederationOptions> options)
    {
        var cfg = options.Value;
        _enabled = cfg.Enabled;
        _configuredRole = cfg.Role;
        _hubBaseUrl = string.IsNullOrWhiteSpace(cfg.HubBaseUrl) ? null : cfg.HubBaseUrl.TrimEnd('/');
        _hubAccessToken = string.IsNullOrWhiteSpace(cfg.HubAccessToken) ? null : cfg.HubAccessToken.Trim();
        _proxyId = string.IsNullOrWhiteSpace(cfg.ProxyId) ? Environment.MachineName : cfg.ProxyId.Trim();
        _enrollmentToken = string.IsNullOrWhiteSpace(cfg.EnrollmentToken) ? null : cfg.EnrollmentToken.Trim();
        _defaultTarget = string.IsNullOrWhiteSpace(cfg.DefaultTarget) ? null : cfg.DefaultTarget.Trim();

        foreach (var t in cfg.Targets)
        {
            if (!string.IsNullOrWhiteSpace(t.Name) && !string.IsNullOrWhiteSpace(t.BaseUrl))
                _targets[t.Name.Trim()] = new FederationTarget(t.Name.Trim(), t.BaseUrl.TrimEnd('/'), t.ApiKey);
        }

        foreach (var r in cfg.WorkspaceRoutes)
        {
            if (!string.IsNullOrWhiteSpace(r.WorkspacePath) && !string.IsNullOrWhiteSpace(r.TargetName))
                _workspaceRoutes[r.WorkspacePath] = r.TargetName.Trim();
        }
    }

    /// <summary>Whether federation is globally enabled.</summary>
    public bool IsEnabled => _enabled;

    /// <summary>Configured federation role before backward-compatibility inference.</summary>
    public FederationRole ConfiguredRole => _configuredRole;

    /// <summary>
    /// Effective federation role. Existing configuration that only sets
    /// <see cref="FederationOptions.Enabled"/> is interpreted as DirectProxy.
    /// </summary>
    public FederationRole EffectiveRole
    {
        get
        {
            if (_configuredRole == FederationRole.Standalone && _enabled)
                return FederationRole.DirectProxy;

            return _configuredRole;
        }
    }

    /// <summary>Hub base URL configured for LocalProxy mode, if any.</summary>
    public string? HubBaseUrl => _hubBaseUrl;

    /// <summary>Stable hub access token configured for inter-server hub traffic, if any.</summary>
    public string? HubAccessToken => _hubAccessToken;

    /// <summary>Whether a stable hub access token is configured.</summary>
    public bool HasHubAccessToken => _hubAccessToken is not null;

    /// <summary>Stable proxy identifier sent to the hub.</summary>
    public string? ProxyId => _proxyId;

    /// <summary>Whether an enrollment token is configured.</summary>
    public bool HasEnrollmentToken => _enrollmentToken is not null;

    /// <summary>
    /// Resolves the federation target for a request given the resolved workspace path.
    /// Returns <c>null</c> when federation is disabled or no matching target exists.
    /// </summary>
    /// <param name="workspacePath">The resolved workspace path (may be <c>null</c>).</param>
    /// <returns>Resolved <see cref="FederationTarget"/>, or <c>null</c>.</returns>
    public FederationTarget? ResolveTarget(string? workspacePath)
    {
        if (!_enabled)
            return null;

        if (EffectiveRole == FederationRole.LocalProxy)
        {
            return _hubBaseUrl is null
                ? null
                : new FederationTarget("hub", _hubBaseUrl, _hubAccessToken);
        }

        if (EffectiveRole != FederationRole.DirectProxy)
            return null;

        // 1. Workspace-specific route
        if (workspacePath is not null &&
            _workspaceRoutes.TryGetValue(workspacePath, out var routeTargetName) &&
            _targets.TryGetValue(routeTargetName, out var routeTarget))
        {
            return routeTarget;
        }

        // 2. Global default
        if (_defaultTarget is not null && _targets.TryGetValue(_defaultTarget, out var defaultTarget))
            return defaultTarget;

        return null;
    }

    /// <summary>Globally enables federation proxying.</summary>
    public void SetEnabled(bool enabled) => _enabled = enabled;

    /// <summary>Sets the default federation target by name. Pass <c>null</c> to clear.</summary>
    /// <param name="name">Target name, or <c>null</c> to clear the default.</param>
    /// <returns><c>true</c> if the named target exists (or <paramref name="name"/> is null).</returns>
    public bool SetDefaultTarget(string? name)
    {
        if (name is null)
        {
            _defaultTarget = null;
            return true;
        }

        if (!_targets.ContainsKey(name))
            return false;

        _defaultTarget = name;
        return true;
    }

    /// <summary>Adds or replaces a federation target at runtime.</summary>
    /// <param name="options">Target configuration.</param>
    /// <param name="error">Error message when the target cannot be added.</param>
    /// <returns><c>true</c> on success.</returns>
    public bool TryAddTarget(FederationTargetOptions options, [NotNullWhen(false)] out string? error)
    {
        if (string.IsNullOrWhiteSpace(options.Name))
        {
            error = "Target name must not be empty.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            error = "Target BaseUrl must not be empty.";
            return false;
        }

        var name = options.Name.Trim();
        if (_targets.ContainsKey(name))
        {
            error = $"A target named '{name}' already exists.";
            return false;
        }

        _targets[name] = new FederationTarget(name, options.BaseUrl.TrimEnd('/'), options.ApiKey);
        error = null;
        return true;
    }

    /// <summary>Removes a federation target by name.</summary>
    /// <returns><c>true</c> if the target was found and removed.</returns>
    public bool TryRemoveTarget(string name)
    {
        if (!_targets.TryRemove(name, out _))
            return false;

        // Clear default if it pointed to the removed target
        if (string.Equals(_defaultTarget, name, StringComparison.OrdinalIgnoreCase))
            _defaultTarget = null;

        return true;
    }

    /// <summary>Adds or updates a workspace routing rule.</summary>
    /// <param name="workspacePath">Absolute workspace path.</param>
    /// <param name="targetName">Target name to route to.</param>
    /// <returns><c>true</c> if the named target exists.</returns>
    public bool SetWorkspaceRoute(string workspacePath, string targetName)
    {
        if (!_targets.ContainsKey(targetName))
            return false;

        _workspaceRoutes[workspacePath] = targetName;
        return true;
    }

    /// <summary>Removes a workspace routing rule.</summary>
    /// <returns><c>true</c> if the rule existed and was removed.</returns>
    public bool RemoveWorkspaceRoute(string workspacePath)
        => _workspaceRoutes.TryRemove(workspacePath, out _);

    /// <summary>Returns a snapshot of all registered federation targets.</summary>
    public IReadOnlyList<FederationTargetInfo> List()
    {
        return _targets.Values
            .Select(t => new FederationTargetInfo(
                t.Name,
                t.BaseUrl,
                t.ApiKey is not null,
                string.Equals(t.Name, _defaultTarget, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    /// <summary>Returns a snapshot of all workspace routing rules.</summary>
    public IReadOnlyList<WorkspaceRouteInfo> ListRoutes()
    {
        return _workspaceRoutes
            .Select(kv => new WorkspaceRouteInfo(kv.Key, kv.Value))
            .ToList();
    }
}

/// <summary>FR-MCP-077: A resolved federation target used internally by the proxy middleware.</summary>
/// <param name="Name">Target name.</param>
/// <param name="BaseUrl">Base URL of the remote server (no trailing slash).</param>
/// <param name="ApiKey">Optional API key to send to the remote server.</param>
public sealed record FederationTarget(string Name, string BaseUrl, string? ApiKey);

/// <summary>FR-MCP-077: Federation target snapshot for the management API.</summary>
/// <param name="Name">Target name.</param>
/// <param name="BaseUrl">Base URL of the remote server.</param>
/// <param name="HasApiKey">Whether a target-specific API key is configured.</param>
/// <param name="IsDefault">Whether this target is the current default.</param>
public sealed record FederationTargetInfo(string Name, string BaseUrl, bool HasApiKey, bool IsDefault);

/// <summary>FR-MCP-077: Workspace routing rule snapshot for the management API.</summary>
/// <param name="WorkspacePath">Absolute workspace path.</param>
/// <param name="TargetName">Name of the target this workspace routes to.</param>
public sealed record WorkspaceRouteInfo(string WorkspacePath, string TargetName);
