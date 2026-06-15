using System.Security.Cryptography;
using System.Text;
using McpServer.Support.Mcp.Controllers;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// Unit tests for <see cref="TurnTransactionCoordinator"/> transaction gating.
/// TEST-MCP-161, TEST-MCP-168, TEST-MCP-169.
/// </summary>
public sealed class TurnTransactionCoordinatorTests
{
    /// <summary>Disabled coordinator bypasses transaction dependencies and preserves existing mutation behavior.</summary>
    [Fact]
    public async Task ExecuteAsync_WhenDisabled_BypassesAndRunsMutation()
    {
        var audit = new InMemoryTransactionAuditWriter();
        var coordinator = CreateCoordinator(new TurnTransactionOptions { Enabled = false }, auditWriter: audit);

        var result = await coordinator.ExecuteAsync(
            CreateRequest("txn-disabled"),
            _ => Task.FromResult(new TurnMutationResult { Success = true, ResultJson = "{\"ok\":true}" }),
            CancellationToken.None).ConfigureAwait(true);

        Assert.Equal("bypassed", result.Status);
        Assert.True(result.MutationApplied);
        Assert.Equal(TransactionFailureReason.None, result.Reason);
        Assert.Contains(audit.Snapshot(), item => item.EventName == "transaction_bypassed");
    }

