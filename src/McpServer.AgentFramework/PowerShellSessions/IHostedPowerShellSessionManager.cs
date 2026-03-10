using System.IO;

namespace McpServer.AgentFramework.PowerShellSessions;

/// <summary>
/// FR-MCP-066/TR-MCP-AGENT-007: Host-facing contract for managing persistent PowerShell sessions
/// that run directly inside the current .NET agent process.
/// </summary>
public interface IHostedPowerShellSessionManager : IDisposable
{
    /// <summary>
    /// Creates a new PowerShell session rooted at the supplied workspace path and optional working
    /// directory.
    /// </summary>
    /// <param name="workspacePath">The workspace path used to initialize the session context.</param>
    /// <param name="workingDirectory">
    /// Optional explicit working directory. When omitted, the workspace path becomes the current
    /// location for the session.
    /// </param>
    /// <returns>The result describing the newly created session.</returns>
    PowerShellSessionCreateResult CreateSession(string workspacePath, string? workingDirectory = null);

    /// <summary>
    /// Executes a non-interactive PowerShell command inside an existing hosted session.
    /// </summary>
    /// <param name="sessionId">The targeted PowerShell session identifier.</param>
    /// <param name="command">The PowerShell script text to execute.</param>
    /// <param name="cancellationToken">The cancellation token used to stop the command.</param>
    /// <returns>The formatted result returned by the hosted PowerShell session.</returns>
    Task<PowerShellSessionCommandResult> ExecuteCommandAsync(
        string sessionId,
        string command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an interactive PowerShell command inside an existing hosted session.
    /// </summary>
    /// <param name="sessionId">The targeted PowerShell session identifier.</param>
    /// <param name="command">The PowerShell script text to execute.</param>
    /// <param name="readLine">
    /// Delegate invoked whenever the hosted PowerShell command requests a line of interactive input.
    /// </param>
    /// <param name="outputWriter">The writer that receives host-facing standard output text.</param>
    /// <param name="errorWriter">The writer that receives host-facing error text.</param>
    /// <param name="cancellationToken">The cancellation token used to stop the command.</param>
    /// <returns>The formatted result returned by the hosted PowerShell session.</returns>
    Task<PowerShellSessionCommandResult> ExecuteInteractiveCommandAsync(
        string sessionId,
        string command,
        Func<CancellationToken, string?> readLine,
        TextWriter outputWriter,
        TextWriter errorWriter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes an existing hosted PowerShell session and releases its resources.
    /// </summary>
    /// <param name="sessionId">The targeted PowerShell session identifier.</param>
    /// <returns>The result describing the close attempt.</returns>
    PowerShellSessionCloseResult CloseSession(string sessionId);
}
