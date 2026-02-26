namespace McpServer.Workspace.Validation.Models;

/// <summary>Read-only workspace view returned by the API.</summary>
public sealed class WorkspaceDto
{
    public string WorkspacePath { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string TodoPath { get; set; } = string.Empty;
    public string? TunnelProvider { get; set; }
    public DateTimeOffset DateTimeCreated { get; set; }
    public DateTimeOffset DateTimeModified { get; set; }
    public string? RunAs { get; set; }
}

/// <summary>Result of listing workspaces.</summary>
public sealed class WorkspaceListResult
{
    public List<WorkspaceDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
}

/// <summary>Result of a workspace mutation (create/update/delete).</summary>
public sealed class WorkspaceMutationResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public WorkspaceDto? Workspace { get; set; }
}

/// <summary>Result of workspace initialization.</summary>
public sealed class WorkspaceInitResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public List<string>? FilesCreated { get; set; }
}

/// <summary>Process status for a workspace instance.</summary>
public sealed class WorkspaceProcessStatus
{
    public bool IsRunning { get; set; }
    public int? Pid { get; set; }
    public string? Uptime { get; set; }
    public int? Port { get; set; }
    public string? Error { get; set; }
}

/// <summary>Error response shape.</summary>
public sealed class ErrorResponse
{
    public string? Error { get; set; }
}
