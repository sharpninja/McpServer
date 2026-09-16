using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace McpServer.Client;

/// <summary>
/// Resolves Linux physical paths from a descriptor pinned to the kernel mount that policy approved.
/// Anchoring <c>RESOLVE_NO_XDEV</c> at that mount accepts a workspace whose root is on a separate
/// local volume while still rejecting symbolic-link and mount swaps into another filesystem.
/// </summary>
internal static class LinuxPhysicalPathResolver
{
    private const long OpenAt2SystemCall = 437;
    private const ulong OpenReadOnly = 0;
    private const ulong OpenReadWrite = 0x00000002;
    private const ulong OpenCreate = 0x00000040;
    private const ulong OpenDirectory = 0x00010000;
    private const ulong OpenNoFollow = 0x00020000;
    private const ulong OpenPath = 0x00200000;
    private const ulong OpenCloseOnExec = 0x00080000;
    private const ulong OwnerReadWrite = 0x00000180;
    private const ulong ResolveNoCrossDevice = 0x01;
    private const ulong ResolveNoMagicLinks = 0x02;
    private const ulong ResolveNoSymbolicLinks = 0x04;
    private const int ErrorNoEntry = 2;
    private const int ErrorAccessDenied = 13;
    private const int ErrorCrossDevice = 18;
    private const int ErrorNotDirectory = 20;
    private const int ErrorNotImplemented = 38;
    private const int ErrorTooManyLinks = 40;
    private const int MaxAncestorDepth = 512;
    private const int MaximumLinkBuffer = 65536;

    /// <summary>
    /// Resolves the deepest existing ancestor and retains a nonexistent suffix without allowing
    /// path lookup to leave the policy-approved mount.
    /// </summary>
    /// <param name="lexicalPath">Absolute normalized Linux path.</param>
    /// <returns>The physical path, or <see langword="null"/> when no ancestor can be resolved.</returns>
    /// <exception cref="NotSupportedException">
    /// The kernel lacks <c>openat2</c>, the approved mount changes while it is pinned, or resolution
    /// would cross a mount boundary.
    /// </exception>
    internal static string? TryResolveWithNonexistentSuffix(string lexicalPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(lexicalPath);
        if (!OperatingSystem.IsLinux())
            throw new PlatformNotSupportedException("Linux physical resolution requires Linux.");

        var normalizedPath = Path.GetFullPath(lexicalPath);
        var mount = BoundedFileSystemPolicy.GetApprovedLinuxMount(
            normalizedPath,
            "Linux physical identity resolution");
        using var mountHandle = OpenApprovedMountAnchor(
            mount,
            "Linux physical identity resolution");

        var existingAncestor = normalizedPath;
        var suffix = new Stack<string>();
        for (var depth = 0; depth < MaxAncestorDepth; depth++)
        {
            if (!TryGetMountRelativePath(mount.MountPoint, existingAncestor, out var relative))
                return null;

            var openResult = TryOpenPath(mountHandle, relative, out var handle, out var error);
            if (openResult)
            {
                using (handle)
                {
                    var resolved = ReadDescriptorPath(handle);
                    if (resolved.EndsWith(" (deleted)", StringComparison.Ordinal))
                    {
                        throw new IOException(
                            "Linux physical identity changed while its descriptor was being resolved.");
                    }

                    while (suffix.TryPop(out var suffixSegment))
                        resolved = Path.Combine(resolved, suffixSegment);

                    return Path.GetFullPath(resolved);
                }
            }

            switch (error)
            {
                case ErrorNoEntry:
                case ErrorNotDirectory:
                    var parent = Path.GetDirectoryName(existingAncestor);
                    var segment = Path.GetFileName(existingAncestor);
                    if (string.IsNullOrEmpty(parent) ||
                        string.IsNullOrEmpty(segment) ||
                        string.Equals(parent, existingAncestor, StringComparison.Ordinal))
                    {
                        return null;
                    }

                    suffix.Push(segment);
                    existingAncestor = parent;
                    continue;
                case ErrorCrossDevice:
                    throw new NotSupportedException(
                        "Linux physical identity resolution rejected a mount transition, including " +
                        "FUSE and network aliases, under the anchored RESOLVE_NO_XDEV policy.");
                case ErrorNotImplemented:
                    throw new NotSupportedException(
                        "Linux physical identity resolution requires kernel openat2 support.");
                case ErrorTooManyLinks:
                    throw new IOException(
                        "Linux physical identity resolution exceeded the symbolic-link limit.");
                case ErrorAccessDenied:
                    throw new UnauthorizedAccessException(
                        $"Linux physical identity resolution cannot access '{existingAncestor}'.");
                default:
                    throw new IOException(
                        $"Linux physical identity resolution failed for '{existingAncestor}'.",
                        new Win32Exception(error));
            }
        }

        throw new IOException(
            $"Linux physical identity resolution exceeded {MaxAncestorDepth} ancestors.");
    }

