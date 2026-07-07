using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace McpServer.Support.Mcp.Storage;

/// <summary>
/// TR-MCP-DB-DTO-001: stores <see cref="DateTimeOffset"/> as a UTC <see cref="DateTime"/>.
/// The SQLite provider cannot translate DateTimeOffset predicates or ordering to SQL, which
/// forced timestamp filters/sorts to run client-side. Every timestamp in this schema is UTC,
/// so collapsing the (always zero) offset is lossless and lets those predicates push down to
/// SQL. Applied to all DateTimeOffset properties via <c>ConfigureConventions</c>.
/// </summary>
public sealed class DateTimeOffsetToUtcDateTimeConverter : ValueConverter<DateTimeOffset, DateTime>
{
    /// <summary>Initializes a new instance of the <see cref="DateTimeOffsetToUtcDateTimeConverter"/> class.</summary>
    public DateTimeOffsetToUtcDateTimeConverter()
        : base(
            v => v.UtcDateTime,
            v => new DateTimeOffset(DateTime.SpecifyKind(v, DateTimeKind.Utc), TimeSpan.Zero))
    {
    }
}
