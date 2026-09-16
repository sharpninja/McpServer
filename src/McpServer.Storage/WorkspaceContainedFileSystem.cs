using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using McpServer.Client;
using Microsoft.Win32.SafeHandles;

namespace McpServer.Support.Mcp.Storage;

/// <summary>Explicit resource limits for one contained workspace traversal.</summary>
public sealed class WorkspaceTraversalLimits
{
    /// <summary>Maximum regular-file count.</summary>
    public int MaxFiles { get; set; } = 10_000;

    /// <summary>Maximum bytes accepted from one file.</summary>
    public long MaxFileBytes { get; set; } = 64L * 1024 * 1024;

    /// <summary>Maximum aggregate bytes accepted across the traversal.</summary>
    public long MaxAggregateBytes { get; set; } = 256L * 1024 * 1024;

    /// <summary>Maximum directory depth below the traversal start.</summary>
    public int MaxDepth { get; set; } = 64;

    /// <summary>Validates that every bound is positive.</summary>
    internal void Validate()
    {
        if (MaxFiles <= 0)
            throw new ArgumentOutOfRangeException(nameof(MaxFiles));
        if (MaxFileBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(MaxFileBytes));
        if (MaxAggregateBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(MaxAggregateBytes));
        if (MaxDepth <= 0)
            throw new ArgumentOutOfRangeException(nameof(MaxDepth));
    }
}

/// <summary>Shared counters used when one logical export traverses multiple directories.</summary>
public sealed class WorkspaceTraversalBudget
{
    private readonly WorkspaceTraversalLimits _limits;
    private int _fileCount;
    private long _aggregateBytes;

    /// <summary>Creates a budget governed by <paramref name="limits"/>.</summary>
    public WorkspaceTraversalBudget(WorkspaceTraversalLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);
        limits.Validate();
        _limits = limits;
    }

    /// <summary>Records a file and rejects any exceeded count or byte bound.</summary>
    internal void RegisterFile(string path, long length)
    {
        if (length < 0 || length > _limits.MaxFileBytes)
        {
            throw new IOException(
                $"Requirements export file exceeds the {_limits.MaxFileBytes}-byte limit: {path}");
        }

        _fileCount = checked(_fileCount + 1);
        if (_fileCount > _limits.MaxFiles)
        {
            throw new IOException(
                $"Requirements export exceeds the {_limits.MaxFiles}-file limit.");
        }

        _aggregateBytes = checked(_aggregateBytes + length);
        if (_aggregateBytes > _limits.MaxAggregateBytes)
        {
            throw new IOException(
                $"Requirements export exceeds the {_limits.MaxAggregateBytes}-byte aggregate limit.");
        }
    }
}

/// <summary>
/// Streams filesystem entries beneath a workspace root without following directory aliases and
/// opens content through handles bound to the same objects that were validated.
/// </summary>
public static partial class WorkspaceContainedFileSystem
{
    private const uint GenericRead = 0x80000000;
    private const uint GenericWrite = 0x40000000;
    private const uint DeleteAccess = 0x00010000;
    private const uint SynchronizeAccess = 0x00100000;
    private const uint FileTraverse = 0x00000020;
    private const uint FileReadAttributes = 0x00000080;
    private const uint FileWriteAttributes = 0x00000100;
    private const uint FileShareRead = 0x00000001;
    private const uint FileShareWrite = 0x00000002;
    private const uint OpenExisting = 3;
    private const uint FileAttributeDirectory = 0x00000010;
    private const uint FileAttributeReparsePoint = 0x00000400;
    private const uint FileFlagOpenReparsePoint = 0x00200000;
    private const uint FileFlagBackupSemantics = 0x02000000;
    private const uint NtFileOpen = 1;
    private const uint NtFileOpenIf = 3;
    private const uint NtFileDirectory = 0x00000001;
    private const uint NtFileNonDirectory = 0x00000040;
    private const uint ObjectCaseInsensitive = 0x00000040;
    private const int FileDispositionInfoClass = 4;
    private const int FileAttributeTagInfoClass = 9;
    private const int ErrorDirectoryNotEmpty = 145;