    /// <summary>
    /// Atomically opens a Linux data file relative to the descriptor for its approved mount without
    /// allowing symbolic-link replacement or mount-swap traversal into another filesystem.
    /// </summary>
    /// <param name="lexicalPath">Absolute normalized Linux path.</param>
    /// <param name="access">Required read or read/write access.</param>
    /// <param name="create">Whether the final file may be created.</param>
    /// <param name="physicalPath">Pinned physical path reported by the kernel descriptor.</param>
    /// <returns>An owning descriptor for the exact opened file.</returns>
    internal static SafeFileHandle OpenFileNoCrossDevice(
        string lexicalPath,
        FileAccess access,
        bool create,
        out string physicalPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(lexicalPath);
        if (!OperatingSystem.IsLinux())
            throw new PlatformNotSupportedException("Atomic no-cross-device opening requires Linux.");
        if (access is not FileAccess.Read and not FileAccess.ReadWrite)
            throw new ArgumentOutOfRangeException(nameof(access), access, "Read or read/write access is required.");
        if (create && access == FileAccess.Read)
            throw new ArgumentException("A read-only atomic open cannot create a file.", nameof(create));

        var normalizedPath = ResolveSymlinksOnApprovedMounts(
            Path.GetFullPath(lexicalPath),
            "Atomic Linux data-file opening");
        var mount = BoundedFileSystemPolicy.GetApprovedLinuxMount(
            normalizedPath,
            "Atomic Linux data-file opening");
        using var mountHandle = OpenApprovedMountAnchor(
            mount,
            "Atomic Linux data-file opening");
        if (!TryGetMountRelativePath(mount.MountPoint, normalizedPath, out var relativePath))
        {
            throw new IOException(
                $"Atomic Linux data-file opening cannot anchor '{normalizedPath}' at " +
                $"approved mount '{mount.MountPoint}'.");
        }

        var how = new OpenHow
        {
            Flags = (access == FileAccess.Read ? OpenReadOnly : OpenReadWrite) |
                    OpenCloseOnExec |
                    (create ? OpenCreate : 0),
            Mode = create ? OwnerReadWrite : 0,
            Resolve = ResolveNoCrossDevice | ResolveNoMagicLinks | ResolveNoSymbolicLinks,
        };
        var descriptor = OpenAt2(
            OpenAt2SystemCall,
            mountHandle.DangerousGetHandle().ToInt32(),
            relativePath,
            ref how,
            (nuint)Marshal.SizeOf<OpenHow>());
        if (descriptor < 0)
        {
            var error = Marshal.GetLastPInvokeError();
            throw CreateAtomicOpenException(normalizedPath, error);
        }

        var handle = new SafeFileHandle((nint)descriptor, ownsHandle: true);
        try
        {
            physicalPath = ReadDescriptorPath(handle);
            if (physicalPath.EndsWith(" (deleted)", StringComparison.Ordinal))
            {
                throw new IOException(
                    "Atomic Linux data-file opening resolved a deleted descriptor.");
            }

            return handle;
        }
        catch
        {
            handle.Dispose();
            throw;
        }
    }
    /// <summary>
    /// Atomically captures an existing ordinary Linux directory beneath the descriptor for its
    /// policy-approved mount. The returned authority cannot resolve through a symbolic-link or
    /// cross-device replacement after provider classification.
    /// </summary>
    /// <param name="lexicalPath">Absolute normalized Linux directory path.</param>
    /// <param name="afterPolicyClassification">
    /// Test-only callback that runs after policy classification and before descriptor capture.
    /// Production callers must leave this value <see langword="null"/>.
    /// </param>
    /// <returns>An owning descriptor for the captured directory.</returns>
    /// <exception cref="DirectoryNotFoundException">The requested directory is absent.</exception>
    /// <exception cref="NotSupportedException">
    /// The kernel lacks <c>openat2</c> support or capture would cross a mount boundary.
    /// </exception>
    internal static SafeFileHandle OpenDirectoryNoCrossDevice(
        string lexicalPath,
        Action? afterPolicyClassification = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(lexicalPath);
        if (!OperatingSystem.IsLinux())
            throw new PlatformNotSupportedException("Atomic no-cross-device directory capture requires Linux.");

        var normalizedPath = Path.GetFullPath(lexicalPath);
        var mount = BoundedFileSystemPolicy.GetApprovedLinuxMount(
            normalizedPath,
            "Atomic Linux export-root capture");
        var hadIdentity = TryStatxPath(normalizedPath, out var beforeIdentity);
        afterPolicyClassification?.Invoke();
        mount = BoundedFileSystemPolicy.GetApprovedLinuxMount(
            normalizedPath,
            "Atomic Linux export-root capture");
        if (hadIdentity)
            EnsureExportRootIdentityUnchanged(normalizedPath, beforeIdentity);
        using var mountHandle = OpenApprovedMountAnchor(
            mount,
            "Atomic Linux export-root capture");
        if (!TryGetMountRelativePath(mount.MountPoint, normalizedPath, out var relativePath))
        {
            throw new IOException(
                $"Atomic Linux export-root capture cannot anchor '{normalizedPath}' at " +
                $"approved mount '{mount.MountPoint}'.");
        }

        var how = new OpenHow
        {
            Flags = OpenPath | OpenDirectory | OpenNoFollow | OpenCloseOnExec,
            Resolve = ResolveNoCrossDevice | ResolveNoMagicLinks | ResolveNoSymbolicLinks,
        };
        var descriptor = OpenAt2(
            OpenAt2SystemCall,
            mountHandle.DangerousGetHandle().ToInt32(),
            relativePath,
            ref how,
            (nuint)Marshal.SizeOf<OpenHow>());
        if (descriptor < 0)
            throw CreateAtomicDirectoryOpenException(normalizedPath, Marshal.GetLastPInvokeError());

        var handle = new SafeFileHandle((nint)descriptor, ownsHandle: true);
        try
        {
            if (ReadDescriptorPath(handle).EndsWith(" (deleted)", StringComparison.Ordinal))
            {
                throw new IOException(
                    "Atomic Linux export-root capture resolved a deleted descriptor.");
            }

            return handle;
        }
        catch
        {
            handle.Dispose();
            throw;
        }
    }



