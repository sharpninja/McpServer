using Microsoft.Extensions.Options;

namespace McpServer.McpAgent;

/// <summary>
/// FR-MCP-066/TR-MCP-AGENT-007: Dependency-injection-friendly factory for canonical hosted-agent session and request identifiers.
/// </summary>
public sealed class McpSessionIdentifierFactory : IMcpSessionIdentifierFactory
{
    private readonly McpAgentOptions _options;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="McpSessionIdentifierFactory"/> class.
    /// </summary>
    /// <param name="options">Hosted-agent configuration whose source type becomes the session-id prefix.</param>
    /// <param name="timeProvider">Clock used when creating deterministic canonical timestamps.</param>
    public McpSessionIdentifierFactory(
        IOptions<McpAgentOptions> options,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <inheritdoc />
    public string SourceType => _options.SourceType;

    /// <inheritdoc />
    public string SanitizeSlugToken(string value)
        => McpSessionIdentifiers.SanitizeSlugToken(value);

    /// <inheritdoc />
    public string CreateSessionId(string suffix)
        => McpSessionIdentifiers.CreateSessionId(SourceType, suffix, _timeProvider.GetUtcNow());

    /// <inheritdoc />
    public string CreateRequestId(string slugOrOrdinal)
        => McpSessionIdentifiers.CreateRequestId(slugOrOrdinal, _timeProvider.GetUtcNow());

    /// <inheritdoc />
    public bool TryValidateSessionId(string? sessionId, out string? validationError)
        => McpSessionIdentifiers.TryValidateSessionId(sessionId, SourceType, out validationError);

    /// <inheritdoc />
    public bool TryValidateRequestId(string? requestId, out string? validationError)
        => McpSessionIdentifiers.TryValidateRequestId(requestId, out validationError);
}
