using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using Microsoft.Extensions.Logging;
using McpServer.UI.Core.Messages;

namespace McpServer.UI.Core.ViewModels;

/// <summary>
/// ViewModel for loading, editing, saving, and resetting the shared workspace global prompt.
/// </summary>
[ViewModelCommand("workspace-global-prompt", Description = "Load, save, and reset the shared workspace global prompt")]
public sealed partial class WorkspaceGlobalPromptViewModel : ObservableObject
{
    private readonly CqrsQueryCommand<WorkspaceGlobalPromptState> _loadCommand;
    private readonly CqrsRelayCommand<WorkspaceGlobalPromptState> _saveCommand;
    private readonly AsyncRelayCommand _resetCommand;
    private readonly ILogger<WorkspaceGlobalPromptViewModel> _logger;

    /// <summary>Initializes a new instance of the workspace global-prompt ViewModel.</summary>
    public WorkspaceGlobalPromptViewModel(Dispatcher dispatcher, ILogger<WorkspaceGlobalPromptViewModel> logger)
    {
        _logger = logger;
        _loadCommand = new CqrsQueryCommand<WorkspaceGlobalPromptState>(dispatcher, static () => new GetWorkspaceGlobalPromptQuery());
        _saveCommand = new CqrsRelayCommand<WorkspaceGlobalPromptState>(dispatcher, () => new UpdateWorkspaceGlobalPromptCommand(TemplateText));
        _resetCommand = new AsyncRelayCommand(ResetAsync);
    }

    /// <summary>Current editor text for the global prompt template.</summary>
    [ObservableProperty]
    private string _templateText = string.Empty;

    /// <summary>Whether the built-in default prompt is active.</summary>
    [ObservableProperty]
    private bool _isDefault;

    /// <summary>Whether a load/save operation is in progress.</summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>Last error message.</summary>
    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>Last status message.</summary>
    [ObservableProperty]
    private string? _statusMessage;

    /// <summary>Timestamp of the last successful load/save operation.</summary>
    [ObservableProperty]
    private DateTimeOffset? _lastUpdatedAt;

    /// <summary>Command that loads the shared workspace global prompt.</summary>
    public IAsyncRelayCommand LoadCommand => _loadCommand;

    /// <summary>Command that saves the current editor text as the shared workspace global prompt.</summary>
    public IAsyncRelayCommand SaveCommand => _saveCommand;

    /// <summary>Command that resets the shared workspace global prompt to the built-in default.</summary>
    public IAsyncRelayCommand ResetCommand => _resetCommand;

    /// <summary>Primary command alias for ViewModel registry execution.</summary>
    public IAsyncRelayCommand PrimaryCommand => LoadCommand;

    /// <summary>Last load result.</summary>
    public Result<WorkspaceGlobalPromptState>? LastResult => _loadCommand.LastResult;

    /// <summary>Last save result.</summary>
    public Result<WorkspaceGlobalPromptState>? LastSaveResult => _saveCommand.LastResult;

    /// <summary>Loads the shared workspace global prompt into the editor.</summary>
    /// <param name="ct">Cancellation token.</param>
    public async Task LoadAsync(CancellationToken ct = default)
    {
        IsBusy = true;
        ErrorMessage = null;
        StatusMessage = "Loading workspace global prompt...";

        try
        {
            var result = await _loadCommand.DispatchAsync(ct).ConfigureAwait(true);
            if (!result.IsSuccess || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Unknown error loading workspace global prompt.";
                StatusMessage = "Workspace global prompt load failed.";
                return;
            }

            TemplateText = result.Value.Template;
            IsDefault = result.Value.IsDefault;
            LastUpdatedAt = DateTimeOffset.UtcNow;
            StatusMessage = result.Value.IsDefault
                ? "Loaded workspace global prompt (built-in default)."
                : "Loaded workspace global prompt.";
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Workspace global prompt load failed.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Saves the current editor text as the shared workspace global prompt.</summary>
    /// <param name="ct">Cancellation token.</param>
    public async Task SaveAsync(CancellationToken ct = default)
    {
        IsBusy = true;
        ErrorMessage = null;
        StatusMessage = "Saving workspace global prompt...";

        try
        {
            var result = await _saveCommand.DispatchAsync(ct).ConfigureAwait(true);
            if (!result.IsSuccess || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Unknown error saving workspace global prompt.";
                StatusMessage = "Workspace global prompt save failed.";
                return;
            }

            TemplateText = result.Value.Template;
            IsDefault = result.Value.IsDefault;
            LastUpdatedAt = DateTimeOffset.UtcNow;
            StatusMessage = result.Value.IsDefault
                ? "Saved workspace global prompt (built-in default)."
                : "Saved workspace global prompt.";
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Workspace global prompt save failed.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Resets the shared workspace global prompt to the built-in default template.</summary>
    public async Task ResetAsync()
    {
        TemplateText = string.Empty;
        await SaveAsync().ConfigureAwait(true);
    }
}
