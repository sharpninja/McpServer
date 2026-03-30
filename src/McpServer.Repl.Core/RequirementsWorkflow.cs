using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Client;
using McpServer.Client.Models;

namespace McpServer.Repl.Core;

/// <summary>
/// Production implementation of requirements workflow that integrates with RequirementsClient.
/// </summary>
public sealed class RequirementsWorkflow : IRequirementsWorkflow
{
    private readonly RequirementsClient _client;
    private RequirementsSelectionState? _selection = null;

    private static readonly Regex FrIdPattern = new(@"^FR-[A-Z]+-\d{3}$", RegexOptions.Compiled);
    private static readonly Regex TrIdPattern = new(@"^TR-[A-Z]+-[A-Z]+-\d{3}$", RegexOptions.Compiled);
    private static readonly Regex TestIdPattern = new(@"^TEST-[A-Z]+-\d{3}$", RegexOptions.Compiled);

    /// <summary>
    /// Initializes a new instance of RequirementsWorkflow with the specified client.
    /// </summary>
    /// <param name="client">The requirements client for API operations.</param>
    public RequirementsWorkflow(RequirementsClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    /// <inheritdoc />
    public async Task<IFrQueryResult> ListFrAsync(string? area = null, string? status = null, CancellationToken cancellationToken = default)
    {
        var entries = await _client.ListFrAsync(cancellationToken);
        var filtered = entries.AsEnumerable();

        if (!string.IsNullOrEmpty(area))
        {
            filtered = filtered.Where(e => ExtractArea(e.Id) == area);
        }

        if (!string.IsNullOrEmpty(status))
        {
            filtered = filtered.Where(e => e.Id.Contains(status));
        }

        var items = filtered.Select(e => new FrItemAdapter(e)).ToList();
        return new FrQueryResultAdapter(items);
    }

    /// <inheritdoc />
    public async Task<IFrItem> GetFrAsync(string id, CancellationToken cancellationToken = default)
    {
        ValidateFrId(id);
        var entry = await _client.GetFrAsync(id, cancellationToken);
        return new FrItemAdapter(entry);
    }

    /// <inheritdoc />
    public async Task<IFrMutationResult> CreateFrAsync(IFrCreateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateFrId(request.Id);

        var clientRequest = new CreateFrRequest
        {
            Id = request.Id,
            Title = request.Title,
            Body = request.Description
        };

        try
        {
            var entry = await _client.CreateFrAsync(clientRequest, cancellationToken);
            return new FrMutationResultAdapter(true, new FrItemAdapter(entry));
        }
        catch (Exception ex) when (ex.Message.Contains("already exists"))
        {
            throw new InvalidOperationException($"FR item with ID {request.Id} already exists", ex);
        }
    }

    /// <inheritdoc />
    public async Task<IFrMutationResult> UpdateFrAsync(IFrUpdateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var frId = _selection?.FrId;
        if (string.IsNullOrEmpty(frId))
        {
            throw new InvalidOperationException("No FR is currently selected");
        }

        var clientRequest = new UpdateFrRequest
        {
            Title = request.Title ?? string.Empty,
            Body = request.Description ?? string.Empty
        };

        var entry = await _client.UpdateFrAsync(frId, clientRequest, cancellationToken);
        return new FrMutationResultAdapter(true, new FrItemAdapter(entry));
    }

    /// <inheritdoc />
    public async Task DeleteFrAsync(string id, CancellationToken cancellationToken = default)
    {
        ValidateFrId(id);
        await _client.DeleteFrAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ITrQueryResult> ListTrAsync(string? area = null, string? subarea = null, string? status = null, CancellationToken cancellationToken = default)
    {
        var entries = await _client.ListTrAsync(cancellationToken);
        var filtered = entries.AsEnumerable();

        if (!string.IsNullOrEmpty(area))
        {
            filtered = filtered.Where(e => ExtractTrArea(e.Id) == area);
        }

        if (!string.IsNullOrEmpty(subarea))
        {
            filtered = filtered.Where(e => ExtractTrSubarea(e.Id) == subarea);
        }

        if (!string.IsNullOrEmpty(status))
        {
            filtered = filtered.Where(e => e.Id.Contains(status));
        }

        var items = filtered.Select(e => new TrItemAdapter(e)).ToList();
        return new TrQueryResultAdapter(items);
    }

    /// <inheritdoc />
    public async Task<ITrItem> GetTrAsync(string id, CancellationToken cancellationToken = default)
    {
        ValidateTrId(id);
        var entry = await _client.GetTrAsync(id, cancellationToken);
        return new TrItemAdapter(entry);
    }

    /// <inheritdoc />
    public async Task<ITrMutationResult> CreateTrAsync(ITrCreateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateTrId(request.Id);

        var clientRequest = new CreateTrRequest
        {
            Id = request.Id,
            Title = request.Title,
            Body = request.Description
        };

        try
        {
            var entry = await _client.CreateTrAsync(clientRequest, cancellationToken);
            return new TrMutationResultAdapter(true, new TrItemAdapter(entry));
        }
        catch (Exception ex) when (ex.Message.Contains("already exists"))
        {
            throw new InvalidOperationException($"TR item with ID {request.Id} already exists", ex);
        }
    }

    /// <inheritdoc />
    public async Task<ITrMutationResult> UpdateTrAsync(ITrUpdateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var trId = _selection?.TrId;
        if (string.IsNullOrEmpty(trId))
        {
            throw new InvalidOperationException("No TR is currently selected");
        }

        var clientRequest = new UpdateTrRequest
        {
            Title = request.Title,
            Body = request.Description ?? string.Empty
        };

        var entry = await _client.UpdateTrAsync(trId, clientRequest, cancellationToken);
        return new TrMutationResultAdapter(true, new TrItemAdapter(entry));
    }

    /// <inheritdoc />
    public async Task DeleteTrAsync(string id, CancellationToken cancellationToken = default)
    {
        ValidateTrId(id);
        await _client.DeleteTrAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ITestQueryResult> ListTestAsync(string? area = null, string? status = null, CancellationToken cancellationToken = default)
    {
        var entries = await _client.ListTestAsync(cancellationToken);
        var filtered = entries.AsEnumerable();

        if (!string.IsNullOrEmpty(area))
        {
            filtered = filtered.Where(e => ExtractTestArea(e.Id) == area);
        }

        if (!string.IsNullOrEmpty(status))
        {
            filtered = filtered.Where(e => e.Id.Contains(status));
        }

        var items = filtered.Select(e => new TestItemAdapter(e)).ToList();
        return new TestQueryResultAdapter(items);
    }

    /// <inheritdoc />
    public async Task<ITestItem> GetTestAsync(string id, CancellationToken cancellationToken = default)
    {
        ValidateTestId(id);
        var entry = await _client.GetTestAsync(id, cancellationToken);
        return new TestItemAdapter(entry);
    }

    /// <inheritdoc />
    public async Task<ITestMutationResult> CreateTestAsync(ITestCreateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateTestId(request.Id);

        var clientRequest = new CreateTestRequest
        {
            Id = request.Id,
            Condition = request.Description
        };

        try
        {
            var entry = await _client.CreateTestAsync(clientRequest, cancellationToken);
            return new TestMutationResultAdapter(true, new TestItemAdapter(entry));
        }
        catch (Exception ex) when (ex.Message.Contains("already exists"))
        {
            throw new InvalidOperationException($"TEST item with ID {request.Id} already exists", ex);
        }
    }

    /// <inheritdoc />
    public async Task<ITestMutationResult> UpdateTestAsync(ITestUpdateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var testId = _selection?.TestId;
        if (string.IsNullOrEmpty(testId))
        {
            throw new InvalidOperationException("No TEST is currently selected");
        }

        var clientRequest = new UpdateTestRequest
        {
            Condition = request.Description ?? string.Empty
        };

        var entry = await _client.UpdateTestAsync(testId, clientRequest, cancellationToken);
        return new TestMutationResultAdapter(true, new TestItemAdapter(entry));
    }

    /// <inheritdoc />
    public async Task DeleteTestAsync(string id, CancellationToken cancellationToken = default)
    {
        ValidateTestId(id);
        await _client.DeleteTestAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IMappingQueryResult> ListMappingsAsync(string? frId = null, string? trId = null, string? testId = null, CancellationToken cancellationToken = default)
    {
        var mappings = await _client.ListMappingsAsync(cancellationToken);
        var filtered = mappings.AsEnumerable();

        if (!string.IsNullOrEmpty(frId))
        {
            filtered = filtered.Where(m => m.FrId == frId);
        }

        if (!string.IsNullOrEmpty(trId))
        {
            filtered = filtered.Where(m => m.TrIds.Contains(trId));
        }

        var items = filtered.SelectMany(m => m.TrIds.Select(tr => new MappingItemAdapter(m.FrId, tr, testId, null))).ToList();
        return new MappingQueryResultAdapter(items);
    }

    /// <inheritdoc />
    public async Task<IMappingMutationResult> CreateMappingAsync(IMappingCreateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrEmpty(request.FrId) && string.IsNullOrEmpty(request.TrId) && string.IsNullOrEmpty(request.TestId))
        {
            throw new ArgumentException("At least one requirement ID must be provided");
        }

        if (!string.IsNullOrEmpty(request.FrId))
        {
            ValidateFrId(request.FrId);
            try
            {
                await _client.GetFrAsync(request.FrId, cancellationToken);
            }
            catch
            {
                throw new InvalidOperationException($"Referenced FR does not exist: {request.FrId}");
            }
        }

        if (!string.IsNullOrEmpty(request.TrId))
        {
            ValidateTrId(request.TrId);
            try
            {
                await _client.GetTrAsync(request.TrId, cancellationToken);
            }
            catch
            {
                throw new InvalidOperationException($"Referenced TR does not exist: {request.TrId}");
            }
        }

        if (!string.IsNullOrEmpty(request.TestId))
        {
            ValidateTestId(request.TestId);
            try
            {
                await _client.GetTestAsync(request.TestId, cancellationToken);
            }
            catch
            {
                throw new InvalidOperationException($"Referenced TEST does not exist: {request.TestId}");
            }
        }

        if (!string.IsNullOrEmpty(request.FrId) && !string.IsNullOrEmpty(request.TrId))
        {
            var upsertRequest = new UpsertFrTrMappingRequest
            {
                TrIds = new[] { request.TrId }
            };

            var mapping = await _client.UpsertMappingAsync(request.FrId, upsertRequest, cancellationToken);
            var item = new MappingItemAdapter(mapping.FrId, request.TrId, request.TestId, request.Notes);
            return new MappingMutationResultAdapter(true, item);
        }

        var mappingItem = new MappingItemAdapter(request.FrId, request.TrId, request.TestId, request.Notes);
        return new MappingMutationResultAdapter(true, mappingItem);
    }

    /// <inheritdoc />
    public async Task DeleteMappingAsync(string? frId = null, string? trId = null, string? testId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(frId) && string.IsNullOrEmpty(trId) && string.IsNullOrEmpty(testId))
        {
            throw new ArgumentException("At least one requirement ID must be provided");
        }

        if (!string.IsNullOrEmpty(frId))
        {
            try
            {
                await _client.DeleteMappingAsync(frId, cancellationToken);
            }
            catch
            {
                throw new InvalidOperationException("Mapping not found");
            }
        }
        else
        {
            throw new InvalidOperationException("Mapping not found");
        }
    }

    /// <inheritdoc />
    public async Task<IDocumentGenerationResult> GenerateDocumentAsync(string format, string docType, CancellationToken cancellationToken = default)
    {
        ValidateFormat(format);
        ValidateDocType(docType);

        var docParam = docType switch
        {
            "fr" => "functional",
            "tr" => "technical",
            "test" => "testing",
            "matrix" => "mapping",
            "all" => "all",
            _ => throw new ArgumentException($"Invalid docType: {docType}. Valid values: fr, tr, test, matrix, all")
        };

        var generatedDoc = await _client.GenerateAsync(docParam, cancellationToken);

        var content = format == "markdown"
            ? System.Text.Encoding.UTF8.GetString(generatedDoc.Content)
            : System.Text.Encoding.UTF8.GetString(generatedDoc.Content);

        return new DocumentGenerationResultAdapter(true, content, format, docType);
    }

    /// <inheritdoc />
    public async Task<IDocumentIngestionResult> IngestDocumentAsync(string content, string format, string mergeStrategy, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(content))
        {
            throw new ArgumentException("Content cannot be null or empty");
        }

        ValidateFormat(format);
        ValidateMergeStrategy(mergeStrategy);

        var request = new RequirementsIngestRequest
        {
            FunctionalMarkdown = content,
            TechnicalMarkdown = content,
            TestingMarkdown = content,
            MappingMarkdown = content
        };

        var result = await _client.IngestAsync(request, cancellationToken);

        return new DocumentIngestionResultAdapter(
            true,
            result.FunctionalAdded,
            result.FunctionalUpdated,
            result.TechnicalAdded,
            result.TechnicalUpdated,
            result.TestingAdded,
            result.TestingUpdated,
            result.MappingAdded,
            new List<IIngestionConflict>()
        );
    }

    /// <inheritdoc />
    public IRequirementsSelectionState? CurrentSelection()
    {
        return _selection;
    }

    private static void ValidateFrId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("FR ID cannot be null or empty", nameof(id));
        }

