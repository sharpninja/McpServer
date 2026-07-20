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

    /// <summary>
    /// TR-MCP-SVC-002: verifies <see cref="ProcessEnvironmentService.ResolveExecutable"/> skips any PATH
    /// directory ending in <c>Microsoft\WindowsApps</c>. Fixture: a temp tree containing a zero-byte
    /// <c>ngrok.exe</c> App Execution Alias stub under <c>AppData\Local\Microsoft\WindowsApps</c> and a
    /// real <c>ngrok.exe</c> under a portable install directory, with the alias directory listed first on PATH.
    /// The alias stub is the shape that raises Win32Exception 1920 under a service account.
    /// </summary>
    [Fact]
    public void ResolveExecutable_SkipsMicrosoftWindowsAppsAliasDirectory()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var tempRoot = Path.Combine(Path.GetTempPath(), $"mcpserver-winapps-alias-{Guid.NewGuid():N}");
        var aliasDir = Path.Combine(tempRoot, "AppData", "Local", "Microsoft", "WindowsApps");
        var portableDir = Path.Combine(tempRoot, "tools", "ngrok");
        var aliasStub = Path.Combine(aliasDir, "ngrok.exe");
        var portableExe = Path.Combine(portableDir, "ngrok.exe");

        Directory.CreateDirectory(aliasDir);
        Directory.CreateDirectory(portableDir);
        File.WriteAllBytes(aliasStub, []);
        File.WriteAllText(portableExe, "real");

        try
        {
            var service = new ProcessEnvironmentService(NullLogger<ProcessEnvironmentService>.Instance);
            var startInfo = new ProcessStartInfo { FileName = "ngrok", UseShellExecute = false };
            startInfo.Environment["PATH"] = $"{aliasDir};{portableDir}";

            var resolved = service.ResolveExecutable(startInfo, "ngrok");

            Assert.Equal(portableExe, resolved);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    /// <summary>
    /// TR-MCP-SVC-002: verifies the App-Execution-Alias guard in
    /// <see cref="ProcessEnvironmentService.ResolveExecutable"/> does NOT match the genuine
    /// <c>Program Files\WindowsApps</c> MSIX package root, which holds real launchable executables.
    /// Fixture: a temp tree with a single PATH entry <c>Program Files\WindowsApps\Contoso.Ngrok_1.0.0_x64</c>
    /// containing a real <c>ngrok.exe</c>.
    /// </summary>
    [Fact]
    public void ResolveExecutable_DoesNotSkipProgramFilesWindowsAppsPackageRoot()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var tempRoot = Path.Combine(Path.GetTempPath(), $"mcpserver-msix-root-{Guid.NewGuid():N}");
        var msixDir = Path.Combine(tempRoot, "Program Files", "WindowsApps", "Contoso.Ngrok_1.0.0_x64");
        var msixExe = Path.Combine(msixDir, "ngrok.exe");

        Directory.CreateDirectory(msixDir);
        File.WriteAllText(msixExe, "real");

        try
        {
            var service = new ProcessEnvironmentService(NullLogger<ProcessEnvironmentService>.Instance);
            var startInfo = new ProcessStartInfo { FileName = "ngrok", UseShellExecute = false };
            startInfo.Environment["PATH"] = msixDir;

            var resolved = service.ResolveExecutable(startInfo, "ngrok");

            Assert.Equal(msixExe, resolved);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }
}
