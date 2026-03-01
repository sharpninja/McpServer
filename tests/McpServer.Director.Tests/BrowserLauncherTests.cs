using McpServer.Director.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace McpServer.Director.Tests;

/// <summary>
/// Unit tests for <see cref="BrowserLauncher"/> guard-clause behavior.
/// Actual browser launch is not tested (requires a desktop session).
/// </summary>
public sealed class BrowserLauncherTests
{
    private readonly BrowserLauncher _sut = new(NullLogger<BrowserLauncher>.Instance);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryOpenUrl_NullOrWhitespace_ReturnsFalse(string? url)
    {
        var result = _sut.TryOpenUrl(url);

        Assert.False(result);
    }
}
