using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.ViewModels.Base;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.ViewModels;

/// <summary>
/// ViewModel for loading, creating, editing, and deleting workspace registrations.
/// </summary>
[ViewModelCommand("get-workspace", Description = "Get, create, edit, and delete workspace details")]
public sealed partial class WorkspaceDetailViewModel : AreaDetailViewModelBase<WorkspaceDetail>
{
    private readonly CqrsQueryCommand<WorkspaceDetail?> _loadCommand;
    private readonly CqrsRelayCommand<WorkspaceMutationOutcome> _createCommand;
    private readonly CqrsRelayCommand<WorkspaceMutationOutcome> _updateCommand;
    private readonly CqrsRelayCommand<WorkspaceMutationOutcome> _deleteCommand;
    private readonly ILogger<WorkspaceDetailViewModel> _logger;

    /// <summary>Initializes a new instance of the workspace detail ViewModel.</summary>
    public WorkspaceDetailViewModel(Dispatcher dispatcher,
        WorkspaceContextViewModel workspaceContext,
        ILogger<WorkspaceDetailViewModel> logger)
        : base(McpArea.Workspaces)
    {
        _logger = logger;
        _loadCommand = new CqrsQueryCommand<WorkspaceDetail?>(dispatcher, () => new GetWorkspaceQuery(WorkspacePath));
        _createCommand = new CqrsRelayCommand<WorkspaceMutationOutcome>(dispatcher, BuildCreateCommand);
        _updateCommand = new CqrsRelayCommand<WorkspaceMutationOutcome>(dispatcher, BuildUpdateCommand);
        _deleteCommand = new CqrsRelayCommand<WorkspaceMutationOutcome>(dispatcher, BuildDeleteCommand);
        workspaceContext.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(WorkspaceContextViewModel.ActiveWorkspacePath))
            {
                var activePath = workspaceContext.ActiveWorkspacePath ?? string.Empty;
                _logger.LogInformation("Workspace changed to '{WorkspacePath}' — synchronizing workspace detail editor", activePath);
                if (string.IsNullOrWhiteSpace(activePath))
                {
                    BeginNewDraft();
                    return;
                }

                WorkspacePath = activePath;
                _ = Task.Run(() => LoadAsync());
            }
        };
    }

    /// <summary>Workspace path to load.</summary>
    [ObservableProperty]
    private string _workspacePath = string.Empty;

    /// <summary>Editor workspace path.</summary>
    [ObservableProperty]
    private string _editorWorkspacePath = string.Empty;

    /// <summary>Editor name.</summary>
    [ObservableProperty]
    private string _editorName = string.Empty;

    /// <summary>Editor TODO path.</summary>
    [ObservableProperty]
    private string _editorTodoPath = string.Empty;

    /// <summary>Editor data directory.</summary>
    [ObservableProperty]
    private string _editorDataDirectory = string.Empty;

    /// <summary>Editor tunnel provider.</summary>
    [ObservableProperty]
    private string _editorTunnelProvider = string.Empty;

    /// <summary>Editor Windows identity.</summary>
    [ObservableProperty]
    private string _editorRunAs = string.Empty;

    /// <summary>Editor primary flag.</summary>
    [ObservableProperty]
    private bool _editorIsPrimary;

    /// <summary>Editor enabled flag.</summary>
    [ObservableProperty]
    private bool _editorIsEnabled = true;

    /// <summary>Editor workspace prompt template.</summary>
    [ObservableProperty]
    private string _editorPromptTemplateText = string.Empty;

    /// <summary>Editor status prompt override.</summary>
    [ObservableProperty]
    private string _editorStatusPromptText = string.Empty;

    /// <summary>Editor implement prompt override.</summary>
    [ObservableProperty]
    private string _editorImplementPromptText = string.Empty;

    /// <summary>Editor plan prompt override.</summary>
    [ObservableProperty]
    private string _editorPlanPromptText = string.Empty;

    /// <summary>Whether the editor currently represents a new draft.</summary>
    [ObservableProperty]
    private bool _isNewDraft = true;

    /// <summary>Last create/update/delete status message.</summary>
    [ObservableProperty]
    private string? _mutationMessage;

    /// <summary>Load command for the current <see cref="WorkspacePath"/>.</summary>
    public IAsyncRelayCommand LoadCommand => _loadCommand;

    /// <summary>Create command using current editor fields.</summary>
    public IAsyncRelayCommand CreateCommand => _createCommand;

    /// <summary>Save command using current editor fields.</summary>
    public IAsyncRelayCommand SaveCommand => _updateCommand;

    /// <summary>Delete command for the currently loaded workspace.</summary>
    public IAsyncRelayCommand DeleteCommand => _deleteCommand;

    /// <summary>Primary command alias for ViewModel registry execution.</summary>
    public IAsyncRelayCommand PrimaryCommand => LoadCommand;

    /// <summary>Last load result.</summary>
    public Result<WorkspaceDetail?>? LastResult => _loadCommand.LastResult;

    /// <summary>Last create result.</summary>
    public Result<WorkspaceMutationOutcome>? LastCreateResult => _createCommand.LastResult;

    /// <summary>Last update result.</summary>
    public Result<WorkspaceMutationOutcome>? LastUpdateResult => _updateCommand.LastResult;

    /// <summary>Last delete result.</summary>
    public Result<WorkspaceMutationOutcome>? LastDeleteResult => _deleteCommand.LastResult;

    /// <summary>Clears the editor and begins a new workspace draft.</summary>
    /// <param name="defaultWorkspacePath">Optional default workspace path.</param>
    public void BeginNewDraft(string? defaultWorkspacePath = null)
    {
        Detail = null;
        IsNewDraft = true;
        MutationMessage = null;
        ErrorMessage = null;
        WorkspacePath = string.Empty;
        EditorWorkspacePath = Normalize(defaultWorkspacePath) ?? string.Empty;
        EditorName = string.Empty;
        EditorTodoPath = string.Empty;
        EditorDataDirectory = string.Empty;
        EditorTunnelProvider = string.Empty;
        EditorRunAs = string.Empty;
        EditorIsPrimary = false;
        EditorIsEnabled = true;
        EditorPromptTemplateText = string.Empty;
        EditorStatusPromptText = string.Empty;
        EditorImplementPromptText = string.Empty;
        EditorPlanPromptText = string.Empty;
        IsDirty = true;
        StatusMessage = "New workspace draft.";
    }

    /// <summary>Loads the workspace detail.</summary>
    public async Task LoadAsync(CancellationToken ct = default)
    {
        IsBusy = true;
        ErrorMessage = null;
        MutationMessage = null;
        StatusMessage = "Loading workspace details...";

        try
        {
            if (string.IsNullOrWhiteSpace(WorkspacePath))
            {
                Detail = null;
                ErrorMessage = "WorkspacePath is required.";
                StatusMessage = "Workspace detail load failed.";
                return;
            }

            var result = await _loadCommand.DispatchAsync(ct).ConfigureAwait(true);
            if (!result.IsSuccess)
            {
                Detail = null;
                ErrorMessage = result.Error ?? "Unknown error loading workspace details.";
                StatusMessage = "Workspace detail load failed.";
                return;
            }

            Detail = result.Value;
            if (result.Value is not null)
            {
                ApplyDetailToEditor(result.Value);
                IsNewDraft = false;
                IsDirty = false;
            }

            LastUpdatedAt = DateTimeOffset.UtcNow;
            StatusMessage = result.Value is null
                ? "Workspace not found."
                : $"Loaded workspace '{result.Value.Name}'.";
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            Detail = null;
            ErrorMessage = ex.Message;
            StatusMessage = "Workspace detail load failed.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Creates a workspace registration from the current editor state.</summary>
    public async Task CreateAsync(CancellationToken ct = default)
    {
        await RunMutationAsync(_createCommand, "Creating workspace...", "Workspace created.", ct, updateWorkspacePathFromItem: true)
            .ConfigureAwait(true);
    }

    /// <summary>Saves the current workspace editor state.</summary>
    public async Task SaveAsync(CancellationToken ct = default)
    {
        if (IsNewDraft)
        {
            await CreateAsync(ct).ConfigureAwait(true);
            return;
        }

        await RunMutationAsync(_updateCommand, "Saving workspace...", "Workspace saved.", ct).ConfigureAwait(true);
    }

    /// <summary>Deletes the current workspace registration.</summary>
    public async Task DeleteAsync(CancellationToken ct = default)
    {
        if (IsNewDraft)
            return;

        await RunMutationAsync(_deleteCommand, "Deleting workspace...", "Workspace deleted.", ct, clearOnDelete: true)
            .ConfigureAwait(true);
    }

    private async Task RunMutationAsync(
        CqrsRelayCommand<WorkspaceMutationOutcome> command,
        string busyMessage,
        string successMessage,
        CancellationToken ct,
        bool clearOnDelete = false,
        bool updateWorkspacePathFromItem = false)
    {
        IsBusy = true;
        ErrorMessage = null;
        MutationMessage = null;
        StatusMessage = busyMessage;

        try
        {
            var result = await command.DispatchAsync(ct).ConfigureAwait(true);
            if (!result.IsSuccess || result.Value is null || !result.Value.Success)
            {
                ErrorMessage = result.Error ?? result.Value?.Error ?? "Workspace mutation failed.";
                MutationMessage = ErrorMessage;
                StatusMessage = busyMessage.Replace("...", " failed.");
                return;
            }

            if (clearOnDelete)
            {
                var deletedPath = WorkspacePath;
                BeginNewDraft();
                MutationMessage = $"Deleted '{deletedPath}'.";
                StatusMessage = successMessage;
                return;
            }

            Detail = result.Value.Item;
            if (Detail is not null)
            {
                ApplyDetailToEditor(Detail);
                if (updateWorkspacePathFromItem)
                    WorkspacePath = Detail.WorkspacePath;
            }

            IsNewDraft = false;
            IsDirty = false;
            LastUpdatedAt = DateTimeOffset.UtcNow;
            MutationMessage = successMessage;
            StatusMessage = successMessage;
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            MutationMessage = ex.Message;
            StatusMessage = busyMessage.Replace("...", " failed.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyDetailToEditor(WorkspaceDetail detail)
    {
        WorkspacePath = detail.WorkspacePath;
        EditorWorkspacePath = detail.WorkspacePath;
        EditorName = detail.Name;
        EditorTodoPath = detail.TodoPath;
        EditorDataDirectory = detail.DataDirectory ?? string.Empty;
        EditorTunnelProvider = detail.TunnelProvider ?? string.Empty;
        EditorRunAs = detail.RunAs ?? string.Empty;
        EditorIsPrimary = detail.IsPrimary;
        EditorIsEnabled = detail.IsEnabled;
        EditorPromptTemplateText = detail.PromptTemplate ?? string.Empty;
        EditorStatusPromptText = detail.StatusPrompt;
        EditorImplementPromptText = detail.ImplementPrompt;
        EditorPlanPromptText = detail.PlanPrompt;
    }

    private CreateWorkspaceCommand BuildCreateCommand()
        => new()
        {
            WorkspacePath = EditorWorkspacePath.Trim(),
            Name = Normalize(EditorName),
            TodoPath = Normalize(EditorTodoPath),
            DataDirectory = Normalize(EditorDataDirectory),
            TunnelProvider = Normalize(EditorTunnelProvider),
            RunAs = Normalize(EditorRunAs),
            IsPrimary = EditorIsPrimary,
            IsEnabled = EditorIsEnabled,
            PromptTemplate = NormalizePromptOverride(EditorPromptTemplateText),
            StatusPrompt = NormalizePromptOverride(EditorStatusPromptText),
            ImplementPrompt = NormalizePromptOverride(EditorImplementPromptText),
            PlanPrompt = NormalizePromptOverride(EditorPlanPromptText),
        };

    private UpdateWorkspaceCommand BuildUpdateCommand()
        => new()
        {
            WorkspacePath = WorkspacePath,
            Name = Normalize(EditorName),
            TodoPath = Normalize(EditorTodoPath),
            DataDirectory = Normalize(EditorDataDirectory),
            TunnelProvider = Normalize(EditorTunnelProvider),
            RunAs = Normalize(EditorRunAs),
            IsPrimary = EditorIsPrimary,
            IsEnabled = EditorIsEnabled,
            PromptTemplate = NormalizePromptOverride(EditorPromptTemplateText),
            StatusPrompt = NormalizePromptOverride(EditorStatusPromptText),
            ImplementPrompt = NormalizePromptOverride(EditorImplementPromptText),
            PlanPrompt = NormalizePromptOverride(EditorPlanPromptText),
        };

    private DeleteWorkspaceCommand BuildDeleteCommand()
        => new(WorkspacePath);

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizePromptOverride(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;
}
