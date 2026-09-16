using System.ComponentModel;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using McpServer.Client;
using Microsoft.Win32.SafeHandles;

namespace McpServer.Support.Mcp.Storage;

/// <summary>Describes one regular file discovered through a bounded contained traversal.</summary>
public sealed record WorkspaceContainedFileEntry
{
    /// <summary>Initializes one contained file entry.</summary>
    /// <param name="fullPath">Lexical full path beneath the caller's root.</param>
    /// <param name="length">Length read from the opened, validated file descriptor.</param>
    public WorkspaceContainedFileEntry(string fullPath, long length)
    {
        FullPath = fullPath;
        Length = length;
    }

    /// <summary>Gets the lexical full path beneath the caller's root.</summary>
    public string FullPath { get; }

    /// <summary>Gets the length read from the opened, validated file descriptor.</summary>
    public long Length { get; }
}

/// <summary>Bounded descriptor-anchored traversal operations.</summary>
public static partial class WorkspaceContainedFileSystem
{
    private const uint FileListDirectory = 0x00000001;
    private const int FileFullDirectoryInfoClass = 14;
    private const int ErrorNoMoreFiles = 18;
    private const int ErrorOperationAborted = 995;
    private const long OpenAt2SystemCall = 437;
    private const ulong LinuxResolveNoCrossDevice = 0x01;
    private const ulong LinuxResolveNoMagicLinks = 0x02;
    private const ulong LinuxResolveNoSymbolicLinks = 0x04;
    private const ulong LinuxResolveInRoot = 0x10;
    private const ulong LinuxOpenReadOnly = 0;
    private const ulong LinuxOpenReadWrite = 0x02;
    private const ulong LinuxOpenCreate = 0x40;
    private const ulong LinuxOpenExclusive = 0x80;
    private const ulong LinuxOpenNonBlock = 0x800;
    private const ulong LinuxOpenDirectory = 0x10000;
    private const ulong LinuxOpenNoFollow = 0x20000;
    private const ulong LinuxOpenCloseOnExec = 0x80000;
    private const ulong LinuxOpenPath = 0x200000;
    private const int LinuxErrorNoEntry = 2;
    private const int LinuxErrorCrossDevice = 18;
    private const int LinuxErrorNotDirectory = 20;
    private const int LinuxErrorNotImplemented = 38;
    private const int LinuxErrorTooManyLinks = 40;
    private const int LinuxDirectoryEntryHeaderSize = 19;
    private const int LinuxDirectoryBufferSize = 64 * 1024;
    private const int MaxTraversalWorkerCount = 4;
    private static readonly TimeSpan s_defaultTraversalTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan s_traversalCleanupTimeout = TimeSpan.FromMilliseconds(250);
    private static Action? s_afterTraversalRootPinned = null;
    private static Action? s_beforeTraversalDirectoryRead = null;
    private static Action<string>? s_beforeTraversalFileOpen = null;
    private static Action? s_beforeLinuxRootAnchoredFileCreate = null;
    private static int s_activeTraversalWorkerCount;

    /// <summary>Gets the number of bounded traversal workers whose native thread is still alive.</summary>
    internal static int ActiveTraversalWorkerCount => Volatile.Read(ref s_activeTraversalWorkerCount);

