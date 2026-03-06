using CommunityToolkit.Mvvm.ComponentModel;
using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.ViewModels.Base;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.ViewModels;

/// <summary>
/// ViewModel for GitHub pull request listing and PR commenting.
/// </summary>
[ViewModelCommand("github-pulls-list", Description = "List and comment on pull requests")]
public sealed partial class PullRequestListViewModel : AreaListViewModelBase<GitHubPullSummary>
{
    private readonly Dispatcher _dispatcher;
    private readonly ILogger<PullRequestListViewModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PullRequestListViewModel"/> class.
    /// </summary>
    /// <param name="dispatcher">CQRS dispatcher.</param>
    /// <param name="logger">Logger instance.</param>
    public PullRequestListViewModel(
        Dispatcher dispatcher,
        ILogger<PullRequestListViewModel> logger)
        : base(McpArea.GitHub)
    {
        _dispatcher = dispatcher;
        _logger = logger;
    }

    /// <summary>Pull request state filter.</summary>
    [ObservableProperty]
    private string? _stateFilter = "open";

    /// <summary>Maximum PR count to return.</summary>
    [ObservableProperty]
    private int _limit = 30;

    /// <summary>
    /// Loads pull requests.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    public async Task LoadAsync(CancellationToken ct = default)
    {
        if (IsLoading)
            return;

        IsLoading = true;
        ErrorMessage = null;
        StatusMessage = "Loading pull requests...";
        try
        {
            var query = new ListPullsQuery(
                string.IsNullOrWhiteSpace(StateFilter) ? null : StateFilter.Trim(),
                Limit <= 0 ? 30 : Limit);
            var result = await _dispatcher.QueryAsync(query, ct).ConfigureAwait(true);
            if (!result.IsSuccess || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Pull request list failed.";
                StatusMessage = "Pull request list failed.";
                return;
            }

            if (!string.IsNullOrWhiteSpace(result.Value.Error))
            {
                ErrorMessage = result.Value.Error;
                StatusMessage = "Pull request list failed.";
                return;
            }

            SetItems(result.Value.Items, result.Value.Items.Count);
            StatusMessage = $"Loaded {Items.Count} pull requests.";
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Pull request list failed.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Adds a comment to a pull request.
    /// </summary>
    /// <param name="number">Pull request number.</param>
    /// <param name="body">Comment markdown body.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Mutation result on success, otherwise null.</returns>
    public async Task<GitHubMutationOutcome?> CommentAsync(int number, string body, CancellationToken ct = default)
    {
        IsLoading = true;
        ErrorMessage = null;
        StatusMessage = $"Commenting on PR #{number}...";
        try
        {
            var result = await _dispatcher.SendAsync(new CommentOnPullCommand(number, body), ct).ConfigureAwait(true);
            if (!result.IsSuccess || result.Value is null)
            {
                ErrorMessage = result.Error ?? "PR comment failed.";
                StatusMessage = "PR comment failed.";
                return null;
            }

            if (!result.Value.Success)
            {
                ErrorMessage = result.Value.ErrorMessage ?? "PR comment failed.";
                StatusMessage = "PR comment failed.";
                return result.Value;
            }

            StatusMessage = $"Comment posted to PR #{number}.";
            return result.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "PR comment failed.";
            return null;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
