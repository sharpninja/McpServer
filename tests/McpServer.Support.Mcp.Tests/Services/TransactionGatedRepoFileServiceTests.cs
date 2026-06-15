using System.Security.Cryptography;
using System.Text;
using McpServer.Support.Mcp.Services;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-161: repo.write transaction gate tests.
/// </summary>
public sealed class TransactionGatedRepoFileServiceTests
{
    /// <summary>repo.write signs and commits before returning the write result.</summary>
    [Fact]
    public async Task WriteAsync_WhenCoordinatorCommits_BuildsTransactionAndReturnsResult()
    {
        var inner = new RecordingRepoFileService { Exists = true, Content = "before" };
        var coordinator = new CapturingCoordinator();
        var sut = CreateSut(inner, coordinator);

        var result = await sut.WriteAsync("docs/notes.md", "after", CancellationToken.None).ConfigureAwait(true);

        Assert.True(result.Written);
        Assert.Equal("after", inner.Content);
        Assert.Equal(1, inner.CaptureCalls);
        Assert.Equal(1, inner.WriteCalls);
        Assert.Equal(0, inner.RestoreCalls);
        Assert.NotNull(coordinator.Request);
        Assert.Equal("repo.write", coordinator.Request.OperationName);
        Assert.Contains("\"relativePath\":\"docs/notes.md\"", coordinator.Request.OperationBodyJson, StringComparison.Ordinal);
    }

    /// <summary>Pre-mutation repo.write transaction rejection does not write the file.</summary>
    [Fact]
    public async Task WriteAsync_WhenCoordinatorRejectsBeforeMutation_DoesNotWriteFile()
    {
        var inner = new RecordingRepoFileService { Exists = true, Content = "before" };
        var coordinator = new CapturingCoordinator
        {
            InvokeMutation = false,
            Status = "rejected",
            Reason = TransactionFailureReason.UnknownKey,
            Message = "signing failed",
        };
        var sut = CreateSut(inner, coordinator);

        var result = await sut.WriteAsync("docs/notes.md", "after", CancellationToken.None).ConfigureAwait(true);

        Assert.False(result.Written);
        Assert.Equal("before", inner.Content);
        Assert.Equal(0, inner.CaptureCalls);
        Assert.Equal(0, inner.WriteCalls);
        Assert.Equal(0, inner.RestoreCalls);
        Assert.Contains("signing failed", result.Error, StringComparison.Ordinal);
    }

    /// <summary>Post-mutation commit failure restores the previous file contents.</summary>
    [Fact]
    public async Task WriteAsync_WhenCommitFailsAfterExistingFileWrite_RestoresPriorContent()
    {
        var inner = new RecordingRepoFileService { Exists = true, Content = "before" };
        var coordinator = new CapturingCoordinator
        {
            Status = "rejected",
            Reason = TransactionFailureReason.SubscriberUnavailable,
            Message = "subscriber unavailable",
            InvokeRollback = true,
        };
        var sut = CreateSut(inner, coordinator);

        var result = await sut.WriteAsync("docs/notes.md", "after", CancellationToken.None).ConfigureAwait(true);

        Assert.False(result.Written);
        Assert.True(inner.Exists);
        Assert.Equal("before", inner.Content);
        Assert.Equal(1, inner.RestoreCalls);
        Assert.Contains("Rollback completed", result.Error, StringComparison.Ordinal);
    }

    /// <summary>Post-mutation commit failure removes a file created only by the rejected transaction.</summary>
    [Fact]
    public async Task WriteAsync_WhenCommitFailsAfterCreate_DeletesCreatedFile()
    {
        var inner = new RecordingRepoFileService { Exists = false, Content = null };
        var coordinator = new CapturingCoordinator
        {
            Status = "rejected",
            Reason = TransactionFailureReason.SubscriberUnavailable,
            Message = "subscriber unavailable",
            InvokeRollback = true,
        };
        var sut = CreateSut(inner, coordinator);

        var result = await sut.WriteAsync("docs/new.md", "created", CancellationToken.None).ConfigureAwait(true);

        Assert.False(result.Written);
        Assert.False(inner.Exists);
        Assert.Null(inner.Content);
        Assert.Equal(1, inner.RestoreCalls);
        Assert.Contains("Rollback completed", result.Error, StringComparison.Ordinal);
    }

    /// <summary>Rollback refuses to overwrite a file that changed after the transaction write.</summary>
    [Fact]
    public async Task WriteAsync_WhenRollbackSeesConcurrentEdit_ReportsRollbackFailureWithoutOverwriting()
    {
        var inner = new RecordingRepoFileService
        {
            Exists = true,
            Content = "before",
            ConcurrentEditBeforeRestore = "human edit",
        };
        var coordinator = new CapturingCoordinator
        {
            Status = "rejected",
            Reason = TransactionFailureReason.SubscriberUnavailable,
            Message = "subscriber unavailable",
            InvokeRollback = true,
        };
        var sut = CreateSut(inner, coordinator);

        var result = await sut.WriteAsync("docs/notes.md", "after", CancellationToken.None).ConfigureAwait(true);

        Assert.False(result.Written);
        Assert.Equal("human edit", inner.Content);
        Assert.Equal(1, inner.RestoreCalls);
        Assert.Contains("Rollback failed", result.Error, StringComparison.Ordinal);
        Assert.Contains("changed after transactional write", result.Error, StringComparison.Ordinal);
    }

