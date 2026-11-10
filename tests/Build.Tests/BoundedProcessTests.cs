// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics;

namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-009 (BUG-BUILD-007): verifies <see cref="BoundedProcess"/>, the one way Build.Tests runs a child process with
/// redirected output. Build-infrastructure test, accountable through TR only. The process cases start real <c>pwsh</c> children
/// (no mocks); each writes the process id it must clean up to a temporary file that the test removes.
/// </summary>
/// <remarks>2026-11-07, TR-MCP-NUKE-009; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the infrastructure half of this class; its Category=Integration case BuildTestsSources_ReadChildOutputOnlyThroughBoundedProcess and the helpers only it used (IsBuildOutput, FindRepositoryRoot) moved to the class of the same name in the integration-owned Build.IntegrationTests, which links BoundedProcess, so this infrastructure-owned project holds no integration case (slice S4, hostile validation finding R14-F4); the five remaining cases and their helpers are unchanged.</remarks>
public sealed class BoundedProcessTests
{
    /// <summary>
    /// TR-MCP-NUKE-009 AC-001: a child that exits while a descendant still holds its redirected output makes RunAsync fail within
    /// the drain period, naming the command and keeping what the child wrote, instead of waiting for the descendant. The pwsh child
    /// starts a second pwsh with <c>Start-Process -NoNewWindow</c>, which inherits the child's stdout and stderr as an MSBuild worker
    /// node started with node reuse does (the BUG-BUILD-007 root cause), records its process id, and exits; the descendant sleeps 30 s.
    /// </summary>
    [Fact]
    public async Task RunAsync_DescendantHoldsOutputAfterChildExits_FailsWithinDrainPeriod()
    {
        var pidFile = NewPidFile();
        var command = "$descendant = Start-Process -FilePath (Get-Process -Id $PID).Path -ArgumentList '-NoLogo','-NoProfile',"
            + "'-NonInteractive','-Command','Start-Sleep -Seconds 30' -NoNewWindow -PassThru; "
            + $"Set-Content -LiteralPath '{pidFile}' -Value $descendant.Id; 'child-exited'";
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var exception = await Assert.ThrowsAsync<IOException>(() => BoundedProcess.RunAsync(
                Pwsh(command), TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));

            Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(20), $"RunAsync waited {stopwatch.Elapsed} for the descendant.");
            Assert.Contains("pwsh", exception.Message, StringComparison.Ordinal);
            Assert.Contains("child-exited", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            KillRecorded(pidFile);
        }
    }

    /// <summary>
    /// TR-MCP-NUKE-009 AC-002: a child that runs past its exit timeout is killed with its process tree and RunAsync throws a
    /// <see cref="TimeoutException"/> naming the command. The pwsh child records its own process id and sleeps 60 s; the exit
    /// timeout is 10 s.
    /// </summary>
    [Fact]
    public async Task RunAsync_ChildRunsPastExitTimeout_KillsTreeAndThrowsTimeout()
    {
        var pidFile = NewPidFile();
        var command = $"Set-Content -LiteralPath '{pidFile}' -Value $PID; Start-Sleep -Seconds 60";
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var exception = await Assert.ThrowsAsync<TimeoutException>(() => BoundedProcess.RunAsync(
                Pwsh(command), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));

            Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(40), $"RunAsync returned after {stopwatch.Elapsed}.");
            Assert.Contains("pwsh", exception.Message, StringComparison.Ordinal);
            Assert.True(HasExited(pidFile), "The child outlived its exit timeout.");
        }
        finally
        {
            KillRecorded(pidFile);
        }
    }

    /// <summary>
    /// TR-MCP-NUKE-009 AC-003: when the caller cancels, RunAsync kills the child's process tree and stops waiting. The pwsh child
    /// records its own process id and sleeps 60 s; the caller cancels after 10 s.
    /// </summary>
    [Fact]
    public async Task RunAsync_CallerCancels_KillsTreeAndStopsWaiting()
    {
        var pidFile = NewPidFile();
        var command = $"Set-Content -LiteralPath '{pidFile}' -Value $PID; Start-Sleep -Seconds 60";
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cancellation.CancelAfter(TimeSpan.FromSeconds(10));
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => BoundedProcess.RunAsync(
                Pwsh(command), TimeSpan.FromMinutes(2), TimeSpan.FromSeconds(2), cancellation.Token));

            Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(40), $"RunAsync returned after {stopwatch.Elapsed}.");
            Assert.True(HasExited(pidFile), "The child outlived the cancellation.");
        }
        finally
        {
            KillRecorded(pidFile);
        }
    }

    /// <summary>
    /// TR-MCP-NUKE-009 AC-004: RunAsync drains standard output and standard error concurrently, so a child that writes 1 MiB to
    /// standard error (more than any pipe buffer holds) before writing to standard output completes. A reader that drains standard
    /// output to the end first would leave the child blocked on the full standard error pipe.
    /// </summary>
    [Fact]
    public async Task RunAsync_ChildFillsStandardErrorBeforeWritingOutput_Completes()
    {
        var result = await BoundedProcess.RunAsync(
            Pwsh("[Console]::Error.Write([string]::new([char]'e', 1048576)); [Console]::Out.Write('done')"),
            TimeSpan.FromMinutes(1),
            TimeSpan.FromSeconds(10),
            TestContext.Current.CancellationToken);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("done", result.Stdout.Trim());
        Assert.Equal(1048576, result.Stderr.Count(character => character == 'e'));
    }

    /// <summary>
    /// TR-MCP-NUKE-009 AC-005: dotnet children ignore persistent build servers, so no MSBuild worker node, MSBuild server, or
    /// compiler server outlives the child holding its redirected output. Pure start-info construction; no process starts.
    /// </summary>
    [Fact]
    public void Dotnet_AppendsDisableBuildServers()
    {
        var startInfo = BoundedProcess.Dotnet("repo-root", ["build", "project.csproj", "-c", "Debug"]);

        string[] expected = ["build", "project.csproj", "-c", "Debug", "--disable-build-servers"];
        Assert.Equal("dotnet", startInfo.FileName);
        Assert.Equal("repo-root", startInfo.WorkingDirectory);
        Assert.Equal(expected, startInfo.ArgumentList);
    }

    private static ProcessStartInfo Pwsh(string command)
    {
        var startInfo = new ProcessStartInfo("pwsh");
        startInfo.ArgumentList.Add("-NoLogo");
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-NonInteractive");
        startInfo.ArgumentList.Add("-Command");
        startInfo.ArgumentList.Add(command);
        return startInfo;
    }

    private static string NewPidFile() => Path.Combine(Path.GetTempPath(), $"bounded-process-{Guid.NewGuid():N}.pid");

    private static bool HasExited(string pidFile)
    {
        Assert.True(File.Exists(pidFile), "The child never recorded its process id.");
        try
        {
            using var process = Process.GetProcessById(int.Parse(File.ReadAllText(pidFile).Trim(), System.Globalization.CultureInfo.InvariantCulture));
            return process.HasExited;
        }
        catch (ArgumentException)
        {
            return true;
        }
    }

    private static void KillRecorded(string pidFile)
    {
        if (!File.Exists(pidFile))
        {
            return;
        }

        try
        {
            using var process = Process.GetProcessById(int.Parse(File.ReadAllText(pidFile).Trim(), System.Globalization.CultureInfo.InvariantCulture));
            process.Kill(entireProcessTree: true);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or FormatException)
        {
        }
        finally
        {
            File.Delete(pidFile);
        }
    }
}
