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

    /// <summary>Lists all functional requirements.</summary>
    public async Task<IReadOnlyList<FrEntry>> ListFrAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<IReadOnlyList<FrEntry>>("mcpserver/requirements/fr", cancellationToken);
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

    /// <summary>Deletes a functional requirement by ID.</summary>
    public async Task<RequirementsMutationResult> DeleteFrAsync(string id, CancellationToken cancellationToken = default)
    {
        return await DeleteAsync<RequirementsMutationResult>($"mcpserver/requirements/fr/{Uri.EscapeDataString(id)}", cancellationToken);
    }

    /// <summary>Lists all technical requirements.</summary>
    public async Task<IReadOnlyList<TrEntry>> ListTrAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<IReadOnlyList<TrEntry>>("mcpserver/requirements/tr", cancellationToken);
    }

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

    /// <summary>Deletes a technical requirement by ID.</summary>
    public async Task<RequirementsMutationResult> DeleteTrAsync(string id, CancellationToken cancellationToken = default)
    {
        return await DeleteAsync<RequirementsMutationResult>($"mcpserver/requirements/tr/{Uri.EscapeDataString(id)}", cancellationToken);
    }

    /// <summary>Lists all testing requirements.</summary>
    public async Task<IReadOnlyList<TestEntry>> ListTestAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<IReadOnlyList<TestEntry>>("mcpserver/requirements/test", cancellationToken);
    }

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

    /// <summary>Deletes a testing requirement by ID.</summary>
    public async Task<RequirementsMutationResult> DeleteTestAsync(string id, CancellationToken cancellationToken = default)
    {
        return await DeleteAsync<RequirementsMutationResult>($"mcpserver/requirements/test/{Uri.EscapeDataString(id)}", cancellationToken);
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
}
