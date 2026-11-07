using System.Data;
using System.Diagnostics;
using QBrainAi.Support.Mcp.Controllers;
using QBrainAi.Support.Mcp.Requirements;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Requirements;

/// <summary>
/// TEST-MCP-REQRECOVERY-001: the helpers that both halves of RequirementsRecoveryTests use (the unit half in QBrainAi.Support.Mcp.Tests, the integration half in QBrainAi.Support.Mcp.EnvironmentIntegrationTests), defined once and compiled into both projects.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-REQRECOVERY-001; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: extracted unchanged from RequirementsRecoveryTests (WorkspacePath, Request, Item, Build, OpenConnection and Count) when that class split into its unit half and its integration half (slice S7, hostile validation finding R14-F4), so both halves use one definition instead of a copy (DRY); only the accessibility changed, from private to internal.</remarks>
internal static class RequirementsRecoveryTestsSupport
{
    internal const string WorkspacePath = "/tmp/mcp-req-recovery";

    internal static RequirementsRecoveryRequest Request(string key, params RequirementsRecoveryItemRequest[] items)
        => new() { IdempotencyKey = key, Items = items.ToList() };

    internal static RequirementsRecoveryItemRequest Item(string kind, string id, string title)
        => new() { Kind = kind, Id = id, Title = title, Body = title + " body" };

    internal static (RequirementsRecoveryService Sut, McpDbContext Db) Build(
        SqliteConnection connection,
        IInterceptor? interceptor = null,
        TimeSpan? budget = null)
    {
        var builder = new DbContextOptionsBuilder<McpDbContext>().UseSqlite(connection);
        if (interceptor is not null)
            builder.AddInterceptors(interceptor);
        var workspace = new WorkspaceContext { WorkspacePath = WorkspacePath };
        var db = new McpDbContext(builder.Options, workspace);
        db.OverrideWorkspaceId(WorkspacePath);
        return (new RequirementsRecoveryService(db, workspace, budget), db);
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
}
