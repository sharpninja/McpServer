using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TR-MCP-SESSIONLOG-006-005; FR-MCP-SESSIONLOGCTX-001-006: the helpers that both halves of SessionLogTurnContextBackfillTests use (the unit half in QBrainAi.Support.Mcp.Tests, the integration half in QBrainAi.Support.Mcp.EnvironmentIntegrationTests), defined once and compiled into both projects.
/// </summary>
/// <remarks>2026-11-07, TR-MCP-SESSIONLOG-006-005; FR-MCP-SESSIONLOGCTX-001-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: extracted unchanged from SessionLogTurnContextBackfillTests (CreateDb and AddSession) when that class split into its unit half and its integration half (slice S7, hostile validation finding R14-F4), so both halves use one definition instead of a copy (DRY); only the accessibility changed, from private to internal.</remarks>
internal static class SessionLogTurnContextBackfillTestsSupport
{
    internal static McpDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase("bf-" + Guid.NewGuid().ToString("N"))
            .Options;
        var db = new McpDbContext(options);
        db.Database.EnsureCreated();
        db.OverrideWorkspaceId(@"F:\ws");
        return db;
    }

    internal static SessionLogEntity AddSession(McpDbContext db)
    {
        var session = new SessionLogEntity
        {
            SourceType = "Cursor",
            SessionId = "Cursor-20260304T113901Z-bf",
            WorkspaceId = @"F:\ws",
        };
        db.SessionLogs.Add(session);
        return session;
    }
}
