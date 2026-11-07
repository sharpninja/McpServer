using System.Buffers.Binary;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace QBrainAi.Support.Mcp.Tests.TestSupport;

/// <summary>
/// Creates a Windows directory junction with operating-system calls made in the test process: the alias folder is
/// created, opened as a reparse point (CreateFileW) and given a mount-point reparse buffer
/// (DeviceIoControl FSCTL_SET_REPARSE_POINT), so a fixture never depends on how long a helper process takes to start
/// (TR-MCP-USECASE-019 AC-008 as replaced 2026-10-26T14:18:23; operator: "Fix flake, strengthen AC (Recommended)";
/// triage-report-8c2c30ef63d442bdb29fd785ec91e7d7).
/// </summary>
internal static class WindowsJunction
{
    /// <summary>IO_REPARSE_TAG_MOUNT_POINT, the reparse tag of a directory junction.</summary>
    internal const uint MountPointTag = 0xA0000003;

    private const uint SetReparsePointControl = 0x000900A4;
    private const uint GenericWrite = 0x40000000;
    private const uint OpenExisting = 3;
    private const uint FlagOpenReparsePoint = 0x00200000;
    private const uint FlagBackupSemantics = 0x02000000;

    /// <summary>
    /// FR-MCP-TESTRESOURCE-001 ac-11: builds the mount-point REPARSE_DATA_BUFFER for <paramref name="targetFullPath"/>: the tag, the data length,
    /// a reserved field, the substitute and print name offsets and lengths, then the UTF-16 substitute name "\??\"
    /// plus the target, a null, the print name (the target) and a null.
    /// </summary>
    /// <param name="targetFullPath">The absolute target folder.</param>
    /// <returns>The buffer bytes.</returns>
    internal static byte[] BuildMountPointBuffer(string targetFullPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(targetFullPath);
        var substitute = Encoding.Unicode.GetBytes(@"\??\" + targetFullPath);
        var print = Encoding.Unicode.GetBytes(targetFullPath);
        var pathLength = substitute.Length + 2 + print.Length + 2;
        var buffer = new byte[16 + pathLength];
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(0, 4), MountPointTag);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(4, 2), checked((ushort)(8 + pathLength)));
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(10, 2), checked((ushort)substitute.Length));
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(12, 2), checked((ushort)(substitute.Length + 2)));
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(14, 2), checked((ushort)print.Length));
        substitute.CopyTo(buffer, 16);
        print.CopyTo(buffer, 16 + substitute.Length + 2);
        return buffer;
    }

    /// <summary>
    /// Creates the junction <paramref name="aliasPath"/> that points at <paramref name="targetPath"/>; the alias folder
    /// must not exist. On failure the alias folder is removed and a <see cref="Win32Exception"/> is thrown.
    /// </summary>
    /// <param name="aliasPath">The junction to create.</param>
    /// <param name="targetPath">The existing folder it points at.</param>
    [SupportedOSPlatform("windows")]
    internal static void Create(string aliasPath, string targetPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(aliasPath);
        ArgumentException.ThrowIfNullOrEmpty(targetPath);
        var buffer = BuildMountPointBuffer(Path.GetFullPath(targetPath));
        Directory.CreateDirectory(aliasPath);
        try
        {
            using var handle = CreateFile(aliasPath, GenericWrite, 0, IntPtr.Zero, OpenExisting, FlagOpenReparsePoint | FlagBackupSemantics, IntPtr.Zero);
            if (handle.IsInvalid)
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"Opening {aliasPath} as a reparse point failed.");
            if (!DeviceIoControl(handle, SetReparsePointControl, buffer, (uint)buffer.Length, IntPtr.Zero, 0, out _, IntPtr.Zero))
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"Setting the junction reparse point on {aliasPath} failed.");
        }
        catch
        {
            Directory.Delete(aliasPath);
            throw;
        }
    }

    [DllImport(
        "kernel32.dll",
        EntryPoint = "CreateFileW",
        CharSet = CharSet.Unicode,
        SetLastError = true)]
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
    private static extern bool DeviceIoControl(
        SafeFileHandle device,
        uint controlCode,
        byte[] inBuffer,
        uint inBufferSize,
        IntPtr outBuffer,
        uint outBufferSize,
        out uint bytesReturned,
        IntPtr overlapped);
}
