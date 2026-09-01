using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using MsOptions = Microsoft.Extensions.Options.Options;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-HANDOFF-005 / TEST-HANDOFF-007: durable ingest/approval leases, TODO provenance,
/// SaveChanges compensation, and Success/ErrorCode honesty.
/// </summary>
public sealed class HandoffDurabilityTests : IDisposable
{
    private readonly string _workspace;
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<McpDbContext> _dbOptions;
    private readonly AdjustableTimeProvider _time = new(new DateTimeOffset(2026, 8, 17, 0, 0, 0, TimeSpan.Zero));
    private readonly FailNextSaveInterceptor _failNextSave = new();

    /// <summary>Creates an isolated in-memory SQLite workspace for durability tests.</summary>
    public HandoffDurabilityTests()
    {
        _workspace = Path.Combine(Path.GetTempPath(), "handoff-durability", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workspace);
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _dbOptions = new DbContextOptionsBuilder<McpDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(_failNextSave)
            .Options;
        using var db = CreateDb();
        db.Database.EnsureCreated();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _connection.Dispose();
        if (Directory.Exists(_workspace))
            Directory.Delete(_workspace, recursive: true);
    }

    /// <summary>A live in-flight reservation is not mapped as a completed replay.</summary>
    [Fact]
    public async Task IngestAsync_ConcurrentReplayOfLiveLease_ReturnsInProgressNotReplay()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var extractor = Substitute.For<IHandoffOneShotExtractor>();
        extractor.ExtractAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(async ci =>
            {
                started.TrySetResult();
                await release.Task.WaitAsync(ci.Arg<CancellationToken>());
                return SuccessfulExtraction(ValidDraft("MCP-HANDOFFDEMO-101"));
            });

        var first = CreateService(extractor, Substitute.For<ITodoService>());
        var second = CreateService(SuccessfulExtractor(ValidDraft("MCP-HANDOFFDEMO-101")), Substitute.For<ITodoService>());
        var request = ContentRequest("lease-live");
        var firstTask = first.IngestAsync(request, TestContext.Current.CancellationToken);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        var replay = await second.IngestAsync(request, TestContext.Current.CancellationToken);
        release.TrySetResult();
        var completed = await firstTask;

