namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>One fixed child-row readback for physical-constraint and stale-token experiments.</summary>
/// <param name="Id">Primary key.</param>
/// <param name="ParentId">Restrict-delete foreign key to compat_parents.Id.</param>
/// <param name="Title">Required text; nullable here only to represent a broken observation.</param>
/// <param name="Code">Globally unique fixture text.</param>
/// <param name="Revision">Explicit integer EF concurrency token.</param>
/// <remarks>2026-11-18: TASK-FEDPH0-002; FR-MCP-186 ac-DoltBaselineAndUpgrade; TR-MCP-FEDERATION-013; TEST-MCP-234.</remarks>
internal sealed record Phase0WriteFixtureRow(string Id, string ParentId, string? Title, string Code, int Revision);

/// <summary>Normalized actual physical rule, read from database metadata rather than EF flags alone.</summary>
/// <param name="Table">Physical table.</param>
/// <param name="Kind">PrimaryKey, Unique, NotNull or ForeignKey.</param>
/// <param name="Columns">Ordered constrained columns.</param>
/// <param name="PrincipalTable">Foreign-key principal table only.</param>
/// <param name="PrincipalColumns">Ordered principal key columns only.</param>
/// <param name="OnDelete">Explicit foreign-key delete behavior only.</param>
/// <remarks>2026-11-18: TASK-FEDPH0-002; FR-MCP-186 ac-DoltBaselineAndUpgrade; TR-MCP-FEDERATION-013; TEST-MCP-234.</remarks>
internal sealed record Phase0PhysicalRule(string Table, string Kind, IReadOnlyList<string> Columns,
    string? PrincipalTable, IReadOnlyList<string> PrincipalColumns, string? OnDelete);

/// <summary>Durable data, physical schema and actual EF result at one boundary.</summary>
/// <param name="ParentIds">All fixture parents in ordinal Id order.</param>
/// <param name="Rows">All children in ordinal Id order, independently reread.</param>
/// <param name="Rules">Complete specified physical-rule inventory, order independent.</param>
/// <param name="ReaderTokens">EF OriginalValues revisions for A/B at the checkpoint, or empty when no readers exist.</param>
/// <param name="AffectedRows">Actual SaveChanges return count; null outside a write or when SaveChanges throws.</param>
/// <param name="ExceptionType">Actual outer exception CLR full name, or null.</param>
/// <param name="NativeErrorCode">Provider-native constraint diagnostic, or null when none exists.</param>
/// <remarks>2026-11-18: TASK-FEDPH0-002; FR-MCP-186 ac-DoltBaselineAndUpgrade; TR-MCP-FEDERATION-013; TEST-MCP-234.</remarks>
internal sealed record Phase0RelationalWriteState(IReadOnlyList<string> ParentIds, IReadOnlyList<Phase0WriteFixtureRow> Rows,
    IReadOnlyList<Phase0PhysicalRule> Rules, IReadOnlyDictionary<string, int> ReaderTokens,
    int? AffectedRows, string? ExceptionType, string? NativeErrorCode);

/// <summary>Disposable EF fixture operations, with a later real provider binding and independent mocks first.</summary>
/// <remarks>2026-11-18: TASK-FEDPH0-002; FR-MCP-186 ac-DoltBaselineAndUpgrade; TR-MCP-FEDERATION-013; TEST-MCP-234.</remarks>
internal interface IPhase0RelationalWriteProbe
{
    /// <summary>Creates the two-table fixture, seeds parents P1/P2 and children C1/C2, then opens both stale readers when requested.</summary>
    /// <param name="scenario">concurrency, primary-key, unique-index, foreign-key, not-null or restrict-delete.</param>
    /// <returns>Actual initial data, metadata and reader tokens.</returns>
    Phase0RelationalWriteState? Open(string scenario);
    /// <summary>Performs the named invalid write or the sequential winner/stale concurrency save through EF.</summary>
    /// <remarks>
    /// winner-A sets C1.Title=winner and Revision=8 after both contexts originally read Revision=7.
    /// stale-B attempts C1.Title=loser and Revision=8 with B original Revision=7.
    /// primary-key inserts (C1,P1,candidate,gamma,1); unique-index inserts (C3,P1,candidate,alpha,1).
    /// foreign-key inserts (C3,missing-parent,candidate,gamma,1); not-null inserts (C3,P1,null,gamma,1).
    /// restrict-delete deletes P1 while C1 still references it. Invalid inserts must use explicit null/value assignments.
    /// Revision is configured as an EF concurrency token; ReaderTokens capture EF OriginalValues after each operation.
    /// Native MySQL error numbers are pinned from https://mysqlconnector.net/api/mysqlconnector/mysqlerrorcodetype/
    /// and https://dev.mysql.com/doc/mysql-errors/8.0/en/server-error-reference.html . This seam is the Dolt lane.
    /// </remarks>
    /// <param name="operation">Exact mutation defined above.</param>
    /// <returns>Actual exception/count and separately reread database state.</returns>
    Phase0RelationalWriteState? Write(string operation);
    /// <summary>Disposes all contexts and connections, reopens the store and rereads complete fixture data and rules.</summary>
    /// <returns>Durable observation with no reader contexts or new write result.</returns>
    Phase0RelationalWriteState? Reopen();
}

