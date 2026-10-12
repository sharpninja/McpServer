namespace QBrainAi.Support.Mcp.Tests.Storage;

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
    public Task WorkspaceIdentityNativeCancellationWindowCompletesWithoutWorkerLeak() =>
        BugTriage139FifteenthNativeScenarios.AssertLinuxIdentityFuseCancellationAsync();

    /// <summary>
    /// A real non-root FUSE SQLite data source is rejected before a worker or handle is created.
    /// </summary>
    [Fact]
    public Task SqliteOpenPlatformNativePreHandleWaitIsBoundedWithoutLeaks() =>
        BugTriage139FifteenthNativeScenarios.AssertLinuxSqliteFuseOpenAsync();

    /// <summary>
    /// A provider lifecycle failure remains exact when caller cancellation coincides with the
    /// Linux worker boundary.
    /// </summary>
    [Fact]
    public Task SqliteOpenKernelWaitThenIndependentDisposalFailurePreservesExactException() =>
        BugTriage139FifteenthNativeScenarios.AssertLinuxSqliteIndependentFailureAsync();
}
