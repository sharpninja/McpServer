using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage.Entities;
using Xunit;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Services;

/// <summary>
/// AC-FR-MCP-SESSIONLOGCTX-001-006 / AC-TR-MCP-SESSIONLOG-006-003 / TEST-MCP-SESSIONLOG-006:
/// extractor ranking from turn contents and fake ~ history.
/// </summary>
/// <remarks>2026-11-07, FR-MCP-SESSIONLOGCTX-001-006; TR-MCP-SESSIONLOG-006-003; TEST-MCP-SESSIONLOG-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Services/SessionLogTurnContextExtractorTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 1 Category=Integration case moved unchanged with the class attributes; only the namespace changed.</remarks>
public sealed class SessionLogTurnContextExtractorTests
{
    /// <summary>AC-FR-MCP-SESSIONLOGCTX-001-006: ~ history supplies a TODO when the turn text has none.</summary>
    /// <remarks>Integration: writes and reads an owned physical agent-history tree (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void Extract_ExactPathUnderUserProfileHistory_UsedWhenTurnHasNoPlan()
    {
        var home = Path.Combine(Path.GetTempPath(), "sesslog-extract-" + Guid.NewGuid().ToString("N"));
        var histDir = Path.Combine(home, ".grok", "sessions", "agent-abc");
        Directory.CreateDirectory(histDir);
        try
        {
            File.WriteAllText(Path.Combine(histDir, "chat.jsonl"), "working MCP-HISTORY-001 on docs/plans/from-home.md");
            var turn = new SessionLogTurnEntity();
            var result = new SessionLogTurnContextExtractor().Extract(turn, @"F:\ws", home, agentSessionId: "agent-abc");
            Assert.Equal("MCP-HISTORY-001", result.TodoId);
            Assert.Equal("docs/plans/from-home.md", result.PlanFile);
        }
        finally
        {
            Directory.Delete(home, recursive: true);
        }
    }
}
