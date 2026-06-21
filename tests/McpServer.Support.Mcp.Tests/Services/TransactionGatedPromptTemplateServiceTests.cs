using System.Security.Cryptography;
using System.Text;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-161: Prompt-template mutation transaction gate tests.
/// </summary>
public sealed class TransactionGatedPromptTemplateServiceTests
{
    /// <summary>prompt_template_create signs and commits before returning the create result.</summary>
    [Fact]
    public async Task CreateAsync_WhenCoordinatorCommits_BuildsTransactionAndReturnsResult()
    {
        var inner = new RecordingPromptTemplateService { FileContent = "before" };
        var coordinator = new CapturingCoordinator();
        var sut = CreateSut(inner, coordinator);

        var result = await sut.CreateAsync(CreateRequest(), CancellationToken.None).ConfigureAwait(true);

        Assert.True(result.Success);
        Assert.Equal("template-alpha", result.Item?.Id);
        Assert.Equal(1, inner.CreateCalls);
        Assert.Equal(2, inner.CaptureCalls);
        Assert.Equal(0, inner.RestoreCalls);
        Assert.NotNull(coordinator.Request);
        Assert.Equal("prompt_template.create", coordinator.Request.OperationName);
        Assert.Contains("\"id\":\"template-alpha\"", coordinator.Request.OperationBodyJson, StringComparison.Ordinal);
        Assert.DoesNotContain("Hello {{name}}", coordinator.Request.OperationBodyJson, StringComparison.Ordinal);
    }

    /// <summary>Pre-mutation coordinator rejection prevents prompt-template creation.</summary>
    [Fact]
    public async Task CreateAsync_WhenCoordinatorRejectsBeforeMutation_DoesNotCreateTemplate()
    {
        var inner = new RecordingPromptTemplateService { FileContent = "before" };
        var coordinator = new CapturingCoordinator
        {
            InvokeMutation = false,
            Status = "rejected",
            Reason = TransactionFailureReason.UnknownKey,
            Message = "signing failed",
        };
        var sut = CreateSut(inner, coordinator);

        var result = await sut.CreateAsync(CreateRequest(), CancellationToken.None).ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Equal("before", inner.FileContent);
        Assert.Equal(0, inner.CreateCalls);
        Assert.Equal(0, inner.RestoreCalls);
        Assert.Contains("signing failed", result.Error, StringComparison.Ordinal);
    }

    /// <summary>Post-mutation commit failure restores the captured prompt-template file snapshot.</summary>
    [Fact]
    public async Task UpdateAsync_WhenCommitFailsAfterMutation_RestoresPriorSnapshot()
    {
        var inner = new RecordingPromptTemplateService { FileContent = "before" };
        var coordinator = new CapturingCoordinator
        {
            Status = "rejected",
            Reason = TransactionFailureReason.SubscriberUnavailable,
            Message = "subscriber unavailable",
            InvokeRollback = true,
        };
        var sut = CreateSut(inner, coordinator);

        var result = await sut.UpdateAsync("template-alpha", new PromptTemplateUpdateRequest { Title = "Updated" }, CancellationToken.None)
            .ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Equal("before", inner.FileContent);
        Assert.Equal(1, inner.UpdateCalls);
        Assert.Equal(1, inner.RestoreCalls);
        Assert.Contains("Rollback completed", result.Error, StringComparison.Ordinal);
    }

