using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Tests;
using Xunit;

[assembly: AssemblyFixture(typeof(HermeticUserProfileFixture))]

namespace QBrainAi.Support.Mcp.Tests;

/// <summary>
/// TR-MCP-WS-005 / AC-TR-MCP-WS-005-001: points marker agent-plugin version resolution at an empty temporary
/// profile for the whole test assembly, so no test in this assembly reads the plugin caches of the user who runs
/// it (operator 2026-10-20: "Fix scans in PR 87 (Recommended)"; unit tests may not depend on the environment).
/// </summary>
/// <remarks>
/// Test data: one empty directory under the system temp folder, created when the assembly starts and deleted when
/// it ends. Tests that need a populated profile set <see cref="MarkerFileService.AgentPluginUserProfileOverride"/>
/// themselves and restore the previous value afterwards, so the value is never the real profile.
/// </remarks>
public sealed class HermeticUserProfileFixture : IDisposable
{
    /// <summary>Creates the empty profile and points plugin-version resolution at it.</summary>
    public HermeticUserProfileFixture()
    {
        Profile = Path.Combine(Path.GetTempPath(), $"mcp-hermetic-profile-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Profile);
        MarkerFileService.AgentPluginUserProfileOverride = Profile;
    }

    /// <summary>The empty temporary profile used for the whole assembly.</summary>
    public string Profile { get; }

    /// <summary>Clears the override when it still holds this profile, and deletes the profile.</summary>
    public void Dispose()
    {
        if (string.Equals(MarkerFileService.AgentPluginUserProfileOverride, Profile, StringComparison.Ordinal))
            MarkerFileService.AgentPluginUserProfileOverride = null;
        if (Directory.Exists(Profile))
            Directory.Delete(Profile, recursive: true);
    }
}
