using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.ViewModels;

/// <summary>
/// ViewModel for GitHub/TODO synchronization and label refresh operations.
/// </summary>
[ViewModelCommand("github-sync", Description = "Run GitHub sync workflows")]
public sealed partial class GitHubSyncViewModel : ObservableObject
{
    private readonly Dispatcher _dispatcher;
    private readonly ILogger<GitHubSyncViewModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GitHubSyncViewModel"/> class.
    /// </summary>
    /// <param name="dispatcher">CQRS dispatcher.</param>
    /// <param name="logger">Logger instance.</param>
    public GitHubSyncViewModel(
        Dispatcher dispatcher,
        ILogger<GitHubSyncViewModel> logger)
    {
        _dispatcher = dispatcher;
        _logger = logger;
        StatusMessage = "Idle.";
    }

    /// <summary>Logical area represented by this ViewModel.</summary>
    public McpArea Area => McpArea.GitHub;

    /// <summary>Whether a sync/label operation is running.</summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>Latest error text.</summary>
    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>Latest status text.</summary>
    [ObservableProperty]
    private string? _statusMessage;

    /// <summary>Most recent bulk sync result.</summary>
    [ObservableProperty]
    private GitHubSyncOutcome? _lastSyncOutcome;

    /// <summary>Most recent single-issue sync result.</summary>
    [ObservableProperty]
    private GitHubSingleIssueSyncOutcome? _lastSingleIssueSyncOutcome;

    /// <summary>Latest known repository labels.</summary>
    public ObservableCollection<GitHubLabelInfo> Labels { get; } = [];

    /// <summary>
    /// Loads repository labels.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Labels query result on success, otherwise null.</returns>
    public async Task<GitHubLabelsResult?> LoadLabelsAsync(CancellationToken ct = default)
    {
        IsBusy = true;
        ErrorMessage = null;
        StatusMessage = "Loading labels...";
        try
        {
            var result = await _dispatcher.QueryAsync(new ListLabelsQuery(), ct);
            if (!result.IsSuccess || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Label load failed.";
                StatusMessage = "Label load failed.";
                return null;
            }

            if (!string.IsNullOrWhiteSpace(result.Value.Error))
            {
                ErrorMessage = result.Value.Error;
                StatusMessage = "Label load failed.";
                return result.Value;
            }

            Labels.Clear();
            foreach (var label in result.Value.Labels)
                Labels.Add(label);

            StatusMessage = $"Loaded {Labels.Count} labels.";
            return result.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Label load failed.";
            return null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Synchronizes issues from GitHub to TODO.
    /// </summary>
    /// <param name="state">Optional issue-state filter.</param>
    /// <param name="limit">Maximum issues to sync.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Sync result on success, otherwise null.</returns>
    public async Task<GitHubSyncOutcome?> SyncFromGitHubAsync(string? state = "open", int limit = 30, CancellationToken ct = default)
    {
        IsBusy = true;
        ErrorMessage = null;
        StatusMessage = "Syncing from GitHub...";
        try
        {
            var result = await _dispatcher.SendAsync(new SyncFromGitHubCommand(state, limit), ct);
            if (!result.IsSuccess || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Sync from GitHub failed.";
                StatusMessage = "Sync from GitHub failed.";
                return null;
            }

            LastSyncOutcome = result.Value;
            StatusMessage = $"Synced from GitHub (synced={result.Value.Synced}, skipped={result.Value.Skipped}, failed={result.Value.Failed}).";
            return result.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Sync from GitHub failed.";
            return null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Synchronizes TODO items to GitHub issues.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Sync result on success, otherwise null.</returns>
    public async Task<GitHubSyncOutcome?> SyncToGitHubAsync(CancellationToken ct = default)
    {
        IsBusy = true;
        ErrorMessage = null;
        StatusMessage = "Syncing to GitHub...";
        try
        {
            var result = await _dispatcher.SendAsync(new SyncToGitHubCommand(), ct);
            if (!result.IsSuccess || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Sync to GitHub failed.";
                StatusMessage = "Sync to GitHub failed.";
                return null;
            }

            LastSyncOutcome = result.Value;
            StatusMessage = $"Synced to GitHub (synced={result.Value.Synced}, skipped={result.Value.Skipped}, failed={result.Value.Failed}).";
            return result.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Sync to GitHub failed.";
            return null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Synchronizes a single issue in one direction.
    /// </summary>
    /// <param name="number">Issue number.</param>
    /// <param name="direction">Sync direction, for example from-github or to-github.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Single-issue sync result on success, otherwise null.</returns>
    public async Task<GitHubSingleIssueSyncOutcome?> SyncSingleIssueAsync(int number, string direction = "from-github", CancellationToken ct = default)
    {
        IsBusy = true;
        ErrorMessage = null;
        StatusMessage = $"Syncing issue #{number} ({direction})...";
        try
        {
            var result = await _dispatcher.SendAsync(new SyncSingleIssueCommand(number, direction), ct);
            if (!result.IsSuccess || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Single issue sync failed.";
                StatusMessage = "Single issue sync failed.";
                return null;
            }

            LastSingleIssueSyncOutcome = result.Value;
            if (!result.Value.Success)
            {
                StatusMessage = $"Issue #{number} sync failed.";
                return result.Value;
            }

            StatusMessage = $"Issue #{number} synced ({direction}).";
            return result.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Single issue sync failed.";
            return null;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
