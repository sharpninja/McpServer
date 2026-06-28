using McpServer.Client;
using McpServer.McpAgent.PowerShellSessions;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.AI;

namespace McpServer.QBAgent.Tools;

/// <summary>
/// FR-MCP-QBTOOLS-001..004 / FR-MCP-QBTOOLS-007: Builds the agent-side external tool surface that QBAgent passes
/// to the Microsoft Agent Framework loop (via <c>baseOptions.ChatOptions.Tools</c>). The tools are deliberately
/// non-<c>mcp_</c>-prefixed so the QuadBrain interceptor treats them as external and lets QBAgent execute them.
/// File tools delegate to the MCP client (server-owned safety); git/bash use <see cref="IProcessRunner"/>;
/// run_powershell uses the in-process session manager.
/// </summary>
/// <remarks>
/// Threat model: the <c>git</c> tool's push gate (AllowGitPush, origin-only) is a convenience guardrail on the
/// structured git interface, not a sandbox. <c>run_bash</c> and <c>run_powershell</c> are general-purpose shells
/// that run with full shell semantics by design, so an agent that has them can run any command, including git.
/// Hosts that need to prevent remote mutation entirely must withhold the shell tools, not rely on the git gate.
/// </remarks>
public static class QBAgentExternalToolSurface
{
    /// <summary>Builds the external tool surface for a QBAgent run.</summary>
    /// <param name="client">The MCP transport client (file tools delegate to its Repo surface).</param>
    /// <param name="powerShellSessions">The hosted PowerShell session manager.</param>
    /// <param name="processRunner">The process runner used by the git and bash tools.</param>
    /// <param name="workspacePath">The workspace directory git/bash/powershell run in.</param>
    /// <param name="allowGitPush">Whether the git tool may run <c>push</c>.</param>
    /// <returns>A disposable tool set; dispose it to release the reused PowerShell session and gate.</returns>
    public static QBAgentToolSet Create(
        McpServerClient client,
        IHostedPowerShellSessionManager powerShellSessions,
        IProcessRunner processRunner,
        string workspacePath,
        bool allowGitPush)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(powerShellSessions);
        ArgumentNullException.ThrowIfNull(processRunner);
        ArgumentException.ThrowIfNullOrWhiteSpace(workspacePath);

        var files = new FileTools(client);
        var git = new GitCommandTool(processRunner, workspacePath, allowGitPush);
        var bash = new BashCommandTool(processRunner, workspacePath);
        var powerShell = new PowerShellTool(powerShellSessions, workspacePath);

        var tools = new AITool[]
        {
            Tool((Func<string, Task<Client.Models.RepoFileReadResult>>)(path => files.ReadFileAsync(path)),
                "read_file",
                "Read a workspace file by its repo-relative path. Returns the file content and whether it exists."),
            Tool((Func<string, string, Task<Client.Models.RepoWriteResult>>)((path, content) => files.WriteFileAsync(path, content)),
                "write_file",
                "Create or overwrite a workspace file with the full given content. Use edit_file for partial changes."),
            Tool((Func<string?, Task<Client.Models.RepoListResult>>)(path => files.ListFilesAsync(path)),
                "list_files",
                "List files and directories under a repo-relative path (or the workspace root when omitted)."),
            Tool((Func<string, string, string, Task<FileEditResult>>)((path, oldString, newString) =>
                    files.EditFileAsync(path, oldString, newString)),
                "edit_file",
                "Apply a targeted replacement of a unique oldString with newString in a workspace file."),
            Tool((Func<string, Task<PowerShellSessionCommandResult>>)(command => powerShell.RunAsync(command)),
                "run_powershell",
                "Run a PowerShell command in the workspace and return its output and error streams. Primary shell on this host."),
            Tool((Func<string, Task<BashToolResult>>)(command => bash.RunAsync(command)),
                "run_bash",
                "Run a command with Git Bash if available. Returns available=false when bash is not installed; prefer run_powershell."),
            Tool((Func<string, string?, Task<GitToolResult>>)((command, arguments) => git.RunAsync(command, arguments)),
                "git",
                "Run an allowlisted git subcommand (status, diff, log, branch, add, commit, checkout, push, reset, ...) in the workspace."),
        };

        return new QBAgentToolSet(tools, powerShell);
    }

    private static AIFunction Tool(Delegate implementation, string name, string description) =>
        AIFunctionFactory.Create(
            implementation,
            new AIFunctionFactoryOptions
            {
                Name = name,
                Description = description,
            });
}

/// <summary>
/// FR-MCP-QBTOOLS-007: The QBAgent external tool surface plus ownership of the disposable tool instances behind
/// it (the reused PowerShell session/gate). Dispose this when the QBAgent run ends.
/// </summary>
public sealed class QBAgentToolSet : IDisposable
{
    private readonly PowerShellTool _powerShellTool;
    private bool _disposed;

    /// <summary>Initializes a new instance of the <see cref="QBAgentToolSet"/> class.</summary>
    /// <param name="tools">The external tool list passed to the Agent Framework loop.</param>
    /// <param name="powerShellTool">The disposable PowerShell tool backing run_powershell.</param>
    internal QBAgentToolSet(IReadOnlyList<AITool> tools, PowerShellTool powerShellTool)
    {
        Tools = tools;
        _powerShellTool = powerShellTool;
    }

    /// <summary>Gets the external tools to merge into the agent run options.</summary>
    public IReadOnlyList<AITool> Tools { get; }

    /// <summary>Releases the reused PowerShell session and gate held by the tool set.</summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _powerShellTool.Dispose();
    }
}
