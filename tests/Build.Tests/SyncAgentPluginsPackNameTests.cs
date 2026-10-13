// SPDX-License-Identifier: Apache-2.0
using System.Text.Json;

namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-SYNC-001 (BUG-BUILD-010): verifies that the SyncAgentPlugins Node vendor step expects the tarball name npm pack actually
/// produces, derived from the plugin core manifest's name and version, so renaming the package does not stop the sync. Build-infrastructure
/// test, accountable through TR only. Fixtures: temporary directories holding a package.json written for each case; no mocks, no npm.
/// </summary>
public sealed class SyncAgentPluginsPackNameTests
{
    /// <summary>
    /// TR-MCP-SYNC-001 (BUG-BUILD-010): a scoped package name loses its leading @ and its / becomes -, which is how npm pack names the
    /// renamed core (@qbrainai/qbrain-ai-plugin-core 0.2.0 packs as qbrainai-qbrain-ai-plugin-core-0.2.0.tgz).
    /// </summary>
    [Fact]
    public void ExpectedPackFileName_ScopedPackage_FollowsNpmNaming()
    {
        var root = WriteManifest(new { name = "@qbrainai/qbrain-ai-plugin-core", version = "0.2.0" });
        try
        {
            Assert.Equal("qbrainai-qbrain-ai-plugin-core-0.2.0.tgz", Build.GetExpectedNodeCorePackFileName(root));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>
    /// TR-MCP-SYNC-001 (BUG-BUILD-010): an unscoped package name is used as is, followed by the manifest version.
    /// </summary>
    [Fact]
    public void ExpectedPackFileName_UnscopedPackage_FollowsNpmNaming()
    {
        var root = WriteManifest(new { name = "plain-core", version = "1.2.3" });
        try
        {
            Assert.Equal("plain-core-1.2.3.tgz", Build.GetExpectedNodeCorePackFileName(root));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>
    /// TR-MCP-SYNC-001 (BUG-BUILD-010): a manifest without a name cannot be packed, so the expected name is refused instead of guessed.
    /// </summary>
    [Fact]
    public void ExpectedPackFileName_ManifestWithoutName_Throws()
    {
        var root = WriteManifest(new { version = "0.2.0" });
        try
        {
            Assert.Throws<InvalidOperationException>(() => Build.GetExpectedNodeCorePackFileName(root));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string WriteManifest(object manifest)
    {
        var root = Path.Combine(Path.GetTempPath(), "sync-pack-name-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "package.json"), JsonSerializer.Serialize(manifest));
        return root;
    }
}
