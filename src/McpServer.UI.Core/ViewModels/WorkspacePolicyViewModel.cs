using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Messages;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.ViewModels;

/// <summary>
/// TR-MCP-DIR-003: ViewModel for editing workspace compliance policy (ban lists).
/// Dispatches <see cref="UpdateWorkspacePolicyCommand"/> through the CQRS Dispatcher.
/// </summary>
[ViewModelCommand("update-policy", Description = "Update workspace compliance policy (ban lists)")]
public partial class WorkspacePolicyViewModel : ObservableObject
{
    private readonly Dispatcher _dispatcher;
    private readonly WorkspaceContextViewModel _workspaceContext;
    private readonly ILogger<WorkspacePolicyViewModel> _logger;


    /// <summary>Initializes a new <see cref="WorkspacePolicyViewModel"/>.</summary>
    /// <param name="dispatcher">The CQRS dispatcher.</param>
    /// <param name="workspaceContext">Shared workspace context for reacting to workspace changes.</param>
    /// <param name="logger">Logger instance.</param>
    public WorkspacePolicyViewModel(Dispatcher dispatcher,
        WorkspaceContextViewModel workspaceContext,
        ILogger<WorkspacePolicyViewModel> logger)
    {
        _logger = logger;
        _dispatcher = dispatcher;
        _workspaceContext = workspaceContext;
        SaveCommand = new CqrsRelayCommand<bool>(dispatcher, BuildCommand);
        workspaceContext.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(WorkspaceContextViewModel.ActiveWorkspacePath))
                OnPropertyChanged(nameof(WorkspacePath));
        };
    }

    /// <summary>The workspace path — delegates to <see cref="WorkspaceContextViewModel"/>.</summary>
    public string WorkspacePath => _workspaceContext.ActiveWorkspacePath ?? "";

    /// <summary>Banned licenses.</summary>
    public ObservableCollection<string> BannedLicenses { get; } = [];

    /// <summary>Banned countries of origin.</summary>
    public ObservableCollection<string> BannedCountriesOfOrigin { get; } = [];

    /// <summary>Banned organizations.</summary>
    public ObservableCollection<string> BannedOrganizations { get; } = [];

    /// <summary>Banned individuals.</summary>
    public ObservableCollection<string> BannedIndividuals { get; } = [];

    /// <summary>Whether a save is in progress.</summary>
    [ObservableProperty]
    private bool _isSaving;

    /// <summary>Error message from the last save attempt.</summary>
    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>Whether the last save succeeded.</summary>
    [ObservableProperty]
    private bool _saveSucceeded;

    /// <summary>The save command.</summary>
    public CqrsRelayCommand<bool> SaveCommand { get; }

    /// <summary>Alias for <see cref="SaveCommand"/> for <see cref="IViewModelRegistry"/> discovery.</summary>
    public CqrsRelayCommand<bool> PrimaryCommand => SaveCommand;

    /// <summary>The result from the last save.</summary>
    public Result<bool>? LastResult => SaveCommand.LastResult;

    /// <summary>Saves the policy by dispatching the command.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task SaveAsync(CancellationToken ct = default)
    {
        IsSaving = true;
        ErrorMessage = null;
        SaveSucceeded = false;
        try
        {
            var result = await SaveCommand.DispatchAsync(ct).ConfigureAwait(true);
            if (result.IsSuccess)
            {
                SaveSucceeded = true;
            }
            else
            {
                ErrorMessage = result.Error ?? "Unknown error saving policy.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsSaving = false;
        }
    }

    private UpdateWorkspacePolicyCommand BuildCommand() => new()
    {
        WorkspacePath = WorkspacePath,
        BannedLicenses = [.. BannedLicenses],
        BannedCountriesOfOrigin = [.. BannedCountriesOfOrigin],
        BannedOrganizations = [.. BannedOrganizations],
        BannedIndividuals = [.. BannedIndividuals],
    };
}