    private const int UnixOpenReadOnly = 0;
    private const int UnixOpenReadWrite = 2;
    private const int UnixOpenCreate = 0x40;
    private const int UnixOpenNonBlock = 0x800;
    private const int UnixOpenDirectory = 0x10000;
    private const int UnixOpenNoFollow = 0x20000;
    private const int UnixOpenCloseOnExec = 0x80000;
    private const int UnixOpenPath = 0x200000;
    private const int UnixAtSymlinkNoFollow = 0x100;
    private const int UnixAtRemoveDirectory = 0x200;
    private const int UnixAtEmptyPath = 0x1000;
    private const uint UnixStatxBasicStats = 0x000007ff;
    private const ushort UnixFileTypeMask = 0xF000;
    private const ushort UnixRegularFile = 0x8000;
    private const ushort UnixDirectory = 0x4000;
    private const uint UnixOwnerReadWrite = 0x180;
    private const uint UnixOwnerReadWriteExecute = 0x1C0;
    private const int UnixNoEntry = 2;
    private const int UnixAlreadyExists = 17;
    private const int UnixDirectoryNotEmpty = 39;

    /// <summary>
    /// Lazily enumerates contained regular files under default traversal limits.
    /// </summary>
    public static IEnumerable<string> EnumerateFiles(
        string rootPath,
        string directoryPath,
        CancellationToken cancellationToken = default)
    {
        var limits = new WorkspaceTraversalLimits();
        return EnumerateFiles(
            rootPath,
            directoryPath,
            limits,
            new WorkspaceTraversalBudget(limits),
            cancellationToken);
    }

    /// <summary>
    /// Lazily enumerates contained regular files under explicit traversal limits.
    /// </summary>
    public static IEnumerable<string> EnumerateFiles(
        string rootPath,
        string directoryPath,
        WorkspaceTraversalLimits limits,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(limits);
        return EnumerateFiles(
            rootPath,
            directoryPath,
            limits,
            new WorkspaceTraversalBudget(limits),
            cancellationToken);
    }

    /// <summary>
    /// Lazily enumerates contained regular files while charging a shared export budget.
    /// </summary>
    public static IEnumerable<string> EnumerateFiles(
        string rootPath,
        string directoryPath,
        WorkspaceTraversalLimits limits,
        WorkspaceTraversalBudget budget,
        CancellationToken cancellationToken)
    {
        ValidateTraversalArguments(rootPath, directoryPath, limits, budget);
        return EnumerateCapturedFiles();

        IEnumerable<string> EnumerateCapturedFiles()
        {
            var entries = EnumerateFilesBoundedAsync(
                    rootPath,
                    directoryPath,
                    limits,
                    s_defaultTraversalTimeout,
                    cancellationToken)
                .GetAwaiter()
                .GetResult();
            foreach (var entry in entries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                budget.RegisterFile(entry.FullPath, entry.Length);
                yield return entry.FullPath;
            }
        }
    }

