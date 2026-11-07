using System.Diagnostics;
using System.Text.Json;

namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-019 BLD-2 binding: real Linux processes prove that the logging worker outlives a short parent,
/// closes that parent's captured pipes and continues writing its own log. This is integration work: it reads
/// checked-in PowerShell tools and uses an owned temporary directory and real pwsh.exe processes.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Platform", "Linux")]
public sealed class IntegrationLaneDetachedWorkerTests
{
    /// <summary>A scripted server keeps emitting after its launching parent exits and its output reaches EOF.</summary>
    [Fact]
    public async Task LinuxWorker_ClosesParentPipesAndKeepsLogging()
    {
        if (!OperatingSystem.IsLinux()) throw new PlatformNotSupportedException("This integration binding requires Linux.");
        var root = Path.Combine(Path.GetTempPath(), "lane-detached-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var stop = Path.Combine(root, "stop");
        var identity = Path.Combine(root, "worker.json");
        var log = Path.Combine(root, "logs", "server.log");
        var server = Path.Combine(root, "fake-ollama");
        var producer = Path.Combine(root, "producer.ps1");
        var repository = FindRepositoryRoot();
        var worker = Path.Combine(repository, "build", "scripts", "Start-OllamaServer.ps1");
        try
        {
            File.WriteAllText(server, $$"""
                #!/usr/bin/env pwsh.exe
                [CmdletBinding()]
                param([string]$Command)
                $ErrorActionPreference='Stop'
                if($Command -ne 'serve') { throw 'Expected serve command.' }
                while(-not (Test-Path -LiteralPath {{Quote(stop)}})) {
                    Write-Output 'owned-worker-alive'
                    Start-Sleep -Milliseconds 25
                }
                """ + Environment.NewLine);
            File.SetUnixFileMode(server, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            File.WriteAllText(producer, $$"""
                $ErrorActionPreference='Stop'
                $assembly=[Reflection.Assembly]::LoadFrom({{Quote(typeof(IntegrationLaneExecution).Assembly.Location)}})
                $launcher=$assembly.GetType('OllamaDetachedLaunch')
                $method=$launcher.GetMethod('StartLinux',[Reflection.BindingFlags]'Static,NonPublic')
                $workerId=[int]$method.Invoke($null,@({{Quote(server)}},{{Quote(log)}},{{Quote(worker)}}))
                $workerProcess=Get-Process -Id $workerId
                @{Id=$workerId;StartedTicks=$workerProcess.StartTime.Ticks} | ConvertTo-Json | Set-Content -LiteralPath {{Quote(identity)}}
                Write-Output 'producer-exited'
                """);
            var check = PowerShell(Path.Combine(repository, "tests", "Build.Tests", "Fixtures", "Check-PowerShellFixtures.ps1"),
                "-ProducerPath", producer, "-ServerPath", server);
            var analysis = await BoundedProcess.RunAsync(check, TimeSpan.FromMinutes(1), TestContext.Current.CancellationToken);
            Assert.True(analysis.ExitCode == 0, analysis.Stdout + analysis.Stderr);

            var result = await BoundedProcess.RunAsync(PowerShell(producer), TimeSpan.FromMinutes(1),
                TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            Assert.True(result.ExitCode == 0, result.Stdout + result.Stderr);
            Assert.Contains("producer-exited", result.Stdout, StringComparison.Ordinal);
            await UntilAsync(() => File.Exists(log) && new FileInfo(log).Length > 0);
            var firstLength = new FileInfo(log).Length;
            await UntilAsync(() => new FileInfo(log).Length > firstLength);
            Assert.Contains("owned-worker-alive", File.ReadAllText(log), StringComparison.Ordinal);
        }
        finally
        {
            File.WriteAllText(stop, "stop");
            if (File.Exists(identity))
            {
                using var document = JsonDocument.Parse(File.ReadAllText(identity));
                var workerId = document.RootElement.GetProperty("Id").GetInt32();
                var ticks = document.RootElement.GetProperty("StartedTicks").GetInt64();
                try
                {
                    using var process = Process.GetProcessById(workerId);
                    // PID reuse cannot authorize killing another process; match the exact instance recorded by our parent.
                    if (!process.HasExited && process.StartTime.Ticks == ticks) process.Kill(entireProcessTree: true);
                }
                catch (ArgumentException) { /* The owned worker already exited. */ }
            }
            Directory.Delete(root, recursive: true);
        }
    }

    private static ProcessStartInfo PowerShell(string script, params string[] arguments)
    {
        var info = new ProcessStartInfo("pwsh.exe");
        foreach (var argument in new[] { "-NoLogo", "-NoProfile", "-NonInteractive", "-File", script }.Concat(arguments))
            info.ArgumentList.Add(argument);
        return info;
    }

    private static async Task UntilAsync(Func<bool> ready)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        while (!ready()) await Task.Delay(25, timeout.Token);
    }

    private static string Quote(string value) => "'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "QBrainAi.sln"))) return current.FullName;
            current = current.Parent;
        }
        throw new DirectoryNotFoundException("The integration binding requires the build checkout.");
    }
}
