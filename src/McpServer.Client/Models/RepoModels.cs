using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace McpServer.Client.Models;

/// <summary>Result of reading a repository file.</summary>
public sealed class RepoFileReadResult
{
    /// <summary>File path.</summary>
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    /// <summary>File content.</summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>Whether the file exists.</summary>
    [JsonPropertyName("exists")]
    public bool Exists { get; set; }
}

/// <summary>Request to write a repository file.</summary>
public sealed class RepoWriteRequest
{
    /// <summary>File path relative to repo root.</summary>
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    /// <summary>File content to write.</summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }
}

/// <summary>Result of writing a repository file.</summary>
public sealed class RepoWriteResult
{
    /// <summary>File path.</summary>
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    /// <summary>Whether the write succeeded.</summary>
    [JsonPropertyName("written")]
    public bool Written { get; set; }
}

/// <summary>FR-MCP-QBTOOLS-006: Request for a targeted repository file edit.</summary>
public sealed class RepoEditRequest
{
    /// <summary>File path relative to repo root.</summary>
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    /// <summary>Exact text to find.</summary>
    [JsonPropertyName("oldString")]
    public string? OldString { get; set; }

    /// <summary>Replacement text.</summary>
    [JsonPropertyName("newString")]
    public string? NewString { get; set; }

    /// <summary>When true, replaces every occurrence instead of requiring a unique match.</summary>
    [JsonPropertyName("replaceAll")]
    public bool ReplaceAll { get; set; }

    /// <summary>Optional expected match-count guard.</summary>
    [JsonPropertyName("expectedOccurrences")]
    public int? ExpectedOccurrences { get; set; }
}

/// <summary>FR-MCP-QBTOOLS-006: Result of a targeted repository file edit.</summary>
public sealed class RepoEditResult
{
    /// <summary>File path.</summary>
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    /// <summary>Whether the edit was applied.</summary>
    [JsonPropertyName("written")]
    public bool Written { get; set; }

    /// <summary>Number of replacements performed.</summary>
    [JsonPropertyName("replacements")]
    public int Replacements { get; set; }

    /// <summary>Error message when the edit was not applied.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

/// <summary>Result of listing repository files.</summary>
public sealed class RepoListResult
{
    /// <summary>Directory path.</summary>
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    /// <summary>Directory entries.</summary>
    [JsonPropertyName("entries")]
    public IReadOnlyList<RepoListEntry> Entries { get; set; } = [];
}

/// <summary>A single entry in a repository listing.</summary>
public sealed class RepoListEntry
{
    /// <summary>Entry name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Whether this entry is a directory.</summary>
    [JsonPropertyName("isDirectory")]
    public bool IsDirectory { get; set; }
}
