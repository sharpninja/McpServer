using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>TEST-MCP-SESSIONLOGSAN-001: timeout fail-closed coverage for pathological configured redaction rules.</summary>
public sealed class SessionLogSanitizerTimeoutTests
{
    /// <summary>Regex timeouts replace the complete affected string and log only rule ID plus field path.</summary>
    [Fact]
    public void SanitizeString_WhenConfiguredRuleTimesOut_ReturnsTimeoutTokenAndDoesNotLogInput()
    {
        var logger = new CapturingLogger<SessionLogSanitizer>();
        var sanitizer = CreateTimeoutSanitizer(logger);
        var input = new string('a', 20000) + "! password=hunter2";

        var output = sanitizer.SanitizeString(input);

        Assert.Equal("[REDACTED:catastrophic:timeout]", output);
        Assert.DoesNotContain("hunter2", output, StringComparison.Ordinal);
        var message = Assert.Single(logger.Messages);
        Assert.Contains("catastrophic", message, StringComparison.Ordinal);
        Assert.Contains("value", message, StringComparison.Ordinal);
        Assert.DoesNotContain("hunter2", message, StringComparison.Ordinal);
        Assert.DoesNotContain(new string('a', 64), message, StringComparison.Ordinal);
    }

    /// <summary>A timeout in one field does not stop later fields from being sanitized.</summary>
    [Fact]
    public void SanitizeSessionLog_WhenOneFieldTimesOut_ContinuesSanitizingOtherFields()
    {
        var logger = new CapturingLogger<SessionLogSanitizer>();
        var sanitizer = CreateTimeoutSanitizer(logger);
        var session = new UnifiedSessionLogDto
        {
            SourceType = "Codex",
            SessionId = "Codex-20260714T000000Z-timeout",
            Title = new string('a', 20000) + "! password=hunter2",
            Turns =
            [
                new UnifiedRequestEntryDto
                {
                    RequestId = "req-20260714T000000Z-timeout",
                    Response = "password=hunter2",
                },
            ],
        };

        var sanitized = sanitizer.SanitizeSessionLog(session);
        var sanitizedTurn = Assert.Single(sanitized!.Turns ?? []);

        Assert.Equal("[REDACTED:catastrophic:timeout]", sanitized.Title);
        Assert.DoesNotContain("hunter2", sanitizedTurn.Response, StringComparison.Ordinal);
        Assert.Equal("[REDACTED:secret-assignment]", sanitizedTurn.Response);
        Assert.Contains(logger.Messages, message => message.Contains("catastrophic", StringComparison.Ordinal) && message.Contains("title", StringComparison.Ordinal));
        Assert.All(logger.Messages, message => Assert.DoesNotContain("hunter2", message, StringComparison.Ordinal));
    }

    private static SessionLogSanitizer CreateTimeoutSanitizer(CapturingLogger<SessionLogSanitizer> logger)
    {
        var options = new SessionLogSanitizationOptions
        {
            RegexTimeoutMilliseconds = 100,
            Rules = [new SessionLogRedactionRuleOptions { Id = "catastrophic", Pattern = "(a+)+$" }],
        };

        // TR-MCP-SESSIONLOGSAN-002: force the catastrophic rule to raise RegexMatchTimeoutException
        // deterministically for the large pathological input (length + pattern, no wall-clock), so the
        // fail-open behavior is verified without scheduler jitter (BUG-TRIAGE-081). Short inputs and
        // other rules run the real Regex.Replace.
        return new SessionLogSanitizer(
            Microsoft.Extensions.Options.Options.Create(options),
            logger,
            (regex, input, evaluator) =>
                input.Length > 10000 && regex.ToString().Contains("(a+)+", StringComparison.Ordinal)
                    ? throw new System.Text.RegularExpressions.RegexMatchTimeoutException()
                    : regex.Replace(input, evaluator));
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
