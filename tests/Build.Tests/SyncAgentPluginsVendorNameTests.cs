using System.Text.RegularExpressions;
using Xunit;

namespace NukeBuild.Tests;

/// <summary>
/// TEST-MCP-194: Verifies the Node plugin core vendor step uses the version-less stable tarball name and
/// guards the packed version against the package manifest, per TR-MCP-SYNC-001.
/// </summary>
/// <remarks>
/// Fixture: the repository sources build/Build.SyncAgentPlugins.cs and plugins/core/lib-node/package.json,
/// read from disk following the established BuildTargetTests idiom. Motivated by
/// triage-report-52e8098cd299475d9922098f00d818b6, where a hard-coded versioned tarball name
/// (sharpninja-mcpserver-plugin-core-0.1.0.tgz) mislabeled 0.2.0 content after a breaking-change bump.
/// </remarks>
public sealed class SyncAgentPluginsVendorNameTests
{
    private const string StableVendorFileName = "sharpninja-mcpserver-plugin-core.tgz";

    /// <summary>
    /// TEST-MCP-194: The vendor step names the stable version-less tarball, so consumer package.json
    /// references never break on a version bump.
    /// </summary>
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
            if (File.Exists(Path.Combine(directory.FullName, "McpServer.sln")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Unable to locate the repository root from the test output directory.");
    }
}
