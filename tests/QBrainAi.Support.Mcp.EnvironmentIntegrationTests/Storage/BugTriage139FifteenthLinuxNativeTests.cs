namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Storage;

/// <summary>
/// TEST-MCP-USECASE-019/020: Linux-only native identity, FUSE, and SQLite boundary evidence.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-USECASE-019; TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: moved whole from QBrainAi.Support.Mcp.Tests (Storage/BugTriage139FifteenthLinuxNativeTests.cs) into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests together with BugTriage139FifteenthNativeScenarios and the harnesses that find it by reflection in their own assembly, so the unit-owned project holds only unit cases (slice S8, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); the OS-conditional compile block that leaves this Linux-only file out of a Windows build moved into this project's file; only the namespace changed.</remarks>
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
