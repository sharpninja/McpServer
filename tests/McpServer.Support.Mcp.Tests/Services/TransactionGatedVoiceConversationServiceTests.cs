using McpServer.Support.Mcp.Services;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using NSubstitute;
using Xunit;
using MsOptions = Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-161: Verifies voice/external agent mutations fail closed while required turn transactions are active.
/// </summary>
public sealed class TransactionGatedVoiceConversationServiceTests
{
    /// <summary>submit-turn fails before invoking the interactive voice service while required transactions are active.</summary>
    [Fact]
    public async Task SubmitTurnAsync_WhenTransactionsRequired_ThrowsWithoutCallingInner()
    {
        var inner = Substitute.For<IVoiceConversationService>();
        var sut = CreateSut(inner, new CapturingCoordinator(enabled: true));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => sut.SubmitTurnAsync("voice-1", new VoiceTurnRequest { UserTranscriptText = "Hello" }))
            .ConfigureAwait(true);

        Assert.Contains("not transaction compensated", ex.Message, StringComparison.OrdinalIgnoreCase);
        await inner.DidNotReceive()
            .SubmitTurnAsync(Arg.Any<string>(), Arg.Any<VoiceTurnRequest>(), Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>send-session-message fails before writing to the external interactive process when degraded.</summary>
    [Fact]
    public async Task SendSessionMessageAsync_WhenCoordinatorDegraded_ThrowsWithoutCallingInner()
    {
        var inner = Substitute.For<IVoiceConversationService>();
        var sut = CreateSut(inner, new CapturingCoordinator(enabled: true, degraded: true, message: "txn degraded"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => sut.SendSessionMessageAsync("voice-1", "User is here."))
            .ConfigureAwait(true);

        Assert.Contains("txn degraded", ex.Message, StringComparison.Ordinal);
        await inner.DidNotReceive()
            .SendSessionMessageAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>voice streaming returns a single error event without invoking the inner stream when blocked.</summary>
    [Fact]
    public async Task SubmitTurnStreamingAsync_WhenTransactionsRequired_ReturnsBlockedErrorEventWithoutCallingInner()
    {
        var inner = Substitute.For<IVoiceConversationService>();
        var sut = CreateSut(inner, new CapturingCoordinator(enabled: true));

        var events = new List<VoiceTurnStreamEvent>();
        await foreach (var streamEvent in sut.SubmitTurnStreamingAsync(
                "voice-1",
                new VoiceTurnRequest { UserTranscriptText = "Hello" })
            .ConfigureAwait(true))
        {
            events.Add(streamEvent);
        }

        var blockedEvent = Assert.Single(events);
        Assert.Equal("error", blockedEvent.Type);
        Assert.Contains("not transaction compensated", blockedEvent.Message, StringComparison.OrdinalIgnoreCase);
        inner.DidNotReceive()
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

        var result = await sut.GetStatusAsync("voice-1").ConfigureAwait(true);

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

        var result = await sut.CreateSessionAsync(new VoiceSessionCreateRequest()).ConfigureAwait(true);

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
