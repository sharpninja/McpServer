namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Manages tool buckets — GitHub repositories containing tool manifest files.
/// Similar to Scoop package manager: add a bucket, browse available tools, then
/// install individual tools into the server (global) or a specific workspace.
/// </summary>
public interface IToolBucketService
{
    /// <summary>List all registered buckets.</summary>
    Task<BucketListResult> ListBucketsAsync(CancellationToken ct = default);

    /// <summary>Add (register) a new bucket repository.</summary>
    Task<BucketMutationResult> AddBucketAsync(BucketAddRequest request, CancellationToken ct = default);

    /// <summary>Remove a bucket and optionally uninstall its tools.</summary>
    Task<BucketMutationResult> RemoveBucketAsync(string bucketName, bool uninstallTools = false, CancellationToken ct = default);

    /// <summary>Browse available tool manifests in a bucket (reads from GitHub).</summary>
    Task<BucketBrowseResult> BrowseAsync(string bucketName, CancellationToken ct = default);

    /// <summary>Install a tool from a bucket into the server (global) or a workspace.</summary>
    Task<ToolMutationResult> InstallAsync(string bucketName, string toolName, string? workspacePath = null, CancellationToken ct = default);

    /// <summary>Sync all installed tools from a bucket to pick up manifest updates.</summary>
    Task<BucketSyncResult> SyncAsync(string bucketName, CancellationToken ct = default);
}

/// <summary>Request to add a bucket repository.</summary>
/// <param name="Name">Short unique name (e.g. <c>official</c>, <c>community</c>).</param>
/// <param name="Owner">GitHub repository owner.</param>
/// <param name="Repo">GitHub repository name.</param>
/// <param name="Branch">Branch to read from (default: <c>main</c>).</param>
/// <param name="ManifestPath">Path within the repo for manifests (default: <c>/</c>).</param>
public sealed record BucketAddRequest(
    string Name,
    string Owner,
    string Repo,
    string? Branch = null,
    string? ManifestPath = null);

/// <summary>Read model for a bucket.</summary>
public sealed record BucketDto(
    int Id,
    string Name,
    string Owner,
    string Repo,
    string Branch,
    string ManifestPath,
    DateTimeOffset DateTimeCreated,
    DateTimeOffset? DateTimeLastSynced);

/// <summary>Result of listing buckets.</summary>
public sealed record BucketListResult(IReadOnlyList<BucketDto> Buckets, int TotalCount);

/// <summary>Result of add/remove bucket mutation.</summary>
public sealed record BucketMutationResult(bool Success, string? Error = null, BucketDto? Bucket = null);

/// <summary>A tool manifest as read from a bucket repository (not yet installed).</summary>
/// <param name="Name">Tool name from the manifest file.</param>
/// <param name="Description">Human-readable description.</param>
/// <param name="Tags">Keyword tags for discovery.</param>
/// <param name="ParameterSchema">Optional JSON schema for parameters.</param>
/// <param name="CommandTemplate">Optional command template.</param>
/// <param name="ManifestFile">File path within the repo.</param>
public sealed record ToolManifest(
    string Name,
    string Description,
    IReadOnlyList<string> Tags,
    string? ParameterSchema,
    string? CommandTemplate,
    string ManifestFile);

/// <summary>Result of browsing a bucket's available tools.</summary>
public sealed record BucketBrowseResult(bool Success, string? Error = null, IReadOnlyList<ToolManifest>? Tools = null);

/// <summary>Result of syncing installed tools from a bucket.</summary>
public sealed record BucketSyncResult(bool Success, string? Error = null, int Updated = 0, int Added = 0, int Unchanged = 0);
