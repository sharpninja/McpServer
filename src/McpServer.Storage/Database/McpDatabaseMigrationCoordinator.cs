using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace McpServer.Support.Mcp.Storage.Database;

/// <summary>
/// Applies provider-owned migrations while preserving compatibility with databases that
/// were previously tracked by the legacy shared migration history.
/// </summary>
public static class McpDatabaseMigrationCoordinator
{
    /// <summary>
    /// Applies the configured provider-owned migration chain to the supplied database context.
    /// </summary>
    /// <param name="dbContext">Database context to migrate.</param>
    /// <param name="providerOptions">Resolved provider options for the current runtime.</param>
    /// <param name="cancellationToken">Cancellation token for async database work.</param>
    /// <returns>A task that completes when migration processing finishes.</returns>
    public static async Task ApplyMigrationsAsync(
        McpDbContext dbContext,
        McpDatabaseProviderOptions providerOptions,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(providerOptions);

        await AdoptLegacyHistoryAsync(dbContext, cancellationToken).ConfigureAwait(false);
        await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task AdoptLegacyHistoryAsync(McpDbContext dbContext, CancellationToken cancellationToken)
    {
        var definedMigrations = dbContext.GetService<IMigrationsAssembly>().Migrations.Keys.ToArray();
        if (definedMigrations.Length == 0)
        {
            return;
        }

        var baselineMigrationId = definedMigrations[0];
        var appliedMigrations = (await dbContext.Database.GetAppliedMigrationsAsync(cancellationToken).ConfigureAwait(false)).ToArray();
        if (appliedMigrations.Length == 0
            || appliedMigrations.Contains(baselineMigrationId, StringComparer.OrdinalIgnoreCase)
            || !appliedMigrations.Any(applied => !definedMigrations.Contains(applied, StringComparer.OrdinalIgnoreCase)))
        {
            return;
        }

        var historyRepository = dbContext.GetService<IHistoryRepository>();
        var insertScript = historyRepository.GetInsertScript(
            new HistoryRow(baselineMigrationId, GetProductVersion()));

        await dbContext.Database.ExecuteSqlRawAsync(insertScript, cancellationToken).ConfigureAwait(false);
    }

    private static string GetProductVersion()
        => typeof(DbContext).Assembly.GetName().Version?.ToString() ?? "9.0.0";
}
