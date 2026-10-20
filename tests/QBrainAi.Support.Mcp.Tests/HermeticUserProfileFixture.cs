using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Tests;
using Xunit;

[assembly: AssemblyFixture(typeof(HermeticUserProfileFixture))]

namespace QBrainAi.Support.Mcp.Tests;

/// <summary>
/// TR-MCP-WS-005 / AC-TR-MCP-WS-005-001: gives marker agent-plugin version resolution test-owned sources for the whole
/// test assembly: no plugin-root environment variable, no folder beside the workspace, and an empty temporary profile,
/// so no test in this assembly reads the environment, the machine or the plugin caches of the user who runs it
/// (operator 2026-10-20: "Fix scans in PR 87 (Recommended)" and "Structural fix, round 4 (Recommended)"; unit tests
/// may not depend on the environment).
/// </summary>
/// <remarks>
/// Test data: one empty directory under the system temp folder, created when the assembly starts and deleted when
/// it ends. The sources are set once here and never changed at run time; tests that exercise resolution with other
/// data pass their own <see cref="AgentPluginVersionSources"/> to the resolution they call.
/// </remarks>
public sealed class HermeticUserProfileFixture : IDisposable
{
    /// <summary>Creates the empty profile and installs the hermetic sources.</summary>
    public HermeticUserProfileFixture()
    {
        Profile = Path.Combine(Path.GetTempPath(), $"mcp-hermetic-profile-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Profile);
        Sources = new AgentPluginVersionSources(_ => null, (_, _) => null, Profile);
        MarkerFileService.AgentPluginVersionSourcesOverride = Sources;
    }

    /// <summary>The empty temporary profile used for the whole assembly.</summary>
    public string Profile { get; }

    /// <summary>The hermetic sources installed for the whole assembly.</summary>
    internal AgentPluginVersionSources Sources { get; }

    /// <summary>Clears the override when it still holds these sources, and deletes the profile.</summary>
    public void Dispose()
    {
        if (ReferenceEquals(MarkerFileService.AgentPluginVersionSourcesOverride, Sources))
            MarkerFileService.AgentPluginVersionSourcesOverride = null;
        if (Directory.Exists(Profile))
            Directory.Delete(Profile, recursive: true);
    }
}
