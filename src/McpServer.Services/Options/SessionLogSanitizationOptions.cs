namespace McpServer.Support.Mcp.Options;

/// <summary>
/// FR-MCP-SESSIONLOGSAN-001: Configures outbound session log sanitization before query results leave the server.
/// </summary>
public sealed class SessionLogSanitizationOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Mcp:SessionLogSanitization";

    /// <summary>
    /// Gets or sets a value indicating whether outbound session log sanitization is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of configured regex redaction rules.
    /// </summary>
    public int MaxRuleCount { get; set; } = 64;

    /// <summary>
    /// Gets or sets the maximum allowed length for a configured regex pattern.
    /// </summary>
    public int MaxPatternLength { get; set; } = 2048;

    /// <summary>
    /// Gets or sets the per-regex match timeout in milliseconds.
    /// </summary>
    public int RegexTimeoutMilliseconds { get; set; } = 250;

    /// <summary>
    /// Gets or sets caller-configured redaction rules.
    /// </summary>
    public List<SessionLogRedactionRuleOptions> Rules { get; set; } = [];
}

/// <summary>
/// Configures one caller-provided session log redaction rule.
/// </summary>
public sealed class SessionLogRedactionRuleOptions
{
    /// <summary>
    /// Gets or sets the stable rule ID used in replacement tokens and diagnostics.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the regular expression pattern to redact.
    /// </summary>
    public string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional replacement value. Capture-group expansion is not allowed.
    /// </summary>
    public string? Replacement { get; set; }
}
