using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Client.Models;

namespace McpServer.Client;

/// <summary>
/// Client for memory management endpoints (<c>/mcpserver/memory</c>).
/// </summary>
/// <seealso cref="McpServerClient.Memory"/>
public sealed class MemoryClient : McpClientBase
{
    /// <inheritdoc />
    public MemoryClient(HttpClient http, McpServerClientOptions options)
        : base(http, options) { }

    internal MemoryClient(HttpClient http, McpServerClientOptions options, WorkspacePathHolder holder)
        : base(http, options, holder) { }

    /// <summary>Lists effective memories visible to the active workspace.</summary>
    public async Task<MemoryQueryResult> ListAsync(
        MemoryScope? scope = null,
        string? category = null,
        string? keyword = null,
        CancellationToken cancellationToken = default)
    {
        var qs = BuildQueryString(scope, category, keyword);
        return await GetAsync<MemoryQueryResult>($"mcpserver/memory{qs}", cancellationToken).ConfigureAwait(true);
    }

    /// <summary>Gets one memory by id.</summary>
    public async Task<MemoryItem> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        return await GetAsync<MemoryItem>($"mcpserver/memory/{Encode(id)}", cancellationToken).ConfigureAwait(true);
    }

    /// <summary>Adds a new memory.</summary>
    public async Task<MemoryMutationResult> AddAsync(MemoryAddRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<MemoryMutationResult>("mcpserver/memory", request, cancellationToken).ConfigureAwait(true);
    }

    /// <summary>Updates one memory by id.</summary>
    public async Task<MemoryMutationResult> UpdateAsync(string id, MemoryUpdateRequest request, CancellationToken cancellationToken = default)
    {
        return await PutAsync<MemoryMutationResult>($"mcpserver/memory/{Encode(id)}", request, cancellationToken).ConfigureAwait(true);
    }

    /// <summary>Removes one memory by id.</summary>
    public async Task<MemoryMutationResult> RemoveAsync(string id, CancellationToken cancellationToken = default)
    {
        return await DeleteAsync<MemoryMutationResult>($"mcpserver/memory/{Encode(id)}", cancellationToken).ConfigureAwait(true);
    }

    private static string BuildQueryString(MemoryScope? scope, string? category, string? keyword)
    {
        var parts = new List<string>();
        if (scope is not null) parts.Add($"scope={Encode(scope.Value.ToString())}");
        if (category is not null) parts.Add($"category={Encode(category)}");
        if (keyword is not null) parts.Add($"keyword={Encode(keyword)}");
        return parts.Count > 0 ? "?" + string.Join("&", parts) : string.Empty;
    }

    private static string Encode(string value) => System.Uri.EscapeDataString(value);
}
