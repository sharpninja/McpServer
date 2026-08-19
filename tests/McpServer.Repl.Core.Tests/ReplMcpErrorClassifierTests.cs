using McpServer.Repl.Core;
using NSubstitute;
using Xunit;

namespace McpServer.Repl.Core.Tests;

/// <summary>
/// TEST-MCP-TRIAGEERR-001: REPL dispatch errors use the shared classifier contract
/// (code, message, retryable, details.inner).
/// </summary>
public sealed class ReplMcpErrorClassifierTests
{
    /// <summary>A local type named DbUpdateException so the shared type-name rule fires.</summary>
    private sealed class DbUpdateException : Exception
    {
        public DbUpdateException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    /// <summary>DbUpdateException with UNIQUE inner text is conflict, not dispatch_error.</summary>
    [Fact]
    public void FromException_DbUpdateUnique_IsConflictWithInner()
    {
        var inner = new Exception("UNIQUE constraint failed: SessionLogs.SessionId");
        var classified = ReplMcpErrorClassifier.FromException(new DbUpdateException(
            "An error occurred while saving the entity changes. See the inner exception for details.",
            inner));

        Assert.Equal("conflict", classified.Code);
        Assert.False(classified.Retryable);
        Assert.Equal(inner.Message, classified.Details!["inner"]);
        Assert.DoesNotContain("See the inner exception", classified.Message, StringComparison.Ordinal);
    }

    /// <summary>ArgumentException is validation_error, not retryable.</summary>
    [Fact]
    public void FromException_ArgumentException_IsValidationError()
    {
        var classified = ReplMcpErrorClassifier.FromException(new ArgumentException("sourceType is required."));
        Assert.Equal("validation_error", classified.Code);
        Assert.False(classified.Retryable);
    }

    /// <summary>KeyNotFoundException is not_found, not retryable.</summary>
    [Fact]
    public void FromException_KeyNotFound_IsNotFound()
    {
        var classified = ReplMcpErrorClassifier.FromException(new KeyNotFoundException("Turn not found."));
        Assert.Equal("not_found", classified.Code);
        Assert.False(classified.Retryable);
    }

    /// <summary>Storage budget expiry is backend_unavailable and retryable.</summary>
    [Fact]
    public void FromException_StorageBudgetExceeded_IsBackendUnavailable()
    {
        var classified = ReplMcpErrorClassifier.FromException(
            new InvalidOperationException("The storage backend did not respond within the 5 second intake budget."));
        Assert.Equal("backend_unavailable", classified.Code);
        Assert.True(classified.Retryable);
    }

    /// <summary>TimeoutException is retryable timeout.</summary>
    [Fact]
    public void FromException_Timeout_IsRetryableTimeout()
    {
        var classified = ReplMcpErrorClassifier.FromException(new TimeoutException("command timed out"));
        Assert.Equal("timeout", classified.Code);
        Assert.True(classified.Retryable);
    }

    /// <summary>SQLITE_BUSY is retryable persistence_error.</summary>
    [Fact]
    public void FromException_SqliteBusy_IsRetryablePersistenceError()
    {
        var classified = ReplMcpErrorClassifier.FromException(
            new Exception("database is locked SQLITE_BUSY"));

        Assert.Equal("persistence_error", classified.Code);
        Assert.True(classified.Retryable);
    }

    /// <summary>Agent-stdio ArgumentException is type:error validation_error with retryable false.</summary>
    [Fact]
    public async Task AgentStdioProtocol_DispatchThrowsArgumentException_WritesValidationEnvelope()
    {
        await AssertTypeErrorAsync(
            new ArgumentException("sourceType is required."),
            "validation_error",
            retryable: false).ConfigureAwait(true);
    }

    /// <summary>Agent-stdio KeyNotFoundException is type:error not_found with retryable false.</summary>
    [Fact]
    public async Task AgentStdioProtocol_DispatchThrowsKeyNotFound_WritesNotFoundEnvelope()
    {
        await AssertTypeErrorAsync(
            new KeyNotFoundException("Turn not found."),
            "not_found",
            retryable: false).ConfigureAwait(true);
    }

    /// <summary>Agent-stdio storage budget is type:error backend_unavailable with retryable true.</summary>
    [Fact]
    public async Task AgentStdioProtocol_DispatchThrowsStorageBudget_WritesBackendUnavailableEnvelope()
    {
        await AssertTypeErrorAsync(
            new InvalidOperationException("The storage backend did not respond within the 5 second intake budget."),
            "backend_unavailable",
            retryable: true).ConfigureAwait(true);
    }

    /// <summary>Agent-stdio dispatch catch emits the four-field envelope.</summary>
    [Fact]
    public async Task AgentStdioProtocol_DispatchThrowsDbUpdateException_WritesClassifiedEnvelope()
    {
        var dispatcher = Substitute.For<IReplCommandDispatcher>();
        dispatcher.DispatchAsync(Arg.Any<IYamlEnvelope>(), Arg.Any<CancellationToken>())
            .Returns<IYamlEnvelope>(_ => throw new DbUpdateException(
                "An error occurred while saving the entity changes. See the inner exception for details.",
                new Exception("UNIQUE constraint failed: SessionLogs.SessionId")));

        var sut = new AgentStdioProtocol(new YamlSerializer(), dispatcher);
        const string input = """
            type: request
            payload:
              requestId: req-repl-class-001
              method: client.todo.QueryAsync

            """;

        using var reader = new StringReader(input);
        using var writer = new StringWriter();
        await sut.RunAsync(reader, writer, CancellationToken.None).ConfigureAwait(true);

        var output = writer.ToString();
        Assert.Contains("type: error", output, StringComparison.Ordinal);
        Assert.Contains("code: conflict", output, StringComparison.Ordinal);
        Assert.Contains("retryable: false", output, StringComparison.Ordinal);
        Assert.Contains("UNIQUE constraint failed", output, StringComparison.Ordinal);
        Assert.DoesNotContain("dispatch_error", output, StringComparison.Ordinal);
    }

    private static async Task AssertTypeErrorAsync(Exception thrown, string code, bool retryable)
    {
        var dispatcher = Substitute.For<IReplCommandDispatcher>();
        dispatcher.DispatchAsync(Arg.Any<IYamlEnvelope>(), Arg.Any<CancellationToken>())
            .Returns<IYamlEnvelope>(_ => throw thrown);

        var sut = new AgentStdioProtocol(new YamlSerializer(), dispatcher);
        const string input = """
            type: request
            payload:
              requestId: req-repl-class-matrix
              method: client.todo.QueryAsync

            """;

        using var reader = new StringReader(input);
        using var writer = new StringWriter();
        await sut.RunAsync(reader, writer, CancellationToken.None).ConfigureAwait(true);

        var output = writer.ToString();
        Assert.Contains("type: error", output, StringComparison.Ordinal);
        Assert.Contains("code: " + code, output, StringComparison.Ordinal);
        Assert.Contains("retryable: " + (retryable ? "true" : "false"), output, StringComparison.Ordinal);
        Assert.Contains("message:", output, StringComparison.Ordinal);
        Assert.Contains("details:", output, StringComparison.Ordinal);
        Assert.Contains("reason:", output, StringComparison.Ordinal);
    }
}
