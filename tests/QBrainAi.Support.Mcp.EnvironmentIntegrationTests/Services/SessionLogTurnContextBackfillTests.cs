using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using static QBrainAi.Support.Mcp.Tests.Services.SessionLogTurnContextBackfillTestsSupport;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Services;

/// <summary>
/// AC-TR-MCP-SESSIONLOG-006-005 / AC-FR-MCP-SESSIONLOGCTX-001-006:
/// backfill upgrades only None columns.
/// </summary>
/// <remarks>2026-11-07, TR-MCP-SESSIONLOG-006-005; FR-MCP-SESSIONLOGCTX-001-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Services/SessionLogTurnContextBackfillTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 1 Category=Integration case moved unchanged with the class attributes; the helpers both halves use come from Services/SessionLogTurnContextBackfillTestsSupport.cs of QBrainAi.Support.Mcp.Tests, linked into this project (DRY); only the namespace changed.</remarks>
public sealed class SessionLogTurnContextBackfillTests
{
    /// <summary>AC-FR-MCP-SESSIONLOGCTX-001-006: fake ~ history upgrades an empty turn.</summary>
    /// <remarks>Integration: writes and reads a test-owned physical agent-history tree (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task RunAsync_UsesAgentHistoryUnderFakeHome_WhenTurnTextHasNoTodo()
    {
        var home = Path.Combine(Path.GetTempPath(), "sesslog-bf-" + Guid.NewGuid().ToString("N"));
        var hist = Path.Combine(home, ".grok", "sessions", "sid-xyz");
        Directory.CreateDirectory(hist);
        File.WriteAllText(Path.Combine(hist, "t.jsonl"), "TODO MCP-HOMEBF-001");
        try
        {
            await using var db = CreateDb();
            var session = AddSession(db);
            session.AgentSessionId = "sid-xyz";
            session.Turns.Add(new SessionLogTurnEntity { RequestId = "req-20260304T113901Z-bf5", QueryText = "empty", PlanFile = "None", TodoId = "None" });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
            var sut = new SessionLogTurnContextBackfill(db, new SessionLogTurnContextExtractor(), NullLogger<SessionLogTurnContextBackfill>.Instance);
            await sut.RunAsync(TestContext.Current.CancellationToken, home).ConfigureAwait(true);
            Assert.Equal("MCP-HOMEBF-001", (await db.SessionLogTurns.SingleAsync(TestContext.Current.CancellationToken).ConfigureAwait(true)).TodoId);
        }
        finally
        {
            Directory.Delete(home, recursive: true);
        }
    }
}
