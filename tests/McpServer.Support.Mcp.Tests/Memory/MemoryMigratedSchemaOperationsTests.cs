using McpServer.Cqrs;
using McpServer.Support.Mcp.Controllers;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-010 / TEST-MCP-MEMORY-014 / TEST-MCP-MEMORY-016:
/// Remember, list, and recall against an applied SQLite migration chain (not EnsureCreated).
/// Proves MemoryVersions soft-delete columns are present after
/// <c>20260919193000_AddMemoryVersionSoftDeleteColumns</c>.
/// </summary>
public sealed class MemoryMigratedSchemaOperationsTests : IDisposable
{
    private const string SoftDeleteMigration = "20260919193000_AddMemoryVersionSoftDeleteColumns";
    private const string PrecedingMigration = "20260919060000_AddMemoryIndexStorage";

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<McpDbContext> _options;
    private readonly string _workspace;

    /// <summary>Opens an isolated in-memory SQLite database using the real Sqlite migrations assembly.</summary>
    public MemoryMigratedSchemaOperationsTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<McpDbContext>()
            .UseSqlite(_connection, sqlite => sqlite.MigrationsAssembly("McpServer.Storage.SqliteMigrations"))
            .Options;
        _workspace = Path.Combine(Path.GetTempPath(), "mcp-mem-mig-" + Guid.NewGuid().ToString("N"));
    }

    /// <inheritdoc />
    public void Dispose() => _connection.Dispose();

    /// <summary>
    /// Legion-shaped schema: September memory tables exist, MemoryVersions lacks soft-delete columns,
    /// and remember fails on insert.
    /// </summary>
    [Fact]
    public async Task Remember_Fails_WhenMemoryVersionsLacksSoftDeleteColumns()
    {
        await using (var db = CreateContext())
        {
            db.GetService<IMigrator>().Migrate(PrecedingMigration);
            Assert.True(TableExists(db, "MemoryVersions"));
            Assert.False(ColumnExists(db, "MemoryVersions", "IsDeleted"));
            Assert.False(ColumnExists(db, "MemoryVersions", "DeletedAtUtc"));
            Assert.False(ColumnExists(db, "MemoryVersions", "DeletedBy"));
            Assert.False(ColumnExists(db, "MemoryVersions", "DeleteReason"));
        }

        var result = await RememberAsync("legion-schema-mismatch-token").ConfigureAwait(true);
        Assert.False(result.StatusCode is 200 or 201, result.Error);
        Assert.False(string.IsNullOrWhiteSpace(result.Error));
        Assert.True(
            result.Error.Contains("DbUpdateException", StringComparison.OrdinalIgnoreCase)
            || result.Error.Contains("SqliteException", StringComparison.OrdinalIgnoreCase)
            || result.Error.Contains("no such column", StringComparison.OrdinalIgnoreCase),
            result.Error);
    }

    /// <summary>Full migrate applies the new soft-delete columns and remember/list/recall succeed.</summary>
    [Fact]
    public async Task RememberListRecall_Succeed_AfterSoftDeleteColumnMigration()
    {
        await using (var db = CreateContext())
        {
            db.Database.Migrate();
            var applied = db.Database.GetAppliedMigrations().ToArray();
            Assert.Contains(SoftDeleteMigration, applied);
            Assert.True(ColumnExists(db, "MemoryVersions", "IsDeleted"));
            Assert.True(ColumnExists(db, "MemoryVersions", "DeletedAtUtc"));
            Assert.True(ColumnExists(db, "MemoryVersions", "DeletedBy"));
            Assert.True(ColumnExists(db, "MemoryVersions", "DeleteReason"));
            Assert.True(ColumnExists(db, "MemoryEdges", "IsDeleted"));
            Assert.True(ColumnExists(db, "MemoryIndexes", "IsDeleted"));
        }

        const string token = "migrated-schema-remember-token";
        var remembered = await RememberAsync(token).ConfigureAwait(true);
        Assert.True(remembered.StatusCode is 200 or 201, remembered.Error);
        Assert.False(string.IsNullOrWhiteSpace(remembered.MemoryId));

        await using (var db = CreateContext())
        {
            var listed = await MemoryS1Harness.CreateService(db)
                .ListAsync(new MemoryListRequest { Keyword = token }, TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            Assert.Contains(listed.Items, item => item.Id == remembered.MemoryId);
        }

        var versions = await ListVersionsAsync(remembered.MemoryId!).ConfigureAwait(true);
        Assert.Equal(200, versions.StatusCode);
        Assert.NotNull(versions.Items);
        Assert.NotEmpty(versions.Items!);

        var recall = await RecallAsync(token).ConfigureAwait(true);
        Assert.Equal(200, recall.StatusCode);
        Assert.Contains(recall.Items ?? [], hit => hit.Id == remembered.MemoryId);
    }

    private McpDbContext CreateContext()
        => new(_options, new WorkspaceContext { WorkspacePath = _workspace });

    private IDispatcher CreateDispatcher()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(_options);
        services.AddCqrs(typeof(RememberMemoryCommand).Assembly, typeof(MemoryController).Assembly);
        return services.BuildServiceProvider().GetRequiredService<IDispatcher>();
    }

    private async Task<MemoryRememberResult> RememberAsync(string content)
    {
        var dispatched = await CreateDispatcher().SendAsync(
            new RememberMemoryCommand(
                _workspace,
                new MemoryRememberRequest { Content = content, Type = "fact" },
                ReadOnlyCaller: false),
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        if (dispatched.IsSuccess && dispatched.Value is not null)
            return dispatched.Value;

        return new MemoryRememberResult(
            StatusCode: 500,
            FailureKind: MemoryMutationFailureKind.None,
            Error: dispatched.Exception?.ToString() ?? dispatched.Error ?? "remember failed");
    }

    private async Task<MemoryVersionListResult> ListVersionsAsync(string memoryId)
    {
        var dispatched = await CreateDispatcher().QueryAsync(
            new ListMemoryVersionsQuery(_workspace, memoryId),
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        if (dispatched.IsSuccess && dispatched.Value is not null)
            return dispatched.Value;

        return new MemoryVersionListResult(
            StatusCode: 500,
            Error: dispatched.Error ?? dispatched.Exception?.Message ?? "list versions failed");
    }

    private async Task<MemoryRecallResult> RecallAsync(string query)
    {
        var dispatched = await CreateDispatcher().QueryAsync(
            new RecallMemoryQuery(_workspace, query, MinScore: 0, TopN: 10),
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        if (dispatched.IsSuccess && dispatched.Value is not null)
            return dispatched.Value;

        return new MemoryRecallResult(
            StatusCode: 500,
            FailureKind: MemoryMutationFailureKind.None,
            Error: dispatched.Error ?? dispatched.Exception?.Message ?? "recall failed");
    }

    private static bool TableExists(McpDbContext db, string table)
    {
        using var cmd = db.Database.GetDbConnection().CreateCommand();
        if (cmd.Connection!.State != System.Data.ConnectionState.Open)
            cmd.Connection.Open();
        cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=$n";
        var parameter = cmd.CreateParameter();
        parameter.ParameterName = "$n";
        parameter.Value = table;
        cmd.Parameters.Add(parameter);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    private static bool ColumnExists(McpDbContext db, string table, string column)
    {
        using var cmd = db.Database.GetDbConnection().CreateCommand();
        if (cmd.Connection!.State != System.Data.ConnectionState.Open)
            cmd.Connection.Open();
        cmd.CommandText = $"PRAGMA table_info({table})";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
