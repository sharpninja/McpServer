using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.ViewModels.Base;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.ViewModels;

/// <summary>
/// ViewModel for GitHub issue detail and mutation workflows.
/// </summary>
[ViewModelCommand("github-issue-detail", Description = "Get/create/update/comment/close issues")]
public sealed class IssueDetailViewModel : AreaDetailViewModelBase<GitHubIssueDetail>
{
    private readonly Dispatcher _dispatcher;
    private readonly ILogger<IssueDetailViewModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="IssueDetailViewModel"/> class.
    /// </summary>
    /// <param name="dispatcher">CQRS dispatcher.</param>
    /// <param name="logger">Logger instance.</param>
    public IssueDetailViewModel(
        Dispatcher dispatcher,
        ILogger<IssueDetailViewModel> logger)
        : base(McpArea.GitHub)
    {
        _dispatcher = dispatcher;
        _logger = logger;
    }

    /// <summary>
    /// Loads issue detail by issue number.
    /// </summary>
    /// <param name="number">Issue number.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Issue detail if found.</returns>
    public async Task<GitHubIssueDetail?> LoadAsync(int number, CancellationToken ct = default)
    {
        IsBusy = true;
        ErrorMessage = null;
        StatusMessage = $"Loading issue #{number}...";
        try
        {
            var result = await _dispatcher.QueryAsync(new GetIssueQuery(number), ct).ConfigureAwait(true);
            if (!result.IsSuccess)
            {
                ErrorMessage = result.Error ?? "Issue load failed.";
                StatusMessage = "Issue load failed.";
                return null;
            }

            Detail = result.Value;
            LastUpdatedAt = DateTimeOffset.UtcNow;
            StatusMessage = result.Value is null ? $"Issue #{number} not found." : $"Loaded issue #{number}.";
            return result.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Issue load failed.";
            return null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Creates an issue and returns creation result.
    /// </summary>
    /// <param name="title">Issue title.</param>
    /// <param name="body">Issue body.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Create result on success, otherwise null.</returns>
    public async Task<GitHubCreateIssueOutcome?> CreateAsync(string title, string? body, CancellationToken ct = default)
    {
        IsBusy = true;
        ErrorMessage = null;
        StatusMessage = "Creating issue...";
        try
        {
            var command = new CreateIssueCommand { Title = title, Body = body };
            var result = await _dispatcher.SendAsync(command, ct).ConfigureAwait(true);
            if (!result.IsSuccess || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Issue create failed.";
                StatusMessage = "Issue create failed.";
                return null;
            }

            StatusMessage = $"Created issue #{result.Value.Number}.";
            await LoadAsync(result.Value.Number, ct).ConfigureAwait(true);
            return result.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Issue create failed.";
            return null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Updates an issue.
    /// </summary>
    /// <param name="command">Issue update payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Mutation result on success, otherwise null.</returns>
    public async Task<GitHubMutationOutcome?> UpdateAsync(UpdateIssueCommand command, CancellationToken ct = default)
    {
        var outcome = await SendMutationAsync(command, $"Saving issue #{command.Number}...", ct).ConfigureAwait(true);
        if (outcome is { Success: true })
            await LoadAsync(command.Number, ct).ConfigureAwait(true);
        return outcome;
    }

    /// <summary>
    /// Closes an issue.
    /// </summary>
    /// <param name="number">Issue number.</param>
    /// <param name="reason">Optional close reason.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Mutation result on success, otherwise null.</returns>
    public async Task<GitHubMutationOutcome?> CloseAsync(int number, string? reason = null, CancellationToken ct = default)
    {
        var outcome = await SendMutationAsync(new CloseIssueCommand(number, reason), $"Closing issue #{number}...", ct)
            .ConfigureAwait(true);
        if (outcome is { Success: true })
            await LoadAsync(number, ct).ConfigureAwait(true);
        return outcome;
    }

    /// <summary>
    /// Reopens an issue.
    /// </summary>
    /// <param name="number">Issue number.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Mutation result on success, otherwise null.</returns>
    public async Task<GitHubMutationOutcome?> ReopenAsync(int number, CancellationToken ct = default)
    {
        var outcome = await SendMutationAsync(new ReopenIssueCommand(number), $"Reopening issue #{number}...", ct)
            .ConfigureAwait(true);
        if (outcome is { Success: true })
            await LoadAsync(number, ct).ConfigureAwait(true);
        return outcome;
    }

    /// <summary>
    /// Adds a comment to an issue.
    /// </summary>
    /// <param name="number">Issue number.</param>
    /// <param name="body">Comment markdown body.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Mutation result on success, otherwise null.</returns>
    public async Task<GitHubMutationOutcome?> CommentAsync(int number, string body, CancellationToken ct = default)
    {
        var outcome = await SendMutationAsync(new CommentOnIssueCommand(number, body), $"Commenting on issue #{number}...", ct)
            .ConfigureAwait(true);
        if (outcome is { Success: true })
            await LoadAsync(number, ct).ConfigureAwait(true);
        return outcome;
    }

    private async Task<GitHubMutationOutcome?> SendMutationAsync<TCommand>(TCommand command, string pendingStatus, CancellationToken ct)
        where TCommand : ICommand<GitHubMutationOutcome>
    {
        IsBusy = true;
        ErrorMessage = null;
        StatusMessage = pendingStatus;
        try
        {
            var result = await _dispatcher.SendAsync(command, ct).ConfigureAwait(true);
            if (!result.IsSuccess || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Issue mutation failed.";
                StatusMessage = "Issue mutation failed.";
                return null;
            }

            if (!result.Value.Success)
            {
                ErrorMessage = result.Value.ErrorMessage ?? "Issue mutation failed.";
                StatusMessage = "Issue mutation failed.";
                return result.Value;
            }

            StatusMessage = "Issue operation completed.";
            return result.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Issue mutation failed.";
            return null;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