    /// <summary>Rollback refuses to overwrite prompt-template files changed after the transaction write.</summary>
    [Fact]
    public async Task DeleteAsync_WhenRollbackSeesConcurrentEdit_ReportsRollbackFailureWithoutOverwriting()
    {
        var inner = new RecordingPromptTemplateService
        {
            FileContent = "before",
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

        var result = await sut.DeleteAsync("template-alpha", CancellationToken.None).ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Equal("human edit", inner.FileContent);
        Assert.Equal(1, inner.DeleteCalls);
        Assert.Equal(1, inner.RestoreCalls);
        Assert.Contains("Rollback failed", result.Error, StringComparison.Ordinal);
        Assert.Contains("changed after transactional write", result.Error, StringComparison.Ordinal);
    }

    /// <summary>Required transaction mode fails closed when template storage cannot compensate writes.</summary>
    [Fact]
    public async Task CreateAsync_WhenCompensationMissingAndTransactionsRequired_FailsWithoutCreating()
    {
        var inner = new NonCompensatingPromptTemplateService();
        var coordinator = new CapturingCoordinator();
        var sut = new TransactionGatedPromptTemplateService(
            inner,
            compensation: null,
            coordinator,
            Microsoft.Extensions.Options.Options.Create(new TurnTransactionOptions { Enabled = true, RequiredForMutations = true }));

        var result = await sut.CreateAsync(CreateRequest(), CancellationToken.None).ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Equal(0, inner.CreateCalls);
        Assert.Null(coordinator.Request);
        Assert.Contains("does not support transaction rollback compensation", result.Error, StringComparison.Ordinal);
    }

    /// <summary>Read and render operations remain pass-through and do not require coordinator transactions.</summary>
    [Fact]
    public async Task QueryGetAndTestAsync_DelegateWithoutCoordinatorTransaction()
    {
        var inner = new RecordingPromptTemplateService { FileContent = "before" };
        var coordinator = new CapturingCoordinator();
        var sut = CreateSut(inner, coordinator);

        var query = await sut.QueryAsync(cancellationToken: CancellationToken.None).ConfigureAwait(true);
        var item = await sut.GetByIdAsync("template-alpha", CancellationToken.None).ConfigureAwait(true);
        var test = await sut.TestAsync("template-alpha", new PromptTemplateTestRequest(), CancellationToken.None).ConfigureAwait(true);
        var inline = await sut.TestInlineAsync(new PromptTemplateTestRequest { InlineTemplate = "{{x}}" }, CancellationToken.None)
            .ConfigureAwait(true);

        Assert.Equal(1, query.TotalCount);
        Assert.Equal("template-alpha", item?.Id);
        Assert.True(test.Success);
        Assert.True(inline.Success);
        Assert.Equal(1, inner.QueryCalls);
        Assert.Equal(1, inner.GetCalls);
        Assert.Equal(1, inner.TestCalls);
        Assert.Equal(1, inner.TestInlineCalls);
        Assert.Null(coordinator.Request);
    }

    private static TransactionGatedPromptTemplateService CreateSut(
        RecordingPromptTemplateService inner,
        ITurnTransactionCoordinator coordinator,
        TurnTransactionOptions? options = null)
        => new(
            inner,
            inner,
            coordinator,
            Microsoft.Extensions.Options.Options.Create(options ?? new TurnTransactionOptions { Enabled = true, RequiredForMutations = true }));

    private static PromptTemplateCreateRequest CreateRequest()
        => new()
        {
            Id = "template-alpha",
            Title = "Template Alpha",
            Category = "system",
            Content = "Hello {{name}}",
        };

    private sealed class RecordingPromptTemplateService : IPromptTemplateService, IPromptTemplateCompensation
    {
        public string? FileContent { get; set; }

        public string? ConcurrentEditBeforeRestore { get; init; }

        public int CaptureCalls { get; private set; }

        public int RestoreCalls { get; private set; }

        public int CreateCalls { get; private set; }

        public int UpdateCalls { get; private set; }

        public int DeleteCalls { get; private set; }

        public int QueryCalls { get; private set; }

        public int GetCalls { get; private set; }

        public int TestCalls { get; private set; }

        public int TestInlineCalls { get; private set; }

        public Task<PromptTemplateFileSnapshot> CaptureFileAsync(CancellationToken cancellationToken = default)
        {
            CaptureCalls++;
            return Task.FromResult(new PromptTemplateFileSnapshot(
                Exists: FileContent is not null,
                Content: FileContent,
                ContentSha256: FileContent is null ? string.Empty : ComputeSha256(FileContent)));
        }

        public Task RestoreFileAsync(
            PromptTemplateFileSnapshot snapshot,
            string expectedCurrentContentSha256,
            CancellationToken cancellationToken = default)
        {
            RestoreCalls++;
            if (ConcurrentEditBeforeRestore is not null)
                FileContent = ConcurrentEditBeforeRestore;

            var currentHash = FileContent is null ? string.Empty : ComputeSha256(FileContent);
            if (!string.Equals(currentHash, expectedCurrentContentSha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Prompt-template file changed after transactional write; rollback refused.");

            FileContent = snapshot.Exists ? snapshot.Content : null;
            return Task.CompletedTask;
        }

        public Task<PromptTemplateQueryResult> QueryAsync(
            string? category = null,
            string? tag = null,
            string? keyword = null,
            CancellationToken cancellationToken = default)
        {
            QueryCalls++;
            return Task.FromResult(new PromptTemplateQueryResult([Template()], 1));
        }

        public Task<PromptTemplate?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            GetCalls++;
            return Task.FromResult<PromptTemplate?>(Template());
        }

        public Task<PromptTemplateMutationResult> CreateAsync(
            PromptTemplateCreateRequest request,
            CancellationToken cancellationToken = default)
        {
            CreateCalls++;
            FileContent = "after-create";
            return Task.FromResult(new PromptTemplateMutationResult(true, Item: Template(request.Id)));
        }

        public Task<PromptTemplateMutationResult> UpdateAsync(
            string id,
            PromptTemplateUpdateRequest request,
            CancellationToken cancellationToken = default)
        {
            UpdateCalls++;
            FileContent = "after-update";
            return Task.FromResult(new PromptTemplateMutationResult(true, Item: Template(id)));
        }

        public Task<PromptTemplateMutationResult> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            DeleteCalls++;
            FileContent = "after-delete";
            return Task.FromResult(new PromptTemplateMutationResult(true, Item: Template(id)));
        }

        public Task<PromptTemplateTestResult> TestAsync(
            string id,
            PromptTemplateTestRequest request,
            CancellationToken cancellationToken = default)
        {
            TestCalls++;
            return Task.FromResult(new PromptTemplateTestResult { Success = true, RenderedContent = "rendered" });
        }

        public Task<PromptTemplateTestResult> TestInlineAsync(
            PromptTemplateTestRequest request,
            CancellationToken cancellationToken = default)
        {
            TestInlineCalls++;
            return Task.FromResult(new PromptTemplateTestResult { Success = true, RenderedContent = "inline" });
        }

        private static PromptTemplate Template(string id = "template-alpha")
            => new()
            {
                Id = id,
                Title = "Template Alpha",
                Category = "system",
                Content = "Hello {{name}}",
            };
    }

    private sealed class NonCompensatingPromptTemplateService : IPromptTemplateService
    {
        public int CreateCalls { get; private set; }

        public Task<PromptTemplateQueryResult> QueryAsync(
            string? category = null,
            string? tag = null,
            string? keyword = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new PromptTemplateQueryResult([], 0));

        public Task<PromptTemplate?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult<PromptTemplate?>(null);

        public Task<PromptTemplateMutationResult> CreateAsync(
            PromptTemplateCreateRequest request,
            CancellationToken cancellationToken = default)
        {
            CreateCalls++;
            return Task.FromResult(new PromptTemplateMutationResult(true, Item: Template(request.Id)));
        }

        public Task<PromptTemplateMutationResult> UpdateAsync(
            string id,
            PromptTemplateUpdateRequest request,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new PromptTemplateMutationResult(true, Item: Template(id)));

        public Task<PromptTemplateMutationResult> DeleteAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult(new PromptTemplateMutationResult(true, Item: Template(id)));

        public Task<PromptTemplateTestResult> TestAsync(
            string id,
            PromptTemplateTestRequest request,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new PromptTemplateTestResult { Success = true });

        public Task<PromptTemplateTestResult> TestInlineAsync(
            PromptTemplateTestRequest request,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new PromptTemplateTestResult { Success = true });

        private static PromptTemplate Template(string id)
            => new()
            {
                Id = id,
                Title = "Template",
                Category = "system",
                Content = "content",
            };
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