        if (!FrIdPattern.IsMatch(id))
        {
            throw new ArgumentException($"Invalid FR ID format: {id}");
        }
    }

    private static void ValidateTrId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("TR ID cannot be null or empty", nameof(id));
        }

        if (!TrIdPattern.IsMatch(id))
        {
            throw new ArgumentException($"Invalid TR ID format: {id}. Expected format: TR-<AREA>-<SUBAREA>-###");
        }
    }

    private static void ValidateTestId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("TEST ID cannot be null or empty", nameof(id));
        }

        if (!TestIdPattern.IsMatch(id))
        {
            throw new ArgumentException($"Invalid TEST ID format: {id}. Expected format: TEST-<AREA>-###");
        }
    }

    private static void ValidateFormat(string format)
    {
        if (format != "markdown" && format != "yaml")
        {
            throw new ArgumentException($"Invalid format: {format}. Valid values: markdown, yaml");
        }
    }

    private static void ValidateDocType(string docType)
    {
        if (docType != "fr" && docType != "tr" && docType != "test" && docType != "matrix" && docType != "all")
        {
            throw new ArgumentException($"Invalid docType: {docType}. Valid values: fr, tr, test, matrix, all");
        }
    }

    private static void ValidateMergeStrategy(string mergeStrategy)
    {
        if (mergeStrategy != "overwrite" && mergeStrategy != "merge" && mergeStrategy != "skip")
        {
            throw new ArgumentException($"Invalid mergeStrategy: {mergeStrategy}. Valid values: overwrite, merge, skip");
        }
    }

    private static string ExtractArea(string id)
    {
        var parts = id.Split('-');
        return parts.Length > 1 ? parts[1] : string.Empty;
    }

    private static string ExtractTrArea(string id)
    {
        var parts = id.Split('-');
        return parts.Length > 1 ? parts[1] : string.Empty;
    }

    private static string ExtractTrSubarea(string id)
    {
        var parts = id.Split('-');
        return parts.Length > 2 ? parts[2] : string.Empty;
    }

    private static string ExtractTestArea(string id)
    {
        var parts = id.Split('-');
        return parts.Length > 1 ? parts[1] : string.Empty;
    }
}

