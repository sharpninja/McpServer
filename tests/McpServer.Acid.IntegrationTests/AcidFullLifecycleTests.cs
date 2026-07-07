using McpServer.TransactionSecurity.Models;

namespace McpServer.Acid.IntegrationTests;

/// <summary>
/// TEST-MCP-ACID-001: Baseline ACID turn-transaction lifecycle integration tests. The coordinator is the real
/// system under test; the harness mocks the MCP Server's interactions by driving it, and both collaborators
/// (third-party key server, subscriber) are mocked in-process. Exercises every published message in the
/// transaction and asserts the expected outcome for the happy path and each failure case.
/// </summary>
[Trait("Category", "Integration")]
public sealed class AcidFullLifecycleTests
{
    /// <summary>Happy path: coordinator signs, applies the mutation, and the subscriber commits the diffgram.</summary>
    [Fact]
    public async Task FullLifecycle_HappyPath_Commits()
    {
        using var harness = AcidTransactionHarness.Create(AcidParticipants.AllMock);
        await harness.RegisterPartiesAsync().ConfigureAwait(true);

        var result = await harness.Coordinator.ExecuteAsync(
            NewRequest("txn-happy", sequence: 1),
            _ => Task.FromResult(new TurnMutationResult { Success = true, ResultJson = "{\"updated\":true}" }),
            CancellationToken.None).ConfigureAwait(true);

        Assert.Equal("committed", result.Status);
        Assert.True(result.MutationApplied);
        Assert.False(result.Degraded);
        Assert.False(string.IsNullOrWhiteSpace(result.DiffgramId));
        Assert.Contains(harness.Audit.Snapshot(), e => e.EventName == "transaction_manifest_signed");
        Assert.Contains(harness.Audit.Snapshot(), e => e.EventName == "diffgram_committed");
    }

    /// <summary>A failed mutation aborts the transaction and invokes rollback compensation before any commit.</summary>
    [Fact]
    public async Task MutationFailure_AbortsAndRollsBack()
    {
        using var harness = AcidTransactionHarness.Create(AcidParticipants.AllMock);
        await harness.RegisterPartiesAsync().ConfigureAwait(true);
        var rolledBack = false;

        var result = await harness.Coordinator.ExecuteAsync(
            NewRequest("txn-mutation-fail", sequence: 2),
            _ => Task.FromResult(new TurnMutationResult
            {
                Success = false,
                Error = "mutation boom",
                RollbackAsync = _ => { rolledBack = true; return Task.CompletedTask; },
            }),
            CancellationToken.None).ConfigureAwait(true);

        Assert.Equal("aborted", result.Status);
        Assert.Equal(TransactionFailureReason.Aborted, result.Reason);
        Assert.True(result.MutationApplied);
        Assert.True(result.RollbackAttempted);
        Assert.True(rolledBack);
        Assert.Contains(harness.Audit.Snapshot(), e => e.EventName == "transaction_aborted");
    }

    /// <summary>An unavailable subscriber drives degraded mode and rolls the mutation back, fail-closed.</summary>
    [Fact]
    public async Task SubscriberUnavailable_DegradesAndRollsBack()
    {
        using var harness = AcidTransactionHarness.Create(
            AcidParticipants.AllMock,
            subscriberOverride: new UnavailableSubscriberCommitService());
        await harness.RegisterPartiesAsync().ConfigureAwait(true);
        var rolledBack = false;

        var result = await harness.Coordinator.ExecuteAsync(
            NewRequest("txn-degraded", sequence: 3),
            _ => Task.FromResult(new TurnMutationResult
            {
                Success = true,
                ResultJson = "{\"updated\":true}",
                RollbackAsync = _ => { rolledBack = true; return Task.CompletedTask; },
            }),
            CancellationToken.None).ConfigureAwait(true);

        Assert.Equal("degraded", result.Status);
        Assert.True(result.Degraded);
        Assert.Equal(TransactionFailureReason.SubscriberUnavailable, result.Reason);
        Assert.True(result.RollbackAttempted);
        Assert.True(result.RollbackSucceeded);
        Assert.True(rolledBack);
    }

