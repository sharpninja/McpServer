namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-PLANNED-013: Abstraction for running external processes, enabling testability.
/// </summary>
public interface IProcessRunner
{
    /// <summary>TR-PLANNED-013: Run an external process and return its result.</summary>
    /// <param name="fileName">Executable file name.</param>
    /// <param name="arguments">Command-line arguments.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Process run result with exit code, stdout, and stderr.</returns>
    Task<ProcessRunResult> RunAsync(string fileName, string arguments, CancellationToken ct = default);
}

/// <summary>TR-PLANNED-013: Result of running an external process.</summary>
/// <param name="ExitCode">Process exit code.</param>
/// <param name="Stdout">Standard output text.</param>
/// <param name="Stderr">Standard error text.</param>
public sealed record ProcessRunResult(int ExitCode, string? Stdout, string? Stderr);
