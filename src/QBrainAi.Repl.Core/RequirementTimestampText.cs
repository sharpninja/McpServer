using System.Globalization;

namespace QBrainAi.Repl.Core;

/// <summary>
/// FR-MCP-040 ac-1: the one text form the REPL requirement adapters report for a stored creation or update time: ISO 8601
/// round-trip text that keeps the stored offset, or null when nothing is stored. It never reads a clock.
/// </summary>
/// <remarks>
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-02</term><description>FR-MCP-040 ac-1: added, replacing the adapters' DateTimeOffset.UtcNow so the REPL reports stored times and never the read time.</description></item>
/// </list>
/// </remarks>
internal static class RequirementTimestampText
{
    /// <summary>Formats a stored time.</summary>
    /// <param name="stored">The stored time, or null.</param>
    /// <returns>The round-trip text, or null.</returns>
    internal static string? Format(DateTimeOffset? stored) => stored?.ToString("o", CultureInfo.InvariantCulture);
}