    /// <summary>Required transaction mode fails closed when the repo provider cannot compensate writes.</summary>
    [Fact]
    public async Task WriteAsync_WhenCompensationMissingAndTransactionsRequired_FailsWithoutWriting()
    {
        var inner = new NonCompensatingRepoFileService();
        var coordinator = new CapturingCoordinator();
        var sut = new TransactionGatedRepoFileService(
            inner,
            compensation: null,
            coordinator,
            Microsoft.Extensions.Options.Options.Create(new TurnTransactionOptions { Enabled = true, RequiredForMutations = true }));

        var result = await sut.WriteAsync("docs/notes.md", "after", CancellationToken.None).ConfigureAwait(true);

        Assert.False(result.Written);
        Assert.Equal(0, inner.WriteCalls);
        Assert.Null(coordinator.Request);
        Assert.Contains("does not support transaction rollback compensation", result.Error, StringComparison.Ordinal);
    }

    /// <summary>repo.write uses the inner service directly when no coordinator is registered.</summary>
    [Fact]
    public async Task WriteAsync_WhenCoordinatorAbsent_WritesDirectly()
    {
        var inner = new RecordingRepoFileService { Exists = false, Content = null };
        var sut = new TransactionGatedRepoFileService(inner, inner);

        var result = await sut.WriteAsync("docs/direct.md", "direct", CancellationToken.None).ConfigureAwait(true);

        Assert.True(result.Written);
        Assert.True(inner.Exists);
        Assert.Equal("direct", inner.Content);
        Assert.Equal(1, inner.WriteCalls);
        Assert.Equal(0, inner.CaptureCalls);
    }

    /// <summary>Read and list operations remain pass-through and do not require coordinator transactions.</summary>
    [Fact]
    public async Task ReadAndListAsync_DelegateWithoutCoordinatorTransaction()
    {
        var inner = new RecordingRepoFileService { Exists = true, Content = "content" };
        var coordinator = new CapturingCoordinator();
        var sut = CreateSut(inner, coordinator);

        var read = await sut.ReadAsync("docs/notes.md", CancellationToken.None).ConfigureAwait(true);
        var list = await sut.ListAsync("docs", CancellationToken.None).ConfigureAwait(true);

        Assert.NotNull(read);
        Assert.Equal("content", read!.Content);
        Assert.Equal("docs", list.Path);
        Assert.Equal(1, inner.ReadCalls);
        Assert.Equal(1, inner.ListCalls);
        Assert.Null(coordinator.Request);
    }

    private static TransactionGatedRepoFileService CreateSut(
        RecordingRepoFileService inner,
        ITurnTransactionCoordinator coordinator,
        TurnTransactionOptions? options = null)
        => new(
            inner,
            inner,
            coordinator,
            Microsoft.Extensions.Options.Options.Create(options ?? new TurnTransactionOptions { Enabled = true, RequiredForMutations = true }));

    private sealed class RecordingRepoFileService : IRepoFileService, IRepoFileCompensation
    {
        public bool Exists { get; set; }

        public string? Content { get; set; }

        public string? ConcurrentEditBeforeRestore { get; init; }

        public int CaptureCalls { get; private set; }

        public int RestoreCalls { get; private set; }

        public int WriteCalls { get; private set; }

        public int ReadCalls { get; private set; }

        public int ListCalls { get; private set; }

        public Task<RepoFileReadResult?> ReadAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            ReadCalls++;
            return Task.FromResult<RepoFileReadResult?>(new RepoFileReadResult(relativePath, Content ?? string.Empty, Exists));
        }

        public Task<RepoListResult> ListAsync(string? relativePath, CancellationToken cancellationToken = default)
        {
            ListCalls++;
            return Task.FromResult(new RepoListResult(relativePath ?? ".", []));
        }

        public Task<RepoFileSnapshot?> CaptureForWriteAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            CaptureCalls++;
            return Task.FromResult<RepoFileSnapshot?>(new RepoFileSnapshot(
                relativePath,
                Exists,
                Content ?? string.Empty,
                Exists ? ComputeSha256(Content ?? string.Empty) : string.Empty));
        }

        public Task RestoreWriteAsync(
            RepoFileSnapshot snapshot,
            string writtenContent,
            CancellationToken cancellationToken = default)
        {
            RestoreCalls++;
            if (ConcurrentEditBeforeRestore is not null)
            {
                Exists = true;
                Content = ConcurrentEditBeforeRestore;
            }

            if (Exists && !string.Equals(ComputeSha256(Content ?? string.Empty), ComputeSha256(writtenContent), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("File changed after transactional write; rollback refused.");

            if (snapshot.Exists)
            {
                Exists = true;
                Content = snapshot.Content;
            }
            else
            {
                Exists = false;
                Content = null;
            }

            return Task.CompletedTask;
        }

        public Task<RepoWriteResult> WriteAsync(string relativePath, string content, CancellationToken cancellationToken = default)
        {
            WriteCalls++;
            Exists = true;
            Content = content;
            return Task.FromResult(new RepoWriteResult(true, null));
        }
    }

    private sealed class NonCompensatingRepoFileService : IRepoFileService
    {
        public int WriteCalls { get; private set; }

        public Task<RepoFileReadResult?> ReadAsync(string relativePath, CancellationToken cancellationToken = default)
            => Task.FromResult<RepoFileReadResult?>(new RepoFileReadResult(relativePath, string.Empty, false));

        public Task<RepoListResult> ListAsync(string? relativePath, CancellationToken cancellationToken = default)
            => Task.FromResult(new RepoListResult(relativePath ?? ".", []));

        public Task<RepoWriteResult> WriteAsync(string relativePath, string content, CancellationToken cancellationToken = default)
        {
            WriteCalls++;
            return Task.FromResult(new RepoWriteResult(true, null));
        }
    }

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

    private static string ComputeSha256(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
