using McpServer.Support.Mcp.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-POOL-SEED: Seeds the agent pool with running instances for every
/// (workspace × agent definition) combination on startup.
/// Must be registered after <see cref="WorkspaceProcessManager"/> to ensure workspaces are initialized.
/// </summary>
public sealed class AgentPoolSeedService : IHostedService
{
    private readonly IAgentPoolService _pool;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<AgentPoolOptions> _poolOptions;
    private readonly ILogger<AgentPoolSeedService> _logger;

    /// <summary>Initializes a new instance of the <see cref="AgentPoolSeedService"/> class.</summary>
    public AgentPoolSeedService(
        IAgentPoolService pool,
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<AgentPoolOptions> poolOptions,
        ILogger<AgentPoolSeedService> logger)
    {
        _pool = pool ?? throw new ArgumentNullException(nameof(pool));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _poolOptions = poolOptions ?? throw new ArgumentNullException(nameof(poolOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_poolOptions.CurrentValue.Enabled)
        {
            _logger.LogInformation("Agent pool is disabled; skipping startup seed.");
            return;
        }

        var agents = _poolOptions.CurrentValue.Agents
            .Where(a => !string.IsNullOrWhiteSpace(a.AgentName))
            .ToList();

        if (agents.Count == 0)
        {
            _logger.LogDebug("No agent definitions configured; skipping seed.");
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var workspaceService = scope.ServiceProvider.GetRequiredService<IWorkspaceService>();
        var workspaces = await workspaceService.ListAsync(cancellationToken).ConfigureAwait(false);
        var enabledWorkspaces = workspaces.Items.Where(w => w.IsEnabled).ToList();

        if (enabledWorkspaces.Count == 0)
        {
            _logger.LogDebug("No enabled workspaces; skipping agent pool seed.");
            return;
        }

        _logger.LogInformation(
            "Seeding agent pool: {AgentCount} definitions × {WorkspaceCount} workspaces",
            agents.Count,
            enabledWorkspaces.Count);

        var tasks = enabledWorkspaces
            .Select(ws => _pool.SeedWorkspaceAgentsAsync(ws.WorkspacePath, cancellationToken))
            .ToArray();

        await Task.WhenAll(tasks).ConfigureAwait(false);

        _logger.LogInformation("Agent pool seed complete.");
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
