using McpServer.Support.Mcp.Requirements;
using McpServer.Support.Mcp.Requirements.Models;
using McpServer.Support.Mcp.Services;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-161: Requirements repository mutations are gated by turn transactions and restore snapshots on failed commits.
/// </summary>
public sealed class TransactionGatedRequirementsDocumentServiceTests
{
    /// <summary>requirements.fr.add signs and commits before returning from the create call.</summary>
    [Fact]
    public async Task AddFrAsync_WhenCoordinatorCommits_BuildsTransactionAndAddsRequirement()
    {
        var inner = new RecordingRequirementsDocumentService();
        var coordinator = new CapturingCoordinator();
        var sut = CreateSut(inner, coordinator);

        await sut.AddFrAsync(new FrEntry("FR-MCP-900", "FR", "created"), CancellationToken.None)
            .ConfigureAwait(true);

        Assert.Equal("created", (await sut.GetFrAsync("FR-MCP-900", CancellationToken.None).ConfigureAwait(true))?.Body);
        Assert.Equal(1, inner.CaptureCalls);
        Assert.Equal(0, inner.RestoreCalls);
        Assert.NotNull(coordinator.Request);
        Assert.Equal("requirements.fr.add", coordinator.Request.OperationName);
        Assert.True(coordinator.Request.Mutating);
        Assert.Contains("\"id\":\"FR-MCP-900\"", coordinator.Request.OperationBodyJson, StringComparison.Ordinal);
    }

    /// <summary>Pre-mutation rejection prevents any requirements repository mutation.</summary>
    [Fact]
    public async Task UpdateFrAsync_WhenCoordinatorRejectsBeforeMutation_DoesNotMutate()
    {
        var inner = new RecordingRequirementsDocumentService();
        await inner.AddFrAsync(new FrEntry("FR-MCP-900", "FR", "old"), CancellationToken.None).ConfigureAwait(true);
        var coordinator = new CapturingCoordinator
        {
            InvokeMutation = false,
            Status = "rejected",
            Reason = TransactionFailureReason.UnknownKey,
            Message = "signing failed",
        };
        var sut = CreateSut(inner, coordinator);

        var ex = await Assert.ThrowsAsync<RequirementsConflictException>(() =>
            sut.UpdateFrAsync(new FrEntry("FR-MCP-900", "FR", "new"), CancellationToken.None)).ConfigureAwait(true);

        Assert.Equal("old", (await inner.GetFrAsync("FR-MCP-900", CancellationToken.None).ConfigureAwait(true))?.Body);
        Assert.Equal(0, inner.UpdateFrCalls);
        Assert.Equal(0, inner.RestoreCalls);
        Assert.Contains("signing failed", ex.Message, StringComparison.Ordinal);
    }

    /// <summary>Post-mutation commit failure restores the captured requirements snapshot.</summary>
    [Fact]
    public async Task UpdateFrAsync_WhenCommitFailsAfterMutation_RestoresPriorSnapshot()
    {
        var inner = new RecordingRequirementsDocumentService();
        await inner.AddFrAsync(new FrEntry("FR-MCP-900", "FR", "old"), CancellationToken.None).ConfigureAwait(true);
        var coordinator = new CapturingCoordinator
        {
            Status = "rejected",
            Reason = TransactionFailureReason.SubscriberUnavailable,
            Message = "subscriber unavailable",
            InvokeRollback = true,
        };
        var sut = CreateSut(inner, coordinator);

        var ex = await Assert.ThrowsAsync<RequirementsConflictException>(() =>
            sut.UpdateFrAsync(new FrEntry("FR-MCP-900", "FR", "new"), CancellationToken.None)).ConfigureAwait(true);

        Assert.Equal("old", (await inner.GetFrAsync("FR-MCP-900", CancellationToken.None).ConfigureAwait(true))?.Body);
        Assert.Equal(1, inner.UpdateFrCalls);
        Assert.Equal(1, inner.RestoreCalls);
        Assert.Contains("Rollback completed", ex.Message, StringComparison.Ordinal);
    }

