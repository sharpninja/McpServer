using System.Security.Cryptography;
using System.Text.Json;
using QBrainAi.Support.Mcp.Services;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

public sealed class WindowsServiceDeploymentGuardTests : IDisposable
{
    private readonly string _tempDirectory;

    public WindowsServiceDeploymentGuardTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "mcp-service-guard-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void HasDeploymentManifest_WhenManifestExists_ReturnsTrue()
    {
        WriteManifest(Array.Empty<ExecutableHashEntry>());

        Assert.True(WindowsServiceDeploymentGuard.HasDeploymentManifest(_tempDirectory));
    }

    [Fact]
    public void HasDeploymentManifest_WhenManifestMissing_ReturnsFalse()
    {
        Assert.False(WindowsServiceDeploymentGuard.HasDeploymentManifest(_tempDirectory));
    }

    [Fact]
    public void EnsureApprovedDeployment_MissingManifest_ThrowsAndLogsClearMessage()
    {
        File.WriteAllText(Path.Combine(_tempDirectory, "appsettings.yaml"), "Logging:\n  LogLevel:\n    Default: Information\n");
        File.WriteAllText(Path.Combine(_tempDirectory, "QBrainAi.Support.Mcp.exe"), "fake exe");
        var failures = new List<string>();

        var exception = Assert.Throws<InvalidOperationException>(
            () => WindowsServiceDeploymentGuard.EnsureApprovedDeployment(_tempDirectory, failures.Add));

        Assert.Equal(
            "Windows service deployment is missing .mcpservice-deployment.json. Redeploy with scripts\\Update-McpService.ps1.",
            exception.Message);
        Assert.Equal([exception.Message], failures);
    }

    [Fact]
    public void EnsureApprovedDeployment_BadExecutableHash_ThrowsAndLogsExecutableName()
    {
        var exePath = Path.Combine(_tempDirectory, "QBrainAi.Support.Mcp.exe");
        File.WriteAllText(Path.Combine(_tempDirectory, "appsettings.yaml"), "Logging:\n  LogLevel:\n    Default: Information\n");
        File.WriteAllText(exePath, "fake exe");
        WriteManifest(
            new[]
            {
                new ExecutableHashEntry("QBrainAi.Support.Mcp.exe", new string('0', 64)),
            });
        var failures = new List<string>();

        var exception = Assert.Throws<InvalidOperationException>(
            () => WindowsServiceDeploymentGuard.EnsureApprovedDeployment(_tempDirectory, failures.Add));

        Assert.Equal(
            "Windows service deployment manifest hash mismatch for 'QBrainAi.Support.Mcp.exe'. Redeploy with scripts\\Update-McpService.ps1.",
            exception.Message);
        Assert.Equal([exception.Message], failures);
    }

    [Fact]
    public void EnsureApprovedDeployment_ValidManifest_SucceedsWithoutLogging()
    {
        var exePath = Path.Combine(_tempDirectory, "QBrainAi.Support.Mcp.exe");
        var launcherPath = Path.Combine(_tempDirectory, "QBrainAi.Launcher.exe");
        File.WriteAllText(Path.Combine(_tempDirectory, "appsettings.yaml"), "Logging:\n  LogLevel:\n    Default: Information\n");
        File.WriteAllText(exePath, "fake exe");
        File.WriteAllText(launcherPath, "fake launcher");
        WriteManifest(
            new[]
            {
                new ExecutableHashEntry("QBrainAi.Launcher.exe", ComputeSha256(launcherPath)),
                new ExecutableHashEntry("QBrainAi.Support.Mcp.exe", ComputeSha256(exePath)),
            });
        var failures = new List<string>();

        WindowsServiceDeploymentGuard.EnsureApprovedDeployment(_tempDirectory, failures.Add);

        Assert.Empty(failures);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
            Directory.Delete(_tempDirectory, recursive: true);
    }

    private void WriteManifest(IEnumerable<ExecutableHashEntry> entries)
    {
        var manifestPath = Path.Combine(_tempDirectory, ".mcpservice-deployment.json");
        var manifest = new
        {
            schemaVersion = 1,
            generatedUtc = DateTime.UtcNow.ToString("O"),
            generatedBy = @"scripts\Update-McpService.ps1",
            operation = "test",
            serviceName = "QBrainAi",
            executable = "QBrainAi.Support.Mcp.exe",
            port = 7147,
            executableHashes = entries.Select(static entry => new { name = entry.Name, sha256 = entry.Sha256 }).ToArray(),
        };

        File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest));
    }

    private static string ComputeSha256(string path) =>
        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();

    private sealed record ExecutableHashEntry(string Name, string Sha256);
}
