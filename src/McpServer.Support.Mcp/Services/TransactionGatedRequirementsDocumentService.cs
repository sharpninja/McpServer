using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using McpServer.Support.Mcp.Requirements;
using McpServer.Support.Mcp.Requirements.Models;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-TXN-001: Executes requirements repository mutations through the turn transaction coordinator.
/// </summary>
public sealed class TransactionGatedRequirementsDocumentService : IRequirementsDocumentService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly string[] CanonicalRequirementsExportFiles =
    [
        "Functional-Requirements.md",
        "Technical-Requirements.md",
        "Testing-Requirements.md",
        "TR-per-FR-Mapping.md",
        "Requirements-Matrix.md",
    ];

    private static readonly string[] WikiRequirementsExportDirectories = ["azure", "github"];

    private readonly IRequirementsDocumentService _inner;
    private readonly IRequirementsCompensation? _compensation;
    private readonly ITurnTransactionCoordinator? _coordinator;
    private readonly IOptions<TurnTransactionOptions>? _transactionOptions;
    private long _lastSequence = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    /// <summary>Initializes a new instance of the <see cref="TransactionGatedRequirementsDocumentService"/> class.</summary>
    /// <param name="inner">Underlying requirements document service.</param>
    /// <param name="compensation">Optional requirements repository compensation provider.</param>
    /// <param name="coordinator">Optional turn transaction coordinator.</param>
    /// <param name="transactionOptions">Optional transaction options.</param>
    public TransactionGatedRequirementsDocumentService(
        IRequirementsDocumentService inner,
        IRequirementsCompensation? compensation = null,
        ITurnTransactionCoordinator? coordinator = null,
        IOptions<TurnTransactionOptions>? transactionOptions = null)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _compensation = compensation;
        _coordinator = coordinator;
        _transactionOptions = transactionOptions;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<FrEntry>> GetAllFrAsync(CancellationToken ct = default)
        => _inner.GetAllFrAsync(ct);

    /// <inheritdoc />
    public Task<IReadOnlyList<FrEntry>> QueryFrAsync(string? area = null, string? status = null, CancellationToken ct = default)
        => _inner.QueryFrAsync(area, status, ct);

    /// <inheritdoc />
    public Task<FrEntry?> GetFrAsync(string id, CancellationToken ct = default)
        => _inner.GetFrAsync(id, ct);

    /// <inheritdoc />
    public Task AddFrAsync(FrEntry entry, CancellationToken ct = default)
        => ExecuteMutationAsync("requirements.fr.add", EntryPayload("fr", entry), token => _inner.AddFrAsync(entry, token), ct);

    /// <inheritdoc />
    public Task UpdateFrAsync(FrEntry entry, CancellationToken ct = default)
        => ExecuteMutationAsync("requirements.fr.update", EntryPayload("fr", entry), token => _inner.UpdateFrAsync(entry, token), ct);

    /// <inheritdoc />
    public Task DeleteFrAsync(string id, CancellationToken ct = default)
        => ExecuteMutationAsync("requirements.fr.delete", new RequirementDeletePayload("fr", id), token => _inner.DeleteFrAsync(id, token), ct);

    /// <inheritdoc />
    public Task<IReadOnlyList<TrEntry>> GetAllTrAsync(CancellationToken ct = default)
        => _inner.GetAllTrAsync(ct);

    /// <inheritdoc />
    public Task<IReadOnlyList<TrEntry>> QueryTrAsync(string? area = null, string? subarea = null, string? status = null, CancellationToken ct = default)
        => _inner.QueryTrAsync(area, subarea, status, ct);

    /// <inheritdoc />
    public Task<TrEntry?> GetTrAsync(string id, CancellationToken ct = default)
        => _inner.GetTrAsync(id, ct);

    /// <inheritdoc />
    public Task AddTrAsync(TrEntry entry, CancellationToken ct = default)
        => ExecuteMutationAsync("requirements.tr.add", EntryPayload("tr", entry), token => _inner.AddTrAsync(entry, token), ct);

    /// <inheritdoc />
    public Task UpdateTrAsync(TrEntry entry, CancellationToken ct = default)
        => ExecuteMutationAsync("requirements.tr.update", EntryPayload("tr", entry), token => _inner.UpdateTrAsync(entry, token), ct);

    /// <inheritdoc />
    public Task DeleteTrAsync(string id, CancellationToken ct = default)
        => ExecuteMutationAsync("requirements.tr.delete", new RequirementDeletePayload("tr", id), token => _inner.DeleteTrAsync(id, token), ct);

    /// <inheritdoc />
    public Task<IReadOnlyList<TestEntry>> GetAllTestAsync(CancellationToken ct = default)
        => _inner.GetAllTestAsync(ct);

    /// <inheritdoc />
    public Task<IReadOnlyList<TestEntry>> QueryTestAsync(string? area = null, string? status = null, CancellationToken ct = default)
        => _inner.QueryTestAsync(area, status, ct);

    /// <inheritdoc />
    public Task<TestEntry?> GetTestAsync(string id, CancellationToken ct = default)
        => _inner.GetTestAsync(id, ct);

    /// <inheritdoc />
    public Task AddTestAsync(TestEntry entry, CancellationToken ct = default)
        => ExecuteMutationAsync("requirements.test.add", EntryPayload(entry), token => _inner.AddTestAsync(entry, token), ct);

    /// <inheritdoc />
    public Task UpdateTestAsync(TestEntry entry, CancellationToken ct = default)
        => ExecuteMutationAsync("requirements.test.update", EntryPayload(entry), token => _inner.UpdateTestAsync(entry, token), ct);

    /// <inheritdoc />
    public Task DeleteTestAsync(string id, CancellationToken ct = default)
        => ExecuteMutationAsync("requirements.test.delete", new RequirementDeletePayload("test", id), token => _inner.DeleteTestAsync(id, token), ct);

    /// <inheritdoc />
    public Task<RequirementsBatchEntries> AddBatchAsync(RequirementsBatchEntries entries, CancellationToken ct = default)
        => ExecuteMutationAsync(
            "requirements.batch.add",
            BatchPayload("add", entries),
            token => _inner.AddBatchAsync(entries, token),
            ct);

    /// <inheritdoc />
    public Task<RequirementsBatchEntries> UpdateBatchAsync(RequirementsBatchEntries entries, CancellationToken ct = default)
        => ExecuteMutationAsync(
            "requirements.batch.update",
            BatchPayload("update", entries),
            token => _inner.UpdateBatchAsync(entries, token),
            ct);

    /// <inheritdoc />
    public Task<IReadOnlyList<FrTrMapping>> GetAllMappingsAsync(CancellationToken ct = default)
        => _inner.GetAllMappingsAsync(ct);

    /// <inheritdoc />
    public Task<FrTrMapping?> GetMappingAsync(string frId, CancellationToken ct = default)
        => _inner.GetMappingAsync(frId, ct);

    /// <inheritdoc />
    public Task UpsertMappingAsync(FrTrMapping mapping, CancellationToken ct = default)
        => ExecuteMutationAsync(
            "requirements.mapping.upsert",
            new RequirementMappingPayload(mapping.FrId, mapping.TrIds, mapping.TestIds),
            token => _inner.UpsertMappingAsync(mapping, token),
            ct);

    /// <inheritdoc />
    public Task DeleteMappingAsync(string frId, CancellationToken ct = default)
        => ExecuteMutationAsync(
            "requirements.mapping.delete",
            new RequirementDeletePayload("mapping", frId),
            token => _inner.DeleteMappingAsync(frId, token),
            ct);

    /// <inheritdoc />
    public Task<IReadOnlyList<RequirementScopeLayerEntry>> GetRequirementLayersAsync(CancellationToken ct = default)
        => _inner.GetRequirementLayersAsync(ct);

    /// <inheritdoc />
    public Task<RequirementScopeLayerEntry> CreateRequirementLayerAsync(RequirementScopeLayerEntry entry, CancellationToken ct = default)
        => ExecuteMutationAsync(
            "requirements.layer.create",
            entry,
            token => _inner.CreateRequirementLayerAsync(entry, token),
            ct);

    /// <inheritdoc />
    public Task<RequirementScopeLayerEntry> UpdateRequirementLayerAsync(RequirementScopeLayerUpdateRequest request, CancellationToken ct = default)
        => ExecuteMutationAsync(
            "requirements.layer.update",
            request,
            token => _inner.UpdateRequirementLayerAsync(request, token),
            ct);

    /// <inheritdoc />
    public Task<RequirementScopeLayerEntry> GetWorkspaceCurrentRequirementLayerAsync(CancellationToken ct = default)
        => _inner.GetWorkspaceCurrentRequirementLayerAsync(ct);

    /// <inheritdoc />
    public Task<RequirementScopeLayerEntry> SetWorkspaceCurrentRequirementLayerAsync(string layerKey, CancellationToken ct = default)
        => ExecuteMutationAsync(
            "requirements.workspace.currentLayer.set",
            new RequirementLayerSelectionPayload(layerKey),
            token => _inner.SetWorkspaceCurrentRequirementLayerAsync(layerKey, token),
            ct);

    /// <inheritdoc />
    public Task<EffectiveRequirementsResult> GetEffectiveRequirementsAsync(string? layerKey = null, CancellationToken ct = default)
        => _inner.GetEffectiveRequirementsAsync(layerKey, ct);

    /// <inheritdoc />
    public Task<(string Content, string MimeType)> GenerateDocumentAsync(RequirementsDocType docType, CancellationToken ct = default)
        => _inner.GenerateDocumentAsync(docType, ct);

    /// <inheritdoc />
    public Task<RequirementsDocumentExportResult> GenerateAllAsync(string outputRootPath, DateTimeOffset? generatedAtUtc = null, CancellationToken ct = default)
        => ExecuteExportMutationAsync(
            "requirements.export.generateAll",
            new RequirementExportPayload("markdown", "all", outputRootPath, generatedAtUtc),
            outputRootPath,
            RequirementsExportSnapshotScope.Canonical,
            token => _inner.GenerateAllAsync(outputRootPath, generatedAtUtc, token),
            ct);

    /// <inheritdoc />
    public Task<RequirementsDocumentExportResult> GenerateWikiAsync(string outputRootPath, DateTimeOffset? generatedAtUtc = null, CancellationToken ct = default)
        => ExecuteExportMutationAsync(
            "requirements.export.generateWiki",
            new RequirementExportPayload("wiki", "all", outputRootPath, generatedAtUtc),
            outputRootPath,
            RequirementsExportSnapshotScope.Wiki,
            token => _inner.GenerateWikiAsync(outputRootPath, generatedAtUtc, token),
            ct);

    /// <inheritdoc />
    public Task<int> PurgeInvalidPlaceholdersAsync(CancellationToken ct = default)
    {
        // Repair is a special admin/cleanup operation to remove corrupted placeholder data.
        // Bypass the turn transaction gate so it can always run (even outside an active turn)
        // to unblock polluted workspaces. Other mutations remain gated.
        return _inner.PurgeInvalidPlaceholdersAsync(ct);
    }

    private async Task ExecuteMutationAsync(
        string operationName,
        object operationBody,
        Func<CancellationToken, Task> mutation,
        CancellationToken cancellationToken)
    {
        await ExecuteMutationAsync(
                operationName,
                operationBody,
                async ct =>
                {
                    await mutation(ct).ConfigureAwait(false);
                    return true;
                },
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<T> ExecuteMutationAsync<T>(
        string operationName,
        object operationBody,
        Func<CancellationToken, Task<T>> mutation,
        CancellationToken cancellationToken)
    {
        if (_coordinator is null)
            return await mutation(cancellationToken).ConfigureAwait(false);

        var status = _coordinator.GetStatus();
        if (status.Degraded)
            throw new RequirementsConflictException(string.IsNullOrWhiteSpace(status.Message)
                ? "Turn transaction coordinator is degraded."
                : status.Message);

        var requiresMutationTransactions = RequiresMutationTransactions(status);
        if (requiresMutationTransactions && _compensation is null)
            throw new RequirementsConflictException("Requirements storage does not support transaction rollback compensation.");

        T? mutationResult = default;
        var hasMutationResult = false;
        var transaction = BuildTransactionRequest(operationName, operationBody);
        var result = await _coordinator.ExecuteAsync(
                transaction,
                async ct =>
                {
                    var snapshot = _compensation is null
                        ? null
                        : await _compensation.CaptureRequirementsSnapshotAsync(ct).ConfigureAwait(false);

                    mutationResult = await mutation(ct).ConfigureAwait(false);
                    hasMutationResult = true;

                    return new TurnMutationResult
                    {
                        Success = true,
                        ResultJson = JsonSerializer.Serialize(mutationResult, JsonOptions),
                        RollbackAsync = snapshot is not null
                            ? rollbackCt => _compensation!.RestoreRequirementsSnapshotAsync(snapshot, rollbackCt)
                            : null,
                    };
                },
                cancellationToken)
            .ConfigureAwait(false);

        if (hasMutationResult && IsTransactionSuccess(result))
            return mutationResult!;

        throw ToTransactionFailure(operationName, result);
    }

    private async Task<RequirementsDocumentExportResult> ExecuteExportMutationAsync(
        string operationName,
        object operationBody,
        string outputRootPath,
        RequirementsExportSnapshotScope snapshotScope,
        Func<CancellationToken, Task<RequirementsDocumentExportResult>> mutation,
        CancellationToken cancellationToken)
    {
        if (_coordinator is null)
            return await mutation(cancellationToken).ConfigureAwait(false);

        var status = _coordinator.GetStatus();
        if (status.Degraded)
            throw new RequirementsConflictException(string.IsNullOrWhiteSpace(status.Message)
                ? "Turn transaction coordinator is degraded."
                : status.Message);

        RequirementsDocumentExportResult? mutationResult = null;
        var hasMutationResult = false;
        var transaction = BuildTransactionRequest(operationName, operationBody);
        var result = await _coordinator.ExecuteAsync(
                transaction,
                async ct =>
                {
                    var before = await CaptureExportSnapshotAsync(outputRootPath, snapshotScope, ct).ConfigureAwait(false);
                    mutationResult = await mutation(ct).ConfigureAwait(false);
                    hasMutationResult = true;
                    var after = await CaptureExportSnapshotAsync(outputRootPath, snapshotScope, ct).ConfigureAwait(false);

                    return new TurnMutationResult
                    {
                        Success = mutationResult.Success,
                        ResultJson = JsonSerializer.Serialize(mutationResult, JsonOptions),
                        Error = mutationResult.Success ? null : "Requirements export failed.",
                        RollbackAsync = mutationResult.Success
                            ? rollbackCt => RestoreExportSnapshotAsync(before, after, rollbackCt)
                            : null,
                    };
                },
                cancellationToken)
            .ConfigureAwait(false);

        if (hasMutationResult && (!mutationResult!.Success || IsTransactionSuccess(result)))
            return mutationResult;

        throw ToTransactionFailure(operationName, result);
    }

    private TurnTransactionRequest BuildTransactionRequest(string operationName, object operationBody)
    {
        var sequence = NextSequence();
        return new TurnTransactionRequest
        {
            TurnId = $"{operationName}-{sequence}",
            OperationName = operationName,
            OperationBodyJson = JsonSerializer.Serialize(operationBody, JsonOptions),
            Sequence = sequence,
            Mutating = true,
        };
    }

    private static async Task<RequirementsExportSnapshot> CaptureExportSnapshotAsync(
        string outputRootPath,
        RequirementsExportSnapshotScope snapshotScope,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(outputRootPath))
            throw new ArgumentException("Requirements export output root is required.", nameof(outputRootPath));

        var root = Path.GetFullPath(outputRootPath);
        var files = new Dictionary<string, RequirementsExportFileSnapshot>(StringComparer.OrdinalIgnoreCase);
        if (!Directory.Exists(root))
            return new RequirementsExportSnapshot(root, snapshotScope, files);

        foreach (var file in EnumerateExportScopeFiles(root, snapshotScope))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relativePath = NormalizeRelativePath(Path.GetRelativePath(root, file));
            var content = await File.ReadAllBytesAsync(file, cancellationToken).ConfigureAwait(false);
            files[relativePath] = new RequirementsExportFileSnapshot(
                relativePath,
                content,
                ComputeSha256(content),
                File.GetLastWriteTimeUtc(file));
        }

        return new RequirementsExportSnapshot(root, snapshotScope, files);
    }

    private static IEnumerable<string> EnumerateExportScopeFiles(
        string root,
        RequirementsExportSnapshotScope snapshotScope)
    {
        if (snapshotScope == RequirementsExportSnapshotScope.Canonical)
        {
            foreach (var relativePath in CanonicalRequirementsExportFiles)
            {
                var fullPath = ResolveExportPath(root, relativePath);
                if (File.Exists(fullPath))
                    yield return fullPath;
            }

            yield break;
        }

        foreach (var relativeDirectory in WikiRequirementsExportDirectories)
        {
            var fullDirectory = ResolveExportPath(root, relativeDirectory);
            if (!Directory.Exists(fullDirectory))
                continue;

            foreach (var file in Directory.EnumerateFiles(fullDirectory, "*", SearchOption.AllDirectories))
                yield return file;
        }
    }

    private static async Task RestoreExportSnapshotAsync(
        RequirementsExportSnapshot before,
        RequirementsExportSnapshot after,
        CancellationToken cancellationToken)
    {
        var current = await CaptureExportSnapshotAsync(before.RootPath, before.Scope, cancellationToken).ConfigureAwait(false);
        EnsureExportSnapshotUnchanged(before, after, current);

        Directory.CreateDirectory(before.RootPath);
        foreach (var file in before.Files.Values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fullPath = ResolveExportPath(before.RootPath, file.RelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            await File.WriteAllBytesAsync(fullPath, file.Content, cancellationToken).ConfigureAwait(false);
            File.SetLastWriteTimeUtc(fullPath, file.LastWriteTimeUtc);
        }

        foreach (var createdPath in after.Files.Keys.Where(path => !before.Files.ContainsKey(path)).ToArray())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fullPath = ResolveExportPath(before.RootPath, createdPath);
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }

        DeleteEmptyExportDirectories(before.RootPath);
    }

    private static void EnsureExportSnapshotUnchanged(
        RequirementsExportSnapshot before,
        RequirementsExportSnapshot after,
        RequirementsExportSnapshot current)
    {
        foreach (var expected in after.Files.Values)
        {
            if (!current.Files.TryGetValue(expected.RelativePath, out var actual) ||
                !string.Equals(actual.ContentSha256, expected.ContentSha256, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Requirements export file '{expected.RelativePath}' changed after transactional export; rollback refused.");
            }
        }

        foreach (var restoredPath in before.Files.Keys.Where(path => !after.Files.ContainsKey(path)))
        {
            if (current.Files.ContainsKey(restoredPath))
            {
                throw new InvalidOperationException(
                    $"Requirements export file '{restoredPath}' changed after transactional export; rollback refused.");
            }
        }
    }

    private static string ResolveExportPath(string rootPath, string relativePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(rootPath, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        var rootWithSeparator = rootPath.EndsWith(Path.DirectorySeparatorChar)
            ? rootPath
            : rootPath + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(fullPath, rootPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Requirements export file is outside the output root: {relativePath}");
        }

        return fullPath;
    }

    private static void DeleteEmptyExportDirectories(string rootPath)
    {
        if (!Directory.Exists(rootPath))
            return;

        foreach (var directory in Directory.EnumerateDirectories(rootPath, "*", SearchOption.AllDirectories)
                     .OrderByDescending(path => path.Length))
        {
            if (!Directory.EnumerateFileSystemEntries(directory).Any())
                Directory.Delete(directory);
        }
    }

    private long NextSequence()
    {
        while (true)
        {
            var current = Volatile.Read(ref _lastSequence);
            var next = Math.Max(current + 1, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            if (Interlocked.CompareExchange(ref _lastSequence, next, current) == current)
                return next;
        }
    }

    private bool RequiresMutationTransactions(TurnTransactionStatusResponse status)
        => status.Enabled && (_transactionOptions?.Value.RequiredForMutations ?? true);

    private static bool IsTransactionSuccess(TurnTransactionResult result)
        => string.Equals(result.Status, "committed", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(result.Status, "bypassed", StringComparison.OrdinalIgnoreCase);

    private static RequirementsConflictException ToTransactionFailure(string operationName, TurnTransactionResult result)
    {
        var transactionId = string.IsNullOrWhiteSpace(result.TransactionId)
            ? "unassigned"
            : result.TransactionId;
        var message = string.IsNullOrWhiteSpace(result.Message)
            ? result.Reason.ToString()
            : result.Message;
        if (result.RollbackAttempted)
        {
            message = result.RollbackSucceeded
                ? $"{message} Rollback completed."
                : $"{message} Rollback failed: {result.RollbackError ?? "unknown error"}.";
        }

        return new RequirementsConflictException(
            $"Turn transaction coordinator did not commit {operationName} '{transactionId}': {message}");
    }

    private static RequirementEntryPayload EntryPayload(string kind, FrEntry entry)
        => new(kind, entry.Id, ComputeSha256(entry.Body), entry.Body.Length, entry.Priority, entry.Status);

    private static RequirementEntryPayload EntryPayload(string kind, TrEntry entry)
        => new(kind, entry.Id, ComputeSha256(entry.Body), entry.Body.Length, entry.Priority, entry.Status);

    private static RequirementEntryPayload EntryPayload(TestEntry entry)
        => new("test", entry.Id, ComputeSha256(entry.Condition), entry.Condition.Length, entry.Priority, entry.Status);

    private static RequirementBatchPayload BatchPayload(string operation, RequirementsBatchEntries entries)
        => new(
            operation,
            entries.Functional.Select(entry => EntryPayload("fr", entry))
                .Concat(entries.Technical.Select(entry => EntryPayload("tr", entry)))
                .Concat(entries.Testing.Select(EntryPayload))
                .ToArray());

    private static string ComputeSha256(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static string ComputeSha256(byte[] value)
        => Convert.ToHexString(SHA256.HashData(value)).ToLowerInvariant();

    private static string NormalizeRelativePath(string relativePath)
        => relativePath.Replace('\\', '/');

    private sealed record RequirementEntryPayload(
        string Kind,
        string Id,
        string BodySha256,
        int BodyLength,
        string Priority,
        string Status);

    private sealed record RequirementDeletePayload(string Kind, string Id);

    private sealed record RequirementLayerSelectionPayload(string LayerKey);

    private sealed record RequirementBatchPayload(string Operation, IReadOnlyList<RequirementEntryPayload> Entries);

    private sealed record RequirementMappingPayload(
        string FrId,
        IReadOnlyList<string> TrIds,
        IReadOnlyList<string> TestIds);

    private sealed record RequirementExportPayload(
        string Format,
        string DocType,
        string OutputRootPath,
        DateTimeOffset? GeneratedAtUtc);

    private sealed record RequirementsExportSnapshot(
        string RootPath,
        RequirementsExportSnapshotScope Scope,
        IReadOnlyDictionary<string, RequirementsExportFileSnapshot> Files);

    private sealed record RequirementsExportFileSnapshot(
        string RelativePath,
        byte[] Content,
        string ContentSha256,
        DateTime LastWriteTimeUtc);

    private enum RequirementsExportSnapshotScope
    {
        Canonical,
        Wiki,
    }
}
