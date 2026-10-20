using System.Data;
using System.Data.Common;
using QBrainAi.Client;

namespace QBrainAi.Support.Mcp.Storage.Database;

/// <summary>TR-MCP-PATHNORM-001: refuses legacy storage before EF converts query parameters.</summary>
public static class WorkspacePathRekeyGuard
{
    /// <summary>Policy A: reuse the Windows lexical policy while preserving non-Windows identifiers verbatim.</summary>
    public static string Normalize(string value)
    {
        var windows = value.StartsWith(@"\\", StringComparison.Ordinal)
            || value.StartsWith("//", StringComparison.Ordinal)
            || (value.Length >= 3 && char.IsAsciiLetter(value[0]) && value[1] == ':' && value[2] is '/' or '\\');
        return windows
            ? WorkspaceIdentityPath.NormalizeLexicalPath(value, WorkspacePathPlatform.Windows)
            : value;
    }

    /// <summary>Reads raw persisted ids; no EF predicate, migration, backfill, or data mutation is performed.</summary>
    public static async Task ValidateStartupAsync(DbConnection connection, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        cancellationToken.ThrowIfCancellationRequested();
        var opened = connection.State != ConnectionState.Open;
        if (opened && connection is Microsoft.Data.Sqlite.SqliteConnection sqlite)
        {
            var settings = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(sqlite.ConnectionString);
            // Do not create a plain-file database merely to inspect it. URI and DataDirectory forms
            // are resolved by the provider; the migration coordinator checks provider existence first.
            if (settings.Mode != Microsoft.Data.Sqlite.SqliteOpenMode.Memory
                && settings.DataSource.Length > 0 && settings.DataSource != ":memory:"
                && !settings.DataSource.StartsWith("file:", StringComparison.OrdinalIgnoreCase)
                && !settings.DataSource.Contains('|') && !File.Exists(settings.DataSource))
                return;
        }
        try
        {
            if (opened) await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var exists = connection.CreateCommand();
            var kind = connection.GetType().Name;
            exists.CommandText = kind switch
            {
                "SqliteConnection" => "SELECT count(*) FROM sqlite_master WHERE type='table' AND name='Workspaces'",
                "SqlConnection" => "SELECT count(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA='dbo' AND TABLE_NAME='Workspaces'",
                "NpgsqlConnection" => "SELECT count(*) FROM information_schema.tables WHERE table_schema=current_schema() AND table_name='Workspaces'",
                _ => throw new NotSupportedException("Workspace rekey guard requires a supported relational provider.")
            };
            if (Convert.ToInt64(await exists.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false)) == 0)
                return;
            await using var query = connection.CreateCommand();
            query.CommandText = "SELECT DISTINCT \"WorkspaceId\" FROM \"Workspaces\"";
            await using var reader = await query.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                if (!reader.IsDBNull(0) && reader.GetString(0) is var key
                    && !string.Equals(key, Normalize(key), StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "Legacy workspace ids detected. Startup is refused before canonical workspace queries. "
                        + "Back up the offline database and run --workspace-path-rekey; "
                        + "rehearse --dry-run --copy-database <copy> first.");
            }
        }
        finally
        {
            if (opened && connection.State == ConnectionState.Open)
                await connection.CloseAsync().ConfigureAwait(false);
        }
    }
}
