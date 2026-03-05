using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace McpServer.Client.Models;

/// <summary>A tool definition.</summary>
public sealed class ToolDto
{
    /// <summary>Tool database ID.</summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>Tool name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Tool description.</summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>Keyword tags.</summary>
    [JsonPropertyName("tags")]
    public IReadOnlyList<string> Tags { get; set; } = [];

    /// <summary>JSON Schema for parameters.</summary>
    [JsonPropertyName("parameterSchema")]
    public string? ParameterSchema { get; set; }

    /// <summary>Command template.</summary>
    [JsonPropertyName("commandTemplate")]
    public string? CommandTemplate { get; set; }

    /// <summary>Workspace scope (null = global).</summary>
    [JsonPropertyName("workspacePath")]
    public string? WorkspacePath { get; set; }

    /// <summary>Creation timestamp.</summary>
    [JsonPropertyName("dateTimeCreated")]
    public DateTimeOffset DateTimeCreated { get; set; }

    /// <summary>Last modification timestamp.</summary>
    [JsonPropertyName("dateTimeModified")]
    public DateTimeOffset DateTimeModified { get; set; }
}

/// <summary>Request to create a tool.</summary>
public sealed class ToolCreateRequest
{
    /// <summary>Tool name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Tool description.</summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>Keyword tags.</summary>
    [JsonPropertyName("tags")]
    public IReadOnlyList<string> Tags { get; set; } = [];

    /// <summary>JSON Schema for parameters.</summary>
    [JsonPropertyName("parameterSchema")]
    public string? ParameterSchema { get; set; }

    /// <summary>Command template.</summary>
    [JsonPropertyName("commandTemplate")]
    public string? CommandTemplate { get; set; }

    /// <summary>Workspace scope (null = global).</summary>
    [JsonPropertyName("workspacePath")]
    public string? WorkspacePath { get; set; }
}

/// <summary>Request to update a tool.</summary>
public sealed class ToolUpdateRequest
{
    /// <summary>Updated name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>Updated description.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>Updated tags.</summary>
    [JsonPropertyName("tags")]
    public IReadOnlyList<string>? Tags { get; set; }

    /// <summary>Updated parameter schema.</summary>
    [JsonPropertyName("parameterSchema")]
    public string? ParameterSchema { get; set; }

    /// <summary>Updated command template.</summary>
    [JsonPropertyName("commandTemplate")]
    public string? CommandTemplate { get; set; }

    /// <summary>Updated workspace scope.</summary>
    [JsonPropertyName("workspacePath")]
    public string? WorkspacePath { get; set; }
}

/// <summary>Result of a tool search.</summary>
public sealed class ToolSearchResult
{
    /// <summary>Matching tools.</summary>
    [JsonPropertyName("tools")]
    public IReadOnlyList<ToolDto> Tools { get; set; } = [];

    /// <summary>Total matching count.</summary>
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }
}

/// <summary>Result of a tool mutation.</summary>
public sealed class ToolMutationResult
{
    /// <summary>Whether the operation succeeded.</summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>Error message.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>The affected tool.</summary>
    [JsonPropertyName("tool")]
    public ToolDto? Tool { get; set; }
}

/// <summary>A tool bucket (GitHub-backed tool repository).</summary>
public sealed class BucketDto
{
    /// <summary>Bucket database ID.</summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>Bucket name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>GitHub repository owner.</summary>
    [JsonPropertyName("owner")]
    public string Owner { get; set; } = string.Empty;

    /// <summary>GitHub repository name.</summary>
    [JsonPropertyName("repo")]
    public string Repo { get; set; } = string.Empty;

    /// <summary>Git branch.</summary>
    [JsonPropertyName("branch")]
    public string Branch { get; set; } = string.Empty;

    /// <summary>Path to the tool manifest in the repo.</summary>
    [JsonPropertyName("manifestPath")]
    public string ManifestPath { get; set; } = string.Empty;

    /// <summary>Creation timestamp.</summary>
    [JsonPropertyName("dateTimeCreated")]
    public DateTimeOffset DateTimeCreated { get; set; }

    /// <summary>Last sync timestamp.</summary>
    [JsonPropertyName("dateTimeLastSynced")]
    public DateTimeOffset? DateTimeLastSynced { get; set; }
}

/// <summary>Request to add a tool bucket.</summary>
public sealed class BucketAddRequest
{
    /// <summary>Bucket name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>GitHub repository owner.</summary>
    [JsonPropertyName("owner")]
    public string Owner { get; set; } = string.Empty;

    /// <summary>GitHub repository name.</summary>
    [JsonPropertyName("repo")]
    public string Repo { get; set; } = string.Empty;

    /// <summary>Git branch (default: main).</summary>
    [JsonPropertyName("branch")]
    public string? Branch { get; set; }

    /// <summary>Manifest path in the repo.</summary>
    [JsonPropertyName("manifestPath")]
    public string? ManifestPath { get; set; }
}

/// <summary>Result of listing buckets.</summary>
public sealed class BucketListResult
{
    /// <summary>Buckets.</summary>
    [JsonPropertyName("buckets")]
    public IReadOnlyList<BucketDto> Buckets { get; set; } = [];

    /// <summary>Total count.</summary>
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }
}

/// <summary>Result of a bucket mutation.</summary>
public sealed class BucketMutationResult
{
    /// <summary>Whether the operation succeeded.</summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>Error message.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>The affected bucket.</summary>
    [JsonPropertyName("bucket")]
    public BucketDto? Bucket { get; set; }
}

/// <summary>A tool manifest entry from a bucket.</summary>
public sealed class ToolManifest
{
    /// <summary>Tool name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Tool description.</summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>Tags.</summary>
    [JsonPropertyName("tags")]
    public IReadOnlyList<string> Tags { get; set; } = [];

    /// <summary>Parameter schema.</summary>
    [JsonPropertyName("parameterSchema")]
    public string? ParameterSchema { get; set; }

    /// <summary>Command template.</summary>
    [JsonPropertyName("commandTemplate")]
    public string? CommandTemplate { get; set; }

    /// <summary>Source manifest file.</summary>
    [JsonPropertyName("manifestFile")]
    public string ManifestFile { get; set; } = string.Empty;
}

/// <summary>Result of browsing a bucket's tools.</summary>
public sealed class BucketBrowseResult
{
    /// <summary>Whether the browse succeeded.</summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>Error message.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>Available tools in the bucket.</summary>
    [JsonPropertyName("tools")]
    public IReadOnlyList<ToolManifest>? Tools { get; set; }
}

/// <summary>Result of syncing a bucket.</summary>
public sealed class BucketSyncResult
{
    /// <summary>Whether the sync succeeded.</summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>Error message.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>Tools updated.</summary>
    [JsonPropertyName("updated")]
    public int Updated { get; set; }

    /// <summary>Tools added.</summary>
    [JsonPropertyName("added")]
    public int Added { get; set; }

    /// <summary>Tools unchanged.</summary>
    [JsonPropertyName("unchanged")]
    public int Unchanged { get; set; }
}
