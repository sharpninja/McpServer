using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace McpServer.TransactionSecurity.IntegrationTests;

/// <summary>
/// TEST-MCP-158 and TEST-MCP-159: Durable transaction-security storage coverage derived from SD-KEYSERVER-001 and SD-DIFFGRAM-001.
/// </summary>
[Trait("Category", "Integration")]
public sealed class DurableTransactionSecurityStorageTests
{
    private const string PublisherPartyId = "publisher-1";
    private const string SubscriberPartyId = "subscriber-1";
    private const string ExternalPublisherSigningKeyId = "publisher-1:signing:external";
    private const string RotatedPublisherSigningKeyId = "publisher-1:signing:rotated";

    /// <summary>Keyserver public descriptors and audit rows survive service recreation.</summary>
    [Fact]
    public async Task KeyServerSqliteStore_VerifiesManifestAfterServiceRecreation()
    {
        using var workspace = TempWorkspace.Create();
        var databasePath = workspace.GetPath("keyserver.db");
        TransactionManifestDto manifest;

        using (var keyServer = CreateKeyServer(databasePath))
        {
            await RegisterStandardPartiesAsync(keyServer).ConfigureAwait(true);
            manifest = await SignManifestAsync(keyServer, "txn-keyserver-durable-verify", 100, "nonce-keyserver-durable")
                .ConfigureAwait(true);
        }

        using var recreatedKeyServer = CreateKeyServer(databasePath);
        var descriptor = await recreatedKeyServer
            .GetPartyKeyAsync(PublisherPartyId, $"{PublisherPartyId}:signing:1")
            .ConfigureAwait(true);
        var verify = await recreatedKeyServer.VerifyManifestAsync(
            new TransactionManifestVerifyRequest
            {
                Manifest = manifest,
                ExpectedSubscriberPartyId = SubscriberPartyId,
            }).ConfigureAwait(true);
        var audit = await ReadAuditEventsAsync(databasePath).ConfigureAwait(true);

        Assert.NotNull(descriptor);
        Assert.Equal(PublisherPartyId, descriptor.PartyId);
        Assert.True(verify.IsValid);
        Assert.Equal(TransactionFailureReason.None, verify.Reason);
        Assert.Contains(audit, entry => entry.EventName == "keyserver.party.registered");
        Assert.Contains(audit, entry => entry.EventName == "keyserver.manifest.signed");
        Assert.Contains(audit, entry => entry.EventName == "keyserver.manifest.verified");
    }

    /// <summary>Signed manifest trace records survive keyserver service recreation without private key material.</summary>
    [Fact]
    public async Task KeyServerSqliteStore_PersistsSignedManifestTraceAcrossServiceRecreation()
    {
        using var workspace = TempWorkspace.Create();
        var databasePath = workspace.GetPath("keyserver-manifest-trace.db");
        TransactionManifestDto manifest;

        using (var keyServer = CreateKeyServer(databasePath))
        {
            await RegisterStandardPartiesAsync(keyServer).ConfigureAwait(true);
            manifest = await SignManifestAsync(keyServer, "txn-keyserver-manifest-trace", 800, "nonce-keyserver-manifest-trace")
                .ConfigureAwait(true);
        }

        using var recreatedKeyServer = CreateKeyServer(databasePath);
        var trace = await recreatedKeyServer.GetManifestAsync("txn-keyserver-manifest-trace").ConfigureAwait(true);
        var traceJson = JsonSerializer.Serialize(trace);

        Assert.NotNull(trace);
        Assert.Equal(manifest.TransactionId, trace.TransactionId);
        Assert.Equal(manifest.TurnId, trace.TurnId);
        Assert.Equal(manifest.PublisherPartyId, trace.PublisherPartyId);
        Assert.Equal(manifest.SubscriberPartyId, trace.SubscriberPartyId);
        Assert.Equal(manifest.PublisherSigningKeyId, trace.PublisherSigningKeyId);
        Assert.Equal(manifest.SubscriberEncryptionKeyId, trace.SubscriberEncryptionKeyId);
        Assert.Equal(manifest.Sequence, trace.Sequence);
        Assert.Equal(manifest.Nonce, trace.Nonce);
        Assert.Equal(manifest.IssuedAtUtc, trace.IssuedAtUtc);
        Assert.Equal(manifest.ExpiresAtUtc, trace.ExpiresAtUtc);
        Assert.Equal(manifest.DiffgramSha256, trace.DiffgramSha256);
        Assert.Equal(manifest.EncryptedBodySha256, trace.EncryptedBodySha256);
        Assert.Equal(manifest.Signature!.Algorithm, trace.SignatureAlgorithm);
        Assert.Equal(manifest.Signature.KeyId, trace.SignatureKeyId);
        Assert.Equal(manifest.Signature.Value, trace.SignatureValue);
        Assert.Equal(manifest.Signature.SignedAtUtc, trace.SignedAtUtc);
        Assert.Equal("signed", trace.Status);
        Assert.False(string.IsNullOrWhiteSpace(trace.ManifestHashSha256));
        Assert.DoesNotContain("PRIVATE KEY", traceJson, StringComparison.Ordinal);
    }

    /// <summary>Signed manifest trace reports query durable ledger records without private key material.</summary>
    [Fact]
    public async Task KeyServerSqliteStore_ReportsSignedManifestTraceAfterServiceRecreation()
    {
        using var workspace = TempWorkspace.Create();
        var databasePath = workspace.GetPath("keyserver-manifest-trace-report.db");

        using (var keyServer = CreateKeyServer(databasePath))
        {
            await RegisterStandardPartiesAsync(keyServer).ConfigureAwait(true);
            await SignManifestAsync(keyServer, "txn-keyserver-trace-report-1", 810, "nonce-keyserver-trace-report-1")
                .ConfigureAwait(true);
            await SignManifestAsync(keyServer, "txn-keyserver-trace-report-2", 811, "nonce-keyserver-trace-report-2")
                .ConfigureAwait(true);
        }

        using var recreatedKeyServer = CreateKeyServer(databasePath);
        var report = await recreatedKeyServer.GetManifestReportAsync(
            new TransactionManifestTraceReportRequest
            {
                PublisherPartyId = PublisherPartyId,
                SubscriberPartyId = SubscriberPartyId,
                Status = "signed",
                Limit = 1,
            }).ConfigureAwait(true);
        var reportJson = JsonSerializer.Serialize(report);

        var trace = Assert.Single(report.Records);
        Assert.Equal(PublisherPartyId, report.PublisherPartyId);
        Assert.Equal(SubscriberPartyId, report.SubscriberPartyId);
        Assert.Equal("signed", report.Status);
        Assert.Equal(1, report.Limit);
        Assert.Equal(2, report.TotalCount);
        Assert.Equal(1, report.ReturnedCount);
        Assert.Equal("txn-keyserver-trace-report-1", trace.TransactionId);
        Assert.Equal("signed", trace.Status);
        Assert.False(string.IsNullOrWhiteSpace(trace.ManifestHashSha256));
        Assert.DoesNotContain("PRIVATE KEY", reportJson, StringComparison.Ordinal);
    }

    /// <summary>Externally supplied signing private material can be re-provisioned after service recreation.</summary>
    [Fact]
    public async Task KeyServerSqliteStore_ReprovisionsExternalSigningPrivateKeyAfterServiceRecreation()
    {
        using var workspace = TempWorkspace.Create();
        using var signingKey = SigningKeyPair.Create();
        var databasePath = workspace.GetPath("keyserver-external-signing.db");
        TransactionManifestDto firstManifest;

        using (var keyServer = CreateKeyServer(databasePath))
        {
            await RegisterExternalSigningPublisherAsync(keyServer, signingKey).ConfigureAwait(true);
            await RegisterSubscriberAsync(keyServer).ConfigureAwait(true);
            firstManifest = await SignManifestAsync(
                keyServer,
                "txn-external-signing-first",
                500,
                "nonce-external-signing-first",
                ExternalPublisherSigningKeyId).ConfigureAwait(true);
        }

        using (var recreatedWithoutMaterial = CreateKeyServer(databasePath))
        {
            var descriptor = await recreatedWithoutMaterial.GetPartyKeyAsync(PublisherPartyId, ExternalPublisherSigningKeyId)
                .ConfigureAwait(true);
            var missingPrivateMaterial = await recreatedWithoutMaterial.SignManifestAsync(
                new TransactionManifestSignRequest
                {
                    TransactionId = "txn-external-signing-missing-private",
                    TurnId = "turn-external-key-material",
                    PublisherPartyId = PublisherPartyId,
                    PublisherSigningKeyId = ExternalPublisherSigningKeyId,
                    SubscriberPartyId = SubscriberPartyId,
                    Sequence = 501,
                    Nonce = "nonce-external-signing-missing-private",
                    DiffgramSha256 = Sha256Hex("plain-diffgram"),
                    EncryptedBodySha256 = Sha256Hex("encrypted-diffgram"),
                }).ConfigureAwait(true);
            var verifyExisting = await recreatedWithoutMaterial.VerifyManifestAsync(
                new TransactionManifestVerifyRequest
                {
                    Manifest = firstManifest,
                    ExpectedSubscriberPartyId = SubscriberPartyId,
                }).ConfigureAwait(true);

            Assert.NotNull(descriptor);
            Assert.Equal(signingKey.PublicKeyPem, descriptor.PublicKeyPem);
            Assert.False(missingPrivateMaterial.Success);
            Assert.Equal(TransactionFailureReason.UnknownKey, missingPrivateMaterial.Reason);
            Assert.True(verifyExisting.IsValid);
        }

        using var recreatedWithMaterial = CreateKeyServer(databasePath);
        var registration = await RegisterExternalSigningPublisherAsync(recreatedWithMaterial, signingKey).ConfigureAwait(true);
        var secondManifest = await SignManifestAsync(
            recreatedWithMaterial,
            "txn-external-signing-second",
            501,
            "nonce-external-signing-second",
            ExternalPublisherSigningKeyId).ConfigureAwait(true);
        var publicDescriptor = await recreatedWithMaterial.GetPartyKeyAsync(PublisherPartyId, ExternalPublisherSigningKeyId)
            .ConfigureAwait(true);
        var registrationJson = JsonSerializer.Serialize(registration);

        Assert.Equal(ExternalPublisherSigningKeyId, secondManifest.PublisherSigningKeyId);
        Assert.NotNull(publicDescriptor);
        Assert.Equal(signingKey.PublicKeyPem, publicDescriptor.PublicKeyPem);
        Assert.DoesNotContain("PRIVATE KEY", registrationJson, StringComparison.Ordinal);
    }

