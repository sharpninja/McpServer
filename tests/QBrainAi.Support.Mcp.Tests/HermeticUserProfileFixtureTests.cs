using QBrainAi.Support.Mcp.Services;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests;

/// <summary>
/// Tests that <see cref="HermeticUserProfileFixture"/> is in effect for this assembly: marker agent-plugin version
/// resolution reads the fixture's sources, which resolve no plugin-root variable, offer no folder beside the workspace,
/// and point at an existing, empty directory under the system temp folder, never the real user profile.
/// </summary>
/// <remarks>
/// Requirement coverage: TR-MCP-WS-005 / FR-MCP-018 ac-1 (operator 2026-10-20: "Fix scans in PR 87
/// (Recommended)" and "Structural fix, round 4 (Recommended)"). Test data: the assembly fixture's profile and sources,
/// injected; negative controls feed the profile assertion an unset override, a directory outside the temp folder, and
/// a temp directory that holds a plugin cache, and prove the assertion rejects each (the source-level controls are in
/// <c>AgentPluginVersionSourcesTests</c>). No test changes the live override at run time, so it is checked to be the
/// fixture's own sources.
/// </remarks>
public sealed class HermeticUserProfileFixtureTests
{
    private readonly HermeticUserProfileFixture _fixture;

    /// <summary>Receives the assembly fixture.</summary>
    /// <param name="fixture">The assembly-wide hermetic profile.</param>
    public HermeticUserProfileFixtureTests(HermeticUserProfileFixture fixture) => _fixture = fixture;

    /// <summary>
    /// Asserts that <paramref name="profile"/> is a hermetic plugin-version profile: set, under
    /// <paramref name="tempRoot"/>, existing, and empty.
    /// </summary>
    private static void AssertHermetic(string? profile, string tempRoot)
    {
        Assert.False(string.IsNullOrWhiteSpace(profile));
        Assert.StartsWith(Path.GetFullPath(tempRoot), Path.GetFullPath(profile), StringComparison.Ordinal);
        Assert.True(Directory.Exists(profile));
        Assert.Empty(Directory.EnumerateFileSystemEntries(profile));
    }

    /// <summary>
    /// The fixture's profile is an existing, empty directory under the temp folder, and the live override is the
    /// fixture's sources, which pass the source hermeticity assertion, so plugin-version resolution never reads the
    /// environment, the machine or the real user profile.
    /// </summary>
    [Fact]
    public void AgentPluginVersionSourcesOverride_IsTheHermeticAssemblySources()
    {
        AssertHermetic(_fixture.Profile, Path.GetTempPath());

        var live = MarkerFileService.AgentPluginVersionSourcesOverride;
        Assert.Same(_fixture.Sources, live);
        QBrainAi.Support.Mcp.Tests.Services.AgentPluginVersionSourcesTests.AssertHermetic(live!);
        Assert.Equal(_fixture.Profile, live!.UserProfile);
    }

    /// <summary>
    /// Negative controls: an unset override, a profile outside the temp folder, and a temp profile that holds a
    /// plugin cache are each rejected by the hermeticity assertion, inside a passing harness.
    /// </summary>
    [Fact]
    public void HermeticityNegativeControls_AreRejectedInsidePassingHarness()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"mcp-hermetic-controls-{Guid.NewGuid():N}");
        var outside = Path.Combine(tempRoot, "outside");
        var temp = Path.Combine(tempRoot, "temp");
        var empty = Path.Combine(temp, "empty");
        var populated = Path.Combine(temp, "populated");
        Directory.CreateDirectory(outside);
        Directory.CreateDirectory(empty);
        Directory.CreateDirectory(Path.Combine(populated, ".claude", "plugins", "cache"));
        try
        {
            Assert.NotNull(Record.Exception(() => AssertHermetic(null, temp)));
            Assert.NotNull(Record.Exception(() => AssertHermetic(outside, temp)));
            Assert.NotNull(Record.Exception(() => AssertHermetic(populated, temp)));
            Assert.Null(Record.Exception(() => AssertHermetic(empty, temp)));
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}
