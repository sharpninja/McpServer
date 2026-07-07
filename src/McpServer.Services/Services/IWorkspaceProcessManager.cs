namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-011, TR-MCP-WS-003, TR-MCP-MT-001: Manages workspace registration, token generation, and marker file lifecycle.
/// In the single-port multi-tenant model, all workspaces are served by one host application.
/// </summary>
public interface IWorkspaceProcessManager : IHostedService
{
    /// <summary>Register a workspace: generate tokens and write marker file.</summary>
    /// <param name="workspace">Full workspace definition (passed through to Handlebars prompt templates).</param>
    /// <param name="ct">Cancellation token.</param>
    Task<WorkspaceProcessStatus> StartAsync(WorkspaceDto workspace, CancellationToken ct = default);

    /// <summary>Unregister a workspace and remove its marker file.</summary>
    Task<WorkspaceProcessStatus> StopAsync(string workspacePath, CancellationToken ct = default);

    /// <summary>Get the registration status of a workspace.</summary>
    WorkspaceProcessStatus GetStatus(string workspacePath);

    /// <summary>Unregister all workspaces and remove marker files.</summary>
    Task StopAllAsync(CancellationToken ct = default);

    /// <summary>Regenerate all marker files for running workspaces (e.g. after a prompt template change).</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <param name="globalPromptOverride">
    /// When non-null, use this value as the global prompt template instead of reading from options.
    /// Pass <see cref="string.Empty"/> to force the built-in default prompt.
    /// </param>
    Task<int> RegenerateAllMarkersAsync(CancellationToken ct = default, string? globalPromptOverride = null);
}

/// <summary>Process status for a workspace instance.</summary>
public sealed record WorkspaceProcessStatus(bool IsRunning, int? Pid = null, TimeSpan? Uptime = null, int? Port = null, string? Error = null);
