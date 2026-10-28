/// <summary>
/// TR-MCP-NUKE-019 AC-8: the provisioning download. The client has no total time limit; the request completes at the
/// response headers, the body is written to the destination as each read arrives (an unbuffered file), and the
/// inactivity limit restarts after every read, so a large release asset that keeps arriving finishes while a stalled
/// transfer fails. An error status or a stall fails with the URL in the message.
/// </summary>
sealed class ProvisioningDownload : IProvisioningDownload
{
    /// <summary>The inactivity limit the provisioning targets use (TR-MCP-NUKE-019 AC-8: two minutes).</summary>
    internal static readonly TimeSpan DefaultInactivityLimit = TimeSpan.FromMinutes(2);

    /// <inheritdoc />
    public HttpClient CreateClient(HttpMessageHandler handler)
        => new(handler, disposeHandler: false) { Timeout = Timeout.InfiniteTimeSpan };

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
        inactivity.CancelAfter(inactivityLimit);
        try
        {
            using var response = await client
                .GetAsync(url, HttpCompletionOption.ResponseHeadersRead, inactivity.Token)
                .ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Download of {url} failed: the server answered {(int)response.StatusCode} {response.ReasonPhrase}.",
                    inner: null,
                    response.StatusCode);
            }

            await using var source = await response.Content.ReadAsStreamAsync(inactivity.Token).ConfigureAwait(false);
            await using var target = new FileStream(
                destinationPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 1,
                useAsync: true);
            var buffer = new byte[81920];
            while (true)
            {
                inactivity.CancelAfter(inactivityLimit);
                var read = await source.ReadAsync(buffer, inactivity.Token).ConfigureAwait(false);
                if (read == 0)
                    break;
                await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (inactivity.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Download of {url} stopped: no data arrived for {inactivityLimit}.");
        }
    }
}
