using CommunityToolkit.Mvvm.ComponentModel;

namespace McpServer.UI.Core.ViewModels.Base;

/// <summary>
/// Shared operation-state model for async UI workflows (load/save/run/etc.).
/// Provides observable busy/error/status tracking with timestamp history.
/// <para>
/// This class is not yet referenced by existing ViewModels but is provided as
/// a reusable building block for future async operations that need more granular
/// state tracking than the base ViewModel classes offer (e.g., multi-step wizards,
/// background sync indicators).
/// </para>
/// </summary>
public partial class OperationState : ObservableObject
{
    /// <summary>Whether an operation is currently running.</summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>Human-readable error message from the last failed operation.</summary>
    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>Optional status message for the UI.</summary>
    [ObservableProperty]
    private string? _statusMessage;

    /// <summary>Timestamp of the last successful operation.</summary>
    [ObservableProperty]
    private DateTimeOffset? _lastSucceededAt;

    /// <summary>Timestamp of the last failed operation.</summary>
    [ObservableProperty]
    private DateTimeOffset? _lastFailedAt;

    /// <summary>Marks the operation as running and clears error state.</summary>
    /// <param name="statusMessage">Optional status text.</param>
    public void Begin(string? statusMessage = null)
    {
        IsBusy = true;
        ErrorMessage = null;
        if (!string.IsNullOrWhiteSpace(statusMessage))
            StatusMessage = statusMessage;
    }

    /// <summary>Marks the operation as completed successfully.</summary>
    /// <param name="statusMessage">Optional status text.</param>
    public void Succeed(string? statusMessage = null)
    {
        IsBusy = false;
        ErrorMessage = null;
        LastSucceededAt = DateTimeOffset.UtcNow;
        if (!string.IsNullOrWhiteSpace(statusMessage))
            StatusMessage = statusMessage;
    }

    /// <summary>Marks the operation as failed.</summary>
    /// <param name="errorMessage">Error text to surface to the UI.</param>
    public void Fail(string? errorMessage)
    {
        IsBusy = false;
        ErrorMessage = string.IsNullOrWhiteSpace(errorMessage) ? "Unknown error." : errorMessage;
        LastFailedAt = DateTimeOffset.UtcNow;
    }
}
