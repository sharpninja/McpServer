using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// Unit tests for <see cref="FederatedSessionLogService"/>. Validates merge semantics,
/// pass-through when federation is disabled, and graceful degradation on remote failure.
/// FR-MCP-083, TEST-MCP-FED-002.
/// </summary>
public sealed class FederatedSessionLogServiceTests
{
    private readonly ISessionLogService _inner = Substitute.For<ISessionLogService>();
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

    private FederatedSessionLogService CreateSut(FederationRegistry? registry = null)
    {
        registry ??= CreateRegistry();
        return new FederatedSessionLogService(
            _inner,
            registry,
            _client,
            NullLogger<FederatedSessionLogService>.Instance);
    }

    // --- QueryAsync ---

    /// <summary>When federation is disabled, delegates directly to the inner service.</summary>
    [Fact]
    public async Task QueryAsync_FederationDisabled_DelegatesToLocal()
    {
        var expected = new SessionLogQueryResult { TotalCount = 1, Limit = 100, Offset = 0, Items = [MakeLog("ClaudeCode", "S-001")] };
        _inner.QueryAsync(Arg.Any<SessionLogQueryRequest>(), Arg.Any<CancellationToken>()).Returns(expected);

        var sut = CreateSut(CreateRegistry(enabled: false));
        var result = await sut.QueryAsync(new SessionLogQueryRequest(), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Same(expected, result);
        await _client.DidNotReceiveWithAnyArgs().QuerySessionLogsAsync(default!, default!, ct: TestContext.Current.CancellationToken);
    }

    /// <summary>When no federation target resolves, delegates to the inner service.</summary>
    [Fact]
    public async Task QueryAsync_NoTargetResolved_DelegatesToLocal()
    {
        var expected = new SessionLogQueryResult { TotalCount = 1, Limit = 100, Offset = 0, Items = [MakeLog("ClaudeCode", "S-001")] };
        _inner.QueryAsync(Arg.Any<SessionLogQueryRequest>(), Arg.Any<CancellationToken>()).Returns(expected);

        var sut = CreateSut(CreateRegistry(enabled: true)); // no targets
        var result = await sut.QueryAsync(new SessionLogQueryRequest(), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Same(expected, result);
    }

    /// <summary>Merges local and remote with local winning on (SourceType, SessionId) collision.</summary>
    [Fact]
    public async Task QueryAsync_BothReturn_MergesLocalWins()
    {
        var localLog = MakeLog("ClaudeCode", "S-001", "Local Title");
        var remoteLog1 = MakeLog("ClaudeCode", "S-001", "Remote Title"); // collision
        var remoteLog2 = MakeLog("Cursor", "S-002", "Remote Only");

        _inner.QueryAsync(Arg.Any<SessionLogQueryRequest>(), Arg.Any<CancellationToken>())
            .Returns(new SessionLogQueryResult { TotalCount = 1, Limit = 100, Offset = 0, Items = [localLog] });
        _client.QuerySessionLogsAsync(Arg.Any<FederationTarget>(), Arg.Any<SessionLogQueryRequest>(), Arg.Any<CancellationToken>())
            .Returns(new SessionLogQueryResult { TotalCount = 2, Limit = 100, Offset = 0, Items = [remoteLog1, remoteLog2] });

        var sut = CreateSut(CreateRegistry(enabled: true, defaultTarget: "remote"));
        var result = await sut.QueryAsync(new SessionLogQueryRequest(), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(2, result.TotalCount);
        Assert.Contains(result.Items, l => l.SourceType == "ClaudeCode" && l.SessionId == "S-001" && l.Title == "Local Title");
        Assert.Contains(result.Items, l => l.SourceType == "Cursor" && l.SessionId == "S-002");
    }

    /// <summary>When remote call throws, returns local-only results gracefully.</summary>
    [Fact]
    public async Task QueryAsync_RemoteFails_ReturnsLocalOnly()
    {
        var localLog = MakeLog("ClaudeCode", "S-001");
        _inner.QueryAsync(Arg.Any<SessionLogQueryRequest>(), Arg.Any<CancellationToken>())
            .Returns(new SessionLogQueryResult { TotalCount = 1, Limit = 100, Offset = 0, Items = [localLog] });
        _client.QuerySessionLogsAsync(Arg.Any<FederationTarget>(), Arg.Any<SessionLogQueryRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Timeout"));

        var sut = CreateSut(CreateRegistry(enabled: true, defaultTarget: "remote"));
        var result = await sut.QueryAsync(new SessionLogQueryRequest(), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Single(result.Items);
        Assert.Equal("S-001", result.Items[0].SessionId);
    }

    /// <summary>When local is empty but remote has data, returns remote data.</summary>
    [Fact]
    public async Task QueryAsync_RemoteOnly_ReturnsRemote()
    {
        _inner.QueryAsync(Arg.Any<SessionLogQueryRequest>(), Arg.Any<CancellationToken>())
            .Returns(new SessionLogQueryResult { TotalCount = 0, Limit = 100, Offset = 0, Items = [] });
        _client.QuerySessionLogsAsync(Arg.Any<FederationTarget>(), Arg.Any<SessionLogQueryRequest>(), Arg.Any<CancellationToken>())
            .Returns(new SessionLogQueryResult { TotalCount = 1, Limit = 100, Offset = 0, Items = [MakeLog("Cursor", "S-002")] });

        var sut = CreateSut(CreateRegistry(enabled: true, defaultTarget: "remote"));
        var result = await sut.QueryAsync(new SessionLogQueryRequest(), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Single(result.Items);
        Assert.Equal("S-002", result.Items[0].SessionId);
    }

    /// <summary>When federation is wrapped by the sanitizer decorator, local and remote merged items are both redacted.</summary>
    [Fact]
    public async Task QueryAsync_WhenWrappedBySanitizer_RedactsLocalAndRemoteMergedItems()
    {
        var localLog = MakeLog("Codex", "S-LOCAL", "local password=hunter2");
        var remoteLog = MakeLog("Claude", "S-REMOTE", "remote password=hunter2");
        _inner.QueryAsync(Arg.Any<SessionLogQueryRequest>(), Arg.Any<CancellationToken>())
            .Returns(new SessionLogQueryResult { TotalCount = 1, Limit = 25, Offset = 5, Items = [localLog] });
        _client.QuerySessionLogsAsync(Arg.Any<FederationTarget>(), Arg.Any<SessionLogQueryRequest>(), Arg.Any<CancellationToken>())
            .Returns(new SessionLogQueryResult { TotalCount = 1, Limit = 25, Offset = 5, Items = [remoteLog] });
        var federated = CreateSut(CreateRegistry(enabled: true, defaultTarget: "remote"));
        var sanitizer = new SessionLogSanitizer(Microsoft.Extensions.Options.Options.Create(new SessionLogSanitizationOptions()));
        var sut = new SessionLogSanitizingService(federated, sanitizer);

        var result = await sut.QueryAsync(new SessionLogQueryRequest { Limit = 25, Offset = 5 }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(25, result.Limit);
        Assert.Equal(5, result.Offset);
        Assert.All(result.Items, item =>
        {
            Assert.DoesNotContain("hunter2", item.Title, StringComparison.Ordinal);
            Assert.Contains("[REDACTED:secret-assignment]", item.Title, StringComparison.Ordinal);
        });
    }

    // --- Write operations ---

    /// <summary>SubmitAsync always delegates to the inner service.</summary>
    [Fact]
    public async Task SubmitAsync_AlwaysDelegatesToLocal()
    {
        _inner.SubmitAsync(Arg.Any<UnifiedSessionLogDto>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(42L);

        var sut = CreateSut(CreateRegistry(enabled: true, defaultTarget: "remote"));
        var result = await sut.SubmitAsync(MakeLog("ClaudeCode", "S-001"), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(42L, result);
    }

    /// <summary>IsUnchangedAsync always delegates to the inner service.</summary>
    [Fact]
    public async Task IsUnchangedAsync_AlwaysDelegatesToLocal()
    {
        _inner.IsUnchangedAsync("ClaudeCode", "S-001", "hash123", Arg.Any<CancellationToken>()).Returns(true);

        var sut = CreateSut(CreateRegistry(enabled: true, defaultTarget: "remote"));
        var result = await sut.IsUnchangedAsync("ClaudeCode", "S-001", "hash123", cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result);
    }

    /// <summary>AppendProcessingDialogAsync always delegates to the inner service.</summary>
    [Fact]
    public async Task AppendProcessingDialogAsync_AlwaysDelegatesToLocal()
    {
        _inner.AppendProcessingDialogAsync("ClaudeCode", "S-001", "req-1", Arg.Any<IReadOnlyList<ProcessingDialogItemDto>>(), Arg.Any<CancellationToken>())
            .Returns(5);

        var sut = CreateSut(CreateRegistry(enabled: true, defaultTarget: "remote"));
        var result = await sut.AppendProcessingDialogAsync("ClaudeCode", "S-001", "req-1", [], cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(5, result);
    }

    // --- Helpers ---

    private static UnifiedSessionLogDto MakeLog(string sourceType, string sessionId, string? title = null) => new()
    {
        SourceType = sourceType,
        SessionId = sessionId,
        Title = title ?? $"Session {sessionId}",
        Status = "completed",
    };
}
