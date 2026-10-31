namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>
/// TEST-MCP-USECASE-019/020: Linux-only native identity, FUSE, and SQLite boundary evidence.
/// </summary>
[Collection(nameof(WorkspaceIdentityNativeProcessStateCollection))]
[Trait("Category", "Integration")]
[Trait("Platform", "Linux")]
public sealed class BugTriage139FifteenthLinuxNativeTests
{
    /// <summary>
    /// A real non-root FUSE wait is rejected within the physical-identity bound with truthful
    /// worker accounting.
    /// </summary>
    /// <remarks>Integration: executes the real Linux FUSE or kernel-wait fixture and verifies native worker cleanup (TR-MCP-USECASE-018 AC-006).</remarks>
    [Fact]
    public Task WorkspaceIdentity_NativeCancellationWindow_CompletesWithoutWorkerLeak() =>
        BugTriage139FifteenthNativeScenarios.AssertLinuxIdentityFuseCancellationAsync();

    /// <summary>
    /// A real non-root FUSE SQLite data source is rejected before a worker or handle is created.
    /// </summary>
    /// <remarks>Integration: executes the real Linux FUSE or kernel-wait fixture and verifies native worker cleanup (TR-MCP-USECASE-018 AC-006).</remarks>
    [Fact]
    public Task SqliteOpen_PlatformNativePreHandleWait_IsBoundedWithoutLeaks() =>
        BugTriage139FifteenthNativeScenarios.AssertLinuxSqliteFuseOpenAsync();

    /// <summary>
    /// A provider lifecycle failure remains exact when caller cancellation coincides with the
    /// Linux worker boundary.
    /// </summary>
    /// <remarks>Integration: executes the real Linux FUSE or kernel-wait fixture and verifies native worker cleanup (TR-MCP-USECASE-018 AC-006).</remarks>
    [Fact]
    public Task SqliteOpen_KernelWaitThenIndependentDisposalFailure_PreservesExactException() =>
        BugTriage139FifteenthNativeScenarios.AssertLinuxSqliteIndependentFailureAsync();
}
