using McpServer.Cqrs;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.Handlers;

/// <summary>Handles <see cref="SubmitSessionLogCommand"/>.</summary>
internal sealed class SubmitSessionLogCommandHandler : ICommandHandler<SubmitSessionLogCommand, SessionLogSubmitOutcome>
{
    private readonly ISessionLogApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<SubmitSessionLogCommandHandler> _logger;

    public SubmitSessionLogCommandHandler(
        ISessionLogApiClient client,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<SubmitSessionLogCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<SessionLogSubmitOutcome>> HandleAsync(SubmitSessionLogCommand command, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.SessionLogSubmit))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.SessionLogSubmit);
            return Result<SessionLogSubmitOutcome>.Failure(SessionLogMutationHandlerHelpers.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _client.SubmitSessionLogAsync(command, context.CancellationToken);
            return Result<SessionLogSubmitOutcome>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<SessionLogSubmitOutcome>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="AppendSessionLogDialogCommand"/>.</summary>
internal sealed class AppendSessionLogDialogCommandHandler : ICommandHandler<AppendSessionLogDialogCommand, SessionLogDialogAppendOutcome>
{
    private readonly ISessionLogApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<AppendSessionLogDialogCommandHandler> _logger;

    public AppendSessionLogDialogCommandHandler(
        ISessionLogApiClient client,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<AppendSessionLogDialogCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<SessionLogDialogAppendOutcome>> HandleAsync(AppendSessionLogDialogCommand command, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.SessionLogAppendDialog))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.SessionLogAppendDialog);
            return Result<SessionLogDialogAppendOutcome>.Failure(SessionLogMutationHandlerHelpers.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _client.AppendSessionLogDialogAsync(command, context.CancellationToken);
            return Result<SessionLogDialogAppendOutcome>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<SessionLogDialogAppendOutcome>.Failure(ex);
        }
    }
}

internal static class SessionLogMutationHandlerHelpers
{
    public static string BuildPermissionDenied(string? requiredRole)
        => string.IsNullOrWhiteSpace(requiredRole)
            ? "Permission denied."
            : $"Permission denied: requires {requiredRole}.";
}
