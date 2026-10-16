// SPDX-License-Identifier: Apache-2.0
using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-SUPPORT-024 AC-005 (FR-SUPPORT-016 ac-6, TR-SUPPORT-CORE-016 ac-4; PLAN-SESSIONTRIAGEFIX-001 A7 follow-up,
/// triage-report-905c3ac1b6af4d57b9c24c2383642379): writes to one session-log turn committed at the same moment keep the turn's ordinals
/// unique, and a write that loses is refused, storing nothing, with an error McpErrorClassifier reports as retryable. Data: hand-built
/// outcomes for the mock gate; a file SQLite database per binding case.
/// </summary>
public sealed class SessionLogConcurrentTurnWriteTests
{
    private static readonly string[] Expected = ["seed-0", "seed-1", "first", "second"];
    private static readonly string[] AfterRefusal = ["seed-0", "seed-1", "first"];

    /// <summary>
    /// TEST-SUPPORT-024 AC-005 mock gate: the concurrent-write outcome assertion accepts a final dialog whose ordinals are distinct and whose
    /// contents are the expected ones, with no refusal or with a refusal of the second writer that McpErrorClassifier reports as retryable
    /// (a SQLite busy error, bare or inside a DbUpdateException) and that stored nothing; it rejects a repeated ordinal, a lost item, a
    /// refusal classified as final, a refusal that stored its item, and two first inserts of one request id stored as two turn rows (each
    /// row's dialog starts at ordinal 0, so the turn's ordinals repeat). Data: hand-built ordinals, contents and exceptions; no database.
    /// </summary>
    [Fact]
    public void ConcurrentTurnWriteOutcomeAssertion_AcceptsDistinctOrRetryableRefusalAndRejectsDuplicateOrPartialWrite()
    {
        var clean = new ConcurrentTurnWriteOutcome([0, 1, 2, 3], Expected, Expected, null, [], "second");
        var busy = new SqliteException("database is locked", 5);
        AssertConcurrentTurnWriteOutcome(clean);
        AssertConcurrentTurnWriteOutcome(clean with { Refusal = busy, ContentsAfterRefusal = AfterRefusal });
        AssertConcurrentTurnWriteOutcome(clean with { Refusal = new DbUpdateException("The save failed.", busy), ContentsAfterRefusal = AfterRefusal });

        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertConcurrentTurnWriteOutcome(clean with { FinalOrdinals = [0, 1, 2, 2] }));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(
            () => AssertConcurrentTurnWriteOutcome(clean with { FinalOrdinals = [0, 1, 2], FinalContents = AfterRefusal }));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertConcurrentTurnWriteOutcome(
            clean with { Refusal = new InvalidOperationException("The write failed."), ContentsAfterRefusal = AfterRefusal }));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(
            () => AssertConcurrentTurnWriteOutcome(clean with { Refusal = busy, ContentsAfterRefusal = Expected }));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(
            () => AssertConcurrentTurnWriteOutcome(new ConcurrentTurnWriteOutcome([0, 0], FirstInserts, FirstInserts, null, [], "second")));
    }

    private const string Source = "Codex";
    private const string SessionId = "Codex-20261016T120000Z-turnwrite";
    private const string RequestId = "req-20261016T120000Z-turnwrite";
    private static readonly string[] Replacement = ["second-0", "second-1", "second-2"];
    private static readonly string[] FirstInserts = ["first", "second"];

    /// <summary>
    /// TEST-SUPPORT-024 AC-005 binding (FR-SUPPORT-016 ac-6, TR-SUPPORT-CORE-016 ac-4): on a file SQLite database, the second writer runs on its
    /// own connection between the first writer's read and its save (a SaveChangesInterceptor on the first context); the second write either
    /// applies with ordinals distinct from the first's or is refused retryably storing nothing, and a retry after the first commits applies.
    /// Data: a session whose turn holds dialog items seed-0 and seed-1; a PATCH or a dialog append adds one item ("first" or "second"); a
    /// dialog section replace sets the dialog to second-0, second-1 and second-2.
    /// </summary>
    /// <param name="first">The first writer: patch or append.</param>
    /// <param name="second">The second writer: patch, append or replace-section.</param>
    [Theory]
    [InlineData("patch", "patch")]
    [InlineData("patch", "append")]
    [InlineData("append", "patch")]
    [InlineData("patch", "replace-section")]
    public async Task TurnWrites_InterleavedOnOneTurn_StoreDistinctOrdinalsOrRefuseRetryably(string first, string second)
    {
        var run = await InterleaveAsync(first, second, RequestId, TestContext.Current.CancellationToken).ConfigureAwait(true);
        IReadOnlyList<string> expected = second == "replace-section"
            ? run.Refusal is null ? [.. Replacement, "first"] : Replacement
            : Expected;
        var refused = second == "replace-section" ? Replacement[0] : "second";
        AssertConcurrentTurnWriteOutcome(new ConcurrentTurnWriteOutcome(run.Ordinals, run.Contents, expected, run.Refusal, run.ContentsAfterRefusal, refused));
    }

    /// <summary>
    /// TEST-SUPPORT-024 AC-005 binding (FR-SUPPORT-016 ac-6, TR-SUPPORT-CORE-016 ac-4; PLAN-SESSIONTRIAGEFIX-001 A7 follow-up, operator decision
    /// 2026-10-16 "Keep retry, revert ac-1 (Recommended)"): two PATCHes that each create the same new turn, the second run on its own connection
    /// between the first writer's read and its save, leave one turn row holding both writes' dialog items with distinct ordinals; the second
    /// waits or is refused retryably storing nothing, and its retry updates the row the first stored. Data: the seeded session of the theory
    /// above and a request id it does not hold.
    /// </summary>
    [Fact]
    public async Task TurnWrites_TwoFirstInsertsOfOneRequestId_LeaveOneTurnRow()
    {
        const string newRequestId = "req-20261016T120100Z-turnwrite-new";
        var run = await InterleaveAsync("create", "create", newRequestId, TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(1, run.TurnRows);
        AssertConcurrentTurnWriteOutcome(new ConcurrentTurnWriteOutcome(run.Ordinals, run.Contents, FirstInserts, run.Refusal, run.ContentsAfterRefusal, "second"));
    }

    // TEST-SUPPORT-024 AC-005: on a fresh file SQLite database holding the seeded session, runs the second writer on its own connection
    // between the first writer's read and its save (a SaveChangesInterceptor on the first context), retries the second writer once after
    // the first commits when it was refused, and reads the dialog of the turns holding the request id and how many such rows exist.
    private static async Task<InterleavedRun> InterleaveAsync(string first, string second, string requestId, CancellationToken ct)
    {
        var path = Path.Combine(Path.GetTempPath(), $"turnwrite-race-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={path};Default Timeout=1;Pooling=False";
        var workspace = new WorkspaceContext { WorkspacePath = "concurrent-turnwrite-workspace" };
        var options = new DbContextOptionsBuilder<McpDbContext>().UseSqlite(connectionString).Options;
        try
        {
            await using (var seedDb = new McpDbContext(options, workspace))
            {
                await seedDb.Database.EnsureCreatedAsync(ct).ConfigureAwait(false);
                var seed = new SessionLogService(seedDb, NullLogger<SessionLogService>.Instance, workspaceContext: workspace);
                await seed.SubmitAsync(SeedSession(), cancellationToken: ct).ConfigureAwait(false);
            }

            await using var secondDb = new McpDbContext(options, workspace);
            var secondService = new SessionLogService(secondDb, NullLogger<SessionLogService>.Instance, workspaceContext: workspace);
            Exception? refusal = null;
            var interleaving = new SaveInterleaving(async () =>
                refusal = await Record.ExceptionAsync(() => WriteAsync(secondService, second, "second", requestId, ct)).ConfigureAwait(false));
            var firstOptions = new DbContextOptionsBuilder<McpDbContext>().UseSqlite(connectionString).AddInterceptors(interleaving).Options;
            await using (var firstDb = new McpDbContext(firstOptions, workspace))
            {
                var firstService = new SessionLogService(firstDb, NullLogger<SessionLogService>.Instance, workspaceContext: workspace);
                await WriteAsync(firstService, first, "first", requestId, ct).ConfigureAwait(false);
            }

            IReadOnlyList<string> afterRefusal = [];
            if (refusal is not null)
            {
                afterRefusal = (await ReadDialogAsync(options, workspace, requestId, ct).ConfigureAwait(false)).Contents;
                secondDb.ChangeTracker.Clear();
                await WriteAsync(secondService, second, "second", requestId, ct).ConfigureAwait(false);
            }

            var (ordinals, contents) = await ReadDialogAsync(options, workspace, requestId, ct).ConfigureAwait(false);
            await using var countDb = new McpDbContext(options, workspace);
            var turnRows = await countDb.SessionLogTurns.CountAsync(turn => turn.RequestId == requestId, ct).ConfigureAwait(false);
            return new InterleavedRun(ordinals, contents, refusal, afterRefusal, turnRows);
        }
        finally
        {
            foreach (var suffix in new[] { string.Empty, "-wal", "-shm", "-journal" })
                File.Delete(path + suffix);
        }
    }

    // TEST-SUPPORT-024 AC-005: one turn write of the given kind to the given request id, carrying the given content; a create is a PATCH that
    // also carries the plan and TODO context a new turn requires.
    private static async Task WriteAsync(SessionLogService service, string kind, string content, string requestId, CancellationToken ct)
    {
        switch (kind)
        {
            case "patch":
                await service.UpsertTurnAsync(Source, SessionId, new UnifiedRequestEntryDto { RequestId = requestId, ProcessingDialog = [Dialog(content)] }, ct)
                    .ConfigureAwait(false);
                break;
            case "create":
                await service.UpsertTurnAsync(
                        Source,
                        SessionId,
                        new UnifiedRequestEntryDto
                        {
                            RequestId = requestId,
                            PlanFile = SessionLogTurnContextValidator.NoneSentinel,
                            TodoId = SessionLogTurnContextValidator.NoneSentinel,
                            ProcessingDialog = [Dialog(content)],
                        },
                        ct)
                    .ConfigureAwait(false);
                break;
            case "append":
                await service.AppendProcessingDialogAsync(Source, SessionId, requestId, [Dialog(content)], ct).ConfigureAwait(false);
                break;
            case "replace-section":
                await service.ReplaceTurnSectionAsync(
                        Source, SessionId, requestId, "dialog", new UnifiedRequestEntryDto { RequestId = requestId, ProcessingDialog = [.. Replacement.Select(Dialog)] }, ct)
                    .ConfigureAwait(false);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown turn write.");
        }
    }

    // TEST-SUPPORT-024 AC-005: the live dialog ordinals and contents of the turns holding the request id, read through a fresh context.
    private static async Task<(IReadOnlyList<int> Ordinals, IReadOnlyList<string> Contents)> ReadDialogAsync(
        DbContextOptions<McpDbContext> options, WorkspaceContext workspace, string requestId, CancellationToken ct)
    {
        await using var db = new McpDbContext(options, workspace);
        var items = await db.SessionLogTurns
            .Where(turn => turn.RequestId == requestId)
            .SelectMany(turn => turn.ProcessingDialog)
            .OrderBy(item => item.Ordinal)
            .Select(item => new { item.Ordinal, item.Content })
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return ([.. items.Select(item => item.Ordinal)], [.. items.Select(item => item.Content)]);
    }

    // TEST-SUPPORT-024 AC-005: what one interleaved pair of turn writes left: the dialog of the turns holding the request id, the error that
    // refused the second writer's first attempt (or null), the dialog stored between that refusal and the retry, and the turn rows.
    private sealed record InterleavedRun(
        IReadOnlyList<int> Ordinals,
        IReadOnlyList<string> Contents,
        Exception? Refusal,
        IReadOnlyList<string> ContentsAfterRefusal,
        int TurnRows);

    private static UnifiedSessionLogDto SeedSession() => new()
    {
        SourceType = Source,
        SessionId = SessionId,
        Title = "Concurrent turn writes",
        Turns =
        [
            new UnifiedRequestEntryDto
            {
                RequestId = RequestId, Timestamp = "2026-10-16T12:00:00Z", QueryTitle = "Concurrent turn writes", QueryText = "Keep ordinals unique",
                Status = "in_progress", PlanFile = "None", TodoId = "None", ProcessingDialog = [Dialog("seed-0"), Dialog("seed-1")],
            },
        ],
    };

    private static ProcessingDialogItemDto Dialog(string content)
        => new() { Role = "assistant", Content = content, Category = "note", Timestamp = "2026-10-16T12:00:00Z" };

    // Runs the second writer between the first writer's read and its save.
    private sealed class SaveInterleaving(Func<Task> beforeSave) : SaveChangesInterceptor
    {
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            await beforeSave().ConfigureAwait(false);
            return result;
        }
    }

    // TEST-SUPPORT-024 AC-005: every final ordinal is distinct, the final contents are the expected ones, and a refused write was retryable
    // and had stored nothing before its retry.
    internal static void AssertConcurrentTurnWriteOutcome(ConcurrentTurnWriteOutcome outcome)
    {
        Assert.Equal(outcome.FinalOrdinals.Count, outcome.FinalOrdinals.Distinct().Count());
        Assert.Equal(outcome.ExpectedContents.Order(StringComparer.Ordinal), outcome.FinalContents.Order(StringComparer.Ordinal));
        if (outcome.Refusal is null)
            return;

        Assert.True(McpErrorClassifier.Classify(outcome.Refusal).Retryable, $"A refused concurrent write must be retryable: {outcome.Refusal}");
        Assert.DoesNotContain(outcome.RefusedContent, outcome.ContentsAfterRefusal);
    }

    /// <summary>TEST-SUPPORT-024 AC-005: what one interleaved pair of turn writes left behind.</summary>
    /// <param name="FinalOrdinals">The dialog ordinals stored at the end.</param>
    /// <param name="FinalContents">The dialog contents stored at the end, in the same order.</param>
    /// <param name="ExpectedContents">The dialog contents both writes together must leave.</param>
    /// <param name="Refusal">The error that refused the second write's first attempt, or null when it applied.</param>
    /// <param name="ContentsAfterRefusal">The dialog contents stored after the refusal and before the retry.</param>
    /// <param name="RefusedContent">The content the refused write carried.</param>
    internal sealed record ConcurrentTurnWriteOutcome(
        IReadOnlyList<int> FinalOrdinals,
        IReadOnlyList<string> FinalContents,
        IReadOnlyList<string> ExpectedContents,
        Exception? Refusal,
        IReadOnlyList<string> ContentsAfterRefusal,
        string RefusedContent);
}
