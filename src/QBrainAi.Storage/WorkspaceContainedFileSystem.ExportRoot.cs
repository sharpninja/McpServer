using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using QBrainAi.Client;
using Microsoft.Win32.SafeHandles;

namespace QBrainAi.Support.Mcp.Storage;

public static partial class WorkspaceContainedFileSystem
{
    internal enum ExportRootPlatform
    {
        Windows,
        Linux,
        Unsupported,
    }

    // Test-only override remains private to prevent callers from bypassing platform policy.
    private static Func<ExportRootPlatform> s_platformOverrideForTests = DetectExportRootPlatform;
    // Test-only callback proves the provider-classification-to-authority-capture race fails
    // closed before a bounded worker is admitted.
    private static Action? s_afterLinuxExportRootPolicyClassification = null;



    /// <summary>
    /// Pins one requirements-export root and provides all subsequent contained operations through
    /// that lifetime authority.
    /// </summary>
    /// <param name="rootPath">Physical export-root path.</param>
    /// <returns>A disposable root capability.</returns>
    /// <exception cref="PlatformNotSupportedException">
    /// The current platform has no contained export mutation implementation.
    /// </exception>
    public static WorkspaceExportRoot OpenExportRoot(string rootPath)
    {
        var platform = s_platformOverrideForTests();
        if (platform == ExportRootPlatform.Unsupported)
        {
            throw new PlatformNotSupportedException(
                "Same-object contained filesystem mutation is implemented for Windows and Linux.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        var normalizedRoot = Path.GetFullPath(rootPath);
        if (platform == ExportRootPlatform.Windows && OperatingSystem.IsWindows())
            return OpenWindowsExportRoot(normalizedRoot);
        if (platform == ExportRootPlatform.Linux && OperatingSystem.IsLinux())
            return OpenLinuxExportRoot(normalizedRoot);

        throw new PlatformNotSupportedException(
            "Same-object contained filesystem mutation is implemented for Windows and Linux.");
    }

    private static ExportRootPlatform DetectExportRootPlatform() =>
        OperatingSystem.IsWindows()
            ? ExportRootPlatform.Windows
            : OperatingSystem.IsLinux()
                ? ExportRootPlatform.Linux
                : ExportRootPlatform.Unsupported;

    [SupportedOSPlatform("windows")]
    private static WorkspaceExportRoot OpenWindowsExportRoot(string rootPath)
    {
        Directory.CreateDirectory(rootPath);
        return new WorkspaceExportRoot(
            rootPath,
            ExportRootPlatform.Windows,
            OpenWindowsDirectory(rootPath),
            default);
    }

    [SupportedOSPlatform("linux")]
    private static WorkspaceExportRoot OpenLinuxExportRoot(string rootPath)
    {
        // Directory capture classifies the provider and resolves every component through the
        // policy-approved mount descriptor in one authority acquisition. In particular, no
        // pathname-based policy decision is reused to open rootPath later.
        SafeFileHandle rootHandle;
        try
        {
            rootHandle = LinuxPhysicalPathResolver.OpenDirectoryNoCrossDevice(
                rootPath,
                Volatile.Read(ref s_afterLinuxExportRootPolicyClassification));
        }
        catch (DirectoryNotFoundException) when (GetLandlockAbiVersion() >= 1)
        {
            // BUG-BASELINE-001: create the missing root on a thread Landlock confines to its deepest
            // existing ancestor, then capture it exactly as an existing root is captured.
            CreateLinuxExportRootConfined(rootPath);
            rootHandle = LinuxPhysicalPathResolver.OpenDirectoryNoCrossDevice(
                rootPath,
                Volatile.Read(ref s_afterLinuxExportRootPolicyClassification));
        }
        try
        {
            var rootIdentity = FStatUnix(rootHandle.DangerousGetHandle().ToInt32());
            EnsureLinuxDirectory(rootIdentity, rootPath);
            return new WorkspaceExportRoot(
                rootPath,
                ExportRootPlatform.Linux,
                rootHandle,
                rootIdentity);
        }
        catch
        {
            rootHandle.Dispose();
            throw;
        }
    }

    /// <summary>
    /// BUG-BASELINE-001 (TR-MCP-USECASE-017): creates a missing Linux export root and its missing
    /// ancestors beneath the deepest existing directory, which is pinned from its approved mount
    /// without following links or crossing mounts; creation runs Landlock-confined to that directory.
    /// </summary>
    [SupportedOSPlatform("linux")]
    private static void CreateLinuxExportRootConfined(string rootPath)
    {
        using var ancestor = LinuxPhysicalPathResolver.OpenDeepestExistingDirectoryNoCrossDevice(
            rootPath,
            "Requirements export root creation",
            out var missingTail);
        if (missingTail.Length == 0)
            return;

        var ancestorIdentity = FStatUnix(ancestor.DangerousGetHandle().ToInt32());
        EnsureLinuxDirectory(ancestorIdentity, rootPath);
        CreateLinuxDirectoriesConfined(ancestor, ancestorIdentity, string.Empty, missingTail.Split('/'));
    }

    /// <summary>
    /// A single-lifetime authority over one export root. Linux operations are descriptor-relative
    /// and never reopen the root pathname after it is pinned.
    /// </summary>
    public sealed class WorkspaceExportRoot : IDisposable
    {
        private readonly ExportRootPlatform _platform;
        private readonly SafeFileHandle _rootHandle;
        private readonly UnixStatx _linuxRootIdentity;
        private bool _disposed;

        internal WorkspaceExportRoot(
            string rootPath,
            ExportRootPlatform platform,
            SafeFileHandle rootHandle,
            UnixStatx linuxRootIdentity)
        {
            RootPath = rootPath;
            _platform = platform;
            _rootHandle = rootHandle;
            _linuxRootIdentity = linuxRootIdentity;
        }

        /// <summary>Gets the lexical physical path supplied when this root was opened.</summary>
        public string RootPath { get; }

        /// <summary>Gets whether root-relative name comparison is case-sensitive.</summary>
        public bool IsCaseSensitive => _platform == ExportRootPlatform.Linux;

        /// <summary>
        /// BUG-BASELINE-001 (TR-MCP-USECASE-017): gets whether entries below a subdirectory of this root can be
        /// deleted safely; always on Windows, and on Linux only when Landlock confines the deletion to this root.
        /// </summary>
        public bool SupportsNestedDeletion =>
            _platform != ExportRootPlatform.Linux || GetLandlockAbiVersion() >= 1;

        /// <summary>Gets the filesystem-appropriate root-relative name comparer.</summary>
        public StringComparer NameComparer =>
            IsCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;

        /// <summary>
        /// Resolves a validated root-relative display path without consulting the filesystem.
        /// </summary>
        /// <param name="relativePath">Contained root-relative path.</param>
        /// <returns>The normalized full display path.</returns>
        public string ResolvePath(string relativePath)
        {
            ThrowIfDisposed();
            var normalized = NormalizeRelativePath(relativePath);
            var fullPath = Path.GetFullPath(
                Path.Combine(RootPath, normalized.Replace('/', Path.DirectorySeparatorChar)));
            if (!IsLexicallyContained(RootPath, fullPath))
                throw new IOException($"Requirements export path escapes its root: {relativePath}");
            return fullPath;
        }

        /// <summary>
        /// Converts a traversal result's absolute display path to a validated export-root-relative
        /// path without rebasing it beneath this root.
        /// </summary>
        /// <param name="fullPath">An absolute traversal display path beneath this export root.</param>
        /// <returns>The normalized root-relative path.</returns>
        public string GetRelativePathFromFullPath(string fullPath)
        {
            ThrowIfDisposed();
            ArgumentException.ThrowIfNullOrWhiteSpace(fullPath);
            var normalizedFullPath = Path.GetFullPath(fullPath);
            if (!IsLexicallyContained(RootPath, normalizedFullPath))
            {
                throw new IOException(
                    $"Traversal result is outside the requirements export root: {fullPath}");
            }

            return NormalizeRelativePath(Path.GetRelativePath(RootPath, normalizedFullPath));
        }


        /// <summary>Creates a contained directory chain through this root capability.</summary>
        /// <param name="relativeDirectory">Contained root-relative directory.</param>
        public void EnsureDirectory(string relativeDirectory)
        {
            ThrowIfDisposed();
            var relative = NormalizeRelativePath(relativeDirectory);
            if (OperatingSystem.IsLinux())
            {
                EnsureLinuxDirectoryRooted(_rootHandle, _linuxRootIdentity, relative);
                return;
            }

            if (OperatingSystem.IsWindows())
            {
                EnsureWindowsDirectory(RootPath, ResolvePath(relative), _rootHandle);
                return;
            }

            throw new PlatformNotSupportedException(
                "Same-object contained directory creation is implemented for Windows and Linux.");
        }

        /// <summary>
        /// Writes text and final metadata through this root capability.
        /// </summary>
        public async Task WriteTextFileAsync(
            string relativePath,
            string content,
            Encoding encoding,
            DateTime lastWriteTimeUtc,
            bool readOnly,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(content);
            ArgumentNullException.ThrowIfNull(encoding);
            ThrowIfDisposed();
            cancellationToken.ThrowIfCancellationRequested();

            var relative = NormalizeRelativePath(relativePath);
            var parent = GetRelativeParent(relative);
            if (!string.Equals(parent, ".", StringComparison.Ordinal))
                EnsureDirectory(parent);

            if (OperatingSystem.IsLinux())
            {
                _ = SetFileReadOnlyIfExists(relative, readOnly: false);
                await using var target = OpenLinuxContainedFileRooted(
                    _rootHandle,
                    _linuxRootIdentity,
                    relative,
                    ResolvePath(relative),
                    write: true,
                    asyncHandle: false);
                target.SetLength(0);
                var bytes = encoding.GetBytes(content);
                await target.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
                await target.FlushAsync(cancellationToken).ConfigureAwait(false);
                File.SetLastWriteTimeUtc(target.SafeFileHandle, lastWriteTimeUtc);
                SetFileReadOnly(target.SafeFileHandle, readOnly);
                return;
            }

            _ = SetFileReadOnlyIfExists(relative, readOnly: false);
            await using var windowsTarget = OpenFileForWrite(relative);
            windowsTarget.SetLength(0);
            var windowsBytes = encoding.GetBytes(content);
            await windowsTarget.WriteAsync(windowsBytes, cancellationToken).ConfigureAwait(false);
            await windowsTarget.FlushAsync(cancellationToken).ConfigureAwait(false);
            File.SetLastWriteTimeUtc(windowsTarget.SafeFileHandle, lastWriteTimeUtc);
            SetFileReadOnly(windowsTarget.SafeFileHandle, readOnly);
        }

        /// <summary>Opens one contained existing file through this root authority.</summary>
        /// <param name="relativePath">Contained root-relative file path.</param>
        /// <returns>An owning stream over the validated regular file.</returns>
        public FileStream OpenFileForRead(string relativePath)
        {
            ThrowIfDisposed();
            var relative = NormalizeRelativePath(relativePath);
            return OperatingSystem.IsLinux()
                ? OpenLinuxContainedFileRooted(
                    _rootHandle,
                    _linuxRootIdentity,
                    relative,
                    ResolvePath(relative),
                    write: false,
                    asyncHandle: false)
                : OperatingSystem.IsWindows()
                    ? OpenWindowsFile(
                        RootPath,
                        ResolvePath(relative),
                        write: false,
                        rootAuthority: _rootHandle)
                    : throw new PlatformNotSupportedException(
                        "Same-object contained file I/O is implemented for Windows and Linux.");
        }

        /// <summary>Opens or creates one contained file through this root authority.</summary>
        /// <param name="relativePath">Contained root-relative file path.</param>
        /// <returns>An owning stream over the validated regular file.</returns>
        public FileStream OpenFileForWrite(string relativePath)
        {
            ThrowIfDisposed();
            var relative = NormalizeRelativePath(relativePath);
            return OperatingSystem.IsLinux()
                ? OpenLinuxContainedFileRooted(
                    _rootHandle,
                    _linuxRootIdentity,
                    relative,
                    ResolvePath(relative),
                    write: true,
                    asyncHandle: false)
                : OperatingSystem.IsWindows()
                    ? OpenWindowsFile(
                        RootPath,
                        ResolvePath(relative),
                        write: true,
                        rootAuthority: _rootHandle)
                    : throw new PlatformNotSupportedException(
                        "Same-object contained file I/O is implemented for Windows and Linux.");
        }


        /// <summary>
        /// Opens one contained ordinary file for bounded asynchronous reading.
        /// </summary>
        public Task<FileStream> OpenFileForReadBoundedAsync(
            string relativePath,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            var relative = NormalizeRelativePath(relativePath);
            if (OperatingSystem.IsLinux())
                return OpenLinuxFileForReadBoundedAsync(relative, timeout, cancellationToken);

            return WorkspaceContainedFileSystem.OpenFileForReadBoundedAsync(
                RootPath,
                ResolvePath(relative),
                timeout,
                cancellationToken);
        }

        [SupportedOSPlatform("linux")]
        private Task<FileStream> OpenLinuxFileForReadBoundedAsync(
            string relativePath,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            ValidateFiniteTimeout(timeout);
            return RunBoundedTraversalWorkerAsync(
                state =>
                {
                    state.ThrowIfCancellationRequested();
                    return OpenLinuxContainedFileRooted(
                        _rootHandle,
                        _linuxRootIdentity,
                        relativePath,
                        ResolvePath(relativePath),
                        write: false,
                        asyncHandle: false);
                },
                timeout,
                cancellationToken,
                pinnedHandle: _rootHandle);
        }

        /// <summary>
        /// Captures timestamp, readonly attributes, and Linux permission metadata through this
        /// contained root authority.
        /// </summary>
        public async Task<WorkspaceExportFileMetadata> GetFileMetadataAsync(
            string relativePath,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            await using var source = await OpenFileForReadBoundedAsync(
                    relativePath,
                    timeout,
                    cancellationToken)
                .ConfigureAwait(false);
            ushort? unixMode = null;
            if (OperatingSystem.IsLinux())
                unixMode = FStatUnix(source.SafeFileHandle.DangerousGetHandle().ToInt32()).Mode;
            return new WorkspaceExportFileMetadata(
                File.GetLastWriteTimeUtc(source.SafeFileHandle),
                File.GetAttributes(source.SafeFileHandle),
                unixMode);
        }

        /// <summary>
        /// Captures metadata from an already-opened contained file without reopening its pathname.
        /// </summary>
        /// <param name="stream">File stream opened through this capability.</param>
        /// <returns>Timestamp, attributes, and Linux mode for the exact open file.</returns>
        public WorkspaceExportFileMetadata GetFileMetadata(FileStream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);
            ThrowIfDisposed();
            ushort? unixMode = null;
            if (OperatingSystem.IsLinux())
            {
                EnsureLinuxDescriptorOnRootMount(
                    _linuxRootIdentity,
                    stream.SafeFileHandle,
                    "open export file");
                unixMode = FStatUnix(stream.SafeFileHandle.DangerousGetHandle().ToInt32()).Mode;
            }

            return new WorkspaceExportFileMetadata(
                File.GetLastWriteTimeUtc(stream.SafeFileHandle),
                File.GetAttributes(stream.SafeFileHandle),
                unixMode);
        }

        /// <summary>
        /// Restores content and metadata for one existing export file through this root authority.
        /// </summary>
        public async Task RestoreFileAsync(
            string relativePath,
            Stream content,
            WorkspaceExportFileMetadata metadata,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(content);
            ArgumentNullException.ThrowIfNull(metadata);
            ThrowIfDisposed();
            var relative = NormalizeRelativePath(relativePath);
            var parent = GetRelativeParent(relative);
            if (!string.Equals(parent, ".", StringComparison.Ordinal))
                EnsureDirectory(parent);
            _ = SetFileReadOnlyIfExists(relative, readOnly: false);

            await using var target = OpenFileForWrite(relative);
            target.SetLength(0);
            await content.CopyToAsync(target, 64 * 1024, cancellationToken).ConfigureAwait(false);
            await target.FlushAsync(cancellationToken).ConfigureAwait(false);
            File.SetLastWriteTimeUtc(target.SafeFileHandle, metadata.LastWriteTimeUtc);
            File.SetAttributes(target.SafeFileHandle, metadata.Attributes);
            if (OperatingSystem.IsLinux() && metadata.UnixMode is ushort unixMode)
            {
                if (FchmodUnix(target.SafeFileHandle.DangerousGetHandle().ToInt32(), unixMode) != 0)
                    throw CreateUnixIOException($"Unable to restore Linux permission metadata for '{relative}'.");
            }
        }

        /// <summary>Sets readonly state for one existing contained ordinary file.</summary>
        public bool SetFileReadOnlyIfExists(string relativePath, bool readOnly)
        {
            ThrowIfDisposed();
            var relative = NormalizeRelativePath(relativePath);
            try
            {
                using var target = OperatingSystem.IsLinux()
                    ? OpenLinuxContainedFileRooted(
                        _rootHandle,
                        _linuxRootIdentity,
                        relative,
                        ResolvePath(relative),
                        write: false,
                        asyncHandle: false)
                    : OperatingSystem.IsWindows()
                        ? OpenWindowsFile(
                            RootPath,
                            ResolvePath(relative),
                            write: false,
                            metadataWrite: true,
                            rootAuthority: _rootHandle)
                        : throw new PlatformNotSupportedException(
                            "Same-object contained metadata mutation is implemented for Windows and Linux.");
                SetFileReadOnly(target.SafeFileHandle, readOnly);
                return true;
            }
            catch (FileNotFoundException)
            {
                return false;
            }
            catch (DirectoryNotFoundException)
            {
                return false;
            }
            catch (IOException exception) when (IsMissingContainedEntry(exception))
            {
                return false;
            }
        }

        /// <summary>Deletes one contained ordinary file through this root capability.</summary>
        public bool DeleteFileIfExists(string relativePath) =>
            DeleteContainedEntry(relativePath, directory: false);

        /// <summary>Deletes one contained empty directory through this root capability.</summary>
        public bool DeleteEmptyDirectory(string relativePath) =>
            DeleteContainedEntry(relativePath, directory: true);

        /// <summary>
        /// Enumerates contained regular files with bounded worker admission.
        /// </summary>
        public async Task<IReadOnlyList<WorkspaceContainedFileEntry>> EnumerateFilesBoundedAsync(
            string relativeDirectory,
            WorkspaceTraversalLimits limits,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(limits);
            limits.Validate();
            var relative = NormalizeRelativePath(relativeDirectory);
            if (OperatingSystem.IsLinux())
            {
                var result = await EnumerateLinuxBoundedAsync(
                        relative,
                        limits,
                        timeout,
                        cancellationToken)
                    .ConfigureAwait(false);
                return result.Files;
            }

            return await WorkspaceContainedFileSystem.EnumerateFilesBoundedAsync(
                    RootPath,
                    ResolvePath(relative),
                    limits,
                    timeout,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Enumerates contained directories in deterministic post-order with bounded worker admission.
        /// </summary>
        public async Task<IReadOnlyList<string>> EnumerateDirectoriesBoundedAsync(
            string relativeDirectory,
            WorkspaceTraversalLimits limits,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(limits);
            limits.Validate();
            var relative = NormalizeRelativePath(relativeDirectory);
            if (OperatingSystem.IsLinux())
            {
                var result = await EnumerateLinuxBoundedAsync(
                        relative,
                        limits,
                        timeout,
                        cancellationToken)
                    .ConfigureAwait(false);
                return result.Directories;
            }

            return await WorkspaceContainedFileSystem.EnumerateDirectoriesBoundedAsync(
                    RootPath,
                    ResolvePath(relative),
                    limits,
                    timeout,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _rootHandle.Dispose();
        }

        [SupportedOSPlatform("linux")]
        private async Task<WorkspacePhysicalTraversalResult> EnumerateLinuxBoundedAsync(
            string relativeDirectory,
            WorkspaceTraversalLimits limits,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            ValidateFiniteTimeout(timeout);
            var directory = ResolvePath(relativeDirectory);
            return await RunBoundedTraversalWorkerAsync(
                    state =>
                    {
                        var files = new List<WorkspaceContainedFileEntry>();
                        var directories = new List<string>();
                        var budget = new WorkspaceTraversalBudget(limits);
                        try
                        {
                            EnumerateLinuxFilesFromPinnedRoot(
                                _rootHandle,
                                _linuxRootIdentity,
                                RootPath,
                                directory,
                                limits,
                                budget,
                                files,
                                directories,
                                state);
                        }
                        catch (DirectoryNotFoundException) when (!LinuxStartDirectoryExists(relativeDirectory))
                        {
                            // BUG-BASELINE-001: parity with the Windows traversal, which treats a missing
                            // path as empty: an export directory that does not exist yet has no entries.
                            return CompletePhysicalTraversal(RootPath, [], []);
                        }

                        return CompletePhysicalTraversal(RootPath, files, directories);
                    },
                    timeout,
                    cancellationToken,
                    pinnedHandle: _rootHandle)
                .ConfigureAwait(false);
        }

        private bool DeleteContainedEntry(string relativePath, bool directory)
        {
            ThrowIfDisposed();
            var relative = NormalizeRelativePath(relativePath);
            if (string.Equals(relative, ".", StringComparison.Ordinal))
                throw new IOException("The requirements export root cannot be deleted.");

            if (OperatingSystem.IsWindows())
            {
                return DeleteWindowsEntry(
                    RootPath,
                    ResolvePath(relative),
                    directory,
                    _rootHandle);
            }

            if (!OperatingSystem.IsLinux())
            {
                throw new PlatformNotSupportedException(
                    "Same-object contained deletion is implemented for Windows and Linux.");
            }

            // unlinkat has no RESOLVE_IN_ROOT equivalent. A pre-opened nested parent could be
            // renamed outside the root between validation and mutation, so nested cleanup runs only
            // on a thread Landlock confines to the root (BUG-BASELINE-001); without Landlock it is
            // rejected before mutation.
            if (relative.Contains('/') && GetLandlockAbiVersion() >= 1)
                return DeleteNestedLinuxEntryConfined(relative, directory);
            if (relative.Contains('/'))
            {
                throw new NotSupportedException(
                    $"Linux requirements export cleanup rejects nested mutation without a kernel-atomic root-relative delete: '{relative}'.");
            }

            if (UnlinkAtUnix(
                    _rootHandle.DangerousGetHandle().ToInt32(),
                    relative,
                    directory ? UnixAtRemoveDirectory : 0) == 0)
            {
                return true;
            }

            var error = Marshal.GetLastPInvokeError();
            if (error == UnixNoEntry || (directory && error == UnixDirectoryNotEmpty))
                return false;
            throw CreateUnixIOException($"Unable to delete contained entry '{relative}'.", error);
        }

        /// <summary>True when the traversal start directory resolves beneath the pinned root (Linux).</summary>
        [SupportedOSPlatform("linux")]
        private bool LinuxStartDirectoryExists(string relativeDirectory)
        {
            var relative = NormalizeRelativePath(relativeDirectory);
            if (string.Equals(relative, ".", StringComparison.Ordinal))
                return true;
            try
            {
                using var start = OpenLinuxPathBeneathRoot(
                    _rootHandle,
                    relative,
                    LinuxOpenPath | LinuxOpenDirectory | LinuxOpenNoFollow | LinuxOpenCloseOnExec,
                    mode: 0,
                    relative);
                return true;
            }
            catch (DirectoryNotFoundException)
            {
                return false;
            }
        }

        /// <summary>
        /// BUG-BASELINE-001 (TR-MCP-USECASE-017): deletes a nested entry on a Landlock-confined thread
        /// through its pinned parent after verifying the target's kind and identity; a missing parent
        /// or target returns false, as a missing direct leaf does.
        /// </summary>
        [SupportedOSPlatform("linux")]
        private bool DeleteNestedLinuxEntryConfined(string relative, bool directory)
        {
            const ulong directoryFlags = LinuxOpenReadOnly | LinuxOpenDirectory | LinuxOpenNoFollow | LinuxOpenCloseOnExec;
            var parentRelative = GetRelativeParent(relative);
            var leaf = relative[(relative.LastIndexOf('/') + 1)..];
            return RunLandlockConfined(
                _rootHandle,
                () =>
                {
                    SafeFileHandle parent;
                    try
                    {
                        parent = OpenLinuxPathBeneathRoot(_rootHandle, parentRelative, directoryFlags, mode: 0, parentRelative);
                    }
                    catch (DirectoryNotFoundException)
                    {
                        return false;
                    }

                    using (parent)
                    {
                        var parentDescriptor = parent.DangerousGetHandle().ToInt32();
                        EnsureLinuxDirectory(FStatUnix(parentDescriptor), parentRelative);
                        EnsureLinuxDescriptorOnRootMount(_linuxRootIdentity, parent, parentRelative);
                        SafeFileHandle target;
                        try
                        {
                            target = OpenLinuxPathBeneathRoot(
                                _rootHandle,
                                relative,
                                LinuxOpenPath | LinuxOpenNoFollow | LinuxOpenCloseOnExec,
                                mode: 0,
                                relative);
                        }
                        catch (DirectoryNotFoundException)
                        {
                            return false;
                        }

                        using (target)
                        {
                            var targetIdentity = FStatUnix(target.DangerousGetHandle().ToInt32());
                            if (directory)
                                EnsureLinuxDirectory(targetIdentity, relative);
                            else
                                EnsureLinuxRegularFile(targetIdentity, relative);
                            if (StatxUnix(parentDescriptor, leaf, UnixAtSymlinkNoFollow, UnixStatxBasicStats, out var entry) != 0)
                            {
                                var statError = Marshal.GetLastPInvokeError();
                                if (statError == UnixNoEntry)
                                    return false;
                                throw CreateUnixIOException($"Unable to inspect contained entry '{relative}'.", statError);
                            }

                            EnsureSameLinuxObject(targetIdentity, entry, relative);
                        }

                        InvokeBeforeLinuxConfinedMutation(relative);
                        if (UnlinkAtUnix(parentDescriptor, leaf, directory ? UnixAtRemoveDirectory : 0) == 0)
                            return true;

                        var error = Marshal.GetLastPInvokeError();
                        if (error == UnixNoEntry || (directory && error == UnixDirectoryNotEmpty))
                            return false;
                        throw CreateUnixIOException($"Unable to delete contained entry '{relative}'.", error);
                    }
                });
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }

        private static string NormalizeRelativePath(string relativePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
            var normalized = relativePath.Replace('\\', '/').Trim('/');
            if (normalized.Length == 0 || normalized == ".")
                return ".";
            var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Any(segment => segment is "." or ".."))
                throw new IOException($"Invalid root-relative export path: {relativePath}");
            return string.Join('/', segments);
        }

        private static string GetRelativeParent(string relativePath)
        {
            var separator = relativePath.LastIndexOf('/');
            return separator < 0 ? "." : relativePath[..separator];
        }
    }

    /// <summary>Snapshot metadata restored with requirements export content.</summary>
    public sealed record WorkspaceExportFileMetadata(
        DateTime LastWriteTimeUtc,
        FileAttributes Attributes,
        ushort? UnixMode);

    [DllImport("libc", EntryPoint = "fchmod", SetLastError = true)]
    private static extern int FchmodUnix(int descriptor, uint mode);
}
