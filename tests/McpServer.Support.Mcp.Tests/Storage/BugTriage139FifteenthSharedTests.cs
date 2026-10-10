namespace McpServer.Support.Mcp.Tests.Storage;

/// <summary>
/// TEST-MCP-USECASE-019/020: Host-independent native containment evidence.
/// </summary>
[Collection(nameof(WorkspaceIdentityNativeProcessStateCollection))]
public sealed class BugTriage139FifteenthSharedTests
{
    /// <summary>
    /// Nested restoration creates searchable directories, reproduces the content, and preserves
    /// the no-follow containment boundary.
    /// </summary>
    [Fact]
    public Task ContainedRestoration_NestedDirectoriesRestoreContentsModeAndContainment() =>
        BugTriage139FifteenthNativeScenarios.AssertContainedRestorationAsync();
}
