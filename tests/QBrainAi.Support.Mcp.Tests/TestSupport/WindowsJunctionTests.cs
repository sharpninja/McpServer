using System.Buffers.Binary;
using System.Text;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.TestSupport;

/// <summary>
/// Tests the mount-point reparse buffer a test fixture writes to make a Windows directory junction with an
/// operating-system call in the test process, instead of starting a helper process on a time budget.
/// </summary>
/// <remarks>
/// Requirement coverage: TR-MCP-USECASE-019 AC-008 as replaced 2026-10-26T14:18:23 ("A test fixture that needs a
/// directory junction or link creates it with an operating-system call made in the test process"; operator: "Fix
/// flake, strengthen AC (Recommended)"; triage-report-8c2c30ef63d442bdb29fd785ec91e7d7). Test data: the documented
/// REPARSE_DATA_BUFFER layout for IO_REPARSE_TAG_MOUNT_POINT (tag 0xA0000003; ReparseDataLength; Reserved;
/// SubstituteNameOffset, SubstituteNameLength, PrintNameOffset, PrintNameLength; then the UTF-16 substitute name
/// "\??\" plus the target, a null, the print name, a null), pinned as literal bytes for one target and decoded field
/// by field for another, run against a builder passed in, so the contract is proved on a scripted reference builder
/// with negative controls before the fixture's builder exists. The checks are pure byte comparisons on every host and
/// touch no file system or environment, so they are unit tests.
/// </remarks>
public sealed class WindowsJunctionTests
{
    private const uint MountPointTag = 0xA0000003;

    /// <summary>The documented buffer for the target C:\a, byte by byte.</summary>
    private static readonly byte[] PinnedBufferForCDriveA =
    [
        0x03, 0x00, 0x00, 0xA0, // ReparseTag 0xA0000003
        0x24, 0x00, // ReparseDataLength 36
        0x00, 0x00, // Reserved
        0x00, 0x00, // SubstituteNameOffset 0
        0x10, 0x00, // SubstituteNameLength 16
        0x12, 0x00, // PrintNameOffset 18
        0x08, 0x00, // PrintNameLength 8
        0x5C, 0x00, 0x3F, 0x00, 0x3F, 0x00, 0x5C, 0x00, 0x43, 0x00, 0x3A, 0x00, 0x5C, 0x00, 0x61, 0x00, // \??\C:\a
        0x00, 0x00, // null
        0x43, 0x00, 0x3A, 0x00, 0x5C, 0x00, 0x61, 0x00, // C:\a
        0x00, 0x00, // null
    ];

