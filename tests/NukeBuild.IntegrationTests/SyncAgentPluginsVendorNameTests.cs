using System.Text.RegularExpressions;
using Xunit;

namespace NukeBuild.IntegrationTests;

/// <summary>
/// TEST-MCP-194: Verifies the Node plugin core vendor step uses the version-less stable tarball name and
/// guards the packed version against the package manifest, per TR-MCP-SYNC-001.
/// </summary>
/// <remarks>
/// <para>
/// Fixture: the repository sources build/Build.SyncAgentPlugins.cs and plugins/core/lib-node/package.json,
/// read from disk following the established BuildTargetTests idiom. Motivated by
/// triage-report-52e8098cd299475d9922098f00d818b6, where a hard-coded versioned tarball name
/// (sharpninja-mcpserver-plugin-core-0.1.0.tgz) mislabeled 0.2.0 content after a breaking-change bump.
/// </para>
/// <para>2026-11-07, TEST-MCP-194; TR-MCP-SYNC-001; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: moved whole and unchanged from Build.Tests into the integration-owned Build.IntegrationTests (root namespace NukeBuild.IntegrationTests), so the infrastructure-owned Build.Tests holds no Category=Integration case (slice S4, hostile validation finding R14-F4; operator 2026-11-06: "Move them (Recommended)"); only the namespace changed and the earlier remarks are kept as the first paragraph.</para>
/// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: Build.IntegrationTests, the project the entry above names, is now NukeBuild.IntegrationTests, renamed to its root namespace (operator 2026-11-07: "Project names and root namespaces for the project should match, so fix the project name."); the class and its cases are unchanged.</para>
/// </remarks>
public sealed class SyncAgentPluginsVendorNameTests
{
    private const string StableVendorFileName = "sharpninja-mcpserver-plugin-core.tgz";

    /// <summary>
    /// TEST-MCP-194: The vendor step names the stable version-less tarball, so consumer package.json
    /// references never break on a version bump.
    /// </summary>
    /// <remarks>TR-MCP-NUKE-019: integration because this case resolves or reads the physical checkout.</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void SyncAgentPlugins_VendorStep_UsesStableVersionlessName()
    {
        var source = ReadSyncAgentPluginsSource();

        Assert.Contains(StableVendorFileName, source, System.StringComparison.Ordinal);
    }

    /// <summary>
    /// TEST-MCP-194: No versioned vendor tarball literal exists in the sync source, so a hard-coded
    /// version cannot silently drift from package.json again.
    /// </summary>
    /// <remarks>TR-MCP-NUKE-019: integration because this case resolves or reads the physical checkout.</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void SyncAgentPlugins_VendorStep_HasNoVersionedTarballLiteral()
    {
        var source = ReadSyncAgentPluginsSource();

        var versioned = Regex.Matches(source, @"sharpninja-mcpserver-plugin-core-\d+\.\d+\.\d+\.tgz");
        Assert.Empty(versioned);
    }

    /// <summary>
    /// TEST-MCP-194: The vendor step validates the packed tarball version against
    /// plugins/core/lib-node/package.json, so pack-versus-manifest drift fails the sync loudly.
    /// </summary>
    /// <remarks>TR-MCP-NUKE-019: integration because this case resolves or reads the physical checkout.</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void SyncAgentPlugins_VendorStep_AssertsPackedVersionAgainstPackageJson()
    {
        var source = ReadSyncAgentPluginsSource();

        Assert.Contains("package.json", source, System.StringComparison.Ordinal);
        Assert.Contains("ReadNodeCorePackageVersion", source, System.StringComparison.Ordinal);
    }

    private static string ReadSyncAgentPluginsSource()
    {
        var repoRoot = FindRepositoryRoot();
        return File.ReadAllText(Path.Combine(repoRoot, "build", "Build.SyncAgentPlugins.cs"));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "QBrainAi.sln")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Unable to locate the repository root from the test output directory.");
    }
}
