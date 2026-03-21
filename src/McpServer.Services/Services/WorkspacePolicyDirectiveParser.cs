using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using McpServer.Common.Copilot;
using McpServer.Support.Mcp.Options;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Parses natural-language policy directives using Copilot with deterministic fallback parsing.
/// </summary>
public sealed class WorkspacePolicyDirectiveParser : IWorkspacePolicyDirectiveParser
{
    private static readonly Regex s_quotedValueRegex = new("""(?:"([^"]+)"|'([^']+)')""", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex s_actionTailRegex = new(
        """(?:ban|block|disallow|prohibit|unban|remove|allow|clear|reset|add)\s+(.+?)(?:\s+(?:from|in|for|across|on)\s+.*)?$""",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex s_workspaceScopeRegex = new(
        """workspace\s+("([^"]+)"|'([^']+)'|([^\.,;]+))""",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex s_spdxLikeRegex = new(
        """\b[A-Za-z0-9][A-Za-z0-9\.\-\+]{2,}\b""",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Dictionary<string, string> s_countryAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["china"] = "CN",
        ["chinese"] = "CN",
        ["russia"] = "RU",
        ["russian"] = "RU",
        ["iran"] = "IR",
        ["iranian"] = "IR",
        ["north korea"] = "KP",
        ["dprk"] = "KP",
        ["belarus"] = "BY",
        ["belarusian"] = "BY",
    };

    private readonly ICopilotClient _copilotClient;
    private readonly WorkspaceServiceAccessor _workspaceAccessor;
    private readonly IOptionsMonitor<TodoPromptOptions> _promptOptions;
    private readonly ILogger<WorkspacePolicyDirectiveParser> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkspacePolicyDirectiveParser"/> class.
    /// </summary>
    public WorkspacePolicyDirectiveParser(
        ICopilotClient copilotClient,
        WorkspaceServiceAccessor workspaceAccessor,
        IOptionsMonitor<TodoPromptOptions> promptOptions,
        ILogger<WorkspacePolicyDirectiveParser> logger)
    {
        _copilotClient = copilotClient;
        _workspaceAccessor = workspaceAccessor;
        _promptOptions = promptOptions;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<WorkspacePolicyParseResult> ParseAsync(string directive, string? workspacePathHint, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(directive))
            return new WorkspacePolicyParseResult { Success = false, Error = "Directive is required." };

        var trimmed = directive.Trim();
        var copilotParsed = await TryParseWithCopilotAsync(trimmed, workspacePathHint, ct).ConfigureAwait(false);
        if (copilotParsed.Success)
            return copilotParsed;

        var fallback = TryParseFallback(trimmed, workspacePathHint);
        if (fallback.Success)
        {
            _logger.LogInformation("Policy directive parsed with deterministic fallback: {Directive}", trimmed);
            return fallback;
        }

        return new WorkspacePolicyParseResult
        {
            Success = false,
            Error = $"Failed to parse directive. Copilot parse error: {copilotParsed.Error}. Fallback parse error: {fallback.Error}",
        };
    }

    private async Task<WorkspacePolicyParseResult> TryParseWithCopilotAsync(string directive, string? workspacePathHint, CancellationToken ct)
    {
        try
        {
            var currentPromptOptions = _promptOptions.CurrentValue;
            var workingDirectory = ResolveWorkingDirectory(workspacePathHint);

            var options = new CopilotClientOptions
            {
                WorkingDirectory = workingDirectory,
                RunAs = currentPromptOptions.RunAs,
                GitHubToken = currentPromptOptions.GitHubToken,
            };
            if (!string.IsNullOrWhiteSpace(currentPromptOptions.AgentPath))
                options.AgentPath = currentPromptOptions.AgentPath;

            var result = await _copilotClient.InvokeAsync(BuildCopilotPrompt(directive, workspacePathHint), options, ct).ConfigureAwait(false);
            if (result.State != CopilotResultState.Success)
            {
                return new WorkspacePolicyParseResult
                {
                    Success = false,
                    Error = $"Copilot invocation failed with state '{result.State}' ({result.Stderr})",
                };
            }

            var content = StripMarkdownCodeFence(result.Body);
            var dto = JsonSerializer.Deserialize<CopilotDirectiveDto>(content, s_jsonOptions);
            if (dto is null)
                return new WorkspacePolicyParseResult { Success = false, Error = "Copilot returned an empty parse payload." };

            var normalized = NormalizeDirective(
                dto.Action,
                dto.Category,
                dto.Values,
                dto.Scope,
                dto.WorkspacePath,
                workspacePathHint,
                parser: "copilot");

            return new WorkspacePolicyParseResult { Success = true, Directive = normalized };
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Copilot policy parser returned invalid JSON.");
            return new WorkspacePolicyParseResult { Success = false, Error = "Copilot returned invalid JSON." };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Copilot policy parser failed.");
            return new WorkspacePolicyParseResult { Success = false, Error = ex.Message };
        }
    }

    private WorkspacePolicyParseResult TryParseFallback(string directive, string? workspacePathHint)
    {
        try
        {
            var lower = directive.ToLowerInvariant();

            var action = lower.Contains("clear", StringComparison.Ordinal)
                         || lower.Contains("reset", StringComparison.Ordinal)
                ? "clear"
                : lower.Contains("unban", StringComparison.Ordinal)
                  || lower.Contains("remove", StringComparison.Ordinal)
                  || lower.Contains("allow", StringComparison.Ordinal)
                    ? "remove"
                    : lower.Contains("ban", StringComparison.Ordinal)
                      || lower.Contains("block", StringComparison.Ordinal)
                      || lower.Contains("disallow", StringComparison.Ordinal)
                      || lower.Contains("prohibit", StringComparison.Ordinal)
                      || lower.Contains("add", StringComparison.Ordinal)
                        ? "add"
                        : string.Empty;

            if (action.Length == 0)
                return new WorkspacePolicyParseResult { Success = false, Error = "Unable to determine action (add/remove/clear)." };

            var category = DetectCategory(lower, directive);
            if (category.Length == 0)
                return new WorkspacePolicyParseResult { Success = false, Error = "Unable to determine category (license/country/organization/individual)." };

            var (scope, scopeWorkspacePath) = DetectScope(directive, workspacePathHint);
            var values = ExtractValues(directive, category, action);

            var normalized = NormalizeDirective(
                action,
                category,
                values,
                scope,
                scopeWorkspacePath,
                workspacePathHint,
                parser: "fallback");

            return new WorkspacePolicyParseResult { Success = true, Directive = normalized };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Fallback policy parser failed.");
            return new WorkspacePolicyParseResult { Success = false, Error = ex.Message };
        }
    }

    private static WorkspacePolicyDirective NormalizeDirective(
        string? actionRaw,
        string? categoryRaw,
        IReadOnlyList<string>? valuesRaw,
        string? scopeRaw,
        string? scopeWorkspacePathRaw,
        string? workspacePathHint,
        string parser)
    {
        var action = NormalizeAction(actionRaw);
        var category = NormalizeCategory(categoryRaw);
        var scope = NormalizeScope(scopeRaw);

        var values = (valuesRaw ?? [])
            .Where(static v => !string.IsNullOrWhiteSpace(v))
            .Select(static v => v.Trim())
            .ToList();

        if (category == "country_of_origin")
        {
            values = values
                .Select(NormalizeCountryValue)
                .Where(static v => !string.IsNullOrWhiteSpace(v))
                .Select(static v => v!)
                .ToList();
        }

        values = values
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (action is "add" or "remove" && values.Count == 0)
            throw new InvalidOperationException("Directive requires at least one value for add/remove actions.");

        var scopeWorkspacePath = scope == "workspace"
            ? (string.IsNullOrWhiteSpace(scopeWorkspacePathRaw) ? workspacePathHint : scopeWorkspacePathRaw)?.Trim()
            : null;

        if (scope == "workspace" && string.IsNullOrWhiteSpace(scopeWorkspacePath))
            throw new InvalidOperationException("Directive scope 'workspace' requires a workspace path.");

        if (scope == "current" && string.IsNullOrWhiteSpace(workspacePathHint))
        {
            // Keep current scope; orchestrator will resolve from ambient workspace context.
            scopeWorkspacePath = null;
        }

        return new WorkspacePolicyDirective
        {
            Action = action,
            Category = category,
            Values = values,
            Scope = scope,
            ScopeWorkspacePath = scopeWorkspacePath,
            Parser = parser,
        };
    }

    private static string NormalizeAction(string? actionRaw)
    {
        var normalized = actionRaw?.Trim().ToLowerInvariant();
        return normalized switch
        {
            "add" or "ban" or "block" or "disallow" or "prohibit" => "add",
            "remove" or "unban" or "allow" => "remove",
            "clear" or "reset" => "clear",
            _ => throw new InvalidOperationException($"Unsupported action '{actionRaw}'."),
        };
    }

    private static string NormalizeCategory(string? categoryRaw)
    {
        var normalized = categoryRaw?.Trim().ToLowerInvariant().Replace('-', '_');
        return normalized switch
        {
            "license" or "licenses" => "license",
            "country" or "countries" or "country_of_origin" or "origin" => "country_of_origin",
            "organization" or "organizations" or "org" => "organization",
            "individual" or "individuals" or "person" => "individual",
            _ => throw new InvalidOperationException($"Unsupported category '{categoryRaw}'."),
        };
    }

    private static string NormalizeScope(string? scopeRaw)
    {
        var normalized = scopeRaw?.Trim().ToLowerInvariant().Replace('-', '_');
        return normalized switch
        {
            "all" or "all_workspaces" or "global" => "all",
            "workspace" or "specific" => "workspace",
            "current" or "current_workspace" or "this" => "current",
            null or "" => "current",
            _ => throw new InvalidOperationException($"Unsupported scope '{scopeRaw}'."),
        };
    }

    private static string DetectCategory(string lowerDirective, string directive)
    {
        if (lowerDirective.Contains("license", StringComparison.Ordinal) || lowerDirective.Contains("spdx", StringComparison.Ordinal))
            return "license";

        if (lowerDirective.Contains("country", StringComparison.Ordinal)
            || lowerDirective.Contains("origin", StringComparison.Ordinal)
            || s_countryAliases.Keys.Any(alias => lowerDirective.Contains(alias, StringComparison.Ordinal)))
        {
            return "country_of_origin";
        }

        if (lowerDirective.Contains("organization", StringComparison.Ordinal)
            || lowerDirective.Contains("organisation", StringComparison.Ordinal)
            || lowerDirective.Contains("company", StringComparison.Ordinal)
            || lowerDirective.Contains("vendor", StringComparison.Ordinal)
            || lowerDirective.Contains("org ", StringComparison.Ordinal))
        {
            return "organization";
        }

        if (lowerDirective.Contains("individual", StringComparison.Ordinal)
            || lowerDirective.Contains("person", StringComparison.Ordinal)
            || lowerDirective.Contains("author", StringComparison.Ordinal)
            || lowerDirective.Contains("maintainer", StringComparison.Ordinal))
        {
            return "individual";
        }

        if (s_spdxLikeRegex.IsMatch(directive))
            return "license";

        return string.Empty;
    }

    private static (string Scope, string? ScopeWorkspacePath) DetectScope(string directive, string? workspacePathHint)
    {
        var lower = directive.ToLowerInvariant();
        if (lower.Contains("all workspaces", StringComparison.Ordinal)
            || lower.Contains("every workspace", StringComparison.Ordinal)
            || lower.Contains("across all workspaces", StringComparison.Ordinal)
            || lower.Contains("globally", StringComparison.Ordinal))
        {
            return ("all", null);
        }

        if (lower.Contains("this workspace", StringComparison.Ordinal)
            || lower.Contains("current workspace", StringComparison.Ordinal))
        {
            return ("current", null);
        }

        var wsMatch = s_workspaceScopeRegex.Match(directive);
        if (wsMatch.Success)
        {
            var value = wsMatch.Groups[2].Value;
            if (string.IsNullOrWhiteSpace(value)) value = wsMatch.Groups[3].Value;
            if (string.IsNullOrWhiteSpace(value)) value = wsMatch.Groups[4].Value;
            if (!string.IsNullOrWhiteSpace(value))
                return ("workspace", value.Trim());
        }

        return ("current", workspacePathHint);
    }

    private static IReadOnlyList<string> ExtractValues(string directive, string category, string action)
    {
        if (action == "clear")
            return [];

        var quoted = s_quotedValueRegex.Matches(directive)
            .Select(match => match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v.Trim())
            .ToList();
        if (quoted.Count > 0)
            return quoted;

        if (category == "country_of_origin")
        {
            var lower = directive.ToLowerInvariant();
            var countries = new List<string>();
            foreach (var alias in s_countryAliases.Keys)
            {
                if (lower.Contains(alias, StringComparison.Ordinal))
                    countries.Add(alias);
            }

            if (countries.Count > 0)
                return countries;
        }

        var tailMatch = s_actionTailRegex.Match(directive);
        if (!tailMatch.Success)
            return [];

        var segment = tailMatch.Groups[1].Value;
        segment = segment
            .Replace("licenses", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("license", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("countries", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("country", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("organizations", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("organization", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("individuals", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("individual", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("sources", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("source", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();

        if (segment.Length == 0)
            return [];

        return segment
            .Replace(" and ", ",", StringComparison.OrdinalIgnoreCase)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v.Trim().TrimEnd('.', ';'))
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToList();
    }

    private static string? NormalizeCountryValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        if (trimmed.Length == 2 && trimmed.All(char.IsLetter))
            return trimmed.ToUpperInvariant();

        if (s_countryAliases.TryGetValue(trimmed, out var aliasCode))
            return aliasCode;

        return trimmed.ToUpperInvariant();
    }

    private string ResolveWorkingDirectory(string? workspacePathHint)
    {
        if (!string.IsNullOrWhiteSpace(workspacePathHint))
            return Path.GetFullPath(workspacePathHint);

        var accessorPath = _workspaceAccessor.GetWorkspacePath();
        if (!string.IsNullOrWhiteSpace(accessorPath))
            return Path.GetFullPath(accessorPath);

        return Path.GetFullPath(Environment.CurrentDirectory);
    }

    private static string BuildCopilotPrompt(string directive, string? workspacePathHint)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Parse this policy directive into STRICT JSON.");
        sb.AppendLine("Return ONLY JSON. No code fences. No prose.");
        sb.AppendLine("Schema:");
        sb.AppendLine("{");
        sb.AppendLine("""  "action": "add|remove|clear",""");
        sb.AppendLine("""  "category": "license|country_of_origin|organization|individual",""");
        sb.AppendLine("""  "values": ["value1", "value2"],""");
        sb.AppendLine("""  "scope": "current|workspace|all",""");
        sb.AppendLine("  \"workspacePath\": \"required when scope=workspace; otherwise null\"");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("Interpretation rules:");
        sb.AppendLine("- ban/block/disallow/prohibit => action=add");
        sb.AppendLine("- unban/remove/allow => action=remove");
        sb.AppendLine("- clear/reset all bans => action=clear");
        sb.AppendLine("- 'all workspaces' => scope=all");
        sb.AppendLine("- 'this/current workspace' => scope=current");
        sb.AppendLine("- if a specific workspace is named/path provided => scope=workspace");
        sb.AppendLine();
        sb.AppendLine($"Workspace path hint: {workspacePathHint ?? "(none)"}");
        sb.AppendLine($"Directive: {directive}");
        return sb.ToString();
    }

    private static string StripMarkdownCodeFence(string text)
    {
        var trimmed = text.Trim();
        if (!trimmed.StartsWith("```", StringComparison.Ordinal))
            return trimmed;

        var firstNewline = trimmed.IndexOf('\n');
        if (firstNewline < 0)
            return trimmed.Trim('`');

        var withoutHeader = trimmed[(firstNewline + 1)..];
        var closingFence = withoutHeader.LastIndexOf("```", StringComparison.Ordinal);
        if (closingFence >= 0)
            withoutHeader = withoutHeader[..closingFence];

        return withoutHeader.Trim();
    }

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private sealed record CopilotDirectiveDto
    {
        public string? Action { get; init; }
        public string? Category { get; init; }
        public List<string>? Values { get; init; }
        public string? Scope { get; init; }
        public string? WorkspacePath { get; init; }
    }
}
