using System.Data.Common;
using System.Data;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.Storage.Database;

/// <summary>
/// Validates native at-rest encryption state for the resolved database provider.
/// </summary>
public static class McpDatabaseEncryptionCoordinator
{
    /// <summary>
    /// Validates that the live database encryption state matches the configured encryption intent.
    /// </summary>
    /// <param name="dbContext">Database context used for provider-specific validation queries.</param>
    /// <param name="runtimeOptions">Resolved runtime provider and encryption settings.</param>
    /// <param name="cancellationToken">Cancellation token for async database work.</param>
    /// <returns>A task that completes when the live state matches the configured expectation.</returns>
    public static async Task ValidateAsync(
        McpDbContext dbContext,
        McpDatabaseRuntimeOptions runtimeOptions,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(runtimeOptions);

        switch (runtimeOptions.ProviderOptions.ProviderKind)
        {
            case McpDatabaseProviderKind.Sqlite:
                ValidateSqlite(runtimeOptions.EncryptionOptions);
                return;
            case McpDatabaseProviderKind.PostgreSql:
                await ValidatePostgreSqlAsync(dbContext, runtimeOptions.EncryptionOptions, cancellationToken).ConfigureAwait(false);
                return;
            case McpDatabaseProviderKind.SqlServer:
                await ValidateSqlServerAsync(dbContext, runtimeOptions.EncryptionOptions, cancellationToken).ConfigureAwait(false);
                return;
            default:
                throw new InvalidOperationException(
                    $"Unsupported MCP database provider '{runtimeOptions.ProviderOptions.ProviderName}' for encryption validation.");
        }
    }

    private static void ValidateSqlite(McpDatabaseEncryptionOptions options)
    {
        if (!options.Enabled)
        {
            return;
        }

        throw new InvalidOperationException(
            "SQLite at-rest encryption is configured, but this runtime still requires a SEE-enabled native SQLite build and an explicit maintenance transition workflow. Encrypted SQLite startup is blocked until that native SEE runtime is provisioned.");
    }

    private static async Task ValidatePostgreSqlAsync(
        McpDbContext dbContext,
        McpDatabaseEncryptionOptions options,
        CancellationToken cancellationToken)
    {
        await using var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }

        var extensionInstalled = await ExecuteScalarAsync<bool>(
            connection,
            """
            SELECT EXISTS (
                SELECT 1
                FROM pg_extension
                WHERE extname = 'pg_tde');
            """,
            cancellationToken).ConfigureAwait(false);

        if (options.Enabled && !extensionInstalled)
        {
            throw new InvalidOperationException(
                "PostgreSQL at-rest encryption is enabled, but the connected database does not expose the pg_tde extension. Provision Percona Server for PostgreSQL with pg_tde before restarting.");
        }

        if (!extensionInstalled)
        {
            return;
        }

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT schemaname, tablename,
                   pg_tde_is_encrypted(format('%I.%I', schemaname, tablename)::regclass) AS is_encrypted
            FROM pg_tables
            WHERE schemaname = 'public'
              AND tablename <> '__EFMigrationsHistory';
            """;

        var hasUnencryptedAppTable = false;
        var hasEncryptedAppTable = false;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var isEncrypted = reader.GetBoolean(2);
            hasEncryptedAppTable |= isEncrypted;
            hasUnencryptedAppTable |= !isEncrypted;
        }

        if (options.Enabled && hasUnencryptedAppTable)
        {
            throw new InvalidOperationException(
                "PostgreSQL at-rest encryption is enabled, but one or more application tables are not yet using pg_tde. Run the documented table-rewrite transition procedure before restarting.");
        }

        if (!options.Enabled && hasEncryptedAppTable)
        {
            throw new InvalidOperationException(
                "PostgreSQL at-rest encryption is disabled in configuration, but encrypted pg_tde tables are still present. Run the documented decrypt transition procedure before restarting.");
        }
    }

    private static async Task ValidateSqlServerAsync(
        McpDbContext dbContext,
        McpDatabaseEncryptionOptions options,
        CancellationToken cancellationToken)
    {
        await using var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }

        var encryptionState = await ExecuteScalarAsync<int?>(
            connection,
            """
            SELECT TOP (1) dek.encryption_state
            FROM sys.dm_database_encryption_keys AS dek
            WHERE dek.database_id = DB_ID();
            """,
            cancellationToken).ConfigureAwait(false);

        var isEncrypted = encryptionState == 3;
        if (options.Enabled && !isEncrypted)
        {
            throw new InvalidOperationException(
                "SQL Server at-rest encryption is enabled in configuration, but the current database is not fully encrypted with TDE. Complete the documented TDE enablement workflow before restarting.");
        }

        if (!options.Enabled && isEncrypted)
        {
            throw new InvalidOperationException(
                "SQL Server at-rest encryption is disabled in configuration, but the current database is still protected by TDE. Complete the documented TDE disable workflow before restarting.");
        }

        if (options.Enabled && string.IsNullOrWhiteSpace(options.SqlServerCertificateName))
        {
            throw new InvalidOperationException(
                "SQL Server at-rest encryption is enabled, but no TDE certificate name was configured. Set the SQL Server TDE certificate configuration before restarting.");
        }
    }

    private static async Task<T?> ExecuteScalarAsync<T>(
        DbConnection connection,
        string commandText,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        if (result is null or DBNull)
        {
            return default;
        }

        return (T)Convert.ChangeType(result, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
    }
}
