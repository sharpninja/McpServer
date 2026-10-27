// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics;
using System.Text;

namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-009 (BUG-BUILD-007): the one way the Build.Tests infrastructure suite runs a child process with redirected output.
/// It bounds the wait for the child to exit and, once the child has exited, the wait for its output to close; drains standard
/// output and standard error concurrently; kills the child's process tree on timeout or cancellation; and starts dotnet children
/// with <c>--disable-build-servers</c>. Without these bounds a descendant that inherits the child's pipes, such as an MSBuild worker
/// node started with node reuse, holds them open after the child exits, and the suite waits for an end of stream that never comes.
/// </summary>
internal static class BoundedProcess
{
    private const int CapturedTailLength = 4000;

    /// <summary>TR-MCP-NUKE-009 AC-001: how long to wait for redirected output to close after the child exits.</summary>
    internal static readonly TimeSpan DefaultDrainTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// TR-MCP-NUKE-009 AC-005: a <c>dotnet</c> start info for <paramref name="arguments"/> that ignores persistent build servers, so
    /// no MSBuild worker node, MSBuild server, or compiler server outlives the child holding its redirected output.
    /// </summary>
    internal static ProcessStartInfo Dotnet(string workingDirectory, IEnumerable<string> arguments)
    {
        var startInfo = new ProcessStartInfo("dotnet") { WorkingDirectory = workingDirectory };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        startInfo.ArgumentList.Add("--disable-build-servers");
        return startInfo;
    }

    /// <summary>Synchronous <see cref="RunAsync(ProcessStartInfo, TimeSpan, CancellationToken)"/> for synchronous fixtures.</summary>
    internal static BoundedProcessResult Run(ProcessStartInfo startInfo, TimeSpan exitTimeout, CancellationToken cancellationToken) =>
        RunAsync(startInfo, exitTimeout, DefaultDrainTimeout, cancellationToken).GetAwaiter().GetResult();

    /// <summary>Runs the child with the default drain period, <see cref="DefaultDrainTimeout"/>.</summary>
    internal static Task<BoundedProcessResult> RunAsync(ProcessStartInfo startInfo, TimeSpan exitTimeout, CancellationToken cancellationToken) =>
        RunAsync(startInfo, exitTimeout, DefaultDrainTimeout, cancellationToken);

    /// <summary>
    /// TR-MCP-NUKE-009 AC-001..AC-004: starts the child with redirected output, drains standard output and standard error
    /// concurrently, and returns the exit code with the complete output.
    /// </summary>
    /// <exception cref="TimeoutException">The child ran past <paramref name="exitTimeout"/>; its process tree was killed.</exception>
    /// <exception cref="OperationCanceledException">The caller cancelled; a still-running child's process tree was killed.</exception>
    /// <exception cref="IOException">The child exited, but its output was still open <paramref name="drainTimeout"/> later.</exception>
    internal static async Task<BoundedProcessResult> RunAsync(
        ProcessStartInfo startInfo,
        TimeSpan exitTimeout,
        TimeSpan drainTimeout,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(startInfo);
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        var command = Describe(startInfo);
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        using var abandon = new CancellationTokenSource();
        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Failed to start {command}.");
        var output = Task.WhenAll(
            PumpAsync(process.StandardOutput, stdout, abandon.Token),
            PumpAsync(process.StandardError, stderr, abandon.Token));

        using (var exit = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
        {
            exit.CancelAfter(exitTimeout);
            try
            {
                await process.WaitForExitAsync(exit.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                await KillTreeAsync(process).ConfigureAwait(false);
                await abandon.CancelAsync().ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                throw new TimeoutException(
                    $"{command} did not exit within {exitTimeout}; its process tree was killed.{Captured(stdout, stderr)}");
            }
        }

        // AC-001: the child has exited, but its output closes only when every holder of the pipes' write ends has closed them.
        try
        {
            await output.WaitAsync(drainTimeout, cancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            await abandon.CancelAsync().ConfigureAwait(false);
            throw new IOException(
                $"{command} exited with code {process.ExitCode}, but its redirected output was still open {drainTimeout} later: a "
                + $"descendant process inherited the pipes, so the captured output may be incomplete.{Captured(stdout, stderr)}");
        }
        catch (OperationCanceledException)
        {
            await abandon.CancelAsync().ConfigureAwait(false);
            throw;
        }

        return new BoundedProcessResult(process.ExitCode, Snapshot(stdout), Snapshot(stderr));
    }

    /// <summary>
    /// Copies one redirected stream into <paramref name="sink"/> until it closes. A read blocked on a pipe that a descendant holds
    /// open cannot be interrupted, so an abandoned pump simply finishes whenever the last writer closes the pipe.
    /// </summary>
    private static async Task PumpAsync(StreamReader reader, StringBuilder sink, CancellationToken abandoned)
    {
        var buffer = new char[4096];
        try
        {
            int read;
            while ((read = await reader.ReadAsync(buffer.AsMemory(), abandoned).ConfigureAwait(false)) > 0)
            {
                lock (sink)
                {
                    sink.Append(buffer, 0, read);
                }
            }
        }
        catch (Exception exception) when (abandoned.IsCancellationRequested
            && exception is OperationCanceledException or ObjectDisposedException or IOException)
        {
            // Abandoned after a timeout, a cancellation, or output held open; what was read is already in the sink.
        }
    }

    /// <summary>TR-MCP-NUKE-009 AC-002, AC-003: kills the child and its descendants, then waits briefly for the kill to land.</summary>
    private static async Task KillTreeAsync(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            // The child exited between the timeout and the kill.
        }

        using var reaped = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        try
        {
            await process.WaitForExitAsync(reaped.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Killed but not yet reaped; the caller still gets the timeout or the cancellation.
        }
    }

    private static string Describe(ProcessStartInfo startInfo)
    {
        if (startInfo.ArgumentList.Count > 0)
        {
            return $"{startInfo.FileName} {string.Join(' ', startInfo.ArgumentList)}";
        }

        return string.IsNullOrEmpty(startInfo.Arguments) ? startInfo.FileName : $"{startInfo.FileName} {startInfo.Arguments}";
    }

    private static string Snapshot(StringBuilder sink)
    {
        lock (sink)
        {
            return sink.ToString();
        }
    }

    private static string Captured(StringBuilder stdout, StringBuilder stderr) =>
        $"{Environment.NewLine}stdout:{Environment.NewLine}{Tail(Snapshot(stdout))}"
        + $"{Environment.NewLine}stderr:{Environment.NewLine}{Tail(Snapshot(stderr))}";

    private static string Tail(string text) => text.Length <= CapturedTailLength ? text : "..." + text[^CapturedTailLength..];
}

/// <summary>TR-MCP-NUKE-009: a finished child's exit code and its complete redirected output.</summary>
/// <param name="ExitCode">The child's exit code.</param>
/// <param name="Stdout">Everything the child wrote to standard output.</param>
/// <param name="Stderr">Everything the child wrote to standard error.</param>
internal sealed record BoundedProcessResult(int ExitCode, string Stdout, string Stderr);
