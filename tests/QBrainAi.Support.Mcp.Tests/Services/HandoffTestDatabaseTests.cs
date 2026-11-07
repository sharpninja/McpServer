using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-HANDOFF-005 / TEST-HANDOFF-007 (test harness): the handoff durability tests give each database context its own
/// connection to one test-owned database, the way production does, so the competing services and the test's own
/// polling never issue statements on one shared connection, where SQLite reports rows affected per connection.
/// </summary>
/// <remarks>
/// Found while gating IMPL-UNITGREEN-001 round 4: <c>HandoffDurabilityTests.ApproveAsync_LeaseExpiresDuringLiveCreate_SecondInstanceWins</c>
/// failed once on TEST-NODE-A with one in-memory connection shared by every context
/// (triage-report-9b96ada18d72483dad2a3113e455c01b); fix chosen by the operator on 2026-10-20 ("Fix the race in PR 87
/// (Recommended)"). Test data: scripted context factories over a temporary database file the test creates and
/// deletes. The positive control gives each context its own connection to that file. Negative controls, each rejected
/// by the harness assertion inside a passing test: the harness's previous shape (one opened in-memory connection
/// handed to every context) and separate private in-memory databases (own connections, but not one database). The
/// sibling sweep of the same round found the race class in <c>HandoffAuditDefectTests</c> and <c>MemoryS1Harness</c>
/// (operator 2026-10-20: "Extend fix, then round 4 (Recommended)"); the one assertion here is bound to all three
/// harnesses.
/// </remarks>
public sealed class HandoffTestDatabaseTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"mcp-handoff-harness-{Guid.NewGuid():N}");

    private readonly List<IDisposable> _owned = [];

    /// <summary>Creates the temporary folder that holds the scripted database file.</summary>
    public HandoffTestDatabaseTests() => Directory.CreateDirectory(_root);

    /// <summary>Releases the scripted connections and deletes the temporary folder.</summary>
    public void Dispose()
    {
        foreach (var item in _owned)
            item.Dispose();
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    /// <summary>
    /// Asserts that <paramref name="createContext"/> gives each context its own connection to one database: two
    /// contexts hold different connection objects, and a row one writes is read by the other.
    /// </summary>
    /// <param name="createContext">Creates a database context the way the harness under test does.</param>
    internal static void AssertOwnConnectionsToOneDatabase(Func<McpDbContext> createContext)
    {
        using var first = createContext();
        using var second = createContext();
        Assert.NotSame(first.Database.GetDbConnection(), second.Database.GetDbConnection());

        first.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS harness_probe (value TEXT); INSERT INTO harness_probe (value) VALUES ('probe');");
        Assert.Equal(1L, Scalar(second.Database.GetDbConnection(), "SELECT count(*) FROM sqlite_master WHERE type = 'table' AND name = 'harness_probe'"));
        Assert.True(Scalar(second.Database.GetDbConnection(), "SELECT count(*) FROM harness_probe") >= 1);
    }

    private static long Scalar(DbConnection connection, string sql)
    {
        var opened = connection.State != System.Data.ConnectionState.Open;
        if (opened)
            connection.Open();
        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            return Convert.ToInt64(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
        }
        finally
        {
            if (opened)
                connection.Close();
        }
    }

    private McpDbContext Context(DbContextOptions<McpDbContext> options) =>
        new(options, new WorkspaceContext { WorkspacePath = _root });

    /// <summary>Scripted harness: each context opens its own connection to one temporary database file.</summary>
    private Func<McpDbContext> OwnConnectionsToOneFile()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseSqlite($"Data Source={Path.Combine(_root, "scripted.db")};Pooling=False")
            .Options;
        return () => Context(options);
    }

    /// <summary>Scripted harness in the previous shape: one opened in-memory connection handed to every context.</summary>
    private Func<McpDbContext> OneSharedInMemoryConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        _owned.Add(connection);
        var options = new DbContextOptionsBuilder<McpDbContext>().UseSqlite(connection).Options;
        return () => Context(options);
    }

    /// <summary>Scripted harness with own connections but no shared database: each context gets a private in-memory database.</summary>
    private Func<McpDbContext> PrivateInMemoryDatabases()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>().UseSqlite("Data Source=:memory:").Options;
        return () => Context(options);
    }

    /// <summary>Mock gate positive control: own connections to one database file pass the harness assertion.</summary>
    [Fact]
    public void OwnConnectionsToOneFile_PassTheHarnessAssertion() =>
        AssertOwnConnectionsToOneDatabase(OwnConnectionsToOneFile());

    /// <summary>
    /// Mock gate negative controls: one shared in-memory connection (the previous harness) and private in-memory
    /// databases are each rejected by the harness assertion, inside a passing harness.
    /// </summary>
    [Fact]
    public void SharedConnectionAndPrivateDatabases_AreRejectedInsidePassingHarness()
    {
        Assert.NotNull(Record.Exception(() => AssertOwnConnectionsToOneDatabase(OneSharedInMemoryConnection())));
        Assert.NotNull(Record.Exception(() => AssertOwnConnectionsToOneDatabase(PrivateInMemoryDatabases())));
    }
}