        Assert.False(replay.Success);
        Assert.False(replay.Replayed);
        Assert.Equal(HandoffErrorCodes.InProgress, replay.ErrorCode);
        Assert.True(completed.Success, completed.Error);
        Assert.Equal(HandoffReviewState.None, completed.Provenance!.ReviewState);
    }

    /// <summary>A stale processing lease is taken over by a second service instance.</summary>
    [Fact]
    public async Task IngestAsync_StaleLease_IsTakenOverBySecondInstance()
    {
        using (var db = CreateDb())
        {
            db.HandoffIngestionRuns.Add(IncompleteRun("handoff-run-stale", "stale-content", processingOwner: "dead-instance"));
            db.SaveChanges();
        }

        _time.Advance(HandoffLeaseDefaults.Duration + TimeSpan.FromSeconds(1));
        var todo = Substitute.For<ITodoService>();
        var sut = CreateService(SuccessfulExtractor(ValidDraft("MCP-HANDOFFDEMO-102")), todo);
        var result = await sut.IngestAsync(ContentRequest("stale-content"), TestContext.Current.CancellationToken);

        Assert.True(result.Success, result.Error);
        Assert.False(result.Replayed);
        Assert.Equal("MCP-HANDOFFDEMO-102", result.Draft!.Id);
        using var verify = CreateDb();
        var stored = Assert.Single(verify.HandoffIngestionRuns);
        Assert.Equal(nameof(HandoffProcessingState.Terminal), stored.ProcessingState);
        Assert.NotEqual("dead-instance", stored.ProcessingOwner);
    }

    /// <summary>Cancellation persists a terminal failed run and then still throws.</summary>
    [Fact]
    public async Task IngestAsync_CancelledAfterReserve_PersistsFailureThenThrows()
    {
        using var cts = new CancellationTokenSource();
        var extractor = Substitute.For<IHandoffOneShotExtractor>();
        extractor.ExtractAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(async ci =>
            {
                cts.Cancel();
                ci.Arg<CancellationToken>().ThrowIfCancellationRequested();
                return SuccessfulExtraction(ValidDraft("MCP-HANDOFFDEMO-103"));
            });

        var sut = CreateService(extractor, Substitute.For<ITodoService>());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sut.IngestAsync(ContentRequest("cancel-me"), cts.Token));

        using var verify = CreateDb();
        var stored = Assert.Single(verify.HandoffIngestionRuns);
        Assert.Equal(nameof(HandoffReviewState.Failed), stored.ReviewState);
        Assert.Equal(HandoffErrorCodes.Cancelled, stored.ErrorCode);
        Assert.False(stored.Succeeded);
        Assert.Equal(nameof(HandoffProcessingState.Terminal), stored.ProcessingState);
    }

    /// <summary>A crashed Approving claim with an expired lease can be recovered by another instance.</summary>
    [Fact]
    public async Task ApproveAsync_StaleApprovalLease_IsRecoveredBySecondInstance()
    {
        var todo = TrackingTodo();
        var ingest = CreateService(SuccessfulExtractor(ValidDraft("MCP-HANDOFFDEMO-104", 0.9)), todo);
        var pending = await ingest.IngestAsync(ContentRequest("approve-stale", HandoffIngestionMode.RequireReview), TestContext.Current.CancellationToken);
        using (var db = CreateDb())
        {
            var run = db.HandoffIngestionRuns.Single(item => item.RunId == pending.Provenance!.RunId);
            run.ReviewState = nameof(HandoffReviewState.Approving);
            run.ApprovalOwner = "dead-approver";
            run.ApprovalLeaseExpiresAtUtc = _time.GetUtcNow().Subtract(TimeSpan.FromMinutes(1));
            db.SaveChanges();
        }

        var approver = CreateService(SuccessfulExtractor(ValidDraft("MCP-HANDOFFDEMO-104", 0.9)), todo);
        var approved = await approver.ApproveAsync(pending.Provenance!.RunId, new HandoffApprovalRequest { Approved = true, Reviewer = "ops" }, TestContext.Current.CancellationToken);

        Assert.True(approved.Created);
        Assert.Equal(1, todo.CreatedCount);
        Assert.Equal("MCP-HANDOFFDEMO-104", approved.CreatedTodoId);
    }

    /// <summary>Concurrent approvals create exactly one TODO.</summary>
    [Fact]
    public async Task ApproveAsync_TwoInstances_CreateExactlyOneTodo()
    {
        var todo = TrackingTodo();
        var ingest = CreateService(SuccessfulExtractor(ValidDraft("MCP-HANDOFFDEMO-105", 0.9)), todo);
        var pending = await ingest.IngestAsync(ContentRequest("approve-race", HandoffIngestionMode.RequireReview), TestContext.Current.CancellationToken);
        var first = CreateService(SuccessfulExtractor(ValidDraft("MCP-HANDOFFDEMO-105", 0.9)), todo);
        var second = CreateService(SuccessfulExtractor(ValidDraft("MCP-HANDOFFDEMO-105", 0.9)), todo);
        var results = await Task.WhenAll(
            first.ApproveAsync(pending.Provenance!.RunId, new HandoffApprovalRequest { Approved = true, Reviewer = "a" }, TestContext.Current.CancellationToken),
            second.ApproveAsync(pending.Provenance.RunId, new HandoffApprovalRequest { Approved = true, Reviewer = "b" }, TestContext.Current.CancellationToken));

        Assert.Equal(1, todo.CreatedCount);
        Assert.Contains(results, item => item.Created || item.Replayed);
        Assert.All(results, item => Assert.Equal("MCP-HANDOFFDEMO-105", item.CreatedTodoId ?? pending.Draft!.Id));
    }

    /// <summary>Rejection does not create a TODO and persists Rejected.</summary>
    [Fact]
    public async Task ApproveAsync_Rejection_DoesNotCreateTodo()
    {
        var todo = TrackingTodo();
        var sut = CreateService(SuccessfulExtractor(ValidDraft("MCP-HANDOFFDEMO-106", 0.9)), todo);
        var pending = await sut.IngestAsync(ContentRequest("reject", HandoffIngestionMode.RequireReview), TestContext.Current.CancellationToken);
        var rejected = await sut.ApproveAsync(pending.Provenance!.RunId, new HandoffApprovalRequest { Approved = false, Reviewer = "ops" }, TestContext.Current.CancellationToken);

        Assert.False(rejected.Created);
        Assert.Equal(HandoffReviewState.Rejected, rejected.Provenance!.ReviewState);
        Assert.Equal(0, todo.CreatedCount);
    }

    /// <summary>A TODO created by this run is healed instead of reported as a caller-owned collision.</summary>
    [Fact]
    public async Task IngestAsync_ExistingTodoFromThisRun_HealsInsteadOfColliding()
    {
        var todo = TrackingTodo();
        var first = CreateService(SuccessfulExtractor(ValidDraft("MCP-HANDOFFDEMO-107", 0.9)), todo);
        var created = await first.IngestAsync(ContentRequest("heal-me", HandoffIngestionMode.CreateWhenConfident), TestContext.Current.CancellationToken);
        Assert.True(created.Created);
        using (var db = CreateDb())
        {
            var run = db.HandoffIngestionRuns.Single(item => item.RunId == created.Provenance!.RunId);
            run.CreatedTodoId = null;
            run.ReviewState = nameof(HandoffReviewState.None);
            run.Succeeded = false;
            run.ProcessingState = nameof(HandoffProcessingState.Processing);
            run.ProcessingLeaseExpiresAtUtc = _time.GetUtcNow().Subtract(TimeSpan.FromMinutes(1));
            db.SaveChanges();
        }

        var second = CreateService(SuccessfulExtractor(ValidDraft("MCP-HANDOFFDEMO-107", 0.9)), todo);
        var healed = await second.IngestAsync(ContentRequest("heal-me", HandoffIngestionMode.CreateWhenConfident), TestContext.Current.CancellationToken);

        Assert.True(healed.Created || healed.Replayed);
        Assert.Equal("MCP-HANDOFFDEMO-107", healed.CreatedTodoId);
        Assert.Equal(1, todo.CreatedCount);
        Assert.DoesNotContain(healed.Diagnostics, item => item.Code == "todo_collision");
    }

    /// <summary>A caller-owned TODO id is a recoverable non-success collision with a stable ErrorCode.</summary>
    [Fact]
    public async Task IngestAsync_CallerOwnedTodoId_IsNonSuccessCollision()
    {
        var todo = TrackingTodo();
        todo.SeedExisting("MCP-HANDOFFDEMO-108", idempotencyKey: "caller-owned");
        var sut = CreateService(SuccessfulExtractor(ValidDraft("MCP-HANDOFFDEMO-108", 0.9)), todo);
        var result = await sut.IngestAsync(ContentRequest("true-collision", HandoffIngestionMode.CreateWhenConfident), TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.False(result.Created);
        Assert.Equal(HandoffErrorCodes.TodoCollision, result.ErrorCode);
        Assert.True(result.RequiresReview);
        Assert.DoesNotContain(result.Diagnostics, item => item.Severity == HandoffDiagnosticSeverity.Error && item.Code == "todo_collision" && result.Success);
        using var verify = CreateDb();
        var stored = Assert.Single(verify.HandoffIngestionRuns);
        Assert.Equal(HandoffErrorCodes.TodoCollision, stored.ErrorCode);
        Assert.False(stored.Succeeded);
    }

    /// <summary>Invalid RequireReview drafts are Failed and non-approvable, never Success=true.</summary>
    [Fact]
    public async Task IngestAsync_InvalidRequireReviewDraft_IsFailedAndNotSuccess()
    {
        const string json =
            """{"id":"MCP-HANDOFFDEMO-109","title":"Demo","section":"MCP Server","priority":"high","estimate":"2h","description":[" "],"technicalDetails":["   "],"implementationTasks":[{"task":"Write tests","done":false}],"dependsOn":[],"functionalRequirements":["FR-HANDOFF-001"],"technicalRequirements":["TR-HANDOFF-CONTRACT-001"],"confidence":0.9,"unknownSourceNotes":[]}""";
        var sut = CreateService(SuccessfulExtractor(json), TrackingTodo());
        var result = await sut.IngestAsync(ContentRequest("invalid-review", HandoffIngestionMode.RequireReview), TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal(HandoffReviewState.Failed, result.Provenance!.ReviewState);
        Assert.False(result.RequiresReview);
        Assert.Contains(result.Diagnostics, item => item.Severity == HandoffDiagnosticSeverity.Error);
    }

    /// <summary>GET preserves persisted Success and ErrorCode after a failed collision.</summary>
    [Fact]
    public async Task GetRunAsync_AfterCollision_PreservesPersistedSuccessAndErrorCode()
    {
        var todo = TrackingTodo();
        todo.SeedExisting("MCP-HANDOFFDEMO-110", idempotencyKey: "other");
        var sut = CreateService(SuccessfulExtractor(ValidDraft("MCP-HANDOFFDEMO-110", 0.9)), todo);
        var created = await sut.IngestAsync(ContentRequest("get-collision", HandoffIngestionMode.CreateWhenConfident), TestContext.Current.CancellationToken);
        var loaded = await sut.GetRunAsync(created.Provenance!.RunId, TestContext.Current.CancellationToken);

        Assert.Equal(created.Success, loaded.Success);
        Assert.Equal(created.ErrorCode, loaded.ErrorCode);
        Assert.False(loaded.Success);
        Assert.Equal(HandoffErrorCodes.TodoCollision, loaded.ErrorCode);
    }

    /// <summary>Save failure after TODO creation heals the run from a fresh context without clearing unrelated tracked entities.</summary>
    [Fact]
    public async Task SaveRunAfterTodo_CommitFailure_HealsAndPreservesUnrelatedTrackedEntities()
    {
        var todo = TrackingTodo();
        var sut = CreateService(SuccessfulExtractor(ValidDraft("MCP-HANDOFFDEMO-111", 0.9)), todo);
        var db = ((HandoffIngestionService)sut).GetType()
            .GetField("_db", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(sut) as McpDbContext;
        Assert.NotNull(db);
        _failNextSave.Unrelated = new AgentDefinitionEntity
        {
            Id = "agent-unrelated",
            WorkspaceId = _workspace,
            DisplayName = "Keep me",
            DefaultLaunchCommand = "dotnet",
            DefaultInstructionFile = "AGENTS.md",
            DefaultBranchStrategy = "feature/x",
            DefaultSeedPrompt = "seed",
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
        };
        _failNextSave.FailOnce = true;

        var result = await sut.IngestAsync(ContentRequest("save-fail", HandoffIngestionMode.CreateWhenConfident), TestContext.Current.CancellationToken);

        Assert.True(result.Created);
        Assert.Equal("MCP-HANDOFFDEMO-111", result.CreatedTodoId);
        Assert.Equal(EntityState.Added, db.Entry(_failNextSave.Unrelated).State);
        using var verify = CreateDb();
        var stored = Assert.Single(verify.HandoffIngestionRuns);
        Assert.Equal(nameof(HandoffReviewState.Created), stored.ReviewState);
        Assert.Equal("MCP-HANDOFFDEMO-111", stored.CreatedTodoId);
    }

    /// <summary>Cancellation after TODO creation still persists the Created receipt and then throws.</summary>
    [Fact]
    public async Task SaveRunAfterTodo_CancellationAfterTodo_PersistsCreatedThenThrows()
    {
        var todo = TrackingTodo();
        using var cts = new CancellationTokenSource();
        todo.OnCreated = () => cts.Cancel();
        var sut = CreateService(SuccessfulExtractor(ValidDraft("MCP-HANDOFFDEMO-112", 0.9)), todo);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            sut.IngestAsync(ContentRequest("cancel-after-todo", HandoffIngestionMode.CreateWhenConfident), cts.Token));

        using var verify = CreateDb();
        var stored = Assert.Single(verify.HandoffIngestionRuns);
        Assert.Equal(nameof(HandoffReviewState.Created), stored.ReviewState);
        Assert.Equal("MCP-HANDOFFDEMO-112", stored.CreatedTodoId);
        Assert.True(stored.Succeeded);
    }

    /// <summary>P1-1: lease expiry during a live blocked extraction is taken over; the stale owner cannot create or clobber the Created receipt.</summary>
    [Fact]
    public async Task IngestAsync_LeaseExpiresDuringLiveExtraction_TakeoverWinsAndFirstCannotCreate()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstExtractor = Substitute.For<IHandoffOneShotExtractor>();
        firstExtractor.ExtractAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(async ci =>
            {
                started.TrySetResult();
                await release.Task.WaitAsync(ci.Arg<CancellationToken>());
                return SuccessfulExtraction(ValidDraft("MCP-HANDOFFDEMO-201", 0.9));
            });

        var lease = ShortLease();
        var todo = TrackingTodo();
        var first = CreateService(firstExtractor, todo, lease);
        var second = CreateService(SuccessfulExtractor(ValidDraft("MCP-HANDOFFDEMO-201", 0.9)), todo, lease);
        var request = ContentRequest("live-lease-expiry", HandoffIngestionMode.CreateWhenConfident);
        var firstTask = first.IngestAsync(request, TestContext.Current.CancellationToken);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await WaitUntilAsync(
            () =>
            {
                using var db = CreateDb();
                var run = db.HandoffIngestionRuns.SingleOrDefault();
                return run is not null
                    && run.ProcessingState == nameof(HandoffProcessingState.Processing)
                    && !string.IsNullOrWhiteSpace(run.ProcessingOwner);
            },
            TimeSpan.FromSeconds(5));
        _time.Advance(lease.Duration + TimeSpan.FromSeconds(1));
        var takeover = await second.IngestAsync(request, TestContext.Current.CancellationToken);
        release.TrySetResult();
        var firstResult = await firstTask;

        Assert.True(takeover.Created, takeover.Error);
        Assert.False(firstResult.Created);
        Assert.Equal(HandoffErrorCodes.InProgress, firstResult.ErrorCode);
        Assert.Equal(1, todo.CreatedCount);
        using var verify = CreateDb();
        var stored = Assert.Single(verify.HandoffIngestionRuns);
        Assert.Equal(nameof(HandoffReviewState.Created), stored.ReviewState);
        Assert.Equal(nameof(HandoffProcessingState.Terminal), stored.ProcessingState);
        Assert.Equal("MCP-HANDOFFDEMO-201", stored.CreatedTodoId);
        Assert.True(stored.Succeeded);
        Assert.Equal(takeover.CreatedTodoId, stored.CreatedTodoId);
    }

    /// <summary>P1-2: an approval lease that expires during a live create is recovered by the second instance; the stale claimant cannot mark Created.</summary>
    [Fact]
    public async Task ApproveAsync_LeaseExpiresDuringLiveCreate_SecondInstanceWins()
    {
        var lease = ShortLease();
        var todo = TrackingTodo();
        todo.CreateGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var ingest = CreateService(SuccessfulExtractor(ValidDraft("MCP-HANDOFFDEMO-202", 0.9)), todo, lease);
        var pending = await ingest.IngestAsync(ContentRequest("live-approve-expiry", HandoffIngestionMode.RequireReview), TestContext.Current.CancellationToken);
        var first = CreateService(SuccessfulExtractor(ValidDraft("MCP-HANDOFFDEMO-202", 0.9)), todo, lease);
        var firstTask = first.ApproveAsync(pending.Provenance!.RunId, new HandoffApprovalRequest { Approved = true, Reviewer = "a" }, TestContext.Current.CancellationToken);
        var firstOwner = await WaitForApprovalOwnerAsync(pending.Provenance.RunId);
        _time.Advance(lease.Duration + TimeSpan.FromSeconds(1));
        var second = CreateService(SuccessfulExtractor(ValidDraft("MCP-HANDOFFDEMO-202", 0.9)), todo, lease);
        var secondTask = second.ApproveAsync(pending.Provenance.RunId, new HandoffApprovalRequest { Approved = true, Reviewer = "b" }, TestContext.Current.CancellationToken);
        await WaitUntilAsync(
            () =>
            {
                using var db = CreateDb();
                var run = db.HandoffIngestionRuns.Single(item => item.RunId == pending.Provenance.RunId);
                return !string.IsNullOrWhiteSpace(run.ApprovalOwner)
                    && !string.Equals(run.ApprovalOwner, firstOwner, StringComparison.Ordinal);
            },
            TimeSpan.FromSeconds(5));
        todo.CreateGate.TrySetResult();
        var takeover = await secondTask;
        var firstResult = await firstTask;

        Assert.True(takeover.Created, takeover.Error);
        Assert.False(firstResult.Created);
        Assert.Equal(HandoffErrorCodes.LostOwnership, firstResult.ErrorCode);
        Assert.Equal(1, todo.CreatedCount);
        using var verify = CreateDb();
        var stored = Assert.Single(verify.HandoffIngestionRuns);
        Assert.Equal(nameof(HandoffReviewState.Created), stored.ReviewState);
        Assert.Equal("MCP-HANDOFFDEMO-202", stored.CreatedTodoId);
        Assert.True(stored.Succeeded);
    }

    /// <summary>P1-2: a live approval claimant is not overwritten by a stale second claim.</summary>
    [Fact]
    public async Task ApproveAsync_LiveClaimant_RejectsStaleSecondClaim()
    {
        var todo = TrackingTodo();
        todo.CreateGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var ingest = CreateService(SuccessfulExtractor(ValidDraft("MCP-HANDOFFDEMO-203", 0.9)), todo);
        var pending = await ingest.IngestAsync(ContentRequest("stale-claimant", HandoffIngestionMode.RequireReview), TestContext.Current.CancellationToken);
        var first = CreateService(SuccessfulExtractor(ValidDraft("MCP-HANDOFFDEMO-203", 0.9)), todo);
        var firstTask = first.ApproveAsync(pending.Provenance!.RunId, new HandoffApprovalRequest { Approved = true, Reviewer = "a" }, TestContext.Current.CancellationToken);
        await WaitForApprovalOwnerAsync(pending.Provenance.RunId);
        var second = CreateService(SuccessfulExtractor(ValidDraft("MCP-HANDOFFDEMO-203", 0.9)), todo);
        var secondResult = await second.ApproveAsync(pending.Provenance.RunId, new HandoffApprovalRequest { Approved = true, Reviewer = "b" }, TestContext.Current.CancellationToken);
        todo.CreateGate.TrySetResult();
        var firstResult = await firstTask;

        Assert.False(secondResult.Created);
        Assert.Equal(HandoffErrorCodes.InProgress, secondResult.ErrorCode);
        Assert.True(firstResult.Created, firstResult.Error);
        Assert.Equal(1, todo.CreatedCount);
        using var verify = CreateDb();
        var stored = Assert.Single(verify.HandoffIngestionRuns);
        Assert.Equal(nameof(HandoffReviewState.Created), stored.ReviewState);
        Assert.Equal("MCP-HANDOFFDEMO-203", stored.CreatedTodoId);
        Assert.True(stored.Succeeded);
    }

    /// <summary>P1-3: same idempotency key with a changed payload is a collision, not a heal.</summary>
    [Fact]
    public async Task IngestAsync_SameKeyChangedPayload_IsCollisionNotHeal()
    {
        var todo = TrackingTodo();
        var first = CreateService(SuccessfulExtractor(ValidDraft("MCP-HANDOFFDEMO-204", 0.9)), todo);
        var created = await first.IngestAsync(ContentRequest("changed-payload", HandoffIngestionMode.CreateWhenConfident), TestContext.Current.CancellationToken);
        Assert.True(created.Created);
        using (var db = CreateDb())
        {
            var run = db.HandoffIngestionRuns.Single(item => item.RunId == created.Provenance!.RunId);
            run.CreatedTodoId = null;
            run.ReviewState = nameof(HandoffReviewState.None);
            run.Succeeded = false;
            run.ProcessingState = nameof(HandoffProcessingState.Processing);
            run.ProcessingLeaseExpiresAtUtc = _time.GetUtcNow().Subtract(TimeSpan.FromMinutes(1));
            db.SaveChanges();
        }

        const string changed =
            """{"id":"MCP-HANDOFFDEMO-204","title":"Different title","section":"MCP Server","priority":"high","estimate":"2h","description":["Do the work"],"technicalDetails":["Use the service"],"implementationTasks":[{"task":"Write tests","done":false}],"dependsOn":[],"functionalRequirements":["FR-HANDOFF-001"],"technicalRequirements":["TR-HANDOFF-CONTRACT-001"],"confidence":0.9,"unknownSourceNotes":[]}""";
        var second = CreateService(SuccessfulExtractor(changed), todo);
        var result = await second.IngestAsync(ContentRequest("changed-payload", HandoffIngestionMode.CreateWhenConfident), TestContext.Current.CancellationToken);

        Assert.False(result.Created);
        Assert.Equal(HandoffErrorCodes.TodoCollision, result.ErrorCode);
        Assert.Equal(1, todo.CreatedCount);
    }

    /// <summary>P1-4: compensation does not report Created when the TODO is absent after an ambiguous save.</summary>
    [Fact]
    public async Task SaveRunAfterTodo_TodoAbsent_DoesNotReportCreatedFromMemory()
    {
        var todo = TrackingTodo();
        todo.HideFromGet = true;
        var sut = CreateService(SuccessfulExtractor(ValidDraft("MCP-HANDOFFDEMO-205", 0.9)), todo);
        _failNextSave.FailOnce = true;
        var result = await sut.IngestAsync(ContentRequest("todo-absent", HandoffIngestionMode.CreateWhenConfident), TestContext.Current.CancellationToken);

        Assert.False(result.Created);
        Assert.Equal(HandoffErrorCodes.CompensationFailed, result.ErrorCode);
    }

    /// <summary>P1-4: caller cancellation after create persists a Created receipt then throws.</summary>
    [Fact]
    public async Task SaveRunAfterTodo_CallerCancellation_PersistsCreatedReceipt()
    {
        var todo = TrackingTodo();
        using var cts = new CancellationTokenSource();
        todo.OnCreated = () => cts.Cancel();
        var sut = CreateService(SuccessfulExtractor(ValidDraft("MCP-HANDOFFDEMO-206", 0.9)), todo);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            sut.IngestAsync(ContentRequest("cancel-receipt", HandoffIngestionMode.CreateWhenConfident), cts.Token));

        using var verify = CreateDb();
        var stored = Assert.Single(verify.HandoffIngestionRuns);
        Assert.Equal(nameof(HandoffReviewState.Created), stored.ReviewState);
        Assert.Equal("MCP-HANDOFFDEMO-206", stored.CreatedTodoId);
    }

    /// <summary>P1-4: GetById timeout during compensation is a stable failure, not a Created memory report.</summary>
    [Fact]
    public async Task SaveRunAfterTodo_CompensationTimeout_DoesNotReportCreatedFromMemory()
    {
        var todo = TrackingTodo();
        todo.GetDelay = TimeSpan.FromSeconds(3);
        todo.HideFromGet = true;
        var sut = CreateService(
            SuccessfulExtractor(ValidDraft("MCP-HANDOFFDEMO-207", 0.9)),
            todo,
            new HandoffLeaseOptions { Duration = TimeSpan.FromSeconds(30), HeartbeatInterval = TimeSpan.FromHours(1), CompensationTimeout = TimeSpan.FromMilliseconds(50) });
        _failNextSave.FailOnce = true;
        var result = await sut.IngestAsync(ContentRequest("compensate-timeout", HandoffIngestionMode.CreateWhenConfident), TestContext.Current.CancellationToken);

        Assert.False(result.Created);
        Assert.Equal(HandoffErrorCodes.CompensationFailed, result.ErrorCode);
    }

    /// <summary>P1-7: undefined mode never creates and returns a stable validation code.</summary>
    [Fact]
    public async Task IngestAsync_UndefinedMode_DoesNotCreate()
    {
        var todo = TrackingTodo();
        var sut = CreateService(SuccessfulExtractor(ValidDraft("MCP-HANDOFFDEMO-208", 0.9)), todo);
        var result = await sut.IngestAsync(new HandoffIngestionRequest
        {
            SourceKind = HandoffSourceKind.Content,
            Content = "bad-mode",
            Mode = (HandoffIngestionMode)999,
        }, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.False(result.Created);
        Assert.Equal(HandoffErrorCodes.InvalidMode, result.ErrorCode);
        Assert.Equal(0, todo.CreatedCount);
    }

    /// <summary>P2-2: a caller-selected prompt template is rejected so replay identity stays truthful.</summary>
    [Fact]
    public async Task IngestAsync_CustomPromptTemplate_IsRejected()
    {
        var sut = CreateService(SuccessfulExtractor(ValidDraft("MCP-HANDOFFDEMO-209")), Substitute.For<ITodoService>());
        var result = await sut.IngestAsync(new HandoffIngestionRequest
        {
            SourceKind = HandoffSourceKind.Content,
            Content = "custom-template",
            Mode = HandoffIngestionMode.DraftOnly,
            PromptTemplateId = "other-template",
        }, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal(HandoffErrorCodes.InvalidPromptTemplate, result.ErrorCode);
    }

    /// <summary>P2-3: credential-bearing locator, model, and review notes are sanitized in the persisted run.</summary>
    [Fact]
    public async Task IngestAndApprove_CredentialBearingProvenance_IsSanitized()
    {
        var extractor = Substitute.For<IHandoffOneShotExtractor>();
        extractor.ExtractAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new HandoffExtractionResult
            {
                Success = true,
                ResponseText = ValidDraft("MCP-HANDOFFDEMO-210", 0.9),
                AgentName = "agent api_key=supersecretvalue",
                PromptVersion = HandoffPromptDefaults.PromptVersion,
                TemplateVersion = HandoffPromptDefaults.TemplateId,
                Model = "model api_key=supersecretvalue",
            });
        var sut = CreateService(extractor, TrackingTodo());
        var ingested = await sut.IngestAsync(new HandoffIngestionRequest
        {
            SourceKind = HandoffSourceKind.Content,
            Content = "secret-handoff",
            Mode = HandoffIngestionMode.RequireReview,
            AgentName = "agent api_key=supersecretvalue",
        }, TestContext.Current.CancellationToken);
        Assert.True(ingested.Success, ingested.Error);
        var approved = await sut.ApproveAsync(
            ingested.Provenance!.RunId,
            new HandoffApprovalRequest
            {
                Approved = false,
                Reviewer = "ops api_key=supersecretvalue",
                Notes = "token=supersecretvalue",
            },
            TestContext.Current.CancellationToken);

        Assert.DoesNotContain("supersecretvalue", approved.Provenance!.SourceLocator ?? string.Empty, StringComparison.Ordinal);
        Assert.DoesNotContain("supersecretvalue", approved.Provenance.Agent ?? string.Empty, StringComparison.Ordinal);
        using var verify = CreateDb();
        var stored = Assert.Single(verify.HandoffIngestionRuns);
        Assert.DoesNotContain("supersecretvalue", stored.SourceLocator, StringComparison.Ordinal);
        Assert.DoesNotContain("supersecretvalue", stored.Reviewer ?? string.Empty, StringComparison.Ordinal);
        Assert.DoesNotContain("supersecretvalue", stored.ReviewNotes ?? string.Empty, StringComparison.Ordinal);
    }

    private static HandoffLeaseOptions ShortLease()
        => new()
        {
            Duration = TimeSpan.FromMilliseconds(1),
            HeartbeatInterval = TimeSpan.FromHours(1),
            CompensationTimeout = TimeSpan.FromSeconds(5),
        };

    private async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(timeout);
        while (!condition())
        {
            await Task.Delay(10, cts.Token).ConfigureAwait(true);
        }
    }

    private async Task<string> WaitForApprovalOwnerAsync(string runId)
    {
        string? owner = null;
        await WaitUntilAsync(
            () =>
            {
                using var db = CreateDb();
                var run = db.HandoffIngestionRuns.SingleOrDefault(item => item.RunId == runId);
                if (run is null || string.IsNullOrWhiteSpace(run.ApprovalOwner) || run.ReviewState != nameof(HandoffReviewState.Approving))
                    return false;
                owner = run.ApprovalOwner;
                return true;
            },
            TimeSpan.FromSeconds(5)).ConfigureAwait(true);
        return owner ?? throw new InvalidOperationException("Approval owner was not assigned.");
    }

    private IHandoffIngestionService CreateService(IHandoffOneShotExtractor extractor, ITodoService todo, HandoffLeaseOptions? lease = null)
    {
        var db = CreateDb();
        SeedRequirements(db);
        var ingestionOptions = MsOptions.Create(new IngestionOptions { RepoRoot = _workspace });
        var resolver = new TodoServiceResolver(todo, ingestionOptions, Substitute.For<ITodoServiceFactory>());
        var accessor = new WorkspaceServiceAccessor(resolver, Substitute.For<IHttpContextAccessor>(), ingestionOptions);
        return new HandoffIngestionService(
            new HandoffSourceResolver(db),
            extractor,
            new HandoffTodoDraftParser(),
            new HandoffTodoDraftValidator(),
            new HandoffModePolicy(),
            accessor,
            db,
            new SessionLogSanitizer(MsOptions.Create(new SessionLogSanitizationOptions { RegexTimeoutMilliseconds = 5000 })),
            _time,
            new TestDbFactory(_dbOptions, _workspace),
            lease is null ? null : MsOptions.Create(lease));
    }

    private McpDbContext CreateDb() => new(_dbOptions, new WorkspaceContext { WorkspacePath = _workspace });

    private void SeedRequirements(McpDbContext db)
    {
        if (db.Requirements.Any())
            return;
        db.Requirements.AddRange(
            new RequirementEntity { WorkspaceId = _workspace, Kind = "fr", Id = "FR-HANDOFF-001", Title = "FR", Body = "b", Priority = "high", Status = "pending" },
            new RequirementEntity { WorkspaceId = _workspace, Kind = "tr", Id = "TR-HANDOFF-CONTRACT-001", Title = "TR", Body = "b", Priority = "high", Status = "pending" });
        db.SaveChanges();
    }

    private HandoffIngestionRunEntity IncompleteRun(string runId, string content, string processingOwner)
        => new()
        {
            RunId = runId,
            WorkspaceId = _workspace,
            SourceKind = nameof(HandoffSourceKind.Content),
            SourceLocator = "content",
            ContentSha256 = Sha256(content),
            ExtractedAtUtc = _time.GetUtcNow(),
            PromptVersion = HandoffPromptDefaults.PromptVersion,
            TemplateVersion = HandoffPromptDefaults.TemplateId,
            Mode = nameof(HandoffIngestionMode.DraftOnly),
            ReviewState = nameof(HandoffReviewState.None),
            ReplayIdentity = HandoffReplayKeys.Create(_workspace, Sha256(content), HandoffPromptDefaults.PromptVersion, force: false, runId),
            Succeeded = false,
            ProcessingState = nameof(HandoffProcessingState.Processing),
            ProcessingOwner = processingOwner,
            ProcessingLeaseExpiresAtUtc = _time.GetUtcNow(),
        };

    private HandoffIngestionRequest ContentRequest(string content, HandoffIngestionMode mode = HandoffIngestionMode.DraftOnly)
        => new()
        {
            SourceKind = HandoffSourceKind.Content,
            Content = content,
            Mode = mode,
        };

    private static IHandoffOneShotExtractor SuccessfulExtractor(string json)
    {
        var extractor = Substitute.For<IHandoffOneShotExtractor>();
        extractor.ExtractAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(SuccessfulExtraction(json));
        return extractor;
    }

    private static HandoffExtractionResult SuccessfulExtraction(string json)
        => new()
        {
            Success = true,
            ResponseText = json,
            AgentName = "plan-agent",
            PromptVersion = HandoffPromptDefaults.PromptVersion,
            TemplateVersion = HandoffPromptDefaults.TemplateId,
            Model = "test-model",
        };

    private static string ValidDraft(string id, double confidence = 0.8)
        => $$"""{"id":"{{id}}","title":"Demo","section":"MCP Server","priority":"high","estimate":"2h","description":["Do the work"],"technicalDetails":["Use the service"],"implementationTasks":[{"task":"Write tests","done":false}],"dependsOn":[],"functionalRequirements":["FR-HANDOFF-001"],"technicalRequirements":["TR-HANDOFF-CONTRACT-001"],"confidence":{{confidence.ToString(System.Globalization.CultureInfo.InvariantCulture)}},"unknownSourceNotes":[]}""";

    private static string Sha256(string content)
        => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(content))).ToLowerInvariant();

    private static TrackingTodoService TrackingTodo() => new();

    private sealed class TrackingTodoService : ITodoService
    {
        private readonly object _sync = new();
        private readonly Dictionary<string, TodoFlatItem> _items = new(StringComparer.Ordinal);
        public int CreatedCount { get; private set; }
        public Action? OnCreated { get; set; }
        public bool HideFromGet { get; set; }
        public TaskCompletionSource? CreateGate { get; set; }
        public TimeSpan? GetDelay { get; set; }

        public void SeedExisting(string id, string? idempotencyKey)
        {
            _items[id] = new TodoFlatItem
            {
                Id = id,
                Title = "Existing",
                Section = "MCP Server",
                Priority = "high",
                Estimate = "2h",
                Description = ["Existing work"],
                TechnicalDetails = ["Existing details"],
                ImplementationTasks = [new TodoFlatTask("Existing task", false)],
                DependsOn = [],
                FunctionalRequirements = ["FR-HANDOFF-001"],
                TechnicalRequirements = ["TR-HANDOFF-CONTRACT-001"],
                Done = false,
                IdempotencyKey = idempotencyKey,
            };
        }

        public Task<TodoQueryResult> QueryAsync(TodoQueryRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new TodoQueryResult(_items.Values.ToArray(), _items.Count));

        public async Task<TodoFlatItem?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            if (CreatedCount > 0 && GetDelay is { } delay)
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            if (CreatedCount > 0 && HideFromGet)
                return null;
            return _items.TryGetValue(id, out var item) ? item : null;
        }

        public async Task<TodoMutationResult> CreateAsync(TodoCreateRequest request, CancellationToken cancellationToken = default)
        {
            if (CreateGate is not null)
                await CreateGate.Task.WaitAsync(cancellationToken).ConfigureAwait(false);

            TodoFlatItem item;
            lock (_sync)
            {
                if (_items.ContainsKey(request.Id))
                    return new TodoMutationResult(false, "exists", FailureKind: TodoMutationFailureKind.Conflict);

                CreatedCount++;
                item = new TodoFlatItem
                {
                    Id = request.Id,
                    Title = request.Title,
                    Section = request.Section,
                    Priority = request.Priority,
                    Estimate = request.Estimate,
                    Description = request.Description,
                    TechnicalDetails = request.TechnicalDetails,
                    ImplementationTasks = request.ImplementationTasks,
                    DependsOn = request.DependsOn,
                    FunctionalRequirements = request.FunctionalRequirements,
                    TechnicalRequirements = request.TechnicalRequirements,
                    Done = false,
                    IdempotencyKey = request.IdempotencyKey,
                };
                _items[request.Id] = item;
            }

            OnCreated?.Invoke();
            cancellationToken.ThrowIfCancellationRequested();
            return new TodoMutationResult(true, Item: item);
        }

        public Task<TodoMutationResult> UpdateAsync(string id, TodoUpdateRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new TodoMutationResult(false, "not used"));

        public Task<TodoMutationResult> DeleteAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult(new TodoMutationResult(false, "not used"));

        public Task<TodoAuditQueryResult> GetAuditAsync(string id, int limit = 50, int offset = 0, CancellationToken cancellationToken = default)
            => Task.FromResult(new TodoAuditQueryResult([], 0));

        public Task<TodoProjectionStatusResult> GetProjectionStatusAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new TodoProjectionStatusResult("database", "memory", "docs/todo.yaml", true, true, false, DateTime.UtcNow.ToString("O")));

        public Task<TodoProjectionRepairResult> RepairProjectionAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new TodoProjectionRepairResult(true, null, new TodoProjectionStatusResult("database", "memory", "docs/todo.yaml", true, true, false, DateTime.UtcNow.ToString("O"))));
    }

    private sealed class TestDbFactory : IDbContextFactory<McpDbContext>
    {
        private readonly DbContextOptions<McpDbContext> _options;
        private readonly string _workspace;
        public TestDbFactory(DbContextOptions<McpDbContext> options, string workspace)
        {
            _options = options;
            _workspace = workspace;
        }

        public McpDbContext CreateDbContext()
            => new(_options, new WorkspaceContext { WorkspacePath = _workspace });
    }

    private sealed class FailNextSaveInterceptor : SaveChangesInterceptor
    {
        public bool FailOnce { get; set; }
        public AgentDefinitionEntity? Unrelated { get; set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (FailOnce
                && eventData.Context is McpDbContext db
                && db.ChangeTracker.Entries<HandoffIngestionRunEntity>().Any(entry => !string.IsNullOrWhiteSpace(entry.Entity.CreatedTodoId)))
            {
                if (Unrelated is not null)
                    db.AgentDefinitions.Add(Unrelated);
                FailOnce = false;
                throw new DbUpdateException("simulated commit ambiguity", new TimeoutException());
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }

    private sealed class AdjustableTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow;
        public AdjustableTimeProvider(DateTimeOffset utcNow) => _utcNow = utcNow;
        public void Advance(TimeSpan value) => _utcNow += value;
        public override DateTimeOffset GetUtcNow() => _utcNow;
    }
}
