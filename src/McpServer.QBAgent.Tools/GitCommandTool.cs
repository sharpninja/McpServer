using McpServer.Support.Mcp.Services;

namespace McpServer.QBAgent.Tools;

/// <summary>
/// FR-MCP-QBTOOLS-004 / TR-MCP-QBTOOLS-004: Agent-side <c>git</c> tool. Runs an allowlisted git subcommand in the
/// workspace through <see cref="IProcessRunner"/> (no shell, so arguments are passed as git argv and cannot be
/// shell-injected). The <c>push</c> subcommand is constrained to the <c>origin</c> remote and is gated behind an
/// explicit opt-in so an autonomous agent cannot move remote state by default.
/// </summary>
public sealed class GitCommandTool
{
    private static readonly HashSet<string> AllowedSubcommands = new(StringComparer.Ordinal)
    {
        "status", "diff", "log", "branch", "add", "commit",
        "checkout", "push", "reset", "show", "fetch", "rev-parse", "remote",
    };

    private readonly IProcessRunner _processRunner;
    private readonly string _workspacePath;
    private readonly bool _allowPush;

    /// <summary>Initializes a new instance of the <see cref="GitCommandTool"/> class.</summary>
    /// <param name="processRunner">The process runner used to launch git.</param>
    /// <param name="workspacePath">The workspace directory git runs in.</param>
    /// <param name="allowPush">Whether the <c>push</c> subcommand is permitted.</param>
    public GitCommandTool(IProcessRunner processRunner, string workspacePath, bool allowPush)
    {
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
        _workspacePath = workspacePath ?? throw new ArgumentNullException(nameof(workspacePath));
        _allowPush = allowPush;
    }

    /// <summary>Runs a git subcommand in the workspace.</summary>
    /// <param name="subcommand">The git subcommand (for example <c>status</c>, <c>commit</c>, <c>push</c>).</param>
    /// <param name="arguments">Additional git arguments, space separated, or null.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The git command result.</returns>
    public async Task<GitToolResult> RunAsync(
        string subcommand,
        string? arguments = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(subcommand))
            return new GitToolResult(false, -1, null, "A git subcommand is required.");

        var normalized = subcommand.Trim();
        if (!AllowedSubcommands.Contains(normalized))
        {
            return new GitToolResult(
                false, -1, null,
                $"git subcommand '{normalized}' is not allowed. Allowed: {string.Join(", ", AllowedSubcommands.Order(StringComparer.Ordinal))}.");
        }

        var effectiveArguments = arguments;
        if (normalized == "push")
        {
            if (!_allowPush)
                return new GitToolResult(false, -1, null, "git push is disabled. A host must enable AllowGitPush to permit pushes.");

            var (ok, guardedArguments, error) = GuardPush(arguments);
            if (!ok)
                return new GitToolResult(false, -1, null, error);
            effectiveArguments = guardedArguments;
        }

        var commandLine = string.IsNullOrWhiteSpace(effectiveArguments)
            ? normalized
            : $"{normalized} {effectiveArguments.Trim()}";

        var result = await _processRunner.RunAsync(
            new ProcessRunRequest("git", commandLine, WorkingDirectory: _workspacePath),
            cancellationToken).ConfigureAwait(false);

        return new GitToolResult(result.ExitCode == 0, result.ExitCode, result.Stdout, result.Stderr);
    }

    /// <summary>
    /// Ensures a <c>push</c> targets the <c>origin</c> remote. The first non-flag token is treated as the remote;
    /// when absent, <c>origin</c> is appended. A non-<c>origin</c> remote is rejected.
    /// </summary>
    /// <param name="arguments">The caller-supplied push arguments.</param>
    /// <returns>A tuple of (accepted, guarded-arguments, rejection-reason).</returns>
    public static (bool Ok, string? Arguments, string? Error) GuardPush(string? arguments)
    {
        var tokens = (arguments ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // Reject URL or scp-style remotes anywhere in the arguments (e.g. https://host/repo, git@host:repo) so a
        // push can never be redirected to an arbitrary endpoint regardless of how the argument string is shaped.
        foreach (var token in tokens)
        {
            if (token.Contains("://", StringComparison.Ordinal) || IsScpLikeRemote(token))
            {
                return (false, null,
                    $"git push to a URL remote ('{token}') is not allowed; pushes are restricted to the 'origin' remote.");
            }
        }

        // The remote is the first non-flag token. When present it must be 'origin'; when absent, 'origin' is
        // appended so the push never relies on an ambient default remote.
        var remoteIndex = Array.FindIndex(tokens, static t => !t.StartsWith('-'));
        if (remoteIndex < 0)
        {
            var withOrigin = tokens.Append("origin");
            return (true, string.Join(' ', withOrigin), null);
        }

        if (!string.Equals(tokens[remoteIndex], "origin", StringComparison.Ordinal))
        {
            return (false, null,
                $"git push is restricted to the 'origin' remote; '{tokens[remoteIndex]}' is not allowed. Name 'origin' explicitly or omit the remote.");
        }

        return (true, arguments, null);
    }

    private static bool IsScpLikeRemote(string token)
    {
        // scp-style git remotes are user@host:path with no URL scheme. git treats a colon before the first slash
        // as the scp separator; mirror that so a Windows drive path (C:\...) is not misclassified as a remote.
        var at = token.IndexOf('@', StringComparison.Ordinal);
        var colon = token.IndexOf(':', StringComparison.Ordinal);
        if (at <= 0 || colon <= at)
            return false;

        var slash = token.IndexOf('/', StringComparison.Ordinal);
        return slash < 0 || slash > colon;
    }
}