    /// <summary>
    /// Lazily enumerates ordinary directories in post-order for safe empty-directory cleanup.
    /// </summary>
    public static IEnumerable<string> EnumerateDirectories(
        string rootPath,
        string directoryPath,
        CancellationToken cancellationToken = default)
    {
        var limits = new WorkspaceTraversalLimits();
        return EnumerateDirectoriesBoundedAsync(
                rootPath,
                directoryPath,
                limits,
                s_defaultTraversalTimeout,
                cancellationToken)
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// Opens an existing contained file for asynchronous reads through the exact validated object.
    /// </summary>
    public static FileStream OpenFileForRead(string rootPath, string filePath)
    {
        var (root, file) = ValidateContainedPaths(rootPath, filePath);
        using var exportRoot = OpenExportRoot(root);
        return exportRoot.OpenFileForRead(Path.GetRelativePath(exportRoot.RootPath, file));
    }

    /// <summary>
    /// Opens or creates a contained file for asynchronous writes without truncating it before final
    /// handle validation.
    /// </summary>
    public static FileStream OpenFileForWrite(string rootPath, string filePath)
    {
        var (root, file) = ValidateContainedPaths(rootPath, filePath);
        using var exportRoot = OpenExportRoot(root);
        return exportRoot.OpenFileForWrite(Path.GetRelativePath(exportRoot.RootPath, file));
    }

    /// <summary>
    /// Creates and pins the export root before contained operations are allowed beneath it.
    /// </summary>
    /// <param name="rootPath">The root that defines the caller's filesystem authority.</param>
    public static void EnsureRootDirectory(string rootPath)
    {
        // OpenExportRoot validates platform policy before any mutation and owns all platform-specific
        // root establishment rules. The short-lived capability preserves legacy callers without
        // reintroducing create-before-platform-validation behavior.
        using var exportRoot = OpenExportRoot(rootPath);
    }

    /// <summary>
    /// Writes content, timestamp metadata, and final read-only state through one validated contained
    /// file handle.
    /// </summary>
    /// <param name="rootPath">The root that defines the caller's filesystem authority.</param>
    /// <param name="filePath">The contained output file.</param>
    /// <param name="content">The complete text content to write.</param>
    /// <param name="encoding">The required output encoding.</param>
    /// <param name="lastWriteTimeUtc">The deterministic output timestamp.</param>
    /// <param name="readOnly">Whether the completed output must be immutable.</param>
    /// <param name="cancellationToken">The caller cancellation token.</param>
    public static async Task WriteTextFileAsync(
        string rootPath,
        string filePath,
        string content,
        Encoding encoding,
        DateTime lastWriteTimeUtc,
        bool readOnly,
        CancellationToken cancellationToken = default)
    {
        var (root, file) = ValidateContainedPaths(rootPath, filePath);
        using var exportRoot = OpenExportRoot(root);
        await exportRoot.WriteTextFileAsync(
                Path.GetRelativePath(exportRoot.RootPath, file),
                content,
                encoding,
                lastWriteTimeUtc,
                readOnly,
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Changes the immutable flag on an existing contained file without resolving a path outside its
    /// validated root.
    /// </summary>
    /// <param name="rootPath">The root that defines the caller's filesystem authority.</param>
    /// <param name="filePath">The contained file whose metadata changes.</param>
    /// <param name="readOnly">Whether the file must be immutable.</param>
    /// <returns><see langword="true"/> when the file existed; otherwise, <see langword="false"/>.</returns>
    public static bool SetFileReadOnlyIfExists(string rootPath, string filePath, bool readOnly)
    {
        var (root, file) = ValidateContainedPaths(rootPath, filePath);
        using var exportRoot = OpenExportRoot(root);
        return exportRoot.SetFileReadOnlyIfExists(
            Path.GetRelativePath(exportRoot.RootPath, file),
            readOnly);
    }

    /// <summary>
    /// Deletes an existing ordinary contained file through a handle or descriptor relative to its
    /// pinned parent.
    /// </summary>
    /// <returns><see langword="true"/> when an entry was deleted; otherwise, <see langword="false"/>.</returns>
    public static bool DeleteFileIfExists(string rootPath, string filePath)
    {
        var (root, file) = ValidateContainedPaths(rootPath, filePath);
        using var exportRoot = OpenExportRoot(root);
        return exportRoot.DeleteFileIfExists(Path.GetRelativePath(exportRoot.RootPath, file));
    }

    /// <summary>
    /// Deletes an existing empty ordinary contained directory relative to its pinned parent.
    /// </summary>
    /// <returns><see langword="true"/> when an entry was deleted; otherwise, <see langword="false"/>.</returns>
    public static bool DeleteEmptyDirectory(string rootPath, string directoryPath)
    {
        var (root, directory) = ValidateContainedPaths(rootPath, directoryPath);
        using var exportRoot = OpenExportRoot(root);
        return exportRoot.DeleteEmptyDirectory(Path.GetRelativePath(exportRoot.RootPath, directory));
    }

    /// <summary>
    /// Creates a contained directory chain while binding every operation to validated parents.
    /// </summary>
    public static void EnsureDirectory(string rootPath, string directoryPath)
    {
        var (root, directory) = ValidateContainedPaths(rootPath, directoryPath);
        using var exportRoot = OpenExportRoot(root);
        exportRoot.EnsureDirectory(Path.GetRelativePath(exportRoot.RootPath, directory));
    }


    private static void ValidateTraversalArguments(
        string rootPath,
        string directoryPath,
        WorkspaceTraversalLimits limits,
        WorkspaceTraversalBudget budget)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);
        ArgumentNullException.ThrowIfNull(limits);
        ArgumentNullException.ThrowIfNull(budget);
        limits.Validate();

        var root = Path.GetFullPath(rootPath);
        var start = Path.GetFullPath(directoryPath);
        if (!IsLexicallyContained(root, start))
        {
            throw new IOException(
                $"Requirements export traversal root is outside the workspace: {directoryPath}");
        }
    }

    private static void SetFileReadOnly(SafeFileHandle handle, bool readOnly)
    {
        var current = File.GetAttributes(handle);
        var updated = readOnly
            ? current | FileAttributes.ReadOnly
            : current & ~FileAttributes.ReadOnly;
        if (updated != current)
            File.SetAttributes(handle, updated);
    }

    private static bool IsMissingContainedEntry(IOException exception) =>
        exception.InnerException is Win32Exception { NativeErrorCode: UnixNoEntry or 3 };


    private static (string Root, string Candidate) ValidateContainedPaths(
        string rootPath,
        string candidatePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(candidatePath);
        var root = Path.GetFullPath(rootPath);
        var candidate = Path.GetFullPath(candidatePath);
        if (!IsLexicallyContained(root, candidate))
        {
            throw new IOException(
                $"Requirements export path is outside its root: {candidatePath}");
        }

        return (root, candidate);
    }

    private static bool IsLexicallyContained(string root, string candidate)
    {
        if (string.Equals(root, candidate, PathComparison))
            return true;
        var rootWithSeparator = Path.EndsInDirectorySeparator(root)
            ? root
            : root + Path.DirectorySeparatorChar;
        return candidate.StartsWith(rootWithSeparator, PathComparison);
    }

    private static StringComparison PathComparison =>
        OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

    [SupportedOSPlatform("windows")]
    private static FileStream OpenWindowsFile(
        string root,
        string file,
        bool write,
        bool metadataWrite = false,
        SafeFileHandle? rootAuthority = null)
    {
        var parent = Path.GetDirectoryName(file)
            ?? throw new IOException($"Contained file has no parent: {file}");
        var fileName = GetContainedEntryName(file);
        var guards = OpenWindowsDirectoryChain(root, parent, rootAuthority);
        try
        {
            var desiredAccess = write
                ? GenericWrite | FileReadAttributes
                : GenericRead | FileReadAttributes | (metadataWrite ? FileWriteAttributes : 0u);
            var handle = OpenWindowsRelative(
                guards[^1],
                fileName,
                desiredAccess,
                FileShareRead | FileShareWrite,
                write ? NtFileOpenIf : NtFileOpen,
                NtFileNonDirectory | FileFlagOpenReparsePoint,
                allowMissing: false,
                $"Unable to open contained file '{file}'.")
                ?? throw new FileNotFoundException("Contained file does not exist.", file);
            try
            {
                ValidateWindowsEntry(root, handle, file, directory: false, rootAuthority);
                return new FileStream(
                    handle,
                    write ? FileAccess.Write : FileAccess.Read,
                    bufferSize: 64 * 1024,
                    isAsync: true);
            }
            catch
            {
                handle.Dispose();
                throw;
            }
        }
        finally
        {
            DisposeHandles(guards);
        }
    }

    [SupportedOSPlatform("windows")]
    private static List<SafeFileHandle> OpenWindowsDirectoryChain(
        string root,
        string directory,
        SafeFileHandle? rootAuthority = null)
    {
        if (!IsLexicallyContained(root, directory))
            throw new IOException($"Directory is outside the workspace root: {directory}");

        var handles = new List<SafeFileHandle>();
        try
        {
            handles.Add(rootAuthority is null
                ? OpenWindowsDirectory(root)
                : DuplicateWindowsHandle(rootAuthority));
            var relative = Path.GetRelativePath(root, directory);
            if (!string.Equals(relative, ".", StringComparison.Ordinal))
            {
                foreach (var segment in SplitContainedSegments(relative))
                    handles.Add(OpenWindowsDirectoryAt(handles[^1], segment));
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
    private static SafeFileHandle OpenWindowsDirectory(string directory)
    {
        var handle = CreateFile(
            directory,
            FileReadAttributes | FileTraverse | SynchronizeAccess,
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
                $"Unable to pin contained directory '{directory}'.",
                new Win32Exception(error));
        }

        ValidateWindowsDirectoryHandle(handle, directory);
        return handle;
    }

    [SupportedOSPlatform("windows")]
    private static SafeFileHandle OpenWindowsDirectoryAt(
        SafeFileHandle parent,
        string segment)
    {
        var handle = OpenWindowsRelative(
            parent,
            segment,
            FileReadAttributes | FileTraverse | SynchronizeAccess,
            FileShareRead | FileShareWrite,
            NtFileOpen,
            NtFileDirectory | FileFlagOpenReparsePoint,
            allowMissing: false,
            $"Unable to pin contained directory segment '{segment}'.")
            ?? throw new DirectoryNotFoundException(segment);
        ValidateWindowsDirectoryHandle(handle, segment);
        return handle;
    }

    [SupportedOSPlatform("windows")]
    private static void EnsureWindowsDirectory(
        string root,
        string directory,
        SafeFileHandle? rootAuthority = null)
    {
        var handles = new List<SafeFileHandle>();
        try
        {
            handles.Add(rootAuthority is null
                ? OpenWindowsDirectory(root)
                : DuplicateWindowsHandle(rootAuthority));
            var relative = Path.GetRelativePath(root, directory);
            if (string.Equals(relative, ".", StringComparison.Ordinal))
                return;

            foreach (var segment in SplitContainedSegments(relative))
            {
                var handle = OpenWindowsRelative(
                    handles[^1],
                    segment,
                    FileReadAttributes | FileTraverse | SynchronizeAccess,
                    FileShareRead | FileShareWrite,
                    NtFileOpenIf,
                    NtFileDirectory | FileFlagOpenReparsePoint,
                    allowMissing: false,
                    $"Unable to create contained directory segment '{segment}'.")
                    ?? throw new DirectoryNotFoundException(segment);
                ValidateWindowsDirectoryHandle(handle, segment);
                handles.Add(handle);
            }
        }
        finally
        {
            DisposeHandles(handles);
        }
    }

    [SupportedOSPlatform("windows")]
    private static bool DeleteWindowsEntry(
        string root,
        string entry,
        bool directory,
        SafeFileHandle? rootAuthority = null)
    {
        var parent = Path.GetDirectoryName(entry)
            ?? throw new IOException($"Contained entry has no parent: {entry}");
        var name = GetContainedEntryName(entry);
        var guards = OpenWindowsDirectoryChain(root, parent, rootAuthority);
        try
        {
            using var handle = OpenWindowsRelative(
                guards[^1],
                name,
                DeleteAccess | FileReadAttributes | SynchronizeAccess,
                FileShareRead | FileShareWrite,
                NtFileOpen,
                (directory ? NtFileDirectory : NtFileNonDirectory) | FileFlagOpenReparsePoint,
                allowMissing: true,
                $"Unable to open contained entry for deletion '{entry}'.");
            if (handle is null)
                return false;

            ValidateWindowsEntry(root, handle, entry, directory, rootAuthority);
            var disposition = new FileDispositionInformation { DeleteFile = 1 };
            if (!SetFileInformationByHandle(
                    handle,
                    FileDispositionInfoClass,
                    ref disposition,
                    (uint)Marshal.SizeOf<FileDispositionInformation>()))
            {
                var error = Marshal.GetLastPInvokeError();
                if (directory && error == ErrorDirectoryNotEmpty)
                    return false;
                throw new IOException(
                    $"Unable to delete contained entry '{entry}'.",
                    new Win32Exception(error));
            }

            return true;
        }
        finally
        {
            DisposeHandles(guards);
        }
    }

    [SupportedOSPlatform("windows")]
    private static SafeFileHandle? OpenWindowsRelative(
        SafeFileHandle parent,
        string name,
        uint desiredAccess,
        uint shareAccess,
        uint createDisposition,
        uint createOptions,
        bool allowMissing,
        string errorMessage)
    {
        if (name.Length > (ushort.MaxValue / sizeof(char)) - 1)
            throw new IOException($"Contained path segment is too long: {name}");

        var nameBuffer = Marshal.StringToHGlobalUni(name);
        var unicodeStringPointer = Marshal.AllocHGlobal(Marshal.SizeOf<UnicodeString>());
        try
        {
            var unicodeString = new UnicodeString
            {
                Length = checked((ushort)(name.Length * sizeof(char))),
                MaximumLength = checked((ushort)((name.Length + 1) * sizeof(char))),
                Buffer = nameBuffer,
            };
            Marshal.StructureToPtr(unicodeString, unicodeStringPointer, fDeleteOld: false);
            var attributes = new ObjectAttributes
            {
                Length = Marshal.SizeOf<ObjectAttributes>(),
                RootDirectory = parent.DangerousGetHandle(),
                ObjectName = unicodeStringPointer,
                Attributes = ObjectCaseInsensitive,
            };
            var status = NtCreateFile(
                out var handle,
                desiredAccess,
                ref attributes,
                out _,
                IntPtr.Zero,
                0,
                shareAccess,
                createDisposition,
                createOptions,
                IntPtr.Zero,
                0);
            if (status >= 0 && handle is not null && !handle.IsInvalid)
                return handle;

            handle?.Dispose();
            var error = unchecked((int)RtlNtStatusToDosError(status));
            if (allowMissing && error is 2 or 3)
                return null;
            throw new IOException(errorMessage, new Win32Exception(error));
        }
        finally
        {
            Marshal.FreeHGlobal(unicodeStringPointer);
            Marshal.FreeHGlobal(nameBuffer);
        }
    }

    [SupportedOSPlatform("windows")]
    private static void ValidateWindowsDirectoryHandle(
        SafeFileHandle handle,
        string displayPath)
    {
        var attributes = GetWindowsHandleAttributes(handle);
        if ((attributes & FileAttributeDirectory) == 0 ||
            (attributes & FileAttributeReparsePoint) != 0)
        {
            handle.Dispose();
            throw new IOException(
                $"Contained directory is not an ordinary non-reparse directory: {displayPath}");
        }
    }

    [SupportedOSPlatform("windows")]
    private static void ValidateWindowsEntry(
        string root,
        SafeFileHandle handle,
        string displayPath,
        bool directory,
        SafeFileHandle? rootAuthority = null)
    {
        var attributes = GetWindowsHandleAttributes(handle);
        var isDirectory = (attributes & FileAttributeDirectory) != 0;
        if (isDirectory != directory ||
            (attributes & FileAttributeReparsePoint) != 0)
        {
            throw new IOException(
                $"Contained entry is not an ordinary {(directory ? "directory" : "file")}: {displayPath}");
        }

        using var rootHandle = rootAuthority is null
            ? OpenWindowsDirectory(root)
            : DuplicateWindowsHandle(rootAuthority);
        var rootFinalPath = GetWindowsFinalPath(rootHandle);
        var finalPath = GetWindowsFinalPath(handle);
        if (!IsLexicallyContained(rootFinalPath, finalPath))
            throw new IOException($"Opened entry escaped the workspace root: {displayPath}");
    }

    [SupportedOSPlatform("windows")]
    private static uint GetWindowsHandleAttributes(SafeFileHandle handle)
    {
        if (!GetFileInformationByHandleEx(
                handle,
                FileAttributeTagInfoClass,
                out var information,
                (uint)Marshal.SizeOf<FileAttributeTagInfo>()))
        {
            throw new IOException(
                "Unable to inspect the opened filesystem object.",
                new Win32Exception(Marshal.GetLastPInvokeError()));
        }

        return information.FileAttributes;
    }

    [SupportedOSPlatform("windows")]
    private static string GetWindowsFinalPath(SafeFileHandle handle)
    {
        var buffer = new StringBuilder(512);
        var length = GetFinalPathNameByHandle(
            handle,
            buffer,
            (uint)buffer.Capacity,
            0);
        if (length == 0)
        {
            throw new IOException(
                "Unable to resolve the opened filesystem object.",
                new Win32Exception(Marshal.GetLastPInvokeError()));
        }

        if (length >= buffer.Capacity)
        {
            buffer.EnsureCapacity(checked((int)length + 1));
            length = GetFinalPathNameByHandle(
                handle,
                buffer,
                (uint)buffer.Capacity,
                0);
            if (length == 0)
            {
                throw new IOException(
                    "Unable to resolve the opened filesystem object.",
                    new Win32Exception(Marshal.GetLastPInvokeError()));
            }
        }

        var path = buffer.ToString();
        const string deviceUncPrefix = @"\\?\UNC\";
        const string devicePrefix = @"\\?\";
        if (path.StartsWith(deviceUncPrefix, StringComparison.OrdinalIgnoreCase))
            path = @"\\" + path[deviceUncPrefix.Length..];
        else if (path.StartsWith(devicePrefix, StringComparison.OrdinalIgnoreCase))
            path = path[devicePrefix.Length..];
        return Path.GetFullPath(path);
    }

    [SupportedOSPlatform("linux")]
    private static UnixStatx FStatUnix(int descriptor)
    {
        if (StatxUnix(
                descriptor,
                string.Empty,
                UnixAtEmptyPath | UnixAtSymlinkNoFollow,
                UnixStatxBasicStats,
                out var information) != 0)
        {
            throw CreateUnixIOException(
                $"Unable to inspect Linux descriptor {descriptor}.");
        }

        return information;
    }

    private static void EnsureLinuxRegularFile(UnixStatx information, string displayPath)
    {
        if ((information.Mode & UnixFileTypeMask) != UnixRegularFile)
            throw new IOException($"Contained entry is not an ordinary regular file: {displayPath}");
    }

    private static void EnsureLinuxDirectory(UnixStatx information, string displayPath)
    {
        if ((information.Mode & UnixFileTypeMask) != UnixDirectory)
            throw new IOException($"Contained entry is not an ordinary directory: {displayPath}");
    }

    private static void EnsureSameLinuxObject(
        UnixStatx expected,
        UnixStatx actual,
        string displayPath)
    {
        if (expected.Inode != actual.Inode ||
            expected.DeviceMajor != actual.DeviceMajor ||
            expected.DeviceMinor != actual.DeviceMinor)
        {
            throw new IOException($"Contained file changed while it was opened: {displayPath}");
        }
    }

    private static IEnumerable<string> SplitContainedSegments(string relative)
    {
        foreach (var segment in relative.Split(
                     [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                     StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment is "." or "..")
                throw new IOException($"Invalid contained directory segment: {segment}");
            yield return segment;
        }
    }

    private static string GetContainedEntryName(string path)
    {
        var name = Path.GetFileName(path);
        if (string.IsNullOrEmpty(name) || name is "." or "..")
            throw new IOException($"Invalid contained entry name: {path}");
        return name;
    }

    private static IOException CreateUnixIOException(string message) =>
        CreateUnixIOException(message, Marshal.GetLastPInvokeError());

    private static IOException CreateUnixIOException(string message, int error) =>
        new(message, new Win32Exception(error));

    private static void DisposeHandles(List<SafeFileHandle> handles)
    {
        for (var index = handles.Count - 1; index >= 0; index--)
            handles[index].Dispose();
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FileAttributeTagInfo
    {
        public uint FileAttributes;
        public uint ReparseTag;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FileDispositionInformation
    {
        public byte DeleteFile;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct UnicodeString
    {
        public ushort Length;
        public ushort MaximumLength;
        public IntPtr Buffer;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ObjectAttributes
    {
        public int Length;
        public IntPtr RootDirectory;
        public IntPtr ObjectName;
        public uint Attributes;
        public IntPtr SecurityDescriptor;
        public IntPtr SecurityQualityOfService;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IoStatusBlock
    {
        public IntPtr Status;
        public UIntPtr Information;
    }

    [StructLayout(LayoutKind.Explicit, Size = 256)]
    internal struct UnixStatx
    {
        [FieldOffset(28)]
        public ushort Mode;

        [FieldOffset(32)]
        public ulong Inode;

        [FieldOffset(40)]
        public ulong Size;

        [FieldOffset(136)]
        public uint DeviceMajor;

        [FieldOffset(140)]
        public uint DeviceMinor;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFile(
        string fileName,
        uint desiredAccess,
        uint shareMode,
        IntPtr securityAttributes,
        uint creationDisposition,
        uint flagsAndAttributes,
        IntPtr templateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetFileInformationByHandleEx(
        SafeFileHandle fileHandle,
        int fileInformationClass,
        out FileAttributeTagInfo fileInformation,
        uint bufferSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetFileInformationByHandle(
        SafeFileHandle fileHandle,
        int fileInformationClass,
        ref FileDispositionInformation fileInformation,
        uint bufferSize);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint GetFinalPathNameByHandle(
        SafeFileHandle fileHandle,
        StringBuilder filePath,
        uint filePathLength,
        uint flags);

    [DllImport("ntdll.dll")]
    private static extern int NtCreateFile(
        out SafeFileHandle? fileHandle,
        uint desiredAccess,
        ref ObjectAttributes objectAttributes,
        out IoStatusBlock ioStatusBlock,
        IntPtr allocationSize,
        uint fileAttributes,
        uint shareAccess,
        uint createDisposition,
        uint createOptions,
        IntPtr extendedAttributes,
        uint extendedAttributesLength);

    [DllImport("ntdll.dll")]
    private static extern uint RtlNtStatusToDosError(int status);

    [DllImport("libc", EntryPoint = "open", SetLastError = true)]
    private static extern int OpenUnix(string path, int flags);

    [DllImport("libc", EntryPoint = "openat", SetLastError = true)]
    private static extern int OpenAtUnix(
        int directoryDescriptor,
        string path,
        int flags,
        uint mode);

    [DllImport("libc", EntryPoint = "mkdirat", SetLastError = true)]
    private static extern int MakeDirectoryAtUnix(
        int directoryDescriptor,
        string path,
        uint mode);

    [DllImport("libc", EntryPoint = "unlinkat", SetLastError = true)]
    private static extern int UnlinkAtUnix(
        int directoryDescriptor,
        string path,
        int flags);

    [DllImport("libc", EntryPoint = "statx", SetLastError = true)]
    private static extern int StatxUnix(
        int directoryDescriptor,
        string path,
        int flags,
        uint mask,
        out UnixStatx information);
}
