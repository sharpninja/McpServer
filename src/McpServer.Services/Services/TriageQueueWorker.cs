using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-TRIAGE-003: Background worker that sweeps due triage groups after their quiet window.
/// </summary>
public sealed class TriageQueueWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<TriageOptions> options,
    ILogger<TriageQueueWorker> logger)
    : BackgroundService
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var triage = scope.ServiceProvider.GetRequiredService<ITriageService>();
                await triage.ProcessDueGroupsAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Triage queue sweep failed.");
            }

            var interval = options.Value.SweepInterval <= TimeSpan.Zero
                ? TimeSpan.FromMinutes(1)
                : options.Value.SweepInterval;
            await Task.Delay(interval, stoppingToken).ConfigureAwait(false);
        }
    }
}
