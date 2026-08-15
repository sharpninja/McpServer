using System.ComponentModel;
using System.Data.Common;
using System.Net.Sockets;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Storage;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-HEALTH-003 (BUG-TRIAGE-096): typed exception signaling that the storage backend is
/// unreachable. Services may throw it explicitly; the shared error path maps it (and raw
/// connection-class provider failures) to HTTP 503 <c>backend_unavailable</c>.
/// </summary>
public sealed class StorageUnavailableException : Exception
{
    /// <summary>TR-MCP-HEALTH-003: initializes with a default message.</summary>
    public StorageUnavailableException()
        : base("The storage backend is currently unreachable.")
    {
    }

    /// <summary>TR-MCP-HEALTH-003: initializes with a message.</summary>
    /// <param name="message">Failure description.</param>
    public StorageUnavailableException(string message)
        : base(message)
    {
    }

    /// <summary>TR-MCP-HEALTH-003: initializes with a message and the causing exception.</summary>
    /// <param name="message">Failure description.</param>
    /// <param name="innerException">The underlying provider failure.</param>
    public StorageUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// TR-MCP-HEALTH-003 (BUG-TRIAGE-096): the single backend-unavailable exception classification
/// shared by the REST error path and the MCP tools. Classifies connection-class storage failures
/// (SqlClient connection errors, SQLite CANTOPEN/IOERR, transient DbExceptions, EF
/// retry-exhaustion, and <see cref="StorageUnavailableException"/>) by walking the exception
/// chain, so callers can replace raw provider text with the stable
/// <c>backend_unavailable</c> error.
/// </summary>
public static class StorageBackendUnavailability
{
    private const int MaxDepth = 8;

    /// <summary>
    /// SqlClient connection-class error numbers that indicate the server is unreachable rather
    /// than a statement-level fault: network/instance resolution (2, 53, 11001, 1231, 1232),
    /// transport drops (121, 232, 10053, 10054, 10060, 10061, 20), timeouts (-2, 258), and
    /// database-availability errors (4060, 40613).
    /// </summary>
    private static readonly HashSet<int> s_sqlConnectionErrorNumbers =
    [
        -2, 0, 2, 20, 53, 121, 232, 258, 1231, 1232, 4060, 10053, 10054, 10060, 10061, 11001, 40613,
    ];

    /// <summary>SQLite primary result codes for unreachable storage: IOERR (10) and CANTOPEN (14).</summary>
    private static readonly HashSet<int> s_sqliteConnectionErrorCodes = [10, 14];

    /// <summary>
    /// TR-MCP-HEALTH-003: returns <see langword="true"/> when the exception (or any inner or
    /// aggregated exception) represents backend-unavailability.
    /// </summary>
    /// <param name="exception">The exception to classify; <see langword="null"/> returns false.</param>
    /// <returns><see langword="true"/> for connection-class storage failures.</returns>
    public static bool IsBackendUnavailable(Exception? exception)
        => Classify(exception, [], depth: 0);

    private static bool Classify(Exception? exception, HashSet<Exception> visited, int depth)
    {
        if (exception is null || depth > MaxDepth || !visited.Add(exception))
            return false;

        switch (exception)
        {
            case StorageUnavailableException:
            case RetryLimitExceededException:
                return true;
            case SqlException sql when sql.IsTransient || s_sqlConnectionErrorNumbers.Contains(sql.Number):
                return true;
            case SqliteException sqlite when s_sqliteConnectionErrorCodes.Contains(sqlite.SqliteErrorCode):
                return true;
            case DbException db when db.IsTransient || db.InnerException is SocketException or Win32Exception:
                return true;
        }

        if (exception is AggregateException aggregate)
        {
            foreach (var inner in aggregate.InnerExceptions)
            {
                if (Classify(inner, visited, depth + 1))
                    return true;
            }
        }

        return Classify(exception.InnerException, visited, depth + 1);
    }
}

/// <summary>
/// TR-MCP-HEALTH-003 (BUG-TRIAGE-096): DI adapter exposing
/// <see cref="StorageBackendUnavailability"/> to the shared
/// <c>GlobalExceptionHandlerMiddleware</c> via <see cref="IBackendUnavailabilityDetector"/>.
/// </summary>
public sealed class StorageBackendUnavailabilityDetector : IBackendUnavailabilityDetector
{
    /// <inheritdoc />
    public bool IsBackendUnavailable(Exception exception)
        => StorageBackendUnavailability.IsBackendUnavailable(exception);
}
