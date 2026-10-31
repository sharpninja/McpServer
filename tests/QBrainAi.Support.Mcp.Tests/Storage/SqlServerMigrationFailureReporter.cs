using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>
/// FR-MCP-TESTRESOURCE-001 ac-5: operation-scoped SQL Server migration diagnostics.
/// EF structured command/connection events retain the original error before a finally-block
/// lock-release error can replace it. This helper neither retries nor suppresses failures.
/// </summary>
internal sealed class SqlServerMigrationFailureReporter
{
    private readonly List<Exception> _failures = [];
    private bool _active;

    /// <summary>Filters EF logging to structured command and connection failures.</summary>
    internal static bool ShouldCapture(EventId eventId, LogLevel _) =>
        eventId.Id == RelationalEventId.CommandError.Id || eventId.Id == RelationalEventId.ConnectionError.Id;

    /// <summary>Receives the actual EF event payload, without parsing a formatted message.</summary>
    internal void Observe(EventData data)
    {
        if (data is CommandErrorEventData command) Record(command.Exception);
        else if (data is ConnectionErrorEventData connection) Record(connection.Exception);
    }

    /// <summary>Retains each exception identity once, in encounter order, during the active operation.</summary>
    internal void Record(Exception error)
    {
        if (_active && !_failures.Any(existing => ReferenceEquals(existing, error)))
            _failures.Add(error);
    }

    /// <summary>Executes one synchronous migration with a fresh diagnostic collection.</summary>
    internal void Run(Action operation)
    {
        Begin();
        try { operation(); }
        catch (OperationCanceledException cancelled)
        {
            Record(cancelled);
            cancelled.Data["MigrationFailures"] = _failures.ToArray();
            throw;
        }
        catch (Exception escaped)
        {
            Record(escaped);
            throw new AggregateException("SQL Server migration: first failure, followed by later failures.", _failures.ToArray());
        }
        finally { _active = false; }
    }

    /// <summary>Executes one asynchronous migration, preserving cancellation identity and token.</summary>
    internal async Task RunAsync(Func<Task> operation)
    {
        Begin();
        try { await operation().ConfigureAwait(false); }
        catch (OperationCanceledException cancelled)
        {
            Record(cancelled);
            cancelled.Data["MigrationFailures"] = _failures.ToArray();
            throw;
        }
        catch (Exception escaped)
        {
            Record(escaped);
            throw new AggregateException("SQL Server migration: first failure, followed by later failures.", _failures.ToArray());
        }
        finally { _active = false; }
    }

    private void Begin()
    {
        if (_active) throw new InvalidOperationException("Migration diagnostic operations cannot overlap.");
        _failures.Clear();
        _active = true;
    }
}
