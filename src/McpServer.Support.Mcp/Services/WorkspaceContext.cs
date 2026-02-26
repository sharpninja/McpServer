namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-MT-001: Per-request ambient context holding the resolved workspace identity.
/// Registered as a scoped service and populated by the workspace resolution middleware
/// before downstream services execute.
/// </summary>
public sealed class WorkspaceContext
{
    /// <summary>Normalized absolute path to the workspace root folder.</summary>
    public string? WorkspacePath { get; set; }

    /// <summary>Human-readable workspace name.</summary>
    public string? WorkspaceName { get; set; }

    /// <summary>Override directory for mcp.db and related data files. Null = <see cref="WorkspacePath"/>.</summary>
    public string? DataDirectory { get; set; }

    /// <summary>Relative path to the TODO file within the workspace.</summary>
    public string? TodoFilePath { get; set; }

    /// <summary>Path to the sessions directory within the workspace.</summary>
    public string? SessionsPath { get; set; }

    /// <summary>Path to the external docs directory within the workspace.</summary>
    public string? ExternalDocsPath { get; set; }

    /// <summary>Whether the API key used was a default (anonymous) token rather than full-access.</summary>
    public bool IsDefaultKey { get; set; }

    /// <summary>Whether workspace resolution succeeded (i.e. <see cref="WorkspacePath"/> is non-null).</summary>
    public bool IsResolved => WorkspacePath is not null;
}