    /// <summary>
    /// Asserts that <paramref name="build"/> gives the pinned bytes for C:\a and, for a longer target with a space,
    /// a buffer whose fields decode to the documented layout.
    /// </summary>
    /// <param name="build">The buffer builder under test.</param>
    internal static void AssertBufferContract(Func<string, byte[]> build)
    {
        ArgumentNullException.ThrowIfNull(build);
        Assert.Equal(PinnedBufferForCDriveA, build(@"C:\a"));

        const string target = @"D:\Temp\mcp twelfth external";
        var buffer = build(target);
        var substitute = @"\??\" + target;
        Assert.Equal(MountPointTag, BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(0, 4)));
        Assert.Equal(buffer.Length - 8, BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(4, 2)));
        Assert.Equal(0, BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(6, 2)));
        Assert.Equal(0, BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(8, 2)));
        Assert.Equal(substitute.Length * 2, BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(10, 2)));
        Assert.Equal((substitute.Length * 2) + 2, BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(12, 2)));
        Assert.Equal(target.Length * 2, BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(14, 2)));
        Assert.Equal(substitute, Encoding.Unicode.GetString(buffer, 16, substitute.Length * 2));
        Assert.Equal(0, BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(16 + (substitute.Length * 2), 2)));
        Assert.Equal(target, Encoding.Unicode.GetString(buffer, 16 + (substitute.Length * 2) + 2, target.Length * 2));
        Assert.Equal(0, BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(buffer.Length - 2, 2)));
        Assert.Equal(16 + (substitute.Length * 2) + 2 + (target.Length * 2) + 2, buffer.Length);
    }

    /// <summary>
    /// Binding: the fixture's builder, <see cref="WindowsJunction.BuildMountPointBuffer"/>, satisfies the same
    /// contract the scripted reference passed and the wrong builders failed, and its tag is the documented one.
    /// </summary>
    [Fact]
    public void FixtureBuilder_SatisfiesTheContract()
    {
        Assert.Equal(MountPointTag, WindowsJunction.MountPointTag);
        AssertBufferContract(WindowsJunction.BuildMountPointBuffer);
    }

    /// <summary>Mock gate positive control: the scripted reference builder satisfies the contract.</summary>
    [Fact]
    public void ScriptedReferenceBuilder_SatisfiesTheContract() =>
        AssertBufferContract(target => Scripted(target, MountPointTag, @"\??\", nullGap: true, headerInLength: false));

    /// <summary>
    /// Mock gate negative controls, each rejected inside a passing harness: the symbolic-link tag instead of the
    /// mount-point tag, a substitute name without the \??\ prefix, a print name that starts without the null after the
    /// substitute name, and a data length that also counts the 8-byte header.
    /// </summary>
    [Fact]
    public void WrongBuilders_AreRejectedInsidePassingHarness()
    {
        Assert.NotNull(Record.Exception(() => AssertBufferContract(target => Scripted(target, 0xA000000C, @"\??\", nullGap: true, headerInLength: false))));
        Assert.NotNull(Record.Exception(() => AssertBufferContract(target => Scripted(target, MountPointTag, string.Empty, nullGap: true, headerInLength: false))));
        Assert.NotNull(Record.Exception(() => AssertBufferContract(target => Scripted(target, MountPointTag, @"\??\", nullGap: false, headerInLength: false))));
        Assert.NotNull(Record.Exception(() => AssertBufferContract(target => Scripted(target, MountPointTag, @"\??\", nullGap: true, headerInLength: true))));
    }

    private static byte[] Scripted(string target, uint tag, string prefix, bool nullGap, bool headerInLength)
    {
        var substitute = Encoding.Unicode.GetBytes(prefix + target);
        var print = Encoding.Unicode.GetBytes(target);
        var gap = nullGap ? 2 : 0;
        var pathLength = substitute.Length + gap + print.Length + 2;
        var buffer = new byte[16 + pathLength];
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(0, 4), tag);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(4, 2), (ushort)(8 + pathLength + (headerInLength ? 8 : 0)));
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(10, 2), (ushort)substitute.Length);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(12, 2), (ushort)(substitute.Length + gap));
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(14, 2), (ushort)print.Length);
        substitute.CopyTo(buffer, 16);
        print.CopyTo(buffer, 16 + substitute.Length + gap);
        return buffer;
    }
}

/// <summary>
/// Binding on a Windows host: <see cref="WindowsJunction.Create"/> makes a real directory junction in the test process
/// that the file system resolves to its target. It creates folders and a file under the official temporary folder and
/// needs the Windows file system, so it is an integration test on Windows hosts (TR-MCP-USECASE-019 AC-008 as replaced
/// 2026-10-26T14:18:23; TR-MCP-NUKE-019).
/// </summary>
[Trait("Category", "Integration")]
[Trait("Platform", "Windows")]
public sealed class WindowsJunctionWindowsTests
{
    /// <summary>
    /// A junction created in the test process is a mount-point reparse point whose link target is the target folder,
    /// and a file read through it is the target's file.
    /// </summary>
    [Fact]
    public void Create_MakesAJunctionThatResolvesToItsTarget()
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("This integration test runs only in the Windows lane.");
        var root = Path.Combine(Path.GetTempPath(), "mcp-junction-" + Guid.NewGuid().ToString("N"));
        var target = Path.Combine(root, "target");
        var alias = Path.Combine(root, "alias");
        Directory.CreateDirectory(target);
        File.WriteAllText(Path.Combine(target, "document.md"), "through the junction");
        try
        {
            WindowsJunction.Create(alias, target);

            var info = new DirectoryInfo(alias);
            Assert.True(info.Attributes.HasFlag(FileAttributes.ReparsePoint));
            Assert.Equal(Path.GetFullPath(target), Path.GetFullPath(Assert.IsType<string>(info.LinkTarget)));
            Assert.Equal("through the junction", File.ReadAllText(Path.Combine(alias, "document.md")));
        }
        finally
        {
            if (Directory.Exists(alias))
                Directory.Delete(alias);
            Directory.Delete(root, recursive: true);
        }
    }
}
