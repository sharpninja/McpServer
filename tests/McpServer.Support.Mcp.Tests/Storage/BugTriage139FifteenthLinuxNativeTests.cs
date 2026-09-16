namespace McpServer.Support.Mcp.Tests.Storage;

/// <summary>
/// TEST-MCP-USECASE-019/020: Linux-only native identity, FUSE, and SQLite boundary evidence.
/// </summary>
[Collection(nameof(WorkspaceIdentityNativeProcessStateCollection))]
public sealed class BugTriage139FifteenthLinuxNativeTests
{
    /// <summary>
    /// A real non-root FUSE wait is rejected within the physical-identity bound with truthful
    /// worker accounting.
    /// </summary>
    [Fact]
    public Task WorkspaceIdentity_NativeCancellationWindow_CompletesWithoutWorkerLeak() =>
        BugTriage139FifteenthNativeScenarios.AssertLinuxIdentityFuseCancellationAsync();

    /// <summary>
    /// A real non-root FUSE SQLite data source is rejected before a worker or handle is created.
    /// </summary>
    [Fact]
    public Task SqliteOpen_PlatformNativePreHandleWait_IsBoundedWithoutLeaks() =>
        BugTriage139FifteenthNativeScenarios.AssertLinuxSqliteFuseOpenAsync();

    /// <summary>
    /// A provider lifecycle failure remains exact when caller cancellation coincides with the
    /// Linux worker boundary.
    /// </summary>
    [Fact]
    public Task SqliteOpen_KernelWaitThenIndependentDisposalFailure_PreservesExactException() =>
        BugTriage139FifteenthNativeScenarios.AssertLinuxSqliteIndependentFailureAsync();
}
