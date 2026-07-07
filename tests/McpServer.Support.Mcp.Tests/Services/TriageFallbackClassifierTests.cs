using McpServer.Common.AgentCli;
using McpServer.Support.Mcp.Services;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-TRIAGE-008: Validates <see cref="TriageFallbackClassifier"/> retryable-failure detection
/// (4xx / rate-limit / unavailable trigger signals and run timeouts) with in-line agent-result
/// fixtures. Validates FR-MCP-TRIAGE-006 / TR-MCP-TRIAGE-006.
/// </summary>
public sealed class TriageFallbackClassifierTests
{
    /// <summary>
    /// TEST-MCP-TRIAGE-008: default retryable API signals in stderr classify as fallback-eligible.
    /// Fixture: default <see cref="TriageOptions"/> trigger signals.
    /// </summary>
    /// <param name="stderr">The captured agent stderr fixture.</param>
    [Theory]
    [InlineData("error: 429 Too Many Requests")]
    [InlineData("Anthropic API error: rate limit exceeded")]
    [InlineData("HTTP 503 Service Unavailable")]
    [InlineData("overloaded_error: the upstream model is overloaded")]
    [InlineData("openai: insufficient_quota for this key")]
    [InlineData("status 401 unauthorized")]
    [InlineData("The service is temporarily unavailable, try again later")]
    public void ShouldFallback_WhenRetryableSignalInStderr_ReturnsTrue(string stderr)
    {
        var result = new AgentCliResult { State = AgentCliResultState.Error, Stderr = stderr, ExitCode = 1 };

        Assert.True(TriageFallbackClassifier.ShouldFallback(result, new TriageOptions(), timedOut: false));
    }

    /// <summary>TEST-MCP-TRIAGE-008: a retryable signal in the body (not stderr) still classifies as fallback-eligible.</summary>
    [Fact]
    public void ShouldFallback_WhenSignalInBody_ReturnsTrue()
    {
        var result = new AgentCliResult { State = AgentCliResultState.Error, Body = "grok CLI: 429 rate-limit", ExitCode = 1 };

        Assert.True(TriageFallbackClassifier.ShouldFallback(result, new TriageOptions(), timedOut: false));
    }

    /// <summary>TEST-MCP-TRIAGE-008: a bare HTTP 4xx status with context classifies as fallback-eligible via the regex path.</summary>
    /// <param name="stderr">The captured agent stderr fixture with an HTTP 4xx status.</param>
    [Theory]
    [InlineData("request failed with HTTP 452")]
    [InlineData("status: 418 returned by gateway")]
    public void ShouldFallback_WhenHttp4xxWithContext_ReturnsTrue(string stderr)
    {
        var result = new AgentCliResult { State = AgentCliResultState.Error, Stderr = stderr, ExitCode = 1 };

        Assert.True(TriageFallbackClassifier.ShouldFallback(result, new TriageOptions(), timedOut: false));
    }

    /// <summary>TEST-MCP-TRIAGE-008: a successful result never falls back even when the output contains a signal token.</summary>
    [Fact]
    public void ShouldFallback_WhenSuccess_ReturnsFalse()
    {
        var result = new AgentCliResult { State = AgentCliResultState.Success, Body = "429 appeared in normal output", ExitCode = 0 };

        Assert.False(TriageFallbackClassifier.ShouldFallback(result, new TriageOptions(), timedOut: false));
    }

    /// <summary>TEST-MCP-TRIAGE-008: a generic non-API failure with no trigger signal does not fall back.</summary>
    [Fact]
    public void ShouldFallback_WhenGenericErrorWithoutSignal_ReturnsFalse()
    {
        var result = new AgentCliResult { State = AgentCliResultState.Error, Stderr = "System.NullReferenceException: object reference not set", ExitCode = 1 };

        Assert.False(TriageFallbackClassifier.ShouldFallback(result, new TriageOptions(), timedOut: false));
    }

    /// <summary>TEST-MCP-TRIAGE-008: a bare 3-digit code embedded in a larger number does not false-trigger (word-boundary match).</summary>
    [Fact]
    public void ShouldFallback_WhenNumericCodeIsSubstringOfLargerNumber_ReturnsFalse()
    {
        var result = new AgentCliResult { State = AgentCliResultState.Error, Stderr = "processed 4013 tokens in 5290 ms", ExitCode = 1 };

        Assert.False(TriageFallbackClassifier.ShouldFallback(result, new TriageOptions(), timedOut: false));
    }

    /// <summary>TEST-MCP-TRIAGE-008: a run timeout falls back when FallbackOnTimeout is enabled (default).</summary>
    [Fact]
    public void ShouldFallback_WhenTimedOutAndTimeoutFallbackEnabled_ReturnsTrue()
    {
        var result = new AgentCliResult { State = AgentCliResultState.Error, Stderr = string.Empty, ExitCode = null };

        Assert.True(TriageFallbackClassifier.ShouldFallback(result, new TriageOptions { FallbackOnTimeout = true }, timedOut: true));
    }

    /// <summary>TEST-MCP-TRIAGE-008: a timeout-marker in stderr falls back when FallbackOnTimeout is enabled even without the runner flag.</summary>
    [Fact]
    public void ShouldFallback_WhenTimeoutMarkerInStderrAndTimeoutFallbackEnabled_ReturnsTrue()
    {
        var result = new AgentCliResult { State = AgentCliResultState.Error, Stderr = "error: One-shot CLI agent run was cancelled or timed out.", ExitCode = null };

        Assert.True(TriageFallbackClassifier.ShouldFallback(result, new TriageOptions { FallbackOnTimeout = true }, timedOut: false));
    }

    /// <summary>TEST-MCP-TRIAGE-008: a run timeout does NOT fall back when FallbackOnTimeout is disabled.</summary>
    [Fact]
    public void ShouldFallback_WhenTimedOutButTimeoutFallbackDisabled_ReturnsFalse()
    {
        var result = new AgentCliResult { State = AgentCliResultState.Error, Stderr = "error: run was cancelled or timed out.", ExitCode = null };

        Assert.False(TriageFallbackClassifier.ShouldFallback(result, new TriageOptions { FallbackOnTimeout = false }, timedOut: true));
    }
}
