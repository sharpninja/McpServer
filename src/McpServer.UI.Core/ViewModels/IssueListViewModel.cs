using CommunityToolkit.Mvvm.ComponentModel;
using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.ViewModels.Base;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.ViewModels;

/// <summary>
/// ViewModel for GitHub issue list queries.
/// </summary>
[ViewModelCommand("github-issues-list", Description = "List GitHub issues")]
public sealed partial class IssueListViewModel : AreaListViewModelBase<GitHubIssueSummary>
{
    private readonly Dispatcher _dispatcher;
    private readonly ILogger<IssueListViewModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="IssueListViewModel"/> class.
    /// </summary>
    /// <param name="dispatcher">CQRS dispatcher.</param>
    /// <param name="logger">Logger instance.</param>
    public IssueListViewModel(
        Dispatcher dispatcher,
        ILogger<IssueListViewModel> logger)
        : base(McpArea.GitHub)
    {
        _dispatcher = dispatcher;
        _logger = logger;
    }

    /// <summary>Issue state filter (for example open/closed/all).</summary>
    [ObservableProperty]
    private string? _stateFilter = "open";

    /// <summary>Maximum number of issues to request.</summary>
    [ObservableProperty]
    private int _limit = 30;

    /// <summary>
    /// Loads issues using the configured filter settings.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    public async Task LoadAsync(CancellationToken ct = default)
    {
        if (IsLoading)
            return;

        IsLoading = true;
        ErrorMessage = null;
        StatusMessage = "Loading GitHub issues...";
        try
        {
            var query = new ListIssuesQuery(
                string.IsNullOrWhiteSpace(StateFilter) ? null : StateFilter.Trim(),
                Limit <= 0 ? 30 : Limit);
            var result = await _dispatcher.QueryAsync(query, ct);
            if (!result.IsSuccess || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Issue list failed.";
                StatusMessage = "Issue list failed.";
                return;
            }

            if (!string.IsNullOrWhiteSpace(result.Value.Error))
            {
                ErrorMessage = result.Value.Error;
                StatusMessage = "Issue list failed.";
                return;
            }

            SetItems(result.Value.Items, result.Value.Items.Count);
            StatusMessage = $"Loaded {Items.Count} issues.";
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Issue list failed.";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