    /// <summary>Message-level: a valid signed diffgram commits at the subscriber and reports committed status.</summary>
    [Fact]
    public async Task Subscriber_ValidCommit_CommitsAndReportsStatus()
    {
        using var harness = AcidTransactionHarness.Create(AcidParticipants.AllMock);
        await harness.RegisterPartiesAsync().ConfigureAwait(true);
        var manifest = await harness.SignManifestAsync("txn-commit", sequence: 10, nonce: "nonce-commit").ConfigureAwait(true);

        var commit = await harness.Subscriber.CommitDiffgramAsync(AcidTransactionHarness.CreateCommitRequest(manifest), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var status = await harness.Subscriber.GetTransactionStatusAsync("txn-commit", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal("committed", commit.Status);
        Assert.Equal(TransactionFailureReason.None, commit.Reason);
        Assert.NotNull(status);
        Assert.Equal("committed", status!.Status);
    }

    /// <summary>Message-level: a tampered manifest fails key-server signature verification at the subscriber.</summary>
    [Fact]
    public async Task Subscriber_TamperedManifest_RejectsSignatureMismatch()
    {
        using var harness = AcidTransactionHarness.Create(AcidParticipants.AllMock);
        await harness.RegisterPartiesAsync().ConfigureAwait(true);
        var manifest = await harness.SignManifestAsync("txn-tampered", sequence: 11, nonce: "nonce-tampered").ConfigureAwait(true);
        manifest.DiffgramSha256 = AcidTransactionHarness.Sha256Hex("tampered");

        var commit = await harness.Subscriber.CommitDiffgramAsync(AcidTransactionHarness.CreateCommitRequest(manifest), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal("rejected", commit.Status);
        Assert.Equal(TransactionFailureReason.ManifestSignatureMismatch, commit.Reason);
    }

    /// <summary>Message-level: an encrypted-body hash that disagrees with the manifest is rejected.</summary>
    [Fact]
    public async Task Subscriber_EncryptedBodyMismatch_Rejected()
    {
        using var harness = AcidTransactionHarness.Create(AcidParticipants.AllMock);
        await harness.RegisterPartiesAsync().ConfigureAwait(true);
        var manifest = await harness.SignManifestAsync("txn-enc-mismatch", sequence: 12, nonce: "nonce-enc-mismatch").ConfigureAwait(true);
        var request = AcidTransactionHarness.CreateCommitRequest(manifest);
        request.EncryptedBodySha256 = AcidTransactionHarness.Sha256Hex("different-encrypted-body");

        var commit = await harness.Subscriber.CommitDiffgramAsync(request, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal("rejected", commit.Status);
        Assert.Equal(TransactionFailureReason.EncryptedBodyHashMismatch, commit.Reason);
    }

    /// <summary>Message-level: a plaintext diffgram hash that disagrees with the manifest is rejected.</summary>
    [Fact]
    public async Task Subscriber_PlaintextMismatch_Rejected()
    {
        using var harness = AcidTransactionHarness.Create(AcidParticipants.AllMock);
        await harness.RegisterPartiesAsync().ConfigureAwait(true);
        var manifest = await harness.SignManifestAsync("txn-plain-mismatch", sequence: 13, nonce: "nonce-plain-mismatch").ConfigureAwait(true);
        var request = AcidTransactionHarness.CreateCommitRequest(manifest);
        request.DiffgramSha256 = AcidTransactionHarness.Sha256Hex("different-plaintext");

        var commit = await harness.Subscriber.CommitDiffgramAsync(request, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal("rejected", commit.Status);
        Assert.Equal(TransactionFailureReason.PlaintextDiffgramHashMismatch, commit.Reason);
    }

    /// <summary>Message-level: a non-monotonic sequence is rejected after a prior commit for the same party pair.</summary>
    [Fact]
    public async Task Subscriber_StaleSequence_Rejected()
    {
        using var harness = AcidTransactionHarness.Create(AcidParticipants.AllMock);
        await harness.RegisterPartiesAsync().ConfigureAwait(true);
        // Sign in monotonic order (the key server enforces increasing sequence at sign time),
        // then commit in reverse order so the lower sequence trips the subscriber's stale guard.
        var lower = await harness.SignManifestAsync("txn-seq-low", sequence: 20, nonce: "nonce-seq-low").ConfigureAwait(true);
        var higher = await harness.SignManifestAsync("txn-seq-high", sequence: 21, nonce: "nonce-seq-high").ConfigureAwait(true);
        await harness.Subscriber.CommitDiffgramAsync(AcidTransactionHarness.CreateCommitRequest(higher), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var commit = await harness.Subscriber.CommitDiffgramAsync(AcidTransactionHarness.CreateCommitRequest(lower), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal("rejected", commit.Status);
        Assert.Equal(TransactionFailureReason.StaleSequence, commit.Reason);
    }

    /// <summary>Message-level: the key server rejects a reused signing nonce.</summary>
    [Fact]
    public async Task KeyServer_ReplayNonceOnSign_Rejected()
    {
        using var harness = AcidTransactionHarness.Create(AcidParticipants.AllMock);
        await harness.RegisterPartiesAsync().ConfigureAwait(true);
        await harness.SignManifestAsync("txn-replay-1", sequence: 30, nonce: "nonce-replay").ConfigureAwait(true);

        var response = await harness.KeyServer.SignManifestAsync(
            AcidTransactionHarness.CreateSignRequest("txn-replay-2", sequence: 31, nonce: "nonce-replay"), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(response.Success);
        Assert.Equal(TransactionFailureReason.ReplayNonce, response.Reason);
    }

    /// <summary>Message-level: aborting a transaction reports aborted status from the subscriber.</summary>
    [Fact]
    public async Task Subscriber_Abort_ReportsAborted()
    {
        using var harness = AcidTransactionHarness.Create(AcidParticipants.AllMock);
        await harness.RegisterPartiesAsync().ConfigureAwait(true);

        var abort = await harness.Subscriber.AbortTransactionAsync(
            "txn-abort",
            new TransactionAbortRequest { Reason = TransactionFailureReason.Aborted, Actor = "test" }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal("aborted", abort.Status);
        Assert.Equal(TransactionFailureReason.Aborted, abort.Reason);
    }

    private static TurnTransactionRequest NewRequest(string transactionId, long sequence)
        => new()
        {
            TransactionId = transactionId,
            TurnId = $"turn-{transactionId}",
            OperationName = "todo.update",
            OperationBodyJson = "{\"id\":\"PLAN-TURNTRANSACTIONS-001\"}",
            PublisherPartyId = AcidTransactionHarness.PublisherPartyId,
            SubscriberPartyId = AcidTransactionHarness.SubscriberPartyId,
            Sequence = sequence,
            Mutating = true,
        };
}
