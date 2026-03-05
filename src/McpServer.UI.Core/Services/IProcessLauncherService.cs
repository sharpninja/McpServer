namespace McpServer.UI.Core.Services;

/// <summary>
/// Host-provided abstraction for launching external processes from UI.Core ViewModels.
/// </summary>
public interface IProcessLauncherService
{
    /// <summary>Opens a file or URL with the OS default application.</summary>
    void OpenWithDefaultApp(string pathOrUrl);

    /// <summary>Runs a process and captures its output.</summary>
    Task<ProcessResult> RunAsync(
        string fileName,
        string arguments,
        string? workingDirectory = null,
        CancellationToken ct = default);

    /// <summary>Runs a command via the OS shell (UseShellExecute = true).</summary>
    void ShellExecute(string command, string arguments);
}

/// <summary>Result of a completed process execution.</summary>
/// <param name="ExitCode">Process exit code.</param>
/// <param name="StandardOutput">Captured stdout content.</param>
/// <param name="StandardError">Captured stderr content.</param>
public sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
