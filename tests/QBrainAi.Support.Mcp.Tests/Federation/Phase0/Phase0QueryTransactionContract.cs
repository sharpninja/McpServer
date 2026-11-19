namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>One observed LINQ aggregate, ordered by its fixed group identity.</summary>
/// <param name="HasNotes">Whether the fixture note is non-null, including empty text.</param>
/// <param name="Count">Rows actually grouped.</param>
/// <param name="Total">Exact decimal sum read through the provider.</param>
/// <remarks>2026-11-18: TASK-FEDPH0-002; FR-MCP-186 ac-DoltBaselineAndUpgrade; TR-MCP-FEDERATION-013; TEST-MCP-234.</remarks>
internal sealed record Phase0QueryAggregate(bool HasNotes, int Count, decimal Total);

/// <summary>Actual query results and raw-command parameters, never a passed flag.</summary>
/// <param name="Rows">Ordered materialized rows.</param>
/// <param name="Groups">Ordered materialized aggregates.</param>
/// <param name="RawSql">Executed raw command only; null for the LINQ cases.</param>
/// <param name="Parameters">Bound raw parameters only; empty for LINQ.</param>
/// <remarks>2026-11-18: TASK-FEDPH0-002; FR-MCP-186 ac-DoltBaselineAndUpgrade; TR-MCP-FEDERATION-013; TEST-MCP-234.</remarks>
internal sealed record Phase0QueryObservation(IReadOnlyList<Phase0FixtureRow> Rows,
    IReadOnlyList<Phase0QueryAggregate> Groups, string? RawSql, IReadOnlyDictionary<string, string> Parameters);

/// <summary>Executes the fixed query against a fresh disposable seeded database in the later real adapter.</summary>
/// <remarks>2026-11-18: TASK-FEDPH0-002; FR-MCP-186 ac-DoltBaselineAndUpgrade; TR-MCP-FEDERATION-013; TEST-MCP-234.</remarks>
internal interface IPhase0QueryProbe
{
    /// <summary>Runs filter-order-page, group-aggregate or raw-parameter as defined by the fixture contract.</summary>
    /// <param name="query">The exact named query.</param>
    /// <param name="seed">Independent seed input, not expected query output.</param>
    /// <param name="rawSql">Raw SQL for the raw-parameter case only.</param>
    /// <param name="parameters">Parameters bound separately from SQL text.</param>
    /// <returns>Materialized results and actual raw-command bindings, or null.</returns>
    Phase0QueryObservation? Execute(string query, IReadOnlyList<Phase0FixtureRow> seed,
        string? rawSql, IReadOnlyDictionary<string, string> parameters);
}

/// <summary>One actual history commit and its ordered parents.</summary>
/// <param name="Hash">Engine commit hash; no symbolic substitution is required by the contract.</param>
/// <param name="Parents">Actual parent hashes.</param>
/// <remarks>2026-11-18: TASK-FEDPH0-002; FR-MCP-175 ac-SaveDoesNotPublish; TR-MCP-FEDERATION-002; TEST-MCP-223.</remarks>
internal sealed record Phase0HistoryPoint(string Hash, IReadOnlyList<string> Parents);

/// <summary>Working rows, real refs, full seeded history and publication side effects at one checkpoint.</summary>
/// <param name="Rows">Rows reread in ordinal Id order.</param>
/// <param name="MainHead">Actual local history head.</param>
/// <param name="RemoteHead">Actual remote history head.</param>
/// <param name="History">Full actual fixture ancestry, inventory order independent.</param>
/// <param name="PublicationIntents">All emitted publication identities.</param>
/// <remarks>2026-11-18: TASK-FEDPH0-002; FR-MCP-175 ac-SaveDoesNotPublish; TR-MCP-FEDERATION-002; TEST-MCP-223.</remarks>
internal sealed record Phase0TransactionState(IReadOnlyList<Phase0FixtureRow> Rows, string MainHead,
    string RemoteHead, IReadOnlyList<Phase0HistoryPoint> History, IReadOnlyList<string> PublicationIntents);

/// <summary>Transaction operations whose real binding uses SQL transactions and a separate Dolt commit.</summary>
/// <remarks>2026-11-18: TASK-FEDPH0-002; FR-MCP-175 ac-SaveDoesNotPublish; FR-MCP-186 ac-DoltBaselineAndUpgrade; TR-MCP-FEDERATION-002/013; TEST-MCP-223/234.</remarks>
internal interface IPhase0TransactionProbe
{
    /// <summary>Prepares a fresh seeded, locally committed and initially synchronized fixture, then begins SQL transaction.</summary>
    /// <param name="seed">Four exact input rows in ordinal Id order.</param>
    /// <returns>Actual state before transactional mutation.</returns>
    Phase0TransactionState? Begin(IReadOnlyList<Phase0FixtureRow> seed);

