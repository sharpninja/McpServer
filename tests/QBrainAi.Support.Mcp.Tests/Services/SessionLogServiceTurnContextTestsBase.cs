using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Notifications;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// FR-MCP-SESSIONLOGCTX-001-001; TR-MCP-SESSIONLOG-006-002: the per-test state and helpers that both halves of SessionLogServiceTurnContextTests use (the unit half in QBrainAi.Support.Mcp.Tests, the integration half in QBrainAi.Support.Mcp.EnvironmentIntegrationTests), defined once in a base class both halves derive from and compiled into both projects.
/// </summary>
/// <remarks>2026-11-07, FR-MCP-SESSIONLOGCTX-001-001; TR-MCP-SESSIONLOG-006-002; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: extracted unchanged from SessionLogServiceTurnContextTests (WorkspacePath, _db, _sut, .ctor, Dispose and CreateSession) when that class split into its unit half and its integration half (slice S7, hostile validation finding R14-F4), so both halves use one definition instead of a copy (DRY); only the accessibility changed, from private to internal, and the constructor takes the base class name and protected accessibility; xUnit still creates one instance of a half per case, so each case gets fresh state as before.</remarks>
public abstract class SessionLogServiceTurnContextTestsBase : IDisposable
{
    internal const string WorkspacePath = @"E:\tests\sessionlog-turn-context";
    internal readonly McpDbContext _db;
    internal readonly SessionLogService _sut;

    /// <summary>Creates an isolated in-memory service fixture.</summary>
    protected SessionLogServiceTurnContextTestsBase()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"SessionLogTurnCtx_{Guid.NewGuid()}")
            .Options;
        _db = new McpDbContext(options);
        _db.Database.EnsureCreated();
        _db.OverrideWorkspaceId(WorkspacePath);
        _sut = new SessionLogService(
            _db,
            NullLogger<SessionLogService>.Instance,
            Substitute.For<IChangeEventBus>(),
            new WorkspaceContext { WorkspacePath = WorkspacePath });
    }

    /// <inheritdoc />
    public void Dispose() => _db.Dispose();

    internal static UnifiedSessionLogDto CreateSession(string sessionId)
    {
        return new UnifiedSessionLogDto
        {
            SourceType = "Cursor",
            SessionId = sessionId,
            Title = "ctx",
            Model = "gpt-4",
            Status = "in_progress",
            TurnCount = 1,
            Turns =
            [
                new UnifiedRequestEntryDto
                {
                    RequestId = "req-20260304T113901Z-seed",
                    QueryText = "seed",
                    Status = "in_progress",
                    PlanFile = "docs/plans/seed.md",
                    TodoId = "MCP-SESSIONLOG-002",
                }
            ]
        };
    }
}
