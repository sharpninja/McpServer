namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>
/// TEST-MCP-USECASE-019/020: Host-independent native containment evidence.
/// </summary>
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
