using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Notifications;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TR-MCP-TRANSCRIPT-004; TEST-MCP-TRANSCRIPT-012: the helpers that both halves of SessionLogResubmissionReviveTests use (the unit half in QBrainAi.Support.Mcp.Tests, the integration half in QBrainAi.Support.Mcp.EnvironmentIntegrationTests), defined once and compiled into both projects.
/// </summary>
/// <remarks>2026-11-07, TR-MCP-TRANSCRIPT-004; TEST-MCP-TRANSCRIPT-012; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: extracted unchanged from SessionLogResubmissionReviveTests (WorkspacePath, Agent, OpenConnection, BuildSut and Submit) when that class split into its unit half and its integration half (slice S7, hostile validation finding R14-F4), so both halves use one definition instead of a copy (DRY); only the accessibility changed, from private to internal.</remarks>
internal static class SessionLogResubmissionReviveTestsSupport
{
    internal const string WorkspacePath = @"E:\tests\sessionlog-resubmit-revive";
    internal const string Agent = "Codex";

    internal static SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return connection;
    }

    internal static (SessionLogService Sut, McpDbContext Db) BuildSut(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<McpDbContext>().UseSqlite(connection).Options;
        var workspaceContext = new WorkspaceContext { WorkspacePath = WorkspacePath };
        var db = new McpDbContext(options, workspaceContext);
        db.Database.EnsureCreated();
        var sut = new SessionLogService(db, NullLogger<SessionLogService>.Instance, Substitute.For<IChangeEventBus>(), workspaceContext);
        return (sut, db);
    }

    internal static void Submit(SqliteConnection connection, UnifiedSessionLogDto dto, string? sourceFilePath = null)
    {
        var (sut, db) = BuildSut(connection);
        using (db)
            sut.SubmitAsync(dto, sourceFilePath, contentHash: sourceFilePath is null ? null : "hash-1").GetAwaiter().GetResult();
    }
}
