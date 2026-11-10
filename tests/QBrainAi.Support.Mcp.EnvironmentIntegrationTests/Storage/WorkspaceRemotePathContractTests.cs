using QBrainAi.Client;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Storage;

/// <summary>
/// BDP v4 step 3: the remote-path contract against <see cref="WorkspaceIdentityPath"/>, which resolves
/// through the Windows filesystem: an integration test on Windows hosts (TR-MCP-USECASE-018 AC-006).
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-PATHNORM-001; TR-MCP-PATHNORM-001; TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: moved whole with its inherited Windows-only Category=Integration cases from QBrainAi.Support.Mcp.Tests (Storage/WorkspaceRemotePathContractTests.cs), whose scripted fixture stays there, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); the contract it derives from is Storage/WorkspaceRemotePathContract.cs of QBrainAi.Support.Mcp.Tests, linked into this project (DRY); only the namespace changed.</remarks>
[Trait("Category", "Integration")]
[Trait("Platform", "Windows")]
public sealed class WorkspaceRemotePathRealTests : WorkspaceRemotePathContractTests
{
    /// <inheritdoc />
    protected override async Task<string> ResolveWindowsAsync(string path)
    {
        var result = await WorkspaceIdentityPath.ResolveAsync(
                path,
                WorkspacePathPlatform.Windows,
                TimeSpan.FromSeconds(5),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        return result.NormalizedPath;
    }
}
