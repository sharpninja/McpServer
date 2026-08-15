using System.Data.Common;
using McpServer.Support.Mcp.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TR-MCP-HEALTH-003 (BUG-TRIAGE-096): unit tests for the single backend-unavailable exception
/// classification (<see cref="StorageBackendUnavailability"/>). Connection-class storage failures
/// (SQLite CANTOPEN/IOERR, transient DbExceptions, EF retry-exhaustion, the EF
/// EnableRetryOnFailure hint wrapper, <see cref="StorageUnavailableException"/>) classify as
/// backend-unavailable; ordinary logic errors do not.
/// Fixture: directly constructed exception instances, including a test-local transient
/// <see cref="DbException"/> subclass.
/// </summary>
public sealed class StorageBackendUnavailabilityTests
{
    private sealed class FakeTransientDbException : DbException
    {
        public FakeTransientDbException()
            : base("A transient database failure occurred.")
        {
        }

        public override bool IsTransient => true;
    }

    /// <summary>AC: SQLite CANTOPEN (14) is a connection-class failure.</summary>
    [Fact]
    public void SqliteCantOpen_IsBackendUnavailable()
        => Assert.True(StorageBackendUnavailability.IsBackendUnavailable(
            new SqliteException("unable to open database file", 14)));

    /// <summary>AC: an ordinary SQLite logic error (1) is not backend-unavailability.</summary>
    [Fact]
    public void SqliteLogicError_IsNotBackendUnavailable()
        => Assert.False(StorageBackendUnavailability.IsBackendUnavailable(
            new SqliteException("SQL logic error", 1)));

    /// <summary>AC: the EF EnableRetryOnFailure hint wrapper classifies via its inner storage exception.</summary>
    [Fact]
    public void EfTransientHintWrapper_IsBackendUnavailable()
        => Assert.True(StorageBackendUnavailability.IsBackendUnavailable(
            new InvalidOperationException(
                "An exception has been raised that is likely due to a transient failure. Consider enabling transient error resiliency by adding 'EnableRetryOnFailure' to the 'UseSqlServer' call.",
                new SqliteException("unable to open database file", 14))));

    /// <summary>AC: EF retry exhaustion (RetryLimitExceededException) is backend-unavailability.</summary>
    [Fact]
    public void RetryLimitExceeded_IsBackendUnavailable()
        => Assert.True(StorageBackendUnavailability.IsBackendUnavailable(
            new RetryLimitExceededException("Maximum number of retries (6) exceeded while executing database operations.")));

    /// <summary>AC: the typed StorageUnavailableException always classifies.</summary>
    [Fact]
    public void StorageUnavailableException_IsBackendUnavailable()
        => Assert.True(StorageBackendUnavailability.IsBackendUnavailable(
            new StorageUnavailableException("The storage backend is unreachable.")));

    /// <summary>AC: any DbException self-reporting IsTransient classifies.</summary>
    [Fact]
    public void TransientDbException_IsBackendUnavailable()
        => Assert.True(StorageBackendUnavailability.IsBackendUnavailable(new FakeTransientDbException()));

    /// <summary>AC: an AggregateException containing a connection-class failure classifies.</summary>
    [Fact]
    public void AggregateWithStorageFailure_IsBackendUnavailable()
        => Assert.True(StorageBackendUnavailability.IsBackendUnavailable(
            new AggregateException(
                new InvalidOperationException("unrelated"),
                new SqliteException("disk I/O error", 10))));

    /// <summary>AC: ordinary exceptions do not classify (guards against over-mapping).</summary>
    [Fact]
    public void OrdinaryException_IsNotBackendUnavailable()
        => Assert.False(StorageBackendUnavailability.IsBackendUnavailable(
            new InvalidOperationException("turn validation failed")));

    /// <summary>AC: null input returns false rather than throwing.</summary>
    [Fact]
    public void Null_IsNotBackendUnavailable()
        => Assert.False(StorageBackendUnavailability.IsBackendUnavailable(null));
}
