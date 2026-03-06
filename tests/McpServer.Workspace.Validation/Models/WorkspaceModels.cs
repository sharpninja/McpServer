namespace McpServer.Workspace.Validation.Models;

/// <summary>Read-only workspace view returned by the API.</summary>
public sealed class WorkspaceDto
{
    /// <summary>Gets or sets WorkspacePath.</summary>
    public string WorkspacePath { get; set; } = string.Empty;
    /// <summary>Gets or sets Name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets TodoPath.</summary>
    public string TodoPath { get; set; } = string.Empty;
    /// <summary>Gets or sets TunnelProvider.</summary>
    public string? TunnelProvider { get; set; }
    /// <summary>Gets or sets DateTimeCreated.</summary>
    public DateTimeOffset DateTimeCreated { get; set; }
    /// <summary>Gets or sets DateTimeModified.</summary>
    public DateTimeOffset DateTimeModified { get; set; }
    /// <summary>Gets or sets RunAs.</summary>
    public string? RunAs { get; set; }
}

/// <summary>Result of listing workspaces.</summary>
public sealed class WorkspaceListResult
{
    /// <summary>Gets or sets Items.</summary>
    public List<WorkspaceDto> Items { get; set; } = [];
    /// <summary>Gets or sets TotalCount.</summary>
    public int TotalCount { get; set; }
}

/// <summary>Result of a workspace mutation (create/update/delete).</summary>
public sealed class WorkspaceMutationResult
{
    /// <summary>Gets or sets Success.</summary>
    public bool Success { get; set; }
    /// <summary>Gets or sets Error.</summary>
    public string? Error { get; set; }
    /// <summary>Gets or sets Workspace.</summary>
    public WorkspaceDto? Workspace { get; set; }
}

/// <summary>Result of workspace initialization.</summary>
public sealed class WorkspaceInitResult
{
    /// <summary>Gets or sets Success.</summary>
    public bool Success { get; set; }
    /// <summary>Gets or sets Error.</summary>
    public string? Error { get; set; }
    /// <summary>Gets or sets FilesCreated.</summary>
    public List<string>? FilesCreated { get; set; }
}

/// <summary>Process status for a workspace instance.</summary>
public sealed class WorkspaceProcessStatus
{
    /// <summary>Gets or sets IsRunning.</summary>
    public bool IsRunning { get; set; }
    /// <summary>Gets or sets Pid.</summary>
    public int? Pid { get; set; }
    /// <summary>Gets or sets Uptime.</summary>
    public string? Uptime { get; set; }
    /// <summary>Gets or sets Port.</summary>
    public int? Port { get; set; }
    /// <summary>Gets or sets Error.</summary>
    public string? Error { get; set; }
}

/// <summary>Error response shape.</summary>
public sealed class ErrorResponse
{
    /// <summary>Gets or sets Error.</summary>
    public string? Error { get; set; }
}
