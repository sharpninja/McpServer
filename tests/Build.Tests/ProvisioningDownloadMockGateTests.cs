namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-019, BDPv4 mock gate: proves the <see cref="ProvisioningDownloadContract"/> assertions accept a
/// scripted download that streams to disk with no total limit and an inactivity limit, and reject deliberately wrong
/// downloads (negative controls), before any production download code is written. Every case runs in memory through
/// a scripted message handler; no network is used.
/// </summary>
public sealed class ProvisioningDownloadMockGateTests
{
    /// <summary>TR-MCP-NUKE-019: the scripted download passes every contract case.</summary>
    [Fact]
    public async Task ScriptedDownload_PassesContract()
        => await ProvisioningDownloadContract.AssertDownloadAsync(new ScriptedDownload(Fault.None)).ConfigureAwait(true);

    /// <summary>
    /// TR-MCP-NUKE-019 negative controls, each rejected inside this passing test: a download that buffers the whole
    /// body before writing, a client with HttpClient's default 100 s total limit, an inactivity deadline that is never
    /// reset, no inactivity limit at all, and failures that do not name the URL.
    /// </summary>
    /// <param name="fault">Injected fault.</param>
    [Theory]
    [InlineData(Fault.BuffersWholeBody)]
    [InlineData(Fault.DefaultClientTimeout)]
    [InlineData(Fault.DeadlineNotReset)]
    [InlineData(Fault.NoInactivityLimit)]
    [InlineData(Fault.UnnamedFailure)]
    public async Task WrongDownload_IsRejected(Fault fault)
        => await Assert.ThrowsAnyAsync<Xunit.Sdk.XunitException>(
            () => ProvisioningDownloadContract.AssertDownloadAsync(new ScriptedDownload(fault))).ConfigureAwait(true);

    /// <summary>Faults injected into the scripted download for negative controls.</summary>
    public enum Fault
    {
        /// <summary>No fault: streams to disk, no total limit, the inactivity limit reset on every read.</summary>
        None,

        /// <summary>Reads the whole body into memory (HttpClient's default completion) before writing the file.</summary>
        BuffersWholeBody,

        /// <summary>Keeps HttpClient's default 100 s total limit.</summary>
        DefaultClientTimeout,

        /// <summary>Sets the inactivity deadline once for the whole transfer instead of after every read.</summary>
        DeadlineNotReset,

        /// <summary>Has no inactivity limit, so a stalled transfer waits forever.</summary>
        NoInactivityLimit,

        /// <summary>Fails without the URL in the message.</summary>
        UnnamedFailure,
    }

    /// <summary>Scripted download: the contract-derived behavior, with an optional injected fault.</summary>
    private sealed class ScriptedDownload(Fault fault) : IProvisioningDownload
    {
        /// <inheritdoc />
        public HttpClient CreateClient(HttpMessageHandler handler)
            => fault == Fault.DefaultClientTimeout
                ? new HttpClient(handler, disposeHandler: false)
                : new HttpClient(handler, disposeHandler: false) { Timeout = Timeout.InfiniteTimeSpan };

        /// <inheritdoc />
        public async Task DownloadAsync(
            HttpMessageHandler handler,
            Uri url,
            string destinationPath,
            TimeSpan inactivityLimit,
            CancellationToken cancellationToken)
        {
            using var client = CreateClient(handler);
            using var inactivity = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var limited = fault != Fault.NoInactivityLimit;
            if (limited)
                inactivity.CancelAfter(inactivityLimit);
            try
            {
                var completion = fault == Fault.BuffersWholeBody
                    ? HttpCompletionOption.ResponseContentRead
                    : HttpCompletionOption.ResponseHeadersRead;
                using var response = await client.GetAsync(url, completion, inactivity.Token).ConfigureAwait(false);
                if (fault == Fault.UnnamedFailure)
                    response.EnsureSuccessStatusCode();
                else if (!response.IsSuccessStatusCode)
                    throw new HttpRequestException($"Download of {url} failed: the server answered {(int)response.StatusCode}.");

                await using var source = await response.Content.ReadAsStreamAsync(inactivity.Token).ConfigureAwait(false);
                await using var target = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 1, useAsync: true);
                var buffer = new byte[81920];
                while (true)
                {
                    if (limited && fault != Fault.DeadlineNotReset)
                        inactivity.CancelAfter(inactivityLimit);
                    var read = await source.ReadAsync(buffer, inactivity.Token).ConfigureAwait(false);
                    if (read == 0)
                        break;
                    await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (inactivity.IsCancellationRequested && !cancellationToken.IsCancellationRequested && fault != Fault.UnnamedFailure)
            {
                throw new TimeoutException($"Download of {url} stopped: no data arrived for {inactivityLimit}.");
            }
        }
    }
}
