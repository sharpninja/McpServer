namespace McpServer.Support.Mcp.Models;

/// <summary>FR-MCP-WIKIEXPORT-003: Wiki dump schema version.</summary>
public static class WikiDumpDefaults
{
    /// <summary>Canonical dump schema.</summary>
    public const string SchemaVersion = "mcp-wiki-dump/v1";

    /// <summary>Dump file name written under the export root.</summary>
    public const string FileName = "mcp-wiki-dump.json";
}

/// <summary>FR-MCP-WIKIEXPORT-003: Export request.</summary>
public sealed class WikiDumpExportRequest
{
    /// <summary>Wiki export output root.</summary>
    public string OutputRoot { get; set; } = string.Empty;

    /// <summary>When false, no dump file is written.</summary>
    public bool IncludeDump { get; set; }

    /// <summary>Source workspace path.</summary>
    public string? WorkspacePath { get; set; }
}

/// <summary>FR-MCP-WIKIEXPORT-004: Import request.</summary>
public sealed class WikiDumpImportRequest
{
    /// <summary>Dump file or directory containing the dump.</summary>
    public string DumpPath { get; set; } = string.Empty;

    /// <summary>Destination workspace path.</summary>
    public string DestinationWorkspacePath { get; set; } = string.Empty;

    /// <summary>Optional todo.yaml path present beside the dump.</summary>
    public string? TodoYamlPath { get; set; }
}

/// <summary>FR-MCP-WIKIEXPORT-003: Dump document.</summary>
public sealed class WikiDumpDocument
{
    /// <summary>Schema version.</summary>
    public string SchemaVersion { get; set; } = WikiDumpDefaults.SchemaVersion;

    /// <summary>Export timestamp.</summary>
    public DateTimeOffset ExportedAtUtc { get; set; }

    /// <summary>Source workspace key.</summary>
    public string SourceWorkspaceKey { get; set; } = string.Empty;

    /// <summary>Source workspace path.</summary>
    public string SourceWorkspacePath { get; set; } = string.Empty;

    /// <summary>Table projections.</summary>
    public List<WikiDumpTable> Tables { get; set; } = [];

    /// <summary>TODO rows.</summary>
    public List<WikiDumpTodo> Todos { get; set; } = [];

    /// <summary>TODO-requirement links.</summary>
    public List<WikiDumpTodoRequirementLink> TodoRequirementLinks { get; set; } = [];

    /// <summary>Canonical SHA-256 of the dump without this field.</summary>
    public string Sha256 { get; set; } = string.Empty;

    /// <summary>Diagnostics.</summary>
    public List<string> Diagnostics { get; set; } = [];
}

/// <summary>FR-MCP-WIKIEXPORT-003: One table projection.</summary>
public sealed class WikiDumpTable
{
    /// <summary>DbSet/table name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Policy code.</summary>
    public string Policy { get; set; } = "COPY-SANITIZE";
}

/// <summary>FR-MCP-WIKIEXPORT-003: TODO projection.</summary>
public sealed class WikiDumpTodo
{
    /// <summary>TODO id.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Section.</summary>
    public string Section { get; set; } = string.Empty;

    /// <summary>Priority.</summary>
    public string Priority { get; set; } = "medium";

    /// <summary>Done.</summary>
    public bool Done { get; set; }
}

/// <summary>FR-MCP-WIKIEXPORT-003: TODO requirement link projection.</summary>
public sealed class WikiDumpTodoRequirementLink
{
    /// <summary>TODO id.</summary>
    public string TodoId { get; set; } = string.Empty;

    /// <summary>Requirement kind.</summary>
    public string RequirementKind { get; set; } = string.Empty;

    /// <summary>Requirement id.</summary>
    public string RequirementId { get; set; } = string.Empty;
}

/// <summary>FR-MCP-WIKIEXPORT-003: Export result.</summary>
public sealed class WikiDumpExportResult
{
    /// <summary>True when the export completed.</summary>
    public bool Success { get; set; }

    /// <summary>Dump file path when written.</summary>
    public string? DumpFilePath { get; set; }

    /// <summary>Error.</summary>
    public string? Error { get; set; }

    /// <summary>Dump document when written.</summary>
    public WikiDumpDocument? Dump { get; set; }
}

/// <summary>FR-MCP-WIKIEXPORT-004: Import result.</summary>
public sealed class WikiDumpImportResult
{
    /// <summary>True when import completed.</summary>
    public bool Success { get; set; }

    /// <summary>Error code.</summary>
    public string? ErrorCode { get; set; }

    /// <summary>Error.</summary>
    public string? Error { get; set; }

    /// <summary>Created TODO ids.</summary>
    public List<string> CreatedTodoIds { get; set; } = [];

    /// <summary>Diagnostics.</summary>
    public List<string> Diagnostics { get; set; } = [];

    /// <summary>Archive path when todo.yaml was archived.</summary>
    public string? TodoYamlArchivePath { get; set; }
}
