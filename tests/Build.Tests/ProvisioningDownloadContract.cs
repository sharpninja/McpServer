using System.Net;

namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-019 (AC-8): Contract cases and assertions for <see cref="IProvisioningDownload"/>. The expected
/// behavior comes from the acceptance criterion: the body is written to the destination as it arrives, the download
/// has no limit on its total duration, and it fails naming the URL on an error status or when no data arrives for the
/// inactivity limit. Every case runs through a scripted <see cref="HttpMessageHandler"/> in memory (no network) and
/// writes under a temporary folder it removes. The same assertions run against the scripted download (BDPv4 mock
/// gate), wrong downloads (negative controls), and the production download.
/// </summary>
/// <remarks>
/// The timed cases keep wide margins so machine load cannot flip them: chunks arrive every 100 ms against a 1 s
/// inactivity limit, the steady transfer runs about 2.5 s (two and a half inactivity limits), and a download that does
/// not fail a stalled transfer is cut off by the harness after 10 s. The client check catches a client that keeps
/// HttpClient's default 100 s total limit, which no short transfer could reveal.
/// </remarks>
internal static class ProvisioningDownloadContract
{
    /// <summary>The URL every case downloads.</summary>
    internal static readonly Uri Url = new("https://downloads.example.test/provisioning/release-asset.bin");

    /// <summary>The inactivity limit every timed case passes.</summary>
    internal static readonly TimeSpan InactivityLimit = TimeSpan.FromSeconds(1);

    /// <summary>How long the harness waits for a stalled download to fail before it rejects the download.</summary>
    internal static readonly TimeSpan StallCutoff = TimeSpan.FromSeconds(10);

    /// <summary>Asserts every contract case against <paramref name="download"/>.</summary>
    /// <param name="download">Download under test.</param>
    internal static async Task AssertDownloadAsync(IProvisioningDownload download)
    {
        AssertClientHasNoTotalLimit(download);
        await AssertWritesAsItArrivesAsync(download).ConfigureAwait(true);
        await AssertSteadySlowTransferCompletesAsync(download).ConfigureAwait(true);
        await AssertStalledTransferFailsNamingUrlAsync(download).ConfigureAwait(true);
        await AssertErrorStatusFailsNamingUrlAsync(download).ConfigureAwait(true);
    }

    /// <summary>The client a download creates has no total time limit.</summary>
    internal static void AssertClientHasNoTotalLimit(IProvisioningDownload download)
    {
        using var handler = new ScriptedHandler(HttpStatusCode.OK, () => new ChunkStream(1, TimeSpan.Zero, null));
        using var client = download.CreateClient(handler);
        Assert.Equal(Timeout.InfiniteTimeSpan, client.Timeout);
    }

    /// <summary>Eight 64 KB chunks: the destination holds every byte, and part of it was on disk before the body ended.</summary>
    internal static async Task AssertWritesAsItArrivesAsync(IProvisioningDownload download)
    {
        using var folder = new TemporaryFolder();
        var destination = Path.Combine(folder.Path, "asset.bin");
        ChunkStream? stream = null;
        using var handler = new ScriptedHandler(HttpStatusCode.OK, () => stream = new ChunkStream(8, TimeSpan.Zero, destination));

        await AssertCompletesAsync(download, handler, destination).ConfigureAwait(true);

        Assert.NotNull(stream);
        Assert.Equal(ChunkStream.Expected(8), await File.ReadAllBytesAsync(destination).ConfigureAwait(true));
        Assert.True(stream!.SawPartialFileBeforeEnd, "The body was not written to the destination as it arrived.");
    }

    /// <summary>25 chunks 100 ms apart (about 2.5 inactivity limits in all) download to completion.</summary>
    internal static async Task AssertSteadySlowTransferCompletesAsync(IProvisioningDownload download)
    {
        using var folder = new TemporaryFolder();
        var destination = Path.Combine(folder.Path, "asset.bin");
        using var handler = new ScriptedHandler(HttpStatusCode.OK, () => new ChunkStream(25, TimeSpan.FromMilliseconds(100), null));

        await AssertCompletesAsync(download, handler, destination).ConfigureAwait(true);

        Assert.Equal(ChunkStream.Expected(25), await File.ReadAllBytesAsync(destination).ConfigureAwait(true));
    }

    /// <summary>A download that must complete: any exception it throws fails the contract with that exception's text.</summary>
    private static async Task AssertCompletesAsync(IProvisioningDownload download, HttpMessageHandler handler, string destination)
    {
        var failure = await Record.ExceptionAsync(
            () => download.DownloadAsync(handler, Url, destination, InactivityLimit, CancellationToken.None)).ConfigureAwait(true);
        Assert.True(failure is null, $"The download did not complete: {failure?.GetType().Name}: {failure?.Message}");
    }

