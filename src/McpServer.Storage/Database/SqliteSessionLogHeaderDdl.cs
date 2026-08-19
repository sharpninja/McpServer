using Microsoft.Data.Sqlite;
using SQLitePCL;

namespace McpServer.Support.Mcp.Storage.Database;

/// <summary>
/// TR-MCP-TRIAGESCHEMA-001: registers a Sqlite scalar that issues ALTER TABLE ADD COLUMN
/// for SessionLogs agent-header fields. The migration SQL skips the call when
/// <c>pragma_table_info</c> already lists the column.
/// </summary>
public static class SqliteSessionLogHeaderDdl
{
    /// <summary>Scalar function name invoked from Sqlite 20260818205751 Up().</summary>
    public const string FunctionName = "mcp_add_sessionlog_text_column_if_missing";

    /// <summary>Registers <see cref="FunctionName"/> on <paramref name="connection"/>.</summary>
    /// <param name="connection">The Sqlite connection used by EF migrate.</param>
    public static void Register(SqliteConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        connection.CreateFunction(FunctionName, (string column) =>
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(column);
            if (column.AsSpan().IndexOfAny("\"';[]") >= 0)
                throw new ArgumentException("Column name must be a simple identifier.", nameof(column));

            var sql = $"""ALTER TABLE "SessionLogs" ADD COLUMN "{column}" TEXT NULL;""";
            var rc = raw.sqlite3_exec(connection.Handle, sql);
            if (rc != raw.SQLITE_OK)
            {
                var message = raw.sqlite3_errmsg(connection.Handle).utf8_to_string();
                throw new InvalidOperationException($"Failed to add SessionLogs.{column}: {message}");
            }

            return 1;
        });
    }
}
