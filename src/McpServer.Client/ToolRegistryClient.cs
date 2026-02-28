using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Client.Models;

namespace McpServer.Client;

/// <summary>
/// Client for tool registry endpoints (<c>/mcpserver/tools</c>). Manages tool definitions (CRUD),
/// keyword search, bucket management (add/remove/browse/sync), and tool installation from
/// buckets.
/// </summary>
/// <seealso cref="McpServerClient.Tools"/>
public sealed class ToolRegistryClient : McpClientBase
{
    /// <inheritdoc />
    public ToolRegistryClient(HttpClient http, McpServerClientOptions options)
        : base(http, options) { }

    internal ToolRegistryClient(HttpClient http, McpServerClientOptions options, WorkspacePathHolder holder)
        : base(http, options, holder) { }

    /// <summary>List all tools, optionally filtered by workspace.</summary>
    public async Task<ToolSearchResult> ListAsync(string? workspace = null, CancellationToken cancellationToken = default)
    {
        var qs = workspace is not null ? $"?workspace={Uri.EscapeDataString(workspace)}" : string.Empty;
        return await GetAsync<ToolSearchResult>($"mcpserver/tools{qs}", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Search tools by keyword.</summary>
    public async Task<ToolSearchResult> SearchAsync(string keyword, string? workspace = null, CancellationToken cancellationToken = default)
    {
        var parts = new List<string> { $"keyword={Uri.EscapeDataString(keyword)}" };
        if (workspace is not null) parts.Add($"workspace={Uri.EscapeDataString(workspace)}");
        return await GetAsync<ToolSearchResult>($"mcpserver/tools/search?{string.Join("&", parts)}", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Get a tool by ID.</summary>
    public async Task<ToolDto> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        return await GetAsync<ToolDto>($"mcpserver/tools/{id}", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Create a new tool definition.</summary>
    public async Task<ToolMutationResult> CreateAsync(ToolCreateRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<ToolMutationResult>("mcpserver/tools", request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Update an existing tool.</summary>
    public async Task<ToolMutationResult> UpdateAsync(int id, ToolUpdateRequest request, CancellationToken cancellationToken = default)
    {
        return await PutAsync<ToolMutationResult>($"mcpserver/tools/{id}", request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Delete a tool.</summary>
    public async Task<ToolMutationResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return await DeleteAsync<ToolMutationResult>($"mcpserver/tools/{id}", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>List tool buckets.</summary>
    public async Task<BucketListResult> ListBucketsAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<BucketListResult>("mcpserver/tools/buckets", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Add a tool bucket.</summary>
    public async Task<BucketMutationResult> AddBucketAsync(BucketAddRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<BucketMutationResult>("mcpserver/tools/buckets", request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Delete a tool bucket.</summary>
    public async Task<BucketMutationResult> DeleteBucketAsync(string name, bool uninstallTools = false, CancellationToken cancellationToken = default)
    {
        var qs = uninstallTools ? "?uninstallTools=true" : string.Empty;
        return await DeleteAsync<BucketMutationResult>($"mcpserver/tools/buckets/{Uri.EscapeDataString(name)}{qs}", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Browse available tools in a bucket.</summary>
    public async Task<BucketBrowseResult> BrowseBucketAsync(string name, CancellationToken cancellationToken = default)
    {
        return await GetAsync<BucketBrowseResult>($"mcpserver/tools/buckets/{Uri.EscapeDataString(name)}/browse", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Install a tool from a bucket.</summary>
    public async Task<ToolMutationResult> InstallFromBucketAsync(string bucketName, string toolName, string? workspace = null, CancellationToken cancellationToken = default)
    {
        var parts = new List<string> { $"toolName={Uri.EscapeDataString(toolName)}" };
        if (workspace is not null) parts.Add($"workspace={Uri.EscapeDataString(workspace)}");
        return await PostAsync<ToolMutationResult>($"mcpserver/tools/buckets/{Uri.EscapeDataString(bucketName)}/install?{string.Join("&", parts)}", null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Sync a bucket with its GitHub repository.</summary>
    public async Task<BucketSyncResult> SyncBucketAsync(string name, CancellationToken cancellationToken = default)
    {
        return await PostAsync<BucketSyncResult>($"mcpserver/tools/buckets/{Uri.EscapeDataString(name)}/sync", null, cancellationToken).ConfigureAwait(false);
    }
}
