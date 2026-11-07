using System.Text;
using QBrainAi.Support.Mcp.Services;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Services;

/// <summary>TEST-HANDOFF-002: handle containment, UNC normalization, and bounded reads.</summary>
/// <remarks>2026-11-07, TEST-HANDOFF-002; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Services/HandoffContainedFileReaderTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 2 Category=Integration cases moved unchanged with the class attributes; only the namespace changed.</remarks>

public sealed class HandoffContainedFileReaderTests
{
    /// <summary>
    /// P1-6 / P2-1: the reader opens with FileShare.Read, as HandoffContainedFileReader does. On Windows that share mode blocks
    /// writes and deletes while the reader holds the file. Linux and macOS have no mandatory share modes (BUG-BASELINE-004), so
    /// there a concurrent delete succeeds and the held handle keeps reading the original bytes.
    /// </summary>
    /// <remarks>Integration: opens a physical file and exercises native sharing and deletion semantics (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task ReadAsync_OpenShare_BlocksWritesAndDeletes()
    {
        var root = Path.Combine(Path.GetTempPath(), "handoff-reader", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "notes.md");
        await File.WriteAllTextAsync(path, "handoff", TestContext.Current.CancellationToken);
        try
        {
            await using var held = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
            if (OperatingSystem.IsWindows())
            {
                Assert.ThrowsAny<IOException>(() => new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.ReadWrite));
                Assert.ThrowsAny<IOException>(() => File.Delete(path));
            }
            else
            {
                File.Delete(path);
                Assert.False(File.Exists(path));
                using var reader = new StreamReader(held, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
                Assert.Equal("handoff", await reader.ReadToEndAsync(TestContext.Current.CancellationToken));
            }
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>P1-6: a long contained path must open and return its bytes; unsupported host configuration fails visibly.</summary>
    /// <remarks>Integration: creates and reads a physical long path (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task ReadAsync_LongContainedPath_Succeeds()
    {
        var root = Path.Combine(Path.GetTempPath(), "handoff-long", Guid.NewGuid().ToString("N"));
        var current = root;
        while (current.Length < 240)
            current = Path.Combine(current, "nested-directory-name");
        var path = Path.Combine(current, "notes.md");
        try
        {
            Directory.CreateDirectory(current);
            await File.WriteAllTextAsync(path, "long-path-handoff", TestContext.Current.CancellationToken);
            var read = await HandoffContainedFileReader.ReadAsync(root, path, TestContext.Current.CancellationToken);
            Assert.True(read.Success, read.Message);
            Assert.Equal("long-path-handoff", read.Text);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }
}

/// <summary>
/// TEST-HANDOFF-002: Windows final-path resolution of a handle. Windows-only, so an integration test on Windows
/// hosts without a silent return elsewhere (TR-MCP-USECASE-018 AC-006, TR-MCP-NUKE-019).
/// </summary>
/// <remarks>2026-11-07, TEST-HANDOFF-002; TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: moved whole with its 1 Windows-only Category=Integration case from QBrainAi.Support.Mcp.Tests (Services/HandoffContainedFileReaderTests.cs), whose unit classes stay there, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); only the namespace changed.</remarks>
[Trait("Category", "Integration")]
[Trait("Platform", "Windows")]
public sealed class HandoffContainedFileReaderWindowsTests
{
    /// <summary>P1-6: an unresolvable handle fails closed.</summary>
    [Fact]
    public void TryGetFinalPath_InvalidHandle_FailsClosedOnWindows()
    {
        using var handle = new SafeFileHandle(IntPtr.Zero, ownsHandle: false);
        Assert.False(HandoffContainedFileReader.TryGetFinalPath(handle, out var path));
        Assert.True(string.IsNullOrEmpty(path));
    }
}
