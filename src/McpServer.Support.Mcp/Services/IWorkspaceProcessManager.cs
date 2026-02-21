namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-011 / TR-MCP-WS-003: Manages child MCP processes for workspaces.
/// </summary>
public interface IWorkspaceProcessManager : IHostedService
{
    /// <summary>Start a hosted MCP instance for the given workspace.</summary>
    Task<WorkspaceProcessStatus> StartAsync(string workspacePath, int port, CancellationToken ct = default);

    /// <summary>Stop the hosted MCP instance for the given workspace.</summary>
    Task<WorkspaceProcessStatus> StopAsync(string workspacePath, CancellationToken ct = default);

    /// <summary>Get the process status of a workspace instance.</summary>
    WorkspaceProcessStatus GetStatus(string workspacePath);

    /// <summary>Stop all running workspace processes.</summary>
    Task StopAllAsync(CancellationToken ct = default);
}

/// <summary>Process status for a workspace instance.</summary>
public sealed record WorkspaceProcessStatus(bool IsRunning, int? Pid = null, TimeSpan? Uptime = null, int? Port = null, string? Error = null);
