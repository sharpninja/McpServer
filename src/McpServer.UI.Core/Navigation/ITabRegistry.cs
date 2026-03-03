using McpServer.UI.Core.Authorization;

namespace McpServer.UI.Core.Navigation;

/// <summary>
/// Declarative tab registration metadata used by host UIs to build area navigation.
/// </summary>
/// <param name="Area">Logical MCP area represented by the tab.</param>
/// <param name="DisplayText">Tab caption.</param>
/// <param name="RequiredRole">Role required for the tab (metadata/display use).</param>
/// <param name="ScreenFactory">Factory that creates the tab view/control instance.</param>
/// <param name="AvailabilityPredicate">Optional host-specific availability check.</param>
public sealed record TabRegistration(
    McpArea Area,
    string DisplayText,
    string RequiredRole,
    Func<IServiceProvider, object> ScreenFactory,
    Func<IServiceProvider, bool>? AvailabilityPredicate = null);

/// <summary>
/// Contract for registering and enumerating UI tab metadata.
/// </summary>
public interface ITabRegistry
{
    /// <summary>Registered tab metadata in display order.</summary>
    IReadOnlyList<TabRegistration> Registrations { get; }

    /// <summary>
    /// Registers a new tab using default availability semantics.
    /// </summary>
    /// <param name="area">Logical area.</param>
    /// <param name="displayText">Display caption.</param>
    /// <param name="requiredRole">Required role metadata.</param>
    /// <param name="screenFactory">Factory that creates the tab view/control.</param>
    void RegisterTab(
        McpArea area,
        string displayText,
        string requiredRole,
        Func<IServiceProvider, object> screenFactory);

    /// <summary>
    /// Registers a complete <see cref="TabRegistration"/>.
    /// </summary>
    /// <param name="registration">Tab registration metadata.</param>
    void RegisterTab(TabRegistration registration);

    /// <summary>Clears all registered tabs.</summary>
    void Clear();
}
