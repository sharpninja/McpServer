namespace NukeBuild.Tests;

/// <summary>
/// TEST-NUKE-002: Verifies GitVersionBumper correctly parses and increments
/// the patch component of GitVersion.yml next-version field.
/// </summary>
public sealed class GitVersionBumperTests
{
    private const string SampleContent = """
        mode: ContinuousDelivery
        next-version: 0.2.85
        branches:
          main:
            increment: Patch
        """;

    [Fact]
    public void ParseVersion_ValidContent_ReturnsMajorMinorPatch()
    {
        var result = GitVersionBumper.ParseVersion(SampleContent);
        Assert.NotNull(result);
        Assert.Equal((0, 2, 85), result.Value);
    }

    [Fact]
    public void ParseVersion_NoNextVersion_ReturnsNull()
    {
        var result = GitVersionBumper.ParseVersion("mode: ContinuousDelivery\nbranches:\n  main:\n");
        Assert.Null(result);
    }

    [Fact]
    public void BumpPatch_ValidContent_IncrementsPatcn()
    {
        var result = GitVersionBumper.BumpPatch(SampleContent);
        Assert.NotNull(result);
        Assert.Equal("0.2.85", result.Value.OldVersion);
        Assert.Equal("0.2.86", result.Value.NewVersion);
        Assert.Contains("next-version: 0.2.86", result.Value.NewContent);
        Assert.DoesNotContain("next-version: 0.2.85", result.Value.NewContent);
    }

    [Fact]
    public void BumpPatch_NoNextVersion_ReturnsNull()
    {
        var result = GitVersionBumper.BumpPatch("mode: ContinuousDelivery");
        Assert.Null(result);
    }

    [Fact]
    public void BumpPatch_PreservesOtherContent()
    {
        var result = GitVersionBumper.BumpPatch(SampleContent);
        Assert.NotNull(result);
        Assert.Contains("mode: ContinuousDelivery", result.Value.NewContent);
        Assert.Contains("increment: Patch", result.Value.NewContent);
    }

    [Theory]
    [InlineData("next-version: 1.0.0", 1, 0, 0)]
    [InlineData("next-version: 10.20.300", 10, 20, 300)]
    [InlineData("next-version:  3.4.5", 3, 4, 5)]
    public void ParseVersion_VariousFormats_ParsesCorrectly(string content, int major, int minor, int patch)
    {
        var result = GitVersionBumper.ParseVersion(content);
        Assert.NotNull(result);
        Assert.Equal((major, minor, patch), result.Value);
    }
}
