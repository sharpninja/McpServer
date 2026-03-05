using McpServer.UI.Core.Messages;

namespace McpServer.UI.Core.Services;

/// <summary>
/// Abstraction over tool-registry endpoints used by UI.Core CQRS handlers.
/// </summary>
public interface IToolRegistryApiClient
{
    /// <summary>Lists tool definitions.</summary>
    Task<ListToolsResult> ListToolsAsync(ListToolsQuery query, CancellationToken cancellationToken = default);

    /// <summary>Searches tool definitions by keyword.</summary>
    Task<ListToolsResult> SearchToolsAsync(SearchToolsQuery query, CancellationToken cancellationToken = default);

    /// <summary>Gets a tool definition by ID.</summary>
    Task<ToolDetail?> GetToolAsync(int toolId, CancellationToken cancellationToken = default);

    /// <summary>Creates a tool definition.</summary>
    Task<ToolMutationOutcome> CreateToolAsync(CreateToolCommand command, CancellationToken cancellationToken = default);

    /// <summary>Updates a tool definition.</summary>
    Task<ToolMutationOutcome> UpdateToolAsync(UpdateToolCommand command, CancellationToken cancellationToken = default);

    /// <summary>Deletes a tool definition.</summary>
    Task<ToolMutationOutcome> DeleteToolAsync(DeleteToolCommand command, CancellationToken cancellationToken = default);

    /// <summary>Lists registered buckets.</summary>
    Task<ListBucketsResult> ListBucketsAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds a bucket.</summary>
    Task<BucketMutationOutcome> AddBucketAsync(AddBucketCommand command, CancellationToken cancellationToken = default);

    /// <summary>Removes a bucket.</summary>
    Task<BucketMutationOutcome> RemoveBucketAsync(RemoveBucketCommand command, CancellationToken cancellationToken = default);

    /// <summary>Browses tools from a bucket.</summary>
    Task<BucketBrowseOutcome> BrowseBucketAsync(BrowseBucketQuery query, CancellationToken cancellationToken = default);

    /// <summary>Installs a tool from a bucket.</summary>
    Task<ToolMutationOutcome> InstallFromBucketAsync(InstallFromBucketCommand command, CancellationToken cancellationToken = default);

    /// <summary>Synchronizes a bucket.</summary>
    Task<BucketSyncOutcome> SyncBucketAsync(SyncBucketCommand command, CancellationToken cancellationToken = default);
}
