namespace QBrainAi.Support.Mcp.Options;

/// <summary>
/// TR-MCP-HOSTILEREVIEW-003: Hosted hostile-review worker options. Invalid relationships fail startup. The agent, model and effort are not
/// pinned (operator rule 2026-10-15, PLAN-SESSIONTRIAGEFIX-001 B9): empty values pin nothing, and only a configured RequiredModel or
/// RequiredEffort is required of a reviewer output (AC-002).
/// </summary>
public sealed class HostileReviewWorkerOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Mcp:HostileReviewWorker";

    /// <summary>When false the worker does not claim work.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Pooled agent name from configuration; no claim path selects an agent by it, and empty (the default) pins no agent.</summary>
    public string AgentName { get; set; } = string.Empty;

    /// <summary>Configured model a reviewer output must report (TR-MCP-HOSTILEREVIEW-003 AC-002); empty requires none, so the reported model is accepted.</summary>
    public string RequiredModel { get; set; } = string.Empty;

    /// <summary>Configured effort a reviewer output must report (TR-MCP-HOSTILEREVIEW-003 AC-002); empty requires none, so the reported effort is accepted.</summary>
    public string RequiredEffort { get; set; } = string.Empty;

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
    }
}
