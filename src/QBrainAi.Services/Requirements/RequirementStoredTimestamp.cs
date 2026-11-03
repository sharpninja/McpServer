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
/// <item><term>2026-11-03</term><description>FR-MCP-040 ac-1 (appended "A readable stored time states its year, its month and its day as date components"): Read accepts only exact formats whose patterns carry the year, the month and the day, instead of the unrestricted parse plus a check that the parsed year appears in the text, because hostile validation round 10 finding R10-F1 showed a year and month alone read with day 1 and year digits in a fraction could pass (operator 2026-11-03: "don't gate on me between rounds"). Earliest uses the same Read.</description></item>
/// </list>
/// </remarks>
internal static class RequirementStoredTimestamp
{
    /// <summary>
    /// FR-MCP-040 ac-1: the stored-time formats Read accepts. Every pattern states the year, the month and the day, so no
    /// part of a date can come from the parser's defaults or the clock; a time may carry a fraction and an offset ("K"
    /// accepts Z, an offset or none). The round-trip text the store writes is the first.
    /// </summary>
    private static readonly string[] CompleteDateFormats =
    [
        "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK",
        "yyyy-MM-dd'T'HH:mmK",
        "yyyy-MM-dd HH:mm:ss.FFFFFFFK",
        "yyyy-MM-dd",
        "M/d/yyyy HH:mm:ss",
        "M/d/yyyy",
        "d MMMM yyyy HH:mm:ss",
        "d MMMM yyyy",
        "r",
    ];

    /// <summary>
    /// Reads one stored time, keeping its stored offset; a value without an offset reads as UTC. Only a text whose format
    /// states the year, the month and the day is read (<see cref="CompleteDateFormats"/>); a time alone, a date without its
    /// year, a year and month alone, and any other text read as null, so no part of a reported time comes from the clock.
    /// </summary>
    /// <param name="stored">The stored text.</param>
    /// <returns>The stored time, or null when nothing readable with a complete date is stored.</returns>
    internal static DateTimeOffset? Read(string? stored) =>
        !string.IsNullOrWhiteSpace(stored)
        && DateTimeOffset.TryParseExact(stored.Trim(), CompleteDateFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var value)
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
