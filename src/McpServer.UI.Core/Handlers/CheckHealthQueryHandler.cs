using McpServer.Cqrs;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.Handlers;

/// <summary>
/// Handles <see cref="CheckHealthQuery"/> using the host-provided health API client.
/// </summary>
internal sealed class CheckHealthQueryHandler : IQueryHandler<CheckHealthQuery, HealthSnapshot>
{
    private readonly IHealthApiClient _healthApiClient;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<CheckHealthQueryHandler> _logger;


    public CheckHealthQueryHandler(IHealthApiClient healthApiClient,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<CheckHealthQueryHandler> logger)
    {
        _logger = logger;
        _healthApiClient = healthApiClient;
        _authorizationPolicy = authorizationPolicy;
    }

    public async Task<Result<HealthSnapshot>> HandleAsync(CheckHealthQuery query, CallContext context)
    {
        // Health is a viewer-level action by default. Hosts can override if needed.
        if (!_authorizationPolicy.CanExecuteAction("health.check"))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole("health.check");
            return Result<HealthSnapshot>.Failure(
                string.IsNullOrWhiteSpace(requiredRole)
                    ? "Permission denied."
                    : $"Permission denied: requires {requiredRole}.");
        }

        try
        {
            var snapshot = await _healthApiClient.CheckHealthAsync(context.CancellationToken).ConfigureAwait(true);
            return Result<HealthSnapshot>.Success(snapshot);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<HealthSnapshot>.Failure(ex);
        }
    }
}
