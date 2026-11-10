using System.Data.Common;
using System.Diagnostics;
using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Notifications;
using QBrainAi.Support.Mcp.Options;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-TRIAGESTORE-007; FR-MCP-TRIAGESTORE-002: the helpers that both halves of SessionLogDenseGraphTests use (the unit half in QBrainAi.Support.Mcp.Tests, the integration half in QBrainAi.Support.Mcp.EnvironmentIntegrationTests), defined once and compiled into both projects.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-TRIAGESTORE-007; FR-MCP-TRIAGESTORE-002; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: extracted unchanged from SessionLogDenseGraphTests (WorkspacePath, Agent, SessionId, ExistingRequestId, SecondRequestId, NewRequestId, SeedDenseSessionAsync, BuildSut, OpenConnection, Count, NewTurn, CreateDenseSession and DenseTurn) when that class split into its unit half and its integration half (slice S7, hostile validation finding R14-F4), so both halves use one definition instead of a copy (DRY); only the accessibility changed, from private to internal.</remarks>
internal static class SessionLogDenseGraphTestsSupport
{
    internal const string WorkspacePath = @"E:\tests\sessionlog-dense-graph";
    internal const string Agent = "Cursor";
    internal const string SessionId = "Cursor-20260928T220000Z-dense-graph";
    internal const string ExistingRequestId = "req-20260928T220000Z-dense-existing";
    internal const string SecondRequestId = "req-20260928T220001Z-dense-second";
    internal const string NewRequestId = "req-20260928T220002Z-dense-new";

    internal static async Task SeedDenseSessionAsync(SqliteConnection connection)
    {
        var (sut, db) = BuildSut(connection, interceptor: null);
        using (db)
        {
            db.Database.EnsureCreated();
            await sut.SubmitAsync(CreateDenseSession(), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        }
    }

    internal static (SessionLogService Sut, McpDbContext Db) BuildSut(
        SqliteConnection connection,
        DbCommandInterceptor? interceptor,
        int? budgetSeconds = null)
    {
        var builder = new DbContextOptionsBuilder<McpDbContext>().UseSqlite(connection);
        if (interceptor is not null)
            builder.AddInterceptors(interceptor);

        var workspace = new WorkspaceContext { WorkspacePath = WorkspacePath };
        var db = new McpDbContext(builder.Options, workspace);
        db.OverrideWorkspaceId(WorkspacePath);
        var submitOptions = budgetSeconds is null
            ? null
            : Microsoft.Extensions.Options.Options.Create(new SessionLogSubmitOptions { SubmitCommandBudgetSeconds = budgetSeconds.Value });
        var sut = new SessionLogService(
            db,
            NullLogger<SessionLogService>.Instance,
            Substitute.For<IChangeEventBus>(),
            workspace,
            submitOptions);
        return (sut, db);
    }

    internal static SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return connection;
    }

    internal static long Count(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return (long)(command.ExecuteScalar() ?? 0L);
    }

    internal static UnifiedRequestEntryDto NewTurn() => new()
    {
        RequestId = NewRequestId,
        Timestamp = "2026-09-28T22:00:02Z",
        QueryText = "begin turn on a dense session",
        Status = "in_progress",
        PlanFile = SessionLogTurnContextValidator.NoneSentinel,
        TodoId = SessionLogTurnContextValidator.NoneSentinel,
    };

    internal static UnifiedSessionLogDto CreateDenseSession()
    {
        return new UnifiedSessionLogDto
        {
            SourceType = Agent,
            SessionId = SessionId,
            Title = "Dense graph",
            Status = "in_progress",
            TurnCount = 2,
            Tags = ["dense", "slice-1"],
            Turns =
            [
                DenseTurn(ExistingRequestId, "2026-09-28T22:00:00Z"),
                DenseTurn(SecondRequestId, "2026-09-28T22:00:01Z"),
            ],
        };
    }

    internal static UnifiedRequestEntryDto DenseTurn(string requestId, string timestamp) => new()
    {
        RequestId = requestId,
        Timestamp = timestamp,
        QueryText = "dense turn " + requestId,
        Status = "in_progress",
        PlanFile = SessionLogTurnContextValidator.NoneSentinel,
        TodoId = SessionLogTurnContextValidator.NoneSentinel,
        Tags = ["turn-tag-a", "turn-tag-b"],
        ContextList = ["docs/a.md", "docs/b.md"],
        Actions =
        [
            new UnifiedActionDto { Order = 0, Description = "edit a", Type = "edit", Status = "completed", FilePath = "src/a.cs" },
            new UnifiedActionDto { Order = 1, Description = "edit b", Type = "edit", Status = "completed", FilePath = "src/b.cs" },
        ],
        ProcessingDialog =
        [
            new ProcessingDialogItemDto { Timestamp = timestamp, Role = "model", Content = "thinking", Category = "reasoning" },
            new ProcessingDialogItemDto { Timestamp = timestamp, Role = "tool", Content = "ran", Category = "tool_call" },
        ],
        Commits =
        [
            new SessionLogCommitDto
            {
                Sha = "sha-" + requestId,
                Branch = "develop",
                Message = "dense",
                Author = "Cursor",
                FilesChanged = ["src/a.cs"],
            },
        ],
        DesignDecisions = ["split the include graph"],
        RequirementsDiscovered = ["FR-MCP-TRIAGESTORE-002"],
        FilesModified = ["src/a.cs", "src/b.cs"],
        Blockers = ["cartesian join"],
    };
}
