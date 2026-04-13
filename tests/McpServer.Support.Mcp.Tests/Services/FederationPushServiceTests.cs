using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// Unit tests for <see cref="FederationPushService"/>. Validates push orchestration
/// including querying local data, pushing to remote targets, and error handling.
/// FR-MCP-085, TEST-MCP-FED-004.
/// </summary>
public sealed class FederationPushServiceTests
{
    private readonly ITodoService _todoService = Substitute.For<ITodoService>();
    private readonly ISessionLogService _sessionLogService = Substitute.For<ISessionLogService>();
    private readonly IFederationDataClient _client = Substitute.For<IFederationDataClient>();

    private static FederationRegistry CreateRegistry(bool enabled = false, string? defaultTarget = null)
    {
        var opts = new FederationOptions { Enabled = enabled };
        if (defaultTarget is not null)
        {
            opts.DefaultTarget = defaultTarget;
            opts.Targets.Add(new FederationTargetOptions { Name = defaultTarget, BaseUrl = "http://remote:7147" });
        }
        return new FederationRegistry(Microsoft.Extensions.Options.Options.Create(opts));
    }

    private FederationPushService CreateSut(FederationRegistry? registry = null)
    {
        registry ??= CreateRegistry();
        return new FederationPushService(
            _todoService,
            _sessionLogService,
            _client,
            registry,
            NullLogger<FederationPushService>.Instance);
    }

    /// <summary>PushTodosAsync queries local TODOs and pushes them to the remote target.</summary>
    [Fact]
    public async Task PushTodosAsync_SendsItemsToRemote()
    {
        var items = new[] { MakeItem("A-001"), MakeItem("A-002") };
        _todoService.QueryAsync(Arg.Any<TodoQueryRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TodoQueryResult(items, 2));
        _client.PushTodosAsync(Arg.Any<FederationTarget>(), Arg.Any<IReadOnlyList<TodoFlatItem>>(), Arg.Any<CancellationToken>())
            .Returns(new FederationPushResult(2, 0, []));

        var sut = CreateSut(CreateRegistry(enabled: true, defaultTarget: "remote"));
        var result = await sut.PushTodosAsync();

        Assert.Equal(2, result.Succeeded);
        Assert.Equal(0, result.Failed);
    }

    /// <summary>When remote push fails, returns error details.</summary>
    [Fact]
    public async Task PushTodosAsync_RemoteFails_ReturnsErrors()
    {
        _todoService.QueryAsync(Arg.Any<TodoQueryRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TodoQueryResult([MakeItem("A-001")], 1));
        _client.PushTodosAsync(Arg.Any<FederationTarget>(), Arg.Any<IReadOnlyList<TodoFlatItem>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var sut = CreateSut(CreateRegistry(enabled: true, defaultTarget: "remote"));
        var result = await sut.PushTodosAsync();

        Assert.Equal(0, result.Succeeded);
        Assert.True(result.Failed > 0);
        Assert.NotEmpty(result.Errors);
    }

    /// <summary>PushSessionLogsAsync queries local session logs and pushes them.</summary>
    [Fact]
    public async Task PushSessionLogsAsync_SendsItemsToRemote()
    {
        var logs = new[] { MakeLog("ClaudeCode", "S-001") };
        _sessionLogService.QueryAsync(Arg.Any<SessionLogQueryRequest>(), Arg.Any<CancellationToken>())
            .Returns(new SessionLogQueryResult { TotalCount = 1, Limit = 100, Offset = 0, Items = logs });
        _client.PushSessionLogsAsync(Arg.Any<FederationTarget>(), Arg.Any<IReadOnlyList<UnifiedSessionLogDto>>(), Arg.Any<CancellationToken>())
            .Returns(new FederationPushResult(1, 0, []));

        var sut = CreateSut(CreateRegistry(enabled: true, defaultTarget: "remote"));
        var result = await sut.PushSessionLogsAsync();

        Assert.Equal(1, result.Succeeded);
        Assert.Equal(0, result.Failed);
    }

    /// <summary>PushAllAsync pushes both TODOs and session logs.</summary>
    [Fact]
    public async Task PushAllAsync_QueriesLocalAndPushes()
    {
        _todoService.QueryAsync(Arg.Any<TodoQueryRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TodoQueryResult([MakeItem("A-001")], 1));
        _sessionLogService.QueryAsync(Arg.Any<SessionLogQueryRequest>(), Arg.Any<CancellationToken>())
            .Returns(new SessionLogQueryResult { TotalCount = 1, Limit = 100, Offset = 0, Items = [MakeLog("CC", "S-001")] });
        _client.PushTodosAsync(Arg.Any<FederationTarget>(), Arg.Any<IReadOnlyList<TodoFlatItem>>(), Arg.Any<CancellationToken>())
            .Returns(new FederationPushResult(1, 0, []));
        _client.PushSessionLogsAsync(Arg.Any<FederationTarget>(), Arg.Any<IReadOnlyList<UnifiedSessionLogDto>>(), Arg.Any<CancellationToken>())
            .Returns(new FederationPushResult(1, 0, []));

        var sut = CreateSut(CreateRegistry(enabled: true, defaultTarget: "remote"));
        var result = await sut.PushAllAsync();

        Assert.Equal(2, result.Succeeded);
        Assert.Equal(0, result.Failed);
    }

    /// <summary>PushAllAsync returns error when federation is disabled.</summary>
    [Fact]
    public async Task PushAllAsync_FederationDisabled_ReturnsError()
    {
        var sut = CreateSut(CreateRegistry(enabled: false));
        var result = await sut.PushAllAsync();

        Assert.Equal(0, result.Succeeded);
        Assert.True(result.Failed > 0);
        Assert.Contains(result.Errors, e => e.Contains("disabled", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>PushAllAsync returns error when no target is resolved.</summary>
    [Fact]
    public async Task PushAllAsync_NoTarget_ReturnsError()
    {
        var sut = CreateSut(CreateRegistry(enabled: true)); // no targets
        var result = await sut.PushAllAsync();

        Assert.Equal(0, result.Succeeded);
        Assert.True(result.Failed > 0);
        Assert.Contains(result.Errors, e => e.Contains("target", StringComparison.OrdinalIgnoreCase));
    }

    // --- Helpers ---

    private static TodoFlatItem MakeItem(string id) => new()
    {
        Id = id,
        Title = $"Item {id}",
        Section = "test",
        Priority = "medium",
        Done = false,
    };

    private static UnifiedSessionLogDto MakeLog(string sourceType, string sessionId) => new()
    {
        SourceType = sourceType,
        SessionId = sessionId,
        Title = $"Session {sessionId}",
        Status = "completed",
    };
}
