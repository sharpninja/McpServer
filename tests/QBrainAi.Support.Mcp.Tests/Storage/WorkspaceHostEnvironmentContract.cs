using NSubstitute;
using QBrainAi.Client;

namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>
/// TEST-MCP-FED-PATH-001 / TR-MCP-FED-PATH-001: contract for the process
/// <see cref="IWorkspaceHostEnvironment"/>: it reports the running OS, the process working
/// directory, and canonical host-native normalization. BDP v4 (plan section 2a):
/// <c>WorkspaceHostEnvironmentMockDataTests</c> then <c>WorkspaceHostEnvironmentRealTests</c>.
/// Scripted Windows and Linux hosts run as unit cases using explicit expectations. Only the real host binding
/// reads the running platform and directory, so only that binding is Integration (TR-MCP-USECASE-018 AC-006).
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-FED-PATH-001; TR-MCP-FED-PATH-001; TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the contract moved unchanged from Storage/WorkspaceHostEnvironmentContractTests.cs into this file, which QBrainAi.Support.Mcp.Tests compiles and QBrainAi.Support.Mcp.EnvironmentIntegrationTests links, because its two scripted fixtures stay unit cases in QBrainAi.Support.Mcp.Tests while its real-host fixture moved to the integration-owned project (slice S7, hostile validation finding R14-F4); one definition for both projects (DRY); the crefs to the fixtures are now text, since each fixture is in one project only.</remarks>
public abstract class WorkspaceHostEnvironmentContractTests
{
    /// <summary>Explicit expected platform for this adapter.</summary>
    protected abstract WorkspacePathPlatform RunningPlatform { get; }

    /// <summary>Explicit expected working directory; only the native binding reads the process.</summary>
    protected abstract string ExpectedCurrentDirectory { get; }

    /// <summary>A host-native path with a dot segment for this runner.</summary>
    protected abstract string NativeSample { get; }

    /// <summary>Expected canonical form of <see cref="NativeSample"/>.</summary>
    protected abstract string NativeSampleExpected { get; }

    /// <summary>Creates the host environment under test.</summary>
    protected abstract IWorkspaceHostEnvironment CreateHost();

    /// <summary>The host reports the platform of the running process.</summary>
    [Fact]
    public void Platform_IsRunningPlatform()
    {
        Assert.Equal(RunningPlatform, CreateHost().Platform);
    }

    /// <summary>The host reports the process working directory.</summary>
    [Fact]
    public void CurrentDirectory_IsProcessWorkingDirectory()
    {
        Assert.Equal(ExpectedCurrentDirectory, CreateHost().CurrentDirectory);
    }

    /// <summary>Host-native normalization collapses dot segments into the canonical form.</summary>
    [Fact]
    public void NormalizeNativePath_CollapsesToCanonicalForm()
    {
        Assert.Equal(NativeSampleExpected, CreateHost().NormalizeNativePath(NativeSample));
    }
}
