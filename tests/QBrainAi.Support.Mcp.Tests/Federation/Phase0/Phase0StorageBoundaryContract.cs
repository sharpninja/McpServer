namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Typed row retained through a disposable schema upgrade.</summary>
/// <param name="Id">Stable fixture identity.</param>
/// <param name="Title">Ordinal text value.</param>
/// <param name="Amount">Fixed-precision decimal, including its scale.</param>
/// <param name="RecordedAt">UTC timestamp with sub-second precision.</param>
/// <param name="Notes">Nullable Unicode fixture text.</param>
/// <remarks>2026-11-17: FR-MCP-186 ac-DoltBaselineAndUpgrade; TEST-MCP-234; plan section 5.</remarks>
internal sealed record Phase0FixtureRow(string Id, string Title, decimal Amount, DateTimeOffset RecordedAt, string? Notes);

/// <summary>Observed schema, migration history and complete rows at one checkpoint.</summary>
/// <param name="SchemaVersion">Observed disposable fixture schema.</param>
/// <param name="AppliedMigrations">Ordered applied migration identifiers.</param>
/// <param name="ChangesApplied">Number of migration operations in this application.</param>
/// <param name="Rows">Complete observed fixture rows.</param>
/// <remarks>2026-11-17: FR-MCP-186 ac-DoltBaselineAndUpgrade; TEST-MCP-234.</remarks>
internal sealed record Phase0MigrationStage(int SchemaVersion, IReadOnlyList<string> AppliedMigrations,
    int ChangesApplied, IReadOnlyList<Phase0FixtureRow> Rows);

/// <summary>Readbacks after create, update, delete and retained-seed checks.</summary>
/// <param name="Created">Row reread after insertion.</param>
/// <param name="Updated">Row reread after update.</param>
/// <param name="Deleted">Lookup after deletion; must be absent.</param>
/// <param name="RetainedSeed">Seed reread after the CRUD sequence.</param>
/// <remarks>2026-11-17: FR-MCP-186 ac-DoltBaselineAndUpgrade; TEST-MCP-234.</remarks>
internal sealed record Phase0CrudObservation(Phase0FixtureRow? Created, Phase0FixtureRow? Updated,
    Phase0FixtureRow? Deleted, Phase0FixtureRow? RetainedSeed);

/// <summary>Test seam whose later adapter must actually perform migration and CRUD operations.</summary>
/// <remarks>2026-11-17: FR-MCP-186 ac-DoltBaselineAndUpgrade; TR-MCP-FEDERATION-013; TEST-MCP-234.</remarks>
internal interface IPhase0MigrationProbe
{
    /// <summary>Reads the initially empty fixture database.</summary>
    /// <returns>Actual schema and rows before any migration.</returns>
    Phase0MigrationStage? InspectEmpty();

    /// <summary>Applies the named disposable schema and observes its resulting state.</summary>
    /// <param name="schemaVersion">Fixture schema 1 or 2, not a product migration name.</param>
    /// <returns>Schema, applied migrations, changed-operation count and rows.</returns>
    Phase0MigrationStage? ApplySchema(int schemaVersion);

    /// <summary>Exercises insert/read/update/delete and rereads the retained seed.</summary>
    /// <param name="seedIfMissing">Creates T1 only on the initial schema1 call.</param>
    /// <returns>Readbacks, not a precomputed success flag.</returns>
    Phase0CrudObservation? ExerciseCrud(bool seedIfMissing);
}

/// <summary>Observable pooled-read identity and value.</summary>
/// <param name="ProjectId">Project actually read.</param>
/// <param name="Branch">Branch actually read.</param>
/// <param name="RowId">Row actually returned.</param>
/// <param name="Title">Read value.</param>
/// <remarks>2026-11-17: FR-MCP-177 ac-PooledConnectionDoesNotLeakBranch; TEST-MCP-225.</remarks>
internal sealed record Phase0BranchRead(string ProjectId, string Branch, string RowId, string Title);

