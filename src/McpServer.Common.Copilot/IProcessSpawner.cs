using System.Diagnostics;

namespace McpServer.Common.Copilot;

/// <summary>
/// Factory for spawning child processes. The default implementation uses
/// <see cref="Process.Start(ProcessStartInfo)"/>; the desktop-aware
/// implementation in the host project uses <c>CreateProcessAsUser</c>
/// to launch on the interactive desktop session.
/// </summary>
public interface IProcessSpawner
{
    /// <summary>
    /// Spawns a new process using the supplied <paramref name="startInfo"/>.
    /// </summary>
    /// <param name="startInfo">Process start configuration.</param>
    /// <returns>An <see cref="ISpawnedProcess"/> wrapping the running process.</returns>
    /// <exception cref="InvalidOperationException">The process could not be started.</exception>
    ISpawnedProcess Spawn(ProcessStartInfo startInfo);
}
