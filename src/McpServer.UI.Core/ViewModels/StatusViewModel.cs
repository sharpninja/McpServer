using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace McpServer.UI.Core.ViewModels;

/// <summary>
/// Global status sink shared across the entire application.
/// <para>
/// <see cref="AddStatus"/> appends to the observable history and raises
/// <see cref="ObservableObject.PropertyChanged"/> for <see cref="Status"/>
/// (the latest entry).
/// </para>
/// <para>
/// The Copilot heartbeat properties (<see cref="IsCopilotRunning"/>,
/// <see cref="CopilotActivityText"/>, <see cref="CopilotHeartbeatState"/>)
/// are updated by the host ViewModel that drives Copilot prompt generation.
/// Views bind to these properties to show a live activity indicator.
/// </para>
/// </summary>
public sealed partial class StatusViewModel : ObservableObject
{
    private static readonly Lazy<StatusViewModel> LazyInstance = new(() => new StatusViewModel());

    /// <summary>Singleton instance shared across the app.</summary>
    public static StatusViewModel Instance => LazyInstance.Value;

    private StatusViewModel() { }

    /// <summary>Full history of status messages.</summary>
    public ObservableCollection<string> Statuses { get; } = [];

    /// <summary>The most recent status message, or empty string if none.</summary>
    public string Status => Statuses.Count > 0 ? Statuses[^1] : string.Empty;

    /// <summary>Whether a Copilot prompt operation is currently running.</summary>
    [ObservableProperty]
    private bool _isCopilotRunning;

    /// <summary>
    /// Human-readable activity description while Copilot is running
    /// (e.g., "Copilot is thinking…", "No heartbeat for 30s — Copilot may have stalled").
    /// </summary>
    [ObservableProperty]
    private string? _copilotActivityText;

    /// <summary>
    /// Current heartbeat state for color-coded UI indicator.
    /// Values: "none", "connecting", "active", "receiving", "warning", "stalled".
    /// </summary>
    [ObservableProperty]
    private string _copilotHeartbeatState = "none";

    /// <summary>Appends a status message and notifies bindings.</summary>
    /// <param name="message">Status text to append.</param>
    public void AddStatus(string message)
    {
        Statuses.Add(message);
        OnPropertyChanged(nameof(Status));
    }

    /// <summary>Resets Copilot indicator state to idle.</summary>
    public void ClearCopilotState()
    {
        IsCopilotRunning = false;
        CopilotActivityText = null;
        CopilotHeartbeatState = "none";
    }
}
