using McpServer.Cqrs;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.Handlers;

/// <summary>Handles <see cref="CreateVoiceSessionCommand"/>.</summary>
internal sealed class CreateVoiceSessionCommandHandler : ICommandHandler<CreateVoiceSessionCommand, VoiceSessionInfo>
{
    private readonly IVoiceApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<CreateVoiceSessionCommandHandler> _logger;

    public CreateVoiceSessionCommandHandler(IVoiceApiClient client, IAuthorizationPolicyService authorizationPolicy, ILogger<CreateVoiceSessionCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<VoiceSessionInfo>> HandleAsync(CreateVoiceSessionCommand command, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.VoiceCreateSession))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.VoiceCreateSession);
            return Result<VoiceSessionInfo>.Failure(VoiceHandlerHelpers.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _client.CreateSessionAsync(command, context.CancellationToken);
            return Result<VoiceSessionInfo>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<VoiceSessionInfo>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="SubmitVoiceTurnCommand"/>.</summary>
internal sealed class SubmitVoiceTurnCommandHandler : ICommandHandler<SubmitVoiceTurnCommand, VoiceTurnInfo>
{
    private readonly IVoiceApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<SubmitVoiceTurnCommandHandler> _logger;

    public SubmitVoiceTurnCommandHandler(IVoiceApiClient client, IAuthorizationPolicyService authorizationPolicy, ILogger<SubmitVoiceTurnCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<VoiceTurnInfo>> HandleAsync(SubmitVoiceTurnCommand command, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.VoiceSubmitTurn))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.VoiceSubmitTurn);
            return Result<VoiceTurnInfo>.Failure(VoiceHandlerHelpers.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _client.SubmitTurnAsync(command, context.CancellationToken);
            return Result<VoiceTurnInfo>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<VoiceTurnInfo>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="InterruptVoiceCommand"/>.</summary>
internal sealed class InterruptVoiceCommandHandler : ICommandHandler<InterruptVoiceCommand, VoiceInterruptInfo>
{
    private readonly IVoiceApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<InterruptVoiceCommandHandler> _logger;

    public InterruptVoiceCommandHandler(IVoiceApiClient client, IAuthorizationPolicyService authorizationPolicy, ILogger<InterruptVoiceCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<VoiceInterruptInfo>> HandleAsync(InterruptVoiceCommand command, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.VoiceInterrupt))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.VoiceInterrupt);
            return Result<VoiceInterruptInfo>.Failure(VoiceHandlerHelpers.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _client.InterruptAsync(command, context.CancellationToken);
            return Result<VoiceInterruptInfo>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<VoiceInterruptInfo>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="GetVoiceStatusQuery"/>.</summary>
internal sealed class GetVoiceStatusQueryHandler : IQueryHandler<GetVoiceStatusQuery, VoiceSessionStatusInfo?>
{
    private readonly IVoiceApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<GetVoiceStatusQueryHandler> _logger;

    public GetVoiceStatusQueryHandler(IVoiceApiClient client, IAuthorizationPolicyService authorizationPolicy, ILogger<GetVoiceStatusQueryHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<VoiceSessionStatusInfo?>> HandleAsync(GetVoiceStatusQuery query, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.VoiceStatus))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.VoiceStatus);
            return Result<VoiceSessionStatusInfo?>.Failure(VoiceHandlerHelpers.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _client.GetStatusAsync(query.SessionId, context.CancellationToken);
            return Result<VoiceSessionStatusInfo?>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<VoiceSessionStatusInfo?>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="GetVoiceTranscriptQuery"/>.</summary>
internal sealed class GetVoiceTranscriptQueryHandler : IQueryHandler<GetVoiceTranscriptQuery, VoiceTranscriptInfo?>
{
    private readonly IVoiceApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<GetVoiceTranscriptQueryHandler> _logger;

    public GetVoiceTranscriptQueryHandler(IVoiceApiClient client, IAuthorizationPolicyService authorizationPolicy, ILogger<GetVoiceTranscriptQueryHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<VoiceTranscriptInfo?>> HandleAsync(GetVoiceTranscriptQuery query, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.VoiceTranscript))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.VoiceTranscript);
            return Result<VoiceTranscriptInfo?>.Failure(VoiceHandlerHelpers.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _client.GetTranscriptAsync(query.SessionId, context.CancellationToken);
            return Result<VoiceTranscriptInfo?>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<VoiceTranscriptInfo?>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="DeleteVoiceSessionCommand"/>.</summary>
internal sealed class DeleteVoiceSessionCommandHandler : ICommandHandler<DeleteVoiceSessionCommand, bool>
{
    private readonly IVoiceApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<DeleteVoiceSessionCommandHandler> _logger;

    public DeleteVoiceSessionCommandHandler(IVoiceApiClient client, IAuthorizationPolicyService authorizationPolicy, ILogger<DeleteVoiceSessionCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<bool>> HandleAsync(DeleteVoiceSessionCommand command, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.VoiceDeleteSession))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.VoiceDeleteSession);
            return Result<bool>.Failure(VoiceHandlerHelpers.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var deleted = await _client.DeleteSessionAsync(command.SessionId, context.CancellationToken);
            return Result<bool>.Success(deleted);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<bool>.Failure(ex);
        }
    }
}

internal static class VoiceHandlerHelpers
{
    public static string BuildPermissionDenied(string? requiredRole)
        => string.IsNullOrWhiteSpace(requiredRole)
            ? "Permission denied."
            : $"Permission denied: requires {requiredRole}.";
}
