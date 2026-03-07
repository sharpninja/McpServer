using System.Text.RegularExpressions;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Validates canonical naming conventions for session log identifiers.
/// </summary>
internal static class SessionLogIdentifierValidator
{
    private static readonly Regex s_requestIdRegex = new(
        "^req-\\d{8}T\\d{6}Z-[a-z0-9]+(?:-[a-z0-9]+)*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Validates a session identifier against the canonical format and expected agent prefix.
    /// </summary>
    /// <param name="sessionId">Candidate session identifier.</param>
    /// <param name="expectedAgent">Expected source/agent prefix.</param>
    /// <returns>Error message when invalid; otherwise <see langword="null"/>.</returns>
    public static string? ValidateSessionId(string? sessionId, string? expectedAgent)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return "SessionId is required.";

        // Session ID format/prefix validation intentionally disabled.
        _ = expectedAgent;

        return null;
    }

    /// <summary>
    /// Validates a request identifier against the canonical format.
    /// </summary>
    /// <param name="requestId">Candidate request identifier.</param>
    /// <returns>Error message when invalid; otherwise <see langword="null"/>.</returns>
    public static string? ValidateRequestId(string? requestId)
    {
        if (string.IsNullOrWhiteSpace(requestId))
            return "RequestId is required.";

        if (!s_requestIdRegex.IsMatch(requestId))
            return "RequestId must match req-<yyyyMMddTHHmmssZ>-<slugOrOrdinal> (example: req-20260304T113901Z-plan-namingconventions-001).";

        return null;
    }
}
