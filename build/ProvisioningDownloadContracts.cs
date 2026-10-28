/// <summary>
/// TR-MCP-NUKE-019 AC-8: how a provisioning download (the portable Ollama release, the SQL Server LocalDB installer,
/// the PostgreSQL binaries) reaches disk. The body is written to the destination file as it arrives and the download
/// has no limit on its total duration; it fails, naming the URL, when the server answers an error status or when no
/// data arrives for the inactivity limit.
/// </summary>
interface IProvisioningDownload
{
    /// <summary>Creates the HTTP client a download uses over <paramref name="handler"/>; it has no total time limit.</summary>
    /// <param name="handler">The message handler the client sends through; the caller owns it.</param>
    HttpClient CreateClient(HttpMessageHandler handler);

    /// <summary>
    /// Downloads <paramref name="url"/> to <paramref name="destinationPath"/>, writing the body as it arrives. Throws,
    /// with the URL in the message, on an error status or when no data arrives for <paramref name="inactivityLimit"/>.
    /// </summary>
    /// <param name="handler">The message handler the download sends through; the caller owns it.</param>
    /// <param name="url">The file to download.</param>
    /// <param name="destinationPath">The file the body is written to.</param>
    /// <param name="inactivityLimit">The longest time without any data before the download fails.</param>
    /// <param name="cancellationToken">Cancels the download.</param>
    Task DownloadAsync(
        HttpMessageHandler handler,
        Uri url,
        string destinationPath,
        TimeSpan inactivityLimit,
        CancellationToken cancellationToken);
}