internal sealed class RequirementsSelectionState : IRequirementsSelectionState
{
    public string? FrId { get; set; }
    public string? TrId { get; set; }
    public string? TestId { get; set; }
    public DateTimeOffset SelectedAt { get; set; } = DateTimeOffset.UtcNow;
}

internal sealed class FrItemAdapter : IFrItem
{
    private readonly FrEntry _entry;

    public FrItemAdapter(FrEntry entry)
    {
        _entry = entry;
    }

    public string Id => _entry.Id;
    public string Title => _entry.Title;
    public string Description => _entry.Body;
    public string Status => "pending";
    public string Priority => "medium";
    public string Area => ExtractArea(_entry.Id);
    public string? Notes => null;
    public string CreatedAt => DateTimeOffset.UtcNow.ToString("o");
    public string UpdatedAt => DateTimeOffset.UtcNow.ToString("o");

    private static string ExtractArea(string id)
    {
        var parts = id.Split('-');
        return parts.Length > 1 ? parts[1] : string.Empty;
    }
}

internal sealed class FrQueryResultAdapter : IFrQueryResult
{
    private readonly IReadOnlyList<IFrItem> _items;

    public FrQueryResultAdapter(IReadOnlyList<IFrItem> items)
    {
        _items = items;
    }

    public IReadOnlyList<IFrItem> Items => _items;
    public int TotalCount => _items.Count;
}

