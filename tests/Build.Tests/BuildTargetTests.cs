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
}
