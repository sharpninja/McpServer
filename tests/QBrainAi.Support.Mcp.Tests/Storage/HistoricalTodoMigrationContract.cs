using System;
using System.Collections.Generic;
using System.Linq;

namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>
/// TEST-MCP-TRIAGE-004: retained historical migration assertions over raw stored rows.
/// PATHNORM query conversion is deliberately outside this historical observation seam.
/// </summary>
public static class HistoricalTodoMigrationContract
{
    private const string Workspace = @"Q:\__mcp_unit_test__\QBrainAi";
    private const string StaleWorkspace = @"C:\ProgramData\QBrainAi";

    /// <summary>Raw historical TODO columns required by the original migration assertions.</summary>
    /// <param name="WorkspaceId">Unconverted stored workspace key.</param>
    /// <param name="Id">Stored TODO identity.</param>
    /// <param name="Title">Stored title.</param>
    /// <param name="Section">Stored section.</param>
    /// <param name="Priority">Stored priority.</param>
    /// <param name="Done">Stored completion state.</param>
    public sealed record Row(string WorkspaceId, string Id, string Title, string Section, string Priority, bool Done);

    /// <summary>
    /// Retains the original anchor-backfill field, stale-row absence and table-removal assertions.
    /// </summary>
    /// <param name="rows">Actual raw rows, without parameter conversion.</param>
    /// <param name="obsoleteTableCount">Actual number of remaining TodoRecords tables.</param>
    public static void AssertAnchor(IReadOnlyList<Row> rows, int obsoleteTableCount)
    {
        var todo = rows.Single(item => item.WorkspaceId == Workspace && item.Id == "BUG-TRIAGE-123");
        Equal("Backfilled triage bug", todo.Title, "title");
        Equal("Backlog", todo.Section, "section");
        Equal("medium", todo.Priority, "priority");
        Require(!todo.Done, "done");
        Require(!rows.Any(item => item.WorkspaceId == StaleWorkspace && item.Id == "BUG-TRIAGE-123"), "stale anchor remains");
        Require(obsoleteTableCount == 0, "obsolete table remains");
    }

    /// <summary>Retains the original copied fields and non-destructive stale-row retention assertion.</summary>
    /// <param name="rows">Actual raw rows, without parameter conversion.</param>
    public static void AssertRepair(IReadOnlyList<Row> rows)
    {
        var todo = rows.Single(item => item.WorkspaceId == Workspace && item.Id == "BUG-TRIAGE-124");
        Equal("Existing stale workspace title", todo.Title, "title");
        Equal("Backlog", todo.Section, "section");
        Equal("high", todo.Priority, "priority");
        Require(!todo.Done, "done");
        Require(rows.Any(item => item.WorkspaceId == StaleWorkspace && item.Id == "BUG-TRIAGE-124"), "source row was destroyed");
    }

    /// <summary>
    /// Proves the same assertion runner accepts independently pinned historical results and
    /// rejects missing, wrong-workspace, damaged, duplicated and destructive row effects.
    /// </summary>
    /// <returns>Positive scenario and rejected negative-control counts.</returns>
    public static int[] VerifyMockGate()
    {
        // Independently authored literals come from the original historical SQL fixtures.
        var anchor = new Row(Workspace, "BUG-TRIAGE-123", "Backfilled triage bug", "Backlog", "medium", false);
        var source = new Row(StaleWorkspace, "BUG-TRIAGE-124", "Existing stale workspace title", "Backlog", "high", false);
        var copy = new Row(Workspace, "BUG-TRIAGE-124", "Existing stale workspace title", "Backlog", "high", false);
        AssertAnchor(new[] { anchor }, 0);
        AssertRepair(new[] { source, copy });
        var rejected = 0;
        void Reject(Action assertion)
        {
            try { assertion(); }
            catch (InvalidOperationException) { rejected++; return; }
            throw new InvalidOperationException("A damaged historical result was accepted.");
        }
        Reject(() => AssertAnchor(Array.Empty<Row>(), 0));
        Reject(() => AssertAnchor(new[] { anchor with { WorkspaceId = "wrong-workspace" } }, 0));
        Reject(() => AssertAnchor(new[] { anchor with { Id = "wrong-id" } }, 0));
        Reject(() => AssertAnchor(new[] { anchor with { Title = "damaged" } }, 0));
        Reject(() => AssertAnchor(new[] { anchor with { Section = "damaged" } }, 0));
        Reject(() => AssertAnchor(new[] { anchor with { Priority = "damaged" } }, 0));
        Reject(() => AssertAnchor(new[] { anchor with { Done = true } }, 0));
        Reject(() => AssertAnchor(new[] { anchor, anchor }, 0));
        Reject(() => AssertAnchor(new[] { anchor, anchor with { WorkspaceId = StaleWorkspace } }, 0));
        Reject(() => AssertAnchor(new[] { anchor }, 1));
        Reject(() => AssertRepair(new[] { source }));
        Reject(() => AssertRepair(new[] { source, copy with { WorkspaceId = "wrong-workspace" } }));
        Reject(() => AssertRepair(new[] { source, copy with { Id = "wrong-id" } }));
        Reject(() => AssertRepair(new[] { source, copy with { Title = "damaged" } }));
        Reject(() => AssertRepair(new[] { source, copy with { Section = "damaged" } }));
        Reject(() => AssertRepair(new[] { source, copy with { Priority = "damaged" } }));
        Reject(() => AssertRepair(new[] { source, copy with { Done = true } }));
        Reject(() => AssertRepair(new[] { source, copy, copy }));
        Reject(() => AssertRepair(new[] { copy }));
        Reject(() => AssertRepair(new[] { source with { WorkspaceId = "wrong-workspace" }, copy }));
        Reject(() => AssertRepair(new[] { source with { Id = "wrong-id" }, copy }));
        Require(rejected == 21, "negative-control denominator changed");
        return new[] { 2, rejected };
    }

    private static void Equal(string expected, string actual, string field) =>
        Require(string.Equals(expected, actual, StringComparison.Ordinal), field + " changed");

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
