using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using QBrainAi.Client;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Requirements;
using Microsoft.Data.Sqlite;

namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>
/// TEST-MCP-USECASE-020-AC001/AC005/AC006: Linux-only eighteenth-review coverage for
/// root-anchored mutation, local-mount compatibility, FUSE rejection, FIFO safety, and non-root
/// execution.
/// </summary>
[Collection(nameof(WorkspaceIdentityNativeProcessStateCollection))]
[SupportedOSPlatform("linux")]
[Trait("Category", "Integration")]
[Trait("Platform", "Linux")]
public sealed class BugTriage139EighteenthLinuxTests
{
    /// <summary>Proves the native Linux gate is executing as a non-root effective user.</summary>
    [Fact]
    public void NativeLinuxGate_EffectiveUserId_IsNonRoot()
    {
        Assert.True(OperatingSystem.IsLinux());
        Assert.NotEqual(0U, GetEffectiveUserId());
    }

    /// <summary>
    /// Renames a pinned parent outside the workspace immediately before file creation and proves
    /// root-relative openat2 containment leaves no file in the external directory.
    /// </summary>
    [Fact]
    public void OpenFileForWrite_ParentRenamedOutsideRoot_RejectsWithoutExternalArtifact()
    {
        using var fixture = new LinuxMutationFixture();
        var hook = ResolveWorkspaceFileSystemHook("s_beforeLinuxRootAnchoredFileCreate");
        var moved = 0;
        hook.SetValue(
            null,
            (Action)(() =>
            {
                Directory.Move(fixture.LiveParent, fixture.ExternalParent);
                Interlocked.Exchange(ref moved, 1);
            }));

        try
        {
            Assert.ThrowsAny<IOException>(
                () =>
                {
                    using var _ = WorkspaceContainedFileSystem.OpenFileForWrite(
                        fixture.Root,
                        Path.Combine(fixture.LiveParent, "escaped.txt"));
                });
        }
        finally
        {
            hook.SetValue(null, null);
        }

        Assert.Equal(1, Volatile.Read(ref moved));
        Assert.False(File.Exists(Path.Combine(fixture.ExternalParent, "escaped.txt")));
        Assert.False(File.Exists(Path.Combine(fixture.LiveParent, "escaped.txt")));
    }

    /// <summary>
    /// BUG-BASELINE-001 fallback (TR-MCP-USECASE-017): without Landlock, Linux has no mkdirat with
    /// openat2's contained-resolution guarantees, so a missing export directory fails closed before
    /// it can create an externally visible child. Landlock is reported unavailable through the seam.
    /// </summary>
    [Fact]
    public void EnsureDirectory_MissingNestedDirectory_FailsClosedBeforeMutation()
    {
        using var fixture = new LinuxMutationFixture();
        using var fallback = LandlockUnavailable();

        Assert.Throws<DirectoryNotFoundException>(
            () => WorkspaceContainedFileSystem.EnsureDirectory(
                fixture.Root,
                Path.Combine(fixture.LiveParent, "escaped-directory")));

        Assert.False(Directory.Exists(Path.Combine(fixture.ExternalParent, "escaped-directory")));
        Assert.False(Directory.Exists(Path.Combine(fixture.LiveParent, "escaped-directory")));
    }

    /// <summary>
    /// BUG-BASELINE-001 (TR-MCP-USECASE-017): with Landlock, a missing nested export folder chain is
    /// created beneath the pinned root and nothing appears outside it.
    /// </summary>
    [Fact]
    public void EnsureDirectory_MissingNestedDirectory_IsCreatedUnderLandlock()
    {
        Assert.True(LandlockAbiVersion() >= 1, "The native Linux gate requires the Landlock LSM.");
        using var fixture = new LinuxMutationFixture();
        var target = Path.Combine(fixture.LiveParent, "created", "nested");

        WorkspaceContainedFileSystem.EnsureDirectory(fixture.Root, target);

        Assert.True(Directory.Exists(target));
        Assert.False(Directory.Exists(fixture.ExternalParent));
    }

    /// <summary>
    /// BUG-BASELINE-001 (TR-MCP-USECASE-017): a pinned parent moved outside the root between pinning
    /// and mkdirat is denied by the kernel through Landlock, so no directory appears outside. The move
    /// runs on another thread because the confined thread itself may not mutate outside the root.
    /// </summary>
    [Fact]
    public void EnsureDirectory_ParentMovedOutsideBeforeCreate_IsDeniedByLandlock()
    {
        using var fixture = new LinuxMutationFixture();
        using var seam = BeforeConfinedMutation(
            () => Directory.Move(fixture.LiveParent, fixture.ExternalParent));

        Assert.ThrowsAny<IOException>(
            () => WorkspaceContainedFileSystem.EnsureDirectory(
                fixture.Root,
                Path.Combine(fixture.LiveParent, "escaped-directory")));

        Assert.True(Directory.Exists(fixture.ExternalParent));
        Assert.False(Directory.Exists(Path.Combine(fixture.ExternalParent, "escaped-directory")));
    }