    /// <summary>
    /// Enumerates regular files from a physically contained directory on an isolated native worker.
    /// The public operation has a finite deadline; pathname aliases and mount transitions are never
    /// followed after the workspace root descriptor has been pinned.
    /// </summary>
    /// <param name="rootPath">Workspace root that defines the physical containment authority.</param>
    /// <param name="directoryPath">Contained directory to enumerate recursively.</param>
    /// <param name="limits">Count, byte, and depth bounds for this traversal.</param>
    /// <param name="timeout">Finite public deadline.</param>
    /// <param name="cancellationToken">Exact caller cancellation token.</param>
    /// <returns>Validated regular-file entries and descriptor-derived lengths.</returns>
    public static async Task<IReadOnlyList<WorkspaceContainedFileEntry>> EnumerateFilesBoundedAsync(
        string rootPath,
        string directoryPath,
        WorkspaceTraversalLimits limits,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(limits);
        ValidateTraversalArguments(
            rootPath,
            directoryPath,
            limits,
            new WorkspaceTraversalBudget(limits));
        ValidateFiniteTimeout(timeout);

        var root = Path.GetFullPath(rootPath);
        var directory = Path.GetFullPath(directoryPath);
        if (OperatingSystem.IsLinux())
        {
            using var exportRoot = OpenExportRoot(root);
            Volatile.Read(ref s_afterTraversalRootPinned)?.Invoke();
            var relativeDirectory = Path.GetRelativePath(exportRoot.RootPath, directory);
            return await exportRoot.EnumerateFilesBoundedAsync(
                    relativeDirectory,
                    limits,
                    timeout,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        return await RunBoundedTraversalWorkerAsync(
                state => TraversePhysical(root, directory, limits, state).Files,
                timeout,
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Enumerates ordinary contained directories in post-order on an isolated bounded worker.
    /// </summary>
    /// <param name="rootPath">Workspace root that defines the physical containment authority.</param>
    /// <param name="directoryPath">Contained directory to enumerate recursively.</param>
    /// <param name="limits">Count, byte, and depth bounds for this traversal.</param>
    /// <param name="timeout">Finite public deadline.</param>
    /// <param name="cancellationToken">Exact caller cancellation token.</param>
    /// <returns>Contained ordinary directories in post-order.</returns>
    public static async Task<IReadOnlyList<string>> EnumerateDirectoriesBoundedAsync(
        string rootPath,
        string directoryPath,
        WorkspaceTraversalLimits limits,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(limits);
        ValidateTraversalArguments(
            rootPath,
            directoryPath,
            limits,
            new WorkspaceTraversalBudget(limits));
        ValidateFiniteTimeout(timeout);

        var root = Path.GetFullPath(rootPath);
        var directory = Path.GetFullPath(directoryPath);
        if (OperatingSystem.IsLinux())
        {
            using var exportRoot = OpenExportRoot(root);
            Volatile.Read(ref s_afterTraversalRootPinned)?.Invoke();
            var relativeDirectory = Path.GetRelativePath(exportRoot.RootPath, directory);
            return await exportRoot.EnumerateDirectoriesBoundedAsync(
                    relativeDirectory,
                    limits,
                    timeout,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        return await RunBoundedTraversalWorkerAsync(
                state => TraversePhysical(root, directory, limits, state).Directories,
                timeout,
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Opens one existing regular file through a bounded worker after pinning the workspace root.
    /// The returned stream owns the exact validated handle and no worker can touch it after publish.
    /// </summary>
    /// <param name="rootPath">Workspace root that defines the physical containment authority.</param>
    /// <param name="filePath">Contained existing file.</param>
    /// <param name="timeout">Finite public deadline.</param>
    /// <param name="cancellationToken">Exact caller cancellation token.</param>
    /// <returns>A stream over the exact contained file.</returns>
    public static async Task<FileStream> OpenFileForReadBoundedAsync(
        string rootPath,
        string filePath,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var (root, file) = ValidateContainedPaths(rootPath, filePath);
        ValidateFiniteTimeout(timeout);
        if (OperatingSystem.IsLinux())
        {
            using var exportRoot = OpenExportRoot(root);
            var relativeFile = Path.GetRelativePath(exportRoot.RootPath, file);
            return await exportRoot.OpenFileForReadBoundedAsync(
                    relativeFile,
                    timeout,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        return await RunBoundedTraversalWorkerAsync(
                state =>
                {
                    state.ThrowIfCancellationRequested();
                    return OpenContainedFileForBoundedRead(root, file);
                },
                timeout,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private static FileStream OpenContainedFileForBoundedRead(string root, string file)
    {
        if (OperatingSystem.IsWindows())
            return OpenWindowsFile(root, file, write: false);

        throw new PlatformNotSupportedException(
            "Bounded contained file opening is implemented for Windows and Linux.");
    }

    [SupportedOSPlatform("linux")]
    private static FileStream OpenLinuxContainedFileRooted(
        SafeFileHandle rootHandle,
        UnixStatx rootIdentity,
        string relativeFile,
        string displayPath,
        bool write,
        bool asyncHandle)
    {
        var flags = (write ? LinuxOpenReadWrite : LinuxOpenReadOnly) |
                    LinuxOpenNoFollow |
                    LinuxOpenNonBlock |
                    LinuxOpenCloseOnExec;
        SafeFileHandle handle;
        try
        {
            handle = OpenLinuxPathBeneathRoot(
                rootHandle,
                relativeFile,
                flags,
                mode: 0,
                displayPath);
        }
        catch (DirectoryNotFoundException) when (write)
        {
            Volatile.Read(ref s_beforeLinuxRootAnchoredFileCreate)?.Invoke();
            handle = OpenLinuxPathBeneathRoot(
                rootHandle,
                relativeFile,
                flags | LinuxOpenCreate | LinuxOpenExclusive,
                UnixOwnerReadWrite,
                displayPath);
        }

        try
        {
            EnsureLinuxRegularFile(FStatUnix(handle.DangerousGetHandle().ToInt32()), displayPath);
            EnsureLinuxDescriptorOnRootMount(rootIdentity, handle, displayPath);
            return new FileStream(
                handle,
                write ? FileAccess.ReadWrite : FileAccess.Read,
                bufferSize: 64 * 1024,
                isAsync: asyncHandle);
        }
        catch
        {
            handle.Dispose();
            throw;
        }
    }

    [SupportedOSPlatform("linux")]
    private static void EnsureLinuxDirectoryRooted(
        SafeFileHandle rootHandle,
        UnixStatx rootIdentity,
        string relativeDirectory)
    {
        if (string.Equals(relativeDirectory, ".", StringComparison.Ordinal))
            return;

        // openat2 can atomically resolve and create an ordinary file beneath this root, but Linux
        // has no equivalent descriptor-relative directory-create operation with RESOLVE_IN_ROOT.
        // A validate-then-mkdirat sequence lets an attacker move a pinned parent outside the root.
        // Therefore a Linux export may use only directories that already resolve under the pinned
        // root; it fails before any externally visible directory mutation.
        var current = string.Empty;
        foreach (var segment in SplitContainedSegments(relativeDirectory))
        {
            var child = string.IsNullOrEmpty(current) ? segment : $"{current}/{segment}";
            using var existing = OpenLinuxPathBeneathRoot(
                rootHandle,
                child,
                LinuxOpenReadOnly |
                LinuxOpenDirectory |
                LinuxOpenNoFollow |
                LinuxOpenCloseOnExec,
                mode: 0,
                child);
            EnsureLinuxDirectory(FStatUnix(existing.DangerousGetHandle().ToInt32()), child);
            EnsureLinuxDescriptorOnRootMount(rootIdentity, existing, child);
            current = child;
        }
    }

    private static WorkspacePhysicalTraversalResult TraversePhysical(
        string root,
        string directory,
        WorkspaceTraversalLimits limits,
        WorkspaceTraversalCancellationState cancellationState)
    {
        var entries = new List<WorkspaceContainedFileEntry>();
        var directories = new List<string>();
        var budget = new WorkspaceTraversalBudget(limits);
        try
        {
            if (OperatingSystem.IsWindows())
            {
                EnumerateWindowsFiles(
                    root,
                    directory,
                    limits,
                    budget,
                    entries,
                    directories,
                    cancellationState);
                return CompletePhysicalTraversal(root, entries, directories);
            }

        }
        catch (Exception exception) when (IsMissingTraversalPath(exception))
        {
            return CompletePhysicalTraversal(root, entries, directories);
        }

        throw new PlatformNotSupportedException(
            "Bounded contained traversal is implemented for Windows and Linux.");
    }

    private static WorkspacePhysicalTraversalResult CompletePhysicalTraversal(
        string root,
        List<WorkspaceContainedFileEntry> entries,
        List<string> directories)
    {
        entries.Sort(
            (left, right) => StringComparer.Ordinal.Compare(
                NormalizeTraversalRelativePath(root, left.FullPath),
                NormalizeTraversalRelativePath(root, right.FullPath)));
        return new WorkspacePhysicalTraversalResult(entries, directories);
    }

    private static string NormalizeTraversalRelativePath(string root, string fullPath) =>
        Path.GetRelativePath(root, fullPath).Replace(Path.DirectorySeparatorChar, '/');

    private static bool IsMissingTraversalPath(Exception exception)
    {
        if (exception is FileNotFoundException or DirectoryNotFoundException)
            return true;
        if (exception is Win32Exception { NativeErrorCode: 2 or 3 })
            return true;
        return exception.InnerException is not null && IsMissingTraversalPath(exception.InnerException);
    }

    [SupportedOSPlatform("windows")]
    private static void EnumerateWindowsFiles(
        string root,
        string directory,
        WorkspaceTraversalLimits limits,
        WorkspaceTraversalBudget budget,
        List<WorkspaceContainedFileEntry> entries,
        List<string> directories,
        WorkspaceTraversalCancellationState cancellationState)
    {
        using var rootHandle = OpenWindowsTraversalRoot(root);
        Volatile.Read(ref s_afterTraversalRootPinned)?.Invoke();
        cancellationState.ThrowIfCancellationRequested();

        var guards = OpenWindowsTraversalDirectoryChain(rootHandle, root, directory);
        try
        {
            EnumerateWindowsDirectory(
                rootHandle,
                guards[^1],
                directory,
                limits,
                budget,
                entries,
                directories,
                depth: 0,
                cancellationState);
        }
        finally
        {
            DisposeHandles(guards);
        }
    }

    [SupportedOSPlatform("windows")]
    private static SafeFileHandle OpenWindowsTraversalRoot(string root)
    {
        var handle = CreateFile(
            root,
            FileListDirectory | FileReadAttributes | FileTraverse | SynchronizeAccess,
            FileShareRead | FileShareWrite,
            IntPtr.Zero,
            OpenExisting,
            FileFlagBackupSemantics | FileFlagOpenReparsePoint,
            IntPtr.Zero);
        if (handle.IsInvalid)
        {
            var error = Marshal.GetLastPInvokeError();
            handle.Dispose();
            throw new IOException(
                $"Unable to pin traversal root '{root}'.",
                new Win32Exception(error));
        }

        ValidateWindowsDirectoryHandle(handle, root);
        return handle;
    }

    [SupportedOSPlatform("windows")]
    private static List<SafeFileHandle> OpenWindowsTraversalDirectoryChain(
        SafeFileHandle rootHandle,
        string root,
        string directory)
    {
        var handles = new List<SafeFileHandle>();
        if (string.Equals(root, directory, StringComparison.OrdinalIgnoreCase))
        {
            handles.Add(DuplicateWindowsHandle(rootHandle));
            return handles;
        }

        try
        {
            var relative = Path.GetRelativePath(root, directory);
            SafeFileHandle parent = rootHandle;
            foreach (var segment in SplitContainedSegments(relative))
            {
                var child = OpenWindowsRelative(
                    parent,
                    segment,
                    FileListDirectory | FileReadAttributes | FileTraverse | SynchronizeAccess,
                    FileShareRead | FileShareWrite,
                    NtFileOpen,
                    NtFileDirectory | FileFlagOpenReparsePoint,
                    allowMissing: false,
                    $"Unable to pin traversal directory segment '{segment}'.")
                    ?? throw new DirectoryNotFoundException(segment);
                ValidateWindowsDirectoryHandle(child, segment);
                handles.Add(child);
                parent = child;
            }

            return handles;
        }
        catch
        {
            DisposeHandles(handles);
            throw;
        }
    }

    [SupportedOSPlatform("windows")]
    private static void EnumerateWindowsDirectory(
        SafeFileHandle rootHandle,
        SafeFileHandle directoryHandle,
        string directoryPath,
        WorkspaceTraversalLimits limits,
        WorkspaceTraversalBudget budget,
        List<WorkspaceContainedFileEntry> entries,
        List<string> directories,
        int depth,
        WorkspaceTraversalCancellationState cancellationState)
    {
        var discoveredEntries = new List<(string Name, uint Attributes, long Length)>();
        var buffer = Marshal.AllocHGlobal(LinuxDirectoryBufferSize);
        try
        {
            while (true)
            {
                cancellationState.ThrowIfCancellationRequested();
                Volatile.Read(ref s_beforeTraversalDirectoryRead)?.Invoke();
                cancellationState.ThrowIfCancellationRequested();
                if (!GetFileInformationByHandleExBuffer(
                        directoryHandle,
                        FileFullDirectoryInfoClass,
                        buffer,
                        LinuxDirectoryBufferSize))
                {
                    var error = Marshal.GetLastPInvokeError();
                    if (error == ErrorNoMoreFiles)
                        break;
                    if (error == ErrorOperationAborted && cancellationState.IsCancellationRequested)
                        cancellationState.ThrowIfCancellationRequested();
                    throw new IOException(
                        $"Requirements export directory could not be enumerated safely: {directoryPath}",
                        new Win32Exception(error));
                }

                var offset = 0;
                while (true)
                {
                    cancellationState.ThrowIfCancellationRequested();
                    var current = IntPtr.Add(buffer, offset);
                    var nextOffset = Marshal.ReadInt32(current, 0);
                    var endOfFile = Marshal.ReadInt64(current, 40);
                    var attributes = unchecked((uint)Marshal.ReadInt32(current, 56));
                    var nameByteLength = Marshal.ReadInt32(current, 60);
                    if (nameByteLength < 0 || (nameByteLength & 1) != 0)
                        throw new IOException($"Invalid native directory entry under '{directoryPath}'.");

                    var name = Marshal.PtrToStringUni(IntPtr.Add(current, 68), nameByteLength / 2)
                        ?? throw new IOException($"Invalid native directory name under '{directoryPath}'.");
                    if (!string.Equals(name, ".", StringComparison.Ordinal) &&
                        !string.Equals(name, "..", StringComparison.Ordinal))
                    {
                        if (string.IsNullOrEmpty(name) ||
                            name.IndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]) >= 0)
                        {
                            throw new IOException($"Invalid contained directory entry name: {name}");
                        }

                        discoveredEntries.Add((name, attributes, endOfFile));
                    }

                    if (nextOffset == 0)
                        break;
                    if (nextOffset < 0 || offset > LinuxDirectoryBufferSize - nextOffset)
                        throw new IOException($"Invalid native directory-entry offset under '{directoryPath}'.");
                    offset += nextOffset;
                }
            }

            foreach (var entry in discoveredEntries.OrderBy(static entry => entry.Name, StringComparer.Ordinal))
            {
                ProcessWindowsDirectoryEntry(
                    rootHandle,
                    directoryHandle,
                    directoryPath,
                    entry.Name,
                    entry.Attributes,
                    entry.Length,
                    limits,
                    budget,
                    entries,
                    directories,
                    depth,
                    cancellationState);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    [SupportedOSPlatform("windows")]
    private static void ProcessWindowsDirectoryEntry(
        SafeFileHandle rootHandle,
        SafeFileHandle parentHandle,
        string parentPath,
        string name,
        uint enumeratedAttributes,
        long enumeratedLength,
        WorkspaceTraversalLimits limits,
        WorkspaceTraversalBudget budget,
        List<WorkspaceContainedFileEntry> entries,
        List<string> directories,
        int depth,
        WorkspaceTraversalCancellationState cancellationState)
    {
        if (name.IndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]) >= 0)
            throw new IOException($"Invalid contained directory entry name: {name}");

        var fullPath = Path.GetFullPath(Path.Combine(parentPath, name));
        if (!IsLexicallyContained(parentPath, fullPath))
            throw new IOException($"Directory entry escaped its parent: {fullPath}");
        if ((enumeratedAttributes & FileAttributeReparsePoint) != 0)
            return;

        if ((enumeratedAttributes & FileAttributeDirectory) != 0)
        {
            if (depth >= limits.MaxDepth)
            {
                throw new IOException(
                    $"Requirements export exceeds the {limits.MaxDepth}-level depth limit: {fullPath}");
            }

            using var child = OpenWindowsRelative(
                parentHandle,
                name,
                FileListDirectory | FileReadAttributes | FileTraverse | SynchronizeAccess,
                FileShareRead | FileShareWrite,
                NtFileOpen,
                NtFileDirectory | FileFlagOpenReparsePoint,
                allowMissing: false,
                $"Unable to pin traversal directory '{fullPath}'.")
                ?? throw new DirectoryNotFoundException(fullPath);
            ValidateWindowsDirectoryHandle(child, fullPath);
            EnumerateWindowsDirectory(
                rootHandle,
                child,
                fullPath,
                limits,
                budget,
                entries,
                directories,
                depth + 1,
                cancellationState);
            directories.Add(fullPath);
            return;
        }

        Volatile.Read(ref s_beforeTraversalFileOpen)?.Invoke(fullPath);
        cancellationState.ThrowIfCancellationRequested();
        using var file = OpenWindowsRelative(
            parentHandle,
            name,
            GenericRead | FileReadAttributes | SynchronizeAccess,
            FileShareRead | FileShareWrite,
            NtFileOpen,
            NtFileNonDirectory | FileFlagOpenReparsePoint,
            allowMissing: false,
            $"Unable to open traversed file '{fullPath}'.")
            ?? throw new FileNotFoundException("Traversed file disappeared.", fullPath);
        var actualAttributes = GetWindowsHandleAttributes(file);
        if ((actualAttributes & (FileAttributeDirectory | FileAttributeReparsePoint)) != 0)
            return;
        if (!GetFileSizeEx(file, out var actualLength))
        {
            throw new IOException(
                $"Requirements export file could not be measured safely: {fullPath}",
                new Win32Exception(Marshal.GetLastPInvokeError()));
        }

        if (enumeratedLength >= 0 && actualLength < 0)
            throw new IOException($"Invalid file length for '{fullPath}'.");
        budget.RegisterFile(fullPath, actualLength);
        entries.Add(new WorkspaceContainedFileEntry(fullPath, actualLength));
    }

    [SupportedOSPlatform("windows")]
    private static SafeFileHandle DuplicateWindowsHandle(SafeFileHandle source)
    {
        var currentProcess = GetCurrentProcess();
        if (!DuplicateHandle(
                currentProcess,
                source,
                currentProcess,
                out var duplicate,
                0,
                false,
                0x00000002))
        {
            throw new IOException(
                "Unable to duplicate the pinned traversal-root handle.",
                new Win32Exception(Marshal.GetLastPInvokeError()));
        }

        return duplicate;
    }


    [SupportedOSPlatform("linux")]
    private static void EnumerateLinuxFilesFromPinnedRoot(
        SafeFileHandle rootHandle,
        UnixStatx rootIdentity,
        string root,
        string directory,
        WorkspaceTraversalLimits limits,
        WorkspaceTraversalBudget budget,
        List<WorkspaceContainedFileEntry> entries,
        List<string> directories,
        WorkspaceTraversalCancellationState cancellationState)
    {
        cancellationState.ThrowIfCancellationRequested();
        var relative = GetLinuxRelativePath(root, directory);
        using var directoryHandle = OpenLinuxPathBeneathRoot(
            rootHandle,
            relative,
            LinuxOpenReadOnly |
            LinuxOpenDirectory |
            LinuxOpenNoFollow |
            LinuxOpenNonBlock |
            LinuxOpenCloseOnExec,
            mode: 0,
            directory);
        EnumerateLinuxDirectory(
            rootHandle,
            rootIdentity,
            directoryHandle,
            directory,
            relative,
            limits,
            budget,
            entries,
            directories,
            depth: 0,
            cancellationState);
    }

    [SupportedOSPlatform("linux")]
    private static void EnumerateLinuxDirectory(
        SafeFileHandle rootHandle,
        UnixStatx rootIdentity,
        SafeFileHandle directoryHandle,
        string directoryPath,
        string relativeDirectory,
        WorkspaceTraversalLimits limits,
        WorkspaceTraversalBudget budget,
        List<WorkspaceContainedFileEntry> entries,
        List<string> directories,
        int depth,
        WorkspaceTraversalCancellationState cancellationState)
    {
        EnsureLinuxDescriptorOnRootMount(rootIdentity, directoryHandle, directoryPath);
        var discoveredNames = new List<string>();
        var buffer = new byte[LinuxDirectoryBufferSize];
        while (true)
        {
            cancellationState.ThrowIfCancellationRequested();
            Volatile.Read(ref s_beforeTraversalDirectoryRead)?.Invoke();
            cancellationState.ThrowIfCancellationRequested();
            EnsureLinuxDescriptorOnRootMount(rootIdentity, directoryHandle, directoryPath);

            var byteCount = GetDents64(
                directoryHandle.DangerousGetHandle().ToInt32(),
                buffer,
                (nuint)buffer.Length);
            if (byteCount < 0)
                throw CreateUnixIOException($"Unable to enumerate contained directory '{directoryPath}'.");
            if (byteCount == 0)
                break;

            var offset = 0;
            while (offset < byteCount)
            {
                cancellationState.ThrowIfCancellationRequested();
                if (byteCount - offset < LinuxDirectoryEntryHeaderSize)
                    throw new IOException($"Invalid Linux directory entry under '{directoryPath}'.");
                var recordLength = BitConverter.ToUInt16(buffer, offset + 16);
                if (recordLength < LinuxDirectoryEntryHeaderSize ||
                    offset > byteCount - recordLength)
                {
                    throw new IOException($"Invalid Linux directory-entry length under '{directoryPath}'.");
                }

                var nameStart = offset + LinuxDirectoryEntryHeaderSize;
                var nameEnd = Array.IndexOf(buffer, (byte)0, nameStart, recordLength - LinuxDirectoryEntryHeaderSize);
                if (nameEnd < 0)
                    throw new IOException($"Invalid Linux directory name under '{directoryPath}'.");
                var name = System.Text.Encoding.UTF8.GetString(buffer, nameStart, nameEnd - nameStart);
                if (!string.Equals(name, ".", StringComparison.Ordinal) &&
                    !string.Equals(name, "..", StringComparison.Ordinal))
                {
                    if (string.IsNullOrEmpty(name) || name.Contains('/'))
                        throw new IOException($"Invalid contained Linux directory entry name: {name}");
                    discoveredNames.Add(name);
                }

                offset += recordLength;
            }
        }

        foreach (var name in discoveredNames.OrderBy(static name => name, StringComparer.Ordinal))
        {
            ProcessLinuxDirectoryEntry(
                rootHandle,
                rootIdentity,
                directoryPath,
                relativeDirectory,
                name,
                limits,
                budget,
                entries,
                directories,
                depth,
                cancellationState);
        }
    }

    [SupportedOSPlatform("linux")]
    private static void ProcessLinuxDirectoryEntry(
        SafeFileHandle rootHandle,
        UnixStatx rootIdentity,
        string parentPath,
        string relativeParent,
        string name,
        WorkspaceTraversalLimits limits,
        WorkspaceTraversalBudget budget,
        List<WorkspaceContainedFileEntry> entries,
        List<string> directories,
        int depth,
        WorkspaceTraversalCancellationState cancellationState)
    {
        if (string.IsNullOrEmpty(name) || name is "." or ".." || name.Contains('/'))
            throw new IOException($"Invalid contained Linux directory entry name: {name}");

        var relative = string.Equals(relativeParent, ".", StringComparison.Ordinal)
            ? name
            : $"{relativeParent}/{name}";
        var fullPath = Path.GetFullPath(Path.Combine(parentPath, name));
        Volatile.Read(ref s_beforeTraversalFileOpen)?.Invoke(fullPath);
        cancellationState.ThrowIfCancellationRequested();

        SafeFileHandle handle;
        try
        {
            handle = OpenLinuxPathBeneathRoot(
                rootHandle,
                relative,
                LinuxOpenPath |
                LinuxOpenNoFollow |
                LinuxOpenCloseOnExec,
                mode: 0,
                fullPath);
        }
        catch (IOException exception) when (
            exception.InnerException is Win32Exception { NativeErrorCode: LinuxErrorTooManyLinks })
        {
            return;
        }

        using (handle)
        {
            var information = FStatUnix(handle.DangerousGetHandle().ToInt32());
            var fileType = (ushort)(information.Mode & UnixFileTypeMask);
            if (fileType == UnixDirectory)
            {
                if (depth >= limits.MaxDepth)
                {
                    throw new IOException(
                        $"Requirements export exceeds the {limits.MaxDepth}-level depth limit: {fullPath}");
                }

                handle.Dispose();
                using var directoryHandle = OpenLinuxPathBeneathRoot(
                    rootHandle,
                    relative,
                    LinuxOpenReadOnly |
                    LinuxOpenDirectory |
                    LinuxOpenNoFollow |
                    LinuxOpenNonBlock |
                    LinuxOpenCloseOnExec,
                    mode: 0,
                    fullPath);
                EnumerateLinuxDirectory(
                    rootHandle,
                    rootIdentity,
                    directoryHandle,
                    fullPath,
                    relative,
                    limits,
                    budget,
                    entries,
                    directories,
                    depth + 1,
                    cancellationState);
                directories.Add(fullPath);
                return;
            }

            if (fileType != UnixRegularFile)
                return;
            EnsureLinuxDescriptorOnRootMount(rootIdentity, handle, fullPath);
            var length = checked((long)information.Size);
            budget.RegisterFile(fullPath, length);
            entries.Add(new WorkspaceContainedFileEntry(fullPath, length));
        }
    }

    [SupportedOSPlatform("linux")]
    private static SafeFileHandle OpenLinuxPathBeneathRoot(
        SafeFileHandle rootHandle,
        string relativePath,
        ulong flags,
        ulong mode,
        string displayPath)
    {
        var how = new TraversalOpenHow
        {
            Flags = flags,
            Mode = mode,
            Resolve =
                LinuxResolveNoCrossDevice |
                LinuxResolveNoMagicLinks |
                LinuxResolveNoSymbolicLinks |
                LinuxResolveInRoot,
        };
        var descriptor = OpenAt2Traversal(
            OpenAt2SystemCall,
            rootHandle.DangerousGetHandle().ToInt32(),
            string.IsNullOrEmpty(relativePath) ? "." : relativePath,
            ref how,
            (nuint)Marshal.SizeOf<TraversalOpenHow>());
        if (descriptor >= 0)
            return new SafeFileHandle((nint)descriptor, ownsHandle: true);

        var error = Marshal.GetLastPInvokeError();
        throw error switch
        {
            LinuxErrorNoEntry => new DirectoryNotFoundException(displayPath),
            LinuxErrorCrossDevice => new NotSupportedException(
                $"Requirements export traversal rejected a mounted filesystem boundary at '{displayPath}'."),
            LinuxErrorNotImplemented => new NotSupportedException(
                "Requirements export traversal requires Linux openat2 support."),
            LinuxErrorTooManyLinks => new IOException(
                $"Requirements export traversal rejected a symbolic link at '{displayPath}'.",
                new Win32Exception(error)),
            LinuxErrorNotDirectory => new IOException(
                $"Requirements export traversal encountered a non-directory path at '{displayPath}'.",
                new Win32Exception(error)),
            _ => new IOException(
                $"Unable to open contained Linux traversal entry '{displayPath}'.",
                new Win32Exception(error)),
        };
    }

    private static string GetLinuxRelativePath(string root, string path)
    {
        var relative = Path.GetRelativePath(root, path);
        if (string.Equals(relative, ".", StringComparison.Ordinal))
            return ".";
        _ = SplitContainedSegments(relative).ToArray();
        return relative.Replace(Path.DirectorySeparatorChar, '/');
    }

    [SupportedOSPlatform("linux")]
    private static void EnsureLinuxDescriptorOnRootMount(
        UnixStatx rootIdentity,
        SafeFileHandle handle,
        string displayPath)
    {
        var actual = FStatUnix(handle.DangerousGetHandle().ToInt32());
        if (actual.DeviceMajor != rootIdentity.DeviceMajor ||
            actual.DeviceMinor != rootIdentity.DeviceMinor)
        {
            throw new IOException(
                $"Traversal descriptor crossed the pinned workspace mount: {displayPath}");
        }
    }

    private static void ValidateFiniteTimeout(TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero || timeout == Timeout.InfiniteTimeSpan)
        {
            throw new ArgumentOutOfRangeException(
                nameof(timeout),
                "A finite positive traversal timeout is required.");
        }
    }

    private static void ReserveTraversalWorkerSlot()
    {
        while (true)
        {
            var activeWorkers = Volatile.Read(ref s_activeTraversalWorkerCount);
            if (activeWorkers >= MaxTraversalWorkerCount)
            {
                throw new InvalidOperationException(
                    $"Contained traversal worker capacity of {MaxTraversalWorkerCount} is exhausted.");
            }

            if (Interlocked.CompareExchange(
                    ref s_activeTraversalWorkerCount,
                    activeWorkers + 1,
                    activeWorkers) == activeWorkers)
            {
                return;
            }
        }
    }

    private static async Task<T> RunBoundedTraversalWorkerAsync<T>(
        Func<WorkspaceTraversalCancellationState, T> operation,
        TimeSpan timeout,
        CancellationToken cancellationToken)
        where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        var workerOutcome = new TaskCompletionSource<WorkspaceTraversalOutcome<T>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var completion = new TaskCompletionSource<WorkspaceTraversalOutcome<T>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var cancellationSignal = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var timeoutSignal = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var state = new WorkspaceTraversalCancellationState();

        using var callerRegistration = cancellationToken.UnsafeRegister(
            static value =>
            {
                var tuple = ((WorkspaceTraversalCancellationState State, TaskCompletionSource Signal))value!;
                tuple.State.Request(WorkspaceTraversalCancellationKind.Caller);
                tuple.Signal.TrySetResult();
            },
            (state, cancellationSignal));
        using var timeoutSource = new CancellationTokenSource(timeout);
        using var timeoutRegistration = timeoutSource.Token.UnsafeRegister(
            static value =>
            {
                var tuple = ((WorkspaceTraversalCancellationState State, TaskCompletionSource Signal))value!;
                tuple.State.Request(WorkspaceTraversalCancellationKind.Timeout);
                tuple.Signal.TrySetResult();
            },
            (state, timeoutSignal));

        var thread = new Thread(
            () =>
            {
                T? result = null;
                Exception? failure = null;
                try
                {
                    state.AttachThread(OperatingSystem.IsWindows() ? GetCurrentThreadIdForTraversal() : 0);
                    result = operation(state);
                }
                catch (Exception exception)
                {
                    failure = exception;
                }
                finally
                {
                    workerOutcome.TrySetResult(new WorkspaceTraversalOutcome<T>(result, failure));
                }
            })
        {
            IsBackground = true,
            Name = "McpContainedTraversal",
        };

        ReserveTraversalWorkerSlot();
        try
        {
            thread.Start();
        }
        catch
        {
            Interlocked.Decrement(ref s_activeTraversalWorkerCount);
            state.Complete();
            throw;
        }

        _ = TrackTraversalWorkerUntilActuallyGoneAsync(
            thread,
            workerOutcome.Task,
            state,
            completion);

        var winner = await Task.WhenAny(
                completion.Task,
                cancellationSignal.Task,
                timeoutSignal.Task)
            .ConfigureAwait(false);
        if (!ReferenceEquals(winner, completion.Task))
        {
            var cleanupDeadline = Task.Delay(s_traversalCleanupTimeout);
            while (!completion.Task.IsCompleted && !cleanupDeadline.IsCompleted)
            {
                state.TryCancelNativeWork();
                await Task.WhenAny(
                        completion.Task,
                        cleanupDeadline,
                        Task.Delay(TimeSpan.FromMilliseconds(10)))
                    .ConfigureAwait(false);
            }
        }

        var cancellationKind = state.RequestedKind;
        if (!completion.Task.IsCompleted)
            ThrowTraversalCancellation(cancellationKind, timeout, cancellationToken, failure: null);

        var outcome = await completion.Task.ConfigureAwait(false);
        if (outcome.Failure is null && outcome.Result is not null)
        {
            if (cancellationKind != WorkspaceTraversalCancellationKind.None)
            {
                (outcome.Result as IDisposable)?.Dispose();
                ThrowTraversalCancellation(cancellationKind, timeout, cancellationToken, failure: null);
            }

            return outcome.Result;
        }

        if (cancellationKind != WorkspaceTraversalCancellationKind.None)
            ThrowTraversalCancellation(cancellationKind, timeout, cancellationToken, outcome.Failure);
        if (outcome.Failure is not null)
            ExceptionDispatchInfo.Capture(outcome.Failure).Throw();

        throw new InvalidOperationException("Bounded traversal worker produced no result.");
    }

    private static async Task TrackTraversalWorkerUntilActuallyGoneAsync<T>(
        Thread thread,
        Task<WorkspaceTraversalOutcome<T>> workerOutcome,
        WorkspaceTraversalCancellationState state,
        TaskCompletionSource<WorkspaceTraversalOutcome<T>> completion)
        where T : class
    {
        while (thread.IsAlive)
            await Task.Delay(TimeSpan.FromMilliseconds(5)).ConfigureAwait(false);

        var outcome = await workerOutcome.ConfigureAwait(false);
        if (state.RequestedKind != WorkspaceTraversalCancellationKind.None)
        {
            (outcome.Result as IDisposable)?.Dispose();
            outcome = outcome with { Result = null };
        }

        state.Complete();
        Interlocked.Decrement(ref s_activeTraversalWorkerCount);
        completion.TrySetResult(outcome);
    }

    private static void ThrowTraversalCancellation(
        WorkspaceTraversalCancellationKind kind,
        TimeSpan timeout,
        CancellationToken cancellationToken,
        Exception? failure)
    {
        if (kind == WorkspaceTraversalCancellationKind.Caller)
        {
            throw new OperationCanceledException(
                "Contained workspace traversal was cancelled by the caller.",
                failure,
                cancellationToken);
        }

        if (kind == WorkspaceTraversalCancellationKind.Timeout)
        {
            throw new TimeoutException(
                $"Contained workspace traversal exceeded the finite timeout of {timeout}.",
                failure);
        }

        throw new InvalidOperationException(
            "Bounded traversal lost its cancellation classification.",
            failure);
    }

    private sealed record WorkspacePhysicalTraversalResult(
        IReadOnlyList<WorkspaceContainedFileEntry> Files,
        IReadOnlyList<string> Directories);

    private sealed record WorkspaceTraversalOutcome<T>(T? Result, Exception? Failure)
        where T : class;

    private enum WorkspaceTraversalCancellationKind
    {
        None = 0,
        Timeout = 1,
        Caller = 2,
    }

    private sealed class WorkspaceTraversalCancellationState
    {
        private readonly object _sync = new();
        private int _requestedKind;
        private uint _threadId;
        private bool _completed;

        internal WorkspaceTraversalCancellationKind RequestedKind =>
            (WorkspaceTraversalCancellationKind)Volatile.Read(ref _requestedKind);

        internal bool IsCancellationRequested => RequestedKind != WorkspaceTraversalCancellationKind.None;

        internal void Request(WorkspaceTraversalCancellationKind kind)
        {
            Interlocked.CompareExchange(
                ref _requestedKind,
                (int)kind,
                (int)WorkspaceTraversalCancellationKind.None);
            TryCancelNativeWork();
        }

        internal void ThrowIfCancellationRequested()
        {
            if (RequestedKind != WorkspaceTraversalCancellationKind.None)
                throw new OperationCanceledException("Bounded traversal cancellation was requested.");
        }

        internal void AttachThread(uint threadId)
        {
            lock (_sync)
            {
                if (_completed)
                    return;
                _threadId = threadId;
            }

            if (IsCancellationRequested)
                TryCancelNativeWork();
        }

        internal void Complete()
        {
            lock (_sync)
            {
                _completed = true;
                _threadId = 0;
            }
        }

        internal void TryCancelNativeWork()
        {
            uint threadId;
            lock (_sync)
            {
                if (_completed)
                    return;
                threadId = _threadId;
            }

            if (!OperatingSystem.IsWindows() || threadId == 0)
                return;

            var threadHandle = OpenThreadForTraversal(0x0001, false, threadId);
            if (threadHandle == IntPtr.Zero)
                return;
            try
            {
                _ = CancelSynchronousIoForTraversal(threadHandle);
            }
            finally
            {
                _ = CloseHandleForTraversal(threadHandle);
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TraversalOpenHow
    {
        internal ulong Flags;
        internal ulong Mode;
        internal ulong Resolve;
    }

    [DllImport("kernel32.dll", EntryPoint = "GetFileInformationByHandleEx", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetFileInformationByHandleExBuffer(
        SafeFileHandle fileHandle,
        int fileInformationClass,
        IntPtr fileInformation,
        int bufferSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetFileSizeEx(SafeFileHandle fileHandle, out long fileSize);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentProcess();

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DuplicateHandle(
        IntPtr sourceProcessHandle,
        SafeFileHandle sourceHandle,
        IntPtr targetProcessHandle,
        out SafeFileHandle targetHandle,
        uint desiredAccess,
        [MarshalAs(UnmanagedType.Bool)] bool inheritHandle,
        uint options);

    [DllImport("kernel32.dll", EntryPoint = "GetCurrentThreadId")]
    private static extern uint GetCurrentThreadIdForTraversal();

    [DllImport("kernel32.dll", EntryPoint = "OpenThread", SetLastError = true)]
    private static extern IntPtr OpenThreadForTraversal(
        uint desiredAccess,
        [MarshalAs(UnmanagedType.Bool)] bool inheritHandle,
        uint threadId);

    [DllImport("kernel32.dll", EntryPoint = "CancelSynchronousIo", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CancelSynchronousIoForTraversal(IntPtr threadHandle);

    [DllImport("kernel32.dll", EntryPoint = "CloseHandle", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandleForTraversal(IntPtr handle);

    [DllImport("libc", EntryPoint = "syscall", SetLastError = true)]
    private static extern long OpenAt2Traversal(
        long number,
        int directoryDescriptor,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string path,
        ref TraversalOpenHow how,
        nuint size);

    [DllImport("libc", EntryPoint = "getdents64", SetLastError = true)]
    private static extern int GetDents64(
        int directoryDescriptor,
        byte[] buffer,
        nuint bufferSize);
}