    /// <summary>Required transaction mode fails closed when requirements storage cannot compensate writes.</summary>
    [Fact]
    public async Task AddFrAsync_WhenCompensationMissingAndTransactionsRequired_FailsWithoutMutating()
    {
        var inner = new NonCompensatingRequirementsDocumentService();
        var coordinator = new CapturingCoordinator();
        var sut = new TransactionGatedRequirementsDocumentService(
            inner,
            compensation: null,
            coordinator,
            Microsoft.Extensions.Options.Options.Create(new TurnTransactionOptions { Enabled = true, RequiredForMutations = true }));

        var ex = await Assert.ThrowsAsync<RequirementsConflictException>(() =>
            sut.AddFrAsync(new FrEntry("FR-MCP-900", "FR", "created"), CancellationToken.None)).ConfigureAwait(true);

        Assert.Equal(0, inner.AddFrCalls);
        Assert.Null(coordinator.Request);
        Assert.Contains("does not support transaction rollback compensation", ex.Message, StringComparison.Ordinal);
    }

    /// <summary>Read-only operations pass through without opening coordinator transactions.</summary>
    [Fact]
    public async Task ReadOperations_DelegateWithoutCoordinatorTransaction()
    {
        var inner = new RecordingRequirementsDocumentService();
        await inner.AddFrAsync(new FrEntry("FR-MCP-900", "FR", "body"), CancellationToken.None).ConfigureAwait(true);
        var coordinator = new CapturingCoordinator();
        var sut = CreateSut(inner, coordinator);

        var fr = await sut.GetFrAsync("FR-MCP-900", CancellationToken.None).ConfigureAwait(true);
        var all = await sut.GetAllFrAsync(CancellationToken.None).ConfigureAwait(true);
        var doc = await sut.GenerateDocumentAsync(RequirementsDocType.Functional, CancellationToken.None).ConfigureAwait(true);

        Assert.Equal("FR-MCP-900", fr?.Id);
        Assert.Single(all);
        Assert.Contains("FR-MCP-900", doc.Content, StringComparison.Ordinal);
        Assert.Null(coordinator.Request);
    }

    /// <summary>
    /// TEST-MCP-REP-001: Repair purge bypasses the transaction gate so polluted workspaces can be cleaned.
    /// </summary>
    [Fact]
    public async Task PurgeInvalidPlaceholdersAsync_BypassesCoordinatorAndPurgesInvalidPlaceholders()
    {
        var inner = new RecordingRequirementsDocumentService();
        // Seed some canonical + one bad placeholder
        await inner.AddFrAsync(new FrEntry("FR-MCP-001", "Good", "ok"), CancellationToken.None).ConfigureAwait(true);
        await inner.AddFrAsync(new FrEntry("FR-SOCIAL-*", "Bad", "Placeholder requirement backfilled for TODO link FR-SOCIAL-*."), CancellationToken.None).ConfigureAwait(true);
        var coordinator = new CapturingCoordinator();
        var sut = CreateSut(inner, coordinator);

        var purged = await sut.PurgeInvalidPlaceholdersAsync(CancellationToken.None).ConfigureAwait(true);

        Assert.Equal(1, purged);
        Assert.Equal(1, inner.PurgeCalls);
        Assert.Equal(0, (await sut.GetAllFrAsync(CancellationToken.None).ConfigureAwait(true)).Count(e => e.Id.Contains("*")));
        Assert.Null(coordinator.Request);
    }

    /// <summary>requirements.export.generateAll signs and commits before returning the export result.</summary>
    [Fact]
    public async Task GenerateAllAsync_WhenCoordinatorCommits_BuildsTransactionAndReturnsExport()
    {
        using var temp = new TempDirectory();
        var inner = new FileWritingRequirementsDocumentService();
        var coordinator = new CapturingCoordinator();
        var sut = CreateSut(inner, coordinator);

        var result = await sut.GenerateAllAsync(temp.Path, ct: CancellationToken.None).ConfigureAwait(true);

        Assert.True(result.Success);
        Assert.True(File.Exists(Path.Combine(temp.Path, "Functional-Requirements.md")));
        Assert.NotNull(coordinator.Request);
        Assert.Equal("requirements.export.generateAll", coordinator.Request.OperationName);
        Assert.True(coordinator.Request.Mutating);
        Assert.Contains("\"format\":\"markdown\"", coordinator.Request.OperationBodyJson, StringComparison.Ordinal);
        Assert.DoesNotContain("generated requirements", coordinator.Request.OperationBodyJson, StringComparison.Ordinal);
    }

