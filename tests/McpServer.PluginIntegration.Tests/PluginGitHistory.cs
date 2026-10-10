using System.Diagnostics;
using Xunit;

namespace McpServer.PluginIntegration.Tests;

/// <summary>
/// TEST-MCP-PLUGININT-001 AC5: git identity checks for P19/P20 receipts.
/// A receipt SHA must be a real commit that is an ancestor of HEAD (including HEAD).
/// HEAD equality is not required: recapturing native suites is a separate P19/P20 closeout.
/// </summary>
public static class PluginGitHistory
{
    /// <summary>
    /// Asserts <paramref name="sha"/> is a 40-character git commit that is an ancestor of HEAD
    /// in <paramref name="repositoryRoot"/>.
    /// </summary>
    /// <param name="repositoryRoot">McpServer repository root.</param>
    /// <param name="sha">Recorded receipt SHA.</param>
    public static void AssertShaIsAncestorOfHead(string repositoryRoot, string sha)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(sha);
        var normalized = sha.Trim().ToLowerInvariant();
        Assert.Matches("^[0-9a-f]{40}$", normalized);

        var catExit = RunGit(repositoryRoot, ["cat-file", "-t", normalized], out var objectType, out var catError);
        Assert.True(
            catExit == 0 && string.Equals(objectType.Trim(), "commit", StringComparison.OrdinalIgnoreCase),
            "Receipt SHA is not a git commit object: " + normalized + " " + catError);

        var ancestorExit = RunGit(
            repositoryRoot,
            ["merge-base", "--is-ancestor", normalized, "HEAD"],
            out _,
            out var ancestorError);
        Assert.True(
            ancestorExit == 0,
            "Receipt SHA " + normalized + " is not an ancestor of HEAD (unrelated or not on this history). " + ancestorError);
    }

    /// <summary>
    /// Returns <c>git rev-parse HEAD</c> as a lowercase 40-character SHA.
    /// </summary>
    /// <param name="repositoryRoot">McpServer repository root.</param>
    /// <returns>HEAD SHA.</returns>
    public static string ReadHead(string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        var exit = RunGit(repositoryRoot, ["rev-parse", "HEAD"], out var output, out var error);
        var head = output.Trim().ToLowerInvariant();
        if (exit != 0 || head.Length != 40)
        {
            throw new InvalidOperationException("git rev-parse HEAD failed: " + error);
        }

        return head;
    }

    private static int RunGit(string repositoryRoot, IReadOnlyList<string> arguments, out string stdout, out string stderr)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = repositoryRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var argument in arguments)
        {
            psi.ArgumentList.Add(argument);
        }

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start git.");
        stdout = process.StandardOutput.ReadToEnd();
        stderr = process.StandardError.ReadToEnd();
        if (!process.WaitForExit(30_000))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException("git timed out: " + string.Join(' ', arguments));
        }

        return process.ExitCode;
    }
}
