using Microsoft.Data.Sqlite;

namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>
/// TR-MCP-DB-001: raw-SQL workspace-key assertions for SQLite data-migration tests. A data migration copies the parent
/// workspace key into every backfilled child row (the child foreign key to Workspaces). EF query parameters compared
/// with workspace keys are canonicalized by WorkspaceKeyConversion, so a legacy key (for example a backslash Windows
/// spelling) is only observable through SQL that bypasses EF.
/// </summary>
internal static class MigrationKeyAssertions
{
    /// <summary>
    /// Asserts that the only non-global workspace row has exactly <paramref name="expectedKey"/> as its WorkspaceId,
    /// and that <paramref name="childTable"/> has rows whose WorkspaceId values are all exactly that key.
    /// </summary>
    /// <param name="connection">Open connection to the migrated database.</param>
    /// <param name="childTable">Backfilled child table that carries a WorkspaceId column.</param>
    /// <param name="expectedKey">The workspace key seeded before the migration, compared ordinally.</param>
    internal static void AssertChildRowsCarryWorkspaceKey(SqliteConnection connection, string childTable, string expectedKey)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrWhiteSpace(childTable);
        ArgumentNullException.ThrowIfNull(expectedKey);

        var parentKeys = ReadStrings(connection, "SELECT \"WorkspaceId\" FROM \"Workspaces\" WHERE \"WorkspaceId\" <> '' ORDER BY 1;");
        Assert.Equal([expectedKey], parentKeys);

        var childKeys = ReadStrings(connection, $"SELECT DISTINCT \"WorkspaceId\" FROM \"{childTable}\" ORDER BY 1;");
        Assert.Equal([expectedKey], childKeys);
    }

    private static List<string> ReadStrings(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        using var reader = command.ExecuteReader();
        var values = new List<string>();
        while (reader.Read())
        {
            values.Add(reader.GetString(0));
        }

        return values;
    }
}
