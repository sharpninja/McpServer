using System.Diagnostics;

namespace McpServer.Common.Copilot;

/// <summary>
/// Abstraction over a spawned child process, providing access to redirected
/// stdio streams, exit status, and lifecycle control.
/// Implementations may use <see cref="Process"/> directly or launch on the
/// interactive desktop via native APIs.
/// </summary>
public interface ISpawnedProcess : IDisposable
{
    /// <summary>Reader connected to the process's standard output.</summary>
    StreamReader StandardOutput { get; }

    /// <summary>Reader connected to the process's standard error.</summary>
    StreamReader StandardError { get; }

    /// <summary>Writer connected to the process's standard input (when redirected).</summary>
    StreamWriter? StandardInput { get; }

    /// <summary>The OS process identifier.</summary>
    int Id { get; }

    /// <summary>Whether the process has exited.</summary>
    bool HasExited { get; }

    /// <summary>The exit code of the process (valid only after exit).</summary>
    int ExitCode { get; }

    /// <summary>Waits asynchronously for the process to exit.</summary>
    Task WaitForExitAsync(CancellationToken cancellationToken = default);

    /// <summary>Attempts to kill the process and its descendants.</summary>
    void Kill();
}
