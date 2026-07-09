using System.Text;
using System.Text.Json;
using McpServer.Client.Models;
using McpServer.Repl.Core;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace McpServer.Repl.Core.Tests;

/// <summary>
/// TEST-MCP-REPL-025: verifies independent primary and filesystem failsafe
/// persistence strategies before they are integrated into the REPL dispatcher.
/// </summary>
public sealed class SessionLogPersistenceStrategyTests
{
    /// <summary>
    /// Primary persistence success remains the normal result and does not invoke failsafe storage.
    /// </summary>
    [Fact]
    public async Task PersistAsync_PrimarySucceeds_DoesNotInvokeFailsafe()
    {
        var snapshot = CreateSessionLog();
        var primary = Substitute.For<ISessionLogPersistenceStrategy>();
        var failsafe = Substitute.For<ISessionLogPersistenceStrategy>();
        var expected = new SessionLogPersistenceResult(
            Persisted: true,
            Degraded: false,
            Strategy: "mcp-service",
            FailsafePath: null,
            Message: null);
        primary.Name.Returns("mcp-service");
        failsafe.Name.Returns("filesystem-failsafe");
        primary.PersistAsync(snapshot, Arg.Any<CancellationToken>())
            .Returns(expected);
        var sut = new FailoverSessionLogPersistenceStrategy(primary, failsafe);

        var result = await sut.PersistAsync(snapshot, TestContext.Current.CancellationToken);

        Assert.Same(expected, result);
        await failsafe.DidNotReceiveWithAnyArgs()
            .PersistAsync(Arg.Any<UnifiedSessionLogDto>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// A primary service failure invokes the independent failsafe strategy with the same snapshot.
    /// </summary>
    [Fact]
    public async Task PersistAsync_PrimaryFails_ReturnsFailsafeResult()
    {
        var snapshot = CreateSessionLog();
        var primary = Substitute.For<ISessionLogPersistenceStrategy>();
        var failsafe = Substitute.For<ISessionLogPersistenceStrategy>();
        var path = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "turn-failsafe.yaml"));
        var expected = new SessionLogPersistenceResult(
            Persisted: true,
            Degraded: true,
            Strategy: "filesystem-failsafe",
            FailsafePath: path,
            Message: "MCP Session Log persistence is degraded.");
        primary.Name.Returns("mcp-service");
        failsafe.Name.Returns("filesystem-failsafe");
        primary.PersistAsync(snapshot, Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("service unavailable"));
        failsafe.PersistAsync(snapshot, Arg.Any<CancellationToken>())
            .Returns(expected);
        var sut = new FailoverSessionLogPersistenceStrategy(primary, failsafe);

        var result = await sut.PersistAsync(snapshot, TestContext.Current.CancellationToken);

        Assert.Same(expected, result);
        await failsafe.Received(1).PersistAsync(
            Arg.Is<UnifiedSessionLogDto>(value => ReferenceEquals(value, snapshot)),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Explicit caller cancellation is never converted into a failsafe write.
    /// </summary>
    [Fact]
    public async Task PersistAsync_CallerCancels_DoesNotInvokeFailsafe()
    {
        var snapshot = CreateSessionLog();
        var primary = Substitute.For<ISessionLogPersistenceStrategy>();
        var failsafe = Substitute.For<ISessionLogPersistenceStrategy>();
        primary.Name.Returns("mcp-service");
        failsafe.Name.Returns("filesystem-failsafe");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        primary.PersistAsync(snapshot, cancellation.Token)
            .Returns<Task<SessionLogPersistenceResult>>(_ => Task.FromCanceled<SessionLogPersistenceResult>(cancellation.Token));
        var sut = new FailoverSessionLogPersistenceStrategy(primary, failsafe);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => sut.PersistAsync(snapshot, cancellation.Token));

        await failsafe.DidNotReceiveWithAnyArgs()
            .PersistAsync(Arg.Any<UnifiedSessionLogDto>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Failure of both strategies remains an error instead of claiming durable persistence.
    /// </summary>
    [Fact]
    public async Task PersistAsync_BothStrategiesFail_ThrowsPersistenceException()
    {
        var snapshot = CreateSessionLog();
        var primary = Substitute.For<ISessionLogPersistenceStrategy>();
        var failsafe = Substitute.For<ISessionLogPersistenceStrategy>();
        primary.Name.Returns("mcp-service");
        failsafe.Name.Returns("filesystem-failsafe");
        primary.PersistAsync(snapshot, Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("service unavailable"));
        failsafe.PersistAsync(snapshot, Arg.Any<CancellationToken>())
            .ThrowsAsync(new IOException("disk unavailable"));
        var sut = new FailoverSessionLogPersistenceStrategy(primary, failsafe);

        var exception = await Assert.ThrowsAsync<SessionLogPersistenceException>(
            () => sut.PersistAsync(snapshot, TestContext.Current.CancellationToken));

        Assert.IsType<HttpRequestException>(exception.PrimaryException);
        Assert.IsType<IOException>(exception.FailsafeException);
    }

    /// <summary>
    /// Filesystem fallback writes an atomic, V4-scoped, replayable recovery envelope.
    /// </summary>
    [Fact]
    public async Task FilesystemPersistAsync_WritesReplayableV4ScopedEnvelope()
    {
        var workspace = Path.Combine(Path.GetTempPath(), "mcp-repl-failsafe-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspace);
        try
        {
            var snapshot = CreateSessionLog();
            var serializer = new YamlSerializer();
            var sut = new FilesystemSessionLogPersistenceStrategy(
                workspace,
                serializer,
                TimeProvider.System);

            var result = await sut.PersistAsync(snapshot, TestContext.Current.CancellationToken);

            Assert.True(result.Persisted);
            Assert.True(result.Degraded);
            Assert.Equal("filesystem-failsafe", result.Strategy);
            var path = Assert.IsType<string>(result.FailsafePath);
            Assert.True(Path.IsPathFullyQualified(path));
            Assert.True(File.Exists(path));
            Assert.Contains(
                Path.Combine(".mcpServer", "failsafe", "Codex", "workspaces"),
                path,
                StringComparison.OrdinalIgnoreCase);
            Assert.Contains(Path.DirectorySeparatorChar + "pending" + Path.DirectorySeparatorChar, path);

            var envelope = serializer.Deserialize(await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
            var request = Assert.IsType<RequestPayload>(envelope.Payload);
            Assert.Equal(SessionLogCommandShapes.ImportRecoveryMethod, request.Method);
            var serializedSession = JsonSerializer.Serialize(request.Params!["sessionLog"]);
            var recovered = JsonSerializer.Deserialize<UnifiedSessionLogDto>(
                serializedSession,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
            Assert.NotNull(recovered);
            Assert.Equal(snapshot.SessionId, recovered!.SessionId);
            Assert.Equal(snapshot.Turns![0].RequestId, recovered.Turns![0].RequestId);
            Assert.Equal("completed", recovered.Turns[0].Status);
            Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(path)!, "*.tmp"));
        }
        finally
        {
            Directory.Delete(workspace, recursive: true);
        }
    }

    private static UnifiedSessionLogDto CreateSessionLog() =>
        new()
        {
            SourceType = "Codex",
            SessionId = "Codex-20260709T213500Z-failsafe-test",
            Title = "Failsafe strategy test",
            Model = "gpt-5.3-codex",
            Started = "2026-07-09T21:35:00Z",
            LastUpdated = "2026-07-09T21:36:00Z",
            Status = "in_progress",
            TurnCount = 1,
            Turns =
            [
                new UnifiedRequestEntryDto
                {
                    RequestId = "req-20260709T213501Z-failsafe-test",
                    Timestamp = "2026-07-09T21:35:01Z",
                    QueryTitle = "Persist degraded turn",
                    QueryText = "Save this turn if MCP Session Log is unavailable.",
                    Response = "Saved.",
                    Status = "completed",
                    Model = "gpt-5.3-codex",
                    Actions =
                    [
                        new UnifiedActionDto
                        {
                            Order = 1,
                            Type = "test",
                            Status = "succeeded",
                            Description = "Verified failsafe"
                        }
                    ],
                    ProcessingDialog =
                    [
                        new ProcessingDialogItemDto
                        {
                            Timestamp = "2026-07-09T21:35:30Z",
                            Role = "assistant",
                            Content = "Selected filesystem fallback.",
                            Category = "decision"
                        }
                    ]
                }
            ]
        };
}

