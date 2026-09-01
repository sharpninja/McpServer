using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.Services;

/// <summary>TR-HANDOFF-AUDIT-001: Provider-stable unique-violation detection.</summary>
public static class HandoffDbExceptions
{
    /// <summary>
    /// Returns true when the exception or any inner exception is a unique/index violation
    /// identified by provider codes or types, not English message text.
    /// </summary>
    public static bool IsUniqueViolation(Exception? exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is DbUpdateException && current.InnerException is not null)
                continue;

            var type = current.GetType();
            if (type.Name == "SqlException")
            {
                var number = type.GetProperty("Number")?.GetValue(current) as int?;
                if (number is 2601 or 2627)
                    return true;
            }

            if (type.Name == "PostgresException")
            {
                var sqlState = type.GetProperty("SqlState")?.GetValue(current) as string;
                if (string.Equals(sqlState, "23505", StringComparison.Ordinal))
                    return true;
            }

            if (type.Name == "SqliteException")
            {
                var extended = type.GetProperty("SqliteExtendedErrorCode")?.GetValue(current) as int?;
                if (extended is 2067 or 1555)
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Returns true when a failed SaveChanges may have committed on the server.
    /// Uses provider numbers, SQLSTATE values, and typed transient/transport exceptions only.
    /// </summary>
    public static bool IsCommitAmbiguous(Exception? exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is TimeoutException)
                return true;

            if (current is System.IO.IOException or System.Net.Sockets.SocketException)
                return true;

            var type = current.GetType();
            if (type.Name == "SqlException")
            {
                var number = type.GetProperty("Number")?.GetValue(current) as int?;
                if (number is -2 or 11 or 1205 or 64 or 233 or 10053 or 10054 or 40197 or 40613)
                    return true;
            }

            if (type.Name is "PostgresException" or "NpgsqlException")
            {
                var sqlState = type.GetProperty("SqlState")?.GetValue(current) as string;
                if (sqlState is "40001" or "40P01" or "08006" or "08001" or "57P01" or "57014" or "08003")
                    return true;
            }

            if (type.Name == "SqliteException")
            {
                var code = type.GetProperty("SqliteErrorCode")?.GetValue(current) as int?;
                if (code is 5 or 6)
                    return true;
            }
        }

        return false;
    }
}
