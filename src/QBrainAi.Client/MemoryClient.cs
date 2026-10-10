using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using QBrainAi.Client.Models;

namespace QBrainAi.Client;

/// <summary>
/// Client for memory management endpoints (<c>/qbrainai/memory</c>).
/// </summary>
/// <seealso cref="QBrainAiClient.Memory"/>
public sealed class MemoryClient : McpClientBase
{
    /// <inheritdoc />
    public MemoryClient(HttpClient http, QBrainAiClientOptions options)
        : base(http, options) { }

    internal MemoryClient(HttpClient http, QBrainAiClientOptions options, WorkspacePathHolder holder)
        : base(http, options, holder) { }

    /// <summary>Lists effective memories visible to the active workspace.</summary>
    public async Task<MemoryQueryResult> ListAsync(
        MemoryScope? scope = null,
        string? category = null,
        string? keyword = null,
        CancellationToken cancellationToken = default)
    {
        var qs = BuildQueryString(scope, category, keyword);
        return await GetAsync<MemoryQueryResult>($"qbrainai/memory{qs}", cancellationToken).ConfigureAwait(true);
    }

    /// <summary>Gets one memory by id.</summary>
    public async Task<MemoryItem> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        return await GetAsync<MemoryItem>($"qbrainai/memory/{Encode(id)}", cancellationToken).ConfigureAwait(true);
    }

    /// <summary>Adds a new memory.</summary>
    public async Task<MemoryMutationResult> AddAsync(MemoryAddRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<MemoryMutationResult>("qbrainai/memory", request, cancellationToken).ConfigureAwait(true);
    }

    /// <summary>Updates one memory by id.</summary>
    public async Task<MemoryMutationResult> UpdateAsync(string id, MemoryUpdateRequest request, CancellationToken cancellationToken = default)
    {
        return await PutAsync<MemoryMutationResult>($"qbrainai/memory/{Encode(id)}", request, cancellationToken).ConfigureAwait(true);
    }

    /// <summary>Removes one memory by id.</summary>
    public async Task<MemoryMutationResult> RemoveAsync(string id, CancellationToken cancellationToken = default)
    {
        return await DeleteAsync<MemoryMutationResult>($"qbrainai/memory/{Encode(id)}", cancellationToken).ConfigureAwait(true);
    }

    /// <summary>Remembers a multi-layer memory.</summary>
    public Task<MemoryRememberResult> RememberAsync(MemoryRememberRequest request, CancellationToken cancellationToken = default)
        => PostAsync<MemoryRememberResult>("qbrainai/memory/remember", request, cancellationToken);

    /// <summary>Recalls memories by meaning or keyword.</summary>
    public async Task<MemoryRecallResult> RecallAsync(MemoryRecallRequest request, CancellationToken cancellationToken = default)
    {
        var result = await PostAsync<MemoryRecallResult>("qbrainai/memory/recall", request, cancellationToken).ConfigureAwait(false);
        result.SynchronizeHitsAndItems();
        return result;
    }

    /// <summary>Explores a memory neighborhood.</summary>
    public async Task<MemoryExploreResult> ExploreAsync(MemoryExploreRequest request, CancellationToken cancellationToken = default)
    {
        var result = await PostAsync<MemoryExploreResult>("qbrainai/memory/explore", request, cancellationToken).ConfigureAwait(false);
        result.SynchronizeHitsAndItems();
        return result;
    }

    /// <summary>Plans or applies consolidate/sleep merge.</summary>
    public async Task<MemoryConsolidateResult> ConsolidateAsync(MemoryConsolidateRequest request, CancellationToken cancellationToken = default)
    {
        var result = await PostAsync<MemoryConsolidateResult>("qbrainai/memory/consolidate", request, cancellationToken).ConfigureAwait(false);
        result.SynchronizeHitsAndItems();
        return result;
    }

    /// <summary>Promotes a session-log or context source into memory.</summary>
    public Task<MemoryPromoteResult> PromoteAsync(MemoryPromoteRequest request, CancellationToken cancellationToken = default)
        => PostAsync<MemoryPromoteResult>("qbrainai/memory/promote", request, cancellationToken);

    /// <summary>Lists versions for one memory.</summary>
    public async Task<MemoryVersionListResult> ListVersionsAsync(string id, CancellationToken cancellationToken = default)
    {
        var result = await GetAsync<MemoryVersionListResult>($"qbrainai/memory/{Encode(id)}/versions", cancellationToken).ConfigureAwait(false);
        result.SynchronizeHitsAndItems();
        return result;
    }

    /// <summary>Reverts a memory to snapshot N.</summary>
    public Task<MemoryRevertResult> RevertAsync(string id, int versionNumber, CancellationToken cancellationToken = default)
        => PostAsync<MemoryRevertResult>($"qbrainai/memory/{Encode(id)}/revert", new MemoryRevertRequest { VersionNumber = versionNumber }, cancellationToken);

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
