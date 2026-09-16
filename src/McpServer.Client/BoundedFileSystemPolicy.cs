namespace McpServer.Client;

/// <summary>
/// Rejects filesystem locations whose native I/O cannot be made finite by the current platform
/// strategy. Linux decisions are derived from the kernel mount table and symbolic-link metadata
/// on already-approved parent filesystems, so userspace and network mounts are never traversed
/// during preflight.
/// </summary>
internal static class BoundedFileSystemPolicy
{


    private static readonly HashSet<string> s_unboundedLinuxFileSystems =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "9p",
            "afs",
            "autofs",
            "ceph",
            "cifs",
            "davfs",
            "davfs2",
            "gcsfuse",
            "glusterfs",
            "lustre",
            "nfs",
            "nfs4",
            "rclone",
            "s3fs",
            "smb3",
            "sshfs",
        };

    private static readonly HashSet<string> s_specialLinuxFileSystems =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "binfmt_misc",
            "cgroup",
            "cgroup2",
            "configfs",
            "debugfs",
            "devpts",
            "devtmpfs",
            "hugetlbfs",
            "mqueue",
            "proc",
            "pstore",
            "rpc_pipefs",
            "securityfs",
            "sysfs",
            "tracefs",
        };

    /// <summary>
    /// Ensures native access to <paramref name="path"/> has a platform strategy that can honor a
    /// finite deadline without first probing the candidate filesystem.
    /// </summary>
    /// <param name="path">Absolute or relative filesystem path to classify.</param>
    /// <param name="operation">Human-readable operation included in diagnostics.</param>
    /// <exception cref="NotSupportedException">
    /// The path is on a userspace, network, automount, or special filesystem, or the host lacks a
    /// bounded native-I/O strategy.
    /// </exception>
    internal static void EnsurePathSupportsBoundedNativeIo(string path, string operation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);

        if (OperatingSystem.IsWindows())
            return;

        if (!OperatingSystem.IsLinux())
        {
            throw new NotSupportedException(
                $"{operation} is unavailable because this platform has no bounded native-I/O strategy.");
        }

        _ = GetApprovedLinuxMount(Path.GetFullPath(path), operation);
    }

    /// <summary>
    /// Classifies a lexical Linux path without traversing its directory entries and returns the
    /// normalized candidate for a subsequent kernel-enforced physical lookup.
    /// </summary>
    /// <param name="path">Candidate path.</param>
    /// <param name="operation">Human-readable operation included in diagnostics.</param>
    /// <returns>The normalized lexical candidate.</returns>
    /// <remarks>
    /// This check deliberately performs no <c>stat</c>, <c>lstat</c>, or managed link traversal.
    /// It is not a physical-identity guarantee. Linux callers must pass the result directly to
    /// <see cref="LinuxPhysicalPathResolver"/>, whose <c>openat2(RESOLVE_NO_XDEV)</c> boundary
    /// prevents a symlink replacement or mount swap from entering another filesystem.
    /// </remarks>
    internal static string EnsureLexicalPathSupportsBoundedNativeIo(
        string path,
        string operation)
    {
        EnsurePathSupportsBoundedNativeIo(path, operation);
        return Path.GetFullPath(path);
    }

    /// <summary>
    /// Returns the longest approved Linux mount containing <paramref name="path"/> so native
    /// callers can anchor <c>RESOLVE_NO_XDEV</c> at that mount instead of rejecting the safe
    /// transition from the process root.
    /// </summary>
    internal static LinuxMount GetApprovedLinuxMount(string path, string operation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);

        var normalizedPath = Path.GetFullPath(path);
        var mount = FindLinuxMount(normalizedPath);
        if (mount is null)
        {
            throw new NotSupportedException(
                $"{operation} cannot establish the Linux filesystem type for '{normalizedPath}'.");
        }

        if (IsUnboundedLinuxFileSystem(mount.Value.FileSystemType))
        {
            throw new NotSupportedException(
                $"{operation} is rejected for Linux filesystem '{mount.Value.FileSystemType}' " +
                $"mounted at '{mount.Value.MountPoint}' because native I/O cannot be bounded safely.");
        }

        return mount.Value;
    }

    private static bool IsUnboundedLinuxFileSystem(string fileSystemType) =>
        fileSystemType.StartsWith("fuse", StringComparison.OrdinalIgnoreCase) ||
        s_unboundedLinuxFileSystems.Contains(fileSystemType) ||
        s_specialLinuxFileSystems.Contains(fileSystemType);

    private static LinuxMount? FindLinuxMount(string path)
    {
        LinuxMount? best = null;
        foreach (var line in File.ReadLines("/proc/self/mountinfo"))
        {
            var separator = line.IndexOf(" - ", StringComparison.Ordinal);
            if (separator < 0)
                continue;

            var prefixFields = line[..separator].Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries);
            var suffixFields = line[(separator + 3)..].Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries);
            if (prefixFields.Length < 5 ||
                suffixFields.Length < 1 ||
                !int.TryParse(prefixFields[0], out var mountId))
            {
                continue;
            }

            var mountPoint = DecodeMountInfoPath(prefixFields[4]);
            if (!IsWithinMount(path, mountPoint))
                continue;

            if (best is null || mountPoint.Length > best.Value.MountPoint.Length)
                best = new LinuxMount(mountPoint, suffixFields[0], mountId);
        }

        return best;
    }



    private static bool IsWithinMount(string path, string mountPoint)
    {
        if (string.Equals(path, mountPoint, StringComparison.Ordinal))
            return true;
        if (string.Equals(mountPoint, "/", StringComparison.Ordinal))
            return path.StartsWith("/", StringComparison.Ordinal);

        var prefix = mountPoint.EndsWith("/", StringComparison.Ordinal)
            ? mountPoint
            : mountPoint + "/";
        return path.StartsWith(prefix, StringComparison.Ordinal);
    }

    private static string DecodeMountInfoPath(string value) =>
        value
            .Replace(@"\040", " ", StringComparison.Ordinal)
            .Replace(@"\011", "\t", StringComparison.Ordinal)
            .Replace(@"\012", "\n", StringComparison.Ordinal)
            .Replace(@"\134", @"\", StringComparison.Ordinal);

    /// <summary>Approved mount anchor and its kernel-reported filesystem type.</summary>
    internal readonly record struct LinuxMount(
        string MountPoint,
        string FileSystemType,
        int MountId);
}
