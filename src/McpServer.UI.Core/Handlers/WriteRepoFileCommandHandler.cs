using McpServer.Cqrs;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;

namespace McpServer.UI.Core.Handlers;

/// <summary>Handles <see cref="WriteRepoFileCommand"/>.</summary>
internal sealed class WriteRepoFileCommandHandler : ICommandHandler<WriteRepoFileCommand, RepoWriteOutcome>
{
    private readonly IRepoApiClient _repoApiClient;
    private readonly IAuthorizationPolicyService _authorizationPolicy;

    public WriteRepoFileCommandHandler(IRepoApiClient repoApiClient, IAuthorizationPolicyService authorizationPolicy)
    {
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
            var result = await _repoApiClient.WriteFileAsync(command, context.CancellationToken).ConfigureAwait(false);
            return Result<RepoWriteOutcome>.Success(result);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError(ex.ToString());
            return Result<RepoWriteOutcome>.Failure(ex);
        }
    }
}
