using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Navigation;

namespace McpServer.Director;

/// <summary>
/// In-memory tab registry used by Director to declaratively build tab navigation.
/// </summary>
internal sealed class DirectorTabRegistry : ITabRegistry
{
    private readonly List<TabRegistration> _registrations = [];

    /// <inheritdoc />
    public IReadOnlyList<TabRegistration> Registrations => _registrations;

    /// <inheritdoc />
    public void RegisterTab(
        McpArea area,
        string displayText,
        string requiredRole,
        Func<IServiceProvider, object> screenFactory)
    {
        RegisterTab(new TabRegistration(area, displayText, requiredRole, screenFactory));
    }

    /// <inheritdoc />
    public void RegisterTab(TabRegistration registration)
    {
        _registrations.Add(registration);
    }

    /// <inheritdoc />
    public void Clear()
    {
        _registrations.Clear();
    }
}
