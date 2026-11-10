using Microsoft.Data.Sqlite;

namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>
/// TR-MCP-DB-001, BDPv4 mock gate for <see cref="MigrationKeyAssertions"/>: scripted SQLite tables (no migrations)
/// stand in for a migrated database. The assertion accepts child rows that carry exactly the parent workspace key and
/// rejects the canonical respelling, mixed keys, an empty child table, a rekeyed parent, and a second workspace, before
/// the assertion is bound to Decompose4nfBackfillMigrationTests.
/// </summary>
public sealed class MigrationKeyAssertionsMockGateTests : IDisposable
{
    private const string LegacyKey = "Q:\\__mcp_unit_test__\\QBrainAi";
    private const string CanonicalKey = "Q:/__mcp_unit_test__/QBrainAi";
    private readonly SqliteConnection _connection;

    /// <summary>Creates an in-memory database with a Workspaces table (global row seeded) and one child table.</summary>
    public MigrationKeyAssertionsMockGateTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        Execute("CREATE TABLE \"Workspaces\" (\"WorkspaceId\" TEXT NOT NULL PRIMARY KEY);");
        Execute("CREATE TABLE \"ChildItems\" (\"Id\" INTEGER PRIMARY KEY, \"WorkspaceId\" TEXT NOT NULL, \"Value\" TEXT NOT NULL);");
        Execute("INSERT INTO \"Workspaces\" VALUES ('');");
    }

    /// <summary>TR-MCP-DB-001: child rows that carry exactly the seeded legacy parent key are accepted.</summary>
    [Fact]
    public void ChildRowsWithParentKey_AreAccepted()
    {
        SeedParent(LegacyKey);
        SeedChild(LegacyKey, "GPL-3.0");
        SeedChild(LegacyKey, "AGPL-3.0");

        MigrationKeyAssertions.AssertChildRowsCarryWorkspaceKey(_connection, "ChildItems", LegacyKey);
    }

    /// <summary>
    /// TR-MCP-DB-001 negative controls: each wrong database state is rejected by the assertion (canonical respelling in
    /// the child, mixed keys, no child rows, parent rekeyed while the child is not, a second workspace).
    /// </summary>
    /// <param name="fault">The injected wrong state.</param>
    [Theory]
    [InlineData("child-canonical")]
    [InlineData("child-mixed")]
    [InlineData("child-empty")]
    [InlineData("parent-canonical")]
    [InlineData("second-workspace")]
    public void WrongKeyState_IsRejected(string fault)
    {
        switch (fault)
        {
            case "child-canonical":
                SeedParent(LegacyKey);
                SeedChild(CanonicalKey, "GPL-3.0");
                break;
            case "child-mixed":
                SeedParent(LegacyKey);
                SeedChild(LegacyKey, "GPL-3.0");
                SeedChild(CanonicalKey, "AGPL-3.0");
                break;
            case "child-empty":
                SeedParent(LegacyKey);
                break;
            case "parent-canonical":
                SeedParent(CanonicalKey);
                SeedChild(LegacyKey, "GPL-3.0");
                break;
            case "second-workspace":
                SeedParent(LegacyKey);
                SeedParent("Q:\\other");
                SeedChild(LegacyKey, "GPL-3.0");
                break;
            default:
                throw new InvalidOperationException("Unknown fault " + fault);
        }

        Assert.ThrowsAny<Xunit.Sdk.XunitException>(
            () => MigrationKeyAssertions.AssertChildRowsCarryWorkspaceKey(_connection, "ChildItems", LegacyKey));
    }

    /// <summary>Releases the in-memory database connection.</summary>
    public void Dispose()
    {
        _connection.Dispose();
    }

    private void SeedParent(string key) => Execute("INSERT INTO \"Workspaces\" VALUES ($key);", key);

    private void SeedChild(string key, string value)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = "INSERT INTO \"ChildItems\" (\"WorkspaceId\", \"Value\") VALUES ($key, $value);";
        command.Parameters.AddWithValue("$key", key);
        command.Parameters.AddWithValue("$value", value);
        command.ExecuteNonQuery();
    }

    private void Execute(string sql, string? key = null)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        if (key is not null)
        {
            command.Parameters.AddWithValue("$key", key);
        }

        command.ExecuteNonQuery();
    }
}
