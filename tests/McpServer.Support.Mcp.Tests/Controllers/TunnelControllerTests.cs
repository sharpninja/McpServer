using System.Net;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Controllers;

/// <summary>Integration tests for <see cref="McpServer.Support.Mcp.Controllers.TunnelController"/> with <see cref="TunnelRegistry"/>.</summary>
public sealed class TunnelControllerTests
{
    /// <summary>Adds auth header from WorkspaceTokenService to the test client.</summary>
    private static void AddAuthHeader(HttpClient client, IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<WorkspaceTokenService>();
        var config = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
        var env = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();

        var repoRoot = config["Mcp:RepoRoot"] ?? ".";
        var workspacePath = Path.IsPathRooted(repoRoot)
            ? Path.GetFullPath(repoRoot)
            : Path.GetFullPath(Path.Combine(env.ContentRootPath, repoRoot));
        workspacePath = workspacePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        var token = tokenService.GetToken(workspacePath);
        if (token is not null)
            client.DefaultRequestHeaders.Add("X-Api-Key", token);
    }

    [Fact]
    public async Task List_EmptyRegistry_ReturnsEmptyArray()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        AddAuthHeader(client, factory.Services);

        var response = await client.GetAsync("/mcp/tunnel/list");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal("[]", body);
    }

    [Fact]
    public async Task Status_UnknownProvider_Returns404()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        AddAuthHeader(client, factory.Services);

        var response = await client.GetAsync("/mcp/tunnel/unknown/status");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Enable_UnknownProvider_Returns404()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        AddAuthHeader(client, factory.Services);

        var response = await client.PostAsync("/mcp/tunnel/unknown/enable", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task List_WithMockProvider_ReturnsProviderInfo()
    {
        var mockProvider = Substitute.For<ITunnelProvider>();
        mockProvider.ProviderName.Returns("ngrok");
        mockProvider.GetStatusAsync(Arg.Any<CancellationToken>())
            .Returns(new TunnelStatus(false, Error: "Not started."));

        await using var factory = new CustomWebApplicationFactory();
        var registry = factory.Services.GetRequiredService<TunnelRegistry>();
        registry.Register(mockProvider, enabled: true);

        using var client = factory.CreateClient();
        AddAuthHeader(client, factory.Services);

        var response = await client.GetAsync("/mcp/tunnel/list");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("ngrok", body, StringComparison.Ordinal);
        Assert.Contains("\"enabled\":true", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Status_WithProvider_ReturnsTunnelStatus()
    {
        var mockProvider = Substitute.For<ITunnelProvider>();
        mockProvider.ProviderName.Returns("ngrok");
        mockProvider.GetStatusAsync(Arg.Any<CancellationToken>())
            .Returns(new TunnelStatus(true, "https://abc.ngrok.io"));

        await using var factory = new CustomWebApplicationFactory();
        var registry = factory.Services.GetRequiredService<TunnelRegistry>();
        registry.Register(mockProvider, enabled: true);

        using var client = factory.CreateClient();
        AddAuthHeader(client, factory.Services);

        var response = await client.GetAsync("/mcp/tunnel/ngrok/status");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("abc.ngrok.io", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Enable_Disable_TogglesState()
    {
        var mockProvider = Substitute.For<ITunnelProvider>();
        mockProvider.ProviderName.Returns("cloudflare");
        mockProvider.GetStatusAsync(Arg.Any<CancellationToken>())
            .Returns(new TunnelStatus(false));

        await using var factory = new CustomWebApplicationFactory();
        var registry = factory.Services.GetRequiredService<TunnelRegistry>();
        registry.Register(mockProvider, enabled: false);

        using var client = factory.CreateClient();
        AddAuthHeader(client, factory.Services);

        // Verify initially disabled
        var statusResp = await client.GetAsync("/mcp/tunnel/cloudflare/status");
        var statusBody = await statusResp.Content.ReadAsStringAsync();
        Assert.Contains("\"enabled\":false", statusBody, StringComparison.Ordinal);

        // Enable
        var enableResp = await client.PostAsync("/mcp/tunnel/cloudflare/enable", null);
        Assert.Equal(HttpStatusCode.OK, enableResp.StatusCode);
        var enableBody = await enableResp.Content.ReadAsStringAsync();
        Assert.Contains("\"enabled\":true", enableBody, StringComparison.Ordinal);

        // Disable
        var disableResp = await client.PostAsync("/mcp/tunnel/cloudflare/disable", null);
        Assert.Equal(HttpStatusCode.OK, disableResp.StatusCode);
        var disableBody = await disableResp.Content.ReadAsStringAsync();
        Assert.Contains("\"enabled\":false", disableBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Start_Disabled_ReturnsError()
    {
        var mockProvider = Substitute.For<ITunnelProvider>();
        mockProvider.ProviderName.Returns("ngrok");
        mockProvider.GetStatusAsync(Arg.Any<CancellationToken>())
            .Returns(new TunnelStatus(false));

        await using var factory = new CustomWebApplicationFactory();
        var registry = factory.Services.GetRequiredService<TunnelRegistry>();
        registry.Register(mockProvider, enabled: false);

        using var client = factory.CreateClient();
        AddAuthHeader(client, factory.Services);

        var response = await client.PostAsync("/mcp/tunnel/ngrok/start", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("disabled", body, StringComparison.OrdinalIgnoreCase);
        await mockProvider.DidNotReceive().StartAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Start_Enabled_CallsStartOnProvider()
    {
        var mockProvider = Substitute.For<ITunnelProvider>();
        mockProvider.ProviderName.Returns("ngrok");
        mockProvider.GetStatusAsync(Arg.Any<CancellationToken>())
            .Returns(
                new TunnelStatus(false),
                new TunnelStatus(true, "https://new.ngrok.io"));

        await using var factory = new CustomWebApplicationFactory();
        var registry = factory.Services.GetRequiredService<TunnelRegistry>();
        registry.Register(mockProvider, enabled: true);

        using var client = factory.CreateClient();
        AddAuthHeader(client, factory.Services);

        var response = await client.PostAsync("/mcp/tunnel/ngrok/start", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await mockProvider.Received(1).StartAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Stop_CallsStopOnProvider()
    {
        var mockProvider = Substitute.For<ITunnelProvider>();
        mockProvider.ProviderName.Returns("ngrok");
        mockProvider.GetStatusAsync(Arg.Any<CancellationToken>())
            .Returns(new TunnelStatus(false));

        await using var factory = new CustomWebApplicationFactory();
        var registry = factory.Services.GetRequiredService<TunnelRegistry>();
        registry.Register(mockProvider, enabled: true);

        using var client = factory.CreateClient();
        AddAuthHeader(client, factory.Services);

        var response = await client.PostAsync("/mcp/tunnel/ngrok/stop", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await mockProvider.Received(1).StopAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Restart_CallsStopThenStart()
    {
        var mockProvider = Substitute.For<ITunnelProvider>();
        mockProvider.ProviderName.Returns("ngrok");
        mockProvider.GetStatusAsync(Arg.Any<CancellationToken>())
            .Returns(new TunnelStatus(true, "https://restarted.ngrok.io"));

        await using var factory = new CustomWebApplicationFactory();
        var registry = factory.Services.GetRequiredService<TunnelRegistry>();
        registry.Register(mockProvider, enabled: true);

        using var client = factory.CreateClient();
        AddAuthHeader(client, factory.Services);

        var response = await client.PostAsync("/mcp/tunnel/ngrok/restart", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Received.InOrder(() =>
        {
            mockProvider.StopAsync(Arg.Any<CancellationToken>());
            mockProvider.StartAsync(Arg.Any<CancellationToken>());
        });
    }
}
