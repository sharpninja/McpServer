using System.Net;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Controllers;

/// <summary>Unit tests for <see cref="McpServer.Support.Mcp.Controllers.TunnelController"/>.</summary>
public sealed class TunnelControllerTests
{
    /// <summary>Adds auth header from WorkspaceTokenService to the test client.</summary>
    private static void AddAuthHeader(HttpClient client, IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<WorkspaceTokenService>();
        var config = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
        var env = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();

        // Resolve workspace path the same way Program.cs does at startup.
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
    public async Task Status_NoProvider_ReturnsNotConfigured()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        AddAuthHeader(client, factory.Services);

        var response = await client.GetAsync("/mcp/tunnel/status");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("No tunnel provider configured", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Start_NoProvider_Returns400()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        AddAuthHeader(client, factory.Services);

        var response = await client.PostAsync("/mcp/tunnel/start", null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("No tunnel provider configured", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Stop_NoProvider_Returns400()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        AddAuthHeader(client, factory.Services);

        var response = await client.PostAsync("/mcp/tunnel/stop", null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Restart_NoProvider_Returns400()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        AddAuthHeader(client, factory.Services);

        var response = await client.PostAsync("/mcp/tunnel/restart", null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Status_WithProvider_ReturnsTunnelStatus()
    {
        var mockProvider = Substitute.For<ITunnelProvider>();
        mockProvider.ProviderName.Returns("ngrok");
        mockProvider.GetStatusAsync(Arg.Any<CancellationToken>())
            .Returns(new TunnelStatus(true, "https://abc.ngrok.io"));

        await using var factory = new CustomWebApplicationFactory(services =>
        {
            services.AddSingleton(mockProvider);
        });
        using var client = factory.CreateClient();
        AddAuthHeader(client, factory.Services);

        var response = await client.GetAsync("/mcp/tunnel/status");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("ngrok", body, StringComparison.Ordinal);
        Assert.Contains("abc.ngrok.io", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Start_AlreadyRunning_ReturnsAlreadyRunning()
    {
        var mockProvider = Substitute.For<ITunnelProvider>();
        mockProvider.ProviderName.Returns("ngrok");
        mockProvider.GetStatusAsync(Arg.Any<CancellationToken>())
            .Returns(new TunnelStatus(true, "https://abc.ngrok.io"));

        await using var factory = new CustomWebApplicationFactory(services =>
        {
            services.AddSingleton(mockProvider);
        });
        using var client = factory.CreateClient();
        AddAuthHeader(client, factory.Services);

        var response = await client.PostAsync("/mcp/tunnel/start", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("already running", body, StringComparison.OrdinalIgnoreCase);
        await mockProvider.DidNotReceive().StartAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Start_NotRunning_CallsStartAndReturnsStatus()
    {
        var mockProvider = Substitute.For<ITunnelProvider>();
        mockProvider.ProviderName.Returns("cloudflare");
        mockProvider.GetStatusAsync(Arg.Any<CancellationToken>())
            .Returns(
                new TunnelStatus(false, Error: "Not started."),
                new TunnelStatus(true, "https://my.trycloudflare.com"));

        await using var factory = new CustomWebApplicationFactory(services =>
        {
            services.AddSingleton(mockProvider);
        });
        using var client = factory.CreateClient();
        AddAuthHeader(client, factory.Services);

        var response = await client.PostAsync("/mcp/tunnel/start", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await mockProvider.Received(1).StartAsync(Arg.Any<CancellationToken>());
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("trycloudflare", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Stop_Running_CallsStopAndReturnsStatus()
    {
        var mockProvider = Substitute.For<ITunnelProvider>();
        mockProvider.ProviderName.Returns("ngrok");
        mockProvider.GetStatusAsync(Arg.Any<CancellationToken>())
            .Returns(new TunnelStatus(false));

        await using var factory = new CustomWebApplicationFactory(services =>
        {
            services.AddSingleton(mockProvider);
        });
        using var client = factory.CreateClient();
        AddAuthHeader(client, factory.Services);

        var response = await client.PostAsync("/mcp/tunnel/stop", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await mockProvider.Received(1).StopAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Restart_CallsStopThenStart()
    {
        var mockProvider = Substitute.For<ITunnelProvider>();
        mockProvider.ProviderName.Returns("ngrok");
        mockProvider.GetStatusAsync(Arg.Any<CancellationToken>())
            .Returns(new TunnelStatus(true, "https://new.ngrok.io"));

        await using var factory = new CustomWebApplicationFactory(services =>
        {
            services.AddSingleton(mockProvider);
        });
        using var client = factory.CreateClient();
        AddAuthHeader(client, factory.Services);

        var response = await client.PostAsync("/mcp/tunnel/restart", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Received.InOrder(() =>
        {
            mockProvider.StopAsync(Arg.Any<CancellationToken>());
            mockProvider.StartAsync(Arg.Any<CancellationToken>());
        });
    }
}
