using McpServer.Support.Mcp.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-QBSEED-001 and TR-MCP-QBSEED-002: Provisions the global QuadBrain slots declared in
/// <see cref="BrainSlotOptions.Slots"/> into the durable registry at startup. The seeder is gated on
/// <see cref="BrainSlotOptions.ExecutionEnabled"/>, is idempotent (each slot is upserted by id), and never
/// aborts host startup when a single slot definition is invalid or the database is not yet ready.
/// </summary>
/// <remarks>
/// QuadBrain brain-slot definitions are GLOBAL (one quad shared by every workspace and session; see
/// <see cref="McpDbContext"/>'s global query filter and stamping for <c>BrainSlotDefinitionEntity</c>), so the
/// seeder provisions a single global set regardless of workspace context.
/// </remarks>
public sealed class BrainSlotStartupSeeder : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<BrainSlotOptions> _options;
    private readonly ILogger<BrainSlotStartupSeeder> _logger;

    /// <summary>Initializes a new instance of the <see cref="BrainSlotStartupSeeder"/> class.</summary>
    /// <param name="scopeFactory">Scope factory for resolving the scoped registry service.</param>
    /// <param name="options">Brain-slot runtime options carrying the seed definitions.</param>
    /// <param name="logger">Diagnostic logger.</param>
    public BrainSlotStartupSeeder(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<BrainSlotOptions> options,
        ILogger<BrainSlotStartupSeeder> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await SeedAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Brain-slot startup provisioning failed; continuing startup without provisioning.");
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>Provisions the configured global slots; surface for unit tests.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of slot upserts applied.</returns>
    internal async Task<int> SeedAsync(CancellationToken cancellationToken)
    {
        var options = _options.CurrentValue;
        if (!options.ExecutionEnabled)
        {
            _logger.LogDebug("Brain-slot execution is disabled; skipping startup provisioning.");
            return 0;
        }

        var slots = options.Slots
            .Where(slot => slot is not null && !string.IsNullOrWhiteSpace(slot.SlotId))
            .ToList();
        if (slots.Count == 0)
        {
            _logger.LogDebug("No brain-slot definitions configured; skipping startup provisioning.");
            return 0;
        }

        using var scope = _scopeFactory.CreateScope();
        var registry = scope.ServiceProvider.GetRequiredService<IBrainSlotRegistryService>();

        var applied = 0;
        foreach (var slot in slots)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await registry.UpsertAsync(slot.SlotId, slot.ToUpsertRequest(), cancellationToken).ConfigureAwait(false);
                applied++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to provision brain slot {SlotId} ({Role}); continuing with remaining slots.",
                    slot.SlotId,
                    slot.Role);
            }
        }

        _logger.LogInformation(
            "Brain-slot startup provisioning complete: {Applied}/{Total} global slots applied.",
            applied,
            slots.Count);
        return applied;
    }
}