/// <summary>One pooled-connection read through the future real fixture boundary.</summary>
/// <remarks>2026-11-17: FR-MCP-177 ac-PooledConnectionDoesNotLeakBranch; TR-MCP-FEDERATION-004; TEST-MCP-225.</remarks>
internal interface IPhase0PooledReadProbe
{
    /// <summary>Opens or reuses a pooled connection for the explicit selected branch.</summary>
    /// <param name="projectId">Atlas fixture project.</param>
    /// <param name="branch">Explicit requested branch.</param>
    /// <param name="rowId">T1 fixture row.</param>
    /// <returns>The identity and value actually read.</returns>
    Phase0BranchRead? Read(string projectId, string branch, string rowId);
}

/// <summary>Refs, local reread and publication effects after an ordinary SQL save.</summary>
/// <param name="MainHead">Version-history main ref.</param>
/// <param name="RemoteHead">Remote publication ref.</param>
/// <param name="StoredRowId">Identity actually saved and reread.</param>
/// <param name="StoredTitle">Value reread from the SQL working set.</param>
/// <param name="PublicationIntents">All emitted publication-intent identities.</param>
/// <param name="HistoryCommitsCreated">Number of new history commits.</param>
/// <remarks>2026-11-17: FR-MCP-175 ac-SaveDoesNotPublish; TEST-MCP-223.</remarks>
internal sealed record Phase0SaveObservation(string MainHead, string RemoteHead, string StoredRowId,
    string StoredTitle, IReadOnlyList<string> PublicationIntents, int HistoryCommitsCreated);

/// <summary>Ordinary SaveChanges boundary; no explicit history-commit request.</summary>
/// <remarks>2026-11-17: FR-MCP-175 ac-SaveDoesNotPublish; TR-MCP-FEDERATION-002; TEST-MCP-223.</remarks>
internal interface IPhase0SaveProbe
{
    /// <summary>Persists a local draft, rereads it and observes all publication side effects.</summary>
    /// <param name="rowId">T1 fixture row.</param>
    /// <param name="title">Draft title to persist.</param>
    /// <returns>Actual working-set readback, refs and publication effects.</returns>
    Phase0SaveObservation? SaveDraft(string rowId, string title);
}

/// <summary>Unchanged requirement-derived assertions for mocks and later disposable real binding.</summary>
/// <remarks>
/// 2026-11-17: TASK-FEDPH0-002; FR-MCP-175 ac-SaveDoesNotPublish;
/// FR-MCP-177 ac-PooledConnectionDoesNotLeakBranch; FR-MCP-186 ac-DoltBaselineAndUpgrade;
/// TR-MCP-FEDERATION-002/004/013; TEST-MCP-223/225/234.
/// These are partial Phase0 contracts. Full-model mappings, LINQ/raw SQL, concurrency,
/// rollback, indexes/FKs and real provider behavior need additional independent evidence.
/// </remarks>
internal static class Phase0StorageBoundaryContract
{
    /// <summary>Checks empty, baseline CRUD, upgrade, upgraded CRUD and idempotent reapply.</summary>
    /// <param name="probe">Scripted or later real disposable migration boundary.</param>
    internal static void AssertBaselineAndUpgrade(IPhase0MigrationProbe probe)
    {
        AssertStage(probe.InspectEmpty(), 0, [], 0, hasSeed: false);
        AssertStage(probe.ApplySchema(1), 1, ["schema1"], 1, hasSeed: false);
        AssertCrud(probe.ExerciseCrud(seedIfMissing: true));
        AssertStage(probe.ApplySchema(2), 2, ["schema1", "schema2"], 1, hasSeed: true);
        AssertCrud(probe.ExerciseCrud(seedIfMissing: false));
        AssertStage(probe.ApplySchema(2), 2, ["schema1", "schema2"], 0, hasSeed: true);
    }

