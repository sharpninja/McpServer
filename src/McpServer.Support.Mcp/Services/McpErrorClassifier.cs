using System.Data.Common;
using McpServer.Support.Mcp.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-TRIAGEERR-001 / TR-MCP-TRIAGEERR-001: shared exception classifier for REST, MCP tools,
/// REPL, and plugin shims.
/// </summary>
public static class McpErrorClassifier
{
    /// <summary>Stable code for storage connectivity failures.</summary>
    public const string BackendUnavailable = "backend_unavailable";

    /// <summary>Stable code for EF/provider persistence failures.</summary>
    public const string PersistenceError = "persistence_error";

    /// <summary>Stable code for validation failures.</summary>
    public const string ValidationError = "validation_error";

    /// <summary>Stable code for missing resources.</summary>
    public const string NotFound = "not_found";

    /// <summary>Stable code for conflicts (duplicate key, already exists).</summary>
    public const string Conflict = "conflict";

    /// <summary>Stable code for timeouts that are not storage-unreachable.</summary>
    public const string Timeout = "timeout";

    /// <summary>Fallback code for unexpected failures.</summary>
    public const string InternalError = "internal_server_error";

    /// <summary>Human message for storage-unavailable (TR-MCP-HEALTH-003).</summary>
    public const string BackendUnavailableMessage =
        "The storage backend is currently unreachable. Retry the operation once connectivity is restored.";

    /// <summary>Classifies <paramref name="exception"/> into the shared envelope.</summary>
    /// <param name="exception">The thrown exception.</param>
    /// <returns>The classified envelope.</returns>
    public static McpErrorClassification Classify(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (StorageBackendUnavailability.IsBackendUnavailable(exception))
        {
            return new McpErrorClassification(
                BackendUnavailable,
                BackendUnavailableMessage,
                Retryable: true,
                Details: ReasonDetails("backend_unavailable"),
                StatusCode: 503);
        }

        if (exception is SessionLogSchemaPendingMigrationException)
        {
            return new McpErrorClassification(
                PersistenceError,
                exception.Message,
                Retryable: false,
                Details: new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["reason"] = "pending_migration",
                },
                StatusCode: 503);
        }

        if (exception is KeyNotFoundException
            || (exception is InvalidOperationException && exception.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)))
        {
            return new McpErrorClassification(
                NotFound,
                exception.Message,
                Retryable: false,
                Details: ReasonDetails("not_found"),
                StatusCode: 404);
        }

        if (exception is ArgumentException or FormatException)
        {
            return new McpErrorClassification(
                ValidationError,
                exception.Message,
                Retryable: false,
                Details: ReasonDetails("validation"),
                StatusCode: 400);
        }

        if (exception is TimeoutException)
        {
            return new McpErrorClassification(
                Timeout,
                exception.Message,
                Retryable: true,
                Details: BuildDetails(exception, includeInner: true),
                StatusCode: 504);
        }

        if (exception is DbUpdateException dbUpdate)
        {
            var inner = InnermostMessage(dbUpdate);
            var conflict = IsConflict(dbUpdate);
            return new McpErrorClassification(
                conflict ? Conflict : PersistenceError,
                conflict
                    ? "The change conflicts with an existing persisted row."
                    : "The change could not be saved.",
                Retryable: IsBusy(dbUpdate),
                Details: new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["inner"] = inner,
                },
                StatusCode: conflict ? 409 : 500);
        }

        if (exception is DbException db && IsBusy(db))
        {
            return new McpErrorClassification(
                PersistenceError,
                "The storage engine is busy. Retry the operation.",
                Retryable: true,
                Details: new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["inner"] = InnermostMessage(db),
                },
                StatusCode: 503);
        }

        return new McpErrorClassification(
            InternalError,
            exception.Message,
            Retryable: false,
            Details: BuildDetails(exception, includeInner: true),
            StatusCode: 500);
    }

    /// <summary>Walks to the innermost exception message.</summary>
    public static string InnermostMessage(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        var current = exception;
        while (current.InnerException is not null)
            current = current.InnerException;
        return current.Message;
    }

    private static bool IsConflict(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException!)
        {
            var text = current.Message;
            if (text.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
                || text.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
                || text.Contains("already exists", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsBusy(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException!)
        {
            if (current is SqliteException sqlite && sqlite.SqliteErrorCode is 5 or 6)
                return true;
            if (current.Message.Contains("SQLITE_BUSY", StringComparison.OrdinalIgnoreCase)
                || current.Message.Contains("deadlock", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static IReadOnlyDictionary<string, object?> ReasonDetails(string reason)
        => new Dictionary<string, object?>(StringComparer.Ordinal) { ["reason"] = reason };

    private static IReadOnlyDictionary<string, object?>? BuildDetails(Exception exception, bool includeInner)
    {
        if (!includeInner)
            return null;

        var inner = InnermostMessage(exception);
        if (string.Equals(inner, exception.Message, StringComparison.Ordinal))
            return null;

        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["inner"] = inner,
        };
    }
}

/// <summary>
/// FR-MCP-TRIAGEERR-001: DI adapter exposing <see cref="McpErrorClassifier"/> to
/// <c>GlobalExceptionHandlerMiddleware</c>.
/// </summary>
public sealed class McpErrorClassifierAdapter : IMcpErrorClassifier
{
    /// <inheritdoc />
    public McpErrorClassification Classify(Exception exception)
        => McpErrorClassifier.Classify(exception);
}
