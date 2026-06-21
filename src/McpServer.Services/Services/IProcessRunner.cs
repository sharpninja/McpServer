namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-PLANNED-CORE-013: Abstraction for running external processes, enabling testability.
/// </summary>
public interface IProcessRunner
{
    /// <summary>TR-PLANNED-CORE-013: Run an external process and return its result.</summary>
    /// <param name="fileName">Executable file name.</param>
    /// <param name="arguments">Command-line arguments.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Process run result with exit code, stdout, and stderr.</returns>
    Task<ProcessRunResult> RunAsync(string fileName, string arguments, CancellationToken ct = default);

    /// <summary>
    /// TR-MCP-GH-003: Run an external process with optional per-call environment overrides.
    /// </summary>
    /// <param name="request">Structured process run request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Process run result with exit code, stdout, and stderr.</returns>
    Task<ProcessRunResult> RunAsync(ProcessRunRequest request, CancellationToken ct = default);
}

/// <summary>TR-PLANNED-CORE-013: Result of running an external process.</summary>
/// <param name="ExitCode">Process exit code.</param>
/// <param name="Stdout">Standard output text.</param>
/// <param name="Stderr">Standard error text.</param>
public sealed record ProcessRunResult(int ExitCode, string? Stdout, string? Stderr);

/// <summary>
/// TR-MCP-GH-003: Structured process run request with optional execution metadata.
/// </summary>
/// <param name="FileName">Executable file name.</param>
/// <param name="Arguments">Command-line arguments.</param>
/// <param name="GitHubTokenOverride">Optional token override passed as <c>GH_TOKEN</c>.</param>
/// <param name="WorkingDirectory">Optional working directory for the process.</param>
/// <param name="EnvironmentVariables">Optional per-process environment variable overrides.</param>
public sealed record ProcessRunRequest(
    string FileName,
    string Arguments,
    string? GitHubTokenOverride = null,
    string? WorkingDirectory = null,
    IReadOnlyDictionary<string, string?>? EnvironmentVariables = null);
