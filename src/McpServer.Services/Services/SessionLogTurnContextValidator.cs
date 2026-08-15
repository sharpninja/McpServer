using System.Text.RegularExpressions;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-SESSIONLOGCTX-001 / TR-MCP-SESSIONLOG-006 / AC-TR-MCP-SESSIONLOG-006-001:
/// Validates required session-turn <c>planFile</c> and <c>todoId</c> values.
/// </summary>
public static class SessionLogTurnContextValidator
{
    /// <summary>Exact sentinel meaning no active plan or TODO.</summary>
    public const string NoneSentinel = "None";

    /// <summary>Canonical MCP TODO id.</summary>
    public static readonly Regex CanonicalTodoId = new(
        @"^[A-Z]+-[A-Z0-9]+-\d{3}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>GitHub-backed ISSUE-N TODO id.</summary>
    public static readonly Regex IssueTodoId = new(
        @"^ISSUE-\d+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>FR/TR/TEST ids that must not be accepted as TODO ids.</summary>
    public static readonly Regex RequirementId = new(
        @"^(FR|TR|TEST)-",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// AC-FR-MCP-SESSIONLOGCTX-001-003 / AC-TR-MCP-SESSIONLOG-006-001:
    /// Validates both fields for a new turn persist.
    /// </summary>
    /// <param name="planFile">Incoming plan file or <see cref="NoneSentinel"/>.</param>
    /// <param name="todoId">Incoming TODO id or <see cref="NoneSentinel"/>.</param>
    /// <returns>Normalized pair.</returns>
    /// <exception cref="ArgumentException">When either field is omitted or invalid.</exception>
    public static (string PlanFile, string TodoId) ValidateForNewEntry(string? planFile, string? todoId)
    {
        return (ValidatePlanFile(planFile, required: true)!, ValidateTodoId(todoId, required: true)!);
    }

    /// <summary>
    /// AC-TR-MCP-SESSIONLOG-006-002: Validates a field only when it was supplied.
    /// </summary>
    /// <param name="planFile">Incoming plan file, or null when omitted.</param>
    /// <param name="todoId">Incoming TODO id, or null when omitted.</param>
    public static void ValidateIfSupplied(string? planFile, string? todoId)
    {
        if (planFile is not null)
            ValidatePlanFile(planFile, required: true);
        if (todoId is not null)
            ValidateTodoId(todoId, required: true);
    }

    /// <summary>
    /// Normalizes a non-sentinel plan path: slash convert, expand <c>~/</c>, reject <c>..</c>.
    /// </summary>
    /// <param name="planFile">Raw plan file value.</param>
    /// <param name="userProfilePath">Optional user profile used to expand <c>~/</c>.</param>
    /// <returns>Normalized path, or <see cref="NoneSentinel"/>.</returns>
    public static string NormalizePlanFile(string planFile, string? userProfilePath = null)
    {
        ArgumentNullException.ThrowIfNull(planFile);
        if (planFile == NoneSentinel)
            return NoneSentinel;

        var trimmed = planFile.Trim();
        if (trimmed.Length == 0)
            throw new ArgumentException("Invalid session turn planFile/todoId: planFile is empty.", nameof(planFile));

        var expanded = ExpandHome(trimmed, userProfilePath);
        var normalized = expanded.Replace('\\', '/');
        if (normalized.Contains("..", StringComparison.Ordinal))
            throw new ArgumentException("Invalid session turn planFile/todoId: planFile contains a '..' segment.", nameof(planFile));
        if (normalized.EndsWith('/'))
            throw new ArgumentException("Invalid session turn planFile/todoId: planFile must be a file, not a directory.", nameof(planFile));
        if (normalized.Any(static ch => char.IsControl(ch)))
            throw new ArgumentException("Invalid session turn planFile/todoId: planFile contains control characters.", nameof(planFile));
        if (normalized.Length > 2048)
            throw new ArgumentException("Invalid session turn planFile/todoId: planFile exceeds 2048 characters.", nameof(planFile));
        return normalized;
    }

    /// <summary>Validates and normalizes <paramref name="planFile"/>.</summary>
    public static string? ValidatePlanFile(string? planFile, bool required)
    {
        if (planFile is null)
        {
            if (required)
                throw new ArgumentException("Invalid session turn planFile/todoId: planFile is omitted.", nameof(planFile));
            return null;
        }

        if (string.IsNullOrWhiteSpace(planFile))
            throw new ArgumentException("Invalid session turn planFile/todoId: planFile is empty.", nameof(planFile));

        if (planFile == NoneSentinel)
            return NoneSentinel;

        if (planFile.Equals(NoneSentinel, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Invalid session turn planFile/todoId: sentinel must be the exact value 'None'.", nameof(planFile));

        return NormalizePlanFile(planFile);
    }

    /// <summary>Validates <paramref name="todoId"/>.</summary>
    public static string? ValidateTodoId(string? todoId, bool required)
    {
        if (todoId is null)
        {
            if (required)
                throw new ArgumentException("Invalid session turn planFile/todoId: todoId is omitted.", nameof(todoId));
            return null;
        }

        if (string.IsNullOrWhiteSpace(todoId))
            throw new ArgumentException("Invalid session turn planFile/todoId: todoId is empty.", nameof(todoId));

        if (todoId == NoneSentinel)
            return NoneSentinel;

        if (todoId.Equals(NoneSentinel, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Invalid session turn planFile/todoId: sentinel must be the exact value 'None'.", nameof(todoId));

        if (todoId.Length > 128)
            throw new ArgumentException("Invalid session turn planFile/todoId: todoId exceeds 128 characters.", nameof(todoId));

        if (RequirementId.IsMatch(todoId))
            throw new ArgumentException("Invalid session turn planFile/todoId: todoId must not be an FR/TR/TEST id.", nameof(todoId));

        if (CanonicalTodoId.IsMatch(todoId) || IssueTodoId.IsMatch(todoId))
            return todoId;

        throw new ArgumentException("Invalid session turn planFile/todoId: todoId is not a canonical TODO id or ISSUE-N.", nameof(todoId));
    }

    /// <summary>Expands a leading <c>~/</c> or <c>~\</c> to the user profile.</summary>
    public static string ExpandHome(string path, string? userProfilePath = null)
    {
        if (path.StartsWith("~/", StringComparison.Ordinal) || path.StartsWith("~\\", StringComparison.Ordinal))
        {
            var home = userProfilePath
                ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                ?? Environment.GetEnvironmentVariable("HOME")
                ?? Environment.GetEnvironmentVariable("USERPROFILE")
                ?? throw new ArgumentException("Invalid session turn planFile/todoId: cannot expand ~/ without a user profile.", nameof(path));
            return Path.Combine(home, path[2..]);
        }

        return path;
    }
}
