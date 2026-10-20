using QBrainAi.Support.Mcp.Services;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests;

/// <summary>
/// Tests that <see cref="HermeticUserProfileFixture"/> is in effect for this assembly: marker agent-plugin version
/// resolution points at an existing, empty directory under the system temp folder, never an unset value that
/// would fall back to the real user profile.
/// </summary>
/// <remarks>
/// Requirement coverage: TR-MCP-WS-005 / AC-TR-MCP-WS-005-001 (operator 2026-10-20: "Fix scans in PR 87
/// (Recommended)"). Test data: the assembly fixture's profile, injected; negative controls feed the hermeticity
/// assertion an unset override, a directory outside the temp folder, and a temp directory that holds a plugin cache,
/// and prove the assertion rejects each. The live override is only checked to be set and under the temp folder:
/// a test class running in parallel may hold its own temporary profile there and delete it when it finishes.
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
    /// The fixture's profile is an existing, empty directory under the temp folder, and the live override is set
    /// and lies under the temp folder, so plugin-version resolution never falls back to the real user profile.
    /// </summary>
    [Fact]
    public void AgentPluginUserProfileOverride_IsAnEmptyTemporaryProfile()
    {
        AssertHermetic(_fixture.Profile, Path.GetTempPath());

        // Read once: another test class may be inside its own hermetic scope, whose folder it deletes afterwards.
        var live = MarkerFileService.AgentPluginUserProfileOverride;
        Assert.False(string.IsNullOrWhiteSpace(live));
        Assert.StartsWith(Path.GetFullPath(Path.GetTempPath()), Path.GetFullPath(live), StringComparison.Ordinal);
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
