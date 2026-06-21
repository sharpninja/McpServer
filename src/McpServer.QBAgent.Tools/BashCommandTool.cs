using McpServer.Support.Mcp.Services;

namespace McpServer.QBAgent.Tools;

/// <summary>
/// FR-MCP-QBTOOLS-003 / TR-MCP-QBTOOLS-003: Optional agent-side <c>run_bash</c> tool. Runs a command through Git
/// Bash (<c>bash.exe</c> resolved from PATH) via <see cref="IProcessRunner"/>. When bash is not installed the tool
/// returns a structured <see cref="BashToolResult.Available"/> = <see langword="false"/> result instead of failing
/// the agent turn; PowerShell (<c>run_powershell</c>) remains the primary shell on Windows.
/// </summary>
public sealed class BashCommandTool
{
    private readonly IProcessRunner _processRunner;
    private readonly string _workspacePath;

    /// <summary>Initializes a new instance of the <see cref="BashCommandTool"/> class.</summary>
    /// <param name="processRunner">The process runner used to launch bash.</param>
    /// <param name="workspacePath">The workspace directory bash runs in.</param>
    public BashCommandTool(IProcessRunner processRunner, string workspacePath)
    {
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
        _workspacePath = workspacePath ?? throw new ArgumentNullException(nameof(workspacePath));
    }

    /// <summary>Runs a command with Git Bash, if available.</summary>
    /// <param name="command">The bash command line to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The bash command result, including availability.</returns>
    public async Task<BashToolResult> RunAsync(string command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command))
            return new BashToolResult(Available: true, Success: false, ExitCode: -1, Output: null, Error: "A bash command is required.");

        var result = await _processRunner.RunAsync(
            new ProcessRunRequest("bash", $"-lc {QuoteArgument(command)}", WorkingDirectory: _workspacePath),
            cancellationToken).ConfigureAwait(false);

        if (IsExecutableMissing(result))
        {
            return new BashToolResult(
                Available: false,
                Success: false,
                ExitCode: -1,
                Output: null,
                Error: "bash is not available on this host (Git Bash not found on PATH). Use run_powershell instead.");
        }

        return new BashToolResult(
            Available: true,
            Success: result.ExitCode == 0,
            ExitCode: result.ExitCode,
            Output: result.Stdout,
            Error: result.Stderr);
    }

    private static bool IsExecutableMissing(ProcessRunResult result)
        => result.ExitCode == -1
           && result.Stderr is not null
           && result.Stderr.Contains("not found", StringComparison.OrdinalIgnoreCase);

    private static string QuoteArgument(string argument)
    {
        // bash -lc expects a single quoted script argument; wrap in double quotes and escape embedded quotes and
        // backslashes so the argv string handed to bash.exe is a single token.
        var escaped = argument.Replace("\\", "\\\\", StringComparison.Ordinal)
                              .Replace("\"", "\\\"", StringComparison.Ordinal);
        return $"\"{escaped}\"";
    }
}
