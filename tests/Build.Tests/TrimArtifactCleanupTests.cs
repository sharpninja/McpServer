// SPDX-License-Identifier: Apache-2.0
namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-010 (BUG-BUILD-009): verifies that the trim-analysis cleanup removes its artifacts root on every OS. Build-infrastructure
/// test, accountable through TR only. Uses a temporary directory tree with nested files shaped like a publish output; no mocks.
/// </summary>
/// <remarks>2026-11-07, TR-MCP-NUKE-010 AC-001; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the cleanup under test moved unchanged from TrimAnalysisWarningInventoryTests, which slice S4 moved to the integration-owned Build.IntegrationTests, into the linked <see cref="TrimArtifactCleanup"/>; this case calls it there and is otherwise unchanged (hostile validation finding R14-F4).</remarks>
public sealed class TrimArtifactCleanupTests
{
    /// <summary>
    /// TR-MCP-NUKE-010 AC-001: <see cref="TrimArtifactCleanup.TryDeleteArtifacts"/> removes a populated artifacts root.
    /// On Windows the delete runs in a detached <c>cmd.exe</c> so an antivirus scan cannot block the test host, so the test polls
    /// for up to 30 s before asserting.
    /// </summary>
    /// <remarks>2026-11-07, TR-MCP-NUKE-010 AC-001; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: calls the cleanup in its new home, TrimArtifactCleanup (slice S4); the assertions are unchanged.</remarks>
    [Fact]
    public async Task TryDeleteArtifacts_PopulatedRoot_IsRemoved()
    {
        var root = Path.Combine(Path.GetTempPath(), "mcpserver-trim-cleanup-test", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "publish", "runtimes"));
        await File.WriteAllTextAsync(Path.Combine(root, "publish", "app.dll"), "app", TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(root, "publish", "runtimes", "native.so"), "native", TestContext.Current.CancellationToken);

        try
        {
            TrimArtifactCleanup.TryDeleteArtifacts(root);

            var deadline = DateTime.UtcNow.AddSeconds(30);
            while (Directory.Exists(root) && DateTime.UtcNow < deadline)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(200), TestContext.Current.CancellationToken);
            }

            Assert.False(Directory.Exists(root), $"TryDeleteArtifacts left {root} in place.");
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
