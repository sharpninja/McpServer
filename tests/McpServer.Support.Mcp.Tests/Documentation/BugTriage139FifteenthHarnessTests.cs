using System.Reflection;
using McpServer.Support.Mcp.Tests.Infrastructure;

namespace McpServer.Support.Mcp.Tests.Documentation;

/// <summary>
/// TEST-MCP-USECASE-019: Fifteenth-review consumer and deterministic-build contract tests.
/// </summary>
[Collection(RepositoryRootProcessStateCollection.Name)]
public sealed class BugTriage139FifteenthHarnessTests
{

    /// <summary>
    /// TEST-MCP-USECASE-019-AC005: Deterministic builds must retain an explicit checkout anchor and
    /// copy Agent Help fixtures beside the external test assembly without an environment override.
    /// </summary>
    [Fact]
    public void DeterministicBuild_EmbedsRepositoryRootAndCopiesAgentHelpFixtures()
    {
        var metadata = typeof(BugTriage139FifteenthHarnessTests).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .SingleOrDefault(attribute =>
                string.Equals(
                    attribute.Key,
                    "McpRepositoryRoot",
                    StringComparison.Ordinal));
        Assert.NotNull(metadata);
        Assert.False(string.IsNullOrWhiteSpace(metadata!.Value));
        Assert.True(
            File.Exists(Path.Combine(metadata.Value!, "McpServer.sln")),
            metadata.Value);

        var fixture = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "AgentHelp",
            "bypass",
            "mcp-tool-failure-description.txt");
        Assert.True(File.Exists(fixture), fixture);
    }

    /// <summary>
    /// TEST-MCP-USECASE-020: The compiled test inventory must contain only the host-applicable
    /// native boundary tests, with an exact count rather than source-text substitutes.
    /// </summary>
    [Fact]
    public void PlatformNativeTests_CompiledInventoryMatchesHost()
    {
        var expected = OperatingSystem.IsWindows()
            ? new[]
            {
                "BugTriage139FifteenthSharedTests.ContainedRestoration_NestedDirectoriesRestoreContentsModeAndContainment",
                "BugTriage139FifteenthWindowsNativeTests.WorkspaceIdentity_NativeCancellationWindow_CompletesWithoutWorkerLeak",
                "BugTriage139FifteenthWindowsNativeTests.SqliteOpen_PlatformNativePreHandleWait_IsBoundedWithoutLeaks",
                "BugTriage139FifteenthWindowsNativeTests.SqliteOpen_KernelWaitThenIndependentDisposalFailure_PreservesExactException",
                "BugTriage139SixteenthIdentityWindowsTests.ResolveAsync_UnstoppableWindowsWorker_CompletesPubliclyAndRemainsObservable",
                "BugTriage139SixteenthIdentityWindowsTests.ResolveAsync_ResultWithheldUntilThreadExit_WorkerRemainsObservable",
                "BugTriage139SixteenthIdentityWindowsTests.ResolveAsync_RemoteUnc_IsRejectedBeforeNativeOpen",
                "BugTriage139SixteenthWindowsProcessTests.ProcessRunner_WindowsLaunchContract_UsesSuspendedCreation",
                "BugTriage139SixteenthWindowsProcessTests.ProcessRunner_AssignmentWindow_KeepsImmediateHelperSuspended",
                "BugTriage139SixteenthWindowsProcessTests.ProcessRunner_ImmediateNativeDescendant_IsKilledWithinBound",
                "BugTriage139SixteenthWindowsProcessTests.ProcessRunner_RawArguments_PreservesMultipleTokens",
                "BugTriage139SixteenthWindowsProcessTests.ProcessRunner_RawArguments_AndArgumentList_PreserveQuotedBoundaries",
            }
            : OperatingSystem.IsLinux()
                ? new[]
                {
                    "BugTriage139FifteenthSharedTests.ContainedRestoration_NestedDirectoriesRestoreContentsModeAndContainment",
                    "BugTriage139FifteenthLinuxNativeTests.WorkspaceIdentity_NativeCancellationWindow_CompletesWithoutWorkerLeak",
                    "BugTriage139FifteenthLinuxNativeTests.SqliteOpen_PlatformNativePreHandleWait_IsBoundedWithoutLeaks",
                    "BugTriage139FifteenthLinuxNativeTests.SqliteOpen_KernelWaitThenIndependentDisposalFailure_PreservesExactException",
                    "RequirementsFourteenthLinuxNativeTests.OpenFileForRead_LinuxFifoWithoutWriter_IsRejectedWithinBound",
                    "BugTriage139SixteenthLinuxTests.WorkspaceIdentity_SafeSymlinkIntoFuse_IsRejectedByAtomicNoCrossDeviceResolver",
                    "BugTriage139SixteenthLinuxTests.WorkspaceIdentity_AliasReplacement_IsRejectedByAtomicNoCrossDeviceResolver",
                    "BugTriage139SixteenthLinuxTests.SqliteOpen_SafeSymlinkIntoFuse_IsRejectedBeforeWorker",
                    "BugTriage139SixteenthLinuxTests.SqliteOpen_AliasReplacementBeforePhysicalResolution_IsRejected",
                    "BugTriage139SixteenthLinuxTests.SqliteOpen_AliasReplacementAtNativeOpen_IsAtomicallyRejected",
                    "BugTriage139SixteenthLinuxTests.WorkspaceIdentity_UnstoppableUnixHelper_RemainsObservableAfterPublicTimeout",
                    "BugTriage139SixteenthLinuxTests.SqlitePinnedDataSource_SupportsTransactionsAndReopen",
                    "BugTriage139EighteenthLinuxTests.NativeLinuxGate_EffectiveUserId_IsNonRoot",
                    "BugTriage139EighteenthLinuxTests.OpenFileForWrite_ParentRenamedOutsideRoot_RejectsWithoutExternalArtifact",
                    "BugTriage139EighteenthLinuxTests.EnsureDirectory_MissingNestedDirectory_FailsClosedBeforeMutation",
                    "BugTriage139EighteenthLinuxTests.RestoreFileAsync_RollsBackUnixPermissionsThroughPinnedAuthority",
                    "BugTriage139EighteenthLinuxTests.EnumerateFilesBoundedAsync_Fifo_IsIgnoredWithoutBlocking",
                    "BugTriage139EighteenthLinuxTests.EnumerateFilesBoundedAsync_DelayedFuseChildMount_IsRejectedBeforeTraversal",
                    "BugTriage139EighteenthLinuxTests.EnumerateFilesBoundedAsync_RootReplacedByDelayedFuseMountAfterPin_UsesPinnedRootOnly",
                    "BugTriage139EighteenthLinuxTests.EnumerateFilesBoundedAsync_RepeatedRootReplacement_DoesNotConsumeWorkerAdmission",
                    "BugTriage139EighteenthLinuxTests.RequirementsExport_ParentMovedAfterResolution_RejectsWithoutExternalArtifact",
                    "BugTriage139EighteenthLinuxTests.EnumerateFilesBoundedAsync_RootReplacedBeforeAuthorityCapture_RejectsWithoutWorkerStranding",
                    "BugTriage139EighteenthLinuxTests.RequirementsExport_RootReplacedByDelayedFuseMountBeforeCreate_UsesPinnedRootOnly",
                    "BugTriage139EighteenthLinuxTests.RequirementsExport_CaseDistinctHomeFiles_RemovesOnlyStaleName",
                    "BugTriage139EighteenthLinuxTests.RequirementsExport_NestedStaleCleanup_IsRejectedBeforeGeneratedWrite",
                    "BugTriage139EighteenthLinuxTests.WorkspaceIdentity_SeparateApprovedLocalMount_IsAccepted",
                    "BugTriage139EighteenthLinuxTests.SqliteOpen_SeparateApprovedLocalMount_IsAccepted",
                }
                : new[]
                {
                    "BugTriage139FifteenthSharedTests.ContainedRestoration_NestedDirectoriesRestoreContentsModeAndContainment",
                };
        var governedTypes = new HashSet<string>(
            StringComparer.Ordinal)
        {
            "BugTriage139FifteenthSharedTests",
            "BugTriage139FifteenthWindowsNativeTests",
            "BugTriage139FifteenthLinuxNativeTests",
            "RequirementsFourteenthLinuxNativeTests",
            "BugTriage139SixteenthIdentityWindowsTests",
            "BugTriage139SixteenthLinuxTests",
            "BugTriage139SixteenthWindowsProcessTests",
            "BugTriage139EighteenthLinuxTests",
        };
        var actual = typeof(BugTriage139FifteenthHarnessTests)
            .Assembly
            .GetTypes()
            .Where(type => governedTypes.Contains(type.Name))
            .SelectMany(type => type
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(method => method.GetCustomAttribute<FactAttribute>() is not null)
                .Select(method => $"{type.Name}.{method.Name}"))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            expected.OrderBy(name => name, StringComparer.Ordinal),
            actual);
    }
}
