using System.Diagnostics;
using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Notifications;
using QBrainAi.Support.Mcp.Options;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-TRIAGESTORE-001: the per-test state and helpers that both halves of SessionLogTriageStoreTests use (the unit half in QBrainAi.Support.Mcp.Tests, the integration half in QBrainAi.Support.Mcp.EnvironmentIntegrationTests), defined once in a base class both halves derive from and compiled into both projects.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-TRIAGESTORE-001; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: extracted unchanged from SessionLogTriageStoreTests (WorkspacePath, _db, _sut, .ctor, Dispose and CreateSession) when that class split into its unit half and its integration half (slice S7, hostile validation finding R14-F4), so both halves use one definition instead of a copy (DRY); only the accessibility changed, from private to internal, and the constructor takes the base class name and protected accessibility; xUnit still creates one instance of a half per case, so each case gets fresh state as before.</remarks>
public abstract class SessionLogTriageStoreTestsBase : IDisposable
{
    internal const string WorkspacePath = @"E:\tests\sessionlog-triage-store";
    internal readonly McpDbContext _db;
    internal readonly SessionLogService _sut;

    /// <summary>Builds an in-memory session-log service.</summary>
    protected SessionLogTriageStoreTestsBase()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"SessionLogTriageStore_{Guid.NewGuid():N}")
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

    internal static UnifiedSessionLogDto CreateSession(
        string sessionId,
        string turnStatus = "in_progress",
        string timestamp = "2026-08-18T20:00:00Z")
    {
        return new UnifiedSessionLogDto
        {
            SourceType = "Cursor",
            SessionId = sessionId,
            Title = "Triage store",
            Status = "in_progress",
            TurnCount = 1,
            Turns =
            [
                new UnifiedRequestEntryDto
                {
                    RequestId = "req-20260818T200000Z-entry-001",
                    Timestamp = timestamp,
                    QueryText = "triage store",
                    Status = turnStatus,
                    PlanFile = SessionLogTurnContextValidator.NoneSentinel,
                    TodoId = SessionLogTurnContextValidator.NoneSentinel,
                },
            ],
        };
    }
}
