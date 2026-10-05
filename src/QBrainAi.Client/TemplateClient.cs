using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using QBrainAi.Client.Models;

namespace QBrainAi.Client;

/// <summary>
/// Client for prompt template management endpoints (<c>/qbrainai/templates</c>).
/// Provides CRUD operations and test/render capabilities.
/// </summary>
/// <seealso cref="QBrainAiClient.Template"/>
public sealed class TemplateClient : McpClientBase
{
    /// <inheritdoc />
    public TemplateClient(HttpClient http, QBrainAiClientOptions options)
        : base(http, options) { }

    internal TemplateClient(HttpClient http, QBrainAiClientOptions options, WorkspacePathHolder holder)
        : base(http, options, holder) { }

    /// <summary>Query templates with optional filters.</summary>
    public async Task<TemplateQueryResult> QueryAsync(
        string? category = null, string? tag = null, string? keyword = null,
        CancellationToken cancellationToken = default)
    {
        var qs = BuildQueryString(category, tag, keyword);
        return await GetAsync<TemplateQueryResult>($"qbrainai/templates{qs}", cancellationToken);
    }

    /// <summary>Get a single template by ID.</summary>
    public async Task<TemplateItem> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        return await GetAsync<TemplateItem>($"qbrainai/templates/{Encode(id)}", cancellationToken);
    }

    /// <summary>Create a new template.</summary>
    public async Task<TemplateMutationResult> CreateAsync(TemplateCreateRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<TemplateMutationResult>("qbrainai/templates", request, cancellationToken);
    }

    /// <summary>Update an existing template.</summary>
    public async Task<TemplateMutationResult> UpdateAsync(string id, TemplateUpdateRequest request, CancellationToken cancellationToken = default)
    {
        return await PutAsync<TemplateMutationResult>($"qbrainai/templates/{Encode(id)}", request, cancellationToken);
    }

    /// <summary>Delete a template.</summary>
    public async Task<TemplateMutationResult> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        return await DeleteAsync<TemplateMutationResult>($"qbrainai/templates/{Encode(id)}", cancellationToken);
    }

    /// <summary>Test/render a stored template with sample data.</summary>
    public async Task<TemplateTestResult> TestAsync(string id, TemplateTestRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<TemplateTestResult>($"qbrainai/templates/{Encode(id)}/test", request, cancellationToken);
    }

    /// <summary>Test/render an inline template (without saving).</summary>
    public async Task<TemplateTestResult> TestInlineAsync(TemplateTestRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<TemplateTestResult>("qbrainai/templates/test", request, cancellationToken);
    }

    /// <summary>Resolve a stored template by ID using a dictionary of values.</summary>
    public async Task<TemplateResolveResult> ResolveAsync(string id, TemplateResolveRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<TemplateResolveResult>($"qbrainai/templates/{Encode(id)}/resolve", request, cancellationToken);
    }

    private static string Encode(string value) => System.Uri.EscapeDataString(value);

    private static string BuildQueryString(string? category, string? tag, string? keyword)
    {
        var parts = new List<string>();
        if (category is not null) parts.Add($"category={Encode(category)}");
        if (tag is not null) parts.Add($"tag={Encode(tag)}");
        if (keyword is not null) parts.Add($"keyword={Encode(keyword)}");
        return parts.Count > 0 ? "?" + string.Join("&", parts) : string.Empty;
    }
}