/// <summary>Asserts physical enforcement, atomic failed writes, optimistic concurrency and durable winner preservation.</summary>
/// <remarks>
/// 2026-11-18: TASK-FEDPH0-002; FR-MCP-186 ac-DoltBaselineAndUpgrade; TR-MCP-FEDERATION-013; TEST-MCP-234.
/// This representative fixture supplements, not replaces, full-model/provider mapping and migration proof.
/// Actual adapters must inspect physical database rules and capture real EF/native errors; no passed flag is accepted.
/// </remarks>
internal static class Phase0RelationalWriteContract
{
    /// <summary>Checks every checkpoint, including reopening after rejected writes.</summary>
    /// <param name="probe">Fresh independent script or later real disposable adapter.</param>
    /// <param name="scenario">The exact physical-constraint or concurrency experiment.</param>
    internal static void AssertWrites(IPhase0RelationalWriteProbe probe, string scenario)
    {
        Assert.Contains(scenario, new[] { "concurrency", "primary-key", "unique-index", "foreign-key", "not-null", "restrict-delete" });
        var concurrent = scenario == "concurrency";
        Check(probe.Open(scenario), false, concurrent ? Tokens(7) : new Dictionary<string, int>(), null, null);
        if (concurrent)
        {
            Check(probe.Write("winner-A"), true, Tokens(8), 1, null);
            Check(probe.Write("stale-B"), true, Tokens(8), null, "Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException");
        }
        else
        {
            Check(probe.Write(scenario), false, new Dictionary<string, int>(), null, "Microsoft.EntityFrameworkCore.DbUpdateException",
                scenario switch { "primary-key" or "unique-index" => "1062", "foreign-key" => "1452", "not-null" => "1048", _ => "1451" });
        }
        Check(probe.Reopen(), concurrent, new Dictionary<string, int>(), null, null);
    }

    /// <summary>Checks exact independent rows, physical rules, tokens, save count and exception observations.</summary>
    /// <param name="actual">Actual checkpoint, not an expected success value.</param>
    /// <param name="winnerSaved">Whether context A's revision-eight winner is durable.</param>
    /// <param name="tokens">Expected context original/current revisions.</param>
    /// <param name="affected">Actual write count expected, or null outside a write.</param>
    /// <param name="exception">Required outer CLR type, or null.</param>
    /// <param name="nativeError">Required MySQL-protocol error number in the Dolt lane, or null.</param>
    private static void Check(Phase0RelationalWriteState? actual, bool winnerSaved,
        IReadOnlyDictionary<string, int> tokens, int? affected, string? exception, string? nativeError = null)
    {
        Assert.NotNull(actual);
        Assert.Equal(new[] { "P1", "P2" }, actual.ParentIds);
        Assert.NotNull(actual.Rows);
        Assert.Equal(2, actual.Rows.Count);
        Assert.Equal(new Phase0WriteFixtureRow("C1", "P1", winnerSaved ? "winner" : "seed", "alpha", winnerSaved ? 8 : 7), actual.Rows[0]);
        Assert.Equal(new Phase0WriteFixtureRow("C2", "P2", "unrelated", "beta", 11), actual.Rows[1]);
        Assert.NotNull(actual.Rules);
        Assert.Equal(5, actual.Rules.Count);
        Rule(actual.Rules, "compat_parents", "PrimaryKey", ["Id"], null, [], null);
        Rule(actual.Rules, "compat_children", "PrimaryKey", ["Id"], null, [], null);
        Rule(actual.Rules, "compat_children", "Unique", ["Code"], null, [], null);
        Rule(actual.Rules, "compat_children", "NotNull", ["Title"], null, [], null);
        Rule(actual.Rules, "compat_children", "ForeignKey", ["ParentId"], "compat_parents", ["Id"], "Restrict");
        Assert.NotNull(actual.ReaderTokens);
        Assert.Equal(tokens.OrderBy(pair => pair.Key, StringComparer.Ordinal), actual.ReaderTokens.OrderBy(pair => pair.Key, StringComparer.Ordinal));
        Assert.Equal(affected, actual.AffectedRows);
        Assert.Equal(exception, actual.ExceptionType);
        Assert.Equal(nativeError, actual.NativeErrorCode);
    }

    /// <summary>Checks one unique physical rule without accepting omitted or duplicate inventory entries.</summary>
    /// <param name="rules">Actual full rule inventory.</param>
    /// <param name="table">Required physical table.</param>
    /// <param name="kind">Required rule kind.</param>
    /// <param name="columns">Ordered constrained columns.</param>
    /// <param name="principalTable">Expected FK principal or null.</param>
    /// <param name="principalColumns">Ordered FK principal columns or empty.</param>
    /// <param name="onDelete">Expected FK delete behavior or null.</param>
    private static void Rule(IReadOnlyList<Phase0PhysicalRule> rules, string table, string kind,
        IReadOnlyList<string> columns, string? principalTable, IReadOnlyList<string> principalColumns, string? onDelete)
    {
        Assert.All(rules, Assert.NotNull);
        var actual = Assert.Single(rules, rule => rule.Table == table && rule.Kind == kind);
        Assert.Equal(columns, actual.Columns);
        Assert.Equal(principalTable, actual.PrincipalTable);
        Assert.Equal(principalColumns, actual.PrincipalColumns);
        Assert.Equal(onDelete, actual.OnDelete);
    }

    /// <summary>Context A advances after its successful save while B retains the original revision seven.</summary>
    /// <param name="a">A's observed token after its own last operation.</param>
    private static IReadOnlyDictionary<string, int> Tokens(int a) => new Dictionary<string, int> { ["A"] = a, ["B"] = 7 };
}
