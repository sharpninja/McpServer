using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Notifications;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-TRIAGESCHEMA-001: missing AgentSession header columns fail closed as
/// pending-migration; after columns exist, sessionlog query succeeds.
/// </summary>
public sealed class SessionLogSchemaGuardTests : IDisposable
{
    /// <summary>Resets the probe cache between tests.</summary>
    public SessionLogSchemaGuardTests() => SessionLogSchemaGuard.ResetCache();

    /// <inheritdoc />
    public void Dispose() => SessionLogSchemaGuard.ResetCache();

    /// <summary>A SessionLogs table without the four agent-header columns is pending-migration.</summary>
    [Fact]
    public void EnsureAgentSessionHeaderColumns_MissingColumns_ThrowsPendingMigration()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = """
                CREATE TABLE SessionLogs (
                    Id INTEGER PRIMARY KEY,
                    WorkspaceId TEXT NOT NULL,
                    SourceType TEXT NOT NULL,
                    SessionId TEXT NOT NULL
                );
                """;
            cmd.ExecuteNonQuery();
        }

        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseSqlite(connection)
            .Options;
        using var db = new McpDbContext(options);

        var ex = Assert.Throws<SessionLogSchemaPendingMigrationException>(
            () => SessionLogSchemaGuard.EnsureAgentSessionHeaderColumns(db));
        Assert.Contains("pending-migration", ex.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("Invalid column name", ex.Message, StringComparison.Ordinal);
    }

    /// <summary>QueryAsync on a missing-column store fails closed without SQL Invalid column name.</summary>
    [Fact]
    public async Task QueryAsync_MissingAgentSessionColumns_FailsClosedWithNamedError()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = """
                CREATE TABLE SessionLogs (
                    Id INTEGER PRIMARY KEY,
                    WorkspaceId TEXT NOT NULL,
                    SourceType TEXT NOT NULL,
                    SessionId TEXT NOT NULL
                );
                """;
            cmd.ExecuteNonQuery();
        }

        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseSqlite(connection)
            .Options;
        using var db = new McpDbContext(options, new WorkspaceContext { WorkspacePath = @"E:\tests\schema-guard" });
        var sut = new SessionLogService(
            db,
            NullLogger<SessionLogService>.Instance,
            Substitute.For<IChangeEventBus>(),
            new WorkspaceContext { WorkspacePath = @"E:\tests\schema-guard" });

        var ex = await Assert.ThrowsAsync<SessionLogSchemaPendingMigrationException>(() =>
            sut.QueryAsync(new SessionLogQueryRequest { Limit = 1 }, TestContext.Current.CancellationToken))
            .ConfigureAwait(true);
        Assert.Contains("pending-migration", ex.Message, StringComparison.Ordinal);
    }

    /// <summary>After the four columns exist, probe and query succeed.</summary>
    [Fact]
    public async Task QueryAsync_AfterColumnsPresent_Succeeds()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseSqlite(connection)
            .Options;
        using var db = new McpDbContext(options, new WorkspaceContext { WorkspacePath = @"E:\tests\schema-guard-ready" });
        db.Database.EnsureCreated();
        db.OverrideWorkspaceId(@"E:\tests\schema-guard-ready");

        Assert.True(SessionLogSchemaGuard.Probe(db));
        SessionLogSchemaGuard.EnsureAgentSessionHeaderColumns(db);

        var sut = new SessionLogService(
            db,
            NullLogger<SessionLogService>.Instance,
            Substitute.For<IChangeEventBus>(),
            new WorkspaceContext { WorkspacePath = @"E:\tests\schema-guard-ready" });
        var result = await sut.QueryAsync(
            new SessionLogQueryRequest { Limit = 1 },
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.NotNull(result);
        Assert.Empty(result.Items);

        var filtered = await sut.QueryAsync(
            new SessionLogQueryRequest { Limit = 1, Text = "does-not-match" },
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.NotNull(filtered);
        Assert.Empty(filtered.Items);
    }

    /// <summary>The pending-migration exception classifies as persistence_error with reason.</summary>
    [Fact]
    public void Classify_PendingMigration_IsPersistenceError()
    {
        var classified = McpErrorClassifier.Classify(new SessionLogSchemaPendingMigrationException());
        Assert.Equal(McpErrorClassifier.PersistenceError, classified.Code);
        Assert.False(classified.Retryable);
        Assert.Equal("pending_migration", classified.Details!["reason"]);
    }
}
