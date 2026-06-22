// FR-MCP-REPL-001: YAML Protocol STDIO REPL Host - Requirements workflow operations
// FR-MCP-REPL-003: Command Namespace Parity - Requirements management operations via REPL
// TR-MCP-REPL-005: Namespace Organization and Handler Parity - Requirements command handlers
// TEST-MCP-REPL-009: Requirements REPL commands match REST endpoint semantics

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
// FR-MCP-REPL-001: YAML Protocol STDIO REPL Host - Requirements workflow implementation
// FR-MCP-REPL-003: Command Namespace Parity - Requirements operation implementation
// TR-MCP-REPL-004: Command Registry and Dispatcher - Requirements workflow handler
// TR-MCP-REPL-005: Namespace Organization and Handler Parity - Requirements workflow delegation
// TEST-MCP-REPL-009: Requirements management operations validate requirement identifier rules
// TEST-MCP-REPL-019: Workflows delegate to typed client contracts without duplicating logic

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

    private static readonly Regex FrIdPattern = new(@"^FR-[A-Z0-9]+(?:-[A-Z0-9]+)*-\d{3}$", RegexOptions.Compiled);
    private static readonly Regex TrIdPattern = new(@"^TR-[A-Z0-9]+(?:-[A-Z0-9]+)+-\d{3}$", RegexOptions.Compiled);
    private static readonly Regex TestIdPattern = new(@"^TEST-[A-Z0-9]+(?:-[A-Z0-9]+)*-\d{3}$", RegexOptions.Compiled);

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
        var entries = await _client.ListFrAsync(area, status, cancellationToken);
        var items = entries.Select(e => new FrItemAdapter(e)).ToList();
        return new FrQueryResultAdapter(items);
    }

    /// <inheritdoc />
    public async Task<int> PurgeInvalidPlaceholdersAsync(CancellationToken cancellationToken = default)
    {
        // The underlying client call hits the /repair endpoint.
        return await _client.RepairFrPlaceholdersAsync(cancellationToken).ConfigureAwait(false);
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
            Body = request.Description,
            Priority = request.Priority,
            Notes = request.Notes,
            AcceptanceCriteria = request.AcceptanceCriteria,
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

        var frId = request.Id ?? _selection?.FrId;
        if (string.IsNullOrEmpty(frId))
        {
            throw new InvalidOperationException("No FR is currently selected");
        }

        ValidateFrId(frId);
        var clientRequest = new UpdateFrRequest
        {
            Title = request.Title,
            Body = request.Description,
            Priority = request.Priority,
            Status = request.Status,
            Notes = request.Notes,
            AcceptanceCriteria = request.AcceptanceCriteria,
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
            filtered = filtered.Where(e => string.Equals(e.Status, status, StringComparison.OrdinalIgnoreCase));
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
            Body = request.Description,
            Priority = request.Priority,
            Notes = request.Notes,
            AcceptanceCriteria = request.AcceptanceCriteria,
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

        var trId = request.Id ?? _selection?.TrId;
        if (string.IsNullOrEmpty(trId))
        {
            throw new InvalidOperationException("No TR is currently selected");
        }

        ValidateTrId(trId);
        var clientRequest = new UpdateTrRequest
        {
            Title = request.Title,
            Body = request.Description,
            Priority = request.Priority,
            Status = request.Status,
            Notes = request.Notes,
            AcceptanceCriteria = request.AcceptanceCriteria,
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
            filtered = filtered.Where(e => string.Equals(e.Status, status, StringComparison.OrdinalIgnoreCase));
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
            Title = request.Title,
            Condition = request.Description,
            Priority = request.Priority,
            Notes = request.Notes,
            AcceptanceCriteria = request.AcceptanceCriteria,
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

        var testId = request.Id ?? _selection?.TestId;
        if (string.IsNullOrEmpty(testId))
        {
            throw new InvalidOperationException("No TEST is currently selected");
        }

        ValidateTestId(testId);
        var clientRequest = new UpdateTestRequest
        {
            Title = request.Title,
            Condition = request.Description,
            Priority = request.Priority,
            Status = request.Status,
            Notes = request.Notes,
            AcceptanceCriteria = request.AcceptanceCriteria,
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
    public async Task<RequirementsBatchResult> CreateFrBatchAsync(CreateFrBatchRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        foreach (var record in RequireRecords(request.Records))
            ValidateFrId(record.Id ?? string.Empty);

        return await _client.CreateFrBatchAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RequirementsBatchResult> UpdateFrBatchAsync(UpdateFrBatchRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        foreach (var record in RequireRecords(request.Records))
            ValidateFrId(record.Id ?? string.Empty);

        return await _client.UpdateFrBatchAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RequirementsBatchResult> CreateTrBatchAsync(CreateTrBatchRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        foreach (var record in RequireRecords(request.Records))
            ValidateTrId(record.Id ?? string.Empty);

        return await _client.CreateTrBatchAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RequirementsBatchResult> UpdateTrBatchAsync(UpdateTrBatchRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        foreach (var record in RequireRecords(request.Records))
            ValidateTrId(record.Id ?? string.Empty);

        return await _client.UpdateTrBatchAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RequirementsBatchResult> CreateTestBatchAsync(CreateTestBatchRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        foreach (var record in RequireRecords(request.Records))
            ValidateTestId(record.Id ?? string.Empty);

        return await _client.CreateTestBatchAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RequirementsBatchResult> UpdateTestBatchAsync(UpdateTestBatchRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        foreach (var record in RequireRecords(request.Records))
            ValidateTestId(record.Id ?? string.Empty);

        return await _client.UpdateTestBatchAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RequirementsBatchResult> CreateBatchAsync(CreateRequirementsBatchRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        foreach (var record in RequireRecords(request.Records))
            ValidateBatchRecordId(record.Kind, record.Id);

        return await _client.CreateBatchAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RequirementsBatchResult> UpdateBatchAsync(UpdateRequirementsBatchRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        foreach (var record in RequireRecords(request.Records))
            ValidateBatchRecordId(record.Kind, record.Id);

        return await _client.UpdateBatchAsync(request, cancellationToken);
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

        if (!string.IsNullOrEmpty(testId))
        {
            filtered = filtered.Where(m => m.TestIds.Contains(testId));
        }

        var items = new List<IMappingItem>();
        foreach (var mapping in filtered)
        {
            var matchingTrIds = string.IsNullOrEmpty(trId)
                ? mapping.TrIds
                : mapping.TrIds.Where(id => id == trId).ToArray();
            var matchingTestIds = string.IsNullOrEmpty(testId)
                ? mapping.TestIds
                : mapping.TestIds.Where(id => id == testId).ToArray();

            if (!string.IsNullOrEmpty(trId) && !string.IsNullOrEmpty(testId))
            {
                items.AddRange(matchingTrIds.SelectMany(tr => matchingTestIds.Select(test => new MappingItemAdapter(mapping.FrId, tr, test, null))));
                continue;
            }

            items.AddRange(matchingTrIds.Select(tr => new MappingItemAdapter(mapping.FrId, tr, null, null)));
            items.AddRange(matchingTestIds.Select(test => new MappingItemAdapter(mapping.FrId, null, test, null)));
        }

        return new MappingQueryResultAdapter(items);
    }

    /// <inheritdoc />
    public async Task<IMappingMutationResult> CreateMappingAsync(IMappingCreateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var trIds = NormalizeIds(request.TrIds, request.TrId);
        var testIds = NormalizeIds(request.TestIds, request.TestId);

        if (string.IsNullOrEmpty(request.FrId))
        {
            throw new ArgumentException("FR ID is required for requirement mappings");
        }

        if (trIds.Count == 0 && testIds.Count == 0)
        {
            throw new ArgumentException("At least one TR or TEST ID must be provided");
        }

        ValidateFrId(request.FrId);
        try
        {
            await _client.GetFrAsync(request.FrId, cancellationToken);
        }
        catch
        {
            throw new InvalidOperationException($"Referenced FR does not exist: {request.FrId}");
        }

        foreach (var id in trIds)
        {
            ValidateTrId(id);
            try
            {
                await _client.GetTrAsync(id, cancellationToken);
            }
            catch
            {
                throw new InvalidOperationException($"Referenced TR does not exist: {id}");
            }
        }

        foreach (var id in testIds)
        {
            ValidateTestId(id);
            try
            {
                await _client.GetTestAsync(id, cancellationToken);
            }
            catch
            {
                throw new InvalidOperationException($"Referenced TEST does not exist: {id}");
            }
        }

        try
        {
            var existing = await _client.GetMappingAsync(request.FrId, cancellationToken);
            trIds = NormalizeIds(existing.TrIds.Concat(trIds), null);
            testIds = NormalizeIds(existing.TestIds.Concat(testIds), null);
        }
        catch
        {
            // No existing mapping for this FR; create one below.
        }

        var upsertRequest = new UpsertFrTrMappingRequest
        {
            TrIds = trIds,
            TestIds = testIds
        };

        var mapping = await _client.UpsertMappingAsync(request.FrId, upsertRequest, cancellationToken);
        var item = new MappingItemAdapter(
            mapping.FrId,
            trIds.FirstOrDefault(),
            testIds.FirstOrDefault(),
            request.Notes);
        return new MappingMutationResultAdapter(true, item);
    }

    /// <inheritdoc />
    public async Task DeleteMappingAsync(string? frId = null, string? trId = null, string? testId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(frId) && string.IsNullOrEmpty(trId) && string.IsNullOrEmpty(testId))
        {
            throw new ArgumentException("At least one requirement ID must be provided");
        }

        var mappings = await _client.ListMappingsAsync(cancellationToken);
        var matching = mappings.AsEnumerable();

        if (!string.IsNullOrEmpty(frId))
        {
            ValidateFrId(frId);
            matching = matching.Where(m => m.FrId == frId);
        }

        if (!string.IsNullOrEmpty(trId))
        {
            ValidateTrId(trId);
            matching = matching.Where(m => m.TrIds.Contains(trId));
        }

        if (!string.IsNullOrEmpty(testId))
        {
            ValidateTestId(testId);
            matching = matching.Where(m => m.TestIds.Contains(testId));
        }

        var targets = matching.ToList();
        if (targets.Count == 0)
        {
            throw new InvalidOperationException("Mapping not found");
        }

        foreach (var mapping in targets)
        {
            if (string.IsNullOrEmpty(trId) && string.IsNullOrEmpty(testId))
            {
                await _client.DeleteMappingAsync(mapping.FrId, cancellationToken);
                continue;
            }

            var remainingTrIds = string.IsNullOrEmpty(trId)
                ? mapping.TrIds
                : mapping.TrIds.Where(id => id != trId).ToArray();
            var remainingTestIds = string.IsNullOrEmpty(testId)
                ? mapping.TestIds
                : mapping.TestIds.Where(id => id != testId).ToArray();

            await _client.UpsertMappingAsync(mapping.FrId, new UpsertFrTrMappingRequest
            {
                TrIds = remainingTrIds,
                TestIds = remainingTestIds
            }, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task<IDocumentGenerationResult> GenerateDocumentAsync(string format, string docType, CancellationToken cancellationToken = default)
    {
        ValidateFormat(format);
        ValidateDocType(docType);

        if (format == "wiki" && docType != "all")
        {
            throw new ArgumentException("Wiki generation requires docType=all");
        }

        var docParam = docType switch
        {
            "fr" => "functional",
            "tr" => "technical",
            "test" => "testing",
            "matrix" => "matrix",
            "all" => "all",
            _ => throw new ArgumentException($"Invalid docType: {docType}. Valid values: fr, tr, test, matrix, all")
        };

        var generatedDoc = await _client.GenerateAsync(docParam, format, cancellationToken);

        if (generatedDoc.ExportResult is not null)
        {
            var export = generatedDoc.ExportResult;
            return new DocumentGenerationResultAdapter(
                true,
                content: string.Empty,
                export.Format,
                export.DocType,
                contentType: generatedDoc.ContentType ?? "application/json",
                outputRoot: export.OutputRoot,
                files: export.Files,
                generatedAt: export.GeneratedAtUtc);
        }

        var contentType = generatedDoc.ContentType ?? "text/markdown";
        if (format == "wiki" || contentType.Contains("zip", StringComparison.OrdinalIgnoreCase))
        {
            return new DocumentGenerationResultAdapter(
                true,
                content: string.Empty,
                format,
                docType,
                contentBase64: Convert.ToBase64String(generatedDoc.Content),
                contentType: contentType,
                fileName: "requirements-wiki-documents.zip");
        }

        var content = System.Text.Encoding.UTF8.GetString(generatedDoc.Content);

        return new DocumentGenerationResultAdapter(
            true,
            content,
            format,
            docType,
            contentType: contentType,
            fileName: null);
    }

    /// <inheritdoc />
    public async Task<IDocumentIngestionResult> IngestDocumentAsync(string content, string format, string mergeStrategy, CancellationToken cancellationToken = default)
        => await IngestDocumentAsync(content, format, mergeStrategy, documents: null, sourceFormat: null, preferredWikiFormat: null, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IDocumentIngestionResult> IngestDocumentAsync(
        string content,
        string format,
        string mergeStrategy,
        IReadOnlyDictionary<string, RequirementsIngestDocument>? documents,
        string? sourceFormat,
        string? preferredWikiFormat,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(content) && documents is not { Count: > 0 })
        {
            throw new ArgumentException("Content or documents cannot be null or empty");
        }

        ValidateFormat(format);
        ValidateMergeStrategy(mergeStrategy);

        var request = documents is { Count: > 0 }
            ? new RequirementsIngestRequest
            {
                SourceFormat = sourceFormat ?? (format == "wiki" ? "wiki" : "auto"),
                PreferredWikiFormat = preferredWikiFormat,
                Documents = documents
            }
            : new RequirementsIngestRequest
            {
                SourceFormat = sourceFormat,
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

    private static IReadOnlyList<T> RequireRecords<T>(IReadOnlyList<T>? records)
    {
        if (records is null || records.Count == 0)
        {
            throw new ArgumentException("Batch records array cannot be null or empty", nameof(records));
        }

        return records;
    }

    private static void ValidateBatchRecordId(string? kind, string? id)
    {
        switch ((kind ?? string.Empty).Trim().ToLowerInvariant())
        {
            case "fr":
            case "functional":
                ValidateFrId(id ?? string.Empty);
                return;
            case "tr":
            case "technical":
                ValidateTrId(id ?? string.Empty);
                return;
            case "test":
            case "testing":
                ValidateTestId(id ?? string.Empty);
                return;
            default:
                throw new ArgumentException($"Invalid requirement kind: {kind}. Valid values: fr, tr, test");
        }
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
            throw new ArgumentException($"Invalid TR ID format: {id}. Expected format: TR-<AREA>-<SUBAREA>[-<QUALIFIER>]-###");
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
            throw new ArgumentException($"Invalid TEST ID format: {id}. Expected format: TEST-<AREA>[-<QUALIFIER>]-###");
        }
    }

    private static void ValidateFormat(string format)
    {
        if (format != "markdown" && format != "yaml" && format != "wiki")
        {
            throw new ArgumentException($"Invalid format: {format}. Valid values: markdown, yaml, wiki");
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

    private static IReadOnlyList<string> NormalizeIds(IEnumerable<string>? ids, string? singleId)
    {
        var values = new List<string>();
        if (ids is not null)
        {
            values.AddRange(ids.Where(id => !string.IsNullOrWhiteSpace(id)));
        }

        if (!string.IsNullOrWhiteSpace(singleId))
        {
            values.Add(singleId);
        }

        return values
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
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
    public string Status => _entry.Status;
    public string Priority => _entry.Priority;
    public string Area => ExtractArea(_entry.Id);
    public string? Notes => _entry.Notes;
    /// <inheritdoc />
    public IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria => _entry.AcceptanceCriteria;
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
    public string Status => _entry.Status;
    public string Priority => _entry.Priority;
    public string Area => ExtractArea(_entry.Id);
    public string Subarea => ExtractSubarea(_entry.Id);
    public string? Notes => _entry.Notes;
    /// <inheritdoc />
    public IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria => _entry.AcceptanceCriteria;
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
    public string Title => string.IsNullOrWhiteSpace(_entry.Title) ? $"Test {_entry.Id}" : _entry.Title;
    public string Description => _entry.Condition;
    public string Status => _entry.Status;
    public string Priority => _entry.Priority;
    public string Area => ExtractArea(_entry.Id);
    public string TestType => "unit";
    public string? Notes => _entry.Notes;
    /// <inheritdoc />
    public IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria => _entry.AcceptanceCriteria;
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
    public DocumentGenerationResultAdapter(
        bool success,
        string content,
        string format,
        string docType,
        string? contentBase64 = null,
        string? contentType = null,
        string? fileName = null,
        string? outputRoot = null,
        IReadOnlyList<RequirementsDocumentExportFile>? files = null,
        DateTimeOffset? generatedAt = null)
    {
        Success = success;
        Content = content;
        Format = format;
        DocType = docType;
        ContentBase64 = contentBase64;
        ContentType = contentType;
        FileName = fileName;
        OutputRoot = outputRoot;
        Files = files ?? [];
        GeneratedAt = (generatedAt ?? DateTimeOffset.UtcNow).ToString("o");
    }

    public bool Success { get; }
    public string Content { get; }
    public string? ContentBase64 { get; }
    public string? ContentType { get; }
    public string? FileName { get; }
    public string? OutputRoot { get; }
    public IReadOnlyList<RequirementsDocumentExportFile> Files { get; }
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
