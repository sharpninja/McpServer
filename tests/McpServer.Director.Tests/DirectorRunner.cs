using System.Diagnostics;

namespace McpServer.Director.Tests;

/// <summary>
/// Helper that invokes the Director CLI executable via <c>dotnet exec</c>
/// with the working directory set to the repository root where the
/// <c>AGENTS-README-FIRST.yaml</c> marker file lives.
/// </summary>
internal static class DirectorRunner
{
    /// <summary>Repository root — working directory for all CLI invocations.</summary>
    internal static readonly string RepoRoot = FindRepoRoot();

    /// <summary>Path to the built <c>director.dll</c>.</summary>
    private static readonly string DirectorDll = Path.GetFullPath(
        Path.Combine(RepoRoot, "src", "McpServer.Director", "bin", "Debug", "net9.0", "director.dll"));

    /// <summary>Default per-command timeout in milliseconds.</summary>
    private const int DefaultTimeoutMs = 30_000;

    /// <summary>
    /// Runs the Director CLI with the given arguments and returns the exit code,
    /// captured stdout, and captured stderr.
    /// </summary>
    internal static async Task<CliResult> RunAsync(string args, int timeoutMs = DefaultTimeoutMs)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"exec \"{DirectorDll}\" {args}",
            WorkingDirectory = RepoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        // Ensure ANSI markup does not leak into captured output.
        psi.Environment["NO_COLOR"] = "1";

        using var process = Process.Start(psi)!;
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        using var cts = new CancellationTokenSource(timeoutMs);
        try
        {
            await process.WaitForExitAsync(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { /* best effort */ }
            throw new TimeoutException(
                $"Director command timed out after {timeoutMs}ms: director {args}");
        }

        var stdout = await stdoutTask.ConfigureAwait(false);
        var stderr = await stderrTask.ConfigureAwait(false);
        return new CliResult(process.ExitCode, stdout, stderr);
    }

    private static string FindRepoRoot()
    {
        // Walk up from the test assembly location to find the .sln file.
        var dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir, "McpServer.sln")))
                return dir;
            dir = Path.GetDirectoryName(dir);
        }

        // Fallback — assume standard repo layout relative to bin output.
        return Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
    }
}

/// <summary>Result of a Director CLI invocation.</summary>
internal sealed record CliResult(int ExitCode, string StdOut, string StdErr)
{
    /// <summary>Combined stdout + stderr for assertions.</summary>
    internal string AllOutput => StdOut + StdErr;
}