    private static SafeFileHandle OpenApprovedMountAnchor(
        BoundedFileSystemPolicy.LinuxMount mount,
        string operation)
    {
        var descriptor = Open(
            mount.MountPoint,
            checked((int)(OpenPath | OpenDirectory | OpenNoFollow | OpenCloseOnExec)));
        if (descriptor < 0)
        {
            throw new IOException(
                $"{operation} could not pin approved mount '{mount.MountPoint}'.",
                new Win32Exception(Marshal.GetLastPInvokeError()));
        }

        var handle = new SafeFileHandle((nint)descriptor, ownsHandle: true);
        try
        {
            var pinnedMountId = ReadPinnedMountId(handle);
            var currentMount = BoundedFileSystemPolicy.GetApprovedLinuxMount(
                mount.MountPoint,
                operation);
            if (pinnedMountId != mount.MountId ||
                currentMount.MountId != mount.MountId ||
                !string.Equals(
                    currentMount.FileSystemType,
                    mount.FileSystemType,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new NotSupportedException(
                    $"{operation} rejected an approved-mount replacement while pinning " +
                    $"'{mount.MountPoint}'.");
            }

            return handle;
        }
        catch
        {
            handle.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Resolves existing symbolic-link prefixes with <c>readlink</c> and mount-table classification
    /// so an absolute same-filesystem alias is not treated as a <c>RESOLVE_NO_XDEV</c> walk from
    /// <c>/</c> through a nested mount such as <c>/tmp</c>. Unbounded targets, including FUSE, fail
    /// closed during classification.
    /// </summary>
    internal static string ResolveSymlinksOnApprovedMounts(string lexicalPath, string operation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(lexicalPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        if (!OperatingSystem.IsLinux())
            throw new PlatformNotSupportedException("Linux symlink resolution requires Linux.");

        var current = Path.GetFullPath(lexicalPath);
        BoundedFileSystemPolicy.EnsurePathSupportsBoundedNativeIo(current, operation);

        for (var hops = 0; hops < MaxAncestorDepth; hops++)
        {
            var segments = current.Split(
                Path.DirectorySeparatorChar,
                StringSplitOptions.RemoveEmptyEntries);
            var accumulated = "/";
            var rebuilt = false;
            for (var index = 0; index < segments.Length; index++)
            {
                var next = accumulated == "/"
                    ? "/" + segments[index]
                    : Path.Combine(accumulated, segments[index]);
                if (TryGetLinuxLinkTarget(next, out var target))
                {
                    var resolvedTarget = Path.IsPathRooted(target)
                        ? Path.GetFullPath(target)
                        : Path.GetFullPath(Path.Combine(accumulated, target));
                    BoundedFileSystemPolicy.EnsurePathSupportsBoundedNativeIo(
                        resolvedTarget,
                        operation);
                    var remainder = segments[(index + 1)..];
                    current = remainder.Length == 0
                        ? resolvedTarget
                        : Path.Combine(resolvedTarget, Path.Combine(remainder));
                    current = Path.GetFullPath(current);
                    rebuilt = true;
                    break;
                }

                if (!LinuxPathExistsWithoutFollowing(next))
                {
                    BoundedFileSystemPolicy.EnsurePathSupportsBoundedNativeIo(current, operation);
                    return current;
                }

                accumulated = next;
            }

            if (!rebuilt)
            {
                BoundedFileSystemPolicy.EnsurePathSupportsBoundedNativeIo(accumulated, operation);
                return accumulated;
            }
        }

        throw new IOException(
            $"{operation} exceeded {MaxAncestorDepth} symbolic-link ancestors.");
    }

    private static bool TryGetLinuxLinkTarget(string path, out string target)
    {
        target = string.Empty;
        try
        {
            var attributes = File.GetAttributes(path);
            if ((attributes & FileAttributes.ReparsePoint) == 0)
                return false;

            var linkTarget = (attributes & FileAttributes.Directory) != 0
                ? new DirectoryInfo(path).LinkTarget
                : new FileInfo(path).LinkTarget;
            if (string.IsNullOrEmpty(linkTarget))
                return false;

            target = linkTarget;
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static bool LinuxPathExistsWithoutFollowing(string path)
    {
        try
        {
            _ = File.GetAttributes(path);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private const int AtFdCwd = -100;
    private const int AtSymlinkNoFollow = 0x100;
    private const uint StatxBasicStats = 0x000007ff;

    private static bool TryStatxPath(string path, out UnixStatxIdentity identity)
    {
        identity = default;
        return Statx(
            AtFdCwd,
            path,
            AtSymlinkNoFollow,
            StatxBasicStats,
            out identity) == 0;
    }

    private static void EnsureExportRootIdentityUnchanged(
        string path,
        UnixStatxIdentity beforeIdentity)
    {
        if (TryStatxPath(path, out var afterIdentity) &&
            afterIdentity.Inode == beforeIdentity.Inode &&
            afterIdentity.DeviceMajor == beforeIdentity.DeviceMajor &&
            afterIdentity.DeviceMinor == beforeIdentity.DeviceMinor)
        {
            return;
        }

        throw new NotSupportedException(
            "Atomic Linux export-root capture rejected a mount or directory identity replacement " +
            $"after policy classification at '{path}'.");
    }

    private static int ReadPinnedMountId(SafeFileHandle handle)
    {
        var descriptorInfo = $"/proc/self/fdinfo/{handle.DangerousGetHandle()}";
        foreach (var line in File.ReadLines(descriptorInfo))
        {
            if (!line.StartsWith("mnt_id:", StringComparison.Ordinal))
                continue;

            var value = line["mnt_id:".Length..].Trim();
            if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var mountId))
                return mountId;
        }

        throw new IOException(
            "Linux physical resolution could not establish the pinned descriptor mount ID.");
    }

    private static bool TryGetMountRelativePath(
        string mountPoint,
        string path,
        out string relativePath)
    {
        if (string.Equals(path, mountPoint, StringComparison.Ordinal))
        {
            relativePath = ".";
            return true;
        }

        var prefix = string.Equals(mountPoint, "/", StringComparison.Ordinal)
            ? "/"
            : mountPoint.TrimEnd('/') + "/";
        if (!path.StartsWith(prefix, StringComparison.Ordinal))
        {
            relativePath = string.Empty;
            return false;
        }

        relativePath = path[prefix.Length..];
        return relativePath.Length > 0;
    }

    private static Exception CreateAtomicDirectoryOpenException(string path, int error) =>
        error switch
        {
            ErrorNoEntry => new DirectoryNotFoundException(
                $"Atomic Linux export-root capture could not find '{path}'."),
            ErrorNotDirectory => new DirectoryNotFoundException(
                $"Atomic Linux export-root capture found a non-directory component under '{path}'."),
            ErrorCrossDevice => new NotSupportedException(
                "Atomic Linux export-root capture rejected a mount transition, including FUSE " +
                "and network aliases, under the anchored RESOLVE_NO_XDEV policy."),
            ErrorNotImplemented => new NotSupportedException(
                "Atomic Linux export-root capture requires kernel openat2 support."),
            ErrorTooManyLinks => new IOException(
                "Atomic Linux export-root capture rejected a symbolic-link component."),
            ErrorAccessDenied => new UnauthorizedAccessException(
                $"Atomic Linux export-root capture cannot access '{path}'."),
            _ => new IOException(
                $"Atomic Linux export-root capture failed for '{path}'.",
                new Win32Exception(error)),
        };

    private static Exception CreateAtomicOpenException(string path, int error) =>
        error switch
        {
            ErrorCrossDevice => new NotSupportedException(
                "Atomic Linux data-file opening rejected a mount transition, including FUSE " +
                "and network aliases, under the anchored RESOLVE_NO_XDEV policy."),
            ErrorNotImplemented => new NotSupportedException(
                "Atomic Linux data-file opening requires kernel openat2 support."),
            ErrorTooManyLinks => new IOException(
                "Atomic Linux data-file opening exceeded the symbolic-link limit."),
            ErrorAccessDenied => new UnauthorizedAccessException(
                $"Atomic Linux data-file opening cannot access '{path}'."),
            _ => new IOException(
                $"Atomic Linux data-file opening failed for '{path}'.",
                new Win32Exception(error)),
        };

    private static bool TryOpenPath(
        SafeFileHandle mountHandle,
        string relativePath,
        out SafeFileHandle handle,
        out int error)
    {
        var how = new OpenHow
        {
            Flags = OpenPath | OpenCloseOnExec,
            Resolve = ResolveNoCrossDevice | ResolveNoMagicLinks,
        };
        var descriptor = OpenAt2(
            OpenAt2SystemCall,
            mountHandle.DangerousGetHandle().ToInt32(),
            relativePath,
            ref how,
            (nuint)Marshal.SizeOf<OpenHow>());
        if (descriptor >= 0)
        {
            handle = new SafeFileHandle((nint)descriptor, ownsHandle: true);
            error = 0;
            return true;
        }

        handle = new SafeFileHandle(nint.Zero, ownsHandle: false);
        error = Marshal.GetLastPInvokeError();
        return false;
    }

    private static string ReadDescriptorPath(SafeFileHandle handle)
    {
        var descriptorPath = $"/proc/self/fd/{handle.DangerousGetHandle()}";
        var size = 1024;
        while (size <= MaximumLinkBuffer)
        {
            var buffer = new byte[size];
            var length = ReadLink(descriptorPath, buffer, (nuint)buffer.Length);
            if (length < 0)
            {
                throw new IOException(
                    "Linux physical identity could not read its pinned descriptor path.",
                    new Win32Exception(Marshal.GetLastPInvokeError()));
            }

            if (length < buffer.Length)
                return Encoding.UTF8.GetString(buffer, 0, checked((int)length));

            size = checked(size * 2);
        }

        throw new PathTooLongException(
            "Linux physical identity exceeded the descriptor-path buffer limit.");
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct OpenHow
    {
        internal ulong Flags;
        internal ulong Mode;
        internal ulong Resolve;
    }

    [DllImport("libc", EntryPoint = "open", SetLastError = true)]
    private static extern int Open(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string path,
        int flags);

    [DllImport("libc", EntryPoint = "syscall", SetLastError = true)]
    private static extern long OpenAt2(
        long number,
        int directoryDescriptor,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string path,
        ref OpenHow how,
        nuint size);

    [DllImport("libc", EntryPoint = "readlink", SetLastError = true)]
    private static extern nint ReadLink(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string path,
        byte[] buffer,
        nuint bufferSize);

    [StructLayout(LayoutKind.Explicit, Size = 256)]
    private struct UnixStatxIdentity
    {
        [FieldOffset(32)]
        internal ulong Inode;

        [FieldOffset(136)]
        internal uint DeviceMajor;

        [FieldOffset(140)]
        internal uint DeviceMinor;
    }

    [DllImport("libc", EntryPoint = "statx", SetLastError = true)]
    private static extern int Statx(
        int directoryDescriptor,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string path,
        int flags,
        uint mask,
        out UnixStatxIdentity identity);
}
