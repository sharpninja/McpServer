namespace McpServer.Director.Helpers;

/// <summary>
/// Abstraction for launching URLs in the user's default browser.
/// </summary>
internal interface IBrowserLauncher
{
    /// <summary>
    /// Attempts to open <paramref name="url"/> in the default browser.
    /// Returns <c>true</c> if the process launched without error; <c>false</c> otherwise.
    /// Failures are logged but never thrown.
    /// </summary>
    /// <param name="url">The URL to open.</param>
    /// <returns><c>true</c> if launched successfully; <c>false</c> otherwise.</returns>
    bool TryOpenUrl(string? url);
}