    /// <summary>Post-mutation export commit failure restores overwritten files and removes transaction-created files.</summary>
    [Fact]
    public async Task GenerateWikiAsync_WhenCommitFailsAfterMutation_RestoresExportFiles()
    {
        using var temp = new TempDirectory();
        var functionalPath = Path.Combine(temp.Path, "azure", "Functional-Requirements.md");
        var stalePath = Path.Combine(temp.Path, "azure", "Stale.md");
        Directory.CreateDirectory(Path.GetDirectoryName(functionalPath)!);
        await File.WriteAllTextAsync(functionalPath, "old functional").ConfigureAwait(true);
        await File.WriteAllTextAsync(stalePath, "stale content").ConfigureAwait(true);
        var inner = new FileWritingRequirementsDocumentService();
        var coordinator = new CapturingCoordinator
        {
            Status = "rejected",
            Reason = TransactionFailureReason.SubscriberUnavailable,
            Message = "subscriber unavailable",
            InvokeRollback = true,
        };
        var sut = CreateSut(inner, coordinator);

        var ex = await Assert.ThrowsAsync<RequirementsConflictException>(() =>
            sut.GenerateWikiAsync(temp.Path, ct: CancellationToken.None)).ConfigureAwait(true);

        Assert.Equal("old functional", await File.ReadAllTextAsync(functionalPath).ConfigureAwait(true));
        Assert.Equal("stale content", await File.ReadAllTextAsync(stalePath).ConfigureAwait(true));
        Assert.False(File.Exists(Path.Combine(temp.Path, "azure", "Requirements-Matrix.md")));
        Assert.Contains("Rollback completed", ex.Message, StringComparison.Ordinal);
    }

    /// <summary>Export rollback refuses to overwrite files changed after the transaction write.</summary>
    [Fact]
    public async Task GenerateAllAsync_WhenRollbackSeesConcurrentEdit_ReportsRollbackFailureWithoutOverwriting()
    {
        using var temp = new TempDirectory();
        var functionalPath = Path.Combine(temp.Path, "Functional-Requirements.md");
        await File.WriteAllTextAsync(functionalPath, "old functional").ConfigureAwait(true);
        var inner = new FileWritingRequirementsDocumentService();
        var coordinator = new CapturingCoordinator
        {
            Status = "rejected",
            Reason = TransactionFailureReason.SubscriberUnavailable,
            Message = "subscriber unavailable",
            InvokeRollback = true,
            BeforeRollbackAsync = () => File.WriteAllTextAsync(functionalPath, "human edit"),
        };
        var sut = CreateSut(inner, coordinator);

        var ex = await Assert.ThrowsAsync<RequirementsConflictException>(() =>
            sut.GenerateAllAsync(temp.Path, ct: CancellationToken.None)).ConfigureAwait(true);

        Assert.Equal("human edit", await File.ReadAllTextAsync(functionalPath).ConfigureAwait(true));
        Assert.Contains("Rollback failed", ex.Message, StringComparison.Ordinal);
        Assert.Contains("changed after transactional export", ex.Message, StringComparison.Ordinal);
    }

    private static TransactionGatedRequirementsDocumentService CreateSut(
        RecordingRequirementsDocumentService inner,
        ITurnTransactionCoordinator coordinator,
        TurnTransactionOptions? options = null)
        => new(
            inner,
            inner,
            coordinator,
            Microsoft.Extensions.Options.Options.Create(options ?? new TurnTransactionOptions { Enabled = true, RequiredForMutations = true }));

    private class RecordingRequirementsDocumentService : IRequirementsDocumentService, IRequirementsCompensation
    {
        private RequirementsCompensationSnapshot _state = RequirementsCompensationSnapshot.Empty;

        public int CaptureCalls { get; private set; }

        public int RestoreCalls { get; private set; }

        public int UpdateFrCalls { get; private set; }

        public int PurgeCalls { get; private set; }

        public Task<RequirementsCompensationSnapshot> CaptureRequirementsSnapshotAsync(CancellationToken cancellationToken = default)
        {
            CaptureCalls++;
            return Task.FromResult(_state);
        }

