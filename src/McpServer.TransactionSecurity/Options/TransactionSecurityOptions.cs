namespace McpServer.Support.Mcp.Options;

/// <summary>
/// FR-MCP-118: Keyserver runtime options bound from <c>Mcp:KeyServer</c>.
/// The first implementation stores generated keys in memory and uses these options
/// for manifest time windows and clock-skew validation.
/// </summary>
public sealed class KeyServerOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Mcp:KeyServer";

    /// <summary>Default manifest lifetime in seconds.</summary>
    public int ManifestTtlSeconds { get; set; } = 300;

    /// <summary>Maximum tolerated manifest clock skew in seconds.</summary>
    public int MaxClockSkewSeconds { get; set; } = 300;

    /// <summary>Whether keyserver audit event capture is enabled.</summary>
    public bool AuditEnabled { get; set; } = true;
}

/// <summary>
/// FR-MCP-119: Subscriber runtime options bound from <c>Mcp:Subscriber</c>.
/// </summary>
public sealed class SubscriberOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Mcp:Subscriber";

    /// <summary>Local subscriber party identifier. Empty means the manifest subscriber is accepted.</summary>
    public string? PartyId { get; set; }

    /// <summary>Commit timeout in seconds.</summary>
    public int CommitTimeoutSeconds { get; set; } = 30;

    /// <summary>Whether subscriber audit event capture is enabled.</summary>
    public bool AuditEnabled { get; set; } = true;
}

/// <summary>
/// FR-MCP-120 and FR-MCP-121: Turn transaction coordinator options bound from
/// <c>Mcp:TurnTransactions</c>. The coordinator is disabled by default so existing
/// mutation paths remain unchanged until explicit adapter slices opt in.
/// </summary>
public sealed class TurnTransactionOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Mcp:TurnTransactions";

    /// <summary>Whether turn transaction gating is enabled.</summary>
    public bool Enabled { get; set; }

    /// <summary>Whether mutations require committed subscriber confirmation when enabled.</summary>
    public bool RequiredForMutations { get; set; } = true;

    /// <summary>Whether dependency failures enter degraded mode instead of hard rejection.</summary>
    public bool DegradedModeEnabled { get; set; } = true;

    /// <summary>Commit timeout in seconds.</summary>
    public int CommitTimeoutSeconds { get; set; } = 30;

    /// <summary>Default publisher party identifier used by the coordinator.</summary>
    public string PublisherPartyId { get; set; } = "mcpserver";

    /// <summary>Default subscriber party identifier used by the coordinator.</summary>
    public string SubscriberPartyId { get; set; } = "subscriber-1";

    /// <summary>Future external keyserver base URL. Current implementation uses the in-process service.</summary>
    public string KeyServerBaseUrl { get; set; } = "http://localhost:7167";

    /// <summary>Future external subscriber base URL. Current implementation uses the in-process service.</summary>
    public string SubscriberBaseUrl { get; set; } = "http://localhost:7168";
}
