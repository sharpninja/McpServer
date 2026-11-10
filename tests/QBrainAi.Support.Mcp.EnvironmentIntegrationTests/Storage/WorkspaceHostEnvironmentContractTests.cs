using NSubstitute;
using QBrainAi.Client;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Storage;

/// <summary>BDP v4 step 3: the host contract against <see cref="SystemWorkspaceHostEnvironment"/>.</summary>
/// <remarks>2026-11-07, TEST-MCP-FED-PATH-001; TR-MCP-FED-PATH-001; TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: moved whole with its 3 inherited Category=Integration cases from QBrainAi.Support.Mcp.Tests (Storage/WorkspaceHostEnvironmentContractTests.cs), whose scripted fixtures stay there, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); the contract it derives from is Storage/WorkspaceHostEnvironmentContract.cs of QBrainAi.Support.Mcp.Tests, linked into this project (DRY); only the namespace changed.</remarks>
[Trait("Category", "Integration")]
public sealed class WorkspaceHostEnvironmentRealTests : WorkspaceHostEnvironmentContractTests
{
    /// <inheritdoc />
    protected override WorkspacePathPlatform RunningPlatform => OperatingSystem.IsWindows() ? WorkspacePathPlatform.Windows : WorkspacePathPlatform.CaseSensitive;
    /// <inheritdoc />
    protected override string ExpectedCurrentDirectory => Environment.CurrentDirectory;
    /// <inheritdoc />
    protected override string NativeSample => OperatingSystem.IsWindows() ? @"C:\svc\.\repo" : "/srv/./repo";
    /// <inheritdoc />
    protected override string NativeSampleExpected => OperatingSystem.IsWindows() ? "C:/svc/repo" : "/srv/repo";

    /// <inheritdoc />
    protected override IWorkspaceHostEnvironment CreateHost() => SystemWorkspaceHostEnvironment.Instance;
}
