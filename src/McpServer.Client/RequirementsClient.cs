using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Client.Models;

namespace McpServer.Client;

/// <summary>
/// Client for requirements endpoints (<c>/mcpserver/requirements</c>), including CRUD
/// operations for FR/TR/TEST entries, FR-to-TR mapping management, and document generation.
/// </summary>
/// <seealso cref="McpServerClient.Requirements"/>
public sealed class RequirementsClient : McpClientBase
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <inheritdoc />
    public RequirementsClient(HttpClient http, McpServerClientOptions options)
        : base(http, options) { }

    internal RequirementsClient(HttpClient http, McpServerClientOptions options, WorkspacePathHolder holder)
        : base(http, options, holder) { }

    /// <summary>Lists functional requirements, optionally filtered by area or status.</summary>
    public async Task<IReadOnlyList<FrEntry>> ListFrAsync(string? area = null, string? status = null, CancellationToken cancellationToken = default)
    {
        var url = BuildQueryUrl("mcpserver/requirements/fr", ("area", area), ("status", status));
        return await GetAsync<IReadOnlyList<FrEntry>>(url, cancellationToken);
    }

    /// <summary>Lists all functional requirements (unfiltered).</summary>
    public async Task<IReadOnlyList<FrEntry>> ListFrAsync(CancellationToken cancellationToken = default)
        => await ListFrAsync(null, null, cancellationToken);

    /// <summary>Repairs the FR catalog by purging invalid backfilled placeholders. Returns purged count.</summary>
    public async Task<int> RepairFrPlaceholdersAsync(CancellationToken cancellationToken = default)
    {
        var result = await PostAsync<object>("mcpserver/requirements/fr/repair", null, cancellationToken).ConfigureAwait(false);
        if (result is System.Text.Json.JsonElement je && je.TryGetProperty("purged", out var p) && p.TryGetInt32(out var n))
            return n;
        if (result is System.Collections.IDictionary dict && dict["purged"] is int i)
            return i;
        return 0;
    }

    /// <summary>Gets a functional requirement by ID.</summary>
    public async Task<FrEntry> GetFrAsync(string id, CancellationToken cancellationToken = default)
    {
        return await GetAsync<FrEntry>($"mcpserver/requirements/fr/{Uri.EscapeDataString(id)}", cancellationToken);
    }

    /// <summary>Creates a new functional requirement.</summary>
    public async Task<FrEntry> CreateFrAsync(CreateFrRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<FrEntry>("mcpserver/requirements/fr", request, cancellationToken);
    }

    /// <summary>Updates an existing functional requirement.</summary>
    public async Task<FrEntry> UpdateFrAsync(string id, UpdateFrRequest request, CancellationToken cancellationToken = default)
    {
        return await PutAsync<FrEntry>($"mcpserver/requirements/fr/{Uri.EscapeDataString(id)}", request, cancellationToken);
    }

    /// <summary>Creates multiple functional requirements atomically.</summary>
    public async Task<RequirementsBatchResult> CreateFrBatchAsync(CreateFrBatchRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<RequirementsBatchResult>("mcpserver/requirements/fr/batch", request, cancellationToken);
    }

    /// <summary>Updates multiple functional requirements atomically.</summary>
    public async Task<RequirementsBatchResult> UpdateFrBatchAsync(UpdateFrBatchRequest request, CancellationToken cancellationToken = default)
    {
        return await PutAsync<RequirementsBatchResult>("mcpserver/requirements/fr/batch", request, cancellationToken);
    }

    /// <summary>Deletes a functional requirement by ID.</summary>
    public async Task<RequirementsMutationResult> DeleteFrAsync(string id, CancellationToken cancellationToken = default)
    {
        return await DeleteAsync<RequirementsMutationResult>($"mcpserver/requirements/fr/{Uri.EscapeDataString(id)}", cancellationToken);
    }

    /// <summary>Lists technical requirements, optionally filtered by area, subarea, or status.</summary>
    public async Task<IReadOnlyList<TrEntry>> ListTrAsync(string? area = null, string? subarea = null, string? status = null, CancellationToken cancellationToken = default)
    {
        var url = BuildQueryUrl("mcpserver/requirements/tr", ("area", area), ("subarea", subarea), ("status", status));
        return await GetAsync<IReadOnlyList<TrEntry>>(url, cancellationToken);
    }

    /// <summary>Lists all technical requirements (unfiltered).</summary>
    public async Task<IReadOnlyList<TrEntry>> ListTrAsync(CancellationToken cancellationToken = default)
        => await ListTrAsync(null, null, null, cancellationToken);

    /// <summary>Gets a technical requirement by ID.</summary>
    public async Task<TrEntry> GetTrAsync(string id, CancellationToken cancellationToken = default)
    {
        return await GetAsync<TrEntry>($"mcpserver/requirements/tr/{Uri.EscapeDataString(id)}", cancellationToken);
    }

    /// <summary>Creates a new technical requirement.</summary>
    public async Task<TrEntry> CreateTrAsync(CreateTrRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<TrEntry>("mcpserver/requirements/tr", request, cancellationToken);
    }

    /// <summary>Updates an existing technical requirement.</summary>
    public async Task<TrEntry> UpdateTrAsync(string id, UpdateTrRequest request, CancellationToken cancellationToken = default)
    {
        return await PutAsync<TrEntry>($"mcpserver/requirements/tr/{Uri.EscapeDataString(id)}", request, cancellationToken);
    }

    /// <summary>Creates multiple technical requirements atomically.</summary>
    public async Task<RequirementsBatchResult> CreateTrBatchAsync(CreateTrBatchRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<RequirementsBatchResult>("mcpserver/requirements/tr/batch", request, cancellationToken);
    }

    /// <summary>Updates multiple technical requirements atomically.</summary>
    public async Task<RequirementsBatchResult> UpdateTrBatchAsync(UpdateTrBatchRequest request, CancellationToken cancellationToken = default)
    {
        return await PutAsync<RequirementsBatchResult>("mcpserver/requirements/tr/batch", request, cancellationToken);
    }

    /// <summary>Deletes a technical requirement by ID.</summary>
    public async Task<RequirementsMutationResult> DeleteTrAsync(string id, CancellationToken cancellationToken = default)
    {
        return await DeleteAsync<RequirementsMutationResult>($"mcpserver/requirements/tr/{Uri.EscapeDataString(id)}", cancellationToken);
    }

    /// <summary>Lists testing requirements, optionally filtered by area or status.</summary>
    public async Task<IReadOnlyList<TestEntry>> ListTestAsync(string? area = null, string? status = null, CancellationToken cancellationToken = default)
    {
        var url = BuildQueryUrl("mcpserver/requirements/test", ("area", area), ("status", status));
        return await GetAsync<IReadOnlyList<TestEntry>>(url, cancellationToken);
    }

    /// <summary>Lists all testing requirements (unfiltered).</summary>
    public async Task<IReadOnlyList<TestEntry>> ListTestAsync(CancellationToken cancellationToken = default)
        => await ListTestAsync(null, null, cancellationToken);

    /// <summary>Gets a testing requirement by ID.</summary>
    public async Task<TestEntry> GetTestAsync(string id, CancellationToken cancellationToken = default)
    {
        return await GetAsync<TestEntry>($"mcpserver/requirements/test/{Uri.EscapeDataString(id)}", cancellationToken);
    }

    /// <summary>Creates a new testing requirement.</summary>
    public async Task<TestEntry> CreateTestAsync(CreateTestRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<TestEntry>("mcpserver/requirements/test", request, cancellationToken);
    }

    /// <summary>Updates an existing testing requirement.</summary>
    public async Task<TestEntry> UpdateTestAsync(string id, UpdateTestRequest request, CancellationToken cancellationToken = default)
    {
        return await PutAsync<TestEntry>($"mcpserver/requirements/test/{Uri.EscapeDataString(id)}", request, cancellationToken);
    }

    /// <summary>Creates multiple testing requirements atomically.</summary>
    public async Task<RequirementsBatchResult> CreateTestBatchAsync(CreateTestBatchRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<RequirementsBatchResult>("mcpserver/requirements/test/batch", request, cancellationToken);
    }

    /// <summary>Updates multiple testing requirements atomically.</summary>
    public async Task<RequirementsBatchResult> UpdateTestBatchAsync(UpdateTestBatchRequest request, CancellationToken cancellationToken = default)
    {
        return await PutAsync<RequirementsBatchResult>("mcpserver/requirements/test/batch", request, cancellationToken);
    }

    /// <summary>Creates mixed functional, technical, and testing requirements atomically.</summary>
    public async Task<RequirementsBatchResult> CreateBatchAsync(CreateRequirementsBatchRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<RequirementsBatchResult>("mcpserver/requirements/batch", request, cancellationToken);
    }

    /// <summary>Updates mixed functional, technical, and testing requirements atomically.</summary>
    public async Task<RequirementsBatchResult> UpdateBatchAsync(UpdateRequirementsBatchRequest request, CancellationToken cancellationToken = default)
    {
        return await PutAsync<RequirementsBatchResult>("mcpserver/requirements/batch", request, cancellationToken);
    }

    /// <summary>Deletes a testing requirement by ID.</summary>
    public async Task<RequirementsMutationResult> DeleteTestAsync(string id, CancellationToken cancellationToken = default)
    {
        return await DeleteAsync<RequirementsMutationResult>($"mcpserver/requirements/test/{Uri.EscapeDataString(id)}", cancellationToken);
    }

    /// <summary>Copies a TODO item's acceptance criteria onto a functional requirement.</summary>
    public async Task<FrEntry> CopyFrAcceptanceCriteriaFromTodoAsync(
        string id,
        CopyAcceptanceCriteriaFromTodoRequest request,
        CancellationToken cancellationToken = default)
    {
        return await CopyAcceptanceCriteriaFromTodoAsync<FrEntry>("fr", id, request, cancellationToken);
    }

    /// <summary>Copies a TODO item's acceptance criteria onto a technical requirement.</summary>
    public async Task<TrEntry> CopyTrAcceptanceCriteriaFromTodoAsync(
        string id,
        CopyAcceptanceCriteriaFromTodoRequest request,
        CancellationToken cancellationToken = default)
    {
        return await CopyAcceptanceCriteriaFromTodoAsync<TrEntry>("tr", id, request, cancellationToken);
    }

    /// <summary>Copies a TODO item's acceptance criteria onto a testing requirement.</summary>
    public async Task<TestEntry> CopyTestAcceptanceCriteriaFromTodoAsync(
        string id,
        CopyAcceptanceCriteriaFromTodoRequest request,
        CancellationToken cancellationToken = default)
    {
        return await CopyAcceptanceCriteriaFromTodoAsync<TestEntry>("test", id, request, cancellationToken);
    }

    /// <summary>Lists all FR-to-TR mapping rows.</summary>
    public async Task<IReadOnlyList<FrTrMapping>> ListMappingsAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<IReadOnlyList<FrTrMapping>>("mcpserver/requirements/mapping", cancellationToken);
    }

    /// <summary>Gets an FR-to-TR mapping row by FR ID.</summary>
    public async Task<FrTrMapping> GetMappingAsync(string frId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<FrTrMapping>($"mcpserver/requirements/mapping/{Uri.EscapeDataString(frId)}", cancellationToken);
    }

    /// <summary>Creates or updates an FR-to-TR mapping row.</summary>
    public async Task<FrTrMapping> UpsertMappingAsync(string frId, UpsertFrTrMappingRequest request, CancellationToken cancellationToken = default)
    {
        return await PutAsync<FrTrMapping>($"mcpserver/requirements/mapping/{Uri.EscapeDataString(frId)}", request, cancellationToken);
    }

    /// <summary>Deletes an FR-to-TR mapping row by FR ID.</summary>
    public async Task<RequirementsMutationResult> DeleteMappingAsync(string frId, CancellationToken cancellationToken = default)
    {
        return await DeleteAsync<RequirementsMutationResult>($"mcpserver/requirements/mapping/{Uri.EscapeDataString(frId)}", cancellationToken);
    }

    /// <summary>
    /// Generates requirements output as inline content or workspace export metadata.
    /// </summary>
    /// <param name="doc">Document selector: <c>functional</c>, <c>technical</c>, <c>testing</c>, <c>mapping</c>, <c>matrix</c>, or <c>all</c>.</param>
    /// <param name="format">Document format: <c>markdown</c>, <c>yaml</c>, or <c>wiki</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Generated content, media type, and optional workspace export metadata.</returns>
    public async Task<RequirementsGeneratedDocument> GenerateAsync(string doc = "all", string format = "markdown", CancellationToken cancellationToken = default)
    {
        var path = $"mcpserver/requirements/generate?doc={Uri.EscapeDataString(doc)}&format={Uri.EscapeDataString(format)}";
        var (content, contentType) = await GetBytesAsync(path, cancellationToken);
        if (string.Equals(contentType, "application/json", StringComparison.OrdinalIgnoreCase))
        {
            var export = JsonSerializer.Deserialize<RequirementsDocumentExportResult>(content, s_jsonOptions);
            return new RequirementsGeneratedDocument
            {
                Content = content,
                ContentType = contentType,
                ExportResult = export
            };
        }

        return new RequirementsGeneratedDocument
        {
            Content = content,
            ContentType = contentType
        };
    }

    /// <summary>
    /// Bulk-ingests requirements markdown and upserts FR/TR/TEST/mapping entities.
    /// </summary>
    /// <param name="request">
    /// Optional markdown payload. If null or empty fields are provided, server defaults may be used.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Parsed, added, and updated counts per requirements document type.</returns>
    public async Task<RequirementsIngestResult> IngestAsync(RequirementsIngestRequest? request = null, CancellationToken cancellationToken = default)
    {
        return await PostAsync<RequirementsIngestResult>("mcpserver/requirements/ingest", request, cancellationToken);
    }

    private async Task<TRequirement> CopyAcceptanceCriteriaFromTodoAsync<TRequirement>(
        string kind,
        string id,
        CopyAcceptanceCriteriaFromTodoRequest request,
        CancellationToken cancellationToken)
    {
        return await PostAsync<TRequirement>(
            $"mcpserver/requirements/{Uri.EscapeDataString(kind)}/{Uri.EscapeDataString(id)}/acceptance-criteria/copy-from-todo",
            request,
            cancellationToken);
    }

    private static string BuildQueryUrl(string path, params (string Name, string? Value)[] query)
    {
        var qs = new List<string>();
        foreach (var (name, value) in query)
        {
            if (!string.IsNullOrWhiteSpace(value))
                qs.Add($"{name}={Uri.EscapeDataString(value)}");
        }

        return qs.Count == 0 ? path : path + "?" + string.Join("&", qs);
    }
}