    /// <summary>Registration rejects mismatched public/private signing material instead of publishing a false descriptor.</summary>
    [Fact]
    public async Task RegisterParty_WithConflictingExternalSigningPublicKey_Throws()
    {
        using var privateMaterial = SigningKeyPair.Create();
        using var conflictingPublicMaterial = SigningKeyPair.Create();
        using var keyServer = CreateKeyServer();

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => keyServer.RegisterPartyAsync(
                new PartyRegistrationRequest
                {
                    PartyId = PublisherPartyId,
                    Role = "publisher",
                    ActiveSigningKeyId = ExternalPublisherSigningKeyId,
                    SigningPrivateKeyPem = privateMaterial.PrivateKeyPem,
                    SigningPublicKeyPem = conflictingPublicMaterial.PublicKeyPem,
                })).ConfigureAwait(true);

        Assert.Contains("does not match", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>In-memory keyserver rotation preserves old public descriptors for historic manifest verification.</summary>
    [Fact]
    public async Task KeyServerMemoryStore_PreservesRotatedSigningPublicKeysForVerification()
    {
        using var keyServer = CreateKeyServer();

        await AssertSigningKeyRotationPreservesVerificationAsync(keyServer).ConfigureAwait(true);
    }

    /// <summary>SQLite keyserver rotation preserves old public descriptors for historic manifest verification.</summary>
    [Fact]
    public async Task KeyServerSqliteStore_PreservesRotatedSigningPublicKeysForVerification()
    {
        using var workspace = TempWorkspace.Create();
        using var keyServer = CreateKeyServer(workspace.GetPath("keyserver-rotation.db"));

        await AssertSigningKeyRotationPreservesVerificationAsync(keyServer).ConfigureAwait(true);
    }

    /// <summary>Keyserver replay nonce and sequence cursors survive SQLite store recreation.</summary>
    [Fact]
    public async Task KeyServerSqliteStore_PersistsReplayStateAcrossStoreRecreation()
    {
        using var workspace = TempWorkspace.Create();
        var databasePath = workspace.GetPath("keyserver-replay.db");
        const string pairKey = $"{PublisherPartyId}\n{SubscriberPartyId}";
        const string nonceKey = $"{pairKey}\nnonce-keyserver-durable-replay";

        using (var store = new SqliteTransactionSecurityStateStore(databasePath))
        {
            var first = await store.TryReserveManifestReplayAsync(
                "sign",
                pairKey,
                sequence: 300,
                nonceKey,
                transactionId: "txn-keyserver-replay-first",
                CancellationToken.None).ConfigureAwait(true);

            Assert.Equal(TransactionFailureReason.None, first);
        }

        using var recreatedStore = new SqliteTransactionSecurityStateStore(databasePath);
        var stale = await recreatedStore.TryReserveManifestReplayAsync(
            "sign",
            pairKey,
            sequence: 299,
            $"{pairKey}\nnonce-keyserver-durable-stale",
            transactionId: "txn-keyserver-replay-stale",
            CancellationToken.None).ConfigureAwait(true);
        var replay = await recreatedStore.TryReserveManifestReplayAsync(
            "sign",
            pairKey,
            sequence: 301,
            nonceKey,
            transactionId: "txn-keyserver-replay-duplicate",
            CancellationToken.None).ConfigureAwait(true);
        var next = await recreatedStore.TryReserveManifestReplayAsync(
            "sign",
            pairKey,
            sequence: 301,
            $"{pairKey}\nnonce-keyserver-durable-next",
            transactionId: "txn-keyserver-replay-next",
            CancellationToken.None).ConfigureAwait(true);

        Assert.Equal(TransactionFailureReason.StaleSequence, stale);
        Assert.Equal(TransactionFailureReason.ReplayNonce, replay);
        Assert.Equal(TransactionFailureReason.None, next);
    }

    /// <summary>Keyserver verification replay nonce and sequence cursors use a durable scope separate from signing.</summary>
    [Fact]
    public async Task KeyServerSqliteStore_PersistsVerificationReplayStateAcrossStoreRecreation()
    {
        using var workspace = TempWorkspace.Create();
        var databasePath = workspace.GetPath("keyserver-verify-replay.db");
        const string pairKey = $"{PublisherPartyId}\n{SubscriberPartyId}";
        const string nonceKey = $"{pairKey}\nnonce-keyserver-verify-replay";

        using (var store = new SqliteTransactionSecurityStateStore(databasePath))
        {
            var first = await store.TryReserveManifestReplayAsync(
                "verify",
                pairKey,
                sequence: 400,
                nonceKey,
                transactionId: "txn-keyserver-verify-first",
                CancellationToken.None).ConfigureAwait(true);

            Assert.Equal(TransactionFailureReason.None, first);
        }

        using var recreatedStore = new SqliteTransactionSecurityStateStore(databasePath);
        var stale = await recreatedStore.TryReserveManifestReplayAsync(
            "verify",
            pairKey,
            sequence: 399,
            $"{pairKey}\nnonce-keyserver-verify-stale",
            transactionId: "txn-keyserver-verify-stale",
            CancellationToken.None).ConfigureAwait(true);
        var replay = await recreatedStore.TryReserveManifestReplayAsync(
            "verify",
            pairKey,
            sequence: 401,
            nonceKey,
            transactionId: "txn-keyserver-verify-duplicate",
            CancellationToken.None).ConfigureAwait(true);
        var signingScope = await recreatedStore.TryReserveManifestReplayAsync(
            "sign",
            pairKey,
            sequence: 400,
            nonceKey,
            transactionId: "txn-keyserver-sign-separate",
            CancellationToken.None).ConfigureAwait(true);

        Assert.Equal(TransactionFailureReason.StaleSequence, stale);
        Assert.Equal(TransactionFailureReason.ReplayNonce, replay);
        Assert.Equal(TransactionFailureReason.None, signingScope);
    }

    /// <summary>Subscriber commits, duplicate idempotency, sequence cursors, and audit rows survive service recreation.</summary>
    [Fact]
    public async Task SubscriberSqliteStore_PersistsCommitStatusAndReplayStateAcrossServiceRecreation()
    {
        using var workspace = TempWorkspace.Create();
        var databasePath = workspace.GetPath("subscriber.db");
        using var keyServer = CreateKeyServer();
        await RegisterStandardPartiesAsync(keyServer).ConfigureAwait(true);
        var staleManifest = await SignManifestAsync(keyServer, "txn-subscriber-durable-stale", 200, "nonce-subscriber-durable-1")
            .ConfigureAwait(true);
        var firstManifest = await SignManifestAsync(keyServer, "txn-subscriber-durable-commit", 201, "nonce-subscriber-durable-2")
            .ConfigureAwait(true);
        var firstRequest = CreateCommitRequest(firstManifest);

        using (var subscriber = CreateSubscriber(keyServer, databasePath))
        {
            var commit = await subscriber.CommitDiffgramAsync(firstRequest).ConfigureAwait(true);

            Assert.Equal("committed", commit.Status);
            Assert.Equal(TransactionFailureReason.None, commit.Reason);
        }

        using var recreatedSubscriber = CreateSubscriber(keyServer, databasePath);
        var status = await recreatedSubscriber.GetTransactionStatusAsync(firstManifest.TransactionId).ConfigureAwait(true);
        var duplicate = await recreatedSubscriber.CommitDiffgramAsync(firstRequest).ConfigureAwait(true);
        var stale = await recreatedSubscriber.CommitDiffgramAsync(CreateCommitRequest(staleManifest)).ConfigureAwait(true);
        var staleStatus = await recreatedSubscriber.GetTransactionStatusAsync(staleManifest.TransactionId).ConfigureAwait(true);
        var audit = await ReadAuditEventsAsync(databasePath).ConfigureAwait(true);

        Assert.NotNull(status);
        Assert.Equal("committed", status.Status);
        Assert.Equal("committed", duplicate.Status);
        Assert.Equal(TransactionFailureReason.None, duplicate.Reason);
        Assert.Equal("rejected", stale.Status);
        Assert.Equal(TransactionFailureReason.StaleSequence, stale.Reason);
        Assert.NotNull(staleStatus);
        Assert.Equal("rejected", staleStatus.Status);
        Assert.Equal(TransactionFailureReason.StaleSequence, staleStatus.Reason);
        Assert.Contains(audit, entry => entry.EventName == "subscriber.transaction.committed");
        Assert.Contains(audit, entry => entry.EventName == "subscriber.transaction.duplicate");
        Assert.Contains(audit, entry => entry.EventName == "subscriber.transaction.rejected");
    }

    /// <summary>Subscriber SQLite storage exposes pending status while commit verification is in flight.</summary>
    [Fact]
    public async Task SubscriberSqliteStore_ExposesPendingStatusDuringInFlightCommit()
    {
        using var workspace = TempWorkspace.Create();
        var databasePath = workspace.GetPath("subscriber-pending.db");
        using var keyServer = CreateKeyServer();
        await RegisterStandardPartiesAsync(keyServer).ConfigureAwait(true);
        var manifest = await SignManifestAsync(keyServer, "txn-subscriber-durable-pending", 210, "nonce-subscriber-durable-pending")
            .ConfigureAwait(true);
        var verifier = new BlockingManifestService(keyServer);
        using var subscriber = CreateSubscriber(verifier, databasePath);

        var commitTask = subscriber.CommitDiffgramAsync(CreateCommitRequest(manifest));
        await verifier.WaitForVerifyAsync().ConfigureAwait(true);

        using var observer = CreateSubscriber(keyServer, databasePath);
        var pending = await observer.GetTransactionStatusAsync(manifest.TransactionId).ConfigureAwait(true);

        Assert.NotNull(pending);
        Assert.Equal("pending", pending.Status);
        Assert.Equal(TransactionFailureReason.None, pending.Reason);
        Assert.Null(pending.CommittedAtUtc);

        verifier.ReleaseVerification();
        var commit = await commitTask.ConfigureAwait(true);
        var committed = await observer.GetTransactionStatusAsync(manifest.TransactionId).ConfigureAwait(true);

        Assert.Equal("committed", commit.Status);
        Assert.NotNull(committed);
        Assert.Equal("committed", committed.Status);
        Assert.NotNull(committed.CommittedAtUtc);
    }

    /// <summary>High-contention duplicate commits never create conflicts for the same payload and settle to committed.</summary>
    [Fact]
    public async Task SubscriberSqliteStore_HighContentionDuplicateCommit_SettlesToSingleCommittedTransaction()
    {
        using var workspace = TempWorkspace.Create();
        var databasePath = workspace.GetPath("subscriber-duplicate-contention.db");
        using var keyServer = CreateKeyServer();
        await RegisterStandardPartiesAsync(keyServer).ConfigureAwait(true);
        var manifest = await SignManifestAsync(
                keyServer,
                "txn-subscriber-contention-duplicate",
                220,
                "nonce-subscriber-contention-duplicate")
            .ConfigureAwait(true);
        var request = CreateCommitRequest(manifest);
        using var subscriber = CreateSubscriber(keyServer, databasePath);

        var responses = await Task.WhenAll(
                Enumerable.Range(0, 32).Select(_ => subscriber.CommitDiffgramAsync(request)))
            .ConfigureAwait(true);
        var finalDuplicate = await subscriber.CommitDiffgramAsync(request).ConfigureAwait(true);
        var status = await subscriber.GetTransactionStatusAsync(manifest.TransactionId).ConfigureAwait(true);
        var audit = await ReadAuditEventsAsync(databasePath).ConfigureAwait(true);

        Assert.All(responses, response =>
            Assert.Contains(response.Status, ["committed", "pending"], StringComparer.OrdinalIgnoreCase));
        Assert.DoesNotContain(responses, response => response.Reason == TransactionFailureReason.DuplicateConflict);
        Assert.Equal("committed", finalDuplicate.Status);
        Assert.NotNull(status);
        Assert.Equal("committed", status.Status);
        Assert.Contains(audit, entry => entry.EventName == "subscriber.transaction.committed");
        Assert.Contains(audit, entry => entry.EventName == "subscriber.transaction.duplicate" || entry.EventName == "subscriber.transaction.pending");
    }

    /// <summary>An abort racing an in-flight commit wins cleanly and the released commit reports an aborted reason.</summary>
    [Fact]
    public async Task SubscriberSqliteStore_AbortDuringInFlightCommit_PreservesAbortAndRejectsReleasedCommit()
    {
        using var workspace = TempWorkspace.Create();
        var databasePath = workspace.GetPath("subscriber-abort-race.db");
        using var keyServer = CreateKeyServer();
        await RegisterStandardPartiesAsync(keyServer).ConfigureAwait(true);
        var manifest = await SignManifestAsync(
                keyServer,
                "txn-subscriber-abort-race",
                230,
                "nonce-subscriber-abort-race")
            .ConfigureAwait(true);
        var verifier = new BlockingManifestService(keyServer);
        using var subscriber = CreateSubscriber(verifier, databasePath);

        var commitTask = subscriber.CommitDiffgramAsync(CreateCommitRequest(manifest));
        await verifier.WaitForVerifyAsync().ConfigureAwait(true);
        var abort = await subscriber.AbortTransactionAsync(
                manifest.TransactionId,
                new TransactionAbortRequest
                {
                    Reason = TransactionFailureReason.Aborted,
                    Actor = "abort-race-test",
                })
            .ConfigureAwait(true);

        verifier.ReleaseVerification();
        var commit = await commitTask.ConfigureAwait(true);
        var status = await subscriber.GetTransactionStatusAsync(manifest.TransactionId).ConfigureAwait(true);
        var audit = await ReadAuditEventsAsync(databasePath).ConfigureAwait(true);

        Assert.Equal("aborted", abort.Status);
        Assert.Equal(TransactionFailureReason.Aborted, abort.Reason);
        Assert.Equal("rejected", commit.Status);
        Assert.Equal(TransactionFailureReason.Aborted, commit.Reason);
        Assert.NotNull(status);
        Assert.Equal("aborted", status.Status);
        Assert.Contains(audit, entry => entry.EventName == "subscriber.transaction.aborted");
        Assert.Contains(audit, entry => entry.EventName == "subscriber.transaction.commit_rejected");
    }

    /// <summary>Durable pub-sub keeps unavailable commit handoffs replayable across store recreation.</summary>
    [Fact]
    public async Task PubSubSqliteStore_PersistsPendingCommitAndReplaysAfterSubscriberRecovery()
    {
        using var workspace = TempWorkspace.Create();
        var databasePath = workspace.GetPath("pubsub-commit-replay.db");
        var request = CreateCommitRequest("txn-pubsub-durable-replay");

        using (var store = new SqliteTransactionSecurityStateStore(databasePath))
        using (var pubSub = new DurableTransactionPubSub(
                   new ScriptedTransactionPubSub(SubscriberUnavailableCommit(request.Manifest.TransactionId)),
                   store))
        {
            var unavailable = await pubSub.PublishCommitAsync(request).ConfigureAwait(true);
            var pending = await pubSub.GetPendingMessagesAsync().ConfigureAwait(true);

            Assert.Equal("rejected", unavailable.Status);
            Assert.Equal(TransactionFailureReason.SubscriberUnavailable, unavailable.Reason);
            var pendingMessage = Assert.Single(pending);
            Assert.Equal("commit", pendingMessage.Kind);
            Assert.Equal("pending", pendingMessage.Status);
        }

        var recoveredInner = new ScriptedTransactionPubSub(Committed(request.Manifest.TransactionId));
        using var replayStore = new SqliteTransactionSecurityStateStore(databasePath);
        using var replayPubSub = new DurableTransactionPubSub(recoveredInner, replayStore);
        var result = await replayPubSub.ReplayPendingAsync().ConfigureAwait(true);
        var afterReplay = await replayPubSub.GetPendingMessagesAsync().ConfigureAwait(true);

        Assert.Equal(1, result.AttemptedCount);
        Assert.Equal(1, result.AcknowledgedCount);
        Assert.Equal(0, result.PendingCount);
        Assert.Empty(afterReplay);
        var replayedRequest = Assert.Single(recoveredInner.CommitRequests);
        Assert.Equal(request.Manifest.TransactionId, replayedRequest.Manifest.TransactionId);
    }

    /// <summary>Durable pub-sub persists topic and subscriber identity for pending messages across store recreation.</summary>
    [Fact]
    public async Task PubSubSqliteStore_PersistsTopicAndSubscriberIdForPendingMessagesAcrossRecreation()
    {
        using var workspace = TempWorkspace.Create();
        var databasePath = workspace.GetPath("pubsub-topic-subscriber.db");
        var request = CreateCommitRequest("txn-pubsub-topic-subscriber");

        using (var store = new SqliteTransactionSecurityStateStore(databasePath))
        using (var pubSub = new DurableTransactionPubSub(
                   new ScriptedTransactionPubSub(SubscriberUnavailableCommit(request.Manifest.TransactionId)),
                   store,
                   topicName: "topic.commit",
                   subscriberId: "subscriber-a"))
        {
            var unavailable = await pubSub.PublishCommitAsync(request).ConfigureAwait(true);

            Assert.Equal(TransactionFailureReason.SubscriberUnavailable, unavailable.Reason);
        }

        using var replayStore = new SqliteTransactionSecurityStateStore(databasePath);
        using var replayPubSub = new DurableTransactionPubSub(
            new ScriptedTransactionPubSub(),
            replayStore,
            topicName: "topic.commit",
            subscriberId: "subscriber-a");
        var pending = await replayPubSub.GetPendingMessagesAsync().ConfigureAwait(true);

        var status = Assert.Single(pending);
        Assert.Equal("topic.commit", status.TopicName);
        Assert.Equal("subscriber-a", status.SubscriberId);
        Assert.Equal("topic.commit:subscriber-a:commit:txn-pubsub-topic-subscriber", status.OperationId);
    }

    /// <summary>Durable pub-sub retention purges expired terminal messages but keeps replayable in-progress work.</summary>
    [Fact]
    public async Task PubSubSqliteStore_PrunesExpiredTerminalMessagesButKeepsPendingAndFreshInProgress()
    {
        using var workspace = TempWorkspace.Create();
        var databasePath = workspace.GetPath("pubsub-retention.db");
        var acknowledged = CreateCommitRequest("txn-pubsub-retention-ack");
        var pending = CreateCommitRequest("txn-pubsub-retention-pending");

        using (var store = new SqliteTransactionSecurityStateStore(databasePath))
        using (var pubSub = new DurableTransactionPubSub(
                   new ScriptedTransactionPubSub(
                       Committed(acknowledged.Manifest.TransactionId),
                       SubscriberUnavailableCommit(pending.Manifest.TransactionId)),
                   store))
        {
            var committed = await pubSub.PublishCommitAsync(acknowledged).ConfigureAwait(true);
            var unavailable = await pubSub.PublishCommitAsync(pending).ConfigureAwait(true);
            var claimed = await store.TryClaimPendingAsync(
                    $"commit:{pending.Manifest.TransactionId}",
                    DateTimeOffset.MaxValue,
                    CancellationToken.None)
                .ConfigureAwait(true);
            var retention = await pubSub.PurgeCompletedAsync(DateTimeOffset.UtcNow.AddSeconds(1), 10)
                .ConfigureAwait(true);

            Assert.Equal(TransactionFailureReason.None, committed.Reason);
            Assert.Equal(TransactionFailureReason.SubscriberUnavailable, unavailable.Reason);
            Assert.NotNull(claimed);
            Assert.Equal(1, retention.PurgedCount);
        }

        var recoveredInner = new ScriptedTransactionPubSub(Committed(pending.Manifest.TransactionId));
        using var replayStore = new SqliteTransactionSecurityStateStore(databasePath);
        using var replayPubSub = new DurableTransactionPubSub(
            recoveredInner,
            replayStore,
            TimeSpan.Zero);
        var replay = await replayPubSub.ReplayPendingAsync().ConfigureAwait(true);

        Assert.Equal(1, replay.AttemptedCount);
        Assert.Equal(1, replay.AcknowledgedCount);
        var replayedRequest = Assert.Single(recoveredInner.CommitRequests);
        Assert.Equal(pending.Manifest.TransactionId, replayedRequest.Manifest.TransactionId);
    }

    /// <summary>A rolled-back coordinator mutation cancels the durable pending commit so recovery replay cannot commit it later.</summary>
    [Fact]
    public async Task TurnCoordinator_WithDurablePubSub_WhenSubscriberUnavailableAfterRollback_DoesNotReplayRolledBackCommit()
    {
        using var workspace = TempWorkspace.Create();
        var databasePath = workspace.GetPath("pubsub-rollback-cancel.db");
        const string TransactionId = "txn-pubsub-rollback-cancel";
        using var keyServer = CreateKeyServer();
        await RegisterStandardPartiesAsync(keyServer).ConfigureAwait(true);
        var unavailableInner = new ScriptedTransactionPubSub(SubscriberUnavailableCommit(TransactionId));
        var options = new TurnTransactionOptions
        {
            Enabled = true,
            DegradedModeEnabled = true,
            PublisherPartyId = PublisherPartyId,
            SubscriberPartyId = SubscriberPartyId,
        };
        var audit = new InMemoryTransactionAuditWriter();
        var mutationState = new List<string>();

        using (var store = new SqliteTransactionSecurityStateStore(databasePath))
        using (var pubSub = new DurableTransactionPubSub(unavailableInner, store))
        {
            var coordinator = new TurnTransactionCoordinator(
                new FixedOptionsMonitor<TurnTransactionOptions>(options),
                keyServer,
                keyServer,
                pubSub,
                new JsonDiffgramBuilder(),
                new TransactionDegradedModePolicy(new FixedOptionsMonitor<TurnTransactionOptions>(options)),
                audit);

            var result = await coordinator.ExecuteAsync(
                new TurnTransactionRequest
                {
                    TransactionId = TransactionId,
                    TurnId = "turn-pubsub-rollback-cancel",
                    OperationName = "todo.update",
                    OperationBodyJson = "{\"id\":\"PLAN-TURNTRANSACTIONS-001\"}",
                    PublisherPartyId = PublisherPartyId,
                    SubscriberPartyId = SubscriberPartyId,
                    Sequence = 930,
                    Mutating = true,
                },
                _ =>
                {
                    mutationState.Add("applied");
                    return Task.FromResult(new TurnMutationResult
                    {
                        Success = true,
                        RollbackAsync = ct =>
                        {
                            mutationState.Clear();
                            return Task.CompletedTask;
                        },
                    });
                },
                CancellationToken.None).ConfigureAwait(true);
            var pendingAfterRollback = await pubSub.GetPendingMessagesAsync().ConfigureAwait(true);

            Assert.Equal("degraded", result.Status);
            Assert.True(result.RollbackSucceeded);
            Assert.Empty(mutationState);
            Assert.Empty(pendingAfterRollback);
            Assert.Single(unavailableInner.CommitRequests);
        }

        var recoveredInner = new ScriptedTransactionPubSub(Committed(TransactionId));
        using var replayStore = new SqliteTransactionSecurityStateStore(databasePath);
        using var replayPubSub = new DurableTransactionPubSub(recoveredInner, replayStore);
        var replay = await replayPubSub.ReplayPendingAsync().ConfigureAwait(true);
        var auditEvents = audit.Snapshot();

        Assert.Equal(0, replay.AttemptedCount);
        Assert.Empty(recoveredInner.CommitRequests);
        Assert.Contains(auditEvents, entry => entry.EventName == "transaction_rollback_completed");
        Assert.Contains(auditEvents, entry => entry.EventName == "transaction_pending_commit_canceled");
    }

    /// <summary>A timed-out durable commit is canceled after rollback so replay cannot later commit compensated state.</summary>
    [Fact]
    public async Task TurnCoordinator_WithDurablePubSub_WhenSubscriberCommitTimesOutAndRollbackSucceeds_CancelsPendingCommit()
    {
        using var workspace = TempWorkspace.Create();
        var databasePath = workspace.GetPath("pubsub-timeout-rollback-cancel.db");
        const string TransactionId = "txn-pubsub-timeout-rollback-cancel";
        using var keyServer = CreateKeyServer();
        await RegisterStandardPartiesAsync(keyServer).ConfigureAwait(true);
        var timeoutInner = new TimeoutTransactionPubSub();
        var options = new TurnTransactionOptions
        {
            Enabled = true,
            DegradedModeEnabled = true,
            CommitTimeoutSeconds = 1,
            PublisherPartyId = PublisherPartyId,
            SubscriberPartyId = SubscriberPartyId,
        };
        var audit = new InMemoryTransactionAuditWriter();
        var mutationState = new List<string>();

        using (var store = new SqliteTransactionSecurityStateStore(databasePath))
        using (var pubSub = new DurableTransactionPubSub(timeoutInner, store))
        {
            var coordinator = new TurnTransactionCoordinator(
                new FixedOptionsMonitor<TurnTransactionOptions>(options),
                keyServer,
                keyServer,
                pubSub,
                new JsonDiffgramBuilder(),
                new TransactionDegradedModePolicy(new FixedOptionsMonitor<TurnTransactionOptions>(options)),
                audit);

            var result = await coordinator.ExecuteAsync(
                new TurnTransactionRequest
                {
                    TransactionId = TransactionId,
                    TurnId = "turn-pubsub-timeout-rollback-cancel",
                    OperationName = "todo.update",
                    OperationBodyJson = "{\"id\":\"PLAN-TURNTRANSACTIONS-001\"}",
                    PublisherPartyId = PublisherPartyId,
                    SubscriberPartyId = SubscriberPartyId,
                    Sequence = 940,
                    Mutating = true,
                },
                _ =>
                {
                    mutationState.Add("applied");
                    return Task.FromResult(new TurnMutationResult
                    {
                        Success = true,
                        RollbackAsync = ct =>
                        {
                            mutationState.Clear();
                            return Task.CompletedTask;
                        },
                    });
                },
                CancellationToken.None).ConfigureAwait(true);
            var pendingAfterRollback = await pubSub.GetPendingMessagesAsync().ConfigureAwait(true);

            Assert.Equal("degraded", result.Status);
            Assert.Equal(TransactionFailureReason.CommitTimeout, result.Reason);
            Assert.True(result.RollbackSucceeded);
            Assert.Empty(mutationState);
            Assert.Empty(pendingAfterRollback);
            Assert.Single(timeoutInner.CommitRequests);
        }

        var recoveredInner = new ScriptedTransactionPubSub(Committed(TransactionId));
        using var replayStore = new SqliteTransactionSecurityStateStore(databasePath);
        using var replayPubSub = new DurableTransactionPubSub(recoveredInner, replayStore);
        var replay = await replayPubSub.ReplayPendingAsync().ConfigureAwait(true);
        var auditEvents = audit.Snapshot();

        Assert.Equal(0, replay.AttemptedCount);
        Assert.Empty(recoveredInner.CommitRequests);
        Assert.Contains(auditEvents, entry => entry.EventName == "transaction_rollback_completed");
        Assert.Contains(auditEvents, entry => entry.EventName == "transaction_pending_commit_canceled");
    }

    /// <summary>Concurrent durable pub-sub replayers claim a pending commit once and do not double-deliver.</summary>
    [Fact]
    public async Task PubSubSqliteStore_ConcurrentReplayPendingCommit_DeliversOnceAndLeavesNoPending()
    {
        using var workspace = TempWorkspace.Create();
        var databasePath = workspace.GetPath("pubsub-concurrent-replay.db");
        var request = CreateCommitRequest("txn-pubsub-concurrent-replay");

        using (var store = new SqliteTransactionSecurityStateStore(databasePath))
        using (var pubSub = new DurableTransactionPubSub(
                   new ScriptedTransactionPubSub(SubscriberUnavailableCommit(request.Manifest.TransactionId)),
                   store))
        {
            var unavailable = await pubSub.PublishCommitAsync(request).ConfigureAwait(true);

            Assert.Equal(TransactionFailureReason.SubscriberUnavailable, unavailable.Reason);
        }

        var recoveredInner = new ScriptedTransactionPubSub(Committed(request.Manifest.TransactionId));
        using var replayStore = new SqliteTransactionSecurityStateStore(databasePath);
        using var replayPubSub = new DurableTransactionPubSub(recoveredInner, replayStore);

        var results = await Task.WhenAll(
                Enumerable.Range(0, 4).Select(_ => replayPubSub.ReplayPendingAsync()))
            .ConfigureAwait(true);
        var afterReplay = await replayPubSub.GetPendingMessagesAsync().ConfigureAwait(true);

        Assert.Equal(1, results.Sum(result => result.AttemptedCount));
        Assert.Equal(1, results.Sum(result => result.AcknowledgedCount));
        Assert.Equal(0, results.Sum(result => result.PendingCount));
        Assert.Empty(afterReplay);
        var replayedRequest = Assert.Single(recoveredInner.CommitRequests);
        Assert.Equal(request.Manifest.TransactionId, replayedRequest.Manifest.TransactionId);
    }

    /// <summary>Concurrent durable replay workers divide a backlog and deliver every pending message once.</summary>
    [Fact]
    public async Task PubSubSqliteStore_ConcurrentReplayBacklog_DeliversEachPendingMessageOnceAndDrainsQueue()
    {
        using var workspace = TempWorkspace.Create();
        var databasePath = workspace.GetPath("pubsub-concurrent-replay-backlog.db");
        const int MessageCount = 48;
        const int WorkerCount = 8;
        var requests = Enumerable.Range(0, MessageCount)
            .Select(index => CreateCommitRequest($"txn-pubsub-replay-backlog-{index:D2}"))
            .ToArray();

        using (var store = new SqliteTransactionSecurityStateStore(databasePath))
        using (var pubSub = new DurableTransactionPubSub(
                   new ScriptedTransactionPubSub(
                       requests.Select(request => SubscriberUnavailableCommit(request.Manifest.TransactionId)).ToArray()),
                   store))
        {
            foreach (var request in requests)
            {
                var unavailable = await pubSub.PublishCommitAsync(request).ConfigureAwait(true);

                Assert.Equal(TransactionFailureReason.SubscriberUnavailable, unavailable.Reason);
            }

            var pending = await pubSub.GetPendingMessagesAsync(MessageCount).ConfigureAwait(true);
            Assert.Equal(MessageCount, pending.Count);
        }

        var recoveredInner = new ScriptedTransactionPubSub();
        var replayWorkers = Enumerable.Range(0, WorkerCount)
            .Select(_ => new DurableTransactionPubSub(
                recoveredInner,
                new SqliteTransactionSecurityStateStore(databasePath)))
            .ToArray();
        try
        {
            var results = await Task.WhenAll(
                    replayWorkers.Select(worker => worker.ReplayPendingAsync(maxMessages: 8)))
                .ConfigureAwait(true);
            using var verificationStore = new SqliteTransactionSecurityStateStore(databasePath);
            using var verificationPubSub = new DurableTransactionPubSub(recoveredInner, verificationStore);
            var afterReplay = await verificationPubSub.GetPendingMessagesAsync(MessageCount).ConfigureAwait(true);

            Assert.Equal(MessageCount, results.Sum(result => result.AttemptedCount));
            Assert.Equal(MessageCount, results.Sum(result => result.AcknowledgedCount));
            Assert.Equal(0, results.Sum(result => result.PendingCount));
            Assert.Empty(afterReplay);
            Assert.Equal(MessageCount, recoveredInner.CommitRequests.Count);
            Assert.Equal(
                requests.Select(request => request.Manifest.TransactionId).OrderBy(value => value, StringComparer.Ordinal),
                recoveredInner.CommitRequests.Select(request => request.Manifest.TransactionId).OrderBy(value => value, StringComparer.Ordinal));
        }
        finally
        {
            foreach (var worker in replayWorkers)
                worker.Dispose();
        }
    }

    /// <summary>High-volume durable pub-sub commit acknowledgements settle without pending replay work.</summary>
    [Fact]
    public async Task PubSubSqliteStore_HighVolumeConcurrentDistinctCommits_AcknowledgesAllAndLeavesNoPending()
    {
        using var workspace = TempWorkspace.Create();
        var databasePath = workspace.GetPath("pubsub-high-volume-distinct.db");
        var requests = Enumerable.Range(0, 64)
            .Select(index => CreateCommitRequest($"txn-pubsub-high-volume-{index:D2}"))
            .ToArray();
        var inner = new ScriptedTransactionPubSub();
        using var store = new SqliteTransactionSecurityStateStore(databasePath);
        using var pubSub = new DurableTransactionPubSub(inner, store);

        var responses = await Task.WhenAll(requests.Select(request => pubSub.PublishCommitAsync(request)))
            .ConfigureAwait(true);
        var pending = await pubSub.GetPendingMessagesAsync().ConfigureAwait(true);
        var replay = await pubSub.ReplayPendingAsync().ConfigureAwait(true);

        Assert.All(responses, response =>
        {
            Assert.Equal("committed", response.Status);
            Assert.Equal(TransactionFailureReason.None, response.Reason);
        });
        Assert.Equal(64, responses.Select(response => response.TransactionId).Distinct(StringComparer.Ordinal).Count());
        Assert.Empty(pending);
        Assert.Equal(0, replay.AttemptedCount);
        Assert.Equal(64, inner.CommitRequests.Count);
        Assert.Equal(
            requests.Select(request => request.Manifest.TransactionId).OrderBy(value => value, StringComparer.Ordinal),
            inner.CommitRequests.Select(request => request.Manifest.TransactionId).OrderBy(value => value, StringComparer.Ordinal));
    }

    /// <summary>High-contention duplicate durable commits keep one pending message with complete attempt accounting.</summary>
    [Fact]
    public async Task PubSubSqliteStore_HighContentionDuplicatePendingCommit_RecordsSinglePendingMessageAndReplaysOriginalPayload()
    {
        using var workspace = TempWorkspace.Create();
        var databasePath = workspace.GetPath("pubsub-high-contention-duplicate.db");
        const int WorkerCount = 32;
        const string TransactionId = "txn-pubsub-high-contention-duplicate";
        var request = CreateCommitRequest(TransactionId);
        var unavailableResponses = Enumerable.Range(0, WorkerCount)
            .Select(_ => SubscriberUnavailableCommit(TransactionId))
            .ToArray();
        var unavailableInner = new ScriptedTransactionPubSub(unavailableResponses);

        using (var store = new SqliteTransactionSecurityStateStore(databasePath))
        using (var pubSub = new DurableTransactionPubSub(unavailableInner, store))
        {
            var responses = await Task.WhenAll(
                    Enumerable.Range(0, WorkerCount).Select(_ => pubSub.PublishCommitAsync(request)))
                .ConfigureAwait(true);
            var pending = await pubSub.GetPendingMessagesAsync().ConfigureAwait(true);

            Assert.All(responses, response =>
            {
                Assert.Equal("rejected", response.Status);
                Assert.Equal(TransactionFailureReason.SubscriberUnavailable, response.Reason);
            });
            Assert.DoesNotContain(responses, response => response.Reason == TransactionFailureReason.DuplicateConflict);
            Assert.Equal(WorkerCount, unavailableInner.CommitRequests.Count);
            var pendingMessage = Assert.Single(pending);
            Assert.Equal("pending", pendingMessage.Status);
            Assert.Equal(WorkerCount, pendingMessage.AttemptCount);
        }

        var recoveredInner = new ScriptedTransactionPubSub(Committed(TransactionId));
        using var replayStore = new SqliteTransactionSecurityStateStore(databasePath);
        using var replayPubSub = new DurableTransactionPubSub(recoveredInner, replayStore);
        var replay = await replayPubSub.ReplayPendingAsync().ConfigureAwait(true);
        var afterReplay = await replayPubSub.GetPendingMessagesAsync().ConfigureAwait(true);

        Assert.Equal(1, replay.AttemptedCount);
        Assert.Equal(1, replay.AcknowledgedCount);
        Assert.Equal(0, replay.PendingCount);
        Assert.Empty(afterReplay);
        var replayedRequest = Assert.Single(recoveredInner.CommitRequests);
        Assert.Equal(request.EncryptedBodySha256, replayedRequest.EncryptedBodySha256);
        Assert.Equal(request.DiffgramSha256, replayedRequest.DiffgramSha256);
    }

    /// <summary>Stale in-progress durable pub-sub claims are reclaimable so crashed replayers do not wedge commits forever.</summary>
    [Fact]
    public async Task PubSubSqliteStore_ReclaimsStaleInProgressCommitAndReplaysAfterLeaseExpiry()
    {
        using var workspace = TempWorkspace.Create();
        var databasePath = workspace.GetPath("pubsub-stale-inprogress-replay.db");
        var request = CreateCommitRequest("txn-pubsub-stale-inprogress-replay");

        using (var store = new SqliteTransactionSecurityStateStore(databasePath))
        using (var pubSub = new DurableTransactionPubSub(
                   new ScriptedTransactionPubSub(SubscriberUnavailableCommit(request.Manifest.TransactionId)),
                   store))
        {
            var unavailable = await pubSub.PublishCommitAsync(request).ConfigureAwait(true);
            var claimed = await store.TryClaimPendingAsync(
                    $"commit:{request.Manifest.TransactionId}",
                    DateTimeOffset.MaxValue,
                    CancellationToken.None)
                .ConfigureAwait(true);

            Assert.Equal(TransactionFailureReason.SubscriberUnavailable, unavailable.Reason);
            Assert.NotNull(claimed);
            Assert.Equal("in_progress", claimed.Status);
        }

        var recoveredInner = new ScriptedTransactionPubSub(Committed(request.Manifest.TransactionId));
        using var replayStore = new SqliteTransactionSecurityStateStore(databasePath);
        using var replayPubSub = new DurableTransactionPubSub(
            recoveredInner,
            replayStore,
            TimeSpan.Zero);
        var result = await replayPubSub.ReplayPendingAsync().ConfigureAwait(true);
        var afterReplay = await replayPubSub.GetPendingMessagesAsync().ConfigureAwait(true);

        Assert.Equal(1, result.AttemptedCount);
        Assert.Equal(1, result.AcknowledgedCount);
        Assert.Equal(0, result.PendingCount);
        Assert.Empty(afterReplay);
        var replayedRequest = Assert.Single(recoveredInner.CommitRequests);
        Assert.Equal(request.Manifest.TransactionId, replayedRequest.Manifest.TransactionId);
    }

    /// <summary>Fresh in-progress durable pub-sub claims are not reclaimed before the configured lease expires.</summary>
    [Fact]
    public async Task PubSubSqliteStore_DoesNotReclaimFreshInProgressCommitBeforeLeaseExpiry()
    {
        using var workspace = TempWorkspace.Create();
        var databasePath = workspace.GetPath("pubsub-fresh-inprogress-replay.db");
        var request = CreateCommitRequest("txn-pubsub-fresh-inprogress-replay");

        using (var store = new SqliteTransactionSecurityStateStore(databasePath))
        using (var pubSub = new DurableTransactionPubSub(
                   new ScriptedTransactionPubSub(SubscriberUnavailableCommit(request.Manifest.TransactionId)),
                   store))
        {
            var unavailable = await pubSub.PublishCommitAsync(request).ConfigureAwait(true);
            var claimed = await store.TryClaimPendingAsync(
                    $"commit:{request.Manifest.TransactionId}",
                    DateTimeOffset.MaxValue,
                    CancellationToken.None)
                .ConfigureAwait(true);

            Assert.Equal(TransactionFailureReason.SubscriberUnavailable, unavailable.Reason);
            Assert.NotNull(claimed);
            Assert.Equal("in_progress", claimed.Status);
        }

        var recoveredInner = new ScriptedTransactionPubSub(Committed(request.Manifest.TransactionId));
        using var replayStore = new SqliteTransactionSecurityStateStore(databasePath);
        using var replayPubSub = new DurableTransactionPubSub(
            recoveredInner,
            replayStore,
            TimeSpan.FromHours(1));
        var pending = await replayPubSub.GetPendingMessagesAsync().ConfigureAwait(true);
        var result = await replayPubSub.ReplayPendingAsync().ConfigureAwait(true);

        Assert.Empty(pending);
        Assert.Equal(0, result.AttemptedCount);
        Assert.Empty(recoveredInner.CommitRequests);
    }

    /// <summary>DI-configured zero-second durable pub-sub leases reclaim fresh in-progress claims immediately.</summary>
    [Fact]
    public async Task AddInProcessTransactionSecurity_WhenDurablePubSubLeaseZeroConfigured_ReclaimsFreshInProgressMessage()
    {
        using var workspace = TempWorkspace.Create();
        var databasePath = workspace.GetPath("pubsub-di-zero-lease.db");
        var request = CreateCommitRequest("txn-pubsub-di-zero-lease");
        var subscriber = new ScriptedSubscriberCommitService(
            SubscriberUnavailableCommit(request.Manifest.TransactionId),
            Committed(request.Manifest.TransactionId));
        using var provider = BuildDurablePubSubProvider(databasePath, 0, subscriber);
        var pubSub = provider.GetRequiredService<ITransactionPubSub>();
        var replay = provider.GetRequiredService<ITransactionPubSubReplayService>();
        var store = provider.GetRequiredService<ITransactionPubSubBrokerStore>();

        var unavailable = await pubSub.PublishCommitAsync(request).ConfigureAwait(true);
        var claimed = await store.TryClaimPendingAsync(
                $"commit:{request.Manifest.TransactionId}",
                DateTimeOffset.MaxValue,
                CancellationToken.None)
            .ConfigureAwait(true);
        var result = await replay.ReplayPendingAsync().ConfigureAwait(true);

        Assert.Equal(TransactionFailureReason.SubscriberUnavailable, unavailable.Reason);
        Assert.NotNull(claimed);
        Assert.Equal(1, result.AttemptedCount);
        Assert.Equal(1, result.AcknowledgedCount);
        Assert.Equal(2, subscriber.CommitRequests.Count);
    }

    /// <summary>DI-configured positive durable pub-sub leases do not reclaim fresh in-progress claims early.</summary>
    [Fact]
    public async Task AddInProcessTransactionSecurity_WhenDurablePubSubLeasePositiveConfigured_DoesNotReclaimFreshInProgressMessage()
    {
        using var workspace = TempWorkspace.Create();
        var databasePath = workspace.GetPath("pubsub-di-positive-lease.db");
        var request = CreateCommitRequest("txn-pubsub-di-positive-lease");
        var subscriber = new ScriptedSubscriberCommitService(
            SubscriberUnavailableCommit(request.Manifest.TransactionId),
            Committed(request.Manifest.TransactionId));
        using var provider = BuildDurablePubSubProvider(databasePath, 3600, subscriber);
        var pubSub = provider.GetRequiredService<ITransactionPubSub>();
        var replay = provider.GetRequiredService<ITransactionPubSubReplayService>();
        var store = provider.GetRequiredService<ITransactionPubSubBrokerStore>();

        var unavailable = await pubSub.PublishCommitAsync(request).ConfigureAwait(true);
        var claimed = await store.TryClaimPendingAsync(
                $"commit:{request.Manifest.TransactionId}",
                DateTimeOffset.MaxValue,
                CancellationToken.None)
            .ConfigureAwait(true);
        var result = await replay.ReplayPendingAsync().ConfigureAwait(true);

        Assert.Equal(TransactionFailureReason.SubscriberUnavailable, unavailable.Reason);
        Assert.NotNull(claimed);
        Assert.Equal(0, result.AttemptedCount);
        Assert.Equal(0, result.AcknowledgedCount);
        Assert.Single(subscriber.CommitRequests);
    }

    /// <summary>Durable pub-sub returns a stored acknowledgement without republishing a committed handoff.</summary>
    [Fact]
    public async Task PubSubSqliteStore_ReturnsAcknowledgedCommitWithoutRepublishing()
    {
        using var workspace = TempWorkspace.Create();
        var request = CreateCommitRequest("txn-pubsub-durable-ack");
        var inner = new ScriptedTransactionPubSub(Committed(request.Manifest.TransactionId));
        using var store = new SqliteTransactionSecurityStateStore(workspace.GetPath("pubsub-ack.db"));
        using var pubSub = new DurableTransactionPubSub(inner, store);

        var first = await pubSub.PublishCommitAsync(request).ConfigureAwait(true);
        var second = await pubSub.PublishCommitAsync(request).ConfigureAwait(true);
        var replay = await pubSub.ReplayPendingAsync().ConfigureAwait(true);

        Assert.Equal("committed", first.Status);
        Assert.Equal("committed", second.Status);
        Assert.Equal(first.DiffgramId, second.DiffgramId);
        Assert.Single(inner.CommitRequests);
        Assert.Equal(0, replay.AttemptedCount);
    }

    /// <summary>Durable pub-sub rejects same-transaction conflicting commit payloads without overwriting replay state.</summary>
    [Fact]
    public async Task PubSubSqliteStore_RejectsConflictingCommitPayloadWithoutOverwritingPendingMessage()
    {
        using var workspace = TempWorkspace.Create();
        var transactionId = "txn-pubsub-durable-conflict";
        var original = CreateCommitRequest(transactionId, "encrypted-diffgram-original");
        var conflicting = CreateCommitRequest(transactionId, "encrypted-diffgram-conflicting");
        var inner = new ScriptedTransactionPubSub(
            SubscriberUnavailableCommit(transactionId),
            Committed(transactionId));
        using var store = new SqliteTransactionSecurityStateStore(workspace.GetPath("pubsub-conflict.db"));
        using var pubSub = new DurableTransactionPubSub(inner, store);

        var unavailable = await pubSub.PublishCommitAsync(original).ConfigureAwait(true);
        var conflict = await pubSub.PublishCommitAsync(conflicting).ConfigureAwait(true);
        var replay = await pubSub.ReplayPendingAsync().ConfigureAwait(true);

        Assert.Equal(TransactionFailureReason.SubscriberUnavailable, unavailable.Reason);
        Assert.Equal("rejected", conflict.Status);
        Assert.Equal(TransactionFailureReason.DuplicateConflict, conflict.Reason);
        Assert.Equal(1, replay.AcknowledgedCount);
        Assert.Equal(2, inner.CommitRequests.Count);
        Assert.Equal(original.EncryptedBodySha256, inner.CommitRequests[1].EncryptedBodySha256);
    }

    /// <summary>Durable pub-sub keeps unavailable abort handoffs replayable across store recreation.</summary>
    [Fact]
    public async Task PubSubSqliteStore_PersistsPendingAbortAndReplaysAfterSubscriberRecovery()
    {
        using var workspace = TempWorkspace.Create();
        var databasePath = workspace.GetPath("pubsub-abort-replay.db");
        const string TransactionId = "txn-pubsub-durable-abort";
        var request = new TransactionAbortRequest
        {
            Reason = TransactionFailureReason.Aborted,
            Actor = "durable-pubsub-test",
        };

        using (var store = new SqliteTransactionSecurityStateStore(databasePath))
        using (var pubSub = new DurableTransactionPubSub(
                   new ScriptedTransactionPubSub(abortResponses: [SubscriberUnavailableAbort(TransactionId)]),
                   store))
        {
            var unavailable = await pubSub.PublishAbortAsync(TransactionId, request).ConfigureAwait(true);
            var pending = await pubSub.GetPendingMessagesAsync().ConfigureAwait(true);

            Assert.Equal("rejected", unavailable.Status);
            Assert.Equal(TransactionFailureReason.SubscriberUnavailable, unavailable.Reason);
            var pendingMessage = Assert.Single(pending);
            Assert.Equal("abort", pendingMessage.Kind);
            Assert.Equal("pending", pendingMessage.Status);
        }

        var recoveredInner = new ScriptedTransactionPubSub(abortResponses: [Aborted(TransactionId)]);
        using var replayStore = new SqliteTransactionSecurityStateStore(databasePath);
        using var replayPubSub = new DurableTransactionPubSub(recoveredInner, replayStore);
        var result = await replayPubSub.ReplayPendingAsync().ConfigureAwait(true);
        var afterReplay = await replayPubSub.GetPendingMessagesAsync().ConfigureAwait(true);

        Assert.Equal(1, result.AttemptedCount);
        Assert.Equal(1, result.AcknowledgedCount);
        Assert.Equal(0, result.PendingCount);
        Assert.Empty(afterReplay);
        var replayedAbort = Assert.Single(recoveredInner.AbortRequests);
        Assert.Equal(TransactionId, replayedAbort.TransactionId);
        Assert.Equal("durable-pubsub-test", replayedAbort.Request.Actor);
    }

    private static ServiceProvider BuildDurablePubSubProvider(
        string databasePath,
        int inProgressClaimLeaseSeconds,
        ISubscriberCommitService subscriber)
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mcp:TurnTransactions:DurablePubSubEnabled"] = "true",
                ["Mcp:TurnTransactions:PubSubDatabasePath"] = databasePath,
                ["Mcp:TurnTransactions:PubSubInProgressClaimLeaseSeconds"] = inProgressClaimLeaseSeconds.ToString(),
            })
            .Build();
        services.AddInProcessTransactionSecurity(configuration);
        services.AddSingleton(subscriber);
        return services.BuildServiceProvider();
    }

    private static InMemoryKeyServerService CreateKeyServer(string? databasePath = null)
        => new(
            new FixedOptionsMonitor<KeyServerOptions>(new KeyServerOptions { DatabasePath = databasePath }),
            new TransactionManifestCanonicalizer());

    private static InMemorySubscriberCommitService CreateSubscriber(
        IKeyServerManifestService keyServer,
        string? databasePath = null)
        => new(
            keyServer,
            new TransactionManifestCanonicalizer(),
            new FixedOptionsMonitor<SubscriberOptions>(
                new SubscriberOptions
                {
                    DatabasePath = databasePath,
                    PartyId = SubscriberPartyId,
                }));

    private static async Task RegisterStandardPartiesAsync(IKeyServerPartyRegistry registry)
    {
        await registry.RegisterPartyAsync(new PartyRegistrationRequest { PartyId = PublisherPartyId, Role = "publisher" })
            .ConfigureAwait(false);
        await RegisterSubscriberAsync(registry).ConfigureAwait(false);
    }

    private static Task<PartyRegistrationResponse> RegisterExternalSigningPublisherAsync(
        IKeyServerPartyRegistry registry,
        SigningKeyPair signingKey)
        => RegisterExternalSigningPublisherAsync(registry, signingKey, ExternalPublisherSigningKeyId);

    private static Task<PartyRegistrationResponse> RegisterExternalSigningPublisherAsync(
        IKeyServerPartyRegistry registry,
        SigningKeyPair signingKey,
        string signingKeyId)
        => registry.RegisterPartyAsync(
            new PartyRegistrationRequest
            {
                PartyId = PublisherPartyId,
                Role = "publisher",
                ActiveSigningKeyId = signingKeyId,
                SigningPrivateKeyPem = signingKey.PrivateKeyPem,
            });

    private static Task<PartyRegistrationResponse> RegisterSubscriberAsync(IKeyServerPartyRegistry registry)
        => registry.RegisterPartyAsync(new PartyRegistrationRequest { PartyId = SubscriberPartyId, Role = "subscriber" });

    private static async Task AssertSigningKeyRotationPreservesVerificationAsync(InMemoryKeyServerService keyServer)
    {
        using var firstSigningKey = SigningKeyPair.Create();
        using var rotatedSigningKey = SigningKeyPair.Create();
        await RegisterExternalSigningPublisherAsync(keyServer, firstSigningKey, ExternalPublisherSigningKeyId)
            .ConfigureAwait(false);
        await RegisterSubscriberAsync(keyServer).ConfigureAwait(false);
        var firstManifest = await SignManifestAsync(
            keyServer,
            "txn-key-rotation-first",
            700,
            "nonce-key-rotation-first",
            ExternalPublisherSigningKeyId).ConfigureAwait(false);

        var rotation = await RegisterExternalSigningPublisherAsync(
            keyServer,
            rotatedSigningKey,
            RotatedPublisherSigningKeyId).ConfigureAwait(false);
        var oldDescriptor = await keyServer.GetPartyKeyAsync(PublisherPartyId, ExternalPublisherSigningKeyId)
            .ConfigureAwait(false);
        var rotatedDescriptor = await keyServer.GetPartyKeyAsync(PublisherPartyId, RotatedPublisherSigningKeyId)
            .ConfigureAwait(false);
        var oldSigningAttempt = await keyServer.SignManifestAsync(
            new TransactionManifestSignRequest
            {
                TransactionId = "txn-key-rotation-old-signing-attempt",
                TurnId = "turn-key-rotation",
                PublisherPartyId = PublisherPartyId,
                PublisherSigningKeyId = ExternalPublisherSigningKeyId,
                SubscriberPartyId = SubscriberPartyId,
                Sequence = 701,
                Nonce = "nonce-key-rotation-old-signing-attempt",
                DiffgramSha256 = Sha256Hex("plain-diffgram"),
                EncryptedBodySha256 = Sha256Hex("encrypted-diffgram"),
            }).ConfigureAwait(false);
        var rotatedManifest = await SignManifestAsync(
            keyServer,
            "txn-key-rotation-rotated",
            702,
            "nonce-key-rotation-rotated",
            RotatedPublisherSigningKeyId).ConfigureAwait(false);
        var verifyFirst = await keyServer.VerifyManifestAsync(
            new TransactionManifestVerifyRequest
            {
                Manifest = firstManifest,
                ExpectedSubscriberPartyId = SubscriberPartyId,
            }).ConfigureAwait(false);
        var verifyRotated = await keyServer.VerifyManifestAsync(
            new TransactionManifestVerifyRequest
            {
                Manifest = rotatedManifest,
                ExpectedSubscriberPartyId = SubscriberPartyId,
            }).ConfigureAwait(false);

        Assert.Equal(RotatedPublisherSigningKeyId, rotation.ActiveSigningKeyId);
        Assert.NotNull(oldDescriptor);
        Assert.Equal(firstSigningKey.PublicKeyPem, oldDescriptor.PublicKeyPem);
        Assert.NotNull(rotatedDescriptor);
        Assert.Equal(rotatedSigningKey.PublicKeyPem, rotatedDescriptor.PublicKeyPem);
        Assert.False(oldSigningAttempt.Success);
        Assert.Equal(TransactionFailureReason.UnknownKey, oldSigningAttempt.Reason);
        Assert.True(verifyFirst.IsValid);
        Assert.Equal(TransactionFailureReason.None, verifyFirst.Reason);
        Assert.True(verifyRotated.IsValid);
        Assert.Equal(TransactionFailureReason.None, verifyRotated.Reason);
    }

    private static async Task<TransactionManifestDto> SignManifestAsync(
        IKeyServerManifestService keyServer,
        string transactionId,
        long sequence,
        string nonce,
        string publisherSigningKeyId)
    {
        var response = await keyServer.SignManifestAsync(new TransactionManifestSignRequest
        {
            TransactionId = transactionId,
            TurnId = "turn-durable-storage",
            PublisherPartyId = PublisherPartyId,
            PublisherSigningKeyId = publisherSigningKeyId,
            SubscriberPartyId = SubscriberPartyId,
            Sequence = sequence,
            Nonce = nonce,
            DiffgramSha256 = Sha256Hex("plain-diffgram"),
            EncryptedBodySha256 = Sha256Hex("encrypted-diffgram"),
        }).ConfigureAwait(false);

        Assert.True(response.Success);
        Assert.NotNull(response.Manifest);
        return response.Manifest;
    }

    private static async Task<TransactionManifestDto> SignManifestAsync(
        IKeyServerManifestService keyServer,
        string transactionId,
        long sequence,
        string nonce)
    {
        var response = await keyServer.SignManifestAsync(new TransactionManifestSignRequest
        {
            TransactionId = transactionId,
            TurnId = "turn-durable-storage",
            PublisherPartyId = PublisherPartyId,
            SubscriberPartyId = SubscriberPartyId,
            Sequence = sequence,
            Nonce = nonce,
            DiffgramSha256 = Sha256Hex("plain-diffgram"),
            EncryptedBodySha256 = Sha256Hex("encrypted-diffgram"),
        }).ConfigureAwait(false);

        Assert.True(response.Success);
        Assert.NotNull(response.Manifest);
        return response.Manifest;
    }

    private static DiffgramCommitRequest CreateCommitRequest(TransactionManifestDto manifest)
        => new()
        {
            Manifest = manifest,
            EncryptedDiffgramBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("encrypted-diffgram")),
            EncryptedBodySha256 = manifest.EncryptedBodySha256,
            DiffgramSha256 = manifest.DiffgramSha256,
        };

    private static DiffgramCommitRequest CreateCommitRequest(
        string transactionId,
        string encryptedBody = "encrypted-diffgram")
    {
        var encryptedBodySha256 = Sha256Hex(encryptedBody);
        return new DiffgramCommitRequest
        {
            Manifest = new TransactionManifestDto
            {
                TransactionId = transactionId,
                TurnId = "turn-durable-pubsub",
                PublisherPartyId = PublisherPartyId,
                SubscriberPartyId = SubscriberPartyId,
                Sequence = 900,
                Nonce = $"nonce-{transactionId}",
                DiffgramSha256 = Sha256Hex("plain-diffgram"),
                EncryptedBodySha256 = encryptedBodySha256,
            },
            EncryptedDiffgramBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(encryptedBody)),
            EncryptedBodySha256 = encryptedBodySha256,
            DiffgramSha256 = Sha256Hex("plain-diffgram"),
        };
    }

    private static async Task<IReadOnlyList<TransactionAuditEntity>> ReadAuditEventsAsync(string databasePath)
    {
        var options = new DbContextOptionsBuilder<TransactionSecurityDbContext>()
            .UseSqlite($"Data Source={Path.GetFullPath(databasePath)}")
            .Options;
        await using var db = new TransactionSecurityDbContext(options);
        return await db.TransactionAuditEvents
            .AsNoTracking()
            .OrderBy(entry => entry.Id)
            .ToArrayAsync()
            .ConfigureAwait(false);
    }

    private static string Sha256Hex(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private sealed class FixedOptionsMonitor<T> : IOptionsMonitor<T>
    {
        private readonly T _value;

        public FixedOptionsMonitor(T value)
        {
            _value = value;
        }

        public T CurrentValue => _value;

        public T Get(string? name) => _value;

        public IDisposable? OnChange(Action<T, string?> listener) => NullDisposable.Instance;
    }

    private sealed class NullDisposable : IDisposable
    {
        public static readonly NullDisposable Instance = new();

        public void Dispose()
        {
        }
    }

    private sealed class SigningKeyPair : IDisposable
    {
        private readonly ECDsa _key;

        private SigningKeyPair(ECDsa key)
        {
            _key = key;
            PublicKeyPem = key.ExportSubjectPublicKeyInfoPem();
            PrivateKeyPem = key.ExportPkcs8PrivateKeyPem();
        }

        public string PublicKeyPem { get; }

        public string PrivateKeyPem { get; }

        public static SigningKeyPair Create()
            => new(ECDsa.Create(ECCurve.NamedCurves.nistP256));

        public void Dispose()
            => _key.Dispose();
    }

    private sealed class BlockingManifestService : IKeyServerManifestService
    {
        private readonly IKeyServerManifestService _inner;
        private readonly TaskCompletionSource _verifyStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _allowVerify = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public BlockingManifestService(IKeyServerManifestService inner)
        {
            _inner = inner;
        }

        public Task<TransactionManifestSignResponse> SignManifestAsync(
            TransactionManifestSignRequest request,
            CancellationToken cancellationToken = default)
            => _inner.SignManifestAsync(request, cancellationToken);

        public async Task<TransactionManifestVerifyResponse> VerifyManifestAsync(
            TransactionManifestVerifyRequest request,
            CancellationToken cancellationToken = default)
        {
            _verifyStarted.TrySetResult();
            await _allowVerify.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            return await _inner.VerifyManifestAsync(request, cancellationToken).ConfigureAwait(false);
        }

        public Task<TransactionManifestTraceRecord?> GetManifestAsync(
            string transactionId,
            CancellationToken cancellationToken = default)
            => _inner.GetManifestAsync(transactionId, cancellationToken);

        public Task<TransactionManifestTraceReport> GetManifestReportAsync(
            TransactionManifestTraceReportRequest request,
            CancellationToken cancellationToken = default)
            => _inner.GetManifestReportAsync(request, cancellationToken);

        public async Task WaitForVerifyAsync()
        {
            await _verifyStarted.Task.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
        }

        public void ReleaseVerification()
            => _allowVerify.TrySetResult();
    }

    private sealed class ScriptedTransactionPubSub : ITransactionPubSub
    {
        private readonly Queue<DiffgramCommitResponse> _commitResponses;
        private readonly Queue<TransactionAbortResponse> _abortResponses;
        private readonly object _gate = new();

        public ScriptedTransactionPubSub(
            params DiffgramCommitResponse[] commitResponses)
            : this(commitResponses, [])
        {
        }

        public ScriptedTransactionPubSub(
            IReadOnlyCollection<DiffgramCommitResponse>? commitResponses = null,
            IReadOnlyCollection<TransactionAbortResponse>? abortResponses = null)
        {
            _commitResponses = new Queue<DiffgramCommitResponse>(commitResponses ?? []);
            _abortResponses = new Queue<TransactionAbortResponse>(abortResponses ?? []);
        }

        public List<DiffgramCommitRequest> CommitRequests { get; } = [];

        public List<(string TransactionId, TransactionAbortRequest Request)> AbortRequests { get; } = [];

        public Task<DiffgramCommitResponse> PublishCommitAsync(
            DiffgramCommitRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lock (_gate)
            {
                CommitRequests.Add(request);
                return Task.FromResult(
                    _commitResponses.Count > 0
                        ? _commitResponses.Dequeue()
                        : Committed(request.Manifest.TransactionId));
            }
        }

        public Task<TransactionAbortResponse> PublishAbortAsync(
            string transactionId,
            TransactionAbortRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lock (_gate)
            {
                AbortRequests.Add((transactionId, request));
                return Task.FromResult(
                    _abortResponses.Count > 0
                        ? _abortResponses.Dequeue()
                        : Aborted(transactionId));
            }
        }
    }

    private sealed class TimeoutTransactionPubSub : ITransactionPubSub
    {
        private readonly object _gate = new();

        public List<DiffgramCommitRequest> CommitRequests { get; } = [];

        public async Task<DiffgramCommitResponse> PublishCommitAsync(
            DiffgramCommitRequest request,
            CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                CommitRequests.Add(request);
            }

            await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);
            return Committed(request.Manifest.TransactionId);
        }

        public Task<TransactionAbortResponse> PublishAbortAsync(
            string transactionId,
            TransactionAbortRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Aborted(transactionId));
        }
    }

    private sealed class ScriptedSubscriberCommitService : ISubscriberCommitService
    {
        private readonly Queue<DiffgramCommitResponse> _commitResponses;
        private readonly object _gate = new();

        public ScriptedSubscriberCommitService(params DiffgramCommitResponse[] commitResponses)
        {
            _commitResponses = new Queue<DiffgramCommitResponse>(commitResponses);
        }

        public List<DiffgramCommitRequest> CommitRequests { get; } = [];

        public Task<DiffgramCommitResponse> CommitDiffgramAsync(
            DiffgramCommitRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lock (_gate)
            {
                CommitRequests.Add(request);
                return Task.FromResult(
                    _commitResponses.Count > 0
                        ? _commitResponses.Dequeue()
                        : Committed(request.Manifest.TransactionId));
            }
        }

        public Task<TransactionStatusResponse?> GetTransactionStatusAsync(
            string transactionId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<TransactionStatusResponse?>(null);
        }

        public Task<TransactionAbortResponse> AbortTransactionAsync(
            string transactionId,
            TransactionAbortRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Aborted(transactionId));
        }
    }

    private static DiffgramCommitResponse Committed(string transactionId)
        => new()
        {
            TransactionId = transactionId,
            Status = "committed",
            Reason = TransactionFailureReason.None,
            DiffgramId = $"diffgram-{transactionId}",
            CommittedAtUtc = DateTimeOffset.UtcNow,
        };

    private static DiffgramCommitResponse SubscriberUnavailableCommit(string transactionId)
        => new()
        {
            TransactionId = transactionId,
            Status = "rejected",
            Reason = TransactionFailureReason.SubscriberUnavailable,
        };

    private static TransactionAbortResponse Aborted(string transactionId)
        => new()
        {
            TransactionId = transactionId,
            Status = "aborted",
            Reason = TransactionFailureReason.Aborted,
            AbortedAtUtc = DateTimeOffset.UtcNow,
        };

    private static TransactionAbortResponse SubscriberUnavailableAbort(string transactionId)
        => new()
        {
            TransactionId = transactionId,
            Status = "rejected",
            Reason = TransactionFailureReason.SubscriberUnavailable,
        };

    private sealed class TempWorkspace : IDisposable
    {
        private TempWorkspace(string rootPath)
        {
            RootPath = rootPath;
        }

        public string RootPath { get; }

        public static TempWorkspace Create()
        {
            var rootPath = Path.Combine(
                Path.GetTempPath(),
                "mcpserver-transaction-security-tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(rootPath);
            return new TempWorkspace(rootPath);
        }

        public string GetPath(string fileName)
            => Path.Combine(RootPath, fileName);

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(RootPath))
                Directory.Delete(RootPath, recursive: true);
        }
    }
}
