using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using QBrainAi.Client.Models;

namespace QBrainAi.Client;

/// <summary>
/// Client for requirements endpoints (<c>/qbrainai/requirements</c>), including CRUD
/// operations for FR/TR/TEST entries, FR-to-TR mapping management, and document generation.
/// </summary>
/// <seealso cref="QBrainAiClient.Requirements"/>
public sealed class RequirementsClient : McpClientBase
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { PropertyNameCaseInsensitive = true, TypeInfoResolver = McpClientJsonContext.Default };

    /// <inheritdoc />
    public RequirementsClient(HttpClient http, QBrainAiClientOptions options)
        : base(http, options) { }

    internal RequirementsClient(HttpClient http, QBrainAiClientOptions options, WorkspacePathHolder holder)
        : base(http, options, holder) { }

    /// <summary>Lists requirement scope layers for the current workspace.</summary>
    public async Task<IReadOnlyList<RequirementScopeLayer>> ListRequirementLayersAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<IReadOnlyList<RequirementScopeLayer>>("qbrainai/requirements/layers", cancellationToken);
    }

    /// <summary>Creates a requirement scope layer for the current workspace.</summary>
    public async Task<RequirementScopeLayer> CreateRequirementLayerAsync(RequirementScopeLayerRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<RequirementScopeLayer>("qbrainai/requirements/layers", request, cancellationToken);
    }

    /// <summary>Updates mutable fields of a requirement scope layer.</summary>
    public async Task<RequirementScopeLayer> UpdateRequirementLayerAsync(string key, RequirementScopeLayerUpdate request, CancellationToken cancellationToken = default)
    {
        return await PutAsync<RequirementScopeLayer>($"qbrainai/requirements/layers/{Uri.EscapeDataString(key)}", request, cancellationToken);
    }

    /// <summary>Gets requirements effective at the workspace current layer or an explicit preview layer.</summary>
    public async Task<EffectiveRequirementsResult> GetEffectiveRequirementsAsync(
        string? layerKey = null,
        CancellationToken cancellationToken = default)
        => await GetEffectiveRequirementsAsync(layerKey, productScope: "product", cancellationToken).ConfigureAwait(true);

    /// <summary>Gets effective requirements with an explicit product scope.</summary>
    /// <param name="layerKey">Optional layer preview.</param>
    /// <param name="productScope"><c>product</c> (default) or <c>local</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<EffectiveRequirementsResult> GetEffectiveRequirementsAsync(
        string? layerKey,
        string? productScope,
        CancellationToken cancellationToken = default)
    {
        var url = BuildQueryUrl(
            "qbrainai/requirements/effective",
            ("layerKey", layerKey),
            ("productScope", productScope));
        return await GetAsync<EffectiveRequirementsResult>(url, cancellationToken);
    }

    /// <summary>Lists functional requirements, optionally filtered by area or status.</summary>
    public async Task<IReadOnlyList<FrEntry>> ListFrAsync(string? area = null, string? status = null, CancellationToken cancellationToken = default)
    {
        var url = BuildQueryUrl("qbrainai/requirements/fr", ("area", area), ("status", status));
        return await GetAsync<IReadOnlyList<FrEntry>>(url, cancellationToken);
    }

    /// <summary>Lists all functional requirements (unfiltered).</summary>
    public async Task<IReadOnlyList<FrEntry>> ListFrAsync(CancellationToken cancellationToken = default)
        => await ListFrAsync(null, null, cancellationToken);

    /// <summary>Repairs the FR catalog by purging invalid backfilled placeholders. Returns purged count.</summary>
    public async Task<int> RepairFrPlaceholdersAsync(CancellationToken cancellationToken = default)
    {
        var result = await PostAsync<object>("qbrainai/requirements/fr/repair", null, cancellationToken).ConfigureAwait(false);
        if (result is System.Text.Json.JsonElement je && je.TryGetProperty("purged", out var p) && p.TryGetInt32(out var n))
            return n;
        if (result is System.Collections.IDictionary dict && dict["purged"] is int i)
            return i;
        return 0;
    }

    /// <summary>Gets a functional requirement by ID.</summary>
    public async Task<FrEntry> GetFrAsync(string id, CancellationToken cancellationToken = default)
    {
        return await GetAsync<FrEntry>($"qbrainai/requirements/fr/{Uri.EscapeDataString(id)}", cancellationToken);
    }

    /// <summary>Creates a new functional requirement.</summary>
    public async Task<FrEntry> CreateFrAsync(CreateFrRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<FrEntry>("qbrainai/requirements/fr", request, cancellationToken);
    }

    /// <summary>Updates an existing functional requirement.</summary>
    public async Task<FrEntry> UpdateFrAsync(string id, UpdateFrRequest request, CancellationToken cancellationToken = default)
    {
        return await PutAsync<FrEntry>($"qbrainai/requirements/fr/{Uri.EscapeDataString(id)}", request, cancellationToken);
    }

    /// <summary>Creates multiple functional requirements atomically.</summary>
    public async Task<RequirementsBatchResult> CreateFrBatchAsync(CreateFrBatchRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<RequirementsBatchResult>("qbrainai/requirements/fr/batch", request, cancellationToken);
    }

    /// <summary>Updates multiple functional requirements atomically.</summary>
    public async Task<RequirementsBatchResult> UpdateFrBatchAsync(UpdateFrBatchRequest request, CancellationToken cancellationToken = default)
    {
        return await PutAsync<RequirementsBatchResult>("qbrainai/requirements/fr/batch", request, cancellationToken);
    }

    /// <summary>Deletes a functional requirement by ID.</summary>
    public async Task<RequirementsMutationResult> DeleteFrAsync(string id, CancellationToken cancellationToken = default)
    {
        return await DeleteAsync<RequirementsMutationResult>($"qbrainai/requirements/fr/{Uri.EscapeDataString(id)}", cancellationToken);
    }

    /// <summary>Lists technical requirements, optionally filtered by area, subarea, or status.</summary>
    public async Task<IReadOnlyList<TrEntry>> ListTrAsync(string? area = null, string? subarea = null, string? status = null, CancellationToken cancellationToken = default)
    {
        var url = BuildQueryUrl("qbrainai/requirements/tr", ("area", area), ("subarea", subarea), ("status", status));
        return await GetAsync<IReadOnlyList<TrEntry>>(url, cancellationToken);
    }

    /// <summary>Lists all technical requirements (unfiltered).</summary>
    public async Task<IReadOnlyList<TrEntry>> ListTrAsync(CancellationToken cancellationToken = default)
        => await ListTrAsync(null, null, null, cancellationToken);

    /// <summary>Gets a technical requirement by ID.</summary>
    public async Task<TrEntry> GetTrAsync(string id, CancellationToken cancellationToken = default)
    {
        return await GetAsync<TrEntry>($"qbrainai/requirements/tr/{Uri.EscapeDataString(id)}", cancellationToken);
    }

    /// <summary>Creates a new technical requirement.</summary>
    public async Task<TrEntry> CreateTrAsync(CreateTrRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<TrEntry>("qbrainai/requirements/tr", request, cancellationToken);
    }

    /// <summary>Updates an existing technical requirement.</summary>
    public async Task<TrEntry> UpdateTrAsync(string id, UpdateTrRequest request, CancellationToken cancellationToken = default)
    {
        return await PutAsync<TrEntry>($"qbrainai/requirements/tr/{Uri.EscapeDataString(id)}", request, cancellationToken);
    }

    /// <summary>Creates multiple technical requirements atomically.</summary>
    public async Task<RequirementsBatchResult> CreateTrBatchAsync(CreateTrBatchRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<RequirementsBatchResult>("qbrainai/requirements/tr/batch", request, cancellationToken);
    }

    /// <summary>Updates multiple technical requirements atomically.</summary>
    public async Task<RequirementsBatchResult> UpdateTrBatchAsync(UpdateTrBatchRequest request, CancellationToken cancellationToken = default)
    {
        return await PutAsync<RequirementsBatchResult>("qbrainai/requirements/tr/batch", request, cancellationToken);
    }

    /// <summary>Deletes a technical requirement by ID.</summary>
    public async Task<RequirementsMutationResult> DeleteTrAsync(string id, CancellationToken cancellationToken = default)
    {
        return await DeleteAsync<RequirementsMutationResult>($"qbrainai/requirements/tr/{Uri.EscapeDataString(id)}", cancellationToken);
    }

    /// <summary>Lists testing requirements, optionally filtered by area or status.</summary>
    public async Task<IReadOnlyList<TestEntry>> ListTestAsync(string? area = null, string? status = null, CancellationToken cancellationToken = default)
    {
        var url = BuildQueryUrl("qbrainai/requirements/test", ("area", area), ("status", status));
        return await GetAsync<IReadOnlyList<TestEntry>>(url, cancellationToken);
    }

    /// <summary>Lists all testing requirements (unfiltered).</summary>
    public async Task<IReadOnlyList<TestEntry>> ListTestAsync(CancellationToken cancellationToken = default)
        => await ListTestAsync(null, null, cancellationToken);

    /// <summary>Gets a testing requirement by ID.</summary>
    public async Task<TestEntry> GetTestAsync(string id, CancellationToken cancellationToken = default)
    {
        return await GetAsync<TestEntry>($"qbrainai/requirements/test/{Uri.EscapeDataString(id)}", cancellationToken);
    }

    /// <summary>Creates a new testing requirement.</summary>
    public async Task<TestEntry> CreateTestAsync(CreateTestRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<TestEntry>("qbrainai/requirements/test", request, cancellationToken);
    }

    /// <summary>Updates an existing testing requirement.</summary>
    public async Task<TestEntry> UpdateTestAsync(string id, UpdateTestRequest request, CancellationToken cancellationToken = default)
    {
        return await PutAsync<TestEntry>($"qbrainai/requirements/test/{Uri.EscapeDataString(id)}", request, cancellationToken);
    }

    /// <summary>Creates multiple testing requirements atomically.</summary>
    public async Task<RequirementsBatchResult> CreateTestBatchAsync(CreateTestBatchRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<RequirementsBatchResult>("qbrainai/requirements/test/batch", request, cancellationToken);
    }

    /// <summary>Updates multiple testing requirements atomically.</summary>
    public async Task<RequirementsBatchResult> UpdateTestBatchAsync(UpdateTestBatchRequest request, CancellationToken cancellationToken = default)
    {
        return await PutAsync<RequirementsBatchResult>("qbrainai/requirements/test/batch", request, cancellationToken);
    }

    /// <summary>Creates mixed functional, technical, and testing requirements atomically.</summary>
    public async Task<RequirementsBatchResult> CreateBatchAsync(CreateRequirementsBatchRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<RequirementsBatchResult>("qbrainai/requirements/batch", request, cancellationToken);
    }

    /// <summary>Updates mixed functional, technical, and testing requirements atomically.</summary>
    public async Task<RequirementsBatchResult> UpdateBatchAsync(UpdateRequirementsBatchRequest request, CancellationToken cancellationToken = default)
    {
        return await PutAsync<RequirementsBatchResult>("qbrainai/requirements/batch", request, cancellationToken);
    }

    /// <summary>Deletes a testing requirement by ID.</summary>
    public async Task<RequirementsMutationResult> DeleteTestAsync(string id, CancellationToken cancellationToken = default)
    {
        return await DeleteAsync<RequirementsMutationResult>($"qbrainai/requirements/test/{Uri.EscapeDataString(id)}", cancellationToken);
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
        return await GetAsync<IReadOnlyList<FrTrMapping>>("qbrainai/requirements/mapping", cancellationToken);
    }

    /// <summary>Gets an FR-to-TR mapping row by FR ID.</summary>
    public async Task<FrTrMapping> GetMappingAsync(string frId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<FrTrMapping>($"qbrainai/requirements/mapping/{Uri.EscapeDataString(frId)}", cancellationToken);
    }

    /// <summary>Creates or updates an FR-to-TR mapping row.</summary>
    public async Task<FrTrMapping> UpsertMappingAsync(string frId, UpsertFrTrMappingRequest request, CancellationToken cancellationToken = default)
    {
        return await PutAsync<FrTrMapping>($"qbrainai/requirements/mapping/{Uri.EscapeDataString(frId)}", request, cancellationToken);
    }

    /// <summary>Deletes an FR-to-TR mapping row by FR ID.</summary>
    public async Task<RequirementsMutationResult> DeleteMappingAsync(string frId, CancellationToken cancellationToken = default)
    {
        return await DeleteAsync<RequirementsMutationResult>($"qbrainai/requirements/mapping/{Uri.EscapeDataString(frId)}", cancellationToken);
    }

    /// <summary>
    /// Generates requirements output as inline content or workspace export metadata.
    /// </summary>
    /// <param name="doc">Document selector: <c>functional</c>, <c>technical</c>, <c>testing</c>, <c>mapping</c>, <c>matrix</c>, or <c>all</c>.</param>
    /// <param name="format">Document format: <c>markdown</c>, <c>yaml</c>, or <c>wiki</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Generated content, media type, and optional workspace export metadata.</returns>
    public Task<RequirementsGeneratedDocument> GenerateAsync(string doc = "all", string format = "markdown", CancellationToken cancellationToken = default)
        => GenerateAsync(doc, format, workspacePath: null, cancellationToken);

    /// <summary>
    /// Generates requirements output for an explicit workspace, overriding the client-bound
    /// <c>X-Workspace-Path</c> header for this call only. Guards exports against silently
    /// targeting the session-bound workspace (triage-report-f77331f9a33e4bd0ae4f55f0470743ed).
    /// </summary>
    /// <param name="doc">Document selector: <c>functional</c>, <c>technical</c>, <c>testing</c>, <c>mapping</c>, <c>matrix</c>, or <c>all</c>.</param>
    /// <param name="format">Document format: <c>markdown</c>, <c>yaml</c>, or <c>wiki</c>.</param>
    /// <param name="workspacePath">Optional workspace path override; null or whitespace keeps the client-bound workspace.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Generated content, media type, and optional workspace export metadata.</returns>
    public async Task<RequirementsGeneratedDocument> GenerateAsync(string doc, string format, string? workspacePath, CancellationToken cancellationToken = default)
    {
        var path = $"qbrainai/requirements/generate?doc={Uri.EscapeDataString(doc)}&format={Uri.EscapeDataString(format)}";
        var (content, contentType) = await GetBytesAsync(path, cancellationToken, workspacePath);
        if (string.Equals(contentType, "application/json", StringComparison.OrdinalIgnoreCase))
        {
            var export = (RequirementsDocumentExportResult?)JsonSerializer.Deserialize(content, s_jsonOptions.GetTypeInfo(typeof(RequirementsDocumentExportResult)));
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
        return await PostAsync<RequirementsIngestResult>("qbrainai/requirements/ingest", request, cancellationToken);
    }

    /// <summary>FR-MCP-REQRECOVERY-001: plans a recovery payload. The server writes nothing.</summary>
    /// <param name="request">Idempotency key and items.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The dry-run plan.</returns>
    public Task<RequirementsRecoveryResult> PlanRecoveryAsync(RequirementsRecoveryRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Mode = "dry-run";
        return PostAsync<RequirementsRecoveryResult>("qbrainai/requirements/recovery", request, cancellationToken);
    }

    /// <summary>FR-MCP-REQRECOVERY-001: applies a recovery payload atomically.</summary>
    /// <param name="request">Idempotency key and items.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The applied or replayed result.</returns>
    public Task<RequirementsRecoveryResult> ApplyRecoveryAsync(RequirementsRecoveryRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Mode = "apply";
        return PostAsync<RequirementsRecoveryResult>("qbrainai/requirements/recovery", request, cancellationToken);
    }

    /// <summary>FR-MCP-REQRECOVERY-001: gets a stored recovery run by idempotency key.</summary>
    /// <param name="idempotencyKey">The key stored with the apply.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The stored result.</returns>
    public Task<RequirementsRecoveryResult> GetRecoveryAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        return GetAsync<RequirementsRecoveryResult>(
            $"qbrainai/requirements/recovery/{Uri.EscapeDataString(idempotencyKey)}",
            cancellationToken);
    }

    private async Task<TRequirement> CopyAcceptanceCriteriaFromTodoAsync<TRequirement>(
        string kind,
        string id,
        CopyAcceptanceCriteriaFromTodoRequest request,
        CancellationToken cancellationToken)
    {
        return await PostAsync<TRequirement>(
            $"qbrainai/requirements/{Uri.EscapeDataString(kind)}/{Uri.EscapeDataString(id)}/acceptance-criteria/copy-from-todo",
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