    /// <summary>
    /// BUG-BASELINE-001 (TR-MCP-USECASE-017): with Landlock, a nested stale file is deleted through
    /// its pinned parent.
    /// </summary>
    [Fact]
    public void DeleteFileIfExists_NestedStaleFile_DeletesUnderLandlock()
    {
        using var fixture = new LinuxMutationFixture();
        var stale = Path.Combine(fixture.LiveParent, "stale.md");
        File.WriteAllText(stale, "stale");

        Assert.True(WorkspaceContainedFileSystem.DeleteFileIfExists(fixture.Root, stale));
        Assert.False(File.Exists(stale));
        Assert.False(WorkspaceContainedFileSystem.DeleteFileIfExists(fixture.Root, stale));
    }

    /// <summary>
    /// BUG-BASELINE-001 (TR-MCP-USECASE-017): a nested stale file whose parent is moved outside the
    /// root before unlinkat stays intact; the kernel denies the delete through Landlock.
    /// </summary>
    [Fact]
    public void DeleteFileIfExists_NestedParentMovedOutsideBeforeUnlink_IsDeniedByLandlock()
    {
        using var fixture = new LinuxMutationFixture();
        var stale = Path.Combine(fixture.LiveParent, "stale.md");
        File.WriteAllText(stale, "stale");
        using var seam = BeforeConfinedMutation(
            () => Directory.Move(fixture.LiveParent, fixture.ExternalParent));

        Assert.ThrowsAny<IOException>(
            () => WorkspaceContainedFileSystem.DeleteFileIfExists(fixture.Root, stale));

        Assert.Equal("stale", File.ReadAllText(Path.Combine(fixture.ExternalParent, "stale.md")));
    }

    /// <summary>
    /// BUG-BASELINE-001 (TR-MCP-USECASE-017): with Landlock, a missing export root and its missing
    /// ancestors are created beneath the deepest existing ancestor and then pinned.
    /// </summary>
    [Fact]
    public void OpenExportRoot_MissingRootAndAncestors_IsCreatedUnderLandlock()
    {
        using var fixture = new LinuxMutationFixture();
        var missingRoot = Path.Combine(fixture.Root, "docs", "Project", "wiki");

        using (var exportRoot = WorkspaceContainedFileSystem.OpenExportRoot(missingRoot))
            Assert.Equal(missingRoot, exportRoot.RootPath);

        Assert.True(Directory.Exists(missingRoot));
        Assert.False(Directory.Exists(fixture.ExternalParent));
    }

    /// <summary>
    /// BUG-BASELINE-001 fallback (TR-MCP-USECASE-017): without Landlock a missing export root still
    /// fails closed before any directory is created.
    /// </summary>
    [Fact]
    public void OpenExportRoot_MissingRoot_FailsClosedWithoutLandlock()
    {
        using var fixture = new LinuxMutationFixture();
        using var fallback = LandlockUnavailable();
        var missingRoot = Path.Combine(fixture.Root, "docs", "Project", "wiki");

        Assert.Throws<DirectoryNotFoundException>(
            () => WorkspaceContainedFileSystem.OpenExportRoot(missingRoot));

        Assert.False(Directory.Exists(Path.Combine(fixture.Root, "docs")));
    }

