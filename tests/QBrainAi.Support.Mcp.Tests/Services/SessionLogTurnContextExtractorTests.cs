using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage.Entities;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// AC-FR-MCP-SESSIONLOGCTX-001-006 / AC-TR-MCP-SESSIONLOG-006-003 / TEST-MCP-SESSIONLOG-006:
/// extractor ranking from turn contents and fake ~ history.
/// </summary>
/// <remarks>2026-11-07, FR-MCP-SESSIONLOGCTX-001-006; TR-MCP-SESSIONLOG-006-003; TEST-MCP-SESSIONLOG-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the unit half of this class: its 1 Category=Integration case (Extract_ExactPathUnderUserProfileHistory_UsedWhenTurnHasNoPlan) moved unchanged to the class of the same name in the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S7, hostile validation finding R14-F4), so this unit-owned project holds only unit cases; the unit cases and the class attributes are unchanged.</remarks>
public sealed class SessionLogTurnContextExtractorTests
{
    private readonly SessionLogTurnContextExtractor _sut = new(new DirectoryTreeWalker(_ => []));

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

    /// <summary>AC-FR-MCP-SESSIONLOGCTX-001-004: ~/ in history expands.</summary>
    [Fact]
    public void Extract_HomeRelativeHistoryHit_ExpandsAndReturnsExactPath()
    {
        const string home = "/fixture/home";
        var turn = new SessionLogTurnEntity();
        turn.ContextItems.Add(new SessionLogTurnContextEntity { ContextItem = "~/plans/live.md", Ordinal = 0 });
        var result = _sut.Extract(turn, @"F:\ws", home);
        Assert.Contains("plans/live.md", result.PlanFile.Replace('\\', '/'), StringComparison.Ordinal);
        Assert.DoesNotContain("~", result.PlanFile, StringComparison.Ordinal);
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

    private static string IsolatedHome() => "/fixture/no-history";
}