    /// <summary>Enabled coordinator bypasses mutation gating when mutation transactions are not required.</summary>
    [Fact]
    public async Task ExecuteAsync_WhenRequiredForMutationsFalse_BypassesAndRunsMutation()
    {
        var keyServer = Substitute.For<IKeyServerManifestService>();
        var transactionPubSub = Substitute.For<ITransactionPubSub>();
        var coordinator = CreateCoordinator(
            new TurnTransactionOptions { Enabled = true, RequiredForMutations = false },
            keyServer: keyServer,
            transactionPubSub: transactionPubSub);

        var result = await coordinator.ExecuteAsync(
            CreateRequest("txn-required-false"),
            _ => Task.FromResult(new TurnMutationResult { Success = true, ResultJson = "{\"ok\":true}" }),
            CancellationToken.None).ConfigureAwait(true);

        Assert.Equal("bypassed", result.Status);
        Assert.True(result.MutationApplied);
        Assert.Contains("not required", result.Message, StringComparison.OrdinalIgnoreCase);
        await keyServer.DidNotReceive().SignManifestAsync(Arg.Any<TransactionManifestSignRequest>(), Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
        await transactionPubSub.DidNotReceive().PublishCommitAsync(Arg.Any<DiffgramCommitRequest>(), Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>Enabled coordinator returns committed only after subscriber commit confirms the diffgram.</summary>
    [Fact]
    public async Task ExecuteAsync_WhenEnabled_CommitsAfterSubscriberConfirmation()
    {
        var registry = Substitute.For<IKeyServerPartyRegistry>();
        registry.GetPartyKeyAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new PartyKeyDescriptor { PartyId = "mcpserver", KeyId = "mcpserver:signing:1", Status = "active" });
        var keyServer = Substitute.For<IKeyServerManifestService>();
        keyServer.SignManifestAsync(Arg.Any<TransactionManifestSignRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TransactionManifestSignResponse
            {
                Success = true,
                Reason = TransactionFailureReason.None,
                Manifest = CreateManifest("txn-commit"),
            });
        var transactionPubSub = Substitute.For<ITransactionPubSub>();
        transactionPubSub.PublishCommitAsync(Arg.Any<DiffgramCommitRequest>(), Arg.Any<CancellationToken>())
            .Returns(new DiffgramCommitResponse
            {
                TransactionId = "txn-commit",
                Status = "committed",
                Reason = TransactionFailureReason.None,
                DiffgramId = "diffgram-txn-commit",
            });
        var coordinator = CreateCoordinator(
            new TurnTransactionOptions { Enabled = true },
            registry,
            keyServer,
            transactionPubSub: transactionPubSub);

        var result = await coordinator.ExecuteAsync(
            CreateRequest("txn-commit"),
            _ => Task.FromResult(new TurnMutationResult { Success = true, ResultJson = "{\"updated\":true}" }),
            CancellationToken.None).ConfigureAwait(true);

        Assert.Equal("committed", result.Status);
        Assert.True(result.MutationApplied);
        Assert.Equal("diffgram-txn-commit", result.DiffgramId);
        await transactionPubSub.Received(1).PublishCommitAsync(
                Arg.Is<DiffgramCommitRequest>(request =>
                    request != null &&
                    request.Manifest.TransactionId == "txn-commit" &&
                    request.Manifest.PublisherPartyId == "mcpserver" &&
                    request.Manifest.SubscriberPartyId == "subscriber-1" &&
                    !string.IsNullOrWhiteSpace(request.DiffgramSha256) &&
                    !string.IsNullOrWhiteSpace(request.EncryptedBodySha256)),
                Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>Enabled coordinator sends a protected envelope when subscriber encryption is required.</summary>
    [Fact]
    public async Task ExecuteAsync_WhenProtectDiffgramsEnabled_SendsProtectedEnvelopeToSubscriber()
    {
        using var encryptionKey = EncryptionKeyPair.Create();
        var canonicalizer = new TransactionManifestCanonicalizer();
        using var keyServer = new InMemoryKeyServerService(Monitor(new KeyServerOptions()), canonicalizer);
        await keyServer.RegisterPartyAsync(new PartyRegistrationRequest { PartyId = "mcpserver", Role = "publisher" })
            .ConfigureAwait(true);
        await keyServer.RegisterPartyAsync(new PartyRegistrationRequest
        {
            PartyId = "subscriber-1",
            Role = "subscriber",
            ActiveEncryptionKeyId = "subscriber-1:encryption:1",
            EncryptionPublicKeyPem = encryptionKey.PublicKeyPem,
        }).ConfigureAwait(true);
        var protector = new TransactionDiffgramProtector();
        var innerSubscriber = new InMemorySubscriberCommitService(
            keyServer,
            canonicalizer,
            Monitor(new SubscriberOptions
            {
                PartyId = "subscriber-1",
                EncryptionKeyId = "subscriber-1:encryption:1",
                EncryptionPrivateKeyPem = encryptionKey.PrivateKeyPem,
                RequireEncryptedDiffgrams = true,
            }),
            protector);
        var subscriber = new CapturingSubscriberCommitService(innerSubscriber);
        var transactionPubSub = new DirectSubscriberTransactionPubSub(subscriber);
        var coordinator = CreateCoordinator(
            new TurnTransactionOptions
            {
                Enabled = true,
                ProtectDiffgrams = true,
                SubscriberEncryptionKeyId = "subscriber-1:encryption:1",
            },
            keyServer,
            keyServer,
            transactionPubSub: transactionPubSub,
            diffgramBuilder: new JsonDiffgramBuilder(protector),
            subscriberOptions: new SubscriberOptions { RequireEncryptedDiffgrams = true });

        var result = await coordinator.ExecuteAsync(
            CreateRequest("txn-protected-coordinator"),
            _ => Task.FromResult(new TurnMutationResult { Success = true, ResultJson = "{\"updated\":true}" }),
            CancellationToken.None).ConfigureAwait(true);

        Assert.Equal("committed", result.Status);
        Assert.NotNull(subscriber.LastCommit);
        var commit = subscriber.LastCommit!;
        Assert.Equal("subscriber-1:encryption:1", commit.Manifest.SubscriberEncryptionKeyId);
        Assert.Equal(commit.EncryptedBodySha256, commit.Manifest.EncryptedBodySha256);
        var envelopeJson = Encoding.UTF8.GetString(Convert.FromBase64String(commit.EncryptedDiffgramBase64));
        Assert.Contains("mcp-transaction-diffgram-v1", envelopeJson, StringComparison.Ordinal);
        Assert.DoesNotContain("PLAN-TURNTRANSACTIONS-001", envelopeJson, StringComparison.Ordinal);
    }

    /// <summary>Keyserver signing failure prevents mutation execution.</summary>
    [Fact]
    public async Task ExecuteAsync_WhenSigningFails_DoesNotRunMutation()
    {
        var keyServer = Substitute.For<IKeyServerManifestService>();
        keyServer.SignManifestAsync(Arg.Any<TransactionManifestSignRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TransactionManifestSignResponse
            {
                Success = false,
                Reason = TransactionFailureReason.UnknownParty,
            });
        var coordinator = CreateCoordinator(
            new TurnTransactionOptions { Enabled = true },
            keyServer: keyServer);
        var mutationRan = false;

        var result = await coordinator.ExecuteAsync(
            CreateRequest("txn-sign-fail"),
            _ =>
            {
                mutationRan = true;
                return Task.FromResult(new TurnMutationResult { Success = true });
            },
            CancellationToken.None).ConfigureAwait(true);

        Assert.Equal("rejected", result.Status);
        Assert.False(result.MutationApplied);
        Assert.False(mutationRan);
        Assert.Equal(TransactionFailureReason.UnknownParty, result.Reason);
    }

    /// <summary>Keyserver exceptions are mapped to dependency failure without running the mutation.</summary>
    [Fact]
    public async Task ExecuteAsync_WhenKeyServerThrows_EntersDegradedModeWithoutMutation()
    {
        var keyServer = Substitute.For<IKeyServerManifestService>();
        keyServer.SignManifestAsync(Arg.Any<TransactionManifestSignRequest>(), Arg.Any<CancellationToken>())
            .Returns<Task<TransactionManifestSignResponse>>(_ => throw new InvalidOperationException("keyserver offline"));
        var coordinator = CreateCoordinator(
            new TurnTransactionOptions { Enabled = true, DegradedModeEnabled = true },
            keyServer: keyServer);
        var mutationRan = false;

        var result = await coordinator.ExecuteAsync(
            CreateRequest("txn-keyserver-throw"),
            _ =>
            {
                mutationRan = true;
                return Task.FromResult(new TurnMutationResult { Success = true });
            },
            CancellationToken.None).ConfigureAwait(true);

        Assert.Equal("degraded", result.Status);
        Assert.False(result.MutationApplied);
        Assert.False(mutationRan);
        Assert.Equal(TransactionFailureReason.KeyServerUnavailable, result.Reason);
    }

    /// <summary>Mutation failure aborts the subscriber transaction and records final status plus audit.</summary>
    [Fact]
    public async Task ExecuteAsync_WhenMutationFails_RecordsAbortStatusAndAudit()
    {
        var audit = new InMemoryTransactionAuditWriter();
        var transactionPubSub = Substitute.For<ITransactionPubSub>();
        transactionPubSub.PublishAbortAsync(
                "txn-mutation-fail",
                Arg.Any<TransactionAbortRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new TransactionAbortResponse
            {
                TransactionId = "txn-mutation-fail",
                Status = "aborted",
                Reason = TransactionFailureReason.Aborted,
            });
        var coordinator = CreateCoordinator(
            new TurnTransactionOptions { Enabled = true },
            transactionPubSub: transactionPubSub,
            auditWriter: audit);

        var result = await coordinator.ExecuteAsync(
            CreateRequest("txn-mutation-fail"),
            _ => Task.FromResult(new TurnMutationResult { Success = false, Error = "mutation failed" }),
            CancellationToken.None).ConfigureAwait(true);
        var status = coordinator.GetStatus();

        Assert.Equal("aborted", result.Status);
        Assert.True(result.MutationApplied);
        Assert.Equal(TransactionFailureReason.Aborted, result.Reason);
        Assert.Equal("txn-mutation-fail", status.LastTransactionId);
        Assert.Equal(TransactionFailureReason.Aborted, status.LastReason);
        Assert.Contains(audit.Snapshot(), item => item.EventName == "transaction_aborted");
        await transactionPubSub.Received(1).PublishAbortAsync(
                "txn-mutation-fail",
                Arg.Is<TransactionAbortRequest>(request =>
                    request != null &&
                    request.Reason == TransactionFailureReason.Aborted &&
                    request.Actor == "TurnTransactionCoordinator"),
                Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
        await transactionPubSub.DidNotReceive().PublishCommitAsync(
                Arg.Any<DiffgramCommitRequest>(),
                Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>Subscriber duplicate acknowledgement is treated as committed by the coordinator.</summary>
    [Fact]
    public async Task ExecuteAsync_WhenPubSubReturnsDuplicate_TreatsAsCommitted()
    {
        var transactionPubSub = Substitute.For<ITransactionPubSub>();
        transactionPubSub.PublishCommitAsync(Arg.Any<DiffgramCommitRequest>(), Arg.Any<CancellationToken>())
            .Returns(new DiffgramCommitResponse
            {
                TransactionId = "txn-duplicate",
                Status = "duplicate",
                Reason = TransactionFailureReason.None,
                DiffgramId = "diffgram-txn-duplicate",
            });
        var coordinator = CreateCoordinator(
            new TurnTransactionOptions { Enabled = true },
            transactionPubSub: transactionPubSub);

        var result = await coordinator.ExecuteAsync(
            CreateRequest("txn-duplicate"),
            _ => Task.FromResult(new TurnMutationResult { Success = true }),
            CancellationToken.None).ConfigureAwait(true);

        Assert.Equal("committed", result.Status);
        Assert.Equal(TransactionFailureReason.None, result.Reason);
        Assert.Equal("diffgram-txn-duplicate", result.DiffgramId);
    }

    /// <summary>Pub-sub transport exceptions map to subscriber-unavailable degraded mode.</summary>
    [Fact]
    public async Task ExecuteAsync_WhenPubSubThrows_EntersDegradedModeWithSubscriberUnavailable()
    {
        var transactionPubSub = Substitute.For<ITransactionPubSub>();
        transactionPubSub.PublishCommitAsync(Arg.Any<DiffgramCommitRequest>(), Arg.Any<CancellationToken>())
            .Returns<Task<DiffgramCommitResponse>>(_ => throw new InvalidOperationException("pubsub offline"));
        var coordinator = CreateCoordinator(
            new TurnTransactionOptions { Enabled = true, DegradedModeEnabled = true },
            transactionPubSub: transactionPubSub);

        var result = await coordinator.ExecuteAsync(
            CreateRequest("txn-pubsub-throw"),
            _ => Task.FromResult(new TurnMutationResult { Success = true }),
            CancellationToken.None).ConfigureAwait(true);

        Assert.Equal("degraded", result.Status);
        Assert.True(result.MutationApplied);
        Assert.True(result.Degraded);
        Assert.Equal(TransactionFailureReason.SubscriberUnavailable, result.Reason);
    }

    /// <summary>Subscriber dependency failure enters degraded mode when configured.</summary>
    [Fact]
    public async Task ExecuteAsync_WhenSubscriberUnavailable_EntersDegradedMode()
    {
        var transactionPubSub = Substitute.For<ITransactionPubSub>();
        transactionPubSub.PublishCommitAsync(Arg.Any<DiffgramCommitRequest>(), Arg.Any<CancellationToken>())
            .Returns(new DiffgramCommitResponse
            {
                TransactionId = "txn-degraded",
                Status = "rejected",
                Reason = TransactionFailureReason.SubscriberUnavailable,
            });
        var audit = new InMemoryTransactionAuditWriter();
        var coordinator = CreateCoordinator(
            new TurnTransactionOptions { Enabled = true, DegradedModeEnabled = true },
            transactionPubSub: transactionPubSub,
            auditWriter: audit);

        var result = await coordinator.ExecuteAsync(
            CreateRequest("txn-degraded"),
            _ => Task.FromResult(new TurnMutationResult { Success = true }),
            CancellationToken.None).ConfigureAwait(true);

        Assert.Equal("degraded", result.Status);
        Assert.True(result.Degraded);
        Assert.True(result.MutationApplied);
        Assert.Equal(TransactionFailureReason.SubscriberUnavailable, result.Reason);
        Assert.Contains(audit.Snapshot(), item => item.EventName == "transaction_degraded");
    }

    /// <summary>Degraded subscriber failure invokes mutation rollback compensation while preserving audit rows.</summary>
    [Fact]
    public async Task ExecuteAsync_WhenSubscriberUnavailableAndRollbackProvided_RunsRollbackCompensation()
    {
        var transactionPubSub = Substitute.For<ITransactionPubSub>();
        transactionPubSub.PublishCommitAsync(Arg.Any<DiffgramCommitRequest>(), Arg.Any<CancellationToken>())
            .Returns(new DiffgramCommitResponse
            {
                TransactionId = "txn-degraded-rollback",
                Status = "rejected",
                Reason = TransactionFailureReason.SubscriberUnavailable,
            });
        var audit = new InMemoryTransactionAuditWriter();
        var coordinator = CreateCoordinator(
            new TurnTransactionOptions { Enabled = true, DegradedModeEnabled = true },
            transactionPubSub: transactionPubSub,
            auditWriter: audit);
        var mutationApplied = false;
        var rollbackApplied = false;

        var result = await coordinator.ExecuteAsync(
            CreateRequest("txn-degraded-rollback"),
            _ =>
            {
                mutationApplied = true;
                return Task.FromResult(new TurnMutationResult
                {
                    Success = true,
                    RollbackAsync = ct =>
                    {
                        rollbackApplied = true;
                        return Task.CompletedTask;
                    },
                });
            },
            CancellationToken.None).ConfigureAwait(true);
        var auditEvents = audit.Snapshot();

        Assert.Equal("degraded", result.Status);
        Assert.True(result.Degraded);
        Assert.True(result.MutationApplied);
        Assert.True(mutationApplied);
        Assert.True(rollbackApplied);
        Assert.True(result.RollbackAttempted);
        Assert.True(result.RollbackSucceeded);
        Assert.Null(result.RollbackError);
        Assert.Equal(TransactionFailureReason.SubscriberUnavailable, result.Reason);
        Assert.Contains(auditEvents, item => item.EventName == "transaction_rollback_completed");
        Assert.Contains(auditEvents, item => item.EventName == "transaction_degraded");
    }

    /// <summary>Rollback failure is reported without hiding the original subscriber commit reason.</summary>
    [Fact]
    public async Task ExecuteAsync_WhenRollbackCompensationFails_PreservesOriginalTransactionReason()
    {
        var transactionPubSub = Substitute.For<ITransactionPubSub>();
        transactionPubSub.PublishCommitAsync(Arg.Any<DiffgramCommitRequest>(), Arg.Any<CancellationToken>())
            .Returns(new DiffgramCommitResponse
            {
                TransactionId = "txn-rollback-failure",
                Status = "rejected",
                Reason = TransactionFailureReason.DuplicateConflict,
            });
        var audit = new InMemoryTransactionAuditWriter();
        var coordinator = CreateCoordinator(
            new TurnTransactionOptions { Enabled = true, DegradedModeEnabled = true },
            transactionPubSub: transactionPubSub,
            auditWriter: audit);

        var result = await coordinator.ExecuteAsync(
            CreateRequest("txn-rollback-failure"),
            _ => Task.FromResult(new TurnMutationResult
            {
                Success = true,
                RollbackAsync = ct => throw new InvalidOperationException("compensation failed"),
            }),
            CancellationToken.None).ConfigureAwait(true);
        var auditEvents = audit.Snapshot();

        Assert.Equal("rejected", result.Status);
        Assert.False(result.Degraded);
        Assert.True(result.MutationApplied);
        Assert.True(result.RollbackAttempted);
        Assert.False(result.RollbackSucceeded);
        Assert.Contains("compensation failed", result.RollbackError, StringComparison.Ordinal);
        Assert.Equal(TransactionFailureReason.DuplicateConflict, result.Reason);
        Assert.Contains(auditEvents, item => item.EventName == "transaction_rollback_failed");
        Assert.Contains(auditEvents, item => item.EventName == "diffgram_rejected");
    }

    /// <summary>Subscriber timeout is mapped to degraded mode when configured.</summary>
    [Fact]
    public async Task ExecuteAsync_WhenSubscriberCommitTimesOut_EntersDegradedMode()
    {
        var transactionPubSub = Substitute.For<ITransactionPubSub>();
        transactionPubSub.PublishCommitAsync(Arg.Any<DiffgramCommitRequest>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                await Task.Delay(TimeSpan.FromSeconds(30), callInfo.Arg<CancellationToken>()).ConfigureAwait(false);
                return new DiffgramCommitResponse();
            });
        var coordinator = CreateCoordinator(
            new TurnTransactionOptions { Enabled = true, DegradedModeEnabled = true, CommitTimeoutSeconds = 1 },
            transactionPubSub: transactionPubSub);

        var result = await coordinator.ExecuteAsync(
            CreateRequest("txn-timeout"),
            _ => Task.FromResult(new TurnMutationResult { Success = true }),
            CancellationToken.None).ConfigureAwait(true);

        Assert.Equal("degraded", result.Status);
        Assert.Equal(TransactionFailureReason.CommitTimeout, result.Reason);
        Assert.True(result.Degraded);
    }

    /// <summary>Concurrent subscriber timeouts all return structured degraded results without skipped work.</summary>
    [Fact]
    public async Task ExecuteAsync_WhenConcurrentSubscriberCommitsTimeOut_AllReturnCommitTimeout()
    {
        const int WorkerCount = 12;
        var registry = Substitute.For<IKeyServerPartyRegistry>();
        registry.GetPartyKeyAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new PartyKeyDescriptor { PartyId = "mcpserver", KeyId = "mcpserver:signing:1", Status = "active" });
        var keyServer = Substitute.For<IKeyServerManifestService>();
        keyServer.SignManifestAsync(Arg.Any<TransactionManifestSignRequest>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var signRequest = callInfo.Arg<TransactionManifestSignRequest>();
                Assert.NotNull(signRequest);
                return new TransactionManifestSignResponse
                {
                    Success = true,
                    Reason = TransactionFailureReason.None,
                    Manifest = CreateManifest(signRequest.TransactionId),
                };
            });
        var transactionPubSub = Substitute.For<ITransactionPubSub>();
        transactionPubSub.PublishCommitAsync(Arg.Any<DiffgramCommitRequest>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                await Task.Delay(TimeSpan.FromSeconds(30), callInfo.Arg<CancellationToken>()).ConfigureAwait(false);
                return new DiffgramCommitResponse();
            });
        var audit = new InMemoryTransactionAuditWriter();
        var coordinator = CreateCoordinator(
            new TurnTransactionOptions { Enabled = true, DegradedModeEnabled = true, CommitTimeoutSeconds = 1 },
            registry,
            keyServer,
            transactionPubSub: transactionPubSub,
            auditWriter: audit);
        var mutationCount = 0;

        var results = await Task.WhenAll(
                Enumerable.Range(0, WorkerCount).Select(index =>
                    coordinator.ExecuteAsync(
                        CreateRequest($"txn-timeout-load-{index:D2}"),
                        _ =>
                        {
                            Interlocked.Increment(ref mutationCount);
                            return Task.FromResult(new TurnMutationResult { Success = true });
                        },
                        CancellationToken.None)))
            .ConfigureAwait(true);
        var status = coordinator.GetStatus();
        var auditEvents = audit.Snapshot();

        Assert.All(results, result =>
        {
            Assert.Equal("degraded", result.Status);
            Assert.Equal(TransactionFailureReason.CommitTimeout, result.Reason);
            Assert.True(result.Degraded);
            Assert.True(result.MutationApplied);
        });
        Assert.Equal(WorkerCount, mutationCount);
        Assert.True(status.Degraded);
        Assert.Equal(TransactionFailureReason.CommitTimeout, status.LastReason);
        Assert.Equal(WorkerCount, auditEvents.Count(item => item.EventName == "transaction_degraded"));
        await transactionPubSub.Received(WorkerCount).PublishCommitAsync(
                Arg.Any<DiffgramCommitRequest>(),
                Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>Status controller exposes degraded mode state after a coordinator failure.</summary>
    [Fact]
    public async Task GetStatus_AfterDegradedFailure_ReturnsCurrentState()
    {
        var transactionPubSub = Substitute.For<ITransactionPubSub>();
        transactionPubSub.PublishCommitAsync(Arg.Any<DiffgramCommitRequest>(), Arg.Any<CancellationToken>())
            .Returns(new DiffgramCommitResponse
            {
                TransactionId = "txn-status",
                Status = "rejected",
                Reason = TransactionFailureReason.SubscriberUnavailable,
            });
        var coordinator = CreateCoordinator(
            new TurnTransactionOptions { Enabled = true, DegradedModeEnabled = true },
            transactionPubSub: transactionPubSub);
        await coordinator.ExecuteAsync(
            CreateRequest("txn-status"),
            _ => Task.FromResult(new TurnMutationResult { Success = true }),
            CancellationToken.None).ConfigureAwait(true);
        var controller = new TurnTransactionsController(coordinator);

        var result = controller.GetStatus();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var status = Assert.IsType<TurnTransactionStatusResponse>(ok.Value);
        Assert.True(status.Enabled);
        Assert.True(status.Degraded);
        Assert.Equal("txn-status", status.LastTransactionId);
        Assert.Equal(TransactionFailureReason.SubscriberUnavailable, status.LastReason);
    }

    private static TurnTransactionCoordinator CreateCoordinator(
        TurnTransactionOptions options,
        IKeyServerPartyRegistry? registry = null,
        IKeyServerManifestService? keyServer = null,
        ISubscriberCommitService? subscriber = null,
        ITransactionPubSub? transactionPubSub = null,
        ITransactionAuditWriter? auditWriter = null,
        IDiffgramBuilder? diffgramBuilder = null,
        SubscriberOptions? subscriberOptions = null)
    {
        var canonicalizer = new TransactionManifestCanonicalizer();
        var realKeyServer = new InMemoryKeyServerService(
            Monitor(new KeyServerOptions()),
            canonicalizer);
        registry ??= realKeyServer;
        keyServer ??= realKeyServer;
        subscriber ??= new InMemorySubscriberCommitService(
            keyServer,
            canonicalizer,
            Monitor(new SubscriberOptions { PartyId = "subscriber-1" }));
        transactionPubSub ??= new DirectSubscriberTransactionPubSub(subscriber);
        return new TurnTransactionCoordinator(
            Monitor(options),
            registry,
            keyServer,
            transactionPubSub,
            diffgramBuilder ?? new JsonDiffgramBuilder(),
            new TransactionDegradedModePolicy(Monitor(options)),
            auditWriter ?? new InMemoryTransactionAuditWriter(),
            subscriberOptions is null ? null : Monitor(subscriberOptions));
    }

    private static TurnTransactionRequest CreateRequest(string transactionId)
        => new()
        {
            TransactionId = transactionId,
            TurnId = "turn-1",
            OperationName = "todo.update",
            OperationBodyJson = "{\"id\":\"PLAN-TURNTRANSACTIONS-001\"}",
            PublisherPartyId = "mcpserver",
            SubscriberPartyId = "subscriber-1",
            Sequence = 1,
            Mutating = true,
        };

    private static TransactionManifestDto CreateManifest(string transactionId)
        => new()
        {
            TransactionId = transactionId,
            PublisherPartyId = "mcpserver",
            SubscriberPartyId = "subscriber-1",
            PublisherSigningKeyId = "mcpserver:signing:1",
            SubscriberEncryptionKeyId = "subscriber-1:encryption:1",
            Sequence = 1,
            Nonce = $"{transactionId}:1",
            IssuedAtUtc = DateTimeOffset.UtcNow,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(5),
            DiffgramSha256 = "plain",
            EncryptedBodySha256 = "encrypted",
            Signature = new TransactionManifestSignatureDto
            {
                Algorithm = "ECDSA-P256-SHA256",
                KeyId = "mcpserver:signing:1",
                Value = "signature",
                SignedAtUtc = DateTimeOffset.UtcNow,
            },
        };

    private static IOptionsMonitor<TOptions> Monitor<TOptions>(TOptions options)
        where TOptions : class
    {
        var monitor = Substitute.For<IOptionsMonitor<TOptions>>();
        monitor.CurrentValue.Returns(options);
        return monitor;
    }

    private sealed class CapturingSubscriberCommitService : ISubscriberCommitService
    {
        private readonly ISubscriberCommitService _inner;

        public CapturingSubscriberCommitService(ISubscriberCommitService inner)
        {
            _inner = inner;
        }

        public DiffgramCommitRequest? LastCommit { get; private set; }

        public Task<DiffgramCommitResponse> CommitDiffgramAsync(
            DiffgramCommitRequest request,
            CancellationToken cancellationToken = default)
        {
            LastCommit = request;
            return _inner.CommitDiffgramAsync(request, cancellationToken);
        }

        public Task<TransactionStatusResponse?> GetTransactionStatusAsync(
            string transactionId,
            CancellationToken cancellationToken = default)
            => _inner.GetTransactionStatusAsync(transactionId, cancellationToken);

        public Task<TransactionAbortResponse> AbortTransactionAsync(
            string transactionId,
            TransactionAbortRequest request,
            CancellationToken cancellationToken = default)
            => _inner.AbortTransactionAsync(transactionId, request, cancellationToken);
    }

    private sealed class EncryptionKeyPair : IDisposable
    {
        private readonly ECDiffieHellman _key;

        private EncryptionKeyPair(ECDiffieHellman key)
        {
            _key = key;
            PublicKeyPem = key.ExportSubjectPublicKeyInfoPem();
            PrivateKeyPem = key.ExportPkcs8PrivateKeyPem();
        }

        public string PublicKeyPem { get; }

        public string PrivateKeyPem { get; }

        public static EncryptionKeyPair Create()
            => new(ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256));

        public void Dispose()
            => _key.Dispose();
    }
}
