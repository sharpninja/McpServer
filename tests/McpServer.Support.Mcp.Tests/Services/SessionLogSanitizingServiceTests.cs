using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>TEST-MCP-SESSIONLOGSAN-001: ISessionLogService decorator coverage for read sanitization and write passthrough.</summary>
public sealed class SessionLogSanitizingServiceTests
{
    /// <summary>QueryAsync sanitizes the completed inner result and preserves the returned sanitized clone.</summary>
    [Fact]
    public async Task QueryAsync_SanitizesInnerResultAfterInnerCompletes()
    {
        var inner = Substitute.For<ISessionLogService>();
        var sanitizer = Substitute.For<ISessionLogSanitizer>();
        var request = new SessionLogQueryRequest { Text = "password=hunter2", Limit = 5, Offset = 1 };
        var raw = new SessionLogQueryResult { TotalCount = 1, Limit = 5, Offset = 1, Items = [CreateSession("password=hunter2")] };
        var sanitized = new SessionLogQueryResult { TotalCount = 1, Limit = 5, Offset = 1, Items = [CreateSession("[REDACTED:secret-assignment]")] };
        inner.QueryAsync(request, TestContext.Current.CancellationToken).Returns(raw);
        sanitizer.SanitizeQueryResult(raw).Returns(sanitized);
        var service = new SessionLogSanitizingService(inner, sanitizer);

        var result = await service.QueryAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Same(sanitized, result);
        Received.InOrder(() =>
        {
            _ = inner.QueryAsync(request, TestContext.Current.CancellationToken);
            _ = sanitizer.SanitizeQueryResult(raw);
        });
    }

    /// <summary>GetAsync sanitizes the completed inner result and returns null unchanged.</summary>
    [Fact]
    public async Task GetAsync_SanitizesInnerResultAfterInnerCompletes()
    {
        var inner = Substitute.For<ISessionLogService>();
        var sanitizer = Substitute.For<ISessionLogSanitizer>();
        var raw = CreateSession("password=hunter2");
        var sanitized = CreateSession("[REDACTED:secret-assignment]");
        inner.GetAsync("Codex", "session", TestContext.Current.CancellationToken).Returns(raw);
        sanitizer.SanitizeSessionLog(raw).Returns(sanitized);
        var service = new SessionLogSanitizingService(inner, sanitizer);

        var result = await service.GetAsync("Codex", "session", TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Same(sanitized, result);
        Received.InOrder(() =>
        {
            _ = inner.GetAsync("Codex", "session", TestContext.Current.CancellationToken);
            _ = sanitizer.SanitizeSessionLog(raw);
        });
    }

    /// <summary>Mutation and scalar methods pass through unchanged and never call the sanitizer.</summary>
    [Fact]
    public async Task MutationMethods_DelegateWithoutSanitizingInputs()
    {
        var inner = Substitute.For<ISessionLogService>();
        var sanitizer = Substitute.For<ISessionLogSanitizer>();
        var service = new SessionLogSanitizingService(inner, sanitizer);
        var session = CreateSession("password=hunter2");
        var turn = new UnifiedRequestEntryDto { RequestId = "req-20260714T000000Z-mutate", Response = "password=hunter2" };
        var dialog = new[] { new ProcessingDialogItemDto { Content = "password=hunter2" } };

        inner.SubmitAsync(session, "source.json", "hash", TestContext.Current.CancellationToken).Returns(41L);
        inner.IsUnchangedAsync("Codex", "session", "hash", TestContext.Current.CancellationToken).Returns(true);
        inner.AppendProcessingDialogAsync("Codex", "session", "request", dialog, TestContext.Current.CancellationToken).Returns(3);
        inner.UpsertTurnAsync("Codex", "session", turn, TestContext.Current.CancellationToken).Returns(42L);
        inner.ReplaceTurnAsync("Codex", "session", turn, TestContext.Current.CancellationToken).Returns(43L);
        inner.ReplaceTurnSectionAsync("Codex", "session", "request", "actions", turn, TestContext.Current.CancellationToken).Returns(true);
        inner.ClearTurnSectionAsync("Codex", "session", "request", "actions", TestContext.Current.CancellationToken).Returns(true);
        inner.DeleteTurnItemAsync("Codex", "session", "request", "actions", "1", TestContext.Current.CancellationToken).Returns(true);
        inner.DeleteTurnAsync("Codex", "session", "request", TestContext.Current.CancellationToken).Returns(true);
        inner.DeleteSessionAsync("Codex", "session", TestContext.Current.CancellationToken).Returns(true);
        inner.OpenSessionAsync("Codex", "session", "title", "model", TestContext.Current.CancellationToken).Returns(true);
        inner.RepairWorkspaceStampsAsync(true, TestContext.Current.CancellationToken).Returns(7);

        Assert.Equal(41L, await service.SubmitAsync(session, "source.json", "hash", TestContext.Current.CancellationToken).ConfigureAwait(true));
        Assert.True(await service.IsUnchangedAsync("Codex", "session", "hash", TestContext.Current.CancellationToken).ConfigureAwait(true));
        Assert.Equal(3, await service.AppendProcessingDialogAsync("Codex", "session", "request", dialog, TestContext.Current.CancellationToken).ConfigureAwait(true));
        Assert.Equal(42L, await service.UpsertTurnAsync("Codex", "session", turn, TestContext.Current.CancellationToken).ConfigureAwait(true));
        Assert.Equal(43L, await service.ReplaceTurnAsync("Codex", "session", turn, TestContext.Current.CancellationToken).ConfigureAwait(true));
        Assert.True(await service.ReplaceTurnSectionAsync("Codex", "session", "request", "actions", turn, TestContext.Current.CancellationToken).ConfigureAwait(true));
        Assert.True(await service.ClearTurnSectionAsync("Codex", "session", "request", "actions", TestContext.Current.CancellationToken).ConfigureAwait(true));
        Assert.True(await service.DeleteTurnItemAsync("Codex", "session", "request", "actions", "1", TestContext.Current.CancellationToken).ConfigureAwait(true));
        Assert.True(await service.DeleteTurnAsync("Codex", "session", "request", TestContext.Current.CancellationToken).ConfigureAwait(true));
        Assert.True(await service.DeleteSessionAsync("Codex", "session", TestContext.Current.CancellationToken).ConfigureAwait(true));
        Assert.True(await service.OpenSessionAsync("Codex", "session", "title", "model", TestContext.Current.CancellationToken).ConfigureAwait(true));
        Assert.Equal(7, await service.RepairWorkspaceStampsAsync(true, TestContext.Current.CancellationToken).ConfigureAwait(true));

        await inner.Received(1).SubmitAsync(session, "source.json", "hash", TestContext.Current.CancellationToken).ConfigureAwait(true);
        await inner.Received(1).UpsertTurnAsync("Codex", "session", turn, TestContext.Current.CancellationToken).ConfigureAwait(true);
        sanitizer.DidNotReceiveWithAnyArgs().SanitizeSessionLog(default);
        sanitizer.DidNotReceiveWithAnyArgs().SanitizeQueryResult(default!);
        sanitizer.DidNotReceiveWithAnyArgs().SanitizeString(default);
    }

    private static UnifiedSessionLogDto CreateSession(string value)
    {
        return new UnifiedSessionLogDto
        {
            SourceType = "Codex",
            SessionId = "session",
            Title = value,
            Turns = [new UnifiedRequestEntryDto { RequestId = "request", Response = value }],
        };
    }
}
