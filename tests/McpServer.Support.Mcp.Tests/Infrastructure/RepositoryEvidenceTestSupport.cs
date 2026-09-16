
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;


namespace McpServer.Support.Mcp.Tests.Infrastructure;

/// <summary>
/// Provides hermetic repository-root resolution and bounded process execution for evidence tests.
/// </summary>
internal static partial class RepositoryEvidenceTestSupport
{
    private static readonly TimeSpan s_defaultProcessTimeout = TimeSpan.FromSeconds(30);

    private static readonly string? s_buildRepositoryRoot =
        typeof(RepositoryEvidenceTestSupport).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(
                attribute => string.Equals(
                    attribute.Key,
                    "McpRepositoryRoot",
                    StringComparison.Ordinal))
            ?.Value;

    /// <summary>
    /// Resolves and validates an explicit repository root, or discovers the checkout from immutable
    /// build-time source provenance before considering process-relative locations.
    /// </summary>
    /// <returns>The validated absolute repository root.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when an explicit candidate is invalid or no checkout can be discovered.
    /// </exception>
    internal static string ResolveRepositoryRoot()
    {
        var configured = Environment.GetEnvironmentVariable("MCP_REPOSITORY_ROOT");
        if (!string.IsNullOrWhiteSpace(configured))
            return ValidateRepositoryRoot(configured, "Configured");

        if (!string.IsNullOrWhiteSpace(s_buildRepositoryRoot))
            return ValidateRepositoryRoot(s_buildRepositoryRoot, "Build-metadata");

        var startingPoints = new[]
        {
            Path.GetDirectoryName(typeof(RepositoryEvidenceTestSupport).Assembly.Location),
            AppContext.BaseDirectory,
            Directory.GetCurrentDirectory(),
        };
        foreach (var startingPoint in startingPoints.Where(
                     candidate => !string.IsNullOrWhiteSpace(candidate)))
        {
            DirectoryInfo? current;
            try
            {
                current = new DirectoryInfo(Path.GetFullPath(startingPoint!));
            }
            catch (Exception exception) when (
                exception is ArgumentException or NotSupportedException or PathTooLongException)
            {
                continue;
            }

            while (current is not null)
            {
                if (IsRepositoryRoot(current.FullName))
                    return current.FullName;

                current = current.Parent;
            }
        }

        throw new InvalidOperationException(
            "Repository root was not found from build metadata, assembly location, " +
            "application base directory, or current directory.");
    }

    /// <summary>Validates an explicit root without silently falling back to another checkout.</summary>
    private static string ValidateRepositoryRoot(string candidate, string source)
    {
        var root = Path.GetFullPath(candidate);
        if (!IsRepositoryRoot(root))
        {
            throw new InvalidOperationException(
                $"{source} repository root is invalid: {root}");
        }

        return root;
    }

    /// <summary>Checks the complete set of markers that identify this repository.</summary>
    private static bool IsRepositoryRoot(string root)
    {
        var gitMarker = Path.Combine(root, ".git");
        return (File.Exists(gitMarker) || Directory.Exists(gitMarker)) &&
               File.Exists(Path.Combine(root, "McpServer.sln")) &&
               File.Exists(Path.Combine(root, "Directory.Build.props")) &&
               File.Exists(
                   Path.Combine(
                       root,
                       "tests",
                       "McpServer.Support.Mcp.Tests",
                       "McpServer.Support.Mcp.Tests.csproj"));
    }

    /// <summary>
    /// Runs Git with binary-safe standard output and a bounded timeout.
    /// </summary>
    /// <param name="workingDirectory">Validated repository working directory.</param>
    /// <param name="arguments">Git arguments.</param>
    /// <param name="standardInput">Optional standard input.</param>
    /// <returns>The process result, including raw standard-output bytes.</returns>
    internal static RepositoryProcessResult RunGit(
        string workingDirectory,
        IReadOnlyList<string> arguments,
        string? standardInput = null) =>
        RunProcess(
            workingDirectory,
            "git",
            arguments,
            standardInput,
            s_defaultProcessTimeout);

