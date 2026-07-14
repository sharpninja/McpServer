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
    private static readonly TimeSpan[] s_atomicWriteRetryDelays =
    [
        TimeSpan.FromMilliseconds(20),
        TimeSpan.FromMilliseconds(50),
        TimeSpan.FromMilliseconds(100)
    ];

    private readonly RequirementsOptions _options;
    private readonly IChangeEventBus? _eventBus;
    private readonly ILogger<RequirementsDocumentService> _logger;
    private readonly IRequirementsWikiExportOrchestrator _wikiExportOrchestrator;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly List<FrEntry> _frEntries;
    private readonly List<TrEntry> _trEntries;
    private readonly List<TestEntry> _testEntries;
    private readonly List<FrTrMapping> _mappings;
    private readonly List<RequirementScopeLayerEntry> _layers = [new(RequirementScopeLayerDefaults.DefaultLayerKey, 1, "Layer 1")];
    private string _currentRequirementLayerKey = RequirementScopeLayerDefaults.DefaultLayerKey;

    /// <summary>Initializes a new instance of the <see cref="RequirementsDocumentService"/> class.</summary>
    public RequirementsDocumentService(
        IOptions<RequirementsOptions> options,
        ILogger<RequirementsDocumentService> logger,
        IChangeEventBus? eventBus = null,
        IRequirementsWikiExportOrchestrator? wikiExportOrchestrator = null)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _eventBus = eventBus;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _wikiExportOrchestrator = wikiExportOrchestrator
            ?? new RequirementsWikiExportOrchestrator(new DisabledRequirementsDocFxWorkflowRunner());

        _frEntries = RequirementsDocumentParser.ParseFunctional(ReadFileIfExists(_options.FunctionalRequirementsPath)).ToList();
        _trEntries = RequirementsDocumentParser.ParseTechnical(ReadFileIfExists(_options.TechnicalRequirementsPath)).ToList();
        _testEntries = RequirementsDocumentParser.ParseTesting(ReadFileIfExists(_options.TestingRequirementsPath)).ToList();
        _mappings = RequirementsDocumentParser.ParseMapping(ReadFileIfExists(_options.MappingPath)).ToList();
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<RequirementScopeLayerEntry>> GetRequirementLayersAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<RequirementScopeLayerEntry>>(_layers.OrderBy(x => x.Order).ToArray());
    }

    /// <inheritdoc />
    public Task<RequirementScopeLayerEntry> CreateRequirementLayerAsync(RequirementScopeLayerEntry entry, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(entry);
        if (_layers.Any(x => IdEquals(x.Key, entry.Key)))
            throw new RequirementsConflictException($"Requirement scope layer '{entry.Key}' already exists.");
        if (_layers.Any(x => x.Order == entry.Order))
            throw new RequirementsConflictException($"Requirement scope layer order '{entry.Order}' already exists.");
        ValidateLayerReference(entry.ScopeEndLayerKey);
        var now = DateTimeOffset.UtcNow;
        var normalized = entry with
        {
            Key = entry.Key.Trim(),
            Name = string.IsNullOrWhiteSpace(entry.Name) ? entry.Key.Trim() : entry.Name.Trim(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        _layers.Add(normalized);
        return Task.FromResult(normalized);
    }

    /// <inheritdoc />
    public Task<RequirementScopeLayerEntry> UpdateRequirementLayerAsync(RequirementScopeLayerUpdateRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(request);
        var index = _layers.FindIndex(x => IdEquals(x.Key, request.Key));
        if (index < 0)
            throw new RequirementsNotFoundException($"Requirement scope layer '{request.Key}' was not found.");
        var existing = _layers[index];
        if (request.Order.HasValue && request.Order.Value != existing.Order)
            throw new InvalidOperationException("Requirement scope layer order is immutable.");
        ValidateLayerReference(request.ScopeEndLayerKey);
        var updated = existing with
        {
            Name = string.IsNullOrWhiteSpace(request.Name) ? existing.Name : request.Name.Trim(),
            Description = request.Description ?? existing.Description,
            ScopeEndLayerKey = string.IsNullOrWhiteSpace(request.ScopeEndLayerKey) ? existing.ScopeEndLayerKey : request.ScopeEndLayerKey.Trim(),
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };
        _layers[index] = updated;
        return Task.FromResult(updated);
    }

    /// <inheritdoc />
    public Task<RequirementScopeLayerEntry> GetWorkspaceCurrentRequirementLayerAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(FindLayer(_currentRequirementLayerKey));
    }

    /// <inheritdoc />
    public Task<RequirementScopeLayerEntry> SetWorkspaceCurrentRequirementLayerAsync(string layerKey, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var layer = FindLayer(layerKey);
        _currentRequirementLayerKey = layer.Key;
        return Task.FromResult(layer);
    }

    /// <inheritdoc />
    public Task<EffectiveRequirementsResult> GetEffectiveRequirementsAsync(string? layerKey = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var layer = FindLayer(string.IsNullOrWhiteSpace(layerKey) ? _currentRequirementLayerKey : layerKey);
        var fr = _frEntries.Where(x => IsEffective(x.ScopeStartLayerKey, x.ScopeEndLayerKey, layer.Order)).ToArray();
        var tr = _trEntries.Where(x => IsEffective(x.ScopeStartLayerKey, x.ScopeEndLayerKey, layer.Order)).ToArray();
        var test = _testEntries.Where(x => IsEffective(x.ScopeStartLayerKey, x.ScopeEndLayerKey, layer.Order)).ToArray();
        var frIds = fr.Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var trIds = tr.Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var testIds = test.Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var mappings = _mappings
            .Where(mapping => frIds.Contains(mapping.FrId))
            .Select(mapping => new FrTrMapping(
                mapping.FrId,
                mapping.TrIds.Where(trIds.Contains).ToArray(),
                mapping.TestIds.Where(testIds.Contains).ToArray(),
                mapping.WorkspaceId))
            .Where(mapping => mapping.TrIds.Count > 0 || mapping.TestIds.Count > 0)
            .ToArray();
        return Task.FromResult(new EffectiveRequirementsResult(layer, fr, tr, test, mappings));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<FrEntry>> GetAllFrAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<FrEntry>>(_frEntries.ToArray());
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<FrEntry>> QueryFrAsync(string? area = null, string? status = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var filtered = _frEntries.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(area))
            filtered = filtered.Where(entry => string.Equals(GetRequirementArea(entry.Id, "FR"), area, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(status))
            filtered = filtered.Where(entry => string.Equals(entry.Status, status, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult<IReadOnlyList<FrEntry>>(filtered.ToArray());
    }

    /// <inheritdoc />
    public async Task<int> PurgeInvalidPlaceholdersAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var before = _frEntries.Count;
            _frEntries.RemoveAll(IsInvalidPlaceholder);
            if (_frEntries.Count != before)
            {
                await PersistFunctionalAsync(ct).ConfigureAwait(false);
                await PublishRequirementsChangeSafeAsync("repaired", "*", ct).ConfigureAwait(false);
            }
            return before - _frEntries.Count;
        }
        finally
        {
            _gate.Release();
        }
    }

    private static readonly System.Text.RegularExpressions.Regex RequirementIdShapeRegex = new(
        @"^(FR|TR|TEST)-[A-Z0-9]+(-[A-Z0-9]+)*$",
        System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    private static bool IsInvalidPlaceholder(FrEntry e)
    {
        if (e?.Body == null || !e.Body.StartsWith("Placeholder requirement backfilled", StringComparison.Ordinal))
            return false;
        // Keep only canonical FR ids like FR-XXX-001 or FR-XXX-SUB-001
        // Treat null/empty Id as invalid (delete)
        var id = e.Id ?? string.Empty;
        return string.IsNullOrEmpty(id) || !IsValidRequirementId(id, "FR");
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
    public Task<IReadOnlyList<TrEntry>> QueryTrAsync(string? area = null, string? subarea = null, string? status = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var filtered = _trEntries.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(area))
            filtered = filtered.Where(entry => string.Equals(GetRequirementArea(entry.Id, "TR"), area, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(subarea))
            filtered = filtered.Where(entry => string.Equals(GetRequirementSubarea(entry.Id, "TR"), subarea, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(status))
            filtered = filtered.Where(entry => string.Equals(entry.Status, status, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult<IReadOnlyList<TrEntry>>(filtered.ToArray());
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
    public Task<IReadOnlyList<TestEntry>> QueryTestAsync(string? area = null, string? status = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var filtered = _testEntries.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(area))
            filtered = filtered.Where(entry => string.Equals(GetRequirementArea(entry.Id, "TEST"), area, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(status))
            filtered = filtered.Where(entry => string.Equals(entry.Status, status, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult<IReadOnlyList<TestEntry>>(filtered.ToArray());
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
            // Idempotent create batch: skip records that already exist. Prevents whole-batch abort on retry/double-submit
            // from client/plugin layers (e.g. compat+typed send paths sending same logical batch).
            var newFr = entries.Functional.Where(f => !_frEntries.Any(e => string.Equals(e.Id, f.Id, StringComparison.OrdinalIgnoreCase))).ToList();
            var newTr = entries.Technical.Where(t => !_trEntries.Any(e => string.Equals(e.Id, t.Id, StringComparison.OrdinalIgnoreCase))).ToList();
            var newTest = entries.Testing.Where(t => !_testEntries.Any(e => string.Equals(e.Id, t.Id, StringComparison.OrdinalIgnoreCase))).ToList();

            _frEntries.AddRange(newFr);
            _trEntries.AddRange(newTr);
            _testEntries.AddRange(newTest);

            if (newFr.Count > 0)
                await PersistFunctionalAsync(ct).ConfigureAwait(false);
            if (newTr.Count > 0)
                await PersistTechnicalAsync(ct).ConfigureAwait(false);
            if (newTest.Count > 0)
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
            RequirementsDocType.Technical => Task.FromResult((RequirementsDocumentRenderer.RenderTechnical(_trEntries, _mappings), "text/markdown")),
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
        var request = new RequirementsWikiExportRequest(
            outputRootPath,
            generated,
            TryInferWorkspacePathFromOptions(),
            _options,
            _frEntries.ToArray(),
            _trEntries.ToArray(),
            _testEntries.ToArray(),
            _mappings.ToArray(),
            ReadExistingMatrixForWikiExport(outputRootPath));
        return await _wikiExportOrchestrator.ExportAsync(request, ct).ConfigureAwait(false);
    }

    private async Task PersistFunctionalAsync(CancellationToken ct) =>
        await AtomicWriteAsync(_options.FunctionalRequirementsPath, RequirementsDocumentRenderer.RenderFunctional(_frEntries), ct).ConfigureAwait(false);

    private async Task PersistTechnicalAsync(CancellationToken ct) =>
        await AtomicWriteAsync(_options.TechnicalRequirementsPath, RequirementsDocumentRenderer.RenderTechnical(_trEntries, _mappings), ct).ConfigureAwait(false);

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

    private string? TryInferWorkspacePathFromOptions()
    {
        var functional = _options.FunctionalRequirementsPath;
        if (string.IsNullOrWhiteSpace(functional))
            return null;

        var projectDir = Path.GetDirectoryName(Path.GetFullPath(functional));
        var docsDir = projectDir is null ? null : Directory.GetParent(projectDir)?.FullName;
        return docsDir is null ? null : Directory.GetParent(docsDir)?.FullName;
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

            await ReplaceOrMoveWithRetryAsync(tempPath, fullPath, ct).ConfigureAwait(false);
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

    private async Task ReplaceOrMoveWithRetryAsync(string tempPath, string fullPath, CancellationToken ct)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                ReplaceOrMove(tempPath, fullPath);
                return;
            }
            catch (Exception ex) when (ex is PlatformNotSupportedException or UnauthorizedAccessException)
            {
                _logger.LogDebug(ex, "Atomic replace is unavailable for {Path}; falling back to overwrite move.", fullPath);
                File.Move(tempPath, fullPath, overwrite: true);
                return;
            }
            catch (IOException ex) when (attempt < s_atomicWriteRetryDelays.Length)
            {
                _logger.LogDebug(ex, "Retrying atomic write for {Path} after transient file-system error.", fullPath);
                await Task.Delay(s_atomicWriteRetryDelays[attempt], ct).ConfigureAwait(false);
            }
            catch (IOException ex)
            {
                _logger.LogDebug(ex, "Atomic write retries exhausted for {Path}; falling back to overwrite move.", fullPath);
                File.Move(tempPath, fullPath, overwrite: true);
                return;
            }
        }
    }

    private static void ReplaceOrMove(string tempPath, string fullPath)
    {
        if (File.Exists(fullPath))
        {
            File.Replace(tempPath, fullPath, null, ignoreMetadataErrors: true);
        }
        else
        {
            File.Move(tempPath, fullPath);
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
        ValidateId(entry.Id, nameof(entry.Id), "FR");
        if (string.IsNullOrWhiteSpace(entry.Title))
            throw new ArgumentException("FR title is required.", nameof(entry));
        if (string.IsNullOrWhiteSpace(entry.Body))
            throw new ArgumentException("FR body is required.", nameof(entry));
    }

    private static void ValidateTr(TrEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ValidateId(entry.Id, nameof(entry.Id), "TR");
        if (string.IsNullOrWhiteSpace(entry.Body))
            throw new ArgumentException("TR body is required.", nameof(entry));
    }

    private static void ValidateTest(TestEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ValidateId(entry.Id, nameof(entry.Id), "TEST");
        if (string.IsNullOrWhiteSpace(entry.Condition))
            throw new ArgumentException("TEST condition is required.", nameof(entry));
    }

    private static void ValidateMapping(FrTrMapping mapping)
    {
        ArgumentNullException.ThrowIfNull(mapping);
        ValidateId(mapping.FrId, nameof(mapping.FrId), "FR");
        ArgumentNullException.ThrowIfNull(mapping.TrIds);
        ArgumentNullException.ThrowIfNull(mapping.TestIds);
        foreach (var trId in mapping.TrIds)
            ValidateId(trId, nameof(mapping.TrIds), "TR");
        foreach (var testId in mapping.TestIds)
            ValidateId(testId, nameof(mapping.TestIds), "TEST");
    }

    private RequirementScopeLayerEntry FindLayer(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Requirement scope layer key is required.", nameof(key));
        return _layers.FirstOrDefault(x => IdEquals(x.Key, key))
            ?? throw new RequirementsNotFoundException($"Requirement scope layer '{key}' was not found.");
    }

    private void ValidateLayerReference(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;
        _ = FindLayer(key);
    }

    private bool IsEffective(string scopeStartLayerKey, string? scopeEndLayerKey, int currentOrder)
    {
        var startLayer = FindLayer(string.IsNullOrWhiteSpace(scopeStartLayerKey) ? RequirementScopeLayerDefaults.DefaultLayerKey : scopeStartLayerKey);
        if (startLayer.Order > currentOrder)
            return false;
        int? endOrder = null;
        if (!string.IsNullOrWhiteSpace(scopeEndLayerKey))
            endOrder = FindLayer(scopeEndLayerKey).Order;
        if (!string.IsNullOrWhiteSpace(startLayer.ScopeEndLayerKey))
        {
            var layerEndOrder = FindLayer(startLayer.ScopeEndLayerKey).Order;
            endOrder = endOrder.HasValue ? Math.Min(endOrder.Value, layerEndOrder) : layerEndOrder;
        }

        return !endOrder.HasValue || endOrder.Value >= currentOrder;
    }

    private static void ValidateId(string id, string paramName, string? expectedPrefix = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("ID is required.", paramName);
        if (expectedPrefix is not null && !IsValidRequirementId(id, expectedPrefix))
            throw new ArgumentException($"Requirement ID '{id}' must match the {expectedPrefix} identifier shape.", paramName);
    }

    private static bool IsValidRequirementId(string id, string expectedPrefix) =>
        RequirementIdShapeRegex.IsMatch(id)
        && id.StartsWith(expectedPrefix + "-", StringComparison.OrdinalIgnoreCase)
        && !id.Contains('*', StringComparison.Ordinal);

    private static string? GetRequirementArea(string id, string expectedPrefix) =>
        SplitRequirementId(id, expectedPrefix) is { Length: >= 2 } parts ? parts[1] : null;

    private static string? GetRequirementSubarea(string id, string expectedPrefix) =>
        SplitRequirementId(id, expectedPrefix) is { Length: >= 3 } parts ? parts[2] : null;

    private static string[]? SplitRequirementId(string id, string expectedPrefix)
    {
        if (!IsValidRequirementId(id, expectedPrefix))
            return null;
        return id.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
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
