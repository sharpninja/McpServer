using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage.Entities;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// AC-FR-MCP-SESSIONLOGCTX-001-006 / AC-TR-MCP-SESSIONLOG-006-003 / TEST-MCP-SESSIONLOG-006:
/// extractor ranking from turn contents and fake ~ history.
/// </summary>
public sealed class SessionLogTurnContextExtractorTests
{
    private readonly SessionLogTurnContextExtractor _sut = new();

    /// <summary>AC-FR-MCP-SESSIONLOGCTX-001-006: no signals yield None/None.</summary>
    [Fact]
    public void Extract_NoSignals_ReturnsNoneNone()
    {
        var result = _sut.Extract(new SessionLogTurnEntity(), workspacePath: @"F:\ws", userProfilePath: IsolatedHome());
        Assert.Equal("None", result.PlanFile);
        Assert.Equal("None", result.TodoId);
    }

    /// <summary>AC-TR-MCP-SESSIONLOG-006-003: a tag TODO id wins over dialog mentions.</summary>
    [Fact]
    public void Extract_TagIsCanonicalTodo_WinsOverDialogMention()
    {
        var turn = new SessionLogTurnEntity();
        turn.Tags.Add(new SessionLogTurnTagEntity { Tag = "MCP-SESSIONLOG-002" });
        turn.ProcessingDialog.Add(new SessionLogProcessingDialogEntity { Role = "model", Content = "also PLAN-OTHER-001", Ordinal = 0 });
        var result = _sut.Extract(turn, @"F:\ws", IsolatedHome());
        Assert.Equal("MCP-SESSIONLOG-002", result.TodoId);
    }

    /// <summary>AC-TR-MCP-SESSIONLOG-006-003: a single contextList plan path is used.</summary>
    [Fact]
    public void Extract_SinglePlanPathInContextList_ReturnsRelativePath()
    {
        var turn = new SessionLogTurnEntity();
        turn.ContextItems.Add(new SessionLogTurnContextEntity { ContextItem = "docs/plans/foo.md", Ordinal = 0 });
        var result = _sut.Extract(turn, @"F:\ws", IsolatedHome());
        Assert.Equal("docs/plans/foo.md", result.PlanFile);
    }

    /// <summary>AC-FR-MCP-SESSIONLOGCTX-001-004: exact workspace path is kept.</summary>
    [Fact]
    public void Extract_ExactPathUnderWorkspace_KeptAsValidPlanFile()
    {
        var turn = new SessionLogTurnEntity();
        turn.ContextItems.Add(new SessionLogTurnContextEntity { ContextItem = @"F:\ws\docs\plans\x.md", Ordinal = 0 });
        var result = _sut.Extract(turn, @"F:\ws", IsolatedHome());
        Assert.Equal("F:/ws/docs/plans/x.md", result.PlanFile);
    }

    /// <summary>AC-FR-MCP-SESSIONLOGCTX-001-006: ~ history supplies a TODO when the turn text has none.</summary>
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
            var result = _sut.Extract(turn, @"F:\ws", home, agentSessionId: "agent-abc");
            Assert.Equal("MCP-HISTORY-001", result.TodoId);
            Assert.Equal("docs/plans/from-home.md", result.PlanFile);
        }
        finally
        {
            Directory.Delete(home, recursive: true);
        }
    }

    /// <summary>AC-FR-MCP-SESSIONLOGCTX-001-004: ~/ in history expands.</summary>
    [Fact]
    public void Extract_HomeRelativeHistoryHit_ExpandsAndReturnsExactPath()
    {
        var home = Path.Combine(Path.GetTempPath(), "sesslog-home-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(home);
        try
        {
            var turn = new SessionLogTurnEntity();
            turn.ContextItems.Add(new SessionLogTurnContextEntity { ContextItem = "~/plans/live.md", Ordinal = 0 });
            var result = _sut.Extract(turn, @"F:\ws", home);
            Assert.Contains("plans/live.md", result.PlanFile.Replace('\\', '/'), StringComparison.Ordinal);
            Assert.DoesNotContain("~", result.PlanFile, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(home, recursive: true);
        }
    }

    /// <summary>AC-TR-MCP-SESSIONLOG-006-003: tied TODO ids stay None.</summary>
    [Fact]
    public void Extract_TiedTodoIds_ReturnsNone()
    {
        var turn = new SessionLogTurnEntity { QueryText = "MCP-AAAA-001 and PLAN-BBBB-001" };
        var result = _sut.Extract(turn, @"F:\ws", IsolatedHome());
        Assert.Equal("None", result.TodoId);
    }

    /// <summary>AC-TR-MCP-SESSIONLOG-006-003: FR/TR/TEST are not TODO ids.</summary>
    [Fact]
    public void Extract_DoesNotTreatFrTrTestAsTodo()
    {
        var turn = new SessionLogTurnEntity { QueryText = "FR-MCP-001 TR-MCP-SESSIONLOG-006 TEST-MCP-001" };
        var result = _sut.Extract(turn, @"F:\ws", IsolatedHome());
        Assert.Equal("None", result.TodoId);
    }

    /// <summary>AC-TR-MCP-SESSIONLOG-006-003: docs/plans wins over other md files.</summary>
    [Fact]
    public void Extract_PrefersDocsPlansOverOtherMd()
    {
        var turn = new SessionLogTurnEntity();
        turn.ContextItems.Add(new SessionLogTurnContextEntity { ContextItem = "readme.md", Ordinal = 0 });
        turn.ContextItems.Add(new SessionLogTurnContextEntity { ContextItem = "docs/plans/real.md", Ordinal = 1 });
        var result = _sut.Extract(turn, @"F:\ws", IsolatedHome());
        Assert.Equal("docs/plans/real.md", result.PlanFile);
    }

    /// <summary>AC-FR-MCP-SESSIONLOGCTX-001-006: extractor does not invent values.</summary>
    [Fact]
    public void Extract_DoesNotInventIds()
    {
        var turn = new SessionLogTurnEntity { QueryText = "no identifiers here" };
        var result = _sut.Extract(turn, @"F:\ws", IsolatedHome());
        Assert.Equal("None", result.PlanFile);
        Assert.Equal("None", result.TodoId);
    }

    private static string IsolatedHome() =>
        Path.Combine(Path.GetTempPath(), "sesslog-isolated-home-missing");
}
