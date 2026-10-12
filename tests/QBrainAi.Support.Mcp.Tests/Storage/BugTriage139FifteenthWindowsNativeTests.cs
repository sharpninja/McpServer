namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>
/// TEST-MCP-USECASE-019/020: Windows-only native identity and SQLite boundary evidence.
/// </summary>
[Collection(nameof(WorkspaceIdentityNativeProcessStateCollection))]
public sealed class BugTriage139FifteenthWindowsNativeTests
{
    /// <summary>
    /// The Windows resolver completes cancellation within its public bound and reports the worker
    /// lifecycle truthfully.
    /// </summary>
    [Fact]
    public Task WorkspaceIdentityNativeCancellationWindowCompletesWithoutWorkerLeak() =>
        BugTriage139FifteenthNativeScenarios
            .AssertWindowsIdentityCreateFileCancellationGapAsync();

    /// <summary>
    /// A real Windows pre-handle kernel wait remains bounded and leaves no SQLite handle.
    /// </summary>
    [Fact]
    public Task SqliteOpenPlatformNativePreHandleWaitIsBoundedWithoutLeaks() =>
        BugTriage139FifteenthNativeScenarios.AssertWindowsSqliteKernelWaitAsync();

    /// <summary>
    /// A provider lifecycle failure remains exact when cancellation coincides with a Windows
    /// kernel wait.
    /// </summary>
    [Fact]
    public Task SqliteOpenKernelWaitThenIndependentDisposalFailurePreservesExactException() =>
        BugTriage139FifteenthNativeScenarios.AssertWindowsSqliteIndependentFailureAsync();
}
