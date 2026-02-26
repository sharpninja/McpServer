using McpServer.Cqrs;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;

namespace McpServer.UI.Core.Handlers;

/// <summary>Handles <see cref="ListTunnelsQuery"/>.</summary>
internal sealed class ListTunnelsQueryHandler : IQueryHandler<ListTunnelsQuery, TunnelListSnapshot>
{
    private readonly ITunnelApiClient _client;

    public ListTunnelsQueryHandler(ITunnelApiClient client) => _client = client;

    public async Task<Result<TunnelListSnapshot>> HandleAsync(ListTunnelsQuery query, CallContext context)
    {
        try
        {
            var result = await _client.ListAsync(context.CancellationToken).ConfigureAwait(false);
            return Result<TunnelListSnapshot>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<TunnelListSnapshot>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="EnableTunnelCommand"/>.</summary>
internal sealed class EnableTunnelCommandHandler : ICommandHandler<EnableTunnelCommand, TunnelProviderSnapshot>
{
    private readonly ITunnelApiClient _client;

    public EnableTunnelCommandHandler(ITunnelApiClient client) => _client = client;

    public async Task<Result<TunnelProviderSnapshot>> HandleAsync(EnableTunnelCommand command, CallContext context)
    {
        try
        {
            var result = await _client.EnableAsync(command.ProviderName, context.CancellationToken).ConfigureAwait(false);
            return Result<TunnelProviderSnapshot>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<TunnelProviderSnapshot>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="DisableTunnelCommand"/>.</summary>
internal sealed class DisableTunnelCommandHandler : ICommandHandler<DisableTunnelCommand, TunnelProviderSnapshot>
{
    private readonly ITunnelApiClient _client;

    public DisableTunnelCommandHandler(ITunnelApiClient client) => _client = client;

    public async Task<Result<TunnelProviderSnapshot>> HandleAsync(DisableTunnelCommand command, CallContext context)
    {
        try
        {
            var result = await _client.DisableAsync(command.ProviderName, context.CancellationToken).ConfigureAwait(false);
            return Result<TunnelProviderSnapshot>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<TunnelProviderSnapshot>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="StartTunnelCommand"/>.</summary>
internal sealed class StartTunnelCommandHandler : ICommandHandler<StartTunnelCommand, TunnelProviderSnapshot>
{
    private readonly ITunnelApiClient _client;

    public StartTunnelCommandHandler(ITunnelApiClient client) => _client = client;

    public async Task<Result<TunnelProviderSnapshot>> HandleAsync(StartTunnelCommand command, CallContext context)
    {
        try
        {
            var result = await _client.StartAsync(command.ProviderName, context.CancellationToken).ConfigureAwait(false);
            return Result<TunnelProviderSnapshot>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<TunnelProviderSnapshot>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="StopTunnelCommand"/>.</summary>
internal sealed class StopTunnelCommandHandler : ICommandHandler<StopTunnelCommand, TunnelProviderSnapshot>
{
    private readonly ITunnelApiClient _client;

    public StopTunnelCommandHandler(ITunnelApiClient client) => _client = client;

    public async Task<Result<TunnelProviderSnapshot>> HandleAsync(StopTunnelCommand command, CallContext context)
    {
        try
        {
            var result = await _client.StopAsync(command.ProviderName, context.CancellationToken).ConfigureAwait(false);
            return Result<TunnelProviderSnapshot>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<TunnelProviderSnapshot>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="RestartTunnelCommand"/>.</summary>
internal sealed class RestartTunnelCommandHandler : ICommandHandler<RestartTunnelCommand, TunnelProviderSnapshot>
{
    private readonly ITunnelApiClient _client;

    public RestartTunnelCommandHandler(ITunnelApiClient client) => _client = client;

    public async Task<Result<TunnelProviderSnapshot>> HandleAsync(RestartTunnelCommand command, CallContext context)
    {
        try
        {
            var result = await _client.RestartAsync(command.ProviderName, context.CancellationToken).ConfigureAwait(false);
            return Result<TunnelProviderSnapshot>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<TunnelProviderSnapshot>.Failure(ex);
        }
    }
}
