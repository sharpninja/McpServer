using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.Storage;

/// <summary>
/// FR-MCP-TRIAGESCHEMA-001 / TR-MCP-TRIAGESCHEMA-001: fail-closed probe for the four
/// SessionLogs agent-header columns added by provider migrations
/// Sqlite <c>20260818205751_AddSessionLogTagsAndAgentSessionHeaders</c>,
/// SqlServer <c>20260818205807_AddSessionLogTagsAndAgentSessionHeaders</c>, and
/// Postgres <c>20260818205822_AddSessionLogTagsAndAgentSessionHeaders</c>.
/// </summary>
public static class SessionLogSchemaGuard
{
    /// <summary>Named error text for a missing AgentSession header schema.</summary>
    public const string PendingMigrationMessage =
        "SessionLogs schema is missing AgentSession header columns (pending-migration Sqlite 20260818205751_AddSessionLogTagsAndAgentSessionHeaders, SqlServer 20260818205807_AddSessionLogTagsAndAgentSessionHeaders, Postgres 20260818205822_AddSessionLogTagsAndAgentSessionHeaders).";

    private static readonly string[] RequiredColumns =
    [
        "AgentSessionId",
        "AgentSessionTranscriptFile",
        "AgentExecutablePath",
        "AgentExecutableVersion",
    ];

    private static readonly object CacheLock = new();
    private static readonly Dictionary<string, bool> Cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Throws <see cref="SessionLogSchemaPendingMigrationException"/> when SessionLogs lacks
    /// the four nullable agent-header columns. In-memory providers are treated as ready.
    /// </summary>
    /// <param name="db">The workspace database.</param>
    public static void EnsureAgentSessionHeaderColumns(McpDbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);

        if (IsInMemory(db))
            return;

        var cacheKey = db.Database.GetConnectionString() ?? db.Database.ProviderName ?? "default";
        lock (CacheLock)
        {
            if (Cache.TryGetValue(cacheKey, out var ready) && ready)
                return;
        }

        if (!Probe(db))
            throw new SessionLogSchemaPendingMigrationException();

        lock (CacheLock)
        {
            Cache[cacheKey] = true;
        }
    }

    /// <summary>Clears the probe cache (tests only).</summary>
    public static void ResetCache()
    {
        lock (CacheLock)
            Cache.Clear();
    }

    /// <summary>Returns true when the four agent-header columns exist on SessionLogs.</summary>
    /// <param name="db">The database to probe.</param>
    /// <returns>True when the columns are present or the provider is in-memory.</returns>
    public static bool Probe(McpDbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);
        if (IsInMemory(db))
            return true;

        try
        {
            var provider = db.Database.ProviderName ?? string.Empty;
            if (provider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
                return ProbeSqlite(db);
            if (provider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
                return ProbeSqlServer(db);
            if (provider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase)
                || provider.Contains("Postgre", StringComparison.OrdinalIgnoreCase))
            {
                return ProbePostgres(db);
            }

            return ProbeSqlite(db);
        }
        catch (Exception ex) when (IsMissingColumn(ex))
        {
            return false;
        }
    }

    private static bool IsInMemory(McpDbContext db)
    {
        var provider = db.Database.ProviderName ?? string.Empty;
        return provider.Contains("InMemory", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ProbeSqlite(McpDbContext db)
    {
        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State != System.Data.ConnectionState.Open;
        if (shouldClose)
            connection.Open();

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA table_info('SessionLogs');";
            using var reader = command.ExecuteReader();
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            while (reader.Read())
            {
                if (!reader.IsDBNull(1))
                    names.Add(reader.GetString(1));
            }

            return RequiredColumns.All(names.Contains);
        }
        finally
        {
            if (shouldClose)
                connection.Close();
        }
    }

    private static bool ProbeSqlServer(McpDbContext db)
    {
        foreach (var column in RequiredColumns)
        {
            var count = CountSql(
                db,
                "SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID(N'SessionLogs') AND name = {0}",
                column);
            if (count == 0)
                return false;
        }

        return true;
    }

    private static bool ProbePostgres(McpDbContext db)
    {
        foreach (var column in RequiredColumns)
        {
            var count = CountSql(
                db,
                """
                SELECT COUNT(*) FROM information_schema.columns
                WHERE table_name = 'SessionLogs' AND column_name = {0}
                """,
                column);
            if (count == 0)
                return false;
        }

        return true;
    }

    private static int CountSql(McpDbContext db, string sql, string column)
    {
        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State != System.Data.ConnectionState.Open;
        if (shouldClose)
            connection.Open();

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql.Replace("{0}", "@p0", StringComparison.Ordinal);
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@p0";
            parameter.Value = column;
            command.Parameters.Add(parameter);
            var scalar = command.ExecuteScalar();
            return Convert.ToInt32(scalar, System.Globalization.CultureInfo.InvariantCulture);
        }
        finally
        {
            if (shouldClose)
                connection.Close();
        }
    }

    private static bool IsMissingColumn(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException!)
        {
            if (current.Message.Contains("Invalid column name", StringComparison.OrdinalIgnoreCase)
                || current.Message.Contains("no such column", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}

/// <summary>
/// FR-MCP-TRIAGESCHEMA-001: named fail-closed error when SessionLogs is missing
/// the AgentSession header columns.
/// </summary>
public sealed class SessionLogSchemaPendingMigrationException : InvalidOperationException
{
    /// <summary>Initializes the named pending-migration error.</summary>
    public SessionLogSchemaPendingMigrationException()
        : base(SessionLogSchemaGuard.PendingMigrationMessage)
    {
    }
}