    /// <summary>Checks all 100 alternating reads, including identity and selected branch.</summary>
    /// <param name="probe">Scripted or later real connection-pool boundary.</param>
    internal static void AssertPooledBranchIsolation(IPhase0PooledReadProbe probe)
    {
        for (var index = 0; index < 100; index++)
        {
            var branch = index % 2 == 0 ? "main" : "feature/search";
            var title = index % 2 == 0 ? "main" : "feature";
            var actual = probe.Read("Atlas", branch, "T1");
            Assert.NotNull(actual);
            Assert.Equal("Atlas", actual.ProjectId);
            Assert.Equal(branch, actual.Branch);
            Assert.Equal("T1", actual.RowId);
            Assert.Equal(title, actual.Title);
        }
    }

    /// <summary>Checks persisted local SQL data without a history or publication side effect.</summary>
    /// <param name="probe">Scripted or later real SaveChanges boundary.</param>
    internal static void AssertSaveDoesNotPublish(IPhase0SaveProbe probe)
    {
        var actual = probe.SaveDraft("T1", "draft");
        Assert.NotNull(actual);
        Assert.Equal("C0", actual.MainHead);
        Assert.Equal("C0", actual.RemoteHead);
        Assert.Equal("T1", actual.StoredRowId);
        Assert.Equal("draft", actual.StoredTitle);
        Assert.Empty(actual.PublicationIntents);
        Assert.Equal(0, actual.HistoryCommitsCreated);
    }

    /// <summary>Checks every migration checkpoint independently against the fixture contract.</summary>
    /// <param name="actual">Observed state.</param>
    /// <param name="schema">Expected schema.</param>
    /// <param name="migrations">Expected ordered migration history.</param>
    /// <param name="changes">Expected changes during this application.</param>
    /// <param name="hasSeed">Whether CRUD has created the persistent seed.</param>
    private static void AssertStage(Phase0MigrationStage? actual, int schema, string[] migrations,
        int changes, bool hasSeed)
    {
        Assert.NotNull(actual);
        Assert.Equal(schema, actual.SchemaVersion);
        Assert.Equal(migrations, actual.AppliedMigrations);
        Assert.Equal(changes, actual.ChangesApplied);
        if (hasSeed)
            AssertRow(Assert.Single(actual.Rows), "T1", "seed");
        else
            Assert.Empty(actual.Rows);
    }

    /// <summary>Checks independent create/read/update/delete observations and seed preservation.</summary>
    /// <param name="actual">Readbacks from one CRUD sequence.</param>
    private static void AssertCrud(Phase0CrudObservation? actual)
    {
        Assert.NotNull(actual);
        AssertRow(actual.Created, "T2", "created");
        AssertRow(actual.Updated, "T2", "updated");
        Assert.Null(actual.Deleted);
        AssertRow(actual.RetainedSeed, "T1", "seed");
    }

    /// <summary>Checks stable identity, ordinal text, decimal scale, UTC precision and nullable text.</summary>
    /// <param name="actual">Actual row readback.</param>
    /// <param name="id">Expected identity.</param>
    /// <param name="title">Expected title.</param>
    private static void AssertRow(Phase0FixtureRow? actual, string id, string title)
    {
        Assert.NotNull(actual);
        Assert.Equal(id, actual.Id);
        Assert.Equal(title, actual.Title);
        Assert.Equal(decimal.GetBits(12.3400m), decimal.GetBits(actual.Amount));
        Assert.Equal(new DateTimeOffset(2026, 11, 17, 0, 0, 0, TimeSpan.Zero).AddTicks(1234567).Ticks,
            actual.RecordedAt.Ticks);
        Assert.Equal(TimeSpan.Zero, actual.RecordedAt.Offset);
        Assert.Equal(id == "T1" ? null : "Ångström 文", actual.Notes);
    }
}
