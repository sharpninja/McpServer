using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Notifications;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.TransactionSecurity.Models;
using QBrainAi.TransactionSecurity.Options;
using QBrainAi.TransactionSecurity.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;
using MsOptions = Microsoft.Extensions.Options;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-221 / FR-MCP-173 / TR-MCP-TXNKEY-001: every session-log mutation, QBAgent
/// included, persists through the inner session-log service without calling the turn
/// transaction coordinator (and therefore without keyserver signing) even when turn
/// transactions are required and the coordinator would reject, fail commit, roll back,
/// or is degraded. TEST-MCP-161 / TR-MCP-TXN-001: applied workspace-stamp repair stays
/// fail-closed while required turn transactions are active. Data: in-memory SQLite with
/// the real SessionLogService and a capturing coordinator double.
/// </summary>
public sealed class TransactionGatedSessionLogServiceTests
{
    private const string WorkspacePath = @"E:\tests\transaction-gated-sessionlog";
    private const string Agent = "QBAgent";
    private const string NonQuadBrainAgent = "GrokCode";
    private const string RequestId = "req-20260614T120000Z-seed-sessionlog-gate";

    /// <summary>
    /// TEST-MCP-221 / FR-MCP-173: a QBAgent submit persists the session and its turn
    /// without calling the coordinator, even though the coordinator would reject before
    /// the mutation.
    /// </summary>
    [Fact]
    public async Task SubmitAsync_WhenCoordinatorWouldRejectBeforeMutation_PersistsWithoutCallingCoordinator()
    {
        using var connection = OpenConnection();
        var coordinator = new CapturingCoordinator
        {
            InvokeMutation = false,
            Status = "rejected",
            Reason = TransactionFailureReason.UnknownKey,
            Message = "signing failed",
        };
        var sessionId = BuildSessionId("submit-rejected-before-mutation");
        var (sut, db) = BuildGatedSut(connection, coordinator);

        using (db)
        {
            var id = await sut.SubmitAsync(CreateSession(sessionId), cancellationToken: TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            Assert.True(id > 0);
        }

        Assert.Null(coordinator.Request);
        Assert.Equal(1, CountSessionRows(connection, sessionId));
        Assert.Equal(1, CountTurnRows(connection, sessionId));
    }

    /// <summary>
    /// TEST-MCP-221 / FR-MCP-173: a QBAgent submit that creates a session persists without
    /// calling the coordinator and without any rollback, even though the coordinator would
    /// fail the commit and roll back.
    /// </summary>
    [Fact]
    public async Task SubmitAsync_WhenCoordinatorWouldFailCommitAndRollBack_PersistsWithoutCallingCoordinator()
    {
        using var connection = OpenConnection();
        var sessionId = BuildSessionId("submit-created-restore");
        var coordinator = new CapturingCoordinator
        {
            Status = "rejected",
            Reason = TransactionFailureReason.SubscriberUnavailable,
            Message = "Subscriber commit failed.",
            InvokeRollback = true,
        };
        var (sut, db) = BuildGatedSut(connection, coordinator);

        using (db)
        {
            var id = await sut.SubmitAsync(CreateSession(sessionId), cancellationToken: TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            Assert.True(id > 0);
        }

        Assert.Null(coordinator.Request);
        Assert.False(coordinator.RollbackAttempted);
        Assert.Equal(1, CountSessionRows(connection, sessionId));
        Assert.Equal(1, CountTurnRows(connection, sessionId));
    }

    /// <summary>
    /// TEST-MCP-221 / FR-MCP-173: a QBAgent turn replace persists the replacement without
    /// calling the coordinator and without any rollback, even though the coordinator would
    /// fail the commit and roll back.
    /// </summary>
    [Fact]
    public async Task ReplaceTurnAsync_WhenCoordinatorWouldFailCommitAndRollBack_PersistsReplacementWithoutCallingCoordinator()
    {
        using var connection = OpenConnection();
        var sessionId = SeedFullSession(connection);
        var coordinator = new CapturingCoordinator
        {
            Status = "rejected",
            Reason = TransactionFailureReason.SubscriberUnavailable,
            Message = "Subscriber commit failed.",
            InvokeRollback = true,
        };
        var (sut, db) = BuildGatedSut(connection, coordinator);

        using (db)
        {
            var id = await sut.ReplaceTurnAsync(
                    Agent,
                    sessionId,
                    new UnifiedRequestEntryDto
                    {
                        RequestId = RequestId,
                        Status = "completed",
                        PlanFile = "None",
                        TodoId = "None",
                        Actions =
                        [
                            new UnifiedActionDto
                            {
                                Order = 0,
                                Description = "replacement action",
                                Type = "edit",
                                Status = "completed",
                            },
                        ],
                    }, cancellationToken: TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            Assert.True(id > 0);
        }

        Assert.Null(coordinator.Request);
        Assert.False(coordinator.RollbackAttempted);
        var replaced = await GetTurnAsync(connection, sessionId).ConfigureAwait(true);
        Assert.Equal("completed", replaced.Status);
        Assert.Contains(replaced.Actions!, action => action.Description == "replacement action");
    }

    /// <summary>
    /// TEST-MCP-221 / FR-MCP-173: a QBAgent session delete takes effect without calling the
    /// coordinator and without any rollback, even though the coordinator would fail the
    /// commit and roll back.
    /// </summary>
    [Fact]
    public async Task DeleteSessionAsync_WhenCoordinatorWouldFailCommitAndRollBack_DeletesWithoutCallingCoordinator()
    {
        using var connection = OpenConnection();
        var sessionId = SeedFullSession(connection);
        var coordinator = new CapturingCoordinator
        {
            Status = "rejected",
            Reason = TransactionFailureReason.SubscriberUnavailable,
            Message = "Subscriber commit failed.",
            InvokeRollback = true,
        };
        var (sut, db) = BuildGatedSut(connection, coordinator);

        using (db)
        {
            await sut.DeleteSessionAsync(Agent, sessionId, cancellationToken: TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
        }

        Assert.Null(coordinator.Request);
        Assert.False(coordinator.RollbackAttempted);
        Assert.Null(await GetSessionAsync(connection, sessionId).ConfigureAwait(true));
    }

    /// <summary>
    /// TEST-MCP-221 / FR-MCP-173 / TR-MCP-TXNKEY-001 (characterization before the retirement
    /// of the unreachable restore path, PLAN-SESSIONTRIAGEFIX-001 A1/A2/A4): the seven
    /// QBAgent mutations no other test covers (dialog append, session title, turn title,
    /// section replace, section clear, item delete, turn delete) each take effect without
    /// calling the coordinator, although it would reject every transaction. Data: the seeded
    /// full session of <see cref="CreateSession"/>.
    /// </summary>
    [Fact]
    public async Task RemainingMutations_WhenCoordinatorWouldReject_TakeEffectWithoutCallingCoordinator()
    {
        using var connection = OpenConnection();
        var sessionId = SeedFullSession(connection);
        var coordinator = new CapturingCoordinator
        {
            InvokeMutation = false,
            Status = "rejected",
            Reason = TransactionFailureReason.UnknownKey,
            Message = "Keyserver manifest signing failed.",
        };
        var (sut, db) = BuildGatedSut(connection, coordinator);
        var ct = TestContext.Current.CancellationToken;

        using (db)
        {
            await sut.AppendProcessingDialogAsync(
                    Agent,
                    sessionId,
                    RequestId,
                    [new ProcessingDialogItemDto { Timestamp = "2026-06-14T12:04:00Z", Role = "model", Content = "appended", Category = "reasoning" }],
                    ct)
                .ConfigureAwait(true);
            await sut.SetSessionTitleAsync(Agent, sessionId, "retitled session", ct).ConfigureAwait(true);
            await sut.SetTurnTitleAsync(Agent, sessionId, RequestId, "retitled turn", ct).ConfigureAwait(true);
            Assert.True(await sut.ReplaceTurnSectionAsync(Agent, sessionId, RequestId, "tags", new UnifiedRequestEntryDto { Tags = ["replaced-tag"] }, ct).ConfigureAwait(true));
            Assert.True(await sut.ClearTurnSectionAsync(Agent, sessionId, RequestId, "blockers", ct).ConfigureAwait(true));
            Assert.True(await sut.DeleteTurnItemAsync(Agent, sessionId, RequestId, "context", "docs/a.md", ct).ConfigureAwait(true));
        }

        var session = await GetSessionAsync(connection, sessionId).ConfigureAwait(true);
        Assert.NotNull(session);
        Assert.Equal("retitled session", session!.Title);
        var turn = session.Turns!.Single(t => t.RequestId == RequestId);
        Assert.Equal("retitled turn", turn.QueryTitle);
        Assert.Contains(turn.ProcessingDialog!, item => item.Content == "appended");
        Assert.Equal("replaced-tag", Assert.Single(turn.Tags!));
        Assert.True(turn.Blockers is null || turn.Blockers.Count == 0);
        Assert.DoesNotContain("docs/a.md", turn.ContextList!);
        Assert.Contains("docs/b.md", turn.ContextList!);

        var (deleteSut, deleteDb) = BuildGatedSut(connection, coordinator);
        using (deleteDb)
            Assert.True(await deleteSut.DeleteTurnAsync(Agent, sessionId, RequestId, ct).ConfigureAwait(true));

        var afterDelete = await GetSessionAsync(connection, sessionId).ConfigureAwait(true);
        Assert.DoesNotContain(afterDelete?.Turns ?? [], t => t.RequestId == RequestId);
        Assert.Null(coordinator.Request);
    }

    /// <summary>
    /// TR-MCP-DB-003: the session-log gate must not bypass tracked soft-delete protection
    /// with bulk physical delete operations.
    /// </summary>
    [Fact]
    public void TransactionGatedSessionLogServiceSource_DoesNotUseBulkPhysicalDeletes()
    {
        var sourcePath = Path.Combine(
            FindRepositoryRoot(),
            "src",
            "QBrainAi.Support.Mcp",
            "Services",
            "TransactionGatedSessionLogService.cs");
        var source = File.ReadAllText(sourcePath);

        Assert.DoesNotContain("ExecuteDelete", source, StringComparison.Ordinal);
        Assert.DoesNotContain("DELETE FROM", source, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// TEST-MCP-221 ac-1: GrokCode SubmitAsync persists without calling the coordinator
    /// even when required turn transactions would reject keyserver signing.
    /// </summary>
    [Fact]
    public async Task SubmitAsync_WhenNonQuadBrainAndCoordinatorWouldReject_PersistsWithoutCallingCoordinator()
    {
        using var connection = OpenConnection();
        var coordinator = new CapturingCoordinator
        {
            InvokeMutation = false,
            Status = "rejected",
            Reason = TransactionFailureReason.UnknownKey,
            Message = "Keyserver manifest signing failed.",
        };
        var sessionId = BuildSessionId("grok-bypass-submit", NonQuadBrainAgent);
        var (sut, db) = BuildGatedSut(connection, coordinator);

        using (db)
        {
            var id = await sut.SubmitAsync(
                    CreateSession(sessionId, NonQuadBrainAgent),
                    cancellationToken: TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            Assert.True(id > 0);
        }

        Assert.Null(coordinator.Request);
        Assert.Equal(1, CountSessionRows(connection, sessionId));
        Assert.Equal(1, CountTurnRows(connection, sessionId));
    }

    /// <summary>
    /// TEST-MCP-221 ac-1: GrokCode UpsertTurnAsync persists without calling the coordinator
    /// when required turn transactions would reject keyserver signing.
    /// </summary>
    [Fact]
    public async Task UpsertTurnAsync_WhenNonQuadBrainAndCoordinatorWouldReject_PersistsWithoutCallingCoordinator()
    {
        using var connection = OpenConnection();
        var sessionId = SeedFullSession(connection, NonQuadBrainAgent);
        var coordinator = new CapturingCoordinator
        {
            InvokeMutation = false,
            Status = "rejected",
            Reason = TransactionFailureReason.UnknownKey,
            Message = "Keyserver manifest signing failed.",
        };
        var (sut, db) = BuildGatedSut(connection, coordinator);

        using (db)
        {
            var id = await sut.UpsertTurnAsync(
                    NonQuadBrainAgent,
                    sessionId,
                    new UnifiedRequestEntryDto
                    {
                        RequestId = RequestId,
                        Status = "in_progress",
                        QueryTitle = "bypass upsert",
                        PlanFile = "None",
                        TodoId = "None",
                    },
                    cancellationToken: TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            Assert.True(id > 0);
        }

        Assert.Null(coordinator.Request);
        Assert.Equal(1, CountTurnRows(connection, sessionId));
    }

    /// <summary>
    /// TEST-MCP-221 ac-3: a degraded coordinator does not block GrokCode session-log writes.
    /// </summary>
    [Fact]
    public async Task SubmitAsync_WhenNonQuadBrainAndCoordinatorDegraded_PersistsWithoutCallingCoordinator()
    {
        using var connection = OpenConnection();
        var coordinator = new CapturingCoordinator
        {
            StatusDegraded = true,
            StatusMessage = "txn degraded",
            InvokeMutation = false,
            Status = "rejected",
            Message = "Turn transaction coordinator is degraded.",
        };
        var sessionId = BuildSessionId("grok-degraded-submit", NonQuadBrainAgent);
        var (sut, db) = BuildGatedSut(connection, coordinator);

        using (db)
        {
            var id = await sut.SubmitAsync(
                    CreateSession(sessionId, NonQuadBrainAgent),
                    cancellationToken: TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            Assert.True(id > 0);
        }

        Assert.Null(coordinator.Request);
        Assert.Equal(1, CountSessionRows(connection, sessionId));
    }

    /// <summary>
    /// TEST-MCP-221 ac-1: GrokCode OpenSessionAsync persists without calling the coordinator
    /// when required turn transactions would reject keyserver signing.
    /// </summary>
    [Fact]
    public async Task OpenSessionAsync_WhenNonQuadBrainAndCoordinatorWouldReject_PersistsWithoutCallingCoordinator()
    {
        using var connection = OpenConnection();
        var coordinator = new CapturingCoordinator
        {
            InvokeMutation = false,
            Status = "rejected",
            Reason = TransactionFailureReason.UnknownKey,
            Message = "Keyserver manifest signing failed.",
        };
        var sessionId = BuildSessionId("grok-bypass-open", NonQuadBrainAgent);
        var (sut, db) = BuildGatedSut(connection, coordinator);

        using (db)
        {
            var opened = await sut.OpenSessionAsync(
                    NonQuadBrainAgent,
                    sessionId,
                    title: "Grok session",
                    model: "grok-4.6",
                    cancellationToken: TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            Assert.True(opened);
        }

        Assert.Null(coordinator.Request);
        Assert.Equal(1, CountSessionRows(connection, sessionId));
    }

    /// <summary>
    /// TEST-MCP-161: Workspace-stamp repair dry-run remains a direct pass-through.
    /// </summary>
    [Fact]
    public async Task RepairWorkspaceStampsAsync_DryRunDoesNotUseCoordinator()
    {
        using var connection = OpenConnection();
        var coordinator = new CapturingCoordinator();
        var (sut, db) = BuildGatedSut(connection, coordinator);

        using (db)
            Assert.Equal(0, await sut.RepairWorkspaceStampsAsync(dryRun: true, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true));

        Assert.Null(coordinator.Request);
    }

    /// <summary>Applied workspace-stamp repair fails closed while required transactions are active.</summary>
    [Fact]
    public async Task RepairWorkspaceStampsAsync_WhenTransactionsRequired_FailsClosedWithoutCoordinatorExecute()
    {
        using var connection = OpenConnection();
        var coordinator = new CapturingCoordinator();
        var (sut, db) = BuildGatedSut(connection, coordinator);

        using (db)
        {
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                    () => sut.RepairWorkspaceStampsAsync(dryRun: false, cancellationToken: TestContext.Current.CancellationToken))
                .ConfigureAwait(true);
            Assert.Contains("not transaction compensated", ex.Message, StringComparison.Ordinal);
        }

        Assert.Null(coordinator.Request);
    }

    /// <summary>Applied workspace-stamp repair fails closed when the transaction coordinator is degraded.</summary>
    [Fact]
    public async Task RepairWorkspaceStampsAsync_WhenCoordinatorDegraded_FailsClosedWithoutCoordinatorExecute()
    {
        using var connection = OpenConnection();
        var coordinator = new CapturingCoordinator { StatusDegraded = true, StatusMessage = "txn degraded" };
        var (sut, db) = BuildGatedSut(connection, coordinator);

        using (db)
        {
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                    () => sut.RepairWorkspaceStampsAsync(dryRun: false, cancellationToken: TestContext.Current.CancellationToken))
                .ConfigureAwait(true);
            Assert.Contains("txn degraded", ex.Message, StringComparison.Ordinal);
        }

        Assert.Null(coordinator.Request);
    }

    /// <summary>Applied workspace-stamp repair delegates when transaction gating is not required for mutations.</summary>
    [Fact]
    public async Task RepairWorkspaceStampsAsync_WhenTransactionsNotRequired_Delegates()
    {
        using var connection = OpenConnection();
        var coordinator = new CapturingCoordinator();
        var (sut, db) = BuildGatedSut(
            connection,
            coordinator,
            new TurnTransactionOptions { Enabled = true, RequiredForMutations = false });

        using (db)
            Assert.Equal(0, await sut.RepairWorkspaceStampsAsync(dryRun: false, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true));

        Assert.Null(coordinator.Request);
    }

    private static SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return connection;
    }

    private static (TransactionGatedSessionLogService Sut, McpDbContext Db) BuildGatedSut(
        SqliteConnection connection,
        CapturingCoordinator coordinator,
        TurnTransactionOptions? transactionOptions = null)
    {
        var options = new DbContextOptionsBuilder<McpDbContext>().UseSqlite(connection).Options;
        var workspaceContext = new WorkspaceContext { WorkspacePath = WorkspacePath };
        var db = new McpDbContext(options, workspaceContext);
        db.Database.EnsureCreated();
        var inner = new SessionLogService(
            db,
            NullLogger<SessionLogService>.Instance,
            Substitute.For<IChangeEventBus>(),
            workspaceContext);
        var sut = new TransactionGatedSessionLogService(
            inner,
            coordinator,
            MsOptions.Options.Create(transactionOptions ?? new TurnTransactionOptions { Enabled = true, RequiredForMutations = true }));
        return (sut, db);
    }

    private static string SeedFullSession(SqliteConnection connection, string? sourceType = null)
    {
        var agent = sourceType ?? Agent;
        var sessionId = BuildSessionId("seed", agent);
        var (sut, db) = BuildGatedSut(connection, new CapturingCoordinator());
        using (db)
            sut.SubmitAsync(CreateSession(sessionId, agent)).GetAwaiter().GetResult();
        return sessionId;
    }

    private static UnifiedSessionLogDto CreateSession(string sessionId, string? sourceType = null)
        => new()
        {
            SourceType = sourceType ?? Agent,
            SessionId = sessionId,
            Title = "Seed Session",
            Model = "gpt-5.4",
            Started = "2026-06-14T12:00:00Z",
            LastUpdated = "2026-06-14T12:30:00Z",
            Status = "in_progress",
            Turns =
            [
                new UnifiedRequestEntryDto
                {
                    RequestId = RequestId,
                    Timestamp = "2026-06-14T12:01:00Z",
                    QueryText = "seed query",
                    QueryTitle = "seed",
                    Response = "seed response",
                    Status = "in_progress",
                    PlanFile = "None",
                    TodoId = "None",
                    Tags = ["seed-tag-a", "seed-tag-b"],
                    ContextList = ["docs/a.md", "docs/b.md"],
                    Actions =
                    [
                        new UnifiedActionDto { Order = 0, Description = "action zero", Type = "edit", Status = "completed", FilePath = "src/a.cs" },
                        new UnifiedActionDto { Order = 1, Description = "action one", Type = "create", Status = "completed", FilePath = "src/b.cs" },
                    ],
                    ProcessingDialog =
                    [
                        new ProcessingDialogItemDto { Timestamp = "2026-06-14T12:02:00Z", Role = "model", Content = "thinking", Category = "reasoning" },
                        new ProcessingDialogItemDto { Timestamp = "2026-06-14T12:03:00Z", Role = "tool", Content = "ran", Category = "tool_call" },
                    ],
                    Commits =
                    [
                        new SessionLogCommitDto { Sha = "sha-1", Branch = "main", Message = "first", Author = "Codex" },
                        new SessionLogCommitDto { Sha = "sha-2", Branch = "main", Message = "second", Author = "Codex" },
                    ],
                    DesignDecisions = ["chose A over B", "deferred C"],
                    RequirementsDiscovered = ["FR-MCP-120"],
                    FilesModified = ["src/a.cs", "src/b.cs"],
                    Blockers = ["needs review"],
                },
            ],
        };

    private static async Task<UnifiedSessionLogDto?> GetSessionAsync(SqliteConnection connection, string sessionId)
    {
        var (sut, db) = BuildGatedSut(connection, new CapturingCoordinator());
        using (db)
            return await sut.GetAsync(Agent, sessionId).ConfigureAwait(true);
    }

    private static async Task<UnifiedRequestEntryDto> GetTurnAsync(SqliteConnection connection, string sessionId)
    {
        var session = await GetSessionAsync(connection, sessionId).ConfigureAwait(true);
        Assert.NotNull(session);
        Assert.NotNull(session!.Turns);
        return session.Turns!.Single(turn => turn.RequestId == RequestId);
    }

    private static int CountSessionRows(SqliteConnection connection, string sessionId)
        => ScalarCount(connection, "SELECT COUNT(*) FROM SessionLogs WHERE SessionId = $sid", ("$sid", sessionId));

    private static int CountTurnRows(SqliteConnection connection, string sessionId)
        => ScalarCount(
            connection,
            "SELECT COUNT(*) FROM SessionLogTurns t JOIN SessionLogs s ON s.Id = t.SessionLogId WHERE s.SessionId = $sid",
            ("$sid", sessionId));

    private static int CountAllChildRows(SqliteConnection connection, string sessionId)
        => CountChildRows(connection, sessionId, null);

    private static int CountVisibleChildRows(SqliteConnection connection, string sessionId)
        => CountChildRows(connection, sessionId, isDeleted: false);

    private static int CountSoftDeletedChildRows(SqliteConnection connection, string sessionId)
        => CountChildRows(connection, sessionId, isDeleted: true);

    private static int CountChildRows(SqliteConnection connection, string sessionId, bool? isDeleted)
    {
        var tables = new[]
        {
            "SessionLogActions", "SessionLogTurnTags", "SessionLogTurnContexts",
            "SessionLogProcessingDialogs", "SessionLogCommits", "SessionLogTurnStringLists",
        };
        var total = 0;
        var deletedPredicate = isDeleted.HasValue ? " AND c.IsDeleted = $isDeleted" : string.Empty;
        foreach (var table in tables)
        {
            total += ScalarCount(
                connection,
                $"SELECT COUNT(*) FROM {table} c JOIN SessionLogTurns t ON t.Id = c.SessionLogTurnId " +
                $"JOIN SessionLogs s ON s.Id = t.SessionLogId WHERE s.SessionId = $sid{deletedPredicate}",
                isDeleted.HasValue ? [("$sid", sessionId), ("$isDeleted", isDeleted.Value ? 1 : 0)] : [("$sid", sessionId)]);
        }

        return total;
    }

    private static string FindRepositoryRoot()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static int ScalarCount(SqliteConnection connection, string sql, params (string Name, object Value)[] args)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (name, value) in args)
            cmd.Parameters.AddWithValue(name, value);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private static long ScalarLong(SqliteConnection connection, string sql, params (string Name, object Value)[] args)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (name, value) in args)
            cmd.Parameters.AddWithValue(name, value);
        return Convert.ToInt64(cmd.ExecuteScalar());
    }

    private static string BuildSessionId(string suffix, string? sourceType = null)
        => $"{sourceType ?? Agent}-20260614T120000Z-{suffix}";

    private sealed class CapturingCoordinator : ITurnTransactionCoordinator
    {
        public TurnTransactionRequest? Request { get; private set; }

        public bool InvokeMutation { get; init; } = true;

        public bool InvokeRollback { get; init; }

        public string Status { get; init; } = "committed";

        public TransactionFailureReason Reason { get; init; } = TransactionFailureReason.None;

        public string? Message { get; init; }

        public bool StatusEnabled { get; init; } = true;

        public bool StatusDegraded { get; init; }

        public string StatusMessage { get; init; } = "ready";

        public bool RollbackAttempted { get; private set; }

        public bool RollbackSucceeded { get; private set; }

        public Action? BeforeRollback { get; init; }

        public Task<TurnTransactionResult> ExecuteAsync(
            TurnTransactionRequest request,
            Func<CancellationToken, Task<TurnMutationResult>> mutation,
            CancellationToken cancellationToken = default)
            => ExecuteCoreAsync(request, mutation, cancellationToken);

        public TurnTransactionStatusResponse GetStatus()
            => new()
            {
                Enabled = StatusEnabled,
                Degraded = StatusDegraded,
                LastReason = TransactionFailureReason.None,
                Message = StatusMessage,
            };

        private async Task<TurnTransactionResult> ExecuteCoreAsync(
            TurnTransactionRequest request,
            Func<CancellationToken, Task<TurnMutationResult>> mutation,
            CancellationToken cancellationToken)
        {
            Request = request;
            TurnMutationResult? mutationResult = null;
            string? rollbackError = null;

            if (InvokeMutation)
            {
                mutationResult = await mutation(cancellationToken).ConfigureAwait(false);
                if (InvokeRollback && mutationResult.RollbackAsync is not null)
                {
                    RollbackAttempted = true;
                    BeforeRollback?.Invoke();
                    try
                    {
                        await mutationResult.RollbackAsync(cancellationToken).ConfigureAwait(false);
                        RollbackSucceeded = true;
                    }
                    catch (Exception ex)
                    {
                        rollbackError = ex.Message;
                    }
                }
            }

            return new TurnTransactionResult
            {
                TransactionId = request.TransactionId ?? "txn-sessionlog-test",
                Status = Status,
                Reason = Reason,
                MutationApplied = InvokeMutation,
                MutationResult = mutationResult,
                Message = Message,
                RollbackAttempted = RollbackAttempted,
                RollbackSucceeded = RollbackSucceeded,
                RollbackError = rollbackError,
            };
        }
    }
}
