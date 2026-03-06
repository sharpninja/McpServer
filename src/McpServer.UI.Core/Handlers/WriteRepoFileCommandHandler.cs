using McpServer.Cqrs;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.Handlers;

/// <summary>Handles <see cref="WriteRepoFileCommand"/>.</summary>
internal sealed class WriteRepoFileCommandHandler : ICommandHandler<WriteRepoFileCommand, RepoWriteOutcome>
{
    private readonly IRepoApiClient _repoApiClient;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<WriteRepoFileCommandHandler> _logger;


    public WriteRepoFileCommandHandler(IRepoApiClient repoApiClient, IAuthorizationPolicyService authorizationPolicy,
        ILogger<WriteRepoFileCommandHandler> logger)
    {
        _logger = logger;
        _repoApiClient = repoApiClient;
        _authorizationPolicy = authorizationPolicy;
    }

    public async Task<Result<RepoWriteOutcome>> HandleAsync(WriteRepoFileCommand command, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.RepoWrite))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.RepoWrite);
            return Result<RepoWriteOutcome>.Failure(
                string.IsNullOrWhiteSpace(requiredRole)
                    ? "Permission denied."
                    : $"Permission denied: requires {requiredRole}.");
        }

        try
        {
            var result = await _repoApiClient.WriteFileAsync(command, context.CancellationToken).ConfigureAwait(true);
            return Result<RepoWriteOutcome>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<RepoWriteOutcome>.Failure(ex);
        }
    }
}
