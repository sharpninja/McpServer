namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-103: Adapter contract for mutable MCP state that can be snapshotted,
/// applied from federation operations, versioned, and deduplicated.
/// </summary>
public interface IFederationStateAdapter
{
    /// <summary>Mutable state domain handled by this adapter.</summary>
    string Domain { get; }

    /// <summary>Whether this domain is intentionally local-only and exempt from replication.</summary>
    bool IsLocalOnly { get; }

    /// <summary>Creates a point-in-time snapshot for a resource.</summary>
    ValueTask<FederationStateSnapshot> SnapshotAsync(string resourceId, CancellationToken cancellationToken);

    /// <summary>Applies a federation operation to local state.</summary>
    ValueTask<FederationApplyResult> ApplyAsync(FederationStateOperation operation, CancellationToken cancellationToken);

    /// <summary>Returns the current version token for a resource.</summary>
    ValueTask<string?> GetVersionAsync(string resourceId, CancellationToken cancellationToken);

    /// <summary>Returns an idempotency key for an operation.</summary>
    string GetIdempotencyKey(FederationStateOperation operation);

    /// <summary>Returns true when the operation is an echo of a locally-originated change.</summary>
    bool IsEcho(FederationStateOperation operation);
}

/// <summary>FR-MCP-103: Snapshot returned by a federation state adapter.</summary>
public sealed class FederationStateSnapshot
{
    /// <summary>Adapter domain.</summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>Resource identifier inside the adapter domain.</summary>
    public string ResourceId { get; set; } = string.Empty;

    /// <summary>Version token for optimistic replay and conflict detection.</summary>
    public string? Version { get; set; }

    /// <summary>Serialized snapshot payload.</summary>
    public string PayloadJson { get; set; } = "{}";
}

/// <summary>FR-MCP-103: Operation passed into a federation state adapter.</summary>
public sealed class FederationStateOperation
{
    /// <summary>Hub-wide operation identifier.</summary>
    public string OperationId { get; set; } = string.Empty;

    /// <summary>Source operation identifier for echo suppression.</summary>
    public string? SourceOperationId { get; set; }

    /// <summary>Adapter domain.</summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>Resource identifier inside the adapter domain.</summary>
    public string? ResourceId { get; set; }

    /// <summary>Proxy-observed base version.</summary>
    public string? BaseVersion { get; set; }

    /// <summary>Serialized operation payload.</summary>
    public string PayloadJson { get; set; } = "{}";
}

/// <summary>FR-MCP-103: Result returned after applying a federation state operation.</summary>
public sealed class FederationApplyResult
{
    /// <summary>Whether the operation was applied.</summary>
    public bool Applied { get; set; }

    /// <summary>Whether the operation was already applied before this attempt.</summary>
    public bool AlreadyApplied { get; set; }

    /// <summary>Whether the operation produced a conflict.</summary>
    public bool Conflict { get; set; }

    /// <summary>Current version token after apply or conflict detection.</summary>
    public string? Version { get; set; }

    /// <summary>Error or conflict details, if any.</summary>
    public string? Message { get; set; }
}

/// <summary>FR-MCP-103: Adapter coverage row used by diagnostics.</summary>
public sealed class FederationStateAdapterCoverage
{
    /// <summary>Mutable state domain.</summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>Whether an adapter is registered for the domain.</summary>
    public bool Covered { get; set; }

    /// <summary>Whether the domain is intentionally exempt from replication.</summary>
    public bool LocalOnly { get; set; }
}

/// <summary>FR-MCP-103: Registry for mutable state adapters and local-only exemptions.</summary>
public sealed class FederationStateAdapterRegistry
{
    /// <summary>Mutable MCP state domains required by PLAN-FEDERATION-001.</summary>
    public static readonly IReadOnlyList<string> RequiredDomains =
    [
        "workspace",
        "todo",
        "session_log",
        "requirements",
        "context_metadata",
        "tools_buckets",
        "agents",
        "github_metadata",
        "repo_file_changes",
        "marker_state",
    ];

    private readonly IReadOnlyDictionary<string, IFederationStateAdapter> _adapters;
    private readonly ISet<string> _localOnlyDomains;

    /// <summary>Initializes a new instance of the <see cref="FederationStateAdapterRegistry"/> class.</summary>
    /// <param name="adapters">Registered state adapters.</param>
    public FederationStateAdapterRegistry(IEnumerable<IFederationStateAdapter> adapters)
    {
        _adapters = adapters.ToDictionary(a => a.Domain, StringComparer.OrdinalIgnoreCase);
        _localOnlyDomains = _adapters.Values
            .Where(a => a.IsLocalOnly)
            .Select(a => a.Domain)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Attempts to resolve an adapter by domain.</summary>
    public bool TryGet(string domain, out IFederationStateAdapter adapter)
        => _adapters.TryGetValue(domain, out adapter!);

    /// <summary>Returns adapter coverage for all required domains.</summary>
    public IReadOnlyList<FederationStateAdapterCoverage> GetCoverage()
        => RequiredDomains
            .Select(domain => new FederationStateAdapterCoverage
            {
                Domain = domain,
                Covered = _adapters.ContainsKey(domain),
                LocalOnly = _localOnlyDomains.Contains(domain),
            })
            .ToList();
}
