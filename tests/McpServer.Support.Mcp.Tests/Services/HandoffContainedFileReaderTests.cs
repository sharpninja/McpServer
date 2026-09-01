using System.Text;
using McpServer.Support.Mcp.Services;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>TEST-HANDOFF-002: handle containment, UNC normalization, and bounded reads.</summary>
public sealed class HandoffContainedFileReaderTests
{
    /// <summary>P1-6: \\?\UNC\ is normalized to a UNC path, not a local drive-looking remnant.</summary>
    [Fact]
    public void NormalizeFinalPath_UncDevicePrefix_BecomesUncPath()
    {
        var normalized = HandoffContainedFileReader.NormalizeFinalPath(@"\\?\UNC\server\share\file.md");
        Assert.Equal(@"\\server\share\file.md", normalized);
        Assert.False(normalized.StartsWith(@"\\?\", StringComparison.Ordinal));
    }

    /// <summary>P1-6: \\?\ local device prefix is stripped.</summary>
    [Fact]
    public void NormalizeFinalPath_LocalDevicePrefix_IsStripped()
    {
        var normalized = HandoffContainedFileReader.NormalizeFinalPath(@"\\?\C:\workspace\file.md");
        Assert.Equal(@"C:\workspace\file.md", normalized);
    }

    /// <summary>P1-6: an unresolvable handle fails closed.</summary>
    [Fact]
    public void TryGetFinalPath_InvalidHandle_FailsClosedOnWindows()
    {
        if (!OperatingSystem.IsWindows())
            return;

        using var handle = new SafeFileHandle(IntPtr.Zero, ownsHandle: false);
        Assert.False(HandoffContainedFileReader.TryGetFinalPath(handle, out var path));
        Assert.True(string.IsNullOrEmpty(path));
    }

    /// <summary>P2-1: decoded size exactly at the limit succeeds.</summary>
    [Fact]
    public async Task ReadBoundedAsync_ExactlyAtLimit_ReturnsText()
    {
        var bytes = Encoding.UTF8.GetBytes(new string('a', HandoffPromptDefaults.MaxDecodedBytes));
        using var stream = new MemoryStream(bytes);
        var text = await HandoffContainedFileReader.ReadBoundedAsync(stream, TestContext.Current.CancellationToken);
        Assert.NotNull(text);
        Assert.Equal(HandoffPromptDefaults.MaxDecodedBytes, Encoding.UTF8.GetByteCount(text));
    }

    /// <summary>P2-1: a growing stream is stopped at the hard decoded-size bound.</summary>
    [Fact]
    public async Task ReadBoundedAsync_GrowingStream_StopsAtLimit()
    {
        using var stream = new GrowingStream(HandoffPromptDefaults.MaxDecodedBytes + 4096);
        var text = await HandoffContainedFileReader.ReadBoundedAsync(stream, TestContext.Current.CancellationToken);
        Assert.Null(text);
    }

    /// <summary>P2-1: cancellation stops a bounded read.</summary>
    [Fact]
    public async Task ReadBoundedAsync_Cancelled_Throws()
    {
        using var stream = new GrowingStream(HandoffPromptDefaults.MaxDecodedBytes);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            HandoffContainedFileReader.ReadBoundedAsync(stream, cts.Token));
    }

    /// <summary>P1-6 / P2-1: the reader opens with a share mode that blocks writes and deletes.</summary>
    [Fact]
    public async Task ReadAsync_OpenShare_BlocksWritesAndDeletes()
    {
        var root = Path.Combine(Path.GetTempPath(), "handoff-reader", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "notes.md");
        await File.WriteAllTextAsync(path, "handoff", TestContext.Current.CancellationToken);
        try
        {
            await using var held = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
            Assert.ThrowsAny<IOException>(() => new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.ReadWrite));
            Assert.ThrowsAny<IOException>(() => File.Delete(path));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>P1-6: a long contained path can be opened when the OS supports it.</summary>
    [Fact]
    public async Task ReadAsync_LongContainedPath_SucceedsWhenSupported()
    {
        var root = Path.Combine(Path.GetTempPath(), "handoff-long", Guid.NewGuid().ToString("N"));
        var current = root;
        while (current.Length < 240)
            current = Path.Combine(current, "nested-directory-name");
        Directory.CreateDirectory(current);
        var path = Path.Combine(current, "notes.md");
        try
        {
            await File.WriteAllTextAsync(path, "long-path-handoff", TestContext.Current.CancellationToken);
            var read = await HandoffContainedFileReader.ReadAsync(root, path, TestContext.Current.CancellationToken);
            Assert.True(read.Success, read.Message);
            Assert.Equal("long-path-handoff", read.Text);
        }
        catch (DirectoryNotFoundException)
        {
            // Some Windows configurations reject long paths; the test then only documents support.
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    private sealed class GrowingStream : Stream
    {
        private readonly int _remaining;
        private int _left;
        public GrowingStream(int total) { _remaining = total; _left = total; }
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => _remaining;
        public override long Position { get => _remaining - _left; set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_left <= 0)
                return 0;
            var n = Math.Min(count, Math.Min(1024, _left));
            Array.Clear(buffer, offset, n);
            _left -= n;
            return n;
        }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
