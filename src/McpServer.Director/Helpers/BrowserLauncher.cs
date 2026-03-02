using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace McpServer.Director.Helpers;

/// <summary>
/// Cross-platform service that opens a URL in the user's default browser.
/// Registered as a singleton in DI via <see cref="DirectorServiceRegistration"/>.
/// </summary>
internal sealed class BrowserLauncher : IBrowserLauncher
{
    private readonly ILogger<BrowserLauncher> _logger;

    /// <summary>Initializes a new instance of the <see cref="BrowserLauncher"/> class.</summary>
    /// <param name="logger">Logger instance.</param>
    public BrowserLauncher(ILogger<BrowserLauncher> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public bool TryOpenUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            _logger.LogWarning("TryOpenUrl called with null or empty URL");
            return false;
        }

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
            else
            {
                _logger.LogWarning("Unsupported OS platform for browser launch");
                return false;
            }

            _logger.LogInformation("Opened browser for URL: {Url}", url);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to open URL: {Url}", url);
            return false;
        }
    }
}
