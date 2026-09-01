using System.Globalization;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Storage;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Orchestrates policy directive parsing, workspace mutation, and policy-change session logging.
/// </summary>
public sealed class WorkspacePolicyService : IWorkspacePolicyService
{
    private readonly IWorkspacePolicyDirectiveParser _parser;
    private readonly IWorkspaceService _workspaceService;
    private readonly ISessionLogService _sessionLogService;
    private readonly WorkspaceServiceAccessor _workspaceAccessor;
    private readonly McpDbContext _dbContext;
    private readonly ILogger<WorkspacePolicyService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkspacePolicyService"/> class.
    /// </summary>
    public WorkspacePolicyService(
        IWorkspacePolicyDirectiveParser parser,
        IWorkspaceService workspaceService,
        ISessionLogService sessionLogService,
        WorkspaceServiceAccessor workspaceAccessor,
        McpDbContext dbContext,
        ILogger<WorkspacePolicyService> logger)
    {
        _parser = parser;
        _workspaceService = workspaceService;
        _sessionLogService = sessionLogService;
        _workspaceAccessor = workspaceAccessor;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<WorkspacePolicyApplyResult> ApplyAsync(WorkspacePolicyApplyRequest request, CancellationToken ct = default)
    {
        if (request is null)
            return new WorkspacePolicyApplyResult { Success = false, Error = "Request body is required." };

        if (string.IsNullOrWhiteSpace(request.Directive))
            return new WorkspacePolicyApplyResult { Success = false, Error = "Directive is required." };

        var parseResult = await _parser.ParseAsync(request.Directive, request.WorkspacePath, ct).ConfigureAwait(false);
        if (!parseResult.Success || parseResult.Directive is null)
        {
            return new WorkspacePolicyApplyResult
            {
                Success = false,
                Error = parseResult.Error ?? "Unable to parse directive.",
            };
        }

        var directive = parseResult.Directive;
        var targets = await ResolveTargetsAsync(directive, request.WorkspacePath, ct).ConfigureAwait(false);
        if (targets.Count == 0)
        {
            return new WorkspacePolicyApplyResult
            {
                Success = false,
                Error = "No target workspaces were resolved for this directive.",
                ParsedDirective = directive,
            };
        }

        var previousWorkspacePath = _workspaceAccessor.GetWorkspacePath();
        var results = new List<WorkspacePolicyMutationResult>(targets.Count);

        try
        {
            foreach (var target in targets)
            {
                var beforeValues = GetCategoryValues(target, directive.Category);
                var afterValues = ApplyMutation(beforeValues, directive);
                var updateRequest = BuildUpdateRequest(directive.Category, afterValues);

                var mutation = await _workspaceService.UpdateAsync(target.WorkspacePath, updateRequest, ct).ConfigureAwait(false);
                if (!mutation.Success)
                {
                    results.Add(new WorkspacePolicyMutationResult
                    {
                        WorkspacePath = target.WorkspacePath,
                        WorkspaceName = target.Name,
                        Success = false,
                        Error = mutation.Error ?? "Workspace update failed.",
                        BeforeValues = beforeValues,
                        AfterValues = beforeValues,
                    });
                    continue;
                }

                var persisted = mutation.Workspace ?? await _workspaceService.GetAsync(target.WorkspacePath, ct).ConfigureAwait(false) ?? target;
                var persistedValues = GetCategoryValues(persisted, directive.Category);

                string? logError = null;
                try
                {
                    await LogPolicyChangeAsync(
                        workspacePath: target.WorkspacePath,
                        workspaceName: target.Name,
                        directive: directive,
                        originalDirective: request.Directive,
                        beforeValues: beforeValues,
                        afterValues: persistedValues,
                        ct: ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logError = $"Policy updated but session log write failed: {ex.Message}";
                    _logger.LogWarning(ex, "Policy change logging failed for workspace {WorkspacePath}", target.WorkspacePath);
                }

                results.Add(new WorkspacePolicyMutationResult
                {
                    WorkspacePath = target.WorkspacePath,
                    WorkspaceName = target.Name,
                    Success = logError is null,
                    Error = logError,
                    BeforeValues = beforeValues,
                    AfterValues = persistedValues,
                });
            }
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(previousWorkspacePath))
                _dbContext.OverrideWorkspaceId(previousWorkspacePath);
        }

        var allSucceeded = results.Count > 0 && results.All(static r => r.Success);
        return new WorkspacePolicyApplyResult
        {
            Success = allSucceeded,
            Error = allSucceeded ? null : "One or more workspace policy mutations failed.",
            ParsedDirective = directive,
            WorkspaceResults = results,
        };
    }

    private async Task<IReadOnlyList<WorkspaceDto>> ResolveTargetsAsync(
        WorkspacePolicyDirective directive,
        string? requestWorkspacePath,
        CancellationToken ct)
    {
        if (directive.Scope == "all")
        {
            var all = await _workspaceService.ListAsync(ct).ConfigureAwait(false);
            return all.Items;
        }

        if (directive.Scope == "workspace")
        {
            var requestedPath = directive.ScopeWorkspacePath ?? requestWorkspacePath;
            if (string.IsNullOrWhiteSpace(requestedPath))
                return [];

            var dto = await _workspaceService.GetAsync(requestedPath, ct).ConfigureAwait(false);
            return dto is null ? [] : [dto];
        }

        var currentWorkspacePath = !string.IsNullOrWhiteSpace(requestWorkspacePath)
            ? requestWorkspacePath
            : _workspaceAccessor.GetWorkspacePath();

        if (string.IsNullOrWhiteSpace(currentWorkspacePath))
            return [];

        var current = await _workspaceService.GetAsync(currentWorkspacePath, ct).ConfigureAwait(false);
        return current is null ? [] : [current];
    }

    private static IReadOnlyList<string> GetCategoryValues(WorkspaceDto workspace, string category)
    {
        return category switch
        {
            "license" => NormalizeValues(workspace.BannedLicenses, category),
            "country_of_origin" => NormalizeValues(workspace.BannedCountriesOfOrigin, category),
            "organization" => NormalizeValues(workspace.BannedOrganizations, category),
            "individual" => NormalizeValues(workspace.BannedIndividuals, category),
            _ => [],
        };
    }

    private static List<string> ApplyMutation(IReadOnlyList<string> beforeValues, WorkspacePolicyDirective directive)
    {
        var comparer = directive.Category == "country_of_origin"
            ? StringComparer.Ordinal
            : StringComparer.OrdinalIgnoreCase;

        var normalizedBefore = NormalizeValues(beforeValues, directive.Category);
        if (directive.Action == "clear")
            return [];

        var normalizedValues = NormalizeValues(directive.Values, directive.Category);
        if (directive.Action == "add")
        {
            var merged = new List<string>(normalizedBefore);
            foreach (var value in normalizedValues)
            {
                if (!merged.Contains(value, comparer))
                    merged.Add(value);
            }

            return merged;
        }

        // remove
        return normalizedBefore
            .Where(existing => !normalizedValues.Contains(existing, comparer))
            .ToList();
    }

    private static WorkspaceUpdateRequest BuildUpdateRequest(string category, List<string> updatedValues)
    {
        return category switch
        {
            "license" => new WorkspaceUpdateRequest { BannedLicenses = updatedValues },
            "country_of_origin" => new WorkspaceUpdateRequest { BannedCountriesOfOrigin = updatedValues },
            "organization" => new WorkspaceUpdateRequest { BannedOrganizations = updatedValues },
            "individual" => new WorkspaceUpdateRequest { BannedIndividuals = updatedValues },
            _ => throw new InvalidOperationException($"Unsupported policy category '{category}'."),
        };
    }

    private async Task LogPolicyChangeAsync(
        string workspacePath,
        string workspaceName,
        WorkspacePolicyDirective directive,
        string originalDirective,
        IReadOnlyList<string> beforeValues,
        IReadOnlyList<string> afterValues,
        CancellationToken ct)
    {
        _dbContext.OverrideWorkspaceId(workspacePath);

        var now = DateTimeOffset.UtcNow;
        var idTimestamp = now.ToString("yyyyMMddTHHmmssZ", CultureInfo.InvariantCulture);
        var scopeSlug = directive.Scope == "workspace" ? "target" : directive.Scope;
        var suffix = $"{SanitizeSlug(directive.Category)}-{SanitizeSlug(scopeSlug)}-{Guid.NewGuid().ToString("N")[..8]}";

        var sessionId = $"Copilot-{idTimestamp}-policy-{suffix}";
        var requestId = $"req-{idTimestamp}-policy-{Guid.NewGuid().ToString("N")[..8]}";

        var turn = new UnifiedRequestEntryDto
        {
            RequestId = requestId,
            Timestamp = now.ToString("o", CultureInfo.InvariantCulture),
            QueryTitle = "Apply workspace policy directive",
            QueryText = originalDirective,
            Interpretation = $"action={directive.Action}; category={directive.Category}; scope={directive.Scope}; parser={directive.Parser}",
            Response = $"Updated {directive.Category} policy in workspace '{workspaceName}'.",
            Status = "completed",
            PlanFile = SessionLogTurnContextValidator.NoneSentinel,
            TodoId = SessionLogTurnContextValidator.NoneSentinel,
            Actions =
            [
                new UnifiedActionDto
                {
                    Order = 1,
                    Type = "policy_change",
                    Status = "completed",
                    FilePath = workspacePath,
                    Description = $"Policy {directive.Action} for {directive.Category}: [{string.Join(", ", beforeValues)}] -> [{string.Join(", ", afterValues)}].",
                }
            ],
            Tags = ["policy", "policy_change", directive.Category],
            ContextList = [workspacePath],
        };

        var dto = new UnifiedSessionLogDto
        {
            SourceType = "Copilot",
            SessionId = sessionId,
            Title = $"Policy mutation - {workspaceName}",
            Model = "policy-management",
            Started = now.ToString("o", CultureInfo.InvariantCulture),
            LastUpdated = now.ToString("o", CultureInfo.InvariantCulture),
            Status = "completed",
            TurnCount = 1,
            Workspace = new WorkspaceInfoDto
            {
                Project = workspaceName,
                Repository = workspacePath,
            },
            Turns = [turn],
        };

        await _sessionLogService.SubmitAsync(dto, cancellationToken: ct).ConfigureAwait(false);
    }

    private static List<string> NormalizeValues(IEnumerable<string>? values, string category)
    {
        if (values is null)
            return [];

        var comparer = category == "country_of_origin" ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
        var seen = new HashSet<string>(comparer);
        var normalized = new List<string>();

        foreach (var raw in values)
        {
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            var candidate = raw.Trim();
            if (category == "country_of_origin")
                candidate = candidate.ToUpperInvariant();

            if (seen.Add(candidate))
                normalized.Add(candidate);
        }

        return normalized;
    }

    private static string SanitizeSlug(string raw)
    {
        var chars = raw
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray();

        var normalized = new string(chars).Trim('-');
        return string.IsNullOrWhiteSpace(normalized) ? "policy" : normalized;
    }
}
