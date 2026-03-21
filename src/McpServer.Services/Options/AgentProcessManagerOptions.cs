namespace McpServer.Support.Mcp.Options;

/// <summary>
/// Configuration for runtime agent process management and isolation assets.
/// </summary>
public sealed class AgentProcessManagerOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Mcp:AgentProcessManager";

    /// <summary>
    /// Gets or sets the directory name used under each workspace to store isolated agent work directories.
    /// </summary>
    public string AgentsDirectory { get; set; } = ".agents";

    /// <summary>
    /// Gets or sets the health-check polling interval in seconds.
    /// </summary>
    public int HealthCheckIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the maximum number of restart attempts.
    /// </summary>
    public int MaxRestarts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the exponential backoff base in seconds for restarts.
    /// </summary>
    public int RestartBackoffBaseSeconds { get; set; } = 5;
}
