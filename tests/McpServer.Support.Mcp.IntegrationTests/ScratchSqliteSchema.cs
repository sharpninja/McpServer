using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Database;

namespace McpServer.Support.Mcp.IntegrationTests;

/// <summary>
/// Scratch-host SQLite contract: apply the same provider-owned migration coordinator
/// the production host uses, then refuse to start if SessionLogs.AgentExecutablePath is missing.
/// </summary>
public static class ScratchSqliteSchema
{
    /// <summary>Required SessionLogs column that backfill and session-log reads project.</summary>
    public const string AgentExecutablePathColumn = "AgentExecutablePath";

    /// <summary>Applies SqliteMigrations and verifies the session-log schema contract.</summary>
    public static async Task ApplyAndVerifyAsync(string databasePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var connectionString = $"Data Source={databasePath}";
        var providerOptions = McpDatabaseProviderFactory.CreateOptions(
            "sqlite",
            connectionString,
            "McpServer.Storage.SqliteMigrations");
        var options = new DbContextOptionsBuilder<McpDbContext>();
        McpDatabaseProviderFactory.Configure(options, providerOptions);
        await using (var db = new McpDbContext(options.Options))
        {
            await McpDatabaseMigrationCoordinator.ApplyMigrationsAsync(db, providerOptions, cancellationToken).ConfigureAwait(false);
        }

        SqliteConnection.ClearAllPools();
        await RepairLegacySessionLogHeaderColumnsAsync(databasePath, cancellationToken).ConfigureAwait(false);
        await EnsureAgentExecutablePathAsync(databasePath, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// SqliteMigrations designers mention AgentExecutablePath but no provider Up() adds it.
    /// Scratch hosts must patch that contract before backfill queries SessionLogs.
    /// </summary>
    public static async Task RepairLegacySessionLogHeaderColumnsAsync(string databasePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        var columns = new (string Name, string Ddl)[]
        {
            ("AgentSessionId", "ALTER TABLE SessionLogs ADD COLUMN AgentSessionId TEXT;"),
            ("AgentSessionTranscriptFile", "ALTER TABLE SessionLogs ADD COLUMN AgentSessionTranscriptFile TEXT;"),
            ("AgentExecutablePath", "ALTER TABLE SessionLogs ADD COLUMN AgentExecutablePath TEXT;"),
            ("AgentExecutableVersion", "ALTER TABLE SessionLogs ADD COLUMN AgentExecutableVersion TEXT;"),
        };

        await using var connection = new SqliteConnection($"Data Source={databasePath}");
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        foreach (var (name, ddl) in columns)
        {
            if (await ColumnExistsAsync(connection, name, cancellationToken).ConfigureAwait(false))
                continue;
            await using var command = connection.CreateCommand();
            command.CommandText = ddl;
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>True when SessionLogs.AgentExecutablePath exists.</summary>
    public static async Task<bool> HasAgentExecutablePathAsync(string databasePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        await using var connection = new SqliteConnection($"Data Source={databasePath}");
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return await ColumnExistsAsync(connection, AgentExecutablePathColumn, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<bool> ColumnExistsAsync(SqliteConnection connection, string columnName, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM pragma_table_info('SessionLogs') WHERE name = $name;";
        command.Parameters.AddWithValue("$name", columnName);
        var count = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false), System.Globalization.CultureInfo.InvariantCulture);
        return count > 0;
    }

    /// <summary>Fails closed when the scratch database is missing the session-log column.</summary>
    public static async Task EnsureAgentExecutablePathAsync(string databasePath, CancellationToken cancellationToken = default)
    {
        if (!await HasAgentExecutablePathAsync(databasePath, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException(
                $"Scratch SQLite schema at '{databasePath}' is missing SessionLogs.{AgentExecutablePathColumn}. Apply McpServer.Storage.SqliteMigrations before starting the host.");
        }
    }
}
