using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using McpServer.Support.Mcp.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Monitors running agent processes and applies configured restart policies.
/// </summary>
public sealed class AgentHealthMonitorService : BackgroundService
{
    private readonly IAgentProcessManager _agentProcessManager;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AgentHealthMonitorService> _logger;
    private readonly AgentProcessManagerOptions _options;
    private readonly ConcurrentDictionary<string, int> _restartCounts = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentHealthMonitorService"/> class.
    /// </summary>
    public AgentHealthMonitorService(
        IAgentProcessManager agentProcessManager,
        IServiceScopeFactory scopeFactory,
        IOptions<AgentProcessManagerOptions> options,
        ILogger<AgentHealthMonitorService> logger)
    {
        _agentProcessManager = agentProcessManager;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = Math.Max(5, _options.HealthCheckIntervalSeconds);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await MonitorOnceAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Agent health monitor iteration failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task MonitorOnceAsync(CancellationToken cancellationToken)
    {
        var runningAgents = await _agentProcessManager.ListRunningAsync(null, cancellationToken).ConfigureAwait(false);
        await using var scope = _scopeFactory.CreateAsyncScope();
        var agentService = scope.ServiceProvider.GetRequiredService<IAgentService>();

        foreach (var info in runningAgents)
        {
            if (info.Status == Models.AgentProcessStatus.Running || info.Status == Models.AgentProcessStatus.Starting)
                continue;

            var statusKey = BuildKey(info.WorkspacePath, info.AgentId);
            if (info.Status == Models.AgentProcessStatus.Stopped && info.ExitCode.GetValueOrDefault() == 0)
            {
                _restartCounts.TryRemove(statusKey, out _);
                continue;
            }

            var config = await agentService.GetWorkspaceAgentAsync(info.WorkspacePath, info.AgentId, cancellationToken).ConfigureAwait(false);
            if (config is null)
                continue;

            var restartPolicy = string.IsNullOrWhiteSpace(config.RestartPolicy) ? "never" : config.RestartPolicy.Trim();
            if (string.Equals(restartPolicy, "never", StringComparison.OrdinalIgnoreCase))
                continue;

            if (string.Equals(restartPolicy, "on-failure", StringComparison.OrdinalIgnoreCase) && info.ExitCode.GetValueOrDefault() == 0)
                continue;

            var restartCount = _restartCounts.AddOrUpdate(statusKey, 1, (_, current) => current + 1);
            if (restartCount > Math.Max(0, _options.MaxRestarts))
            {
                _logger.LogWarning(
                    "Skipping restart for agent {AgentId} in {WorkspacePath} because max restarts ({MaxRestarts}) was exceeded.",
                    info.AgentId,
                    info.WorkspacePath,
                    _options.MaxRestarts);
                continue;
            }

            var backoffBaseSeconds = Math.Max(0, _options.RestartBackoffBaseSeconds);
            var backoffSeconds = backoffBaseSeconds * (int)Math.Pow(2, restartCount - 1);
            _logger.LogWarning(
                "Restarting agent {AgentId} in {WorkspacePath} after exit status {Status} and exit code {ExitCode}. Attempt {Attempt}. Backoff {BackoffSeconds}s.",
                info.AgentId,
                info.WorkspacePath,
                info.Status,
                info.ExitCode,
                restartCount,
                backoffSeconds);

            if (backoffSeconds > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(backoffSeconds), cancellationToken).ConfigureAwait(false);
            }

            try
            {
                await agentService.LaunchAgentAsync(info.WorkspacePath, info.AgentId, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to restart agent {AgentId} in {WorkspacePath}.", info.AgentId, info.WorkspacePath);
            }
        }
    }

    private static string BuildKey(string workspacePath, string agentId)
        => $"{workspacePath}::{agentId}";
}