    /// <summary>Updates T1.title to edited, deletes T3 and inserts TX1.title=inserted with fixed typed fixture values.</summary>
    /// <returns>Actual state inside the still-open SQL transaction.</returns>
    Phase0TransactionState? Mutate();

    /// <summary>Commits or rolls back the SQL transaction, then rereads through a separate connection.</summary>
    /// <param name="commit">True for SQL commit, false for rollback.</param>
    /// <returns>Actual post-transaction state, including all history/publication effects.</returns>
    Phase0TransactionState? CompleteSql(bool commit);

    /// <summary>Creates exactly one explicit Dolt history commit, then rereads rows, history and remote ref.</summary>
    /// <returns>Actual post-history-commit state; no remote push is requested.</returns>
    Phase0TransactionState? CommitHistory();
}

/// <summary>Reusable fixed-input assertions for representative queries, rollback, SQL commit and explicit history commit.</summary>
/// <remarks>
/// 2026-11-18: TASK-FEDPH0-002; FR-MCP-186 ac-DoltBaselineAndUpgrade;
/// FR-MCP-175 ac-SaveDoesNotPublish; TR-MCP-FEDERATION-002/013; TEST-MCP-223/234.
/// This supports the full Task002 definition. Physical constraint enforcement, concurrency,
/// full-model creation, migrations, two-host native execution and B-output acceptance remain separate obligations.
/// </remarks>
internal static class Phase0QueryTransactionContract
{
    private const string LiteralTitle = "Ångström 文' OR 1=1 --";
    private const string RawCommand = "SELECT Id, Title, Amount, RecordedAt, Notes FROM compat_rows WHERE Title = @title ORDER BY Id";

    /// <summary>Checks exact filtering, ordering, paging, nullable grouping, sums and separately bound raw SQL.</summary>
    /// <remarks>
    /// filter-order-page: Where Amount &gt;= 10, OrderByDescending Amount, ThenBy Id ordinal, Skip 1, Take 2.
    /// group-aggregate: GroupBy Notes != null, OrderBy boolean key false then true, project Count and Sum Amount.
    /// raw-parameter: execute the supplied SELECT with the literal title bound separately as @title.
    /// 2026-11-18: TASK-FEDPH0-002; FR-MCP-186 ac-DoltBaselineAndUpgrade; TR-MCP-FEDERATION-013; TEST-MCP-234.
    /// </remarks>
    /// <param name="probe">Independent scripted or later real query adapter.</param>
    internal static void AssertQueries(IPhase0QueryProbe probe)
    {
        foreach (var query in new[] { "filter-order-page", "group-aggregate", "raw-parameter" })
        {
            var expectedRows = query switch
            {
                "filter-order-page" => new[] { Seed()[0], Seed()[2] },
                "raw-parameter" => new[] { Seed()[1] },
                _ => []
            };
            var expectedGroups = query == "group-aggregate"
                ? new[] { new Phase0QueryAggregate(false, 2, 112.3300m), new Phase0QueryAggregate(true, 2, 10.3400m) }
                : [];
            var sql = query == "raw-parameter" ? RawCommand : null;
            var arguments = query == "raw-parameter"
                ? new Dictionary<string, string> { ["@title"] = LiteralTitle }
                : new Dictionary<string, string>();
            // Freeze independent expected objects before the adapter sees any mutable input.
            var expectedParameters = arguments.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
            var actual = probe.Execute(query, Seed(), sql, arguments);
            Assert.NotNull(actual);
            AssertRows(expectedRows, actual.Rows);
            Assert.NotNull(actual.Groups);
            Assert.Equal(expectedGroups.Length, actual.Groups.Count);
            for (var i = 0; i < expectedGroups.Length; i++)
            {
                Assert.NotNull(actual.Groups[i]);
                Assert.Equal(expectedGroups[i].HasNotes, actual.Groups[i].HasNotes);
                Assert.Equal(expectedGroups[i].Count, actual.Groups[i].Count);
                Assert.Equal(decimal.GetBits(expectedGroups[i].Total), decimal.GetBits(actual.Groups[i].Total));
            }
            Assert.Equal(sql, actual.RawSql);
            Assert.NotNull(actual.Parameters);
            Assert.Equal(expectedParameters.OrderBy(pair => pair.Key, StringComparer.Ordinal),
                actual.Parameters.OrderBy(pair => pair.Key, StringComparer.Ordinal));
        }
    }