    /// <summary>
    /// Runs Git and returns UTF-8 output, throwing when Git reports an error.
    /// </summary>
    /// <param name="workingDirectory">Validated repository working directory.</param>
    /// <param name="arguments">Git arguments.</param>
    /// <param name="standardInput">Optional standard input.</param>
    /// <returns>Git standard output decoded as UTF-8.</returns>
    internal static string RunGitText(
        string workingDirectory,
        IReadOnlyList<string> arguments,
        string? standardInput = null)
    {
        var result = RunGit(workingDirectory, arguments, standardInput);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"git {string.Join(' ', arguments)} failed: {result.StandardError}");
        }

        return Encoding.UTF8.GetString(result.StandardOutput);
    }

    /// <summary>
    /// Runs a process while concurrently draining both redirected output streams and enforcing a
    /// bounded timeout that kills the complete process tree.
    /// </summary>
    /// <param name="workingDirectory">Existing process working directory.</param>
    /// <param name="fileName">Executable name or path.</param>
    /// <param name="arguments">Process arguments.</param>
    /// <param name="standardInput">Optional standard input.</param>
    /// <param name="timeout">Positive finite timeout.</param>
    /// <returns>The completed process result.</returns>
    /// <exception cref="TimeoutException">Thrown after an unresponsive process is killed.</exception>
    internal static RepositoryProcessResult RunProcess(
        string workingDirectory,
        string fileName,
        IReadOnlyList<string> arguments,
        string? standardInput,
        TimeSpan timeout) =>
        RunProcessAsync(
                workingDirectory,
                fileName,
                arguments,
                standardInput,
                timeout)
            .GetAwaiter()
            .GetResult();

    private static async Task<RepositoryProcessResult> RunProcessAsync(
        string workingDirectory,
        string fileName,
        IReadOnlyList<string> arguments,
        string? standardInput,
        TimeSpan timeout)
    {
        if (!Directory.Exists(workingDirectory))
            throw new DirectoryNotFoundException(workingDirectory);
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("Process file name is required.", nameof(fileName));
        if (timeout <= TimeSpan.Zero || timeout == Timeout.InfiniteTimeSpan)
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "Timeout must be positive and finite.");

        if (OperatingSystem.IsWindows())
        {
            return await WindowsSuspendedProcess
                .RunAsync(
                    workingDirectory,
                    fileName,
                    arguments,
                    standardInput,
                    timeout)
                .ConfigureAwait(false);
        }

        var (startInfo, isolatedPosixGroup) = CreateProcessStartInfo(
            workingDirectory,
            fileName,
            arguments,
            standardInput is not null);
        using var processTree = new PosixProcessGroupLease(isolatedPosixGroup);
        using var process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true,
        };
        if (!process.Start())
            throw new InvalidOperationException($"Unable to start process: {fileName}");
        processTree.Attach(process);

        using var standardOutput = new MemoryStream();
        var outputTask = process.StandardOutput.BaseStream.CopyToAsync(standardOutput);
        var errorTask = process.StandardError.ReadToEndAsync();
        using var timeoutSource = new CancellationTokenSource(timeout);
        var inputTask = WriteStandardInputAsync(
            process,
            standardInput,
            timeoutSource.Token);

        try
        {
            await process
                .WaitForExitAsync(timeoutSource.Token)
                .ConfigureAwait(false);
            await inputTask.WaitAsync(timeoutSource.Token).ConfigureAwait(false);
            await Task.WhenAll(outputTask, errorTask)
                .WaitAsync(timeoutSource.Token)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested)
        {
            processTree.Kill(process);
            await AwaitTerminationAndDrainsAsync(
                    process,
                    outputTask,
                    errorTask)
                .ConfigureAwait(false);
            throw new TimeoutException(
                $"Process '{fileName}' exceeded the {timeout} timeout.");
        }

        return new RepositoryProcessResult(
            process.ExitCode,
            standardOutput.ToArray(),
            await errorTask.ConfigureAwait(false));
    }

    /// <summary>
    /// Creates a redirected process start and, on POSIX hosts, places it behind a session leader so
    /// descendants remain addressable after the direct child exits.
    /// </summary>
    private static (ProcessStartInfo StartInfo, bool IsolatedPosixGroup) CreateProcessStartInfo(
        string workingDirectory,
        string fileName,
        IReadOnlyList<string> arguments,
        bool redirectStandardInput)
    {
        var setSid = ResolveSetSidExecutable();
        var startInfo = new ProcessStartInfo(setSid ?? fileName)
        {
            WorkingDirectory = Path.GetFullPath(workingDirectory),
            RedirectStandardInput = redirectStandardInput,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        if (setSid is not null)
        {
            startInfo.ArgumentList.Add("--wait");
            startInfo.ArgumentList.Add(fileName);
        }

        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);

        return (startInfo, setSid is not null);
    }

    /// <summary>Finds the standard POSIX session launcher used for stable process-group cleanup.</summary>
    private static string? ResolveSetSidExecutable()
    {
        if (OperatingSystem.IsWindows())
            return null;

        return new[] { "/usr/bin/setsid", "/bin/setsid" }
            .FirstOrDefault(File.Exists);
    }

    private static async Task WriteStandardInputAsync(
        Process process,
        string? standardInput,
        CancellationToken cancellationToken)
    {
        if (standardInput is null)
            return;

        await process.StandardInput
            .WriteAsync(standardInput.AsMemory(), cancellationToken)
            .ConfigureAwait(false);
        await process.StandardInput.FlushAsync(cancellationToken).ConfigureAwait(false);
        process.StandardInput.Close();
    }

    /// <summary>
    /// Tracks the POSIX session leader created by <c>setsid --wait</c> so descendants remain
    /// addressable after the direct child exits.
    /// </summary>
    private sealed class PosixProcessGroupLease(bool isolatedPosixGroup) : IDisposable
    {
        private const int PosixSigKill = 9;
        private int? _posixGroupId;

        /// <summary>Records the session leader that already owns the child at process creation.</summary>
        internal void Attach(Process process)
        {
            if (isolatedPosixGroup)
                _posixGroupId = process.Id;
        }

        /// <summary>Terminates the complete POSIX process group and then the direct process.</summary>
        internal void Kill(Process process)
        {
            if (_posixGroupId is int groupId)
                _ = KillPosixProcessGroup(-groupId, PosixSigKill);

            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }

        [DllImport("libc", EntryPoint = "kill", SetLastError = true)]
        private static extern int KillPosixProcessGroup(int processId, int signal);
    }


    private static async Task AwaitTerminationAndDrainsAsync(
        Process process,
        Task outputTask,
        Task<string> errorTask)
    {
        using var cleanupSource = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        try
        {
            await process
                .WaitForExitAsync(cleanupSource.Token)
                .ConfigureAwait(false);
            await Task.WhenAll(outputTask, errorTask)
                .WaitAsync(cleanupSource.Token)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
    }
}

/// <summary>
/// Captures a bounded evidence-process result without decoding binary standard output.
/// </summary>
/// <param name="ExitCode">Process exit code.</param>
/// <param name="StandardOutput">Raw standard-output bytes.</param>
/// <param name="StandardError">Decoded standard-error text.</param>
internal sealed record RepositoryProcessResult(
    int ExitCode,
    byte[] StandardOutput,
    string StandardError);
