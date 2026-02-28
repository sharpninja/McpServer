using McpServer.Cqrs;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.Handlers;

/// <summary>Handles <see cref="ListTunnelsQuery"/>.</summary>
internal sealed class ListTunnelsQueryHandler : IQueryHandler<ListTunnelsQuery, TunnelListSnapshot>
{
    private readonly ITunnelApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<ListTunnelsQueryHandler> _logger;

    public ListTunnelsQueryHandler(ITunnelApiClient client,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<ListTunnelsQueryHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<TunnelListSnapshot>> HandleAsync(ListTunnelsQuery query, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.TunnelList))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.TunnelList);
            return Result<TunnelListSnapshot>.Failure(BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _client.ListAsync(context.CancellationToken).ConfigureAwait(false);
            return Result<TunnelListSnapshot>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<TunnelListSnapshot>.Failure(ex);
        }
    }

    private static string BuildPermissionDenied(string? requiredRole)
        => string.IsNullOrWhiteSpace(requiredRole)
            ? "Permission denied."
            : $"Permission denied: requires {requiredRole}.";
}

/// <summary>Handles <see cref="EnableTunnelCommand"/>.</summary>
internal sealed class EnableTunnelCommandHandler : ICommandHandler<EnableTunnelCommand, TunnelProviderSnapshot>
{
    private readonly ITunnelApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<EnableTunnelCommandHandler> _logger;

    public EnableTunnelCommandHandler(ITunnelApiClient client,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<EnableTunnelCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<TunnelProviderSnapshot>> HandleAsync(EnableTunnelCommand command, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.TunnelEnable))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.TunnelEnable);
            return Result<TunnelProviderSnapshot>.Failure(BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _client.EnableAsync(command.ProviderName, context.CancellationToken).ConfigureAwait(false);
            return Result<TunnelProviderSnapshot>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<TunnelProviderSnapshot>.Failure(ex);
        }
    }

    private static string BuildPermissionDenied(string? requiredRole)
        => string.IsNullOrWhiteSpace(requiredRole)
            ? "Permission denied."
            : $"Permission denied: requires {requiredRole}.";
}

/// <summary>Handles <see cref="DisableTunnelCommand"/>.</summary>
internal sealed class DisableTunnelCommandHandler : ICommandHandler<DisableTunnelCommand, TunnelProviderSnapshot>
{
    private readonly ITunnelApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<DisableTunnelCommandHandler> _logger;

    public DisableTunnelCommandHandler(ITunnelApiClient client,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<DisableTunnelCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<TunnelProviderSnapshot>> HandleAsync(DisableTunnelCommand command, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.TunnelDisable))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.TunnelDisable);
            return Result<TunnelProviderSnapshot>.Failure(BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _client.DisableAsync(command.ProviderName, context.CancellationToken).ConfigureAwait(false);
            return Result<TunnelProviderSnapshot>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<TunnelProviderSnapshot>.Failure(ex);
        }
    }

    private static string BuildPermissionDenied(string? requiredRole)
        => string.IsNullOrWhiteSpace(requiredRole)
            ? "Permission denied."
            : $"Permission denied: requires {requiredRole}.";
}

/// <summary>Handles <see cref="StartTunnelCommand"/>.</summary>
internal sealed class StartTunnelCommandHandler : ICommandHandler<StartTunnelCommand, TunnelProviderSnapshot>
{
    private readonly ITunnelApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<StartTunnelCommandHandler> _logger;

    public StartTunnelCommandHandler(ITunnelApiClient client,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<StartTunnelCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<TunnelProviderSnapshot>> HandleAsync(StartTunnelCommand command, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.TunnelStart))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.TunnelStart);
            return Result<TunnelProviderSnapshot>.Failure(BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _client.StartAsync(command.ProviderName, context.CancellationToken).ConfigureAwait(false);
            return Result<TunnelProviderSnapshot>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<TunnelProviderSnapshot>.Failure(ex);
        }
    }

    private static string BuildPermissionDenied(string? requiredRole)
        => string.IsNullOrWhiteSpace(requiredRole)
            ? "Permission denied."
            : $"Permission denied: requires {requiredRole}.";
}

/// <summary>Handles <see cref="StopTunnelCommand"/>.</summary>
internal sealed class StopTunnelCommandHandler : ICommandHandler<StopTunnelCommand, TunnelProviderSnapshot>
{
    private readonly ITunnelApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<StopTunnelCommandHandler> _logger;

    public StopTunnelCommandHandler(ITunnelApiClient client,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<StopTunnelCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<TunnelProviderSnapshot>> HandleAsync(StopTunnelCommand command, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.TunnelStop))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.TunnelStop);
            return Result<TunnelProviderSnapshot>.Failure(BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _client.StopAsync(command.ProviderName, context.CancellationToken).ConfigureAwait(false);
            return Result<TunnelProviderSnapshot>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<TunnelProviderSnapshot>.Failure(ex);
        }
    }

    private static string BuildPermissionDenied(string? requiredRole)
        => string.IsNullOrWhiteSpace(requiredRole)
            ? "Permission denied."
            : $"Permission denied: requires {requiredRole}.";
}

/// <summary>Handles <see cref="RestartTunnelCommand"/>.</summary>
internal sealed class RestartTunnelCommandHandler : ICommandHandler<RestartTunnelCommand, TunnelProviderSnapshot>
{
    private readonly ITunnelApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<RestartTunnelCommandHandler> _logger;

    public RestartTunnelCommandHandler(ITunnelApiClient client,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<RestartTunnelCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<TunnelProviderSnapshot>> HandleAsync(RestartTunnelCommand command, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.TunnelRestart))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.TunnelRestart);
            return Result<TunnelProviderSnapshot>.Failure(BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _client.RestartAsync(command.ProviderName, context.CancellationToken).ConfigureAwait(false);
            return Result<TunnelProviderSnapshot>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<TunnelProviderSnapshot>.Failure(ex);
        }
    }

    private static string BuildPermissionDenied(string? requiredRole)
        => string.IsNullOrWhiteSpace(requiredRole)
            ? "Permission denied."
            : $"Permission denied: requires {requiredRole}.";
}
