using System.Diagnostics;

using McpServer.Common.AgentCli;

using Microsoft.Extensions.Logging.Abstractions;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// Verifies service-launched agent processes inherit user CLI install paths needed by triage agents.
/// </summary>
public sealed class ProcessEnvironmentServiceTests
{
    [Fact]
    public void ApplyRunAsEnvironment_IncludesGrokBinWhenProfileContainsGrokInstall()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var originalPublic = Environment.GetEnvironmentVariable("PUBLIC");
        var tempRoot = Path.Combine(Path.GetTempPath(), $"mcpserver-process-env-{Guid.NewGuid():N}");
        var userName = "mcp-grok-user";
        var publicPath = Path.Combine(tempRoot, "Public");
        var userProfile = Path.Combine(tempRoot, userName);
        var grokBin = Path.Combine(userProfile, ".grok", "bin");

        Directory.CreateDirectory(publicPath);
        Directory.CreateDirectory(grokBin);
        Directory.CreateDirectory(Path.Combine(userProfile, "AppData", "Local"));

        try
        {
            Environment.SetEnvironmentVariable("PUBLIC", publicPath);
            var service = new ProcessEnvironmentService(NullLogger<ProcessEnvironmentService>.Instance);
            var startInfo = new ProcessStartInfo
            {
                FileName = "grok",
                UseShellExecute = false,
            };

            service.ApplyRunAsEnvironment(startInfo, userName);

            Assert.True(startInfo.Environment.TryGetValue("PATH", out var path));
            Assert.Contains(grokBin, path, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Environment.SetEnvironmentVariable("PUBLIC", originalPublic);
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public void ResolveExecutable_FindsGrokFromInjectedUserPath()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var tempRoot = Path.Combine(Path.GetTempPath(), $"mcpserver-grok-bin-{Guid.NewGuid():N}");
        var grokBin = Path.Combine(tempRoot, ".grok", "bin");
        var grokExe = Path.Combine(grokBin, "grok.exe");

        Directory.CreateDirectory(grokBin);
        File.WriteAllText(grokExe, string.Empty);

        try
        {
            var service = new ProcessEnvironmentService(NullLogger<ProcessEnvironmentService>.Instance);
            var startInfo = new ProcessStartInfo
            {
                FileName = "grok",
                UseShellExecute = false,
            };
            startInfo.Environment["PATH"] = grokBin;

            var resolved = service.ResolveExecutable(startInfo, "grok");

            Assert.Equal(grokExe, resolved);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }
}
