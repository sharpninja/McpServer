using System.Globalization;

namespace QBrainAi.Support.Mcp.Requirements;

/// <summary>
/// FR-MCP-040 ac-1: reads the creation and update times stored with a requirement or traceability link, so a read reports
/// the stored time and never the time of the read. Stored values are round-trip text written in UTC; a blank or
/// unreadable value reads as null, and so does a value without a complete date, so no part of a reported time comes
/// from the read clock.
/// </summary>
/// <remarks>
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-02</term><description>FR-MCP-040 ac-1: added Read and Earliest, because requirement reads reported the time of the read instead of the stored times (operator 2026-11-02: "This HAS to be fixed.").</description></item>
/// <item><term>2026-11-02</term><description>FR-MCP-040 ac-1 (appended "A read never fills a missing date from the clock"): Read refuses a value without a complete date, because hostile validation finding R9-F4 showed the unrestricted parse gave a time alone the read day's date (operator 2026-11-02: "Strengthen 5 ACs, fix all 8 (Recommended)").</description></item>
/// </list>
/// </remarks>
internal static class RequirementStoredTimestamp
{
    /// <summary>
    /// Reads one stored time, keeping its stored offset; a value without an offset reads as UTC. A value without a
    /// complete date (a time alone, or a date without its year) reads as null: the framework fills a missing date or
    /// year from the clock, so the year the parse reports must be written, as four digits, in the stored text.
    /// </summary>
    /// <param name="stored">The stored text.</param>
    /// <returns>The stored time, or null when nothing readable with a complete date is stored.</returns>
    internal static DateTimeOffset? Read(string? stored) =>
        !string.IsNullOrWhiteSpace(stored)
        && DateTimeOffset.TryParse(stored, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var value)
        && stored.Contains(value.Year.ToString("D4", CultureInfo.InvariantCulture), StringComparison.Ordinal)
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
