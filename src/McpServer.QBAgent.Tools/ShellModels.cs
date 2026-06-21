namespace McpServer.QBAgent.Tools;

/// <summary>
/// FR-MCP-QBTOOLS-004: Result of a QBAgent <c>git</c> tool invocation. The shape is serialized to the model so
/// it can reason over the outcome of a version-control command.
/// </summary>
/// <param name="Success">Whether the git command exited with code 0.</param>
/// <param name="ExitCode">The process exit code (-1 when git could not be launched).</param>
/// <param name="Output">Captured standard output, when any.</param>
/// <param name="Error">Captured standard error or a guard-rejection reason, when any.</param>
public sealed record GitToolResult(bool Success, int ExitCode, string? Output, string? Error);

/// <summary>
/// FR-MCP-QBTOOLS-003: Result of a QBAgent <c>run_bash</c> tool invocation. When Git Bash is not installed the
/// tool reports <see cref="Available"/> = <see langword="false"/> rather than failing the agent turn.
/// </summary>
/// <param name="Available">Whether a <c>bash</c> executable was found on PATH.</param>
/// <param name="Success">Whether the command exited with code 0 (always false when unavailable).</param>
/// <param name="ExitCode">The process exit code (-1 when bash could not be launched).</param>
/// <param name="Output">Captured standard output, when any.</param>
/// <param name="Error">Captured standard error or an availability message, when any.</param>
public sealed record BashToolResult(bool Available, bool Success, int ExitCode, string? Output, string? Error);

/// <summary>
/// FR-MCP-QBTOOLS-001 / FR-MCP-QBTOOLS-006: Result of a QBAgent <c>edit_file</c> tool invocation. In Phase A this
/// is produced by a stub; Phase B maps the server <c>RepoFileService.EditAsync</c> result onto the same shape so
/// the tool's contract is stable across phases.
/// </summary>
/// <param name="Written">Whether the edit was applied.</param>
/// <param name="Replacements">The number of replacements performed.</param>
/// <param name="Error">A failure reason when the edit was not applied.</param>
public sealed record FileEditResult(bool Written, int Replacements, string? Error);
