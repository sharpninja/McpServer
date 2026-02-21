namespace McpServer.ToolRegistry.Validation.Models;

public sealed class ToolDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
    public string? ParameterSchema { get; set; }
    public string? CommandTemplate { get; set; }
    public string? WorkspacePath { get; set; }
    public DateTimeOffset DateTimeCreated { get; set; }
    public DateTimeOffset DateTimeModified { get; set; }
}

public sealed class ToolSearchResult
{
    public List<ToolDto> Tools { get; set; } = [];
    public int TotalCount { get; set; }
}

public sealed class ToolMutationResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public ToolDto? Tool { get; set; }
}

public sealed class BucketDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public string Repo { get; set; } = string.Empty;
    public string Branch { get; set; } = string.Empty;
    public string ManifestPath { get; set; } = string.Empty;
    public DateTimeOffset DateTimeCreated { get; set; }
    public DateTimeOffset? DateTimeLastSynced { get; set; }
}

public sealed class BucketListResult
{
    public List<BucketDto> Buckets { get; set; } = [];
    public int TotalCount { get; set; }
}

public sealed class BucketMutationResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public BucketDto? Bucket { get; set; }
}

public sealed class ToolManifest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
    public string? ParameterSchema { get; set; }
    public string? CommandTemplate { get; set; }
    public string ManifestFile { get; set; } = string.Empty;
}

public sealed class BucketBrowseResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public List<ToolManifest>? Tools { get; set; }
}

public sealed class BucketSyncResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public int Updated { get; set; }
    public int Added { get; set; }
    public int Unchanged { get; set; }
}
