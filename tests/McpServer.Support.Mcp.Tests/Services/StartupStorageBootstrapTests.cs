using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TR-MCP-HEALTH-003 / FR-MCP-TRIAGESTORE-002: host startup must not die when SQL is
/// unreachable (including SSL pre-login handshake). <see cref="StartupStorageBootstrap"/>
/// swallows classified backend-unavailable failures and returns false so Kestrel can still
/// serve <c>/health</c> liveness.
/// Fixture: delegates that throw constructed <c>SqlException</c> instances.
/// </summary>
public sealed class StartupStorageBootstrapTests
{
    /// <summary>AC: SSL pre-login handshake during initialize does not throw; returns false.</summary>
    [Fact]
    public async Task TryInitializeAsync_SqlPreLoginHandshake_ReturnsFalse()
    {
        var result = await StartupStorageBootstrap.TryInitializeAsync(
            _ => throw SqlExceptionFactory.Create(
                233,
                "A connection was successfully established with the server, but then an error occurred during the pre-login handshake. (provider: SSL Provider, error: 0 - The wait operation timed out)"),
            NullLogger.Instance,
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(result);
    }

    /// <summary>AC: a successful initialize returns true.</summary>
    [Fact]
    public async Task TryInitializeAsync_Success_ReturnsTrue()
    {
        var result = await StartupStorageBootstrap.TryInitializeAsync(
            _ => Task.CompletedTask,
            NullLogger.Instance,
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result);
    }

    /// <summary>AC: non-storage failures still abort startup.</summary>
    [Fact]
    public async Task TryInitializeAsync_OrdinaryException_Rethrows()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            StartupStorageBootstrap.TryInitializeAsync(
                _ => throw new InvalidOperationException("config is invalid"),
                NullLogger.Instance,
                TestContext.Current.CancellationToken)).ConfigureAwait(true);
    }
}
