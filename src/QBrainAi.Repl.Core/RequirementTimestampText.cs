using System.Globalization;

namespace QBrainAi.Repl.Core;

/// <summary>
/// FR-MCP-040 ac-1: the one text form the REPL requirement adapters report for a stored creation or update time: ISO 8601
/// round-trip text that keeps the stored offset, or null when nothing is stored. It never reads a clock.
/// </summary>
internal static class RequirementTimestampText
{
    /// <summary>Formats a stored time.</summary>
    /// <param name="stored">The stored time, or null.</param>
    /// <returns>The round-trip text, or null.</returns>
    internal static string? Format(DateTimeOffset? stored) => stored?.ToString("o", CultureInfo.InvariantCulture);
}
