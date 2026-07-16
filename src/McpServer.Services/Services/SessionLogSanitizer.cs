using System.Collections;
using System.Text.Json;
using System.Text.RegularExpressions;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Sanitizes session log text and read models before they leave server read surfaces.
/// </summary>
public sealed class SessionLogSanitizer : ISessionLogSanitizer
{
    private const string RedactedPrefix = "[REDACTED:";
    private const int MaxPayloadDepth = 32;
    private readonly SessionLogSanitizationOptions options;
    private readonly ILogger<SessionLogSanitizer> logger;
    private readonly IReadOnlyList<RedactionRule> rules;

    // TR-MCP-SESSIONLOGSAN-002: the per-rule regex replace is an injectable seam so the timeout
    // fail-open behavior can be verified deterministically (a test forces exactly one field to raise
    // RegexMatchTimeoutException) instead of depending on catastrophic-backtracking wall-clock jitter
    // (BUG-TRIAGE-081). The default is a plain Regex.Replace, so production behavior is unchanged.
    private readonly RegexReplaceInvoker regexReplace;

    /// <summary>Runs a redaction rule's regex replace; the seam that makes timeout behavior testable.</summary>
    internal delegate string RegexReplaceInvoker(Regex regex, string input, MatchEvaluator evaluator);

    private static readonly RegexReplaceInvoker DefaultRegexReplace =
        static (regex, input, evaluator) => regex.Replace(input, evaluator);

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionLogSanitizer"/> class.
    /// </summary>
    /// <param name="options">Sanitization options.</param>
    public SessionLogSanitizer(IOptions<SessionLogSanitizationOptions> options)
        : this(options, NullLogger<SessionLogSanitizer>.Instance)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionLogSanitizer"/> class.
    /// </summary>
    /// <param name="options">Sanitization options.</param>
    /// <param name="logger">Logger used for sanitized timeout diagnostics.</param>
    public SessionLogSanitizer(
        IOptions<SessionLogSanitizationOptions> options,
        ILogger<SessionLogSanitizer> logger)
        : this(options, logger, DefaultRegexReplace)
    {
    }

    /// <summary>
    /// Test seam constructor: injects the regex-replace invoker so timeout handling is deterministic.
    /// </summary>
    /// <param name="options">Sanitization options.</param>
    /// <param name="logger">Logger used for sanitized timeout diagnostics.</param>
    /// <param name="regexReplace">Regex replace invoker (defaults to Regex.Replace in production).</param>
    internal SessionLogSanitizer(
        IOptions<SessionLogSanitizationOptions> options,
        ILogger<SessionLogSanitizer> logger,
        RegexReplaceInvoker regexReplace)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(regexReplace);

