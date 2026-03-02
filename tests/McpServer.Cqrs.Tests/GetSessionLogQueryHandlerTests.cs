using McpServer.UI.Core;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace McpServer.Cqrs.Tests;

/// <summary>Tests for session-log detail query handling via <see cref="Dispatcher"/>.</summary>
public sealed class GetSessionLogQueryHandlerTests
{
    [Fact]
    public async Task QueryAsync_EmptySessionId_ReturnsFailure()
    {
        using var sp = BuildProvider(Substitute.For<ISessionLogApiClient>(), AllowAllAuth());
        var dispatcher = sp.GetRequiredService<Dispatcher>();

        var result = await dispatcher.QueryAsync(new GetSessionLogQuery(string.Empty));

        Assert.True(result.IsFailure);
        Assert.Equal("SessionId is required.", result.Error);
    }

    [Fact]
    public async Task QueryAsync_Unauthorized_ReturnsPermissionFailure()
    {
        var apiClient = Substitute.For<ISessionLogApiClient>();
        var auth = Substitute.For<IAuthorizationPolicyService>();
        auth.CanExecuteAction(McpActionKeys.SessionLogQuery).Returns(false);
        auth.GetRequiredRole(McpActionKeys.SessionLogQuery).Returns("director");

        using var sp = BuildProvider(apiClient, auth);
        var dispatcher = sp.GetRequiredService<Dispatcher>();

        var result = await dispatcher.QueryAsync(new GetSessionLogQuery("session-1"));

        Assert.True(result.IsFailure);
        Assert.Equal("Permission denied: requires director.", result.Error);
        await apiClient.DidNotReceiveWithAnyArgs().GetSessionLogAsync(default!, default);
    }

    [Fact]
    public async Task QueryAsync_Authorized_ReturnsDetail()
    {
        var apiClient = Substitute.For<ISessionLogApiClient>();
        var detail = BuildDetail();
        apiClient.GetSessionLogAsync("session-1", Arg.Any<CancellationToken>())
            .Returns(detail);

        using var sp = BuildProvider(apiClient, AllowAllAuth());
        var dispatcher = sp.GetRequiredService<Dispatcher>();

        var result = await dispatcher.QueryAsync(new GetSessionLogQuery("session-1"));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("session-1", result.Value!.SessionId);
        Assert.Equal("req-1", result.Value.Entries[0].RequestId);
    }

    [Fact]
    public async Task QueryAsync_ClientThrows_ReturnsFailure()
    {
        var apiClient = Substitute.For<ISessionLogApiClient>();
        apiClient.GetSessionLogAsync("session-1", Arg.Any<CancellationToken>())
            .Returns<Task<SessionLogDetail?>>(_ => throw new InvalidOperationException("boom"));

        using var sp = BuildProvider(apiClient, AllowAllAuth());
        var dispatcher = sp.GetRequiredService<Dispatcher>();

        var result = await dispatcher.QueryAsync(new GetSessionLogQuery("session-1"));

        Assert.True(result.IsFailure);
        Assert.Equal("boom", result.Error);
    }

    private static ServiceProvider BuildProvider(ISessionLogApiClient apiClient, IAuthorizationPolicyService auth)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton(apiClient);
        services.AddSingleton(auth);
        services.AddCqrs(typeof(GetSessionLogQueryHandlerTests).Assembly);
        services.AddUiCore();
        return services.BuildServiceProvider();
    }

    private static IAuthorizationPolicyService AllowAllAuth()
    {
        var auth = Substitute.For<IAuthorizationPolicyService>();
        auth.CanExecuteAction(Arg.Any<string>()).Returns(true);
        return auth;
    }

    private static SessionLogDetail BuildDetail()
        => new(
            SessionId: "session-1",
            SourceType: "Copilot",
            Title: "Sample Session",
            Status: "completed",
            Model: "gpt-5.3-codex",
            Started: "2026-03-01T17:00:00Z",
            LastUpdated: "2026-03-01T17:05:00Z",
            EntryCount: 1,
            TotalTokens: 42,
            CursorSessionLabel: null,
            Workspace: null,
            CopilotStatistics: null,
            Entries:
            [
                new SessionLogEntryDetail(
                    RequestId: "req-1",
                    Timestamp: "2026-03-01T17:02:00Z",
                    QueryTitle: "Implement feature",
                    QueryText: "Do the thing",
                    Response: "Done.",
                    Interpretation: "Implement requested change.",
                    Status: "completed",
                    Model: "gpt-5.3-codex",
                    ModelProvider: "openai",
                    TokenCount: 42,
                    FailureNote: null,
                    Score: null,
                    IsPremium: false,
                    Tags: ["feature"],
                    ContextList: ["src\\McpServer.Director\\Screens\\SessionLogScreen.cs"],
                    DesignDecisions: [],
                    RequirementsDiscovered: [],
                    FilesModified: [],
                    Blockers: [],
                    Actions: [],
                    ProcessingDialog: [],
                    Commits: [])
            ]);
}
