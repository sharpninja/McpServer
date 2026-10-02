using McpServer.Support.Mcp.Services;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using NSubstitute;
using Xunit;
using MsOptions = Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-221 / FR-MCP-173: Voice/external agent mutations skip coordinator/keyserver.
/// </summary>
public sealed class TransactionGatedVoiceConversationServiceTests
{
    /// <summary>submit-turn delegates to the inner voice service while required transactions are active.</summary>
    [Fact]
    public async Task SubmitTurnAsync_WhenTransactionsRequired_DelegatesToInner()
    {
        var inner = Substitute.For<IVoiceConversationService>();
        var sut = CreateSut(inner, new CapturingCoordinator(enabled: true));

        await sut.SubmitTurnAsync("voice-1", new VoiceTurnRequest { UserTranscriptText = "Hello" }, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        await inner.Received(1)
            .SubmitTurnAsync("voice-1", Arg.Any<VoiceTurnRequest>(), Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>send-session-message delegates when the coordinator is degraded.</summary>
    [Fact]
    public async Task SendSessionMessageAsync_WhenCoordinatorDegraded_DelegatesToInner()
    {
        var inner = Substitute.For<IVoiceConversationService>();
        var sut = CreateSut(inner, new CapturingCoordinator(enabled: true, degraded: true, message: "txn degraded"));

        await sut.SendSessionMessageAsync("voice-1", "User is here.", cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        await inner.Received(1)
            .SendSessionMessageAsync("voice-1", "User is here.", Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>voice streaming delegates to the inner stream while required transactions are active.</summary>
    [Fact]
    public async Task SubmitTurnStreamingAsync_WhenTransactionsRequired_DelegatesToInner()
    {
        var inner = Substitute.For<IVoiceConversationService>();
        inner.SubmitTurnStreamingAsync(Arg.Any<string>(), Arg.Any<VoiceTurnRequest>(), Arg.Any<CancellationToken>())
            .Returns(OneVoiceEvent());
        var sut = CreateSut(inner, new CapturingCoordinator(enabled: true));

        var events = new List<VoiceTurnStreamEvent>();
        await foreach (var streamEvent in sut.SubmitTurnStreamingAsync(
                "voice-1",
                new VoiceTurnRequest { UserTranscriptText = "Hello" }, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true))
        {
            events.Add(streamEvent);
        }

        var streamed = Assert.Single(events);
        Assert.Equal("chunk", streamed.Type);
        inner.Received(1)
            .SubmitTurnStreamingAsync(Arg.Any<string>(), Arg.Any<VoiceTurnRequest>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Status reads delegate even while mutation transactions are required.</summary>
    [Fact]
    public async Task GetStatusAsync_WhenTransactionsRequired_Delegates()
    {
        var inner = Substitute.For<IVoiceConversationService>();
        inner.GetStatusAsync("voice-1", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<VoiceSessionStatusDto?>(CreateStatus()));
        var sut = CreateSut(inner, new CapturingCoordinator(enabled: true));

        var result = await sut.GetStatusAsync("voice-1", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.NotNull(result);
        Assert.Equal("voice-1", result!.SessionId);
        await inner.Received(1)
            .GetStatusAsync("voice-1", Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>voice session creation delegates when mutation transactions are not required.</summary>
    [Fact]
    public async Task CreateSessionAsync_WhenTransactionsNotRequired_Delegates()
    {
        var inner = Substitute.For<IVoiceConversationService>();
        inner.CreateSessionAsync(Arg.Any<VoiceSessionCreateRequest?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new VoiceSessionCreateResponse
            {
                SessionId = "voice-1",
                Status = "idle",
                Language = "en-US",
                ExecutionStrategy = "test",
            }));
        var sut = CreateSut(
            inner,
            new CapturingCoordinator(enabled: true),
            new TurnTransactionOptions { Enabled = true, RequiredForMutations = false });

        var result = await sut.CreateSessionAsync(new VoiceSessionCreateRequest(), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal("voice-1", result.SessionId);
        await inner.Received(1)
            .CreateSessionAsync(Arg.Any<VoiceSessionCreateRequest?>(), Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    private static TransactionGatedVoiceConversationService CreateSut(
        IVoiceConversationService inner,
        ITurnTransactionCoordinator coordinator,
        TurnTransactionOptions? options = null)
        => new(
            inner,
            coordinator,
            MsOptions.Options.Create(options ?? new TurnTransactionOptions { Enabled = true, RequiredForMutations = true }));

    private static VoiceSessionStatusDto CreateStatus()
        => new()
        {
            SessionId = "voice-1",
            Status = "idle",
            Language = "en-US",
            CreatedUtc = "2026-06-14T12:00:00Z",
            LastUpdatedUtc = "2026-06-14T12:00:00Z",
            ExecutionStrategy = "test",
        };

    private static async IAsyncEnumerable<VoiceTurnStreamEvent> OneVoiceEvent()
    {
        yield return new VoiceTurnStreamEvent { Type = "chunk", Text = "ok" };
        await Task.CompletedTask;
    }

    private sealed class CapturingCoordinator : ITurnTransactionCoordinator
    {
        private readonly TurnTransactionStatusResponse _status;

        public CapturingCoordinator(bool enabled, bool degraded = false, string message = "")
        {
            _status = new TurnTransactionStatusResponse
            {
                Enabled = enabled,
                Degraded = degraded,
                Message = message,
            };
        }

        public Task<TurnTransactionResult> ExecuteAsync(
            TurnTransactionRequest request,
            Func<CancellationToken, Task<TurnMutationResult>> mutation,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public TurnTransactionStatusResponse GetStatus() => _status;
    }
}
