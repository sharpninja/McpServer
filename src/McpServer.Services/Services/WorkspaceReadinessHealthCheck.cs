using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-133 / TR-MCP-HEALTH-002: readiness check for the subsystem that gates <c>/mcpserver/*</c>.
/// Reports unhealthy when workspace registry or auth-token readiness would make data routes fail startup auth.
/// </summary>
public sealed class WorkspaceReadinessHealthCheck : IHealthCheck
{
    private readonly WorkspaceTokenService _tokenService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WorkspaceReadinessHealthCheck> _logger;

    /// <summary>Initializes a new instance of the <see cref="WorkspaceReadinessHealthCheck"/> class.</summary>
    /// <param name="tokenService">Per-workspace token service.</param>
    /// <param name="scopeFactory">Scope factory used to resolve scoped workspace services.</param>
    /// <param name="logger">Logger.</param>
    public WorkspaceReadinessHealthCheck(
        WorkspaceTokenService tokenService,
        IServiceScopeFactory scopeFactory,
        ILogger<WorkspaceReadinessHealthCheck> logger)
    {
        _tokenService = tokenService;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!_tokenService.IsInitialized)
            return HealthCheckResult.Unhealthy("Workspace auth-token subsystem has not been initialized.");

        WorkspaceListResult list;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var workspaceService = scope.ServiceProvider.GetRequiredService<IWorkspaceService>();
            list = await workspaceService.ListAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Workspace readiness check could not enumerate workspaces.");
            return HealthCheckResult.Unhealthy("Workspace registry is unavailable.", ex);
        }

        var primary = list.Items.FirstOrDefault(w => w.IsPrimary && w.IsEnabled)
                   ?? list.Items.FirstOrDefault(w => w.IsEnabled);
        if (primary is null || string.IsNullOrWhiteSpace(primary.WorkspacePath))
            return HealthCheckResult.Unhealthy("No enabled workspace is registered.");

        if (_tokenService.GetToken(primary.WorkspacePath) is null)
            return HealthCheckResult.Unhealthy($"Primary workspace '{primary.Name}' has no seeded auth token.");

        return HealthCheckResult.Healthy("Workspace registry and auth tokens are ready.");
    }
}
