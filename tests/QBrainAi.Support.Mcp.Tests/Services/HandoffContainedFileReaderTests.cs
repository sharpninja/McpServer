using System.Text;
using QBrainAi.Support.Mcp.Services;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>TEST-HANDOFF-002: handle containment, UNC normalization, and bounded reads.</summary>
/// <remarks>2026-11-07, TEST-HANDOFF-002; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the unit half of this class: its 2 Category=Integration cases (ReadAsync_OpenShare_BlocksWritesAndDeletes and ReadAsync_LongContainedPath_Succeeds) moved unchanged to the class of the same name in the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S7, hostile validation finding R14-F4), so this unit-owned project holds only unit cases; the unit cases and the class attributes are unchanged.</remarks>

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
