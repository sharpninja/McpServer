using System.Reflection;
using QBrainAi.Support.Mcp.Tests.Infrastructure;

namespace QBrainAi.Support.Mcp.Tests.Documentation;

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
    public void DeterministicBuildEmbedsRepositoryRootAndCopiesAgentHelpFixtures()
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
            File.Exists(Path.Combine(metadata.Value!, "QBrainAi.sln")),
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
    public void PlatformNativeTestsCompiledInventoryMatchesHost()
    {
        var expected = OperatingSystem.IsWindows()
            ? new[]
            {
                "BugTriage139FifteenthSharedTests.ContainedRestorationNestedDirectoriesRestoreContentsModeAndContainment",
                "BugTriage139FifteenthWindowsNativeTests.WorkspaceIdentityNativeCancellationWindowCompletesWithoutWorkerLeak",
                "BugTriage139FifteenthWindowsNativeTests.SqliteOpenPlatformNativePreHandleWaitIsBoundedWithoutLeaks",
                "BugTriage139FifteenthWindowsNativeTests.SqliteOpenKernelWaitThenIndependentDisposalFailurePreservesExactException",
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
                    "BugTriage139FifteenthSharedTests.ContainedRestorationNestedDirectoriesRestoreContentsModeAndContainment",
                    "BugTriage139FifteenthLinuxNativeTests.WorkspaceIdentityNativeCancellationWindowCompletesWithoutWorkerLeak",
                    "BugTriage139FifteenthLinuxNativeTests.SqliteOpenPlatformNativePreHandleWaitIsBoundedWithoutLeaks",
                    "BugTriage139FifteenthLinuxNativeTests.SqliteOpenKernelWaitThenIndependentDisposalFailurePreservesExactException",
                    "RequirementsFourteenthLinuxNativeTests.OpenFileForRead_LinuxFifoWithoutWriter_IsRejectedWithinBound",
                    "BugTriage139SixteenthLinuxTests.WorkspaceIdentitySafeSymlinkIntoFuseIsRejectedByAtomicNoCrossDeviceResolver",
                    "BugTriage139SixteenthLinuxTests.WorkspaceIdentityAliasReplacementIsRejectedByAtomicNoCrossDeviceResolver",
                    "BugTriage139SixteenthLinuxTests.SqliteOpenSafeSymlinkIntoFuseIsRejectedBeforeWorker",
                    "BugTriage139SixteenthLinuxTests.SqliteOpenAliasReplacementBeforePhysicalResolutionIsRejected",
                    "BugTriage139SixteenthLinuxTests.SqliteOpenAliasReplacementAtNativeOpenIsAtomicallyRejected",
                    "BugTriage139SixteenthLinuxTests.WorkspaceIdentityUnstoppableUnixHelperRemainsObservableAfterPublicTimeout",
                    "BugTriage139SixteenthLinuxTests.SqlitePinnedDataSourceSupportsTransactionsAndReopen",
                    "BugTriage139EighteenthLinuxTests.NativeLinuxGateEffectiveUserIdIsNonRoot",
                    "BugTriage139EighteenthLinuxTests.OpenFileForWriteParentRenamedOutsideRootRejectsWithoutExternalArtifact",
                    "BugTriage139EighteenthLinuxTests.EnsureDirectoryMissingNestedDirectoryFailsClosedBeforeMutation",
                    "BugTriage139EighteenthLinuxTests.RestoreFileAsyncRollsBackUnixPermissionsThroughPinnedAuthority",
                    "BugTriage139EighteenthLinuxTests.EnumerateFilesBoundedAsyncFifoIsIgnoredWithoutBlocking",
                    "BugTriage139EighteenthLinuxTests.EnumerateFilesBoundedAsyncDelayedFuseChildMountIsRejectedBeforeTraversal",
                    "BugTriage139EighteenthLinuxTests.EnumerateFilesBoundedAsyncRootReplacedByDelayedFuseMountAfterPinUsesPinnedRootOnly",
                    "BugTriage139EighteenthLinuxTests.EnumerateFilesBoundedAsyncRepeatedRootReplacementDoesNotConsumeWorkerAdmission",
                    "BugTriage139EighteenthLinuxTests.RequirementsExportParentMovedAfterResolutionRejectsWithoutExternalArtifact",
                    "BugTriage139EighteenthLinuxTests.EnumerateFilesBoundedAsyncRootReplacedBeforeAuthorityCaptureRejectsWithoutWorkerStranding",
                    "BugTriage139EighteenthLinuxTests.RequirementsExportRootReplacedByDelayedFuseMountBeforeCreateUsesPinnedRootOnly",
                    "BugTriage139EighteenthLinuxTests.RequirementsExportCaseDistinctHomeFilesRemovesOnlyStaleName",
                    "BugTriage139EighteenthLinuxTests.RequirementsExportNestedStaleCleanupIsRejectedBeforeGeneratedWrite",
                    "BugTriage139EighteenthLinuxTests.WorkspaceIdentitySeparateApprovedLocalMountIsAccepted",
                    "BugTriage139EighteenthLinuxTests.SqliteOpenSeparateApprovedLocalMountIsAccepted",
                }
                : new[]
                {
                    "BugTriage139FifteenthSharedTests.ContainedRestorationNestedDirectoriesRestoreContentsModeAndContainment",
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
