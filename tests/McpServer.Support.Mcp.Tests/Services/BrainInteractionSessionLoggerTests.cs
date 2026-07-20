using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-QBEXEC-003: Verifies inter-brain interactions are written to the session log in full text with secret
/// redaction, that logging is a no-op without session/turn context, and that append failures never propagate
/// (FR-MCP-QBEXEC-003).
/// </summary>
public sealed class BrainInteractionSessionLoggerTests
{
    private readonly ISessionLogService _sessionLog = Substitute.For<ISessionLogService>();

    private BrainInteractionSessionLogger CreateSut()
        => new(_sessionLog, NullLogger<BrainInteractionSessionLogger>.Instance);

    /// <summary>A brain interaction appends the full prompt and full output as two dialog items.</summary>
    [Fact]
    public async Task Log_AppendsFullPromptAndOutput()
    {
        _sessionLog.AppendProcessingDialogAsync("QBAgent", "S", "T", Arg.Any<IReadOnlyList<ProcessingDialogItemDto>>(), Arg.Any<CancellationToken>())
            .Returns(2);
        var sut = CreateSut();

        await sut.LogInteractionAsync("QBAgent", "S", "T", "Creativity", "the full prompt", "the full output", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        await _sessionLog.Received(1).AppendProcessingDialogAsync(
            "QBAgent", "S", "T",
            Arg.Is<IReadOnlyList<ProcessingDialogItemDto>>(items =>
                items != null
                && items.Count == 2
                && items[0].Content!.Contains("the full prompt", StringComparison.Ordinal)
                && items[1].Content!.Contains("the full output", StringComparison.Ordinal)
                && items[0].Content!.Contains("Creativity", StringComparison.Ordinal)),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>Without session/turn context, nothing is appended.</summary>
    [Fact]
    public async Task Log_NoSessionContext_NoOps()
    {
        var sut = CreateSut();

        await sut.LogInteractionAsync("QBAgent", sessionId: null, turnId: "T", "Left", "p", "o", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        await sut.LogInteractionAsync("QBAgent", "S", turnId: " ", "Left", "p", "o", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        await _sessionLog.DidNotReceive().AppendProcessingDialogAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<IReadOnlyList<ProcessingDialogItemDto>>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>Secrets in the prompt/output are redacted before logging.</summary>
    [Fact]
    public async Task Log_RedactsSecrets()
    {
        _sessionLog.AppendProcessingDialogAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<ProcessingDialogItemDto>>(), Arg.Any<CancellationToken>())
            .Returns(2);
        var sut = CreateSut();

        await sut.LogInteractionAsync("QBAgent", "S", "T", "Left", "auth Bearer abc123XYZ.def trailing", "x", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        await _sessionLog.Received(1).AppendProcessingDialogAsync(
            "QBAgent", "S", "T",
            Arg.Is<IReadOnlyList<ProcessingDialogItemDto>>(items =>
                items != null
                && items[0].Content!.Contains("[REDACTED]", StringComparison.Ordinal)
                && !items[0].Content!.Contains("abc123XYZ", StringComparison.Ordinal)),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>An append failure is swallowed so orchestration is never interrupted.</summary>
    [Fact]
    public async Task Log_WhenAppendThrows_DoesNotPropagate()
    {
        _sessionLog.AppendProcessingDialogAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<ProcessingDialogItemDto>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("turn not found"));
        var sut = CreateSut();

        await sut.LogInteractionAsync("QBAgent", "S", "T", "Left", "p", "o", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        // No exception thrown = pass.
    }

    /// <summary>Redact replaces bearer tokens and api keys.</summary>
    [Fact]
    public void Redact_RemovesBearerAndApiKey()
    {
        Assert.Equal("Bearer [REDACTED]", BrainInteractionSessionLogger.Redact("Bearer abc.def-123"));
        Assert.DoesNotContain("secretvalue123", BrainInteractionSessionLogger.Redact("api_key: secretvalue123"), StringComparison.Ordinal);
    }
}