internal sealed class FrMutationResultAdapter : IFrMutationResult
{
    public FrMutationResultAdapter(bool success, IFrItem item)
    {
        Success = success;
        Item = item;
    }

    public bool Success { get; }
    public IFrItem Item { get; }
}

internal sealed class TrItemAdapter : ITrItem
{
    private readonly TrEntry _entry;

    public TrItemAdapter(TrEntry entry)
    {
        _entry = entry;
    }

    public string Id => _entry.Id;
    public string Title => _entry.Title;
    public string Description => _entry.Body;
    public string Status => "pending";
    public string Priority => "medium";
    public string Area => ExtractArea(_entry.Id);
    public string Subarea => ExtractSubarea(_entry.Id);
    public string? Notes => null;
    public string CreatedAt => DateTimeOffset.UtcNow.ToString("o");
    public string UpdatedAt => DateTimeOffset.UtcNow.ToString("o");

    private static string ExtractArea(string id)
    {
        var parts = id.Split('-');
        return parts.Length > 1 ? parts[1] : string.Empty;
    }

    private static string ExtractSubarea(string id)
    {
        var parts = id.Split('-');
        return parts.Length > 2 ? parts[2] : string.Empty;
    }
}

internal sealed class TrQueryResultAdapter : ITrQueryResult
{
    private readonly IReadOnlyList<ITrItem> _items;

