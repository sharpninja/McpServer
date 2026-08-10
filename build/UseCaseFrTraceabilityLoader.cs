using Microsoft.Data.Sqlite;

/// <summary>
/// FR-MCP-USECASE-010: Loads UseCaseFrLinks data for Nuke ValidateTraceability and runs the
/// shared Realizes coverage algorithm via <see cref="TraceabilityValidator.BuildUseCaseFrFindings"/>.
/// Aligns with <c>UseCaseFrCoverageCore</c> / <c>UseCaseTraceabilityGate</c>.
/// </summary>
static class UseCaseFrTraceabilityLoader
{
    /// <summary>
    /// When <paramref name="sqlitePath"/> exists and contains Use Case tables, returns Realizes findings.
    /// When the DB or tables are absent, returns an empty list (docs-only validation remains).
    /// </summary>
    public static IReadOnlyList<string> LoadFindingsFromSqlite(string? sqlitePath)
    {
        if (string.IsNullOrWhiteSpace(sqlitePath) || !File.Exists(sqlitePath))
            return Array.Empty<string>();

        try
        {
            using var conn = new SqliteConnection($"Data Source={sqlitePath}");
            conn.Open();
            if (!TableExists(conn, "UseCases") || !TableExists(conn, "UseCaseFrLinks") || !TableExists(conn, "Requirements"))
                return Array.Empty<string>();

            var useCases = new List<(long UseCaseId, string Title)>();
            using (var cmd = conn.CreateCommand())
            {
                // Soft-delete columns may be present; prefer excluding deleted when IsDeleted exists.
                cmd.CommandText = ColumnExists(conn, "UseCases", "IsDeleted")
                    ? "SELECT UseCaseId, Title FROM UseCases WHERE IFNULL(IsDeleted, 0) = 0"
                    : "SELECT UseCaseId, Title FROM UseCases";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    useCases.Add((r.GetInt64(0), r.IsDBNull(1) ? string.Empty : r.GetString(1)));
            }

            var frIds = new List<string>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = ColumnExists(conn, "Requirements", "IsDeleted")
                    ? "SELECT Id FROM Requirements WHERE Kind = 'fr' AND IFNULL(IsDeleted, 0) = 0"
                    : "SELECT Id FROM Requirements WHERE Kind = 'fr'";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    frIds.Add(r.GetString(0));
            }

            var links = new List<(long UseCaseId, string FrId)>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = ColumnExists(conn, "UseCaseFrLinks", "IsDeleted")
                    ? "SELECT UseCaseId, FrId FROM UseCaseFrLinks WHERE LinkType = 'Realizes' AND IFNULL(IsDeleted, 0) = 0"
                    : "SELECT UseCaseId, FrId FROM UseCaseFrLinks WHERE LinkType = 'Realizes'";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    links.Add((r.GetInt64(0), r.GetString(1)));
            }

            return TraceabilityValidator.BuildUseCaseFrFindings(useCases, frIds, links);
        }
        catch (Exception)
        {
            // Traceability docs validation must not crash when DB is locked or schema is mid-migration.
            return Array.Empty<string>();
        }
    }

    /// <summary>
    /// Resolves a candidate mcp.db path under the repo for optional UseCaseFrLinks validation.
    /// </summary>
    public static string? ResolveDefaultSqlitePath(string rootDirectory)
    {
        var candidates = new[]
        {
            Path.Combine(rootDirectory, "src", "McpServer.Support.Mcp", "mcp.db"),
            Path.Combine(rootDirectory, "mcp.db"),
        };
        return candidates.FirstOrDefault(File.Exists);
    }

    static bool TableExists(SqliteConnection conn, string name)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=$n";
        cmd.Parameters.AddWithValue("$n", name);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    static bool ColumnExists(SqliteConnection conn, string table, string column)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info({table})";
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            if (string.Equals(r.GetString(1), column, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
