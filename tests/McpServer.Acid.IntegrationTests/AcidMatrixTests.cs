using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;

namespace McpServer.Acid.IntegrationTests;

/// <summary>
/// TEST-MCP-ACID-003: Key server <c>SignManifest</c> request outcomes - the success path and every
/// deterministically reachable rejection (unknown party, unknown key, replay nonce, stale sequence).
/// </summary>
[Trait("Category", "Integration")]
public sealed class KeyServerSignMatrixTests
{
    /// <summary>A registered publisher/subscriber pair signs successfully.</summary>
    [Fact]
    public async Task Sign_ValidRequest_Succeeds()
    {
        using var harness = AcidTransactionHarness.Create(AcidParticipants.AllMock);
        await harness.RegisterPartiesAsync().ConfigureAwait(true);

        var response = await harness.KeyServer.SignManifestAsync(
            AcidTransactionHarness.CreateSignRequest("txn-sign-ok", sequence: 1, nonce: "n-sign-ok"), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(response.Success);
        Assert.Equal(TransactionFailureReason.None, response.Reason);
        Assert.NotNull(response.Manifest);
    }

    /// <summary>Signing for an unregistered publisher is rejected as an unknown party.</summary>
    [Fact]
    public async Task Sign_UnregisteredPublisher_UnknownParty()
    {
        using var harness = AcidTransactionHarness.Create(AcidParticipants.AllMock);
        await harness.RegisterPartiesAsync().ConfigureAwait(true);
        var request = AcidTransactionHarness.CreateSignRequest("txn-sign-ghost", sequence: 1, nonce: "n-ghost");
        request.PublisherPartyId = "ghost-publisher";

        var response = await harness.KeyServer.SignManifestAsync(request, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(response.Success);
        Assert.Equal(TransactionFailureReason.UnknownParty, response.Reason);
    }

    /// <summary>Signing with a non-existent signing key id is rejected as an unknown key.</summary>
    [Fact]
    public async Task Sign_NonexistentSigningKey_UnknownKey()
    {
        using var harness = AcidTransactionHarness.Create(AcidParticipants.AllMock);
        await harness.RegisterPartiesAsync().ConfigureAwait(true);
        var request = AcidTransactionHarness.CreateSignRequest("txn-sign-badkey", sequence: 1, nonce: "n-badkey");
        request.PublisherSigningKeyId = "no-such-key";

        var response = await harness.KeyServer.SignManifestAsync(request, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(response.Success);
        Assert.Equal(TransactionFailureReason.UnknownKey, response.Reason);
    }

    /// <summary>A reused signing nonce for the same party pair is rejected as a replay.</summary>
    [Fact]
    public async Task Sign_ReusedNonce_ReplayNonce()
    {
        using var harness = AcidTransactionHarness.Create(AcidParticipants.AllMock);
        await harness.RegisterPartiesAsync().ConfigureAwait(true);
        await harness.SignManifestAsync("txn-nonce-1", sequence: 5, nonce: "dup-nonce").ConfigureAwait(true);

        var response = await harness.KeyServer.SignManifestAsync(
            AcidTransactionHarness.CreateSignRequest("txn-nonce-2", sequence: 6, nonce: "dup-nonce"), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(response.Success);
        Assert.Equal(TransactionFailureReason.ReplayNonce, response.Reason);
    }

    /// <summary>A non-monotonic signing sequence for the same party pair is rejected as stale.</summary>
    [Fact]
    public async Task Sign_DecreasingSequence_StaleSequence()
    {
        using var harness = AcidTransactionHarness.Create(AcidParticipants.AllMock);
        await harness.RegisterPartiesAsync().ConfigureAwait(true);
        await harness.SignManifestAsync("txn-seq-hi", sequence: 50, nonce: "n-seq-hi").ConfigureAwait(true);

        var response = await harness.KeyServer.SignManifestAsync(
            AcidTransactionHarness.CreateSignRequest("txn-seq-lo", sequence: 49, nonce: "n-seq-lo"), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(response.Success);
        Assert.Equal(TransactionFailureReason.StaleSequence, response.Reason);
    }
}

/// <summary>
/// TEST-MCP-ACID-004: Key server <c>VerifyManifest</c> request outcomes - valid verification plus the
/// rejections a relying party (subscriber) acts on: signature mismatch and wrong subscriber.
/// </summary>
[Trait("Category", "Integration")]
public sealed class KeyServerVerifyMatrixTests
{
    /// <summary>A genuine signed manifest verifies as valid for its intended subscriber.</summary>
    [Fact]
    public async Task Verify_ValidManifest_IsValid()
    {
        using var harness = AcidTransactionHarness.Create(AcidParticipants.AllMock);
        await harness.RegisterPartiesAsync().ConfigureAwait(true);
        var manifest = await harness.SignManifestAsync("txn-verify-ok", sequence: 1, nonce: "n-verify-ok").ConfigureAwait(true);

        var response = await harness.KeyServer.VerifyManifestAsync(new TransactionManifestVerifyRequest
        {
            Manifest = manifest,
            ExpectedSubscriberPartyId = AcidTransactionHarness.SubscriberPartyId,
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(response.IsValid);
        Assert.Equal(TransactionFailureReason.None, response.Reason);
    }

    /// <summary>A manifest mutated after signing fails signature verification.</summary>
    [Fact]
    public async Task Verify_TamperedManifest_SignatureMismatch()
    {
        using var harness = AcidTransactionHarness.Create(AcidParticipants.AllMock);
        await harness.RegisterPartiesAsync().ConfigureAwait(true);
        var manifest = await harness.SignManifestAsync("txn-verify-tampered", sequence: 1, nonce: "n-verify-tampered").ConfigureAwait(true);
        manifest.DiffgramSha256 = AcidTransactionHarness.Sha256Hex("tampered");

        var response = await harness.KeyServer.VerifyManifestAsync(new TransactionManifestVerifyRequest
        {
            Manifest = manifest,
            ExpectedSubscriberPartyId = AcidTransactionHarness.SubscriberPartyId,
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(response.IsValid);
        Assert.Equal(TransactionFailureReason.ManifestSignatureMismatch, response.Reason);
    }

    /// <summary>Verification for a different expected subscriber than the manifest names is rejected.</summary>
    [Fact]
    public async Task Verify_WrongExpectedSubscriber_WrongSubscriber()
    {
        using var harness = AcidTransactionHarness.Create(AcidParticipants.AllMock);
        await harness.RegisterPartiesAsync().ConfigureAwait(true);
        var manifest = await harness.SignManifestAsync("txn-verify-wrongsub", sequence: 1, nonce: "n-verify-wrongsub").ConfigureAwait(true);

        var response = await harness.KeyServer.VerifyManifestAsync(new TransactionManifestVerifyRequest
        {
            Manifest = manifest,
            ExpectedSubscriberPartyId = "someone-else",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(response.IsValid);
        Assert.Equal(TransactionFailureReason.WrongSubscriber, response.Reason);
    }
}

/// <summary>
/// TEST-MCP-ACID-005: Subscriber <c>CommitDiffgram</c> request outcomes - the subscriber validates the key
/// server's verification result and the diffgram evidence, covering commit, idempotent re-commit, and every
/// rejection (signature mismatch, encrypted-body mismatch, plaintext mismatch, stale sequence, wrong subscriber,
/// decrypt-required failure).
/// </summary>
[Trait("Category", "Integration")]
public sealed class SubscriberCommitMatrixTests
{
    /// <summary>A valid signed diffgram commits.</summary>
    [Fact]
    public async Task Commit_Valid_Committed()
    {
        using var harness = AcidTransactionHarness.Create(AcidParticipants.AllMock);
        await harness.RegisterPartiesAsync().ConfigureAwait(true);
        var manifest = await harness.SignManifestAsync("txn-c-ok", sequence: 1, nonce: "n-c-ok").ConfigureAwait(true);

        var commit = await harness.Subscriber.CommitDiffgramAsync(AcidTransactionHarness.CreateCommitRequest(manifest), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal("committed", commit.Status);
        Assert.Equal(TransactionFailureReason.None, commit.Reason);
    }

    /// <summary>Re-committing the same transaction is idempotent and still reports committed.</summary>
    [Fact]
    public async Task Commit_Duplicate_IsIdempotent()
    {
        using var harness = AcidTransactionHarness.Create(AcidParticipants.AllMock);
        await harness.RegisterPartiesAsync().ConfigureAwait(true);
        var manifest = await harness.SignManifestAsync("txn-c-dup", sequence: 1, nonce: "n-c-dup").ConfigureAwait(true);
        await harness.Subscriber.CommitDiffgramAsync(AcidTransactionHarness.CreateCommitRequest(manifest), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var second = await harness.Subscriber.CommitDiffgramAsync(AcidTransactionHarness.CreateCommitRequest(manifest), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal("committed", second.Status);
    }

    /// <summary>A tampered manifest is rejected at the subscriber via key-server verification.</summary>
    [Fact]
    public async Task Commit_TamperedManifest_SignatureMismatch()
    {
        using var harness = AcidTransactionHarness.Create(AcidParticipants.AllMock);
        await harness.RegisterPartiesAsync().ConfigureAwait(true);
        var manifest = await harness.SignManifestAsync("txn-c-tampered", sequence: 1, nonce: "n-c-tampered").ConfigureAwait(true);
        manifest.DiffgramSha256 = AcidTransactionHarness.Sha256Hex("tampered");

        var commit = await harness.Subscriber.CommitDiffgramAsync(AcidTransactionHarness.CreateCommitRequest(manifest), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal("rejected", commit.Status);
        Assert.Equal(TransactionFailureReason.ManifestSignatureMismatch, commit.Reason);
    }

    /// <summary>An encrypted-body hash that disagrees with the manifest is rejected.</summary>
    [Fact]
    public async Task Commit_EncryptedBodyMismatch_Rejected()
    {
        using var harness = AcidTransactionHarness.Create(AcidParticipants.AllMock);
        await harness.RegisterPartiesAsync().ConfigureAwait(true);
        var manifest = await harness.SignManifestAsync("txn-c-enc", sequence: 1, nonce: "n-c-enc").ConfigureAwait(true);
        var request = AcidTransactionHarness.CreateCommitRequest(manifest);
        request.EncryptedBodySha256 = AcidTransactionHarness.Sha256Hex("other-encrypted");

        var commit = await harness.Subscriber.CommitDiffgramAsync(request, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal("rejected", commit.Status);
        Assert.Equal(TransactionFailureReason.EncryptedBodyHashMismatch, commit.Reason);
    }

    /// <summary>A plaintext diffgram hash that disagrees with the manifest is rejected.</summary>
    [Fact]
    public async Task Commit_PlaintextMismatch_Rejected()
    {
        using var harness = AcidTransactionHarness.Create(AcidParticipants.AllMock);
        await harness.RegisterPartiesAsync().ConfigureAwait(true);
        var manifest = await harness.SignManifestAsync("txn-c-plain", sequence: 1, nonce: "n-c-plain").ConfigureAwait(true);
        var request = AcidTransactionHarness.CreateCommitRequest(manifest);
        request.DiffgramSha256 = AcidTransactionHarness.Sha256Hex("other-plaintext");

        var commit = await harness.Subscriber.CommitDiffgramAsync(request, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal("rejected", commit.Status);
        Assert.Equal(TransactionFailureReason.PlaintextDiffgramHashMismatch, commit.Reason);
    }

    /// <summary>A non-monotonic commit sequence for the party pair is rejected as stale.</summary>
    [Fact]
    public async Task Commit_StaleSequence_Rejected()
    {
        using var harness = AcidTransactionHarness.Create(AcidParticipants.AllMock);
        await harness.RegisterPartiesAsync().ConfigureAwait(true);
        var low = await harness.SignManifestAsync("txn-c-seq-low", sequence: 10, nonce: "n-c-seq-low").ConfigureAwait(true);
        var high = await harness.SignManifestAsync("txn-c-seq-high", sequence: 11, nonce: "n-c-seq-high").ConfigureAwait(true);
        await harness.Subscriber.CommitDiffgramAsync(AcidTransactionHarness.CreateCommitRequest(high), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var commit = await harness.Subscriber.CommitDiffgramAsync(AcidTransactionHarness.CreateCommitRequest(low), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal("rejected", commit.Status);
        Assert.Equal(TransactionFailureReason.StaleSequence, commit.Reason);
    }

    /// <summary>A subscriber whose configured party id differs from the manifest target rejects with wrong subscriber.</summary>
    [Fact]
    public async Task Commit_WrongConfiguredSubscriber_WrongSubscriber()
    {
        using var harness = AcidTransactionHarness.Create(AcidParticipants.AllMock);
        await harness.RegisterPartiesAsync().ConfigureAwait(true);
        var manifest = await harness.SignManifestAsync("txn-c-wrongsub", sequence: 1, nonce: "n-c-wrongsub").ConfigureAwait(true);
        using var misconfigured = new InMemorySubscriberCommitService(
            harness.KeyServer,
            new TransactionManifestCanonicalizer(),
            new FixedOptionsMonitor<SubscriberOptions>(new SubscriberOptions { PartyId = "different-subscriber" }),
            new TransactionDiffgramProtector());

        var commit = await misconfigured.CommitDiffgramAsync(AcidTransactionHarness.CreateCommitRequest(manifest), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal("rejected", commit.Status);
        Assert.Equal(TransactionFailureReason.WrongSubscriber, commit.Reason);
    }

    /// <summary>A subscriber that requires encrypted diffgrams rejects a plaintext body as a decrypt failure.</summary>
    [Fact]
    public async Task Commit_RequireEncryptedWithPlaintext_DecryptFailed()
    {
        using var harness = AcidTransactionHarness.Create(AcidParticipants.AllMock);
        await harness.RegisterPartiesAsync().ConfigureAwait(true);
        var manifest = await harness.SignManifestAsync("txn-c-decrypt", sequence: 1, nonce: "n-c-decrypt").ConfigureAwait(true);
        using var strict = new InMemorySubscriberCommitService(
            harness.KeyServer,
            new TransactionManifestCanonicalizer(),
            new FixedOptionsMonitor<SubscriberOptions>(new SubscriberOptions
            {
                PartyId = AcidTransactionHarness.SubscriberPartyId,
                RequireEncryptedDiffgrams = true,
            }),
            new TransactionDiffgramProtector());

        var commit = await strict.CommitDiffgramAsync(AcidTransactionHarness.CreateCommitRequest(manifest), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal("rejected", commit.Status);
        Assert.Equal(TransactionFailureReason.DecryptFailed, commit.Reason);
    }
}

/// <summary>
/// TEST-MCP-ACID-006: Coordinator (system under test) transaction outcomes for a mutating turn - committed,
/// bypassed (disabled / non-mutating), aborted (mutation failed), rejected when it validates a key-server sign
/// failure, rejected when it validates a subscriber commit rejection, and degraded when the subscriber is down.
/// </summary>
[Trait("Category", "Integration")]
public sealed class CoordinatorOutcomeMatrixTests
{
    /// <summary>A mutating turn with healthy collaborators commits.</summary>
    [Fact]
    public async Task Coordinator_HappyPath_Committed()
    {
        using var harness = AcidTransactionHarness.Create(AcidParticipants.AllMock);
        await harness.RegisterPartiesAsync().ConfigureAwait(true);

        var result = await harness.Coordinator.ExecuteAsync(
            Request("txn-co-ok", sequence: 1, mutating: true),
            _ => Task.FromResult(new TurnMutationResult { Success = true, ResultJson = "{}" }),
            CancellationToken.None).ConfigureAwait(true);

        Assert.Equal("committed", result.Status);
        Assert.True(result.MutationApplied);
    }

    /// <summary>When transactions are disabled the mutation runs and the turn is bypassed.</summary>
    [Fact]
    public async Task Coordinator_TransactionsDisabled_Bypassed()
    {
        using var harness = AcidTransactionHarness.Create(AcidParticipants.AllMock, enabled: false);
        await harness.RegisterPartiesAsync().ConfigureAwait(true);
        var applied = false;

        var result = await harness.Coordinator.ExecuteAsync(
            Request("txn-co-disabled", sequence: 1, mutating: true),
            _ => { applied = true; return Task.FromResult(new TurnMutationResult { Success = true }); },
            CancellationToken.None).ConfigureAwait(true);

        Assert.Equal("bypassed", result.Status);
        Assert.True(result.MutationApplied);
        Assert.True(applied);
    }

    /// <summary>A non-mutating (read-only) turn is bypassed without transaction gating.</summary>
    [Fact]
    public async Task Coordinator_NonMutating_Bypassed()
    {
        using var harness = AcidTransactionHarness.Create(AcidParticipants.AllMock);
        await harness.RegisterPartiesAsync().ConfigureAwait(true);

        var result = await harness.Coordinator.ExecuteAsync(
            Request("txn-co-readonly", sequence: 1, mutating: false),
            _ => Task.FromResult(new TurnMutationResult { Success = true }),
            CancellationToken.None).ConfigureAwait(true);

        Assert.Equal("bypassed", result.Status);
    }

    /// <summary>A failed mutation aborts and rolls back.</summary>
    [Fact]
    public async Task Coordinator_MutationFails_Aborted()
    {
        using var harness = AcidTransactionHarness.Create(AcidParticipants.AllMock);
        await harness.RegisterPartiesAsync().ConfigureAwait(true);

        var result = await harness.Coordinator.ExecuteAsync(
            Request("txn-co-abort", sequence: 1, mutating: true),
            _ => Task.FromResult(new TurnMutationResult { Success = false, Error = "boom" }),
            CancellationToken.None).ConfigureAwait(true);

        Assert.Equal("aborted", result.Status);
        Assert.Equal(TransactionFailureReason.Aborted, result.Reason);
        Assert.True(result.MutationApplied);
    }

    /// <summary>The coordinator validates the key server's sign result: a stale sequence fails closed before the mutation runs.</summary>
    [Fact]
    public async Task Coordinator_KeyServerRejectsSign_RejectedWithoutMutation()
    {
        using var harness = AcidTransactionHarness.Create(AcidParticipants.AllMock);
        await harness.RegisterPartiesAsync().ConfigureAwait(true);
        var mutationCount = 0;
        // First transaction advances the signing sequence for the pair to 10.
        await harness.Coordinator.ExecuteAsync(
            Request("txn-co-sign-1", sequence: 10, mutating: true),
            _ => { mutationCount++; return Task.FromResult(new TurnMutationResult { Success = true }); },
            CancellationToken.None).ConfigureAwait(true);

        // Fresh transaction with a lower sequence: the key server rejects the sign as stale.
        var result = await harness.Coordinator.ExecuteAsync(
            Request("txn-co-sign-2", sequence: 5, mutating: true),
            _ => { mutationCount++; return Task.FromResult(new TurnMutationResult { Success = true }); },
            CancellationToken.None).ConfigureAwait(true);

        Assert.Equal("rejected", result.Status);
        Assert.Equal(TransactionFailureReason.StaleSequence, result.Reason);
        Assert.False(result.MutationApplied);
        Assert.Equal(1, mutationCount); // only the first transaction ran its mutation
    }

    /// <summary>The coordinator validates the subscriber commit result: a non-degraded rejection rolls back and reports rejected.</summary>
    [Fact]
    public async Task Coordinator_SubscriberRejectsCommit_RejectedAndRolledBack()
    {
        using var harness = AcidTransactionHarness.Create(
            AcidParticipants.AllMock,
            subscriberOverride: new RejectingSubscriberCommitService(TransactionFailureReason.EncryptedBodyHashMismatch));
        await harness.RegisterPartiesAsync().ConfigureAwait(true);
        var rolledBack = false;

        var result = await harness.Coordinator.ExecuteAsync(
            Request("txn-co-commit-reject", sequence: 1, mutating: true),
            _ => Task.FromResult(new TurnMutationResult
            {
                Success = true,
                RollbackAsync = _ => { rolledBack = true; return Task.CompletedTask; },
            }),
            CancellationToken.None).ConfigureAwait(true);

        Assert.Equal("rejected", result.Status);
        Assert.Equal(TransactionFailureReason.EncryptedBodyHashMismatch, result.Reason);
        Assert.True(result.MutationApplied);
        Assert.True(result.RollbackAttempted);
        Assert.True(rolledBack);
    }

    /// <summary>An unavailable subscriber drives degraded mode and rolls back, fail-closed.</summary>
    [Fact]
    public async Task Coordinator_SubscriberUnavailable_Degraded()
    {
        using var harness = AcidTransactionHarness.Create(
            AcidParticipants.AllMock,
            subscriberOverride: new UnavailableSubscriberCommitService());
        await harness.RegisterPartiesAsync().ConfigureAwait(true);

        var result = await harness.Coordinator.ExecuteAsync(
            Request("txn-co-degraded", sequence: 1, mutating: true),
            _ => Task.FromResult(new TurnMutationResult
            {
                Success = true,
                RollbackAsync = _ => Task.CompletedTask,
            }),
            CancellationToken.None).ConfigureAwait(true);

        Assert.Equal("degraded", result.Status);
        Assert.True(result.Degraded);
        Assert.Equal(TransactionFailureReason.SubscriberUnavailable, result.Reason);
        Assert.True(result.RollbackSucceeded);
    }

    private static TurnTransactionRequest Request(string transactionId, long sequence, bool mutating)
        => new()
        {
            TransactionId = transactionId,
            TurnId = $"turn-{transactionId}",
            OperationName = "todo.update",
            OperationBodyJson = "{\"id\":\"PLAN-TURNTRANSACTIONS-001\"}",
            PublisherPartyId = AcidTransactionHarness.PublisherPartyId,
            SubscriberPartyId = AcidTransactionHarness.SubscriberPartyId,
            Sequence = sequence,
            Mutating = mutating,
        };
}
