using McpServer.Director.Handlers;

namespace McpServer.Director.Tests;

/// <summary>
/// Tests for <see cref="LoginDialogAuthConfigHandler"/> auth-config discovery behavior.
/// </summary>
public sealed class LoginDialogAuthConfigHandlerTests
{
    [Fact]
    public async Task DiscoverAuthConfigAsync_WhenDiscoveryReturnsConfig_ReturnsConfig()
    {
        var expected = new AuthConfigResponse
        {
            Enabled = true,
            Authority = "http://localhost:7080/realms/mcpserver",
            ClientId = "mcp-director",
            Scopes = "openid profile email",
            DeviceAuthorizationEndpoint = "http://localhost:7080/device",
            TokenEndpoint = "http://localhost:7080/token",
        };

        CancellationToken observedToken = default;
        var sut = new LoginDialogAuthConfigHandler(ct =>
        {
            observedToken = ct;
            return Task.FromResult<AuthConfigResponse?>(expected);
        });

        using var cts = new CancellationTokenSource();
        var actual = await sut.DiscoverAuthConfigAsync(cts.Token);

        Assert.Same(expected, actual);
        Assert.Equal(cts.Token, observedToken);
    }

    [Fact]
    public async Task DiscoverAuthConfigAsync_WhenDiscoveryReturnsNull_ReturnsNull()
    {
        var sut = new LoginDialogAuthConfigHandler(_ => Task.FromResult<AuthConfigResponse?>(null));

        var actual = await sut.DiscoverAuthConfigAsync();

        Assert.Null(actual);
    }

    [Fact]
    public async Task DiscoverAuthConfigAsync_WhenDiscoveryThrows_ReturnsNull()
    {
        var sut = new LoginDialogAuthConfigHandler(
            _ => Task.FromException<AuthConfigResponse?>(new InvalidOperationException("boom")));

        var actual = await sut.DiscoverAuthConfigAsync();

        Assert.Null(actual);
    }
}
