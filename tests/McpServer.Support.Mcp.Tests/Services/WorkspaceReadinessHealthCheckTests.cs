using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>TEST-MCP-HEALTH-002: Unit tests for <see cref="WorkspaceReadinessHealthCheck"/>.</summary>
public sealed class WorkspaceReadinessHealthCheckTests
{
    private const string Primary = @"C:\real\workspace";

    private static WorkspaceDto Ws(string path, bool isPrimary = true, bool isEnabled = true) => new()
    {
        WorkspacePath = path,
        Name = "ws",
        TodoPath = "docs/todo.yaml",
        StatusPrompt = "s",
        ImplementPrompt = "i",
        PlanPrompt = "p",
        IsPrimary = isPrimary,
        IsEnabled = isEnabled,
    };

    private static WorkspaceReadinessHealthCheck Build(
        WorkspaceTokenService tokens,
        params WorkspaceDto[] items)
    {
        var workspaceService = Substitute.For<IWorkspaceService>();
        workspaceService.ListAsync(Arg.Any<CancellationToken>())
            .Returns(new WorkspaceListResult(items, items.Length));
        var serviceProvider = new ServiceCollection()
            .AddScoped<IWorkspaceService>(_ => workspaceService)
            .BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        return new WorkspaceReadinessHealthCheck(
            tokens,
            scopeFactory,
            NullLogger<WorkspaceReadinessHealthCheck>.Instance);
    }

    /// <summary>Healthy when an enabled primary workspace is registered and has a seeded token.</summary>
    [Fact]
    public async Task Healthy_WhenPrimaryRegisteredAndTokenSeeded()
    {
        var tokens = new WorkspaceTokenService();
        tokens.GenerateToken(Primary);
        var check = Build(tokens, Ws(Primary));

        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    /// <summary>Unhealthy when the token subsystem has not been initialized.</summary>
    [Fact]
    public async Task Unhealthy_WhenSubsystemNotInitialized()
    {
        var check = Build(new WorkspaceTokenService(), Ws(Primary));

        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    /// <summary>Unhealthy when no enabled workspace is registered, even if some token exists.</summary>
    [Fact]
    public async Task Unhealthy_WhenNoEnabledWorkspace()
    {
        var tokens = new WorkspaceTokenService();
        tokens.GenerateToken(@"C:\some\other");
        var check = Build(tokens);

        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    /// <summary>Unhealthy when the primary workspace is missing its seeded full-access token.</summary>
    [Fact]
    public async Task Unhealthy_WhenPrimaryWorkspaceTokenMissing()
    {
        var tokens = new WorkspaceTokenService();
        tokens.GenerateToken(@"C:\some\other");
        var check = Build(tokens, Ws(Primary));

        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }
}