    /// <summary>Checks each SQL boundary and, when requested, a distinct single-child Dolt commit without publication.</summary>
    /// <param name="probe">Fresh scripted or later real disposable transaction adapter.</param>
    /// <param name="outcome">rollback, sql-commit or dolt-commit.</param>
    internal static void AssertTransaction(IPhase0TransactionProbe probe, string outcome)
    {
        Assert.Contains(outcome, new[] { "rollback", "sql-commit", "dolt-commit" });
        var expectedBefore = Seed();
        var expectedInside = new[] { Seed()[0] with { Title = "edited" }, Seed()[1], Seed()[3], Seed()[2] with { Id = "TX1", Title = "inserted" } };
        var before = Freeze(RequireState(probe.Begin(Seed())));
        AssertRows(expectedBefore, before.Rows);
        Assert.Equal(before.MainHead, before.RemoteHead);
        var inside = RequireState(probe.Mutate());
        AssertRows(expectedInside, inside.Rows);
        AssertRefsAndHistory(before, inside);
        var after = RequireState(probe.CompleteSql(outcome != "rollback"));
        AssertRows(outcome == "rollback" ? expectedBefore : expectedInside, after.Rows);
        AssertRefsAndHistory(before, after);
        if (outcome != "dolt-commit") return;
        var committed = RequireState(probe.CommitHistory());
        AssertRows(expectedInside, committed.Rows);
        Assert.NotEqual(before.MainHead, committed.MainHead);
        Assert.Equal(before.RemoteHead, committed.RemoteHead);
        Assert.Equal(before.History.Count + 1, committed.History.Count);
        foreach (var old in before.History)
        {
            var retained = Assert.Single(committed.History, point => point.Hash == old.Hash);
            Assert.Equal(old.Parents, retained.Parents);
        }
        var added = Assert.Single(committed.History, point => point.Hash == committed.MainHead);
        Assert.Equal(new[] { before.MainHead }, added.Parents);
    }

    /// <summary>Constructs immutable-value seed rows with null/empty/Unicode text, ties, negative decimals and seven-digit time precision.</summary>
    private static Phase0FixtureRow[] Seed()
    {
        var time = new DateTimeOffset(2026, 11, 17, 0, 0, 0, TimeSpan.Zero).AddTicks(1234567);
        return [new("T1", "seed", 12.3400m, time, null), new("T2", LiteralTitle, -2.0000m, time, "Ångström 文"),
            new("T3", "repeat", 12.3400m, time, ""), new("T4", "high", 99.9900m, time, null)];
    }

    /// <summary>Compares typed values without silently normalizing precision, scale, null, empty text or order.</summary>
    private static void AssertRows(IReadOnlyList<Phase0FixtureRow> expected, IReadOnlyList<Phase0FixtureRow> actual)
    {
        Assert.NotNull(actual);
        Assert.Equal(expected.Count, actual.Count);
        for (var i = 0; i < expected.Count; i++)
        {
            Assert.NotNull(actual[i]);
            Assert.Equal(expected[i].Id, actual[i].Id);
            Assert.Equal(expected[i].Title, actual[i].Title);
            Assert.Equal(decimal.GetBits(expected[i].Amount), decimal.GetBits(actual[i].Amount));
            Assert.Equal(expected[i].RecordedAt.Ticks, actual[i].RecordedAt.Ticks);
            Assert.Equal(TimeSpan.Zero, actual[i].RecordedAt.Offset);
            Assert.Equal(expected[i].Notes, actual[i].Notes);
        }
    }

    /// <summary>Validates observation shape and history identity before comparing boundary effects.</summary>
    private static Phase0TransactionState RequireState(Phase0TransactionState? actual)
    {
        Assert.NotNull(actual);
        Assert.NotNull(actual.Rows);
        Assert.False(string.IsNullOrWhiteSpace(actual.MainHead));
        Assert.False(string.IsNullOrWhiteSpace(actual.RemoteHead));
        Assert.NotNull(actual.History);
        Assert.NotEmpty(actual.History);
        Assert.All(actual.History, point => { Assert.NotNull(point); Assert.False(string.IsNullOrWhiteSpace(point.Hash)); Assert.NotNull(point.Parents); });
        Assert.Equal(actual.History.Count, actual.History.Select(point => point.Hash).Distinct(StringComparer.Ordinal).Count());
        Assert.Single(actual.History, point => point.Hash == actual.MainHead);
        Assert.NotNull(actual.PublicationIntents);
        Assert.Empty(actual.PublicationIntents);
        return actual;
    }

    /// <summary>Copies all mutable observations before a later operation can rewrite the baseline.</summary>
    private static Phase0TransactionState Freeze(Phase0TransactionState state)
        => state with { Rows = state.Rows.ToArray(), History = state.History.Select(point => point with { Parents = point.Parents.ToArray() }).ToArray(), PublicationIntents = state.PublicationIntents.ToArray() };

    /// <summary>Rejects implicit history commits, remote mutations or rewritten ancestry at an ordinary SQL boundary.</summary>
    private static void AssertRefsAndHistory(Phase0TransactionState expected, Phase0TransactionState actual)
    {
        Assert.Equal(expected.MainHead, actual.MainHead);
        Assert.Equal(expected.RemoteHead, actual.RemoteHead);
        Assert.Equal(expected.History.Count, actual.History.Count);
        foreach (var point in expected.History)
            Assert.Equal(point.Parents, Assert.Single(actual.History, observed => observed.Hash == point.Hash).Parents);
    }
}