    /// <summary>
    /// BUG-BASELINE-001 (TR-MCP-USECASE-017): an operation run under the export confinement cannot
    /// create a directory outside its hierarchy even through an absolute path, proving the kernel,
    /// not the caller's path checks, enforces the boundary.
    /// </summary>
    [Fact]
    public void LandlockConfinement_DeniesDirectoryCreationOutsideItsHierarchy()
    {
        using var fixture = new LinuxMutationFixture();
        Directory.CreateDirectory(fixture.ExternalParent);
        var outside = Path.Combine(fixture.ExternalParent, "outside-attempt");
        var run = typeof(WorkspaceContainedFileSystem).GetMethod(
            "RunLandlockConfined",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(run);
        var descriptor = OpenPathDescriptor(fixture.LiveParent, 0x200000 | 0x10000 | 0x80000);
        Assert.True(descriptor >= 0, $"open(O_PATH) failed: {Marshal.GetLastPInvokeError()}");
        using var hierarchy = new Microsoft.Win32.SafeHandles.SafeFileHandle((nint)descriptor, ownsHandle: true);

        var failure = Assert.IsType<TargetInvocationException>(
            Record.Exception(() => run!.MakeGenericMethod(typeof(bool)).Invoke(
                null,
                [hierarchy, (Func<bool>)(() =>
                {
                    Directory.CreateDirectory(outside);
                    return true;
                })])));

        Assert.IsAssignableFrom<UnauthorizedAccessException>(failure.InnerException);
        Assert.False(Directory.Exists(outside));
    }

    /// <summary>
    /// BUG-BASELINE-001 what-if (TR-MCP-USECASE-017 AC-004): when the confinement cannot be set up, the
    /// operation never runs and the call fails. The kernel itself rejects the path-beneath rule here,
    /// because directory rights cannot be granted on a regular-file descriptor (landlock_add_rule EINVAL).
    /// </summary>
    [Fact]
    public void LandlockConfinement_RuleSetupFailure_NeverRunsTheOperation()
    {
        using var fixture = new LinuxMutationFixture();
        var regularFile = Path.Combine(fixture.LiveParent, "not-a-directory");
        File.WriteAllText(regularFile, "file");
        var run = typeof(WorkspaceContainedFileSystem).GetMethod(
            "RunLandlockConfined",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(run);
        var descriptor = OpenPathDescriptor(regularFile, 0x200000 | 0x80000);
        Assert.True(descriptor >= 0, $"open(O_PATH) failed: {Marshal.GetLastPInvokeError()}");
        using var hierarchy = new Microsoft.Win32.SafeHandles.SafeFileHandle((nint)descriptor, ownsHandle: true);
        var ran = false;

        var failure = Assert.IsType<TargetInvocationException>(
            Record.Exception(() => run!.MakeGenericMethod(typeof(bool)).Invoke(
                null,
                [hierarchy, (Func<bool>)(() =>
                {
                    ran = true;
                    return true;
                })])));

        Assert.IsAssignableFrom<IOException>(failure.InnerException);
        Assert.False(ran);
    }

    /// <summary>
    /// BUG-BASELINE-001 what-if (TR-MCP-USECASE-017 AC-005): a symbolic link planted at a missing folder
    /// name just before mkdirat is never followed. mkdirat reports EEXIST, the re-open beneath the anchor
    /// refuses the link, the call fails, and nothing is created through the link outside the root.
    /// </summary>
    [Fact]
    public void EnsureDirectory_SymlinkPlantedAtMissingSegment_IsNeverFollowed()
    {
        using var fixture = new LinuxMutationFixture();
        Directory.CreateDirectory(fixture.ExternalParent);
        var planted = Path.Combine(fixture.LiveParent, "created");
        using var seam = BeforeConfinedMutation(
            () => Directory.CreateSymbolicLink(planted, fixture.ExternalParent));

        Assert.NotNull(Record.Exception(
            () => WorkspaceContainedFileSystem.EnsureDirectory(
                fixture.Root,
                Path.Combine(planted, "nested"))));

        Assert.Empty(Directory.EnumerateFileSystemEntries(fixture.ExternalParent));
    }

    /// <summary>
    /// Proves a rejected Linux export transaction restores both bytes and the exact Unix permissions
    /// through the retained root authority rather than by reopening an untrusted pathname.
    /// </summary>
    [Fact]
    public async Task RestoreFileAsync_RollsBackUnixPermissionsThroughPinnedAuthority()
    {
        using var fixture = new LinuxMutationFixture();
        var path = Path.Combine(fixture.Root, "rollback.md");
        const string originalContent = "before";
        var originalMode = UnixFileMode.UserRead |
                           UnixFileMode.UserWrite |
                           UnixFileMode.GroupRead;
        await File.WriteAllTextAsync(path, originalContent, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        File.SetUnixFileMode(path, originalMode);

        using var exportRoot = WorkspaceContainedFileSystem.OpenExportRoot(fixture.Root);
        var metadata = await exportRoot.GetFileMetadataAsync(
                "rollback.md",
                TimeSpan.FromSeconds(2),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        await exportRoot.WriteTextFileAsync(
                "rollback.md",
                "changed",
                Encoding.UTF8,
                DateTime.UtcNow,
                readOnly: true,
                cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        await using var originalBytes = new MemoryStream(Encoding.UTF8.GetBytes(originalContent), writable: false);
        await exportRoot.RestoreFileAsync(
                "rollback.md",
                originalBytes,
                metadata,
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.Equal(
            originalContent,
            await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken).ConfigureAwait(true));
        Assert.Equal(originalMode, File.GetUnixFileMode(path));
    }


    /// <summary>
    /// Proves a FIFO entry is classified by descriptor metadata and never opened as file content or
    /// allowed to block bounded traversal.
    /// </summary>
    [Fact]
    public async Task EnumerateFilesBoundedAsync_Fifo_IsIgnoredWithoutBlocking()
    {
        using var fixture = new LinuxMutationFixture();
        var ordinaryFile = Path.Combine(fixture.LiveParent, "ordinary.md");
        var fifo = Path.Combine(fixture.LiveParent, "blocking.fifo");
        await File.WriteAllTextAsync(
                ordinaryFile,
                "ordinary",
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.Equal(0, CreateFifo(fifo, Convert.ToUInt32("600", 8)));

        var stopwatch = Stopwatch.StartNew();
        var entries = await WorkspaceContainedFileSystem.EnumerateFilesBoundedAsync(
                fixture.Root,
                fixture.LiveParent,
                new WorkspaceTraversalLimits(),
                TimeSpan.FromSeconds(2),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        stopwatch.Stop();

        var entry = Assert.Single(entries);
        Assert.Equal(ordinaryFile, entry.FullPath);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(1), $"FIFO traversal took {stopwatch.Elapsed}.");
    }

    /// <summary>
    /// Proves traversal rejects a real delayed FUSE child mount at the root-descriptor boundary
    /// without entering its blocking metadata implementation.
    /// </summary>
    [Fact(Timeout = 60_000)]
    public async Task EnumerateFilesBoundedAsync_DelayedFuseChildMount_IsRejectedBeforeTraversal()
    {
        Assert.True(LinuxDelayedFuseFixture.IsSupported);
        await using var fixture = await LinuxDelayedFuseFixture.CreateAsync().ConfigureAwait(true);
        var stopwatch = Stopwatch.StartNew();

        var exception = await Assert.ThrowsAsync<NotSupportedException>(
                () => WorkspaceContainedFileSystem.EnumerateFilesBoundedAsync(
                    fixture.RootPath,
                    fixture.RootPath,
                    new WorkspaceTraversalLimits(),
                    TimeSpan.FromMilliseconds(500),
                    TestContext.Current.CancellationToken))
            .ConfigureAwait(true);
        stopwatch.Stop();

        Assert.Contains("mount", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(1), $"FUSE rejection took {stopwatch.Elapsed}.");
        Assert.Equal(0, WorkspaceContainedFileSystem.ActiveTraversalWorkerCount);
    }

    /// <summary>
    /// Replaces the pathname after its root descriptor is pinned with a real bind-mounted delayed
    /// FUSE filesystem. Traversal must finish from the original descriptor without opening the
    /// replacement mount or consuming a bounded worker.
    /// </summary>
    [Fact(Timeout = 60_000)]
    public async Task EnumerateFilesBoundedAsync_RootReplacedByDelayedFuseMountAfterPin_UsesPinnedRootOnly()
    {
        Assert.True(OperatingSystem.IsLinux());
        Assert.True(LinuxDelayedFuseFixture.IsSupported);
        await using var fixture = await LinuxDelayedFuseFixture.CreateAsync().ConfigureAwait(true);
        var root = Path.Combine(fixture.RootPath, "pinned-root");
        var movedRoot = Path.Combine(fixture.RootPath, "moved-root");
        var ordinary = Path.Combine(root, "ordinary.md");
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(
                ordinary,
                "ordinary",
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        var hook = ResolveWorkspaceFileSystemHook("s_afterTraversalRootPinned");
        var replacements = 0;
        hook.SetValue(
            null,
            (Action)(() =>
            {
                if (Interlocked.Exchange(ref replacements, 1) != 0)
                    return;
                Directory.Move(root, movedRoot);
                Directory.CreateDirectory(root);
                fixture.MountDelayedFuseAtAsync(root).GetAwaiter().GetResult();
            }));

        try
        {
            var stopwatch = Stopwatch.StartNew();
            var entries = await WorkspaceContainedFileSystem.EnumerateFilesBoundedAsync(
                    root,
                    root,
                    new WorkspaceTraversalLimits(),
                    TimeSpan.FromMilliseconds(750),
                    TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            stopwatch.Stop();

            var entry = Assert.Single(entries);
            Assert.Equal(ordinary, entry.FullPath);
            Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(1), $"Pinned-root traversal took {stopwatch.Elapsed}.");
        }
        finally
        {
            hook.SetValue(null, null);
        }

        Assert.Equal(1, Volatile.Read(ref replacements));
        Assert.True(File.Exists(Path.Combine(movedRoot, "ordinary.md")));
        Assert.Equal(0, WorkspaceContainedFileSystem.ActiveTraversalWorkerCount);
    }

    /// <summary>
    /// Repeats real delayed-FUSE root replacement across more callers than the bounded-worker cap.
    /// Every caller must complete from its already-pinned root descriptor; no blocked replacement
    /// provider may consume an admission slot for a later caller.
    /// </summary>
    [Fact(Timeout = 60_000)]
    public async Task EnumerateFilesBoundedAsync_RepeatedRootReplacement_DoesNotConsumeWorkerAdmission()
    {
        Assert.True(OperatingSystem.IsLinux());
        Assert.True(LinuxDelayedFuseFixture.IsSupported);
        await using var fixture = await LinuxDelayedFuseFixture.CreateAsync().ConfigureAwait(true);
        var roots = Enumerable.Range(0, 5)
            .Select(index => Path.Combine(fixture.RootPath, $"admission-root-{index}"))
            .ToArray();
        var movedRoots = Enumerable.Range(0, roots.Length)
            .Select(index => Path.Combine(fixture.RootPath, $"admission-root-moved-{index}"))
            .ToArray();
        foreach (var root in roots)
        {
            Directory.CreateDirectory(root);
            await File.WriteAllTextAsync(
                    Path.Combine(root, "ordinary.md"),
                    "ordinary",
                    TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
        }

        var hook = ResolveWorkspaceFileSystemHook("s_afterTraversalRootPinned");
        var replacements = -1;
        hook.SetValue(
            null,
            (Action)(() =>
            {
                var index = Interlocked.Increment(ref replacements);
                Assert.InRange(index, 0, roots.Length - 1);
                Directory.Move(roots[index], movedRoots[index]);
                Directory.CreateDirectory(roots[index]);
                fixture.MountDelayedFuseAtAsync(roots[index]).GetAwaiter().GetResult();
            }));

        try
        {
            for (var index = 0; index < roots.Length; index++)
            {
                var stopwatch = Stopwatch.StartNew();
                var entries = await WorkspaceContainedFileSystem.EnumerateFilesBoundedAsync(
                        roots[index],
                        roots[index],
                        new WorkspaceTraversalLimits(),
                        TimeSpan.FromMilliseconds(750),
                        TestContext.Current.CancellationToken)
                    .ConfigureAwait(true);
                stopwatch.Stop();

                Assert.Equal(Path.Combine(roots[index], "ordinary.md"), Assert.Single(entries).FullPath);
                Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(1), $"Caller {index} took {stopwatch.Elapsed}.");
                Assert.Equal(0, WorkspaceContainedFileSystem.ActiveTraversalWorkerCount);
            }
        }
        finally
        {
            hook.SetValue(null, null);
        }

        Assert.Equal(roots.Length - 1, Volatile.Read(ref replacements));
        Assert.Equal(0, WorkspaceContainedFileSystem.ActiveTraversalWorkerCount);
    }

    /// <summary>
    /// A requirements export rejects a parent that moves outside the workspace after the writer has
    /// resolved it, without creating the generated file in that moved directory.
    /// </summary>
    [Fact]
    public async Task RequirementsExport_ParentMovedAfterResolution_RejectsWithoutExternalArtifact()
    {
        using var fixture = new LinuxMutationFixture();
        var hook = ResolveWorkspaceFileSystemHook("s_beforeLinuxRootAnchoredFileCreate");
        var moved = 0;
        hook.SetValue(
            null,
            (Action)(() =>
            {
                if (Interlocked.Exchange(ref moved, 1) != 0)
                    return;
                Directory.Move(fixture.LiveParent, fixture.ExternalParent);
            }));

        try
        {
            await Assert.ThrowsAnyAsync<IOException>(
                    () => RequirementsDocumentExportWriter.WriteAsync(
                        fixture.Root,
                        "wiki",
                        "all",
                        DateTimeOffset.UtcNow,
                        [new RequirementsRenderedDocument("live/generated.md", "generated", "text/markdown")],
                        ["live"],
                        TestContext.Current.CancellationToken))
                .ConfigureAwait(true);
        }
        finally
        {
            hook.SetValue(null, null);
        }

        Assert.Equal(1, Volatile.Read(ref moved));
        Assert.False(File.Exists(Path.Combine(fixture.ExternalParent, "generated.md")));
        Assert.False(File.Exists(Path.Combine(fixture.LiveParent, "generated.md")));
    }

    /// <summary>
    /// Replaces an approved root pathname with delayed FUSE after provider classification and before
    /// descriptor authority capture. Every attempted caller must reject promptly without consuming
    /// a traversal worker, and a later caller of the original retained tree must still succeed.
    /// </summary>
    [Fact(Timeout = 60_000)]
    public async Task EnumerateFilesBoundedAsync_RootReplacedBeforeAuthorityCapture_RejectsWithoutWorkerStranding()
    {
        Assert.True(OperatingSystem.IsLinux());
        Assert.True(LinuxDelayedFuseFixture.IsSupported);
        await using var fixture = await LinuxDelayedFuseFixture.CreateAsync().ConfigureAwait(true);
        var root = Path.Combine(fixture.RootPath, "pre-capture-root");
        var movedRoot = Path.Combine(fixture.RootPath, "pre-capture-root-retained");
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(
                Path.Combine(root, "ordinary.md"),
                "ordinary",
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        var hook = ResolveWorkspaceFileSystemHook("s_afterLinuxExportRootPolicyClassification");
        var replacements = 0;
        using var replacementReady = new ManualResetEventSlim(false);
        hook.SetValue(
            null,
            (Action)(() =>
            {
                if (Interlocked.Exchange(ref replacements, 1) == 0)
                {
                    try
                    {
                        Directory.Move(root, movedRoot);
                        Directory.CreateDirectory(root);
                        fixture.MountDelayedFuseAtAsync(root).GetAwaiter().GetResult();
                    }
                    finally
                    {
                        replacementReady.Set();
                    }
                }
                else
                {
                    replacementReady.Wait();
                }
            }));

        try
        {
            var completionTimes = await Task.WhenAll(
                    Enumerable.Range(0, 5).Select(
                        caller => Task.Run(
                            async () =>
                            {
                                var stopwatch = Stopwatch.StartNew();
                                var exception = await Assert.ThrowsAsync<NotSupportedException>(
                                        () => WorkspaceContainedFileSystem.EnumerateFilesBoundedAsync(
                                            root,
                                            root,
                                            new WorkspaceTraversalLimits(),
                                            TimeSpan.FromMilliseconds(750),
                                            TestContext.Current.CancellationToken))
                                    .ConfigureAwait(false);
                                stopwatch.Stop();
                                Assert.Contains("mount", exception.Message, StringComparison.OrdinalIgnoreCase);
                                return (caller, stopwatch.Elapsed);
                            })))
                .ConfigureAwait(true);
            Assert.All(
                completionTimes,
                result => Assert.True(
                    result.Elapsed < TimeSpan.FromSeconds(1),
                    $"Caller {result.caller} took {result.Elapsed}."));
            Assert.Equal(0, WorkspaceContainedFileSystem.ActiveTraversalWorkerCount);
        }
        finally
        {
            hook.SetValue(null, null);
        }

        var retainedEntries = await WorkspaceContainedFileSystem.EnumerateFilesBoundedAsync(
                movedRoot,
                movedRoot,
                new WorkspaceTraversalLimits(),
                TimeSpan.FromSeconds(1),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.Equal(Path.Combine(movedRoot, "ordinary.md"), Assert.Single(retainedEntries).FullPath);
        Assert.Equal(1, Volatile.Read(ref replacements));
        Assert.Equal(0, WorkspaceContainedFileSystem.ActiveTraversalWorkerCount);
    }


    /// <summary>
    /// Replaces the export root pathname with a real delayed FUSE bind mount after final
    /// root-relative validation and immediately before O_CREAT. The atomic openat2 mutation must
    /// write only through the pinned root descriptor, never through the replacement mount.
    /// </summary>
    [Fact(Timeout = 60_000)]
    public async Task RequirementsExport_RootReplacedByDelayedFuseMountBeforeCreate_UsesPinnedRootOnly()
    {
        Assert.True(OperatingSystem.IsLinux());
        Assert.True(LinuxDelayedFuseFixture.IsSupported);
        await using var fixture = await LinuxDelayedFuseFixture.CreateAsync().ConfigureAwait(true);
        var root = Path.Combine(fixture.RootPath, "export-root");
        var movedRoot = Path.Combine(fixture.RootPath, "export-root-moved");
        Directory.CreateDirectory(root);
        var hook = ResolveWorkspaceFileSystemHook("s_beforeLinuxRootAnchoredFileCreate");
        var replacements = 0;
        hook.SetValue(
            null,
            (Action)(() =>
            {
                if (Interlocked.Exchange(ref replacements, 1) != 0)
                    return;
                Directory.Move(root, movedRoot);
                Directory.CreateDirectory(root);
                fixture.MountDelayedFuseAtAsync(root).GetAwaiter().GetResult();
            }));

        try
        {
            var stopwatch = Stopwatch.StartNew();
            await RequirementsDocumentExportWriter.WriteAsync(
                    root,
                    "wiki",
                    "all",
                    DateTimeOffset.UtcNow,
                    [new RequirementsRenderedDocument("generated.md", "generated", "text/markdown")],
                    ct: TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            stopwatch.Stop();
            Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(1), $"Pinned-root export took {stopwatch.Elapsed}.");
        }
        finally
        {
            hook.SetValue(null, null);
        }

        Assert.Equal(1, Volatile.Read(ref replacements));
        Assert.True(File.Exists(Path.Combine(movedRoot, "generated.md")));
        Assert.Equal(0, WorkspaceContainedFileSystem.ActiveTraversalWorkerCount);
    }

    /// <summary>
    /// On a case-sensitive Linux export root, Home.md and home.md are distinct names; cleanup
    /// removes the stale spelling instead of treating it as the expected generated file.
    /// </summary>
    [Fact]
    public async Task RequirementsExport_CaseDistinctHomeFiles_RemovesOnlyStaleName()
    {
        using var fixture = new LinuxMutationFixture();
        var stale = Path.Combine(fixture.Root, "home.md");
        var generated = Path.Combine(fixture.Root, "Home.md");
        await File.WriteAllTextAsync(
                stale,
                "stale",
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        await RequirementsDocumentExportWriter.WriteAsync(
                fixture.Root,
                "markdown",
                "all",
                DateTimeOffset.UtcNow,
                [new RequirementsRenderedDocument("Home.md", "generated", "text/markdown")],
                ["."],
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.False(File.Exists(stale));
        Assert.Equal(
            "generated",
            await File.ReadAllTextAsync(generated, TestContext.Current.CancellationToken).ConfigureAwait(true));
    }

    /// <summary>
    /// BUG-BASELINE-001 (TR-MCP-USECASE-017): with Landlock, export cleanup deletes a nested stale
    /// file and writes the generated file.
    /// </summary>
    [Fact]
    public async Task RequirementsExport_NestedStaleCleanup_DeletesStaleUnderLandlock()
    {
        using var fixture = new LinuxMutationFixture();
        var stale = Path.Combine(fixture.LiveParent, "stale.md");
        var generated = Path.Combine(fixture.Root, "generated.md");
        await File.WriteAllTextAsync(stale, "stale", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        await RequirementsDocumentExportWriter.WriteAsync(
                fixture.Root,
                "markdown",
                "all",
                DateTimeOffset.UtcNow,
                [new RequirementsRenderedDocument("generated.md", "generated", "text/markdown")],
                ["."],
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.False(File.Exists(stale));
        Assert.Equal(
            "generated",
            await File.ReadAllTextAsync(generated, TestContext.Current.CancellationToken).ConfigureAwait(true));
    }

    /// <summary>
    /// BUG-BASELINE-001 fallback (TR-MCP-USECASE-017): without Landlock a nested stale entry needs a
    /// delete for which Linux has no contained atomic primitive, so cleanup rejects the export before
    /// any generated file becomes visible. Landlock is reported unavailable through the seam.
    /// </summary>
    [Fact]
    public async Task RequirementsExport_NestedStaleCleanup_IsRejectedBeforeGeneratedWrite()
    {
        using var fixture = new LinuxMutationFixture();
        using var fallback = LandlockUnavailable();
        var stale = Path.Combine(fixture.LiveParent, "stale.md");
        var generated = Path.Combine(fixture.Root, "generated.md");
        await File.WriteAllTextAsync(
                stale,
                "stale",
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        var exception = await Assert.ThrowsAsync<NotSupportedException>(
                () => RequirementsDocumentExportWriter.WriteAsync(
                    fixture.Root,
                    "markdown",
                    "all",
                    DateTimeOffset.UtcNow,
                    [new RequirementsRenderedDocument("generated.md", "generated", "text/markdown")],
                    ["."],
                    TestContext.Current.CancellationToken))
            .ConfigureAwait(true);

        Assert.Contains("live/stale.md", exception.Message, StringComparison.Ordinal);

        Assert.True(File.Exists(stale));
        Assert.False(File.Exists(generated));
    }

    /// <summary>
    /// Proves physical identity accepts a workspace rooted on an actually separate,
    /// policy-approved local Linux mount instead of rejecting the mount crossing from AT_FDCWD.
    /// </summary>
    [Fact]
    public async Task WorkspaceIdentity_SeparateApprovedLocalMount_IsAccepted()
    {
        using var fixture = SeparateLocalMountFixture.Create();
        var workspace = Path.Combine(fixture.TestRoot, "identity-workspace");
        Directory.CreateDirectory(workspace);

        var identity = await WorkspaceIdentityPath.ResolveAsync(
                workspace,
                WorkspacePathPlatform.CaseSensitive,
                TimeSpan.FromSeconds(2),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.StartsWith(fixture.MountPoint, identity.NormalizedPath, StringComparison.Ordinal);
        Assert.DoesNotContain(" (deleted)", identity.NormalizedPath, StringComparison.Ordinal);
    }

    /// <summary>
    /// Proves SQLite can create, use, and dispose a database on an actually separate,
    /// policy-approved local Linux mount.
    /// </summary>
    [Fact]
    public async Task SqliteOpen_SeparateApprovedLocalMount_IsAccepted()
    {
        using var fixture = SeparateLocalMountFixture.Create();
        var dataSource = Path.Combine(fixture.TestRoot, "separate-mount.db");
        using var caller = new SqliteConnection(
            new SqliteConnectionStringBuilder
            {
                DataSource = dataSource,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Pooling = false,
            }.ToString());

        await using var opened = await SqliteBoundedConnectionOpener.OpenAsync(
                caller,
                TimeSpan.FromSeconds(3),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        await using var command = opened.CreateCommand();
        command.CommandText = "SELECT 1;";
        var result = await command.ExecuteScalarAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.Equal(1L, Assert.IsType<long>(result));
        Assert.Equal(System.Data.ConnectionState.Closed, caller.State);
    }

    private static FieldInfo ResolveWorkspaceFileSystemHook(string name)
    {
        var field = typeof(WorkspaceContainedFileSystem).GetField(
            name,
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(field);
        Assert.Null(field!.GetValue(null));
        return field;
    }

    [DllImport("libc", EntryPoint = "geteuid")]
    private static extern uint GetEffectiveUserId();

    [DllImport("libc", EntryPoint = "mkfifo", SetLastError = true)]
    private static extern int CreateFifo(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string path,
        uint mode);

    private static FieldInfo StorageSeam(string name)
    {
        var field = typeof(WorkspaceContainedFileSystem).GetField(
            name,
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(field);
        return field!;
    }

    private static int LandlockAbiVersion()
    {
        var method = typeof(WorkspaceContainedFileSystem).GetMethod(
            "GetLandlockAbiVersion",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        return Assert.IsType<int>(method!.Invoke(null, null));
    }

    /// <summary>Reports Landlock unavailable (ABI 0) until disposed, selecting the fail-closed fallback.</summary>
    private static SeamScope LandlockUnavailable()
    {
        var field = StorageSeam("s_landlockAbiVersionOverride");
        field.SetValue(null, (Func<int>)(() => 0));
        return new SeamScope(field);
    }

    /// <summary>
    /// Runs <paramref name="unconfinedAction"/> on another thread at the confined-mutation boundary (the
    /// confined thread itself cannot mutate outside the root), once.
    /// </summary>
    private static SeamScope BeforeConfinedMutation(Action unconfinedAction)
    {
        var field = StorageSeam("s_beforeLinuxConfinedMutation");
        var fired = 0;
        field.SetValue(
            null,
            (Action<string>)(_ =>
            {
                if (Interlocked.Exchange(ref fired, 1) == 0)
                    Task.Run(unconfinedAction).GetAwaiter().GetResult();
            }));
        return new SeamScope(field);
    }

    [DllImport("libc", EntryPoint = "open", SetLastError = true)]
    private static extern int OpenPathDescriptor(string path, int flags);

    private sealed class SeamScope(FieldInfo field) : IDisposable
    {
        public void Dispose() => field.SetValue(null, null);
    }

    private sealed class LinuxMutationFixture : IDisposable
    {
        internal LinuxMutationFixture()
        {
            Sandbox = Path.Combine(
                Path.GetTempPath(),
                "mcp-bug139-eighteenth-mutation-" + Guid.NewGuid().ToString("N"));
            Root = Path.Combine(Sandbox, "workspace");
            LiveParent = Path.Combine(Root, "live");
            ExternalParent = Path.Combine(Sandbox, "moved-outside");
            Directory.CreateDirectory(LiveParent);
        }

        internal string Sandbox { get; }

        internal string Root { get; }

        internal string LiveParent { get; }

        internal string ExternalParent { get; }

        public void Dispose()
        {
            if (Directory.Exists(Sandbox))
                Directory.Delete(Sandbox, recursive: true);
        }
    }

    private sealed class SeparateLocalMountFixture : IDisposable
    {
        private static readonly HashSet<string> s_approvedFixtureTypes =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "btrfs",
                "ext4",
                "overlay",
                "tmpfs",
                "xfs",
            };

        private SeparateLocalMountFixture(string mountPoint, string fileSystemType, string testRoot)
        {
            MountPoint = mountPoint;
            FileSystemType = fileSystemType;
            TestRoot = testRoot;
        }

        internal string MountPoint { get; }

        internal string FileSystemType { get; }

        internal string TestRoot { get; }

        internal static SeparateLocalMountFixture Create()
        {
            Assert.NotEqual(0U, GetEffectiveUserId());
            foreach (var mount in ReadMounts()
                         .Where(static item =>
                             !string.Equals(item.MountPoint, "/", StringComparison.Ordinal) &&
                             s_approvedFixtureTypes.Contains(item.FileSystemType))
                         .OrderByDescending(static item =>
                             item.FileSystemType is "ext4" or "xfs" or "btrfs" or "overlay"))
            {
                var testRoot = Path.Combine(
                    mount.MountPoint,
                    "mcp-bug139-eighteenth-mount-" + Guid.NewGuid().ToString("N"));
                try
                {
                    Directory.CreateDirectory(testRoot);
                    return new SeparateLocalMountFixture(
                        mount.MountPoint,
                        mount.FileSystemType,
                        testRoot);
                }
                catch (UnauthorizedAccessException)
                {
                }
                catch (IOException)
                {
                }
            }

            throw new InvalidOperationException(
                "The Linux native gate requires a writable separate ext4, xfs, btrfs, overlay, " +
                "bind/container, or tmpfs mount.");
        }

        public void Dispose()
        {
            if (Directory.Exists(TestRoot))
                Directory.Delete(TestRoot, recursive: true);
        }

        private static IEnumerable<(string MountPoint, string FileSystemType)> ReadMounts()
        {
            foreach (var line in File.ReadLines("/proc/self/mountinfo"))
            {
                var separator = line.IndexOf(" - ", StringComparison.Ordinal);
                if (separator < 0)
                    continue;
                var prefix = line[..separator].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var suffix = line[(separator + 3)..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (prefix.Length < 5 || suffix.Length < 1)
                    continue;

                yield return (DecodeMountPath(prefix[4]), suffix[0]);
            }
        }

        private static string DecodeMountPath(string value) =>
            value
                .Replace(@"\040", " ", StringComparison.Ordinal)
                .Replace(@"\011", "\t", StringComparison.Ordinal)
                .Replace(@"\012", "\n", StringComparison.Ordinal)
                .Replace(@"\134", @"\", StringComparison.Ordinal);
    }
}
