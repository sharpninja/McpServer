using McpServer.Launcher.Models;

namespace McpServer.Launcher.Services;

/// <summary>
/// Launches a process on the interactive desktop using <c>CreateProcessWithTokenW</c>.
/// </summary>
public interface IProcessLauncher
{
    /// <summary>
    /// Launches a process with the specified parameters on the interactive desktop.
    /// </summary>
    /// <param name="request">The process launch parameters.</param>
    /// <returns>A result indicating success or failure with details.</returns>
    ProcessLaunchResult Launch(ProcessLaunchRequest request);
}
