using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.Services;

/// <summary>FR-MCP-HYGIENE-001 through 005: Read-only workspace hygiene validator.</summary>
public sealed class WorkspaceValidationService : IWorkspaceValidationService
{
    private const int QueryBatchSize = 256;

    private readonly McpDbContext _db;
    private readonly TimeProvider _time;

    /// <summary>TR-MCP-HYGIENE-001: Constructor.</summary>
    public WorkspaceValidationService(McpDbContext db, TimeProvider? time = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _time = time ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async Task<WorkspaceValidationResult> ValidateAsync(WorkspaceValidationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        var started = _time.GetUtcNow();
        var workspaceId = _db.CurrentWorkspaceId ?? string.Empty;

        if (!request.Authenticated)
        {
            return new WorkspaceValidationResult
            {
                Success = false,
                HttpStatus = 401,
                ErrorCode = "unauthenticated",
                Error = "Workspace validation requires authentication.",
                WorkspaceId = workspaceId,
                RunUtc = started,
            };
        }

        var threshold = request.StaleTurnThresholdHours ?? 48;
        if (threshold <= 0 || threshold > 168)
        {
            return new WorkspaceValidationResult
            {
                Success = false,
                HttpStatus = 400,
                ErrorCode = "threshold_out_of_range",
                Error = "Stale-turn threshold must be greater than 0 and at most 168 hours.",
                WorkspaceId = workspaceId,
                RunUtc = started,
                StaleTurnThresholdHours = threshold,
            };
        }

        var enabled = new HashSet<string>(WorkspaceValidationRuleRegistry.KnownCodes, StringComparer.Ordinal);
        var diagnostics = new List<string>();
        if (request.RuleCodes.Count > 0)
        {
            enabled.Clear();
            foreach (var code in request.RuleCodes)
            {
                if (WorkspaceValidationRuleRegistry.KnownCodes.Contains(code))
                    enabled.Add(code);
                else
                    diagnostics.Add("unknown_rule: " + code);
            }
        }

        var findings = new List<WorkspaceValidationFinding>();
        await CollectRequirementFindingsAsync(enabled, findings, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        await CollectTodoFindingsAsync(enabled, findings, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        await CollectSessionFindingsAsync(enabled, findings, threshold, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        await CollectTriageFindingsAsync(enabled, findings, cancellationToken).ConfigureAwait(false);

        var errorCount = findings.Count(item => item.Severity is "Error" or "Critical");
        var warningCount = findings.Count(item => item.Severity == "Warning");
        var limit = request.Limit <= 0 ? 200 : Math.Min(request.Limit, 1000);
        var page = findings
            .Skip(Math.Max(0, request.Offset))
            .Take(limit)
            .ToList();

        return new WorkspaceValidationResult
        {
            Success = true,
            HttpStatus = 200,
            WorkspaceId = workspaceId,
            RuleRegistryVersion = WorkspaceValidationRuleRegistry.Version,
            RunUtc = started,
            Duration = _time.GetUtcNow() - started,
            StaleTurnThresholdHours = threshold,
            ThresholdOverrideRecorded = request.StaleTurnThresholdHours is not null,
            ExitCode = errorCount > 0 ? 1 : 0,
            Diagnostics = diagnostics,
            Findings = page,
            TotalFindings = findings.Count,
            ErrorCount = errorCount,
            WarningCount = warningCount,
        };
    }

    private async Task CollectRequirementFindingsAsync(HashSet<string> enabled, List<WorkspaceValidationFinding> findings, CancellationToken cancellationToken)
    {
        var requirements = await LoadBatchesAsync(
                _db.Requirements.AsNoTracking().OrderBy(item => item.Kind).ThenBy(item => item.Id),
                cancellationToken)
            .ConfigureAwait(false);
        var criteria = await LoadBatchesAsync(
                _db.RequirementAcceptanceCriteria.AsNoTracking().OrderBy(item => item.Id),
                cancellationToken)
            .ConfigureAwait(false);
        var links = await LoadBatchesAsync(
                _db.RequirementTraceabilityLinks.AsNoTracking()
                    .OrderBy(item => item.FrId)
                    .ThenBy(item => item.TargetKind)
                    .ThenBy(item => item.TargetId),
                cancellationToken)
            .ConfigureAwait(false);
        var requirementIds = requirements.Select(item => (item.Kind, item.Id)).ToHashSet();
        var criteriaByRequirement = criteria.GroupBy(item => (item.RequirementKind, item.RequirementId)).ToDictionary(group => group.Key, group => group.Count());

        if (enabled.Contains("missing_acceptance_criteria"))
        {
            foreach (var requirement in requirements)
            {
                criteriaByRequirement.TryGetValue((requirement.Kind, requirement.Id), out var count);
                if (count == 0)
                {
                    findings.Add(Finding("missing_acceptance_criteria", "Error", "requirement", requirement.Id,
                        "Requirement has no acceptance-criteria rows.", "Add structured acceptance criteria."));
                }
            }
        }

        if (enabled.Contains("tr_orphan_no_fr"))
        {
            foreach (var tr in requirements.Where(item => item.Kind == "tr"))
            {
                if (!links.Any(link => link.TargetKind == "tr" && link.TargetId == tr.Id))
                    findings.Add(Finding("tr_orphan_no_fr", "Error", "requirement", tr.Id, "TR is not mapped from any FR.", "Add an FR-to-TR mapping."));
            }
        }

        if (enabled.Contains("fr_missing_tr_or_test"))
        {
            foreach (var fr in requirements.Where(item => item.Kind == "fr"))
            {
                var hasTr = links.Any(link => link.FrId == fr.Id && link.TargetKind == "tr");
                var hasTest = links.Any(link => link.FrId == fr.Id && link.TargetKind == "test");
                if (!hasTr || !hasTest)
                    findings.Add(Finding("fr_missing_tr_or_test", "Error", "requirement", fr.Id, "FR lacks required TR or TEST coverage.", "Map at least one TR and one TEST."));
            }
        }

        if (enabled.Contains("test_orphan_no_fr"))
        {
            foreach (var test in requirements.Where(item => item.Kind == "test"))
            {
                if (!links.Any(link => link.TargetKind == "test" && link.TargetId == test.Id))
                    findings.Add(Finding("test_orphan_no_fr", "Error", "requirement", test.Id, "TEST is not mapped from any FR.", "Add an FR-to-TEST mapping."));
            }
        }

        if (enabled.Contains("broken_or_duplicate_mapping"))
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var link in links)
            {
                var key = link.FrId + "|" + link.TargetKind + "|" + link.TargetId;
                if (!seen.Add(key))
                {
                    findings.Add(Finding("broken_or_duplicate_mapping", "Error", "mapping", key, "Duplicate traceability mapping.", "Remove the duplicate mapping."));
                    continue;
                }

                if (!requirementIds.Contains(("fr", link.FrId)) || !requirementIds.Contains((link.TargetKind, link.TargetId)))
                    findings.Add(Finding("broken_or_duplicate_mapping", "Error", "mapping", key, "Mapping references a missing requirement.", "Repair or delete the broken mapping."));
            }
        }
    }

    private async Task CollectTodoFindingsAsync(HashSet<string> enabled, List<WorkspaceValidationFinding> findings, CancellationToken cancellationToken)
    {
        var todos = await LoadBatchesAsync(
                _db.TodoItems.AsNoTracking().OrderBy(item => item.Id),
                cancellationToken)
            .ConfigureAwait(false);
        var tasks = await LoadBatchesAsync(
                _db.TodoItemTasks.AsNoTracking().OrderBy(item => item.Id),
                cancellationToken)
            .ConfigureAwait(false);
        var lists = await LoadBatchesAsync(
                _db.TodoItemListItems.AsNoTracking().OrderBy(item => item.Id),
                cancellationToken)
            .ConfigureAwait(false);
        var reqLinks = await LoadBatchesAsync(
                _db.TodoRequirementLinks.AsNoTracking()
                    .OrderBy(item => item.TodoId)
                    .ThenBy(item => item.RequirementId),
                cancellationToken)
            .ConfigureAwait(false);
        var requirements = (await LoadBatchesAsync(
                _db.Requirements.AsNoTracking().OrderBy(item => item.Id),
                cancellationToken)
            .ConfigureAwait(false))
            .Select(item => item.Id)
            .ToList();
        var todoIds = todos.Select(item => item.Id).ToHashSet(StringComparer.Ordinal);
        var requirementIds = requirements.ToHashSet(StringComparer.Ordinal);
        var tasksByTodo = tasks.GroupBy(item => item.TodoId).ToDictionary(group => group.Key, group => group.ToList());

        foreach (var todo in todos)
        {
            tasksByTodo.TryGetValue(todo.Id, out var todoTasks);
            todoTasks ??= [];
            if (enabled.Contains("todo_done_incomplete_tasks") && todo.Done && todoTasks.Any(task => !task.Done))
                findings.Add(Finding("todo_done_incomplete_tasks", "Error", "todo", todo.Id, "done=true with an incomplete implementation task.", "Complete remaining tasks or mark the TODO open."));
            if (enabled.Contains("todo_open_all_tasks_complete") && !todo.Done && todoTasks.Count > 0 && todoTasks.All(task => task.Done))
                findings.Add(Finding("todo_open_all_tasks_complete", "Error", "todo", todo.Id, "done=false with all implementation tasks complete.", "Mark the TODO done or reopen a task."));
            if (enabled.Contains("todo_done_missing_summary") && todo.Done && string.IsNullOrWhiteSpace(todo.DoneSummary))
                findings.Add(Finding("todo_done_missing_summary", "Error", "todo", todo.Id, "done=true without doneSummary.", "Record a doneSummary."));
            if (enabled.Contains("todo_remaining_contradicts_completion") && todo.Done && !string.IsNullOrWhiteSpace(todo.Remaining))
                findings.Add(Finding("todo_remaining_contradicts_completion", "Warning", "todo", todo.Id, "Remaining text contradicts completion.", "Clear remaining or reopen the TODO."));
        }

        if (enabled.Contains("todo_missing_dependency"))
        {
            foreach (var item in lists.Where(row => row.ListType == "DependsOn"))
            {
                if (!todoIds.Contains(item.Value))
                    findings.Add(Finding("todo_missing_dependency", "Error", "todo", item.TodoId, "Depends-on target is missing: " + item.Value, "Fix or remove the dependency."));
            }
        }

        if (enabled.Contains("todo_missing_requirement"))
        {
            foreach (var link in reqLinks)
            {
                if (!requirementIds.Contains(link.RequirementId))
                    findings.Add(Finding("todo_missing_requirement", "Error", "todo", link.TodoId, "Referenced requirement is missing: " + link.RequirementId, "Fix or remove the requirement link."));
            }

            foreach (var item in lists.Where(row => row.ListType is "FunctionalRequirement" or "TechnicalRequirement"))
            {
                if (!requirementIds.Contains(item.Value))
                    findings.Add(Finding("todo_missing_requirement", "Error", "todo", item.TodoId, "Referenced requirement is missing: " + item.Value, "Fix or remove the requirement reference."));
            }
        }
    }

    private async Task CollectSessionFindingsAsync(HashSet<string> enabled, List<WorkspaceValidationFinding> findings, double thresholdHours, CancellationToken cancellationToken)
    {
        if (!enabled.Contains("session_stale_in_progress"))
            return;

        var cutoff = _time.GetUtcNow() - TimeSpan.FromHours(thresholdHours);
        var stale = await _db.SessionLogTurns.AsNoTracking()
            .Where(turn => turn.Status == "in_progress" && turn.Timestamp != null && turn.Timestamp < cutoff)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        foreach (var turn in stale)
        {
            findings.Add(Finding("session_stale_in_progress", "Error", "session-turn", turn.RequestId ?? turn.Id.ToString(),
                "Turn has been in_progress longer than the stale threshold.", "Complete or fail the turn."));
        }
    }

    private async Task CollectTriageFindingsAsync(HashSet<string> enabled, List<WorkspaceValidationFinding> findings, CancellationToken cancellationToken)
    {
        var reports = await LoadBatchesAsync(
                _db.TriageReports.AsNoTracking().OrderBy(item => item.ReportId),
                cancellationToken)
            .ConfigureAwait(false);
        foreach (var report in reports)
        {
            if (TriageService.IsFailedStatus(report.Status))
            {
                if (enabled.Contains("triage_processing_failed"))
                    findings.Add(Finding("triage_processing_failed", "Error", "triage", report.ReportId, "Triage report processing failed.", "Inspect the failed report; this is distinct from pending work."));
                continue;
            }

            if (enabled.Contains("triage_nonterminal") && TriageService.IsNonTerminalStatus(report.Status))
                findings.Add(Finding("triage_nonterminal", "Error", "triage", report.ReportId, "Triage report is in a live non-terminal state: " + report.Status, "Finish processing or explicitly close the report."));
        }
    }

    private static async Task<List<T>> LoadBatchesAsync<T>(IOrderedQueryable<T> query, CancellationToken cancellationToken)
    {
        var results = new List<T>();
        var offset = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var batch = await query
                .Skip(offset)
                .Take(QueryBatchSize)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            if (batch.Count == 0)
                break;
            results.AddRange(batch);
            if (batch.Count < QueryBatchSize)
                break;
            offset += batch.Count;
        }

        return results;
    }

    private static WorkspaceValidationFinding Finding(string code, string severity, string kind, string recordId, string evidence, string remediation)
        => new()
        {
            RuleCode = code,
            RuleVersion = WorkspaceValidationRuleRegistry.Version,
            Severity = severity,
            EntityKind = kind,
            RecordId = recordId,
            Evidence = evidence,
            Remediation = remediation,
        };
}
