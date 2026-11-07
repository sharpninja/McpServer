using System.Buffers.Binary;
using System.Text;
using Xunit;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.TestSupport;

/// <summary>
/// Binding on a Windows host: <see cref="WindowsJunction.Create"/> makes a real directory junction in the test process
/// that the file system resolves to its target. It creates folders and a file under the official temporary folder and
/// needs the Windows file system, so it is an integration test on Windows hosts (TR-MCP-USECASE-019 AC-008 as replaced
/// 2026-10-26T14:18:23; TR-MCP-NUKE-019).
/// </summary>
/// <remarks>2026-11-07, TR-MCP-USECASE-019 AC-008; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: moved whole with its 1 Windows-only Category=Integration case from QBrainAi.Support.Mcp.Tests (TestSupport/WindowsJunctionTests.cs), whose unit classes stay there, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); only the namespace changed.</remarks>
[Trait("Category", "Integration")]
[Trait("Platform", "Windows")]
public sealed class WindowsJunctionWindowsTests
{
    /// <summary>
    /// A junction created in the test process is a mount-point reparse point whose link target is the target folder,
    /// and a file read through it is the target's file.
    /// </summary>
    [Fact]
    public void Create_MakesAJunctionThatResolvesToItsTarget()
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("This integration test runs only in the Windows lane.");
        var root = Path.Combine(Path.GetTempPath(), "mcp-junction-" + Guid.NewGuid().ToString("N"));
        var target = Path.Combine(root, "target");
        var alias = Path.Combine(root, "alias");
        Directory.CreateDirectory(target);
        File.WriteAllText(Path.Combine(target, "document.md"), "through the junction");
        try
        {
            WindowsJunction.Create(alias, target);

            var info = new DirectoryInfo(alias);
            Assert.True(info.Attributes.HasFlag(FileAttributes.ReparsePoint));
            Assert.Equal(Path.GetFullPath(target), Path.GetFullPath(Assert.IsType<string>(info.LinkTarget)));
            Assert.Equal("through the junction", File.ReadAllText(Path.Combine(alias, "document.md")));
        }
        finally
        {
            if (Directory.Exists(alias))
                Directory.Delete(alias);
            Directory.Delete(root, recursive: true);
        }
    }
}
