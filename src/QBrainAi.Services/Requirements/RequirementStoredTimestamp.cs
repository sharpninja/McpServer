using System.Globalization;

namespace QBrainAi.Support.Mcp.Requirements;

/// <summary>
/// FR-MCP-040 ac-1: reads the creation and update times stored with a requirement or traceability link, so a read reports
/// the stored time and never the time of the read. Stored values are round-trip text written in UTC; a blank or
/// unreadable value reads as null.
/// </summary>
internal static class RequirementStoredTimestamp
{
    /// <summary>Reads one stored time, keeping its stored offset.</summary>
    /// <param name="stored">The stored text.</param>
    /// <returns>The stored time, or null when nothing readable is stored.</returns>
    internal static DateTimeOffset? Read(string? stored) =>
        !string.IsNullOrWhiteSpace(stored)
        && DateTimeOffset.TryParse(stored, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var value)
            ? value
            : null;

    /// <summary>Reads the earliest readable stored time, for a mapping that aggregates several links.</summary>
    /// <param name="stored">The links' stored creation texts.</param>
    /// <returns>The earliest stored time, or null when none is readable.</returns>
    internal static DateTimeOffset? Earliest(IEnumerable<string?> stored)
    {
        ArgumentNullException.ThrowIfNull(stored);
        DateTimeOffset? earliest = null;
        foreach (var value in stored.Select(Read))
        {
            if (value is not null && (earliest is null || value.Value < earliest.Value))
                earliest = value;
        }

        return earliest;
    }
}
