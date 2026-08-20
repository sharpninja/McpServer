using System.Text.Json;
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
/// TEST-MCP-TRIAGESTORE-001: session-level tags must round-trip through Sqlite
/// GetAsync/QueryAsync on a fresh DbContext. InMemory same-context tests hide the
/// live SQL Server failure where SessionLogTags rows exist and GET returns tags:null
/// because Include(s =&gt; s.Tags) plus AsSplitQuery does not materialize session tags
/// next to ThenInclude(turn.Tags).
/// </summary>
public sealed class SessionLogSessionTagsSqliteTests
{
    private const string WorkspacePath = @"E:\tests\sessionlog-session-tags-sqlite";

    /// <summary>
    /// TEST-MCP-TRIAGESTORE-001: after SubmitAsync, SessionLogTags rows exist and a
    /// new context GetAsync returns those session tags (not null).
    /// </summary>
    [Fact]
    public async Task GetAsync_NewContext_ReturnsPersistedSessionTags()
    {
        await using var connection = OpenConnection();
        var sessionId = "Cursor-20260820T071556Z-session-tags-sqlite";

        {
            var (sut, db) = BuildSut(connection);
            using (db)
            {
                var dto = CreateSession(sessionId);
                dto.Tags = ["hostile-113", "cluster-closeout", "after-updateservice"];
                await sut.SubmitAsync(dto, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
            }
        }

        long tagRows;
        await using (var countCmd = connection.CreateCommand())
        {
            countCmd.CommandText = "SELECT COUNT(*) FROM SessionLogTags";
            tagRows = (long)(countCmd.ExecuteScalar() ?? 0L);
        }

        Assert.Equal(3, tagRows);

        {
            var (sut, db) = BuildSut(connection);
            using (db)
            {
                var fetched = await sut.GetAsync("Cursor", sessionId, TestContext.Current.CancellationToken).ConfigureAwait(true);
                Assert.NotNull(fetched);
                Assert.NotNull(fetched!.Tags);
                Assert.Equal(3, fetched.Tags!.Count);
                Assert.Contains("hostile-113", fetched.Tags);
                Assert.Contains("cluster-closeout", fetched.Tags);
                Assert.Contains("after-updateservice", fetched.Tags);
                var json = JsonSerializer.Serialize(fetched, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                Assert.Contains("hostile-113", json, StringComparison.Ordinal);
            }
        }
    }

    /// <summary>
    /// TEST-MCP-TRIAGESTORE-001: QueryAsync on a fresh context also returns session tags.
    /// </summary>
    [Fact]
    public async Task QueryAsync_NewContext_ReturnsPersistedSessionTags()
    {
        await using var connection = OpenConnection();
        var sessionId = "Cursor-20260820T071600Z-session-tags-query";

        {
            var (sut, db) = BuildSut(connection);
            using (db)
            {
                var dto = CreateSession(sessionId);
                dto.Tags = ["triage", "cluster"];
                await sut.SubmitAsync(dto, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
            }
        }

        {
            var (sut, db) = BuildSut(connection);
            using (db)
            {
                var queried = await sut.QueryAsync(
                    new SessionLogQueryRequest { Agent = "Cursor" },
                    TestContext.Current.CancellationToken).ConfigureAwait(true);
                var session = Assert.Single(queried.Items, item => item.SessionId == sessionId);
                Assert.NotNull(session.Tags);
                Assert.Contains("triage", session.Tags!);
                Assert.Contains("cluster", session.Tags!);
            }
        }
    }

    private static SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return connection;
    }

    private static (SessionLogService Sut, McpDbContext Db) BuildSut(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<McpDbContext>().UseSqlite(connection).Options;
        var workspaceContext = new WorkspaceContext { WorkspacePath = WorkspacePath };
        var db = new McpDbContext(options, workspaceContext);
        db.Database.EnsureCreated();
        var sut = new SessionLogService(
            db,
            NullLogger<SessionLogService>.Instance,
            Substitute.For<IChangeEventBus>(),
            workspaceContext);
        return (sut, db);
    }

    private static UnifiedSessionLogDto CreateSession(string sessionId)
    {
        return new UnifiedSessionLogDto
        {
            SourceType = "Cursor",
            SessionId = sessionId,
            Title = "Sqlite session tags",
            Status = "in_progress",
            TurnCount = 1,
            Turns =
            [
                new UnifiedRequestEntryDto
                {
                    RequestId = "req-20260820T071556Z-entry-001",
                    Timestamp = "2026-08-20T07:15:56Z",
                    QueryText = "session tags sqlite",
                    Status = "canceled",
                    PlanFile = SessionLogTurnContextValidator.NoneSentinel,
                    TodoId = SessionLogTurnContextValidator.NoneSentinel,
                },
            ],
        };
    }
}
