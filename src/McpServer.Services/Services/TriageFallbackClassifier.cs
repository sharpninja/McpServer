using System.Text.RegularExpressions;
using McpServer.Common.AgentCli;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-TRIAGE-006: Classifies a triage agent result as a retryable API failure
/// (4xx / rate-limit / unavailable) or a run timeout that should advance the
/// primary -&gt; secondary -&gt; tertiary fallback chain.
/// </summary>
internal static class TriageFallbackClassifier
{
    private static readonly Regex HttpClientErrorRegex = new(
        @"(?:http[\s/]*|status[:\s]+|code[:\s]+)4\d\d\b|\b4\d\d\b[\s:]+(?:unauthorized|forbidden|not\s+found|too\s+many\s+requests|bad\s+request|client\s+error|request\s+timeout)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>
    /// TR-MCP-TRIAGE-006: Returns <see langword="true"/> when <paramref name="result"/> represents a
    /// retryable failure (a configured trigger signal or HTTP 4xx status in stderr/body/stdout) or,
    /// when <paramref name="timedOut"/> and <see cref="TriageOptions.FallbackOnTimeout"/> is enabled,
    /// a run timeout. Successful results never fall back.
    /// </summary>
    /// <param name="result">The agent invocation result to classify.</param>
    /// <param name="options">Triage options carrying the trigger signals and timeout policy.</param>
    /// <param name="timedOut">Whether the runner's per-attempt timeout fired for this invocation.</param>
    /// <returns><see langword="true"/> to advance to the next fallback strategy.</returns>
    public static bool ShouldFallback(AgentCliResult result, TriageOptions options, bool timedOut)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(options);

        if (result.State == AgentCliResultState.Success)
        {
            return false;
        }

        if (options.FallbackOnTimeout &&
            (timedOut || result.State == AgentCliResultState.Timeout || ContainsTimeoutMarker(result)))
        {
            return true;
        }

        var haystack = string.Join('\n', result.Stderr, result.Body, result.Stdout);
        if (string.IsNullOrWhiteSpace(haystack))
        {
            return false;
        }

        foreach (var signal in options.FallbackTriggerSignals)
        {
            if (string.IsNullOrWhiteSpace(signal))
            {
                continue;
            }

            var trimmed = signal.Trim();
            var matched = IsNumericCode(trimmed)
                ? Regex.IsMatch(haystack, $@"\b{Regex.Escape(trimmed)}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
                : haystack.Contains(trimmed, StringComparison.OrdinalIgnoreCase);
            if (matched)
            {
                return true;
            }
        }

        return HttpClientErrorRegex.IsMatch(haystack);
    }

    private static bool ContainsTimeoutMarker(AgentCliResult result) =>
        ContainsIgnoreCase(result.Stderr, "timed out") ||
        ContainsIgnoreCase(result.Body, "timed out") ||
        ContainsIgnoreCase(result.Stdout, "timed out");

    private static bool ContainsIgnoreCase(string? value, string token) =>
        !string.IsNullOrEmpty(value) && value.Contains(token, StringComparison.OrdinalIgnoreCase);

    private static bool IsNumericCode(string value)
    {
        if (value.Length is < 3 or > 4)
        {
            return false;
        }

        foreach (var ch in value)
        {
            if (!char.IsAsciiDigit(ch))
            {
                return false;
            }
        }

        return true;
    }
}
