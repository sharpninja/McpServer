using McpServer.Cqrs;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.Handlers;

/// <summary>Handles <see cref="ListIssuesQuery"/>.</summary>
internal sealed class ListIssuesQueryHandler : IQueryHandler<ListIssuesQuery, GitHubIssueListResult>
{
    private readonly IGitHubApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<ListIssuesQueryHandler> _logger;

    public ListIssuesQueryHandler(IGitHubApiClient client, IAuthorizationPolicyService authorizationPolicy, ILogger<ListIssuesQueryHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<GitHubIssueListResult>> HandleAsync(ListIssuesQuery query, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.GitHubIssueList))
            return Result<GitHubIssueListResult>.Failure(GitHubHandlerHelpers.BuildPermissionDenied(_authorizationPolicy.GetRequiredRole(McpActionKeys.GitHubIssueList)));

        try
        {
            var result = await _client.ListIssuesAsync(query, context.CancellationToken).ConfigureAwait(false);
            return Result<GitHubIssueListResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<GitHubIssueListResult>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="GetIssueQuery"/>.</summary>
internal sealed class GetIssueQueryHandler : IQueryHandler<GetIssueQuery, GitHubIssueDetail?>
{
    private readonly IGitHubApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<GetIssueQueryHandler> _logger;

    public GetIssueQueryHandler(IGitHubApiClient client, IAuthorizationPolicyService authorizationPolicy, ILogger<GetIssueQueryHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<GitHubIssueDetail?>> HandleAsync(GetIssueQuery query, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.GitHubIssueGet))
            return Result<GitHubIssueDetail?>.Failure(GitHubHandlerHelpers.BuildPermissionDenied(_authorizationPolicy.GetRequiredRole(McpActionKeys.GitHubIssueGet)));

        try
        {
            var result = await _client.GetIssueAsync(query.Number, context.CancellationToken).ConfigureAwait(false);
            return Result<GitHubIssueDetail?>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<GitHubIssueDetail?>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="CreateIssueCommand"/>.</summary>
internal sealed class CreateIssueCommandHandler : ICommandHandler<CreateIssueCommand, GitHubCreateIssueOutcome>
{
    private readonly IGitHubApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<CreateIssueCommandHandler> _logger;

    public CreateIssueCommandHandler(IGitHubApiClient client, IAuthorizationPolicyService authorizationPolicy, ILogger<CreateIssueCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<GitHubCreateIssueOutcome>> HandleAsync(CreateIssueCommand command, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.GitHubIssueMutate))
            return Result<GitHubCreateIssueOutcome>.Failure(GitHubHandlerHelpers.BuildPermissionDenied(_authorizationPolicy.GetRequiredRole(McpActionKeys.GitHubIssueMutate)));

        try
        {
            var result = await _client.CreateIssueAsync(command, context.CancellationToken).ConfigureAwait(false);
            return Result<GitHubCreateIssueOutcome>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<GitHubCreateIssueOutcome>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="UpdateIssueCommand"/>.</summary>
internal sealed class UpdateIssueCommandHandler : ICommandHandler<UpdateIssueCommand, GitHubMutationOutcome>
{
    private readonly IGitHubApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<UpdateIssueCommandHandler> _logger;

    public UpdateIssueCommandHandler(IGitHubApiClient client, IAuthorizationPolicyService authorizationPolicy, ILogger<UpdateIssueCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<GitHubMutationOutcome>> HandleAsync(UpdateIssueCommand command, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.GitHubIssueMutate))
            return Result<GitHubMutationOutcome>.Failure(GitHubHandlerHelpers.BuildPermissionDenied(_authorizationPolicy.GetRequiredRole(McpActionKeys.GitHubIssueMutate)));

        try
        {
            var result = await _client.UpdateIssueAsync(command, context.CancellationToken).ConfigureAwait(false);
            return Result<GitHubMutationOutcome>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<GitHubMutationOutcome>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="CloseIssueCommand"/>.</summary>
internal sealed class CloseIssueCommandHandler : ICommandHandler<CloseIssueCommand, GitHubMutationOutcome>
{
    private readonly IGitHubApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<CloseIssueCommandHandler> _logger;

    public CloseIssueCommandHandler(IGitHubApiClient client, IAuthorizationPolicyService authorizationPolicy, ILogger<CloseIssueCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<GitHubMutationOutcome>> HandleAsync(CloseIssueCommand command, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.GitHubIssueMutate))
            return Result<GitHubMutationOutcome>.Failure(GitHubHandlerHelpers.BuildPermissionDenied(_authorizationPolicy.GetRequiredRole(McpActionKeys.GitHubIssueMutate)));

        try
        {
            var result = await _client.CloseIssueAsync(command, context.CancellationToken).ConfigureAwait(false);
            return Result<GitHubMutationOutcome>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<GitHubMutationOutcome>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="ReopenIssueCommand"/>.</summary>
internal sealed class ReopenIssueCommandHandler : ICommandHandler<ReopenIssueCommand, GitHubMutationOutcome>
{
    private readonly IGitHubApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<ReopenIssueCommandHandler> _logger;

    public ReopenIssueCommandHandler(IGitHubApiClient client, IAuthorizationPolicyService authorizationPolicy, ILogger<ReopenIssueCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<GitHubMutationOutcome>> HandleAsync(ReopenIssueCommand command, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.GitHubIssueMutate))
            return Result<GitHubMutationOutcome>.Failure(GitHubHandlerHelpers.BuildPermissionDenied(_authorizationPolicy.GetRequiredRole(McpActionKeys.GitHubIssueMutate)));

        try
        {
            var result = await _client.ReopenIssueAsync(command, context.CancellationToken).ConfigureAwait(false);
            return Result<GitHubMutationOutcome>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<GitHubMutationOutcome>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="CommentOnIssueCommand"/>.</summary>
internal sealed class CommentOnIssueCommandHandler : ICommandHandler<CommentOnIssueCommand, GitHubMutationOutcome>
{
    private readonly IGitHubApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<CommentOnIssueCommandHandler> _logger;

    public CommentOnIssueCommandHandler(IGitHubApiClient client, IAuthorizationPolicyService authorizationPolicy, ILogger<CommentOnIssueCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<GitHubMutationOutcome>> HandleAsync(CommentOnIssueCommand command, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.GitHubIssueMutate))
            return Result<GitHubMutationOutcome>.Failure(GitHubHandlerHelpers.BuildPermissionDenied(_authorizationPolicy.GetRequiredRole(McpActionKeys.GitHubIssueMutate)));

        try
        {
            var result = await _client.CommentOnIssueAsync(command, context.CancellationToken).ConfigureAwait(false);
            return Result<GitHubMutationOutcome>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<GitHubMutationOutcome>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="ListLabelsQuery"/>.</summary>
internal sealed class ListLabelsQueryHandler : IQueryHandler<ListLabelsQuery, GitHubLabelsResult>
{
    private readonly IGitHubApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<ListLabelsQueryHandler> _logger;

    public ListLabelsQueryHandler(IGitHubApiClient client, IAuthorizationPolicyService authorizationPolicy, ILogger<ListLabelsQueryHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<GitHubLabelsResult>> HandleAsync(ListLabelsQuery query, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.GitHubLabelList))
            return Result<GitHubLabelsResult>.Failure(GitHubHandlerHelpers.BuildPermissionDenied(_authorizationPolicy.GetRequiredRole(McpActionKeys.GitHubLabelList)));

        try
        {
            var result = await _client.ListLabelsAsync(context.CancellationToken).ConfigureAwait(false);
            return Result<GitHubLabelsResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<GitHubLabelsResult>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="ListPullsQuery"/>.</summary>
internal sealed class ListPullsQueryHandler : IQueryHandler<ListPullsQuery, GitHubPullListResult>
{
    private readonly IGitHubApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<ListPullsQueryHandler> _logger;

    public ListPullsQueryHandler(IGitHubApiClient client, IAuthorizationPolicyService authorizationPolicy, ILogger<ListPullsQueryHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<GitHubPullListResult>> HandleAsync(ListPullsQuery query, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.GitHubPullList))
            return Result<GitHubPullListResult>.Failure(GitHubHandlerHelpers.BuildPermissionDenied(_authorizationPolicy.GetRequiredRole(McpActionKeys.GitHubPullList)));

        try
        {
            var result = await _client.ListPullsAsync(query, context.CancellationToken).ConfigureAwait(false);
            return Result<GitHubPullListResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<GitHubPullListResult>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="CommentOnPullCommand"/>.</summary>
internal sealed class CommentOnPullCommandHandler : ICommandHandler<CommentOnPullCommand, GitHubMutationOutcome>
{
    private readonly IGitHubApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<CommentOnPullCommandHandler> _logger;

    public CommentOnPullCommandHandler(IGitHubApiClient client, IAuthorizationPolicyService authorizationPolicy, ILogger<CommentOnPullCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<GitHubMutationOutcome>> HandleAsync(CommentOnPullCommand command, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.GitHubPullComment))
            return Result<GitHubMutationOutcome>.Failure(GitHubHandlerHelpers.BuildPermissionDenied(_authorizationPolicy.GetRequiredRole(McpActionKeys.GitHubPullComment)));

        try
        {
            var result = await _client.CommentOnPullAsync(command, context.CancellationToken).ConfigureAwait(false);
            return Result<GitHubMutationOutcome>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<GitHubMutationOutcome>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="SyncFromGitHubCommand"/>.</summary>
internal sealed class SyncFromGitHubCommandHandler : ICommandHandler<SyncFromGitHubCommand, GitHubSyncOutcome>
{
    private readonly IGitHubApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<SyncFromGitHubCommandHandler> _logger;

    public SyncFromGitHubCommandHandler(IGitHubApiClient client, IAuthorizationPolicyService authorizationPolicy, ILogger<SyncFromGitHubCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<GitHubSyncOutcome>> HandleAsync(SyncFromGitHubCommand command, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.GitHubSync))
            return Result<GitHubSyncOutcome>.Failure(GitHubHandlerHelpers.BuildPermissionDenied(_authorizationPolicy.GetRequiredRole(McpActionKeys.GitHubSync)));

        try
        {
            var result = await _client.SyncFromGitHubAsync(command, context.CancellationToken).ConfigureAwait(false);
            return Result<GitHubSyncOutcome>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<GitHubSyncOutcome>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="SyncToGitHubCommand"/>.</summary>
internal sealed class SyncToGitHubCommandHandler : ICommandHandler<SyncToGitHubCommand, GitHubSyncOutcome>
{
    private readonly IGitHubApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<SyncToGitHubCommandHandler> _logger;

    public SyncToGitHubCommandHandler(IGitHubApiClient client, IAuthorizationPolicyService authorizationPolicy, ILogger<SyncToGitHubCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<GitHubSyncOutcome>> HandleAsync(SyncToGitHubCommand command, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.GitHubSync))
            return Result<GitHubSyncOutcome>.Failure(GitHubHandlerHelpers.BuildPermissionDenied(_authorizationPolicy.GetRequiredRole(McpActionKeys.GitHubSync)));

        try
        {
            var result = await _client.SyncToGitHubAsync(context.CancellationToken).ConfigureAwait(false);
            return Result<GitHubSyncOutcome>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<GitHubSyncOutcome>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="SyncSingleIssueCommand"/>.</summary>
internal sealed class SyncSingleIssueCommandHandler : ICommandHandler<SyncSingleIssueCommand, GitHubSingleIssueSyncOutcome>
{
    private readonly IGitHubApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<SyncSingleIssueCommandHandler> _logger;

    public SyncSingleIssueCommandHandler(IGitHubApiClient client, IAuthorizationPolicyService authorizationPolicy, ILogger<SyncSingleIssueCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<GitHubSingleIssueSyncOutcome>> HandleAsync(SyncSingleIssueCommand command, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.GitHubSync))
            return Result<GitHubSingleIssueSyncOutcome>.Failure(GitHubHandlerHelpers.BuildPermissionDenied(_authorizationPolicy.GetRequiredRole(McpActionKeys.GitHubSync)));

        try
        {
            var result = await _client.SyncSingleIssueAsync(command, context.CancellationToken).ConfigureAwait(false);
            return Result<GitHubSingleIssueSyncOutcome>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<GitHubSingleIssueSyncOutcome>.Failure(ex);
        }
    }
}

internal static class GitHubHandlerHelpers
{
    public static string BuildPermissionDenied(string? requiredRole)
        => string.IsNullOrWhiteSpace(requiredRole)
            ? "Permission denied."
            : $"Permission denied: requires {requiredRole}.";
}
