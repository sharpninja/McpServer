namespace McpServer.Repl.Core;

/// <summary>
/// FR-MCP-TRIAGEERR-001 / TR-MCP-TRIAGEERR-001: REPL-side consumer of the shared
/// <c>McpErrorClassifier</c> contract. Uses the same codes, retryable rules, and
/// <c>details.inner</c> mapping so plugin shims can branch without scraping prose.
/// </summary>
public static class ReplMcpErrorClassifier
{
    /// <summary>Classifies <paramref name="exception"/> into the shared envelope fields.</summary>
    /// <param name="exception">The thrown exception.</param>
    /// <returns>The classified envelope fields.</returns>
    public static ReplClassifiedError FromException(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (IsNamed(exception, "StorageCommandBudgetExceededException")
            || exception.Message.Contains("5 second intake budget", StringComparison.OrdinalIgnoreCase)
            || exception.Message.Contains("backend is currently unreachable", StringComparison.OrdinalIgnoreCase))
        {
            return new ReplClassifiedError(
                "backend_unavailable",
                "The storage backend is currently unreachable. Retry the operation once connectivity is restored.",
                Retryable: true,
                Details: new Dictionary<string, object?>(StringComparer.Ordinal) { ["reason"] = "backend_unavailable" });
        }

        if (IsBusy(exception))
        {
            return new ReplClassifiedError(
                "persistence_error",
                "The storage engine is busy. Retry the operation.",
                Retryable: true,
                Details: new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["inner"] = InnermostMessage(exception),
                });
        }

        if (IsNamed(exception, "DbUpdateException"))
        {
            var inner = InnermostMessage(exception);
            var conflict = IsConflict(exception);
            return new ReplClassifiedError(
                conflict ? "conflict" : "persistence_error",
                conflict
                    ? "The change conflicts with an existing persisted row."
                    : "The change could not be saved.",
                Retryable: false,
                Details: new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["inner"] = inner,
                });
        }

        if (exception is KeyNotFoundException
            || exception.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return new ReplClassifiedError(
                "not_found",
                exception.Message,
                Retryable: false,
                Details: new Dictionary<string, object?>(StringComparer.Ordinal) { ["reason"] = "not_found" });
        }

        if (exception is ArgumentException or FormatException)
        {
            return new ReplClassifiedError(
                "validation_error",
                exception.Message,
                Retryable: false,
                Details: new Dictionary<string, object?>(StringComparer.Ordinal) { ["reason"] = "validation" });
        }

        if (exception is TimeoutException)
        {
            return new ReplClassifiedError("timeout", exception.Message, Retryable: true, Details: null);
        }

        IReadOnlyDictionary<string, object?>? details = null;
        if (exception.InnerException is not null)
        {
            details = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["inner"] = InnermostMessage(exception),
            };
        }

        return new ReplClassifiedError("dispatch_error", exception.Message, Retryable: false, Details: details);
    }

    private static bool IsNamed(Exception exception, string typeName)
    {
        for (var current = exception; current is not null; current = current.InnerException!)
        {
            for (var type = current.GetType(); type is not null; type = type.BaseType)
            {
                if (string.Equals(type.Name, typeName, StringComparison.Ordinal))
                    return true;
            }
        }

        return false;
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
            if (current.Message.Contains("SQLITE_BUSY", StringComparison.OrdinalIgnoreCase)
                || current.Message.Contains("deadlock", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string InnermostMessage(Exception exception)
    {
        var current = exception;
        while (current.InnerException is not null)
            current = current.InnerException;
        return current.Message;
    }
}

/// <summary>FR-MCP-TRIAGEERR-001: classified REPL error fields.</summary>
/// <param name="Code">Stable snake_case code.</param>
/// <param name="Message">Human message without raw provider retry ads.</param>
/// <param name="Retryable">Whether the caller should retry.</param>
/// <param name="Details">Optional details; EF inner text lives under <c>inner</c>.</param>
public sealed record ReplClassifiedError(
    string Code,
    string Message,
    bool Retryable,
    IReadOnlyDictionary<string, object?>? Details);
