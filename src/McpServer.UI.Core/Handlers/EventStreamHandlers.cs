using McpServer.Cqrs;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.Handlers;

/// <summary>Handles <see cref="SubscribeToEventsQuery"/>.</summary>
internal sealed class SubscribeToEventsQueryHandler : IQueryHandler<SubscribeToEventsQuery, IAsyncEnumerable<ChangeEventItem>>
{
    private readonly IEventStreamApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<SubscribeToEventsQueryHandler> _logger;

    public SubscribeToEventsQueryHandler(
        IEventStreamApiClient client,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<SubscribeToEventsQueryHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<IAsyncEnumerable<ChangeEventItem>>> HandleAsync(SubscribeToEventsQuery query, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.EventsSubscribe))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.EventsSubscribe);
            return Result<IAsyncEnumerable<ChangeEventItem>>.Failure(
                string.IsNullOrWhiteSpace(requiredRole)
                    ? "Permission denied."
                    : $"Permission denied: requires {requiredRole}.");
        }

        try
        {
            var stream = await _client.SubscribeAsync(query.Category, context.CancellationToken).ConfigureAwait(false);
            return Result<IAsyncEnumerable<ChangeEventItem>>.Success(stream);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<IAsyncEnumerable<ChangeEventItem>>.Failure(ex);
        }
    }
}