    /// <summary>One chunk, then no data: the download fails within the cutoff with the URL in the message.</summary>
    internal static async Task AssertStalledTransferFailsNamingUrlAsync(IProvisioningDownload download)
    {
        using var folder = new TemporaryFolder();
        var destination = Path.Combine(folder.Path, "asset.bin");
        using var handler = new ScriptedHandler(HttpStatusCode.OK, () => new ChunkStream(1, TimeSpan.Zero, null, stallAfter: true));
        using var cutoff = new CancellationTokenSource(StallCutoff);

        var attempt = download.DownloadAsync(handler, Url, destination, InactivityLimit, cutoff.Token);
        var failure = await Record.ExceptionAsync(() => attempt).ConfigureAwait(true);

        Assert.NotNull(failure);
        Assert.False(cutoff.IsCancellationRequested, "A stalled transfer did not fail within the inactivity limit.");
        Assert.Contains(Url.ToString(), failure!.Message, StringComparison.Ordinal);
    }

    /// <summary>A 404 answer fails the download with the URL in the message.</summary>
    internal static async Task AssertErrorStatusFailsNamingUrlAsync(IProvisioningDownload download)
    {
        using var folder = new TemporaryFolder();
        var destination = Path.Combine(folder.Path, "asset.bin");
        using var handler = new ScriptedHandler(HttpStatusCode.NotFound, () => new ChunkStream(1, TimeSpan.Zero, null));

        var failure = await Record.ExceptionAsync(
            () => download.DownloadAsync(handler, Url, destination, InactivityLimit, CancellationToken.None)).ConfigureAwait(true);

        Assert.NotNull(failure);
        Assert.Contains(Url.ToString(), failure!.Message, StringComparison.Ordinal);
    }

    /// <summary>Answers every request with the status and a fresh body stream.</summary>
    internal sealed class ScriptedHandler(HttpStatusCode status, Func<Stream> body) : HttpMessageHandler
    {
        /// <inheritdoc />
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(status) { Content = new StreamContent(body()), RequestMessage = request });
    }

    /// <summary>
    /// A body of 64 KB chunks with an optional delay before each chunk and an optional stall after the last one. When
    /// given the destination path, it records whether the file already held data when a later chunk was read.
    /// </summary>
    internal sealed class ChunkStream(int chunks, TimeSpan delay, string? destination, bool stallAfter = false) : Stream
    {
        /// <summary>The size of each chunk.</summary>
        internal const int ChunkSize = 64 * 1024;

        private int _chunk;
        private int _offset;

        /// <summary>Gets whether the destination already held data when a later chunk was read.</summary>
        internal bool SawPartialFileBeforeEnd { get; private set; }

        /// <inheritdoc />
        public override bool CanRead => true;

        /// <inheritdoc />
        public override bool CanSeek => false;

        /// <inheritdoc />
        public override bool CanWrite => false;

        /// <inheritdoc />
        public override long Length => throw new NotSupportedException();

        /// <inheritdoc />
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        /// <summary>The bytes the body holds for <paramref name="chunks"/> chunks.</summary>
        internal static byte[] Expected(int chunks)
        {
            var bytes = new byte[chunks * ChunkSize];
            for (var i = 0; i < bytes.Length; i++)
                bytes[i] = (byte)((i / ChunkSize) + 1);
            return bytes;
        }

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
            => ReadAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();

        /// <inheritdoc />
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_chunk >= chunks)
            {
                if (stallAfter)
                    await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
                return 0;
            }

            if (_offset == 0)
            {
                if (_chunk >= 2 && destination is not null && File.Exists(destination) && new FileInfo(destination).Length > 0)
                    SawPartialFileBeforeEnd = true;
                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }

            var count = Math.Min(buffer.Length, ChunkSize - _offset);
            buffer.Span[..count].Fill((byte)(_chunk + 1));
            _offset += count;
            if (_offset == ChunkSize)
            {
                _offset = 0;
                _chunk++;
            }

            return count;
        }

        /// <inheritdoc />
        public override void Flush()
        {
        }

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        /// <inheritdoc />
        public override void SetLength(long value) => throw new NotSupportedException();

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    /// <summary>A temporary folder removed on dispose.</summary>
    private sealed class TemporaryFolder : IDisposable
    {
        public TemporaryFolder()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "provisioning-download-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}
