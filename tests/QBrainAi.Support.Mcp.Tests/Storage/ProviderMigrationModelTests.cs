using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Database;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>
/// TR-MCP-HOSTILEREVIEW-001..004 (PLAN-SESSIONTRIAGEFIX-001 B1, triage-report-3081edb1): the provider migrations that
/// startup applies (McpDatabaseMigrationCoordinator) create every table McpDbContext maps, the HostileReview tables
/// included, so no provider has pending model changes. Data: in-memory SQLite connections; the provider strategies'
/// own option configuration with each provider's migrations assembly.
/// </summary>
public sealed class ProviderMigrationModelTests
{
    /// <summary>
    /// B1 mock gate: the table-exists assertion accepts a table present in the SQLite catalog and rejects a missing
    /// one. Data: an in-memory database with one table created by SQL.
    /// </summary>
    [Fact]
    public void TableAssertion_AcceptsExistingTableAndRejectsMissingTable()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using (var create = connection.CreateCommand())
        {
            create.CommandText = "CREATE TABLE ProbeTable (Id INTEGER PRIMARY KEY)";
            create.ExecuteNonQuery();
        }

        AssertTableExists(connection, "ProbeTable");
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertTableExists(connection, "MissingTable"));
    }

    /// <summary>
    /// B1 assertion proof: the pending-model check reports changes for a context whose model no migration describes. Data: a
    /// one-entity probe context on in-memory SQLite with no migrations of its own.
    /// </summary>
    [Fact]
    public void PendingModelCheck_FlagsAModelWithNoMigrations()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var probe = new ProbeDbContext(new DbContextOptionsBuilder<ProbeDbContext>().UseSqlite(connection).Options);
        Assert.True(probe.Database.HasPendingModelChanges());
    }

    /// <summary>
    /// B1 binding (TR-MCP-HOSTILEREVIEW-001..004, triage-report-3081edb1): for every supported provider, McpDbContext configured
    /// the way startup configures it, with that provider's own migrations assembly, has no pending model changes, so the
    /// migrations startup applies cover the whole model, the HostileReview tables included. Data: each provider's strategy
    /// options; placeholder connection strings (the check compares the model with the migrations, it opens no database).
    /// </summary>
    [Theory]
    [MemberData(nameof(Providers))]
    public void ProviderMigrations_HaveNoPendingModelChanges(string provider, string connectionString)
    {
        using var db = CreateContext(provider, connectionString);
        Assert.False(db.Database.HasPendingModelChanges(), $"{provider}: the model has changes that no migration in its migrations assembly applies");
    }

    /// <summary>
    /// B1 binding (TR-MCP-HOSTILEREVIEW-001..004, triage-report-3081edb1): migrating an empty SQLite database with the SQLite
    /// migrations creates every HostileReview table the model maps. Data: a temporary SQLite file, deleted afterwards.
    /// </summary>
    [Fact]
    public void SqliteMigrations_CreateEveryHostileReviewTable()
    {
        var path = Path.Combine(Path.GetTempPath(), "qbrain-b1-" + Guid.NewGuid().ToString("N") + ".db");
        try
        {
            string[] tables;
            using (var db = CreateContext("sqlite", $"Data Source={path};Pooling=False"))
            {
                db.Database.Migrate();
                tables = db.Model.GetEntityTypes()
                    .Where(e => e.ClrType.Name.StartsWith("HostileReview", StringComparison.Ordinal))
                    .Select(e => e.GetTableName())
                    .OfType<string>()
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();
            }

            Assert.Equal(5, tables.Length);
            using var connection = new SqliteConnection($"Data Source={path};Pooling=False");
            connection.Open();
            foreach (var table in tables)
                AssertTableExists(connection, table);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(path);
        }
    }

    /// <summary>Every supported provider with a placeholder connection string.</summary>
    public static TheoryData<string, string> Providers() => new()
    {
        { "sqlite", "Data Source=:memory:" },
        { "postgresql", "Host=localhost;Database=design_time;Username=design;Password=design" },
        { "sqlserver", "Server=localhost;Database=design_time;TrustServerCertificate=True;Integrated Security=False;User Id=design;Password=design" },
    };

    // B1: the SQLite catalog holds a table with this exact name.
    internal static void AssertTableExists(SqliteConnection connection, string table)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = @name";
        cmd.Parameters.AddWithValue("@name", table);
        Assert.True(Convert.ToInt32(cmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture) == 1, $"table {table} is missing");
    }

    private static McpDbContext CreateContext(string provider, string connectionString)
    {
        var builder = new DbContextOptionsBuilder<McpDbContext>();
        McpDatabaseProviderFactory.Configure(builder, McpDatabaseProviderFactory.CreateOptions(provider, connectionString));
        return new McpDbContext(builder.Options);
    }

    private sealed class ProbeDbContext(DbContextOptions<ProbeDbContext> options) : DbContext(options)
    {
        public DbSet<ProbeRow> Rows => Set<ProbeRow>();
    }

    private sealed class ProbeRow
    {
        public int Id { get; set; }
    }
}
