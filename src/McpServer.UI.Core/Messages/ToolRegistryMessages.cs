using McpServer.Cqrs;

namespace McpServer.UI.Core.Messages;

/// <summary>Query to list all tool definitions.</summary>
public sealed record ListToolsQuery(string? WorkspacePath = null) : IQuery<ListToolsResult>;

/// <summary>Query to search tools by keyword.</summary>
public sealed record SearchToolsQuery(string Keyword, string? WorkspacePath = null) : IQuery<ListToolsResult>;

/// <summary>Query to load a single tool definition by ID.</summary>
public sealed record GetToolQuery(int ToolId) : IQuery<ToolDetail?>;

/// <summary>Command to create a new tool definition.</summary>
public sealed record CreateToolCommand : ICommand<ToolMutationOutcome>
{
    /// <summary>Tool name.</summary>
    public required string Name { get; init; }

    /// <summary>Tool description.</summary>
    public required string Description { get; init; }

    /// <summary>Keyword tags.</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>JSON schema for tool parameters.</summary>
    public string? ParameterSchema { get; init; }

    /// <summary>Command template for execution.</summary>
    public string? CommandTemplate { get; init; }

    /// <summary>Optional workspace scope.</summary>
    public string? WorkspacePath { get; init; }
}

/// <summary>Command to update an existing tool definition.</summary>
public sealed record UpdateToolCommand : ICommand<ToolMutationOutcome>
{
    /// <summary>Tool ID to update.</summary>
    public required int ToolId { get; init; }

    /// <summary>Updated name.</summary>
    public string? Name { get; init; }

    /// <summary>Updated description.</summary>
    public string? Description { get; init; }

    /// <summary>Updated tags.</summary>
    public IReadOnlyList<string>? Tags { get; init; }

    /// <summary>Updated parameter schema.</summary>
    public string? ParameterSchema { get; init; }

    /// <summary>Updated command template.</summary>
    public string? CommandTemplate { get; init; }

    /// <summary>Updated workspace scope.</summary>
    public string? WorkspacePath { get; init; }
}

/// <summary>Command to delete a tool definition.</summary>
public sealed record DeleteToolCommand(int ToolId) : ICommand<ToolMutationOutcome>;

/// <summary>Query to list registered tool buckets.</summary>
public sealed record ListBucketsQuery : IQuery<ListBucketsResult>;

/// <summary>Command to add a tool bucket.</summary>
public sealed record AddBucketCommand : ICommand<BucketMutationOutcome>
{
    /// <summary>Bucket name.</summary>
    public required string Name { get; init; }

    /// <summary>Repository owner.</summary>
    public required string Owner { get; init; }

    /// <summary>Repository name.</summary>
    public required string Repo { get; init; }

    /// <summary>Optional branch override.</summary>
    public string? Branch { get; init; }

    /// <summary>Optional manifest path override.</summary>
    public string? ManifestPath { get; init; }
}

/// <summary>Command to remove a tool bucket.</summary>
public sealed record RemoveBucketCommand(string Name, bool UninstallTools = false) : ICommand<BucketMutationOutcome>;

/// <summary>Query to browse tools from a specific bucket.</summary>
public sealed record BrowseBucketQuery(string Name) : IQuery<BucketBrowseOutcome>;

/// <summary>Command to install a tool from a bucket.</summary>
public sealed record InstallFromBucketCommand(
    string BucketName,
    string ToolName,
    string? WorkspacePath = null) : ICommand<ToolMutationOutcome>;

/// <summary>Command to synchronize a bucket with its source repository.</summary>
public sealed record SyncBucketCommand(string Name) : ICommand<BucketSyncOutcome>;

/// <summary>List result for tool definitions.</summary>
public sealed record ListToolsResult(IReadOnlyList<ToolListItem> Items, int TotalCount);

/// <summary>List-friendly tool summary.</summary>
public sealed record ToolListItem(int Id, string Name, string Description, IReadOnlyList<string> Tags, string? WorkspacePath);

/// <summary>Detailed tool definition.</summary>
public sealed record ToolDetail(
    int Id,
    string Name,
    string Description,
    IReadOnlyList<string> Tags,
    string? ParameterSchema,
    string? CommandTemplate,
    string? WorkspacePath,
    DateTimeOffset DateTimeCreated,
    DateTimeOffset DateTimeModified);

/// <summary>Result of a tool mutation command.</summary>
public sealed record ToolMutationOutcome(bool Success, string? Error, ToolDetail? Tool);

/// <summary>List result for tool buckets.</summary>
public sealed record ListBucketsResult(IReadOnlyList<BucketDetail> Items, int TotalCount);

/// <summary>Detailed bucket definition.</summary>
public sealed record BucketDetail(
    int Id,
    string Name,
    string Owner,
    string Repo,
    string Branch,
    string ManifestPath,
    DateTimeOffset DateTimeCreated,
    DateTimeOffset? DateTimeLastSynced);

/// <summary>Result of a bucket mutation command.</summary>
public sealed record BucketMutationOutcome(bool Success, string? Error, BucketDetail? Bucket);

/// <summary>Manifest entry discovered when browsing a bucket.</summary>
public sealed record BucketToolManifest(
    string Name,
    string Description,
    IReadOnlyList<string> Tags,
    string? ParameterSchema,
    string? CommandTemplate,
    string ManifestFile);

/// <summary>Result for bucket browse operations.</summary>
public sealed record BucketBrowseOutcome(bool Success, string? Error, IReadOnlyList<BucketToolManifest> Tools);

/// <summary>Result for bucket synchronization operations.</summary>
public sealed record BucketSyncOutcome(bool Success, string? Error, int Updated, int Added, int Unchanged);
