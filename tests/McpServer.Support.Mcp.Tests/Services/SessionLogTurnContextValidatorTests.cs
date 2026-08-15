using McpServer.Support.Mcp.Services;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// AC-TR-MCP-SESSIONLOG-006-001 / AC-FR-MCP-SESSIONLOGCTX-001-002 / AC-FR-MCP-SESSIONLOGCTX-001-003 /
/// AC-FR-MCP-SESSIONLOGCTX-001-004 / TEST-MCP-SESSIONLOG-006: validates required planFile/todoId rules.
/// </summary>
public sealed class SessionLogTurnContextValidatorTests
{
    /// <summary>AC-FR-MCP-SESSIONLOGCTX-001-002: None/None is a valid new-entry pair.</summary>
    [Fact]
    public void ValidateForNewEntry_BothNone_Succeeds()
    {
        var result = SessionLogTurnContextValidator.ValidateForNewEntry("None", "None");
        Assert.Equal("None", result.PlanFile);
        Assert.Equal("None", result.TodoId);
    }

    /// <summary>AC-FR-MCP-SESSIONLOGCTX-001-004: relative plan path slash-normalizes.</summary>
    [Fact]
    public void ValidateForNewEntry_ValidPlanAndTodo_Succeeds_AndNormalizesSlashes()
    {
        var result = SessionLogTurnContextValidator.ValidateForNewEntry(@"docs\plans\foo.md", "MCP-SESSIONLOG-002");
        Assert.Equal("docs/plans/foo.md", result.PlanFile);
        Assert.Equal("MCP-SESSIONLOG-002", result.TodoId);
    }

    /// <summary>AC-FR-MCP-SESSIONLOGCTX-001-003: omitted planFile is rejected.</summary>
    [Fact]
    public void ValidateForNewEntry_NullPlanFile_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => SessionLogTurnContextValidator.ValidateForNewEntry(null, "None"));
        Assert.Contains("planFile", ex.Message, StringComparison.Ordinal);
    }

    /// <summary>AC-FR-MCP-SESSIONLOGCTX-001-003: empty todoId is rejected.</summary>
    [Fact]
    public void ValidateForNewEntry_OmittedTodoIdEmpty_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => SessionLogTurnContextValidator.ValidateForNewEntry("None", "  "));
    }

    /// <summary>AC-FR-MCP-SESSIONLOGCTX-001-003: whitespace planFile is rejected.</summary>
    [Fact]
    public void ValidateForNewEntry_WhitespacePlanFile_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => SessionLogTurnContextValidator.ValidateForNewEntry("   ", "None"));
    }

    /// <summary>AC-TR-MCP-SESSIONLOG-006-001: lowercase none is not the sentinel.</summary>
    [Fact]
    public void ValidateForNewEntry_LowercaseNone_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => SessionLogTurnContextValidator.ValidateForNewEntry("none", "None"));
    }

    /// <summary>AC-TR-MCP-SESSIONLOG-006-001: FR ids are not TODO ids.</summary>
    [Fact]
    public void ValidateForNewEntry_FrIdAsTodo_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => SessionLogTurnContextValidator.ValidateForNewEntry("None", "FR-MCP-001"));
        Assert.Contains("todoId", ex.Message, StringComparison.Ordinal);
    }

    /// <summary>AC-FR-MCP-SESSIONLOGCTX-001-004: exact Windows path is accepted.</summary>
    [Fact]
    public void ValidateForNewEntry_ExactWindowsPlan_Succeeds()
    {
        var result = SessionLogTurnContextValidator.ValidateForNewEntry(@"C:\Users\kingd\docs\plan.md", "None");
        Assert.Equal("C:/Users/kingd/docs/plan.md", result.PlanFile);
    }

    /// <summary>AC-FR-MCP-SESSIONLOGCTX-001-004: ~/ expands to the user profile.</summary>
    [Fact]
    public void ValidateForNewEntry_HomeRelativePlan_ExpandsAndSucceeds()
    {
        var expanded = SessionLogTurnContextValidator.NormalizePlanFile("~/plans/x.md", userProfilePath: @"C:\Users\fake");
        Assert.Contains("plans/x.md", expanded.Replace('\\', '/'), StringComparison.Ordinal);
        Assert.DoesNotContain("~", expanded, StringComparison.Ordinal);
    }

    /// <summary>AC-FR-MCP-SESSIONLOGCTX-001-004: .. is rejected.</summary>
    [Fact]
    public void ValidateForNewEntry_ParentSegmentPlan_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => SessionLogTurnContextValidator.ValidateForNewEntry("docs/plans/../appsettings.yaml", "None"));
    }

    /// <summary>AC-TR-MCP-SESSIONLOG-006-001: canonical and ISSUE ids succeed.</summary>
    [Fact]
    public void ValidateForNewEntry_CanonicalTodoAndIssueId_Succeed()
    {
        Assert.Equal("ISSUE-17", SessionLogTurnContextValidator.ValidateForNewEntry("None", "ISSUE-17").TodoId);
        Assert.Equal("PLAN-FOO-001", SessionLogTurnContextValidator.ValidateForNewEntry("None", "PLAN-FOO-001").TodoId);
    }

    /// <summary>AC-TR-MCP-SESSIONLOG-006-002: omitted fields on additive update are allowed.</summary>
    [Fact]
    public void ValidateIfSupplied_BothNull_Succeeds()
    {
        SessionLogTurnContextValidator.ValidateIfSupplied(null, null);
    }

    /// <summary>AC-TR-MCP-SESSIONLOG-006-002: supplied invalid values still throw.</summary>
    [Fact]
    public void ValidateIfSupplied_InvalidWhenPresent_Throws()
    {
        Assert.Throws<ArgumentException>(() => SessionLogTurnContextValidator.ValidateIfSupplied(null, "FR-MCP-001"));
    }
}
