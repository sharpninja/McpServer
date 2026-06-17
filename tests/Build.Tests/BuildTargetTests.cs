using Nuke.Common.IO;

namespace NukeBuild.Tests;

/// <summary>
/// TEST-NUKE-001: Verifies that the Nuke _build project compiles and the Build class
/// is defined with the expected targets. Since NukeBuild requires specific runtime
/// initialization (assembly name = "_build"), we verify via reflection rather than
/// direct instantiation.
/// </summary>
public sealed class BuildTargetTests
{
    private static readonly Type BuildType = typeof(Build);

    [Fact]
    public void Build_ExtendsNukeBuild()
    {
        Assert.True(BuildType.IsSubclassOf(typeof(Nuke.Common.NukeBuild)));
    }

    [Fact]
    public void Build_HasCompileTarget()
    {
        var prop = BuildType.GetProperty("Compile");
        Assert.NotNull(prop);
    }

    [Fact]
    public void Build_HasCleanTarget()
    {
        var prop = BuildType.GetProperty("Clean");
        Assert.NotNull(prop);
    }

    [Fact]
    public void Build_HasRestoreTarget()
    {
        var prop = BuildType.GetProperty("Restore");
        Assert.NotNull(prop);
    }

    [Fact]
    public void Build_HasConfigurationParameter()
    {
        var field = BuildType.GetField("Configuration");
        Assert.NotNull(field);
        Assert.Equal(typeof(string), field!.FieldType);
    }

    [Fact]
    public void Build_HasSolutionField()
    {
        var field = BuildType.GetField("Solution",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(field);
    }

    [Fact]
    public void Build_HasDirectoryProperties()
    {
        Assert.NotNull(BuildType.GetProperty("SourceDirectory"));
        Assert.NotNull(BuildType.GetProperty("TestsDirectory"));
        Assert.NotNull(BuildType.GetProperty("ArtifactsDirectory"));
        Assert.NotNull(BuildType.GetProperty("LocalPackagesDirectory"));
    }

    [Fact]
    public void Build_HasTestTarget()
    {
        var prop = BuildType.GetProperty("Test");
        Assert.NotNull(prop);
    }

    [Theory]
    [InlineData("BuildTrustedThirdParty")]
    [InlineData("PublishTrustedThirdParty")]
    [InlineData("UpdateTrustedThirdPartyService")]
    [InlineData("BuildAgent")]
    [InlineData("PublishAgent")]
    public void Build_HasTrustedThirdPartyAndAgentTargets(string targetName)
    {
        var prop = BuildType.GetProperty(targetName);
        Assert.NotNull(prop);
    }

    [Theory]
    [InlineData("TrustedThirdPartyServiceName")]
    [InlineData("TrustedThirdPartyInstallPath")]
    [InlineData("TrustedThirdPartyPort")]
    [InlineData("TrustedThirdPartyPublishSource")]
    public void Build_HasTrustedThirdPartyDeploymentParameters(string parameterName)
    {
        var field = BuildType.GetField(
            parameterName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(field);
    }

    [Fact]
    public void CopyBrainSlotRuntimeConfig_CopiesAssignmentFileToDeploymentConfigPath()
    {
        var repoRoot = FindRepositoryRoot();
        var destination = Path.Combine(Path.GetTempPath(), $"mcpserver-build-test-{Guid.NewGuid():N}");

        try
        {
            var copied = Build.CopyBrainSlotRuntimeConfig((AbsolutePath)repoRoot, destination);

            Assert.True(File.Exists(copied));
            Assert.EndsWith(
                Path.Combine("config", Build.BrainSlotConfigDirectoryName, Build.BrainSlotConfigFileName),
                copied,
                StringComparison.OrdinalIgnoreCase);
            Assert.Contains("slotId: brain-slot-arbiter-of-truth-grok-build", File.ReadAllText(copied), StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(destination))
                Directory.Delete(destination, true);
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName,
                "config",
                Build.BrainSlotConfigDirectoryName,
                Build.BrainSlotConfigFileName);
            if (File.Exists(candidate))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root containing brain-slot configuration.");
    }
}