    public TrQueryResultAdapter(IReadOnlyList<ITrItem> items)
    {
        _items = items;
    }

    public IReadOnlyList<ITrItem> Items => _items;
    public int TotalCount => _items.Count;
}

internal sealed class TrMutationResultAdapter : ITrMutationResult
{
    public TrMutationResultAdapter(bool success, ITrItem item)
    {
        Success = success;
        Item = item;
    }

    public bool Success { get; }
    public ITrItem Item { get; }
}

internal sealed class TestItemAdapter : ITestItem
{
    private readonly TestEntry _entry;

    public TestItemAdapter(TestEntry entry)
    {
        _entry = entry;
    }

    public string Id => _entry.Id;
    public string Title => $"Test {_entry.Id}";
    public string Description => _entry.Condition;
    public string Status => "pending";
    public string Priority => "medium";
    public string Area => ExtractArea(_entry.Id);
    public string TestType => "unit";
    public string? Notes => null;
    public string CreatedAt => DateTimeOffset.UtcNow.ToString("o");
    public string UpdatedAt => DateTimeOffset.UtcNow.ToString("o");

    private static string ExtractArea(string id)
    {
        var parts = id.Split('-');
        return parts.Length > 1 ? parts[1] : string.Empty;
    }
}

internal sealed class TestQueryResultAdapter : ITestQueryResult
{
    private readonly IReadOnlyList<ITestItem> _items;

    public TestQueryResultAdapter(IReadOnlyList<ITestItem> items)
    {
        _items = items;
    }

    public IReadOnlyList<ITestItem> Items => _items;
    public int TotalCount => _items.Count;
}

internal sealed class TestMutationResultAdapter : ITestMutationResult
{
    public TestMutationResultAdapter(bool success, ITestItem item)
    {
        Success = success;
        Item = item;
    }

    public bool Success { get; }
    public ITestItem Item { get; }
}

internal sealed class MappingItemAdapter : IMappingItem
{
    public MappingItemAdapter(string? frId, string? trId, string? testId, string? notes)
    {
        FrId = frId;
        TrId = trId;
        TestId = testId;
        Notes = notes;
    }

    public string? FrId { get; }
    public string? TrId { get; }
    public string? TestId { get; }
    public string CreatedAt => DateTimeOffset.UtcNow.ToString("o");
    public string? Notes { get; }
}

internal sealed class MappingQueryResultAdapter : IMappingQueryResult
{
    private readonly IReadOnlyList<IMappingItem> _items;

    public MappingQueryResultAdapter(IReadOnlyList<IMappingItem> items)
    {
        _items = items;
    }

    public IReadOnlyList<IMappingItem> Items => _items;
    public int TotalCount => _items.Count;
}

internal sealed class MappingMutationResultAdapter : IMappingMutationResult
{
    public MappingMutationResultAdapter(bool success, IMappingItem item)
    {
        Success = success;
        Item = item;
    }

    public bool Success { get; }
    public IMappingItem Item { get; }
}

internal sealed class DocumentGenerationResultAdapter : IDocumentGenerationResult
{
    public DocumentGenerationResultAdapter(bool success, string content, string format, string docType)
    {
        Success = success;
        Content = content;
        Format = format;
        DocType = docType;
        GeneratedAt = DateTimeOffset.UtcNow.ToString("o");
    }

    public bool Success { get; }
    public string Content { get; }
    public string Format { get; }
    public string DocType { get; }
    public string GeneratedAt { get; }
}

internal sealed class DocumentIngestionResultAdapter : IDocumentIngestionResult
{
    public DocumentIngestionResultAdapter(
        bool success,
        int frCreated,
        int frUpdated,
        int trCreated,
        int trUpdated,
        int testCreated,
        int testUpdated,
        int mappingsCreated,
        IReadOnlyList<IIngestionConflict> conflicts)
    {
        Success = success;
        FrCreated = frCreated;
        FrUpdated = frUpdated;
        TrCreated = trCreated;
        TrUpdated = trUpdated;
        TestCreated = testCreated;
        TestUpdated = testUpdated;
        MappingsCreated = mappingsCreated;
        Conflicts = conflicts;
        IngestedAt = DateTimeOffset.UtcNow.ToString("o");
    }

    public bool Success { get; }
    public int FrCreated { get; }
    public int FrUpdated { get; }
    public int TrCreated { get; }
    public int TrUpdated { get; }
    public int TestCreated { get; }
    public int TestUpdated { get; }
    public int MappingsCreated { get; }
    public IReadOnlyList<IIngestionConflict> Conflicts { get; }
    public string IngestedAt { get; }
}
