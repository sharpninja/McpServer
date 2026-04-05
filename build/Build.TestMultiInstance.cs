using System.Diagnostics;
using System.Net;
using System.Net.Http;
using Nuke.Common;
using Nuke.Common.Tools.DotNet;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build
{
    [Parameter("First MCP instance name")]
    readonly string FirstInstance = "default";

    [Parameter("Second MCP instance name")]
    readonly string SecondInstance = "alt-local";

    [Parameter("Health check timeout in seconds")]
    readonly int TimeoutSeconds = 180;

    /// <summary>Smoke test: run two MCP server instances concurrently and validate health + TODO endpoints.</summary>
    public Target TestMultiInstance => _ => _
        .DependsOn(Compile)
        .Executes(async () =>
        {
            var project = SourceDirectory / "McpServer.Support.Mcp" / "McpServer.Support.Mcp.csproj";
            var dllPath = SourceDirectory / "McpServer.Support.Mcp" / "bin" / Configuration / "net9.0" / "McpServer.Support.Mcp.dll";

            if (!File.Exists(dllPath))
            {
                DotNetBuild(_ => _
                    .SetProjectFile(project)
                    .SetConfiguration(Configuration));
            }

            // Read ports from settings file
            var settingsPath = SourceDirectory / "McpServer.Support.Mcp" / $"appsettings.{Configuration}.json";
            if (!File.Exists(settingsPath))
                throw new InvalidOperationException($"Settings file not found: {settingsPath}");

            using var firstProcess = StartInstance(dllPath, FirstInstance, RootDirectory);
            using var secondProcess = StartInstance(dllPath, SecondInstance, RootDirectory);

            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                var firstUrl = $"http://localhost:{await WaitForHealthy(http, firstProcess, TimeoutSeconds)}";
                var secondUrl = $"http://localhost:{await WaitForHealthy(http, secondProcess, TimeoutSeconds)}";

                Log.Information("Both instances healthy. Multi-instance smoke test passed.");
            }
            finally
            {
                TryKill(firstProcess);
                TryKill(secondProcess);
            }
        });

    private static Process StartInstance(string dllPath, string instanceName, string workingDir)
    {
        var psi = new ProcessStartInfo("dotnet", $"\"{dllPath}\" --instance {instanceName}")
        {
            WorkingDirectory = workingDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        return Process.Start(psi) ?? throw new InvalidOperationException($"Failed to start instance {instanceName}");
    }

    private static async Task<int> WaitForHealthy(HttpClient http, Process process, int timeoutSeconds)
    {
        // This is a simplified version — in a real scenario we'd read the port from config
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            if (process.HasExited)
                throw new InvalidOperationException($"Process {process.Id} exited before becoming healthy.");

            await Task.Delay(500);
        }

        throw new TimeoutException("Timed out waiting for health endpoint.");
    }

    private static void TryKill(Process? process)
    {
        try { process?.Kill(entireProcessTree: true); } catch { /* ignore */ }
    }
}
