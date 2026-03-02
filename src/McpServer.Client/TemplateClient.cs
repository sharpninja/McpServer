using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Client.Models;

namespace McpServer.Client;

/// <summary>
/// Client for prompt template management endpoints (<c>/mcpserver/templates</c>).
/// Provides CRUD operations and test/render capabilities.
/// </summary>
/// <seealso cref="McpServerClient.Template"/>
public sealed class TemplateClient : McpClientBase
{
    /// <inheritdoc />
    public TemplateClient(HttpClient http, McpServerClientOptions options)
        : base(http, options) { }

    internal TemplateClient(HttpClient http, McpServerClientOptions options, WorkspacePathHolder holder)
        : base(http, options, holder) { }

    /// <summary>Query templates with optional filters.</summary>
    public async Task<TemplateQueryResult> QueryAsync(
        string? category = null, string? tag = null, string? keyword = null,
        CancellationToken cancellationToken = default)
    {
        var qs = BuildQueryString(category, tag, keyword);
        return await GetAsync<TemplateQueryResult>($"mcpserver/templates{qs}", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Get a single template by ID.</summary>
    public async Task<TemplateItem> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        return await GetAsync<TemplateItem>($"mcpserver/templates/{Encode(id)}", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Create a new template.</summary>
    public async Task<TemplateMutationResult> CreateAsync(TemplateCreateRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<TemplateMutationResult>("mcpserver/templates", request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Update an existing template.</summary>
    public async Task<TemplateMutationResult> UpdateAsync(string id, TemplateUpdateRequest request, CancellationToken cancellationToken = default)
    {
        return await PutAsync<TemplateMutationResult>($"mcpserver/templates/{Encode(id)}", request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Delete a template.</summary>
    public async Task<TemplateMutationResult> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        return await DeleteAsync<TemplateMutationResult>($"mcpserver/templates/{Encode(id)}", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Test/render a stored template with sample data.</summary>
    public async Task<TemplateTestResult> TestAsync(string id, TemplateTestRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<TemplateTestResult>($"mcpserver/templates/{Encode(id)}/test", request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Test/render an inline template (without saving).</summary>
    public async Task<TemplateTestResult> TestInlineAsync(TemplateTestRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<TemplateTestResult>("mcpserver/templates/test", request, cancellationToken).ConfigureAwait(false);
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
