namespace McpServer.Support.Mcp.Options;

/// <summary>
/// Configuration options for the tool registry, including default buckets
/// that are automatically registered on first startup.
/// </summary>
public sealed class ToolRegistryOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Mcp:ToolRegistry";

    /// <summary>
    /// Default tool buckets to register on startup if they don't already exist.
    /// This ensures new installations have the primary tool repository configured.
    /// </summary>
    public List<DefaultBucketEntry> DefaultBuckets { get; set; } = [];
}

/// <summary>
/// A bucket entry to seed on startup.
/// </summary>
public sealed class DefaultBucketEntry
{
    /// <summary>Short unique name for the bucket (e.g. "official").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>GitHub repository owner.</summary>
    public string Owner { get; set; } = string.Empty;

    /// <summary>GitHub repository name.</summary>
    public string Repo { get; set; } = string.Empty;

    /// <summary>Branch to read from (default: "main").</summary>
    public string Branch { get; set; } = "main";

    /// <summary>Path within the repo for manifest files (default: "/").</summary>
    public string ManifestPath { get; set; } = "/";
}
