using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using QBrainAi.Client.Models;

namespace QBrainAi.Client;

/// <summary>
/// Client for tool registry endpoints (<c>/qbrainai/tools</c>). Manages tool definitions (CRUD),
/// keyword search, bucket management (add/remove/browse/sync), and tool installation from
/// buckets.
/// </summary>
/// <seealso cref="QBrainAiClient.Tools"/>
public sealed class ToolRegistryClient : McpClientBase
{
    /// <inheritdoc />
    public ToolRegistryClient(HttpClient http, QBrainAiClientOptions options)
        : base(http, options) { }

    internal ToolRegistryClient(HttpClient http, QBrainAiClientOptions options, WorkspacePathHolder holder)
        : base(http, options, holder) { }

    /// <summary>List all tools, optionally filtered by workspace.</summary>
    public async Task<ToolSearchResult> ListAsync(string? workspace = null, CancellationToken cancellationToken = default)
    {
        var qs = workspace is not null ? $"?workspace={Uri.EscapeDataString(workspace)}" : string.Empty;
        return await GetAsync<ToolSearchResult>($"qbrainai/tools{qs}", cancellationToken);
    }

    /// <summary>Search tools by keyword.</summary>
    public async Task<ToolSearchResult> SearchAsync(string keyword, string? workspace = null, CancellationToken cancellationToken = default)
    {
        var parts = new List<string> { $"keyword={Uri.EscapeDataString(keyword)}" };
        if (workspace is not null) parts.Add($"workspace={Uri.EscapeDataString(workspace)}");
        return await GetAsync<ToolSearchResult>($"qbrainai/tools/search?{string.Join("&", parts)}", cancellationToken);
    }

    /// <summary>Get a tool by ID.</summary>
    public async Task<ToolDto> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        return await GetAsync<ToolDto>($"qbrainai/tools/{id}", cancellationToken);
    }

    /// <summary>Create a new tool definition.</summary>
    public async Task<ToolMutationResult> CreateAsync(ToolCreateRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<ToolMutationResult>("qbrainai/tools", request, cancellationToken);
    }

    /// <summary>Update an existing tool.</summary>
    public async Task<ToolMutationResult> UpdateAsync(int id, ToolUpdateRequest request, CancellationToken cancellationToken = default)
    {
        return await PutAsync<ToolMutationResult>($"qbrainai/tools/{id}", request, cancellationToken);
    }

    /// <summary>Delete a tool.</summary>
    public async Task<ToolMutationResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return await DeleteAsync<ToolMutationResult>($"qbrainai/tools/{id}", cancellationToken);
    }

    /// <summary>List tool buckets.</summary>
    public async Task<BucketListResult> ListBucketsAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<BucketListResult>("qbrainai/tools/buckets", cancellationToken);
    }

    /// <summary>Add a tool bucket.</summary>
    public async Task<BucketMutationResult> AddBucketAsync(BucketAddRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<BucketMutationResult>("qbrainai/tools/buckets", request, cancellationToken);
    }

    /// <summary>Delete a tool bucket.</summary>
    public async Task<BucketMutationResult> DeleteBucketAsync(string name, bool uninstallTools = false, CancellationToken cancellationToken = default)
    {
        var qs = uninstallTools ? "?uninstallTools=true" : string.Empty;
        return await DeleteAsync<BucketMutationResult>($"qbrainai/tools/buckets/{Uri.EscapeDataString(name)}{qs}", cancellationToken);
    }

    /// <summary>Browse available tools in a bucket.</summary>
    public async Task<BucketBrowseResult> BrowseBucketAsync(string name, CancellationToken cancellationToken = default)
    {
        return await GetAsync<BucketBrowseResult>($"qbrainai/tools/buckets/{Uri.EscapeDataString(name)}/browse", cancellationToken);
    }

    /// <summary>Install a tool from a bucket.</summary>
    public async Task<ToolMutationResult> InstallFromBucketAsync(string bucketName, string toolName, string? workspace = null, CancellationToken cancellationToken = default)
    {
        var parts = new List<string> { $"toolName={Uri.EscapeDataString(toolName)}" };
        if (workspace is not null) parts.Add($"workspace={Uri.EscapeDataString(workspace)}");
        return await PostAsync<ToolMutationResult>($"qbrainai/tools/buckets/{Uri.EscapeDataString(bucketName)}/install?{string.Join("&", parts)}", null, cancellationToken);
    }

    /// <summary>Sync a bucket with its GitHub repository.</summary>
    public async Task<BucketSyncResult> SyncBucketAsync(string name, CancellationToken cancellationToken = default)
    {
        return await PostAsync<BucketSyncResult>($"qbrainai/tools/buckets/{Uri.EscapeDataString(name)}/sync", null, cancellationToken);
    }
}
