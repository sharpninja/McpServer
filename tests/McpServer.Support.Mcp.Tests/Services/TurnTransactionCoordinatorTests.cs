using McpServer.Support.Mcp.Controllers;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
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
        var subscriber = Substitute.For<ISubscriberCommitService>();
        var coordinator = CreateCoordinator(
            new TurnTransactionOptions { Enabled = true, RequiredForMutations = false },
            keyServer: keyServer,
            subscriber: subscriber);

        var result = await coordinator.ExecuteAsync(
            CreateRequest("txn-required-false"),
            _ => Task.FromResult(new TurnMutationResult { Success = true, ResultJson = "{\"ok\":true}" }),
            CancellationToken.None).ConfigureAwait(true);

        Assert.Equal("bypassed", result.Status);
        Assert.True(result.MutationApplied);
        Assert.Contains("not required", result.Message, StringComparison.OrdinalIgnoreCase);
        await keyServer.DidNotReceive().SignManifestAsync(Arg.Any<TransactionManifestSignRequest>(), Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
        await subscriber.DidNotReceive().CommitDiffgramAsync(Arg.Any<DiffgramCommitRequest>(), Arg.Any<CancellationToken>())
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
        var subscriber = Substitute.For<ISubscriberCommitService>();
        subscriber.CommitDiffgramAsync(Arg.Any<DiffgramCommitRequest>(), Arg.Any<CancellationToken>())
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
            subscriber);

        var result = await coordinator.ExecuteAsync(
            CreateRequest("txn-commit"),
            _ => Task.FromResult(new TurnMutationResult { Success = true, ResultJson = "{\"updated\":true}" }),
            CancellationToken.None).ConfigureAwait(true);

        Assert.Equal("committed", result.Status);
        Assert.True(result.MutationApplied);
        Assert.Equal("diffgram-txn-commit", result.DiffgramId);
        await subscriber.Received(1).CommitDiffgramAsync(Arg.Any<DiffgramCommitRequest>(), Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
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
        var coordinator = CreateCoordinator(
            new TurnTransactionOptions { Enabled = true },
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
    }

    /// <summary>Subscriber dependency failure enters degraded mode when configured.</summary>
    [Fact]
    public async Task ExecuteAsync_WhenSubscriberUnavailable_EntersDegradedMode()
    {
        var subscriber = Substitute.For<ISubscriberCommitService>();
        subscriber.CommitDiffgramAsync(Arg.Any<DiffgramCommitRequest>(), Arg.Any<CancellationToken>())
            .Returns(new DiffgramCommitResponse
            {
                TransactionId = "txn-degraded",
                Status = "rejected",
                Reason = TransactionFailureReason.SubscriberUnavailable,
            });
        var audit = new InMemoryTransactionAuditWriter();
        var coordinator = CreateCoordinator(
            new TurnTransactionOptions { Enabled = true, DegradedModeEnabled = true },
            subscriber: subscriber,
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

    /// <summary>Subscriber timeout is mapped to degraded mode when configured.</summary>
    [Fact]
    public async Task ExecuteAsync_WhenSubscriberCommitTimesOut_EntersDegradedMode()
    {
        var subscriber = Substitute.For<ISubscriberCommitService>();
        subscriber.CommitDiffgramAsync(Arg.Any<DiffgramCommitRequest>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                await Task.Delay(TimeSpan.FromSeconds(30), callInfo.Arg<CancellationToken>()).ConfigureAwait(false);
                return new DiffgramCommitResponse();
            });
        var coordinator = CreateCoordinator(
            new TurnTransactionOptions { Enabled = true, DegradedModeEnabled = true, CommitTimeoutSeconds = 1 },
            subscriber: subscriber);

        var result = await coordinator.ExecuteAsync(
            CreateRequest("txn-timeout"),
            _ => Task.FromResult(new TurnMutationResult { Success = true }),
            CancellationToken.None).ConfigureAwait(true);

        Assert.Equal("degraded", result.Status);
        Assert.Equal(TransactionFailureReason.CommitTimeout, result.Reason);
        Assert.True(result.Degraded);
    }

    /// <summary>Status controller exposes degraded mode state after a coordinator failure.</summary>
    [Fact]
    public async Task GetStatus_AfterDegradedFailure_ReturnsCurrentState()
    {
        var subscriber = Substitute.For<ISubscriberCommitService>();
        subscriber.CommitDiffgramAsync(Arg.Any<DiffgramCommitRequest>(), Arg.Any<CancellationToken>())
            .Returns(new DiffgramCommitResponse
            {
                TransactionId = "txn-status",
                Status = "rejected",
                Reason = TransactionFailureReason.SubscriberUnavailable,
            });
        var coordinator = CreateCoordinator(
            new TurnTransactionOptions { Enabled = true, DegradedModeEnabled = true },
            subscriber: subscriber);
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
        ITransactionAuditWriter? auditWriter = null)
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
        return new TurnTransactionCoordinator(
            Monitor(options),
            registry,
            keyServer,
            subscriber,
            new JsonDiffgramBuilder(),
            new TransactionDegradedModePolicy(Monitor(options)),
            auditWriter ?? new InMemoryTransactionAuditWriter());
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
}
