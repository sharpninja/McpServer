namespace McpServer.Support.Mcp.Options;

/// <summary>TR-MCP-HOSTILEREVIEW-003: Hosted hostile-review worker options. Invalid relationships fail startup.</summary>
public sealed class HostileReviewWorkerOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Mcp:HostileReviewWorker";

    /// <summary>When false the worker does not claim work.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Explicit pooled agent name. Must be Astra.</summary>
    public string AgentName { get; set; } = "Astra";

    /// <summary>Required model id. Must be gpt-6-astra.</summary>
    public string RequiredModel { get; set; } = "gpt-6-astra";

    /// <summary>Required effort. Must be xhigh.</summary>
    public string RequiredEffort { get; set; } = "xhigh";

    /// <summary>Maximum active queued reviews.</summary>
    public int MaximumActiveQueue { get; set; } = 200;

    /// <summary>Maximum concurrent reviews.</summary>
    public int MaximumConcurrentReviews { get; set; } = 1;

    /// <summary>Poll interval.</summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>Lease duration.</summary>
    public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Lease renewal interval. Must be less than <see cref="LeaseDuration"/>.</summary>
    public TimeSpan RenewalInterval { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>Maximum attempts per request.</summary>
    public int MaximumAttempts { get; set; } = 3;

    /// <summary>Base retry delay.</summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Retry delay cap.</summary>
    public TimeSpan RetryDelayCap { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>Execution timeout.</summary>
    public TimeSpan ExecutionTimeout { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>Maximum artifact links.</summary>
    public int MaximumLinks { get; set; } = 64;

    /// <summary>Per-artifact decoded limit in bytes.</summary>
    public int MaximumArtifactBytes { get; set; } = 1 * 1024 * 1024;

    /// <summary>Aggregate resolved input limit in bytes.</summary>
    public int MaximumAggregateBytes { get; set; } = 8 * 1024 * 1024;

    /// <summary>Reviewer output limit in bytes.</summary>
    public int MaximumOutputBytes { get; set; } = 2 * 1024 * 1024;

    /// <summary>Maximum JSON depth.</summary>
    public int MaximumJsonDepth { get; set; } = 32;

    /// <summary>Maximum serialized submit payload in bytes.</summary>
    public int MaximumSubmitBytes { get; set; } = 1 * 1024 * 1024;

    /// <summary>Throws when option relationships are invalid.</summary>
    public void Validate()
    {
        if (RenewalInterval >= LeaseDuration)
            throw new InvalidOperationException("HostileReviewWorker RenewalInterval must be less than LeaseDuration.");
        if (MaximumActiveQueue < 1)
            throw new InvalidOperationException("HostileReviewWorker MaximumActiveQueue must be at least 1.");
        if (MaximumConcurrentReviews < 1)
            throw new InvalidOperationException("HostileReviewWorker MaximumConcurrentReviews must be at least 1.");
        if (MaximumAttempts < 1)
            throw new InvalidOperationException("HostileReviewWorker MaximumAttempts must be at least 1.");
        if (!string.Equals(AgentName, "Astra", StringComparison.Ordinal))
            throw new InvalidOperationException("HostileReviewWorker AgentName must be Astra.");
        if (!string.Equals(RequiredModel, "gpt-6-astra", StringComparison.Ordinal))
            throw new InvalidOperationException("HostileReviewWorker RequiredModel must be gpt-6-astra.");
        if (!string.Equals(RequiredEffort, "xhigh", StringComparison.Ordinal))
            throw new InvalidOperationException("HostileReviewWorker RequiredEffort must be xhigh.");
    }
}
