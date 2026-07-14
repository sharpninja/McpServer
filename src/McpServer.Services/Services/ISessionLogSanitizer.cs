using McpServer.Support.Mcp.Models;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Sanitizes session log read models without mutating the stored source values.
/// </summary>
public interface ISessionLogSanitizer
{
    /// <summary>
    /// Sanitizes a scalar string value.
    /// </summary>
    /// <param name="value">The raw string value.</param>
    /// <returns>The sanitized value, or the original null/empty value.</returns>
    string? SanitizeString(string? value);

    /// <summary>
    /// Creates a sanitized clone of a session log DTO.
    /// </summary>
    /// <param name="sessionLog">The source session log DTO.</param>
    /// <returns>A sanitized clone, or null when the source is null.</returns>
    UnifiedSessionLogDto? SanitizeSessionLog(UnifiedSessionLogDto? sessionLog);

    /// <summary>
    /// Creates a sanitized clone of a paged session log query result.
    /// </summary>
    /// <param name="queryResult">The source query result.</param>
    /// <returns>A sanitized query result clone.</returns>
    SessionLogQueryResult SanitizeQueryResult(SessionLogQueryResult queryResult);
}
