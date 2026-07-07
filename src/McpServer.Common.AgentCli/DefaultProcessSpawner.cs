using System.Diagnostics;

namespace McpServer.Common.AgentCli;

/// <summary>
/// Default <see cref="IProcessSpawner"/> that uses <see cref="Process.Start(ProcessStartInfo)"/>.
/// Suitable when the host process runs in an interactive desktop session.
/// </summary>
public sealed class DefaultProcessSpawner : IProcessSpawner
{
    /// <inheritdoc />
    public ISpawnedProcess Spawn(ProcessStartInfo startInfo)
    {
        var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Process.Start returned null.");
        return new ManagedProcess(process);
    }

    /// <summary>
    /// Wraps a <see cref="Process"/> as an <see cref="ISpawnedProcess"/>.
    /// </summary>
    private sealed class ManagedProcess(Process process) : ISpawnedProcess
    {
        /// <inheritdoc />
        public StreamReader StandardOutput => process.StandardOutput;

        /// <inheritdoc />
        public StreamReader StandardError => process.StandardError;

        /// <inheritdoc />
        public StreamWriter? StandardInput =>
            process.StartInfo.RedirectStandardInput ? process.StandardInput : null;

        /// <inheritdoc />
        public int Id => process.Id;

        /// <inheritdoc />
        public bool HasExited => process.HasExited;

        /// <inheritdoc />
        public int ExitCode => process.ExitCode;

        /// <inheritdoc />
        public Task WaitForExitAsync(CancellationToken cancellationToken = default) =>
            process.WaitForExitAsync(cancellationToken);

        /// <inheritdoc />
        public void Kill() => process.Kill(entireProcessTree: true);

        /// <inheritdoc />
        public void Dispose() => process.Dispose();
    }
}
