namespace McpServer.Support.Mcp.Options;

/// <summary>
/// FR-MCP-TRIAGESTORE-002 / TR-MCP-TRIAGESTORE-002: SaveChanges budget for session-log Submit only.
/// Triage intake and other session-log saves stay on the 5 second <c>StorageCommandBudget.Default</c>.
/// </summary>
public sealed class SessionLogSubmitOptions
{
    /// <summary>Configuration section name (<c>Mcp:SessionLog</c>).</summary>
    public const string SectionName = "Mcp:SessionLog";

    /// <summary>Shipped and recommended Submit SaveChanges budget, in seconds.</summary>
    public const int DefaultSubmitCommandBudgetSeconds = 30;

    /// <summary>Smallest accepted Submit budget, in seconds.</summary>
    public const int MinimumSubmitCommandBudgetSeconds = 1;

    /// <summary>Largest accepted Submit budget, in seconds. Bounds a misconfigured hang.</summary>
    public const int MaximumSubmitCommandBudgetSeconds = 300;

    /// <summary>
    /// Gets or sets the <c>SessionLogService.SubmitAsync</c> SaveChanges budget, in seconds.
    /// Default and recommended deploy value is 30. Raise this when a full-graph submit is known
    /// to exceed 30 seconds (observed bursts have taken longer than a minute). This value does
    /// not change triage intake or session-log replace/section saves.
    /// </summary>
    public int SubmitCommandBudgetSeconds { get; set; } = DefaultSubmitCommandBudgetSeconds;

    /// <summary>Converts <see cref="SubmitCommandBudgetSeconds"/> to a positive <see cref="TimeSpan"/>.</summary>
    /// <returns>The Submit SaveChanges budget.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The configured seconds are outside 1..300.</exception>
    public TimeSpan GetSubmitCommandBudget()
    {
        if (SubmitCommandBudgetSeconds is < MinimumSubmitCommandBudgetSeconds or > MaximumSubmitCommandBudgetSeconds)
        {
            throw new ArgumentOutOfRangeException(
                nameof(SubmitCommandBudgetSeconds),
                SubmitCommandBudgetSeconds,
                $"Session-log Submit command budget must be between {MinimumSubmitCommandBudgetSeconds} and {MaximumSubmitCommandBudgetSeconds} seconds.");
        }

        return TimeSpan.FromSeconds(SubmitCommandBudgetSeconds);
    }
}
