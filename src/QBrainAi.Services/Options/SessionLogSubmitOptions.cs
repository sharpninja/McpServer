namespace QBrainAi.Support.Mcp.Options;

/// <summary>
/// FR-MCP-TRIAGESTORE-002 / TR-MCP-TRIAGESTORE-002: command budget for session-log Submit
/// SaveChanges and for full session-graph materialization. Triage intake and replace/section
/// SaveChanges stay on the 5 second <c>StorageCommandBudget.Default</c>.
/// </summary>
public sealed class SessionLogSubmitOptions
{
    /// <summary>Configuration section name (<c>Mcp:SessionLog</c>).</summary>
    public const string SectionName = "Mcp:SessionLog";

    /// <summary>Shipped and recommended Submit and graph-load budget, in seconds.</summary>
    public const int DefaultSubmitCommandBudgetSeconds = 30;

    /// <summary>Smallest accepted budget, in seconds.</summary>
    public const int MinimumSubmitCommandBudgetSeconds = 1;

    /// <summary>Largest accepted budget, in seconds. Bounds a misconfigured hang.</summary>
    public const int MaximumSubmitCommandBudgetSeconds = 300;

    /// <summary>
    /// Gets or sets the session-log Submit SaveChanges and graph-materialization budget, in seconds.
    /// Default and recommended deploy value is 30. Raise this when a full-graph mutation is known
    /// to exceed 30 seconds. This value does not change triage intake or replace/section SaveChanges.
    /// </summary>
    public int SubmitCommandBudgetSeconds { get; set; } = DefaultSubmitCommandBudgetSeconds;

    /// <summary>Converts <see cref="SubmitCommandBudgetSeconds"/> to a positive <see cref="TimeSpan"/>.</summary>
    /// <returns>The session-log mutation command budget.</returns>
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