        this.options = options.Value;
        this.logger = logger;
        this.regexReplace = regexReplace;
        rules = BuildRules(this.options);
    }

    /// <inheritdoc />
    public string? SanitizeString(string? value) => SanitizeString(value, "value");

    private string? SanitizeString(string? value, string fieldPath)
    {
        if (!options.Enabled || string.IsNullOrEmpty(value))
            return value;

        var sanitized = value;
        foreach (var rule in rules)
        {
            try
            {
                sanitized = regexReplace(rule.Regex, sanitized, match => rule.Replace(match));
            }
            catch (RegexMatchTimeoutException)
            {
                logger.LogWarning(
                    "Session log sanitization rule {RuleId} timed out at {FieldPath}.",
                    rule.Id,
                    fieldPath);
                return $"[REDACTED:{rule.Id}:timeout]";
            }
        }

        return sanitized;
    }

    /// <inheritdoc />
    public UnifiedSessionLogDto? SanitizeSessionLog(UnifiedSessionLogDto? sessionLog)
    {
        if (sessionLog is null)
            return null;

        return new UnifiedSessionLogDto
        {
            SourceType = SanitizeString(sessionLog.SourceType),
            SessionId = SanitizeString(sessionLog.SessionId),
            AgentDefinitionId = SanitizeString(sessionLog.AgentDefinitionId),
            Title = SanitizeString(sessionLog.Title, "title"),
            Model = SanitizeString(sessionLog.Model),
            Started = SanitizeString(sessionLog.Started),
            LastUpdated = SanitizeString(sessionLog.LastUpdated),
            Status = SanitizeString(sessionLog.Status),
            TurnCount = sessionLog.TurnCount,
            Workspace = SanitizeWorkspace(sessionLog.Workspace),
            Turns = SanitizeTurns(sessionLog.Turns),
            TotalTokens = sessionLog.TotalTokens,
            CursorSessionLabel = SanitizeString(sessionLog.CursorSessionLabel),
            CopilotStatistics = SanitizeCopilotStatistics(sessionLog.CopilotStatistics),
        };
    }

    /// <inheritdoc />
    public SessionLogQueryResult SanitizeQueryResult(SessionLogQueryResult queryResult)
    {
        ArgumentNullException.ThrowIfNull(queryResult);

        return new SessionLogQueryResult
        {
            TotalCount = queryResult.TotalCount,
            Limit = queryResult.Limit,
            Offset = queryResult.Offset,
            Items = queryResult.Items.Select(item => SanitizeSessionLog(item)!).ToList(),
        };
    }

    private WorkspaceInfoDto? SanitizeWorkspace(WorkspaceInfoDto? workspace)
    {
        if (workspace is null)
            return null;

        return new WorkspaceInfoDto
        {
            Project = SanitizeString(workspace.Project),
            TargetFramework = SanitizeString(workspace.TargetFramework),
            Repository = SanitizeString(workspace.Repository),
            Branch = SanitizeString(workspace.Branch),
        };
    }

    private CopilotStatisticsDto? SanitizeCopilotStatistics(CopilotStatisticsDto? statistics)
    {
        if (statistics is null)
            return null;

        return new CopilotStatisticsDto
        {
            AverageSuccessScore = statistics.AverageSuccessScore,
            TotalNetTokens = statistics.TotalNetTokens,
            TotalNetPremiumRequests = statistics.TotalNetPremiumRequests,
            CompletedCount = statistics.CompletedCount,
            InProgressCount = statistics.InProgressCount,
        };
    }

    private ICollection<UnifiedRequestEntryDto>? SanitizeTurns(ICollection<UnifiedRequestEntryDto>? turns)
    {
        if (turns is null)
            return null;

        return turns.Select(SanitizeTurn).ToList();
    }

    private UnifiedRequestEntryDto SanitizeTurn(UnifiedRequestEntryDto turn)
    {
        return new UnifiedRequestEntryDto
        {
            RequestId = SanitizeString(turn.RequestId),
            Timestamp = SanitizeString(turn.Timestamp),
            QueryText = SanitizeString(turn.QueryText),
            QueryTitle = SanitizeString(turn.QueryTitle),
            Response = SanitizeString(turn.Response, "turns.response"),
            Interpretation = SanitizeString(turn.Interpretation),
            Status = SanitizeString(turn.Status),
            Actions = SanitizeActions(turn.Actions),
            Model = SanitizeString(turn.Model),
            ModelProvider = SanitizeString(turn.ModelProvider),
            TokenCount = turn.TokenCount,
            Tags = SanitizeStringCollection(turn.Tags),
            ContextList = SanitizeStringCollection(turn.ContextList),
            FailureNote = SanitizeString(turn.FailureNote),
            Score = turn.Score,
            IsPremium = turn.IsPremium,
            RawContext = SanitizePayload(turn.RawContext, 0, []),
            OriginalEntry = SanitizePayload(turn.OriginalEntry, 0, []),
            ProcessingDialog = SanitizeProcessingDialog(turn.ProcessingDialog),
            Commits = SanitizeCommits(turn.Commits),
            DesignDecisions = SanitizeStringCollection(turn.DesignDecisions),
            RequirementsDiscovered = SanitizeStringCollection(turn.RequirementsDiscovered),
            FilesModified = SanitizeStringCollection(turn.FilesModified),
            Blockers = SanitizeStringCollection(turn.Blockers),
        };
    }

    private ICollection<UnifiedActionDto>? SanitizeActions(ICollection<UnifiedActionDto>? actions)
    {
        if (actions is null)
            return null;

        return actions.Select(action => new UnifiedActionDto
        {
            Order = action.Order,
            Description = SanitizeString(action.Description),
            Type = SanitizeString(action.Type),
            Status = SanitizeString(action.Status),
            FilePath = SanitizeString(action.FilePath),
        }).ToList();
    }

    private ICollection<ProcessingDialogItemDto>? SanitizeProcessingDialog(ICollection<ProcessingDialogItemDto>? dialog)
    {
        if (dialog is null)
            return null;

        return dialog.Select(item => new ProcessingDialogItemDto
        {
            Timestamp = SanitizeString(item.Timestamp),
            Role = SanitizeString(item.Role),
            Content = SanitizeString(item.Content),
            Category = SanitizeString(item.Category),
        }).ToList();
    }

    private ICollection<SessionLogCommitDto>? SanitizeCommits(ICollection<SessionLogCommitDto>? commits)
    {
        if (commits is null)
            return null;

        return commits.Select(commit => new SessionLogCommitDto
        {
            Sha = SanitizeString(commit.Sha),
            Branch = SanitizeString(commit.Branch),
            Message = SanitizeString(commit.Message),
            Author = SanitizeString(commit.Author),
            Timestamp = SanitizeString(commit.Timestamp),
            FilesChanged = SanitizeStringCollection(commit.FilesChanged),
        }).ToList();
    }

    private ICollection<string>? SanitizeStringCollection(ICollection<string>? values)
    {
        if (values is null)
            return null;

        return values.Select(value => SanitizeString(value) ?? string.Empty).ToList();
    }

    private object? SanitizePayload(object? value, int depth, HashSet<object> visited)
    {
        if (value is null)
            return null;

        if (depth > MaxPayloadDepth)
            return ReplacementToken("payload-depth");

        if (value is string text)
            return SanitizeString(text);

        if (value is JsonElement jsonElement)
            return SanitizeJsonElement(jsonElement, depth, visited);

        if (IsScalar(value))
            return value;

        if (!visited.Add(value))
            return ReplacementToken("payload-cycle");

        try
        {
            return value switch
            {
                IDictionary<string, object?> dictionary => SanitizeDictionary(dictionary, depth, visited),
                IReadOnlyDictionary<string, object?> dictionary => SanitizeDictionary(dictionary, depth, visited),
                IDictionary dictionary => SanitizeDictionary(dictionary, depth, visited),
                object?[] array => array.Select(item => SanitizePayload(item, depth + 1, visited)).ToArray(),
                IList<object?> list => list.Select(item => SanitizePayload(item, depth + 1, visited)).ToList(),
                IList list => SanitizeList(list, depth, visited),
                _ => value,
            };
        }
        finally
        {
            visited.Remove(value);
        }
    }

    private Dictionary<string, object?> SanitizeDictionary(
        IEnumerable<KeyValuePair<string, object?>> dictionary,
        int depth,
        HashSet<object> visited)
    {
        var sanitized = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var item in dictionary)
        {
            var key = SanitizeString(item.Key) ?? item.Key;
            sanitized[key] = SanitizePayload(item.Value, depth + 1, visited);
        }

        return sanitized;
    }

    private Dictionary<string, object?> SanitizeDictionary(IDictionary dictionary, int depth, HashSet<object> visited)
    {
        var sanitized = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (DictionaryEntry item in dictionary)
        {
            var key = item.Key is string textKey ? SanitizeString(textKey) ?? textKey : item.Key?.ToString() ?? string.Empty;
            sanitized[key] = SanitizePayload(item.Value, depth + 1, visited);
        }

        return sanitized;
    }

    private List<object?> SanitizeList(IList list, int depth, HashSet<object> visited)
    {
        var sanitized = new List<object?>(list.Count);
        foreach (var item in list)
            sanitized.Add(SanitizePayload(item, depth + 1, visited));

        return sanitized;
    }

    private JsonElement SanitizeJsonElement(JsonElement element, int depth, HashSet<object> visited)
    {
        var sanitized = SanitizeJsonElementValue(element, depth + 1, visited);
        return JsonSerializer.SerializeToElement(sanitized);
    }

    private object? SanitizeJsonElementValue(JsonElement element, int depth, HashSet<object> visited)
    {
        if (depth > MaxPayloadDepth)
            return ReplacementToken("payload-depth");

        return element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(
                property => SanitizeString(property.Name) ?? property.Name,
                property => SanitizeJsonElementValue(property.Value, depth + 1, visited),
                StringComparer.Ordinal),
            JsonValueKind.Array => element.EnumerateArray()
                .Select(item => SanitizeJsonElementValue(item, depth + 1, visited))
                .ToList(),
            JsonValueKind.String => SanitizeString(element.GetString()),
            JsonValueKind.Number => ReadJsonNumber(element),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => null,
        };
    }

    private static object ReadJsonNumber(JsonElement element)
    {
        if (element.TryGetInt64(out var longValue))
            return longValue;

        if (element.TryGetDecimal(out var decimalValue))
            return decimalValue;

        return element.GetDouble();
    }

    private static bool IsScalar(object value)
    {
        var type = value.GetType();
        return type.IsPrimitive
            || value is decimal
            || value is DateTime
            || value is DateTimeOffset
            || value is TimeSpan
            || value is Guid
            || value is Uri;
    }

    private static IReadOnlyList<RedactionRule> BuildRules(SessionLogSanitizationOptions options)
    {
        var timeout = TimeSpan.FromMilliseconds(options.RegexTimeoutMilliseconds);
        var rules = new List<RedactionRule>();

        foreach (var configuredRule in options.Rules ?? [])
        {
            var ruleId = configuredRule.Id.Trim();
            var replacement = string.IsNullOrEmpty(configuredRule.Replacement)
                ? ReplacementToken(ruleId)
                : configuredRule.Replacement;
            rules.Add(CreateRule(ruleId, configuredRule.Pattern, timeout, _ => replacement));
        }

        rules.AddRange(
        [
            CreateRule(
                "pem-private-key",
                "-----BEGIN [A-Z ]*PRIVATE KEY-----[\\s\\S]*?-----END [A-Z ]*PRIVATE KEY-----",
                timeout),
            CreateRule(
                "bearer-token",
                "\\bBearer\\s+(?!\\[REDACTED:)[A-Za-z0-9._~+/=-]{16,}\\b",
                timeout,
                match => $"Bearer {ReplacementToken("bearer-token")}"),
            CreateRule(
                "jwt",
                "\\beyJ[A-Za-z0-9_-]{5,}\\.[A-Za-z0-9_-]{5,}\\.[A-Za-z0-9_-]{5,}\\b",
                timeout),
            CreateRule(
                "provider-token",
                "\\b(?:sk-[A-Za-z0-9_-]{16,}|ghp_[A-Za-z0-9_]{16,}|github_pat_[A-Za-z0-9_]{16,}|xox[baprs]-[A-Za-z0-9-]{10,}|AIza[0-9A-Za-z_-]{20,})\\b",
                timeout),
            CreateRule(
                "connection-string-password",
                "(;)(Password|Pwd)\\s*=\\s*(?!\\[REDACTED:)([^;\\r\\n]+)(?=;)",
                timeout,
                match => $"{match.Groups[1].Value}{match.Groups[2].Value}={ReplacementToken("connection-string-password")}"),
            CreateRule(
                "secret-assignment",
                "\\b(?:password|passwd|secret|api[_-]?key|apikey|access[_-]?token|refresh[_-]?token|token)\\s*[:=]\\s*(?!\\[REDACTED:)(?:\"[^\"\\r\\n]*\"|'[^'\\r\\n]*'|[^;\\s,&]+)",
                timeout),
        ]);

        return rules;
    }

    private static RedactionRule CreateRule(
        string id,
        string pattern,
        TimeSpan timeout,
        Func<Match, string>? replacementFactory = null)
    {
        var regex = new Regex(
            pattern,
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase,
            timeout);

        return new RedactionRule(id, regex, replacementFactory ?? (_ => ReplacementToken(id)));
    }

    private static string ReplacementToken(string id) => $"{RedactedPrefix}{id}]";

    private sealed record RedactionRule(string Id, Regex Regex, Func<Match, string> Replace);
}

