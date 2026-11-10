using NSubstitute;
using QBrainAi.Client;

namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>BDP v4 step 2: the host contract proven against scripted data.</summary>
/// <remarks>2026-11-07, TEST-MCP-FED-PATH-001; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: unchanged; the contract it derives from moved into Storage/WorkspaceHostEnvironmentContract.cs beside this file, which the integration project links, and the real-host fixture moved to the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S7, hostile validation finding R14-F4).</remarks>
public sealed class WorkspaceHostEnvironmentMockDataTests : WorkspaceHostEnvironmentContractTests
{
    /// <inheritdoc />
    protected override WorkspacePathPlatform RunningPlatform => WorkspacePathPlatform.Windows;
    /// <inheritdoc />
    protected override string ExpectedCurrentDirectory => "C:/fixture/current";
    /// <inheritdoc />
    protected override string NativeSample => "C:/fixture/./repo";
    /// <inheritdoc />
    protected override string NativeSampleExpected => "C:/fixture/repo";

    /// <inheritdoc />
    protected override IWorkspaceHostEnvironment CreateHost()
    {
        var host = Substitute.For<IWorkspaceHostEnvironment>();
        host.Platform.Returns(RunningPlatform);
        host.CurrentDirectory.Returns(ExpectedCurrentDirectory);
        host.NormalizeNativePath(NativeSample).Returns(NativeSampleExpected);
        return host;
    }
}

/// <summary>Pure Linux-host scripted data preserves Linux cases without reading the machine's platform or directory.</summary>
/// <remarks>2026-11-07, TEST-MCP-FED-PATH-001; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: unchanged; the contract it derives from moved into Storage/WorkspaceHostEnvironmentContract.cs beside this file, which the integration project links, and the real-host fixture moved to the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S7, hostile validation finding R14-F4); this Linux scripted fixture stays a unit case.</remarks>
public sealed class WorkspaceHostEnvironmentLinuxMockDataTests : WorkspaceHostEnvironmentContractTests
{
    /// <inheritdoc />
    protected override WorkspacePathPlatform RunningPlatform => WorkspacePathPlatform.CaseSensitive;
    /// <inheritdoc />
    protected override string ExpectedCurrentDirectory => "/fixture/current";
    /// <inheritdoc />
    protected override string NativeSample => "/fixture/./repo";
    /// <inheritdoc />
    protected override string NativeSampleExpected => "/fixture/repo";
    /// <inheritdoc />
    protected override IWorkspaceHostEnvironment CreateHost()
    {
        var host = Substitute.For<IWorkspaceHostEnvironment>();
        host.Platform.Returns(RunningPlatform);
        host.CurrentDirectory.Returns(ExpectedCurrentDirectory);
        host.NormalizeNativePath(NativeSample).Returns(NativeSampleExpected);
        return host;
    }
}
