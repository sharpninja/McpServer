using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-TXN-004 acceptance: first-party memory mutations are gated by turn transactions and compensate on failed commits.
/// </summary>
public sealed class TransactionGatedMemoryServiceTests
{
    /// <summary>The stdio host can resolve the gated memory service even when no transaction coordinator is registered.</summary>
    [Fact]
    public void ServiceProvider_WhenCoordinatorIsAbsent_ResolvesDirectFallback()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<McpDbContext>(options =>
            options.UseInMemoryDatabase($"TransactionGatedMemoryServiceTests_{Guid.NewGuid():N}"));
        services.AddScoped<IMemoryService, MemoryService>();
        services.AddScoped<ITransactionGatedMemoryService, TransactionGatedMemoryService>();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var resolved = scope.ServiceProvider.GetRequiredService<ITransactionGatedMemoryService>();

        Assert.NotNull(resolved);
    }

    /// <summary>memory.add signs and commits before returning the created memory result.</summary>
    [Fact]
    public async Task AddAsync_WhenCoordinatorCommits_BuildsTransactionAndReturnsCreatedMemory()
    {
        var service = Substitute.For<IMemoryService>();
        var created = CreateMemory("MEMORY-OPERATOR-001", "created");
        service.AddAsync(Arg.Any<MemoryAddRequest>(), Arg.Any<CancellationToken>())
            .Returns(new MemoryMutationResult(true, Memory: created));
        var coordinator = new CapturingCoordinator();
        var gated = new TransactionGatedMemoryService(service, coordinator);

        var result = await gated.AddAsync(
            new MemoryAddRequest
            {
                Id = "MEMORY-OPERATOR-001",
                Category = "operator",
                Scope = MemoryScope.Global,
                Text = "created",
                UpdatedBy = "Codex",
            },
            CancellationToken.None).ConfigureAwait(true);

        Assert.True(result.Success);
        Assert.Equal(created.Id, result.Memory?.Id);
        Assert.NotNull(coordinator.Request);
        Assert.Equal("memory.add", coordinator.Request.OperationName);
        Assert.True(coordinator.Request.Mutating);
        Assert.Contains("\"id\":\"MEMORY-OPERATOR-001\"", coordinator.Request.OperationBodyJson, StringComparison.Ordinal);
        await service.Received(1).AddAsync(Arg.Any<MemoryAddRequest>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>Pre-mutation memory.add transaction rejection does not call the memory service.</summary>
    [Fact]
    public async Task AddAsync_WhenCoordinatorRejectsBeforeMutation_DoesNotMutateAndReturnsConflict()
    {
        var service = Substitute.For<IMemoryService>();
        var coordinator = new CapturingCoordinator
        {
            InvokeMutation = false,
            Status = "rejected",
            Reason = TransactionFailureReason.UnknownKey,
            Message = "signing failed",
        };
        var gated = new TransactionGatedMemoryService(service, coordinator);

        var result = await gated.AddAsync(
            new MemoryAddRequest
            {
                Category = "operator",
                Scope = MemoryScope.Global,
                Text = "created",
            },
            CancellationToken.None).ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Equal(MemoryMutationFailureKind.Conflict, result.FailureKind);
        Assert.Contains("signing failed", result.Error, StringComparison.Ordinal);
        await service.DidNotReceiveWithAnyArgs().AddAsync(default!, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
    }

    /// <summary>memory.add rollback restores the created memory record when a subscriber rejects after mutation.</summary>
    [Fact]
    public async Task AddAsync_WhenCommitFailsAfterMutation_RestoresCreatedMemory()
    {
        using var db = CreateDb();
        var service = CreateService(db);
        var coordinator = new CapturingCoordinator
        {
            Status = "rejected",
            Reason = TransactionFailureReason.SubscriberUnavailable,
            Message = "Subscriber commit failed.",
            InvokeRollback = true,
        };
        var gated = new TransactionGatedMemoryService(service, coordinator, db);

        var result = await gated.AddAsync(
            new MemoryAddRequest
            {
                Id = "MEMORY-OPERATOR-001",
                Category = "operator",
                Scope = MemoryScope.Global,
                Text = "created",
                UpdatedBy = "Codex",
            },
            CancellationToken.None).ConfigureAwait(true);
        var visible = await service.GetAsync("MEMORY-OPERATOR-001", CancellationToken.None).ConfigureAwait(true);
        var row = await db.Memories
            .IgnoreQueryFilters()
            .SingleAsync(memory => memory.Id == "MEMORY-OPERATOR-001", CancellationToken.None)
            .ConfigureAwait(true);
        var retry = await service.AddAsync(
            new MemoryAddRequest
            {
                Id = "MEMORY-OPERATOR-001",
                Category = "operator",
                Scope = MemoryScope.Global,
                Text = "retry",
                UpdatedBy = "Codex",
            },
            CancellationToken.None).ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.NotNull(visible);
        Assert.Equal("created", visible!.Text);
        Assert.Equal(1, visible.Version);
        Assert.False(db.Entry(row).Property<bool>("IsDeleted").CurrentValue);
        Assert.False(retry.Success);
        Assert.Equal(MemoryMutationFailureKind.Conflict, retry.FailureKind);
        Assert.Contains("already exists", retry.Error, StringComparison.Ordinal);
        Assert.Contains("Rollback completed", result.Error, StringComparison.Ordinal);
    }

    /// <summary>memory.add reports rollback failure when the created memory cannot be restored after subscriber rejection.</summary>
    [Fact]
    public async Task AddAsync_WhenRollbackFails_ReportsRollbackFailure()
    {
        var service = Substitute.For<IMemoryService>();
        service.AddAsync(Arg.Any<MemoryAddRequest>(), Arg.Any<CancellationToken>())
            .Returns(new MemoryMutationResult(true, Memory: CreateMemory("MEMORY-OPERATOR-001", "created")));
        service.UpdateAsync("MEMORY-OPERATOR-001", Arg.Any<MemoryUpdateRequest>(), Arg.Any<CancellationToken>())
            .Returns(new MemoryMutationResult(false, "restore failed", FailureKind: MemoryMutationFailureKind.Conflict));
        var coordinator = new CapturingCoordinator
        {
            Status = "rejected",
            Reason = TransactionFailureReason.SubscriberUnavailable,
            Message = "Subscriber commit failed.",
            InvokeRollback = true,
        };
        var gated = new TransactionGatedMemoryService(service, coordinator);

        var result = await gated.AddAsync(
            new MemoryAddRequest
            {
                Id = "MEMORY-OPERATOR-001",
                Category = "operator",
                Scope = MemoryScope.Global,
                Text = "created",
            },
            CancellationToken.None).ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Equal(MemoryMutationFailureKind.Conflict, result.FailureKind);
        Assert.Contains("Rollback failed", result.Error, StringComparison.Ordinal);
        Assert.Contains("restore failed", result.Error, StringComparison.Ordinal);
    }

    /// <summary>memory.update signs and commits before returning the updated memory result.</summary>
    [Fact]
    public async Task UpdateAsync_WhenCoordinatorCommits_BuildsTransactionAndReturnsUpdatedMemory()
    {
        var service = Substitute.For<IMemoryService>();
        var updated = CreateMemory("MEMORY-OPERATOR-001", "updated");
        service.GetAsync(updated.Id, Arg.Any<CancellationToken>())
            .Returns(CreateMemory(updated.Id, "previous"));
        service.UpdateAsync(updated.Id, Arg.Any<MemoryUpdateRequest>(), Arg.Any<CancellationToken>())
            .Returns(new MemoryMutationResult(true, Memory: updated));
        var coordinator = new CapturingCoordinator();
        var gated = new TransactionGatedMemoryService(service, coordinator);

        var result = await gated.UpdateAsync(
            updated.Id,
            new MemoryUpdateRequest { Text = "updated" },
            CancellationToken.None).ConfigureAwait(true);

        Assert.True(result.Success);
        Assert.Equal(updated.Id, result.Memory?.Id);
        Assert.NotNull(coordinator.Request);
        Assert.Equal("memory.update", coordinator.Request.OperationName);
        Assert.True(coordinator.Request.Mutating);
        Assert.Contains("\"id\":\"MEMORY-OPERATOR-001\"", coordinator.Request.OperationBodyJson, StringComparison.Ordinal);
        await service.Received(1).UpdateAsync(updated.Id, Arg.Any<MemoryUpdateRequest>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>Pre-mutation transaction rejection does not call the memory service.</summary>
    [Fact]
    public async Task UpdateAsync_WhenCoordinatorRejectsBeforeMutation_DoesNotMutateAndReturnsConflict()
    {
        var service = Substitute.For<IMemoryService>();
        var coordinator = new CapturingCoordinator
        {
            InvokeMutation = false,
            Status = "rejected",
            Reason = TransactionFailureReason.UnknownKey,
            Message = "signing failed",
        };
        var gated = new TransactionGatedMemoryService(service, coordinator);

        var result = await gated.UpdateAsync(
            "MEMORY-OPERATOR-001",
            new MemoryUpdateRequest { Text = "updated" },
            CancellationToken.None).ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Equal(MemoryMutationFailureKind.Conflict, result.FailureKind);
        Assert.Contains("signing failed", result.Error, StringComparison.Ordinal);
        await service.DidNotReceiveWithAnyArgs().GetAsync(default!, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        await service.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default!, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
    }

    /// <summary>memory.update rollback restores the prior visible memory fields and version.</summary>
    [Fact]
    public async Task UpdateAsync_WhenCommitFailsAfterMutation_RestoresPriorMemoryExactly()
    {
        using var db = CreateDb();
        var service = CreateService(db);
        var created = await service.AddAsync(new MemoryAddRequest
        {
            Id = "MEMORY-OPERATOR-001",
            Category = "operator",
            Scope = MemoryScope.Global,
            Text = "old",
            UpdatedBy = "Codex",
        }, CancellationToken.None).ConfigureAwait(true);
        Assert.True(created.Success);
        var coordinator = new CapturingCoordinator
        {
            Status = "rejected",
            Reason = TransactionFailureReason.SubscriberUnavailable,
            Message = "Subscriber commit failed.",
            InvokeRollback = true,
        };
        var gated = new TransactionGatedMemoryService(service, coordinator, db);

        var result = await gated.UpdateAsync(
            "MEMORY-OPERATOR-001",
            new MemoryUpdateRequest { Text = "new" },
            CancellationToken.None).ConfigureAwait(true);
        var restored = await service.GetAsync("MEMORY-OPERATOR-001", CancellationToken.None).ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.NotNull(restored);
        Assert.Equal("old", restored!.Text);
        Assert.Equal(1, restored.Version);
        Assert.Equal(created.Memory!.UpdatedAtUtc, restored.UpdatedAtUtc);
    }

    /// <summary>memory.remove rollback makes the soft-deleted memory visible again.</summary>
    [Fact]
    public async Task RemoveAsync_WhenCommitFailsAfterMutation_RestoresSoftDeletedMemoryExactly()
    {
        using var db = CreateDb();
        var service = CreateService(db);
        var created = await service.AddAsync(new MemoryAddRequest
        {
            Id = "MEMORY-OPERATOR-001",
            Category = "operator",
            Scope = MemoryScope.Global,
            Text = "old",
            UpdatedBy = "Codex",
        }, CancellationToken.None).ConfigureAwait(true);
        Assert.True(created.Success);
        var coordinator = new CapturingCoordinator
        {
            Status = "rejected",
            Reason = TransactionFailureReason.SubscriberUnavailable,
            Message = "Subscriber commit failed.",
            InvokeRollback = true,
        };
        var gated = new TransactionGatedMemoryService(service, coordinator, db);

        var result = await gated.RemoveAsync("MEMORY-OPERATOR-001", CancellationToken.None).ConfigureAwait(true);
        var restored = await service.GetAsync("MEMORY-OPERATOR-001", CancellationToken.None).ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.NotNull(restored);
        Assert.Equal("old", restored!.Text);
        Assert.Equal(1, restored.Version);
        Assert.Equal(created.Memory!.CreatedAtUtc, restored.CreatedAtUtc);
    }

    private static MemoryItem CreateMemory(
        string id,
        string text,
        MemoryScope scope = MemoryScope.Global,
        string category = "OPERATOR")
        => new()
        {
            Id = id,
            Category = category,
            Scope = scope,
            Text = text,
            Version = 1,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        };

    private static McpDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"TransactionGatedMemoryServiceTests_{Guid.NewGuid():N}")
            .Options;
        var db = new McpDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static MemoryService CreateService(McpDbContext db)
        => new(db, NullLogger<MemoryService>.Instance);

    private sealed class CapturingCoordinator : ITurnTransactionCoordinator
    {
        public TurnTransactionRequest? Request { get; private set; }

        public bool InvokeMutation { get; init; } = true;

        public bool InvokeRollback { get; init; }

        public string Status { get; init; } = "committed";

        public TransactionFailureReason Reason { get; init; } = TransactionFailureReason.None;

        public string? Message { get; init; }

        public async Task<TurnTransactionResult> ExecuteAsync(
            TurnTransactionRequest request,
            Func<CancellationToken, Task<TurnMutationResult>> mutation,
            CancellationToken cancellationToken = default)
        {
            Request = request;
            TurnMutationResult? mutationResult = null;
            var rollbackAttempted = false;
            var rollbackSucceeded = false;
            string? rollbackError = null;

            if (InvokeMutation)
            {
                mutationResult = await mutation(cancellationToken).ConfigureAwait(false);
                if (InvokeRollback && mutationResult.RollbackAsync is not null)
                {
                    rollbackAttempted = true;
                    try
                    {
                        await mutationResult.RollbackAsync(cancellationToken).ConfigureAwait(false);
                        rollbackSucceeded = true;
                    }
                    catch (Exception ex)
                    {
                        rollbackError = ex.Message;
                    }
                }
            }

            return new TurnTransactionResult
            {
                TransactionId = request.TransactionId ?? "txn-test",
                Status = Status,
                Reason = Reason,
                MutationApplied = InvokeMutation,
                MutationResult = mutationResult,
                Message = Message,
                RollbackAttempted = rollbackAttempted,
                RollbackSucceeded = rollbackSucceeded,
                RollbackError = rollbackError,
            };
        }

        public TurnTransactionStatusResponse GetStatus()
            => new()
            {
                Enabled = true,
                Degraded = false,
                LastReason = TransactionFailureReason.None,
                Message = "Turn transactions are available.",
            };
    }
}
