namespace McpServer.AgentFramework;

/// <summary>
/// FR-MCP-066/TR-MCP-AGENT-007: Creates and validates canonical hosted-agent session and request identifiers.
/// </summary>
public interface IMcpSessionIdentifierFactory
{
    /// <summary>
    /// Gets the canonical source type prefix used when creating session identifiers.
    /// </summary>
    string SourceType { get; }

    /// <summary>
    /// Normalizes a raw suffix token into the canonical lowercase slug form shared by session and request identifiers.
    /// </summary>
    /// <param name="value">Raw suffix text to normalize.</param>
    /// <returns>The canonical lowercase slug token.</returns>
    string SanitizeSlugToken(string value);

    /// <summary>
    /// Creates a canonical session identifier in the form <c>&lt;Agent&gt;-&lt;yyyyMMddTHHmmssZ&gt;-&lt;suffix&gt;</c>.
    /// </summary>
    /// <param name="suffix">The suffix text to normalize and append to the configured source-type prefix.</param>
    /// <returns>A canonical session identifier.</returns>
    string CreateSessionId(string suffix);

    /// <summary>
    /// Creates a canonical request identifier in the form <c>req-&lt;yyyyMMddTHHmmssZ&gt;-&lt;slugOrOrdinal&gt;</c>.
    /// </summary>
    /// <param name="slugOrOrdinal">The slug or ordinal text to normalize and append after the timestamp.</param>
    /// <returns>A canonical request identifier.</returns>
    string CreateRequestId(string slugOrOrdinal);

    /// <summary>
    /// Validates a canonical session identifier against the configured source-type prefix.
    /// </summary>
    /// <param name="sessionId">Candidate session identifier.</param>
    /// <param name="validationError">Validation error when the identifier is invalid; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the identifier is valid; otherwise <see langword="false"/>.</returns>
    bool TryValidateSessionId(string? sessionId, out string? validationError);

    /// <summary>
    /// Validates a canonical request identifier.
    /// </summary>
    /// <param name="requestId">Candidate request identifier.</param>
    /// <param name="validationError">Validation error when the identifier is invalid; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the identifier is valid; otherwise <see langword="false"/>.</returns>
    bool TryValidateRequestId(string? requestId, out string? validationError);
}