        public Task RestoreRequirementsSnapshotAsync(RequirementsCompensationSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            RestoreCalls++;
            _state = snapshot;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<FrEntry>> GetAllFrAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<FrEntry>>(_state.Functional);

        public Task<IReadOnlyList<FrEntry>> QueryFrAsync(string? area = null, string? status = null, CancellationToken ct = default)
        {
            var filtered = _state.Functional.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(area))
                filtered = filtered.Where(entry => string.Equals(GetSegment(entry.Id, 1), area, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(status))
                filtered = filtered.Where(entry => string.Equals(entry.Status, status, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult<IReadOnlyList<FrEntry>>(filtered.ToArray());
        }

        public Task<FrEntry?> GetFrAsync(string id, CancellationToken ct = default)
            => Task.FromResult(_state.Functional.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase)));

        public virtual Task AddFrAsync(FrEntry entry, CancellationToken ct = default)
        {
            _state = _state with { Functional = [.. _state.Functional, entry] };
            return Task.CompletedTask;
        }

        public Task UpdateFrAsync(FrEntry entry, CancellationToken ct = default)
        {
            UpdateFrCalls++;
            _state = _state with
            {
                Functional = _state.Functional
                    .Select(existing => string.Equals(existing.Id, entry.Id, StringComparison.OrdinalIgnoreCase) ? entry : existing)
                    .ToArray()
            };
            return Task.CompletedTask;
        }

        public Task DeleteFrAsync(string id, CancellationToken ct = default)
        {
            _state = _state with
            {
                Functional = _state.Functional
                    .Where(existing => !string.Equals(existing.Id, id, StringComparison.OrdinalIgnoreCase))
                    .ToArray()
            };
            return Task.CompletedTask;
        }

        public Task<int> PurgeInvalidPlaceholdersAsync(CancellationToken ct = default)
        {
            PurgeCalls++;
            var before = _state.Functional.Count;
            var valid = _state.Functional
                .Where(e => !string.IsNullOrEmpty(e.Id) && System.Text.RegularExpressions.Regex.IsMatch(e.Id, @"^FR-[A-Z0-9]+-\d+$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                .ToArray();
            if (valid.Length != before)
            {
                _state = _state with { Functional = valid };
            }
            return Task.FromResult(before - valid.Length);
        }

        public Task<IReadOnlyList<TrEntry>> GetAllTrAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<TrEntry>>(_state.Technical);

        public Task<IReadOnlyList<TrEntry>> QueryTrAsync(string? area = null, string? subarea = null, string? status = null, CancellationToken ct = default)
        {
            var filtered = _state.Technical.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(area))
                filtered = filtered.Where(entry => string.Equals(GetSegment(entry.Id, 1), area, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(subarea))
                filtered = filtered.Where(entry => string.Equals(GetSegment(entry.Id, 2), subarea, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(status))
                filtered = filtered.Where(entry => string.Equals(entry.Status, status, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult<IReadOnlyList<TrEntry>>(filtered.ToArray());
        }

        public Task<TrEntry?> GetTrAsync(string id, CancellationToken ct = default)
            => Task.FromResult(_state.Technical.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase)));

        public Task AddTrAsync(TrEntry entry, CancellationToken ct = default)
        {
            _state = _state with { Technical = [.. _state.Technical, entry] };
            return Task.CompletedTask;
        }

        public Task UpdateTrAsync(TrEntry entry, CancellationToken ct = default)
        {
            _state = _state with
            {
                Technical = _state.Technical
                    .Select(existing => string.Equals(existing.Id, entry.Id, StringComparison.OrdinalIgnoreCase) ? entry : existing)
                    .ToArray()
            };
            return Task.CompletedTask;
        }

        public Task DeleteTrAsync(string id, CancellationToken ct = default)
        {
            _state = _state with
            {
                Technical = _state.Technical
                    .Where(existing => !string.Equals(existing.Id, id, StringComparison.OrdinalIgnoreCase))
                    .ToArray()
            };
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<TestEntry>> GetAllTestAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<TestEntry>>(_state.Testing);

        public Task<IReadOnlyList<TestEntry>> QueryTestAsync(string? area = null, string? status = null, CancellationToken ct = default)
        {
            var filtered = _state.Testing.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(area))
                filtered = filtered.Where(entry => string.Equals(GetSegment(entry.Id, 1), area, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(status))
                filtered = filtered.Where(entry => string.Equals(entry.Status, status, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult<IReadOnlyList<TestEntry>>(filtered.ToArray());
        }

        public Task<TestEntry?> GetTestAsync(string id, CancellationToken ct = default)
            => Task.FromResult(_state.Testing.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase)));

        public Task AddTestAsync(TestEntry entry, CancellationToken ct = default)
        {
            _state = _state with { Testing = [.. _state.Testing, entry] };
            return Task.CompletedTask;
        }

        public Task UpdateTestAsync(TestEntry entry, CancellationToken ct = default)
        {
            _state = _state with
            {
                Testing = _state.Testing
                    .Select(existing => string.Equals(existing.Id, entry.Id, StringComparison.OrdinalIgnoreCase) ? entry : existing)
                    .ToArray()
            };
            return Task.CompletedTask;
        }

        public Task DeleteTestAsync(string id, CancellationToken ct = default)
        {
            _state = _state with
            {
                Testing = _state.Testing
                    .Where(existing => !string.Equals(existing.Id, id, StringComparison.OrdinalIgnoreCase))
                    .ToArray()
            };
            return Task.CompletedTask;
        }

        private static string? GetSegment(string id, int index)
        {
            var parts = id.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return index < parts.Length ? parts[index] : null;
        }

        public Task<RequirementsBatchEntries> AddBatchAsync(RequirementsBatchEntries entries, CancellationToken ct = default)
        {
            _state = _state with
            {
                Functional = [.. _state.Functional, .. entries.Functional],
                Technical = [.. _state.Technical, .. entries.Technical],
                Testing = [.. _state.Testing, .. entries.Testing],
            };
            return Task.FromResult(entries);
        }

        public Task<RequirementsBatchEntries> UpdateBatchAsync(RequirementsBatchEntries entries, CancellationToken ct = default)
            => Task.FromResult(entries);

        public Task<IReadOnlyList<FrTrMapping>> GetAllMappingsAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<FrTrMapping>>(_state.Mappings);

        public Task<FrTrMapping?> GetMappingAsync(string frId, CancellationToken ct = default)
            => Task.FromResult(_state.Mappings.FirstOrDefault(x => string.Equals(x.FrId, frId, StringComparison.OrdinalIgnoreCase)));

        public Task UpsertMappingAsync(FrTrMapping mapping, CancellationToken ct = default)
        {
            _state = _state with
            {
                Mappings =
                [
                    .. _state.Mappings.Where(existing => !string.Equals(existing.FrId, mapping.FrId, StringComparison.OrdinalIgnoreCase)),
                    mapping,
                ]
            };
            return Task.CompletedTask;
        }

        public Task DeleteMappingAsync(string frId, CancellationToken ct = default)
        {
            _state = _state with
            {
                Mappings = _state.Mappings
                    .Where(existing => !string.Equals(existing.FrId, frId, StringComparison.OrdinalIgnoreCase))
                    .ToArray()
            };
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<RequirementScopeLayerEntry>> GetRequirementLayersAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<RequirementScopeLayerEntry>>([DefaultLayer()]);

        public Task<RequirementScopeLayerEntry> CreateRequirementLayerAsync(RequirementScopeLayerEntry entry, CancellationToken ct = default)
            => Task.FromResult(entry);

        public Task<RequirementScopeLayerEntry> UpdateRequirementLayerAsync(RequirementScopeLayerUpdateRequest request, CancellationToken ct = default)
            => Task.FromResult(new RequirementScopeLayerEntry(
                request.Key,
                request.Order ?? 1,
                request.Name ?? "Layer 1",
                request.Description,
                request.ScopeEndLayerKey));

        public Task<RequirementScopeLayerEntry> GetWorkspaceCurrentRequirementLayerAsync(CancellationToken ct = default)
            => Task.FromResult(DefaultLayer());

        public Task<RequirementScopeLayerEntry> SetWorkspaceCurrentRequirementLayerAsync(string layerKey, CancellationToken ct = default)
            => Task.FromResult(DefaultLayer() with { Key = layerKey });

        public Task<EffectiveRequirementsResult> GetEffectiveRequirementsAsync(string? layerKey = null, CancellationToken ct = default)
            => Task.FromResult(new EffectiveRequirementsResult(
                DefaultLayer() with { Key = layerKey ?? RequirementScopeLayerDefaults.DefaultLayerKey },
                _state.Functional,
                _state.Technical,
                _state.Testing,
                _state.Mappings));

        private static RequirementScopeLayerEntry DefaultLayer()
            => new(RequirementScopeLayerDefaults.DefaultLayerKey, 1, "Layer 1");

        public Task<(string Content, string MimeType)> GenerateDocumentAsync(RequirementsDocType docType, CancellationToken ct = default)
            => Task.FromResult(($"# Requirements\n\n{string.Join("\n", _state.Functional.Select(x => x.Id))}", "text/markdown"));

        public virtual Task<RequirementsDocumentExportResult> GenerateAllAsync(string outputRootPath, DateTimeOffset? generatedAtUtc = null, CancellationToken ct = default)
            => Task.FromResult(new RequirementsDocumentExportResult { Success = true, OutputRoot = outputRootPath });

        public virtual Task<RequirementsDocumentExportResult> GenerateWikiAsync(string outputRootPath, DateTimeOffset? generatedAtUtc = null, CancellationToken ct = default)
            => Task.FromResult(new RequirementsDocumentExportResult { Success = true, OutputRoot = outputRootPath, Format = "wiki" });
    }

    private sealed class FileWritingRequirementsDocumentService : RecordingRequirementsDocumentService
    {
        public override async Task<RequirementsDocumentExportResult> GenerateAllAsync(
            string outputRootPath,
            DateTimeOffset? generatedAtUtc = null,
            CancellationToken ct = default)
        {
            Directory.CreateDirectory(outputRootPath);
            var fullPath = Path.Combine(outputRootPath, "Functional-Requirements.md");
            await File.WriteAllTextAsync(fullPath, "generated requirements", ct).ConfigureAwait(false);
            return new RequirementsDocumentExportResult
            {
                Success = true,
                Format = "markdown",
                DocType = "all",
                OutputRoot = outputRootPath,
                GeneratedAtUtc = generatedAtUtc ?? DateTimeOffset.UtcNow,
                Files =
                [
                    new RequirementsDocumentExportFile
                    {
                        RelativePath = "Functional-Requirements.md",
                        FullPath = fullPath,
                        ContentType = "text/markdown",
                        LastModifiedUtc = generatedAtUtc ?? DateTimeOffset.UtcNow,
                    }
                ],
            };
        }

        public override async Task<RequirementsDocumentExportResult> GenerateWikiAsync(
            string outputRootPath,
            DateTimeOffset? generatedAtUtc = null,
            CancellationToken ct = default)
        {
            var azure = Path.Combine(outputRootPath, "azure");
            Directory.CreateDirectory(azure);
            var functionalPath = Path.Combine(azure, "Functional-Requirements.md");
            var matrixPath = Path.Combine(azure, "Requirements-Matrix.md");
            await File.WriteAllTextAsync(functionalPath, "new functional", ct).ConfigureAwait(false);
            await File.WriteAllTextAsync(matrixPath, "new matrix", ct).ConfigureAwait(false);
            foreach (var file in Directory.EnumerateFiles(azure, "*", SearchOption.AllDirectories)
                         .Where(file => !string.Equals(file, functionalPath, StringComparison.OrdinalIgnoreCase) &&
                                        !string.Equals(file, matrixPath, StringComparison.OrdinalIgnoreCase)))
            {
                File.Delete(file);
            }

            return new RequirementsDocumentExportResult
            {
                Success = true,
                Format = "wiki",
                DocType = "all",
                OutputRoot = outputRootPath,
                GeneratedAtUtc = generatedAtUtc ?? DateTimeOffset.UtcNow,
                Files =
                [
                    new RequirementsDocumentExportFile
                    {
                        RelativePath = "azure/Functional-Requirements.md",
                        FullPath = functionalPath,
                        ContentType = "text/markdown",
                        LastModifiedUtc = generatedAtUtc ?? DateTimeOffset.UtcNow,
                    },
                    new RequirementsDocumentExportFile
                    {
                        RelativePath = "azure/Requirements-Matrix.md",
                        FullPath = matrixPath,
                        ContentType = "text/markdown",
                        LastModifiedUtc = generatedAtUtc ?? DateTimeOffset.UtcNow,
                    }
                ],
            };
        }
    }

    private sealed class NonCompensatingRequirementsDocumentService : RecordingRequirementsDocumentService, IRequirementsDocumentService
    {
        public int AddFrCalls { get; private set; }

        public override Task AddFrAsync(FrEntry entry, CancellationToken ct = default)
        {
            AddFrCalls++;
            return base.AddFrAsync(entry, ct);
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

        public Func<Task>? BeforeRollbackAsync { get; init; }

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
                        if (BeforeRollbackAsync is not null)
                            await BeforeRollbackAsync().ConfigureAwait(false);

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

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "mcp-req-export-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            try
            {
                Directory.Delete(Path, recursive: true);
            }
            catch
            {
            }
        }
    }
}
