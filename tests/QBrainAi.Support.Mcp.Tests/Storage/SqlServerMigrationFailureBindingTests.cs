using System.Data;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>
/// FR-MCP-TESTRESOURCE-001 ac-5: the same scripted assertions bind the real reporting helper.
/// EF event bindings use a private in-memory SQLite connection and an explicit failing connection
/// fake; no SQL Server, host configuration, file or process environment is accessed.
/// </summary>
public sealed class SqlServerMigrationFailureBindingTests
{
    private static SqlServerMigrationFailureContractTests.Probe Create()
    {
        var reporter = new SqlServerMigrationFailureReporter();
        return new(reporter.Record, reporter.Run, reporter.RunAsync);
    }

    /// <summary>Real reporter retains first command/connection failures and all distinct later failures.</summary>
    [Fact]
    public void Reporter_PreservesFirstAndLaterErrors() =>
        SqlServerMigrationFailureContractTests.AssertFailureSequences(Create);

    /// <summary>Real reporter resets between successful and failed operations.</summary>
    [Fact]
    public void Reporter_IsolatesOperations() => SqlServerMigrationFailureContractTests.AssertOperationIsolation(Create);

    /// <summary>Real async reporter retains failure order and cancellation identity and token.</summary>
    [Fact]
    public Task Reporter_PreservesAsyncErrorsAndCancellation() =>
        SqlServerMigrationFailureContractTests.AssertAsyncSequences(Create);

    /// <summary>An actual EF command-error event survives a later simulated release failure.</summary>
    [Fact]
    public void StructuredCommandEvent_PreservesOriginalBeforeReleaseFailure()
    {
        var reporter = new SqlServerMigrationFailureReporter();
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var db = new DbContext(new DbContextOptionsBuilder().UseSqlite(connection)
            .LogTo(SqlServerMigrationFailureReporter.ShouldCapture, reporter.Observe).Options);
        var release = new InvalidOperationException("later lock release");
        var result = Assert.Throws<AggregateException>(() => reporter.Run(() =>
        {
            try { db.Database.ExecuteSqlRaw("SELECT * FROM missing_scripted_table"); }
            catch (SqliteException) { throw release; }
        }));
        Assert.Collection(result.InnerExceptions,
            error => Assert.Contains("missing_scripted_table", Assert.IsType<SqliteException>(error).Message),
            error => Assert.Same(release, error));
    }

    /// <summary>An actual EF connection-error event survives a later simulated release failure.</summary>
    [Fact]
    public void StructuredConnectionEvent_PreservesOriginalBeforeReleaseFailure()
    {
        var reporter = new SqlServerMigrationFailureReporter();
        var first = new IOException("scripted connection open failure");
        using var connection = Substitute.For<DbConnection>();
        connection.State.Returns(ConnectionState.Closed);
        connection.DataSource.Returns("scripted-source");
        connection.Database.Returns("scripted-database");
        connection.ConnectionString.Returns("Data Source=:memory:");
        connection.When(c => c.Open()).Do(_ => throw first);
        using var db = new DbContext(new DbContextOptionsBuilder().UseSqlite(connection)
            .LogTo(SqlServerMigrationFailureReporter.ShouldCapture, reporter.Observe).Options);
        var release = new InvalidOperationException("later lock release");
        var result = Assert.Throws<AggregateException>(() => reporter.Run(() =>
        {
            try { db.Database.OpenConnection(); }
            catch (IOException) { throw release; }
        }));
        Assert.Collection(result.InnerExceptions, error => Assert.Same(first, error), error => Assert.Same(release, error));
    }
}
