using System.Text;
using McpServer.Support.Mcp.Notifications;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Requirements.Models;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Requirements;

/// <summary>
/// File-backed requirements document service that parses, stores, mutates, and re-renders the canonical requirements documents.
/// </summary>
public sealed class RequirementsDocumentService : IRequirementsDocumentService
{
    private static readonly UTF8Encoding s_utf8NoBom = new(false);

    private readonly RequirementsOptions _options;
    private readonly IChangeEventBus? _eventBus;
    private readonly ILogger<RequirementsDocumentService> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly List<FrEntry> _frEntries;
    private readonly List<TrEntry> _trEntries;
    private readonly List<TestEntry> _testEntries;
    private readonly List<FrTrMapping> _mappings;

    /// <summary>Initializes a new instance of the <see cref="RequirementsDocumentService"/> class.</summary>
    public RequirementsDocumentService(IOptions<RequirementsOptions> options, ILogger<RequirementsDocumentService> logger, IChangeEventBus? eventBus = null)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _eventBus = eventBus;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _frEntries = RequirementsDocumentParser.ParseFunctional(ReadFileIfExists(_options.FunctionalRequirementsPath)).ToList();
        _trEntries = RequirementsDocumentParser.ParseTechnical(ReadFileIfExists(_options.TechnicalRequirementsPath)).ToList();
        _testEntries = RequirementsDocumentParser.ParseTesting(ReadFileIfExists(_options.TestingRequirementsPath)).ToList();
        _mappings = RequirementsDocumentParser.ParseMapping(ReadFileIfExists(_options.MappingPath)).ToList();
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<FrEntry>> GetAllFrAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<FrEntry>>(_frEntries.ToArray());
    }

    /// <inheritdoc />
    public Task<FrEntry?> GetFrAsync(string id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_frEntries.FirstOrDefault(entry => IdEquals(entry.Id, id)));
    }

    /// <inheritdoc />
    public async Task AddFrAsync(FrEntry entry, CancellationToken ct = default)
    {
        ValidateFr(entry);
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            ThrowIfExists(_frEntries, entry.Id, static item => item.Id, "FR");
            _frEntries.Add(entry);
            await PersistFunctionalAsync(ct).ConfigureAwait(false);
            await PublishRequirementsChangeSafeAsync(ChangeEventActions.Created, entry.Id, ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task UpdateFrAsync(FrEntry entry, CancellationToken ct = default)
    {
        ValidateFr(entry);
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var index = FindIndexOrThrow(_frEntries, entry.Id, static item => item.Id, "FR");
            _frEntries[index] = entry;
            await PersistFunctionalAsync(ct).ConfigureAwait(false);
            await PublishRequirementsChangeSafeAsync(ChangeEventActions.Updated, entry.Id, ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task DeleteFrAsync(string id, CancellationToken ct = default)
    {
        ValidateId(id, nameof(id));
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var index = FindIndexOrThrow(_frEntries, id, static item => item.Id, "FR");
            _frEntries.RemoveAt(index);

            var mappingIndex = _mappings.FindIndex(mapping => IdEquals(mapping.FrId, id));
            var mappingRemoved = false;
            if (mappingIndex >= 0)
            {
                _mappings.RemoveAt(mappingIndex);
                mappingRemoved = true;
            }

            await PersistFunctionalAsync(ct).ConfigureAwait(false);
            if (mappingRemoved)
                await PersistMappingAsync(ct).ConfigureAwait(false);
            await PublishRequirementsChangeSafeAsync(ChangeEventActions.Deleted, id, ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<TrEntry>> GetAllTrAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<TrEntry>>(_trEntries.ToArray());
    }

    /// <inheritdoc />
    public Task<TrEntry?> GetTrAsync(string id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_trEntries.FirstOrDefault(entry => IdEquals(entry.Id, id)));
    }

    /// <inheritdoc />
    public async Task AddTrAsync(TrEntry entry, CancellationToken ct = default)
    {
        ValidateTr(entry);
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            ThrowIfExists(_trEntries, entry.Id, static item => item.Id, "TR");
            _trEntries.Add(entry);
            await PersistTechnicalAsync(ct).ConfigureAwait(false);
            await PublishRequirementsChangeSafeAsync(ChangeEventActions.Created, entry.Id, ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task UpdateTrAsync(TrEntry entry, CancellationToken ct = default)
    {
        ValidateTr(entry);
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var index = FindIndexOrThrow(_trEntries, entry.Id, static item => item.Id, "TR");
            _trEntries[index] = entry;
            await PersistTechnicalAsync(ct).ConfigureAwait(false);
            await PublishRequirementsChangeSafeAsync(ChangeEventActions.Updated, entry.Id, ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task DeleteTrAsync(string id, CancellationToken ct = default)
    {
        ValidateId(id, nameof(id));
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var index = FindIndexOrThrow(_trEntries, id, static item => item.Id, "TR");
            _trEntries.RemoveAt(index);

            var mappingChanged = false;
            for (var i = 0; i < _mappings.Count; i++)
            {
                var filtered = _mappings[i].TrIds.Where(trId => !IdEquals(trId, id)).ToArray();
                if (filtered.Length == _mappings[i].TrIds.Count)
                    continue;

                _mappings[i] = _mappings[i] with { TrIds = filtered };
                mappingChanged = true;
            }

            await PersistTechnicalAsync(ct).ConfigureAwait(false);
            if (mappingChanged)
                await PersistMappingAsync(ct).ConfigureAwait(false);
            await PublishRequirementsChangeSafeAsync(ChangeEventActions.Deleted, id, ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<TestEntry>> GetAllTestAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<TestEntry>>(_testEntries.ToArray());
    }

    /// <inheritdoc />
    public Task<TestEntry?> GetTestAsync(string id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_testEntries.FirstOrDefault(entry => IdEquals(entry.Id, id)));
    }

    /// <inheritdoc />
    public async Task AddTestAsync(TestEntry entry, CancellationToken ct = default)
    {
        ValidateTest(entry);
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            ThrowIfExists(_testEntries, entry.Id, static item => item.Id, "TEST");
            _testEntries.Add(entry);
            await PersistTestingAsync(ct).ConfigureAwait(false);
            await PublishRequirementsChangeSafeAsync(ChangeEventActions.Created, entry.Id, ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task UpdateTestAsync(TestEntry entry, CancellationToken ct = default)
    {
        ValidateTest(entry);
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var index = FindIndexOrThrow(_testEntries, entry.Id, static item => item.Id, "TEST");
            _testEntries[index] = entry;
            await PersistTestingAsync(ct).ConfigureAwait(false);
            await PublishRequirementsChangeSafeAsync(ChangeEventActions.Updated, entry.Id, ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task DeleteTestAsync(string id, CancellationToken ct = default)
    {
        ValidateId(id, nameof(id));
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var index = FindIndexOrThrow(_testEntries, id, static item => item.Id, "TEST");
            _testEntries.RemoveAt(index);
            await PersistTestingAsync(ct).ConfigureAwait(false);
            await PublishRequirementsChangeSafeAsync(ChangeEventActions.Deleted, id, ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<RequirementsBatchEntries> AddBatchAsync(RequirementsBatchEntries entries, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ValidateBatchEntries(entries);
        ValidateBatchUniqueIds(entries);

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            ThrowIfAnyExists(_frEntries, entries.Functional, static item => item.Id, static item => item.Id, "FR");
            ThrowIfAnyExists(_trEntries, entries.Technical, static item => item.Id, static item => item.Id, "TR");
            ThrowIfAnyExists(_testEntries, entries.Testing, static item => item.Id, static item => item.Id, "TEST");

            _frEntries.AddRange(entries.Functional);
            _trEntries.AddRange(entries.Technical);
            _testEntries.AddRange(entries.Testing);

            if (entries.Functional.Count > 0)
                await PersistFunctionalAsync(ct).ConfigureAwait(false);
            if (entries.Technical.Count > 0)
                await PersistTechnicalAsync(ct).ConfigureAwait(false);
            if (entries.Testing.Count > 0)
                await PersistTestingAsync(ct).ConfigureAwait(false);

            await PublishBatchRequirementsChangeSafeAsync(ChangeEventActions.Created, entries, ct).ConfigureAwait(false);
            return entries;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<RequirementsBatchEntries> UpdateBatchAsync(RequirementsBatchEntries entries, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ValidateBatchEntries(entries);
        ValidateBatchUniqueIds(entries);

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var frIndices = FindBatchIndicesOrThrow(_frEntries, entries.Functional, static item => item.Id, static item => item.Id, "FR");
            var trIndices = FindBatchIndicesOrThrow(_trEntries, entries.Technical, static item => item.Id, static item => item.Id, "TR");
            var testIndices = FindBatchIndicesOrThrow(_testEntries, entries.Testing, static item => item.Id, static item => item.Id, "TEST");

            for (var i = 0; i < entries.Functional.Count; i++)
                _frEntries[frIndices[i]] = entries.Functional[i];
            for (var i = 0; i < entries.Technical.Count; i++)
                _trEntries[trIndices[i]] = entries.Technical[i];
            for (var i = 0; i < entries.Testing.Count; i++)
                _testEntries[testIndices[i]] = entries.Testing[i];

            if (entries.Functional.Count > 0)
                await PersistFunctionalAsync(ct).ConfigureAwait(false);
            if (entries.Technical.Count > 0)
                await PersistTechnicalAsync(ct).ConfigureAwait(false);
            if (entries.Testing.Count > 0)
                await PersistTestingAsync(ct).ConfigureAwait(false);

            await PublishBatchRequirementsChangeSafeAsync(ChangeEventActions.Updated, entries, ct).ConfigureAwait(false);
            return entries;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<FrTrMapping>> GetAllMappingsAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<FrTrMapping>>(_mappings.ToArray());
    }

    /// <inheritdoc />
    public Task<FrTrMapping?> GetMappingAsync(string frId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_mappings.FirstOrDefault(mapping => IdEquals(mapping.FrId, frId)));
    }

    /// <inheritdoc />
    public async Task UpsertMappingAsync(FrTrMapping mapping, CancellationToken ct = default)
    {
        ValidateMapping(mapping);
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var normalized = mapping with
            {
                TrIds = mapping.TrIds
                    .Where(static value => !string.IsNullOrWhiteSpace(value))
                    .Select(static value => value.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray()
            };

            var index = _mappings.FindIndex(item => IdEquals(item.FrId, normalized.FrId));
            if (index >= 0)
                _mappings[index] = normalized;
            else
                _mappings.Add(normalized);

            await PersistMappingAsync(ct).ConfigureAwait(false);
            await PublishRequirementsChangeSafeAsync(ChangeEventActions.Updated, normalized.FrId, ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task DeleteMappingAsync(string frId, CancellationToken ct = default)
    {
        ValidateId(frId, nameof(frId));
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var index = FindIndexOrThrow(_mappings, frId, static item => item.FrId, "mapping");
            _mappings.RemoveAt(index);
            await PersistMappingAsync(ct).ConfigureAwait(false);
            await PublishRequirementsChangeSafeAsync(ChangeEventActions.Deleted, frId, ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public Task<(string Content, string MimeType)> GenerateDocumentAsync(RequirementsDocType docType, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        return docType switch
        {
            RequirementsDocType.Functional => Task.FromResult((RequirementsDocumentRenderer.RenderFunctional(_frEntries), "text/markdown")),
            RequirementsDocType.Technical => Task.FromResult((RequirementsDocumentRenderer.RenderTechnical(_trEntries), "text/markdown")),
            RequirementsDocType.Testing => Task.FromResult((RequirementsDocumentRenderer.RenderTesting(_testEntries), "text/markdown")),
            RequirementsDocType.Mapping => Task.FromResult((RequirementsDocumentRenderer.RenderMapping(_mappings), "text/markdown")),
            RequirementsDocType.Matrix => Task.FromResult((RequirementsDocumentRenderer.RenderMatrix(_frEntries, _trEntries, _testEntries, ReadFileIfExists(_options.MatrixPath)), "text/markdown")),
            RequirementsDocType.All => throw new ArgumentOutOfRangeException(nameof(docType), "Use GenerateAllAsync for docType=All."),
            _ => throw new ArgumentOutOfRangeException(nameof(docType), docType, "Unknown requirements document type.")
        };
    }

    /// <inheritdoc />
    public async Task<RequirementsDocumentExportResult> GenerateAllAsync(string outputRootPath, DateTimeOffset? generatedAtUtc = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var generated = (generatedAtUtc ?? DateTimeOffset.UtcNow).ToUniversalTime();
        var documents = RequirementsWikiDocumentRenderer.RenderCanonicalFiles(_frEntries, _trEntries, _testEntries, _mappings, ReadExistingMatrixForExport(outputRootPath));
        return await RequirementsDocumentExportWriter.WriteAsync(
            outputRootPath,
            "markdown",
            "all",
            generated,
            documents,
            ct: ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<RequirementsDocumentExportResult> GenerateWikiAsync(string outputRootPath, DateTimeOffset? generatedAtUtc = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var generated = (generatedAtUtc ?? DateTimeOffset.UtcNow).ToUniversalTime();
        var documents = RequirementsWikiDocumentRenderer.RenderWikiFiles(
            _frEntries,
            _trEntries,
            _testEntries,
            _mappings,
            generated,
            ReadExistingMatrixForWikiExport(outputRootPath));

        return await RequirementsDocumentExportWriter.WriteAsync(
            outputRootPath,
            "wiki",
            "all",
            generated,
            documents,
            [RequirementsWikiDocumentRenderer.AzureFolder, RequirementsWikiDocumentRenderer.GitHubFolder],
            ct).ConfigureAwait(false);
    }

    private async Task PersistFunctionalAsync(CancellationToken ct) =>
        await AtomicWriteAsync(_options.FunctionalRequirementsPath, RequirementsDocumentRenderer.RenderFunctional(_frEntries), ct).ConfigureAwait(false);

    private async Task PersistTechnicalAsync(CancellationToken ct) =>
        await AtomicWriteAsync(_options.TechnicalRequirementsPath, RequirementsDocumentRenderer.RenderTechnical(_trEntries), ct).ConfigureAwait(false);

    private async Task PersistTestingAsync(CancellationToken ct) =>
        await AtomicWriteAsync(_options.TestingRequirementsPath, RequirementsDocumentRenderer.RenderTesting(_testEntries), ct).ConfigureAwait(false);

    private async Task PersistMappingAsync(CancellationToken ct) =>
        await AtomicWriteAsync(_options.MappingPath, RequirementsDocumentRenderer.RenderMapping(_mappings), ct).ConfigureAwait(false);

    private string? ReadExistingMatrixForExport(string outputRootPath)
    {
        var outputMatrix = Path.Combine(outputRootPath, RequirementsDocumentRenderer.MatrixFileName);
        return ReadFileIfExists(outputMatrix) ?? ReadFileIfExists(_options.MatrixPath);
    }

    private string? ReadExistingMatrixForWikiExport(string outputRootPath)
    {
        var projectRoot = Directory.GetParent(Path.GetFullPath(outputRootPath))?.FullName;
        var projectMatrix = string.IsNullOrWhiteSpace(projectRoot)
            ? null
            : Path.Combine(projectRoot, RequirementsDocumentRenderer.MatrixFileName);
        return ReadFileIfExists(projectMatrix) ?? ReadFileIfExists(_options.MatrixPath);
    }

    private async Task AtomicWriteAsync(string path, string content, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new InvalidOperationException("Requirements document path is not configured.");

        var fullPath = Path.GetFullPath(path);
        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);

        var tempPath = fullPath + "." + Guid.NewGuid().ToString("N")[..8] + ".tmp";
        try
        {
            await File.WriteAllTextAsync(tempPath, content, s_utf8NoBom, ct).ConfigureAwait(false);

            if (File.Exists(fullPath))
            {
                try
                {
                    File.Replace(tempPath, fullPath, null, ignoreMetadataErrors: true);
                }
                catch (Exception ex) when (ex is PlatformNotSupportedException or UnauthorizedAccessException)
                {
                    _logger.LogError("{ExceptionDetail}", ex.ToString());
                    File.Move(tempPath, fullPath, overwrite: true);
                }
            }
            else
            {
                File.Move(tempPath, fullPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write requirements document {Path}", fullPath);
            throw;
        }
        finally
        {
            try
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
            catch (Exception cleanupEx)
            {
                _logger.LogDebug(cleanupEx, "Failed to delete temp file {TempPath}", tempPath);
            }
        }
    }

    private static string? ReadFileIfExists(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        return File.Exists(path) ? File.ReadAllText(path) : null;
    }

    private static void ValidateFr(FrEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ValidateId(entry.Id, nameof(entry.Id));
        if (string.IsNullOrWhiteSpace(entry.Title))
            throw new ArgumentException("FR title is required.", nameof(entry));
        if (string.IsNullOrWhiteSpace(entry.Body))
            throw new ArgumentException("FR body is required.", nameof(entry));
    }

    private static void ValidateTr(TrEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ValidateId(entry.Id, nameof(entry.Id));
        if (string.IsNullOrWhiteSpace(entry.Body))
            throw new ArgumentException("TR body is required.", nameof(entry));
    }

    private static void ValidateTest(TestEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ValidateId(entry.Id, nameof(entry.Id));
        if (string.IsNullOrWhiteSpace(entry.Condition))
            throw new ArgumentException("TEST condition is required.", nameof(entry));
    }

    private static void ValidateMapping(FrTrMapping mapping)
    {
        ArgumentNullException.ThrowIfNull(mapping);
        ValidateId(mapping.FrId, nameof(mapping.FrId));
        ArgumentNullException.ThrowIfNull(mapping.TrIds);
    }

    private static void ValidateId(string id, string paramName)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("ID is required.", paramName);
    }

    private static bool IdEquals(string left, string right) =>
        string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);

    private static int FindIndexOrThrow<T>(IReadOnlyList<T> items, string id, Func<T, string> getId, string label)
    {
        for (var i = 0; i < items.Count; i++)
        {
            if (IdEquals(getId(items[i]), id))
                return i;
        }

        throw new RequirementsNotFoundException($"{label} '{id}' was not found.");
    }

    private static void ThrowIfExists<T>(IEnumerable<T> items, string id, Func<T, string> getId, string label)
    {
        if (items.Any(item => IdEquals(getId(item), id)))
            throw new RequirementsConflictException($"{label} '{id}' already exists.");
    }

    private static void ThrowIfAnyExists<TExisting, TIncoming>(
        IEnumerable<TExisting> existingItems,
        IReadOnlyList<TIncoming> incomingItems,
        Func<TExisting, string> getExistingId,
        Func<TIncoming, string> getIncomingId,
        string label)
    {
        foreach (var incoming in incomingItems)
            ThrowIfExists(existingItems, getIncomingId(incoming), getExistingId, label);
    }

    private static int[] FindBatchIndicesOrThrow<TExisting, TIncoming>(
        IReadOnlyList<TExisting> existingItems,
        IReadOnlyList<TIncoming> incomingItems,
        Func<TExisting, string> getExistingId,
        Func<TIncoming, string> getIncomingId,
        string label)
    {
        var indices = new int[incomingItems.Count];
        for (var i = 0; i < incomingItems.Count; i++)
            indices[i] = FindIndexOrThrow(existingItems, getIncomingId(incomingItems[i]), getExistingId, label);

        return indices;
    }

    private static void ValidateBatchEntries(RequirementsBatchEntries entries)
    {
        ArgumentNullException.ThrowIfNull(entries.Functional);
        ArgumentNullException.ThrowIfNull(entries.Technical);
        ArgumentNullException.ThrowIfNull(entries.Testing);

        foreach (var entry in entries.Functional)
            ValidateFr(entry);
        foreach (var entry in entries.Technical)
            ValidateTr(entry);
        foreach (var entry in entries.Testing)
            ValidateTest(entry);
    }

    private static void ValidateBatchUniqueIds(RequirementsBatchEntries entries)
    {
        ValidateUniqueIds(entries.Functional, static item => item.Id, "FR");
        ValidateUniqueIds(entries.Technical, static item => item.Id, "TR");
        ValidateUniqueIds(entries.Testing, static item => item.Id, "TEST");
    }

    private static void ValidateUniqueIds<T>(IReadOnlyList<T> items, Func<T, string> getId, string label)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            var id = getId(item);
            if (!seen.Add(id.Trim()))
                throw new ArgumentException($"Duplicate {label} ID '{id}' in batch.", nameof(items));
        }
    }

    private async Task PublishBatchRequirementsChangeSafeAsync(string action, RequirementsBatchEntries entries, CancellationToken ct)
    {
        foreach (var entry in entries.Functional)
            await PublishRequirementsChangeSafeAsync(action, entry.Id, ct).ConfigureAwait(false);
        foreach (var entry in entries.Technical)
            await PublishRequirementsChangeSafeAsync(action, entry.Id, ct).ConfigureAwait(false);
        foreach (var entry in entries.Testing)
            await PublishRequirementsChangeSafeAsync(action, entry.Id, ct).ConfigureAwait(false);
    }

    private async Task PublishRequirementsChangeSafeAsync(string action, string entityId, CancellationToken ct)
    {
        if (_eventBus is null)
            return;

        try
        {
            await _eventBus.PublishAsync(
                new ChangeEvent
                {
                    Category = ChangeEventCategories.Requirements,
                    Action = action,
                    EntityId = entityId,
                    ResourceUri = $"mcp://workspace/requirements/{entityId}",
                },
                ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed publishing requirements change event for {EntityId}", entityId);
        }
    }
}
