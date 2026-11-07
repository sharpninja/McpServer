namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Storage;

/// <summary>
/// TEST-MCP-USECASE-019/020: Host-independent native containment evidence.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-USECASE-019; TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: moved whole from QBrainAi.Support.Mcp.Tests (Storage/BugTriage139FifteenthSharedTests.cs) into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests together with BugTriage139FifteenthNativeScenarios and the harness that finds it by reflection in its own assembly (BugTriage139FifteenthHarnessTests), so the unit-owned project holds only unit cases (slice S8, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); only the namespace changed.</remarks>
[Collection(nameof(WorkspaceIdentityNativeProcessStateCollection))]
[Trait("Category", "Integration")]
public sealed class BugTriage139FifteenthSharedTests
{
    /// <summary>
    /// Nested restoration creates searchable directories, reproduces the content, and preserves
    /// the no-follow containment boundary.
    /// </summary>
    /// <remarks>Integration: restores physical nested files and their native permissions through the contained-filesystem fixture (TR-MCP-USECASE-018 AC-006).</remarks>
    [Fact]
    public Task ContainedRestoration_NestedDirectoriesRestoreContentsModeAndContainment() =>
        BugTriage139FifteenthNativeScenarios.AssertContainedRestorationAsync();
}
