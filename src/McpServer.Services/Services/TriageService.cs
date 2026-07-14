using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-TRIAGE-001..003, TR-MCP-TRIAGE-001..004: Durable triage intake,
/// grouping, asynchronous research, and TODO conversion service.
/// </summary>
public sealed class TriageService : ITriageService
{
    private const string StatusCollecting = "collecting";
    private const string StatusQueued = "queued";
    private const string StatusProcessing = "processing";
    private const string StatusCompleted = "completed";
    private const string StatusFailed = "failed";
    private const string ReportStatusGrouped = "grouped";
    private const string TodoPrefix = "BUG-TRIAGE-";
    private const string TriageRuntimeInstructions =
        "Runtime shell note. When shell inspection is necessary on Windows, invoke PowerShell as `pwsh.exe` from PATH. Do not hard-code `C:\\Program Files\\PowerShell\\7\\pwsh.exe`; WindowsApps installs use a different location. If shell inspection is unnecessary, return schema-valid JSON from the Group JSON without launching shell commands.";
    private static readonly string[] TriageQueueStatuses = ["new", "quieting", "pending", StatusCollecting];
    private static readonly string[] ReportGroupQueueStatuses = ["ready", StatusQueued, "in_progress", StatusProcessing, "retry_pending"];

    private static readonly SemaphoreSlim TodoCreationLock = new(1, 1);

    private readonly McpDbContext _db;
    private readonly WorkspaceContext _workspaceContext;
    private readonly IWorkspaceService _workspaceService;
    private readonly ITriageResearchRunner _researchRunner;
    private readonly ITodoService _todoService;
    private readonly ITriageTodoCreator _triageTodoCreator;
    private readonly IPromptTemplateService _promptTemplateService;
    private readonly TriageOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<TriageService> _logger;

    /// <summary>Initializes a new instance of the <see cref="TriageService"/> class.</summary>
    public TriageService(
        McpDbContext db,
        WorkspaceContext workspaceContext,
        IWorkspaceService workspaceService,
        ITriageResearchRunner researchRunner,
        ITodoService todoService,
        ITriageTodoCreator triageTodoCreator,
        IPromptTemplateService promptTemplateService,
        IOptions<TriageOptions> options,
        TimeProvider timeProvider,
        ILogger<TriageService> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _workspaceContext = workspaceContext ?? throw new ArgumentNullException(nameof(workspaceContext));
        _workspaceService = workspaceService ?? throw new ArgumentNullException(nameof(workspaceService));
        _researchRunner = researchRunner ?? throw new ArgumentNullException(nameof(researchRunner));
        _todoService = todoService ?? throw new ArgumentNullException(nameof(todoService));
        _triageTodoCreator = triageTodoCreator ?? throw new ArgumentNullException(nameof(triageTodoCreator));
        _promptTemplateService = promptTemplateService ?? throw new ArgumentNullException(nameof(promptTemplateService));
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<TriageReportSubmitResult> SubmitReportAsync(
        TriageReportRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var validationError = ValidateReport(request);
        if (validationError is not null)
        {
            return new TriageReportSubmitResult
            {
                Success = false,
                Error = validationError,
            };
        }

        var originalWorkspacePath = ResolveSubmittingWorkspace(request);
        var mcpServerRelated = IsMcpServerRelated(request);
        var effectiveWorkspacePath = await ResolveEffectiveWorkspaceAsync(
            originalWorkspacePath,
            mcpServerRelated,
            cancellationToken).ConfigureAwait(false);
        _db.OverrideWorkspaceId(effectiveWorkspacePath);

        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existing = await _db.TriageReports
                .FirstOrDefaultAsync(r => r.IdempotencyKey == request.IdempotencyKey, cancellationToken)
                .ConfigureAwait(false);
            if (existing is not null)
            {
                var existingGroup = await _db.TriageGroups
                    .FirstAsync(g => g.GroupId == existing.GroupId, cancellationToken)
                    .ConfigureAwait(false);
                return Accepted(existing.ReportId, existing.GroupId, existingGroup.Status, existingGroup.QuietDeadlineUtc, effectiveWorkspacePath);
            }
        }

        var now = _timeProvider.GetUtcNow();
        var fingerprint = BuildFingerprint(request);
        var groupKey = Hash($"{effectiveWorkspacePath}|{fingerprint}");
        var groupId = $"triage-group-{groupKey[..16]}";
        var quietDeadline = now.Add(_options.QuietPeriod);

        var group = await _db.TriageGroups
            .FirstOrDefaultAsync(g => g.GroupKey == groupKey, cancellationToken)
            .ConfigureAwait(false);

        if (group is null)
        {
            group = new TriageGroupEntity
            {
                WorkspaceId = effectiveWorkspacePath,
                GroupId = groupId,
                GroupKey = groupKey,
                EffectiveWorkspacePath = effectiveWorkspacePath,
                Title = request.Title.Trim(),
                Summary = request.Summary.Trim(),
                Status = StatusCollecting,
                ReportCount = 0,
                FirstReportAtUtc = now,
                LastReportAtUtc = now,
                QuietDeadlineUtc = quietDeadline,
                IsMcpServerRelated = mcpServerRelated,
            };
            _db.TriageGroups.Add(group);
        }

        group.LastReportAtUtc = now;
        group.QuietDeadlineUtc = quietDeadline;
        group.ReportCount += 1;
        if (group.Status == StatusFailed)
        {
            group.Status = StatusCollecting;
            group.LastError = null;
        }

        var reportId = $"triage-report-{Guid.NewGuid():N}";
        var report = new TriageReportEntity
        {
            WorkspaceId = effectiveWorkspacePath,
            ReportId = reportId,
            GroupId = group.GroupId,
            OriginalWorkspacePath = originalWorkspacePath,
            EffectiveWorkspacePath = effectiveWorkspacePath,
            Title = request.Title.Trim(),
            Summary = request.Summary.Trim(),
            ObservedBehavior = TrimOrNull(request.ObservedBehavior),
            ExpectedBehavior = TrimOrNull(request.ExpectedBehavior),
            Severity = TrimOrNull(request.Severity) ?? "medium",
            Component = TrimOrNull(request.Component),
            DedupeKey = TrimOrNull(request.DedupeKey),
            ErrorSignature = TrimOrNull(request.ErrorSignature),
            Fingerprint = fingerprint,
            EvidenceJson = SerializeMap(request.Evidence),
            ListItems = BuildTriageListItems(request, effectiveWorkspacePath),
            ReporterAgent = TrimOrNull(request.ReporterAgent),
            SessionId = TrimOrNull(request.SessionId),
            TurnId = TrimOrNull(request.TurnId),
            CurrentTodoId = TrimOrNull(request.CurrentTodoId),
            IdempotencyKey = TrimOrNull(request.IdempotencyKey),
            Status = ReportStatusGrouped,
            CreatedUtc = now,
        };
        _db.TriageReports.Add(report);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Accepted(reportId, group.GroupId, group.Status, group.QuietDeadlineUtc, effectiveWorkspacePath);
    }

    /// <inheritdoc />
    public async Task<TriageReportDetail> GetReportAsync(string reportId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reportId))
            throw new ArgumentException("Report id is required.", nameof(reportId));

        var report = await _db.TriageReports
            .FirstOrDefaultAsync(r => r.ReportId == reportId, cancellationToken)
            .ConfigureAwait(false);

        return report is null
            ? throw new KeyNotFoundException($"Triage report '{reportId}' was not found.")
            : ToReportDetail(report);
    }

    /// <inheritdoc />
    public async Task<TriageGroupQueryResult> QueryGroupsAsync(
        string? status = null,
        string? workspacePath = null,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(workspacePath))
            _db.OverrideWorkspaceId(workspacePath.Trim());

        var query = _db.TriageGroups.AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
        {
            var trimmedStatus = status.Trim();
            query = query.Where(g => g.Status == trimmedStatus);
        }

        var groups = (await query
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false))
            .OrderByDescending(g => g.LastReportAtUtc)
            .ToList();

        var details = new List<TriageGroupDetail>(groups.Count);
        foreach (var group in groups)
            details.Add(await ToGroupDetailAsync(group, includeReports: false, cancellationToken).ConfigureAwait(false));

        return new TriageGroupQueryResult { Items = details, TotalCount = details.Count };
    }

    /// <inheritdoc />
    public async Task<TriageDashboardResult> GetDashboardAsync(
        string? workspacePath = null,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(workspacePath))
            _db.OverrideWorkspaceId(workspacePath.Trim());

        var groups = (await _db.TriageGroups
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false))
            .OrderByDescending(g => g.LastReportAtUtc)
            .ToList();

        var details = new List<TriageGroupDetail>(groups.Count);
        foreach (var group in groups)
            details.Add(await ToGroupDetailAsync(group, includeReports: true, cancellationToken).ConfigureAwait(false));

        var runHistory = await QueryRunsAsync(workspacePath: workspacePath, cancellationToken: cancellationToken).ConfigureAwait(false);
        return new TriageDashboardResult
        {
            TriageQueue = details.Where(group => HasStatus(group.Status, TriageQueueStatuses)).ToList(),
            ReportGroupQueue = details.Where(group => HasStatus(group.Status, ReportGroupQueueStatuses)).ToList(),
            RunHistory = runHistory.Items,
            TotalGroupCount = details.Count,
            TotalRunCount = runHistory.TotalCount,
        };
    }

    /// <inheritdoc />
    public async Task<TriageGroupDetail> GetGroupAsync(string groupId, CancellationToken cancellationToken = default)
    {
        var group = await GetGroupEntityAsync(groupId, cancellationToken).ConfigureAwait(false);
        return await ToGroupDetailAsync(group, includeReports: true, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TriageRunQueryResult> QueryRunsAsync(
        string? status = null,
        string? groupId = null,
        string? workspacePath = null,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(workspacePath))
            _db.OverrideWorkspaceId(workspacePath.Trim());

        var query = _db.TriageResearchRuns.AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
        {
            var trimmedStatus = status.Trim();
            query = query.Where(run => run.Status == trimmedStatus);
        }

        if (!string.IsNullOrWhiteSpace(groupId))
        {
            var trimmedGroupId = groupId.Trim();
            query = query.Where(run => run.GroupId == trimmedGroupId);
        }

        var runs = (await query
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false))
            .OrderByDescending(run => run.StartedUtc)
            .ToList();

        var details = await ToRunDetailsAsync(runs, cancellationToken).ConfigureAwait(false);
        return new TriageRunQueryResult { Items = details, TotalCount = details.Count };
    }

    /// <inheritdoc />
    public async Task<TriageResearchRunDetail> GetRunAsync(string runId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(runId))
            throw new ArgumentException("Run id is required.", nameof(runId));

        var run = await _db.TriageResearchRuns
            .FirstOrDefaultAsync(r => r.RunId == runId, cancellationToken)
            .ConfigureAwait(false);
        if (run is null)
            throw new KeyNotFoundException($"Triage run '{runId}' was not found.");

        return (await ToRunDetailsAsync([run], cancellationToken).ConfigureAwait(false)).Single();
    }

    /// <inheritdoc />
    public async Task<TriageCreatedTodoQueryResult> QueryCreatedTodosAsync(
        string? workspacePath = null,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(workspacePath))
            _db.OverrideWorkspaceId(workspacePath.Trim());

        var runs = await _db.TriageResearchRuns
            .Where(run => !string.IsNullOrEmpty(run.CreatedTodoId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var runGroupIds = runs
            .Select(run => run.GroupId)
            .Where(static groupId => !string.IsNullOrWhiteSpace(groupId))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var groups = await _db.TriageGroups
            .Where(group => !string.IsNullOrEmpty(group.CreatedTodoId) || runGroupIds.Contains(group.GroupId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var todoIds = groups
            .Select(group => group.CreatedTodoId)
            .Concat(runs.Select(run => run.CreatedTodoId))
            .Where(static todoId => !string.IsNullOrWhiteSpace(todoId))
            .Select(static todoId => todoId!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (todoIds.Length == 0)
            return new TriageCreatedTodoQueryResult();

        var todoItems = await _db.TodoItems
            .Where(todo => todoIds.Contains(todo.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var groupsById = groups.ToDictionary(group => group.GroupId, StringComparer.Ordinal);
        var groupsByTodoId = groups
            .Where(group => !string.IsNullOrWhiteSpace(group.CreatedTodoId))
            .GroupBy(group => group.CreatedTodoId!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(item => item.LastReportAtUtc).First(),
                StringComparer.OrdinalIgnoreCase);
        var runsByTodoId = runs
            .Where(run => !string.IsNullOrWhiteSpace(run.CreatedTodoId))
            .GroupBy(run => run.CreatedTodoId!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(item => item.CompletedUtc ?? item.StartedUtc)
                    .First(),
                StringComparer.OrdinalIgnoreCase);

        var items = todoItems
            .Select(todo =>
            {
                runsByTodoId.TryGetValue(todo.Id, out var run);
                TriageGroupEntity? group = null;
                if (!groupsByTodoId.TryGetValue(todo.Id, out group)
                    && run is not null)
                {
                    groupsById.TryGetValue(run.GroupId, out group);
                }

                return new TriageCreatedTodoDetail
                {
                    TodoId = todo.Id,
                    CreatedAtUtc = run?.CompletedUtc ?? run?.StartedUtc ?? group?.LastReportAtUtc ?? group?.FirstReportAtUtc ?? default,
                    WorkspacePath = group?.EffectiveWorkspacePath ?? todo.WorkspaceId,
                    GroupId = group?.GroupId ?? run?.GroupId,
                    RunId = run?.RunId,
                    GroupStatus = group?.Status,
                    RunStatus = run?.Status,
                    GroupTitle = group?.Title,
                    GroupSummary = group?.Summary,
                    ReportCount = group?.ReportCount ?? 0,
                    QuietDeadlineUtc = group?.QuietDeadlineUtc,
                };
            })
            .OrderByDescending(item => item.CreatedAtUtc)
            .ToList();

        return new TriageCreatedTodoQueryResult { Items = items, TotalCount = items.Count };
    }

    /// <inheritdoc />
    public async Task<TriageGroupDetail> FlushGroupAsync(string groupId, CancellationToken cancellationToken = default)
    {
        var group = await GetGroupEntityAsync(groupId, cancellationToken).ConfigureAwait(false);
        var now = _timeProvider.GetUtcNow();
        if (group.Status == StatusProcessing)
            await FailStaleProcessingRunsAsync(now, group.GroupId, cancellationToken).ConfigureAwait(false);

        group.QuietDeadlineUtc = now;
        if (group.Status == StatusFailed)
        {
            group.Status = StatusCollecting;
            group.LastError = null;
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await ToGroupDetailAsync(group, includeReports: true, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TriageGroupDetail> RetryGroupAsync(
        string groupId,
        bool force = false,
        CancellationToken cancellationToken = default)
    {
        var group = await GetGroupEntityAsync(groupId, cancellationToken).ConfigureAwait(false);
        var now = _timeProvider.GetUtcNow();
        if (group.Status == StatusProcessing)
        {
            if (force)
            {
                await ForceFailProcessingRunsAsync(group, now, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await FailStaleProcessingRunsAsync(now, group.GroupId, cancellationToken).ConfigureAwait(false);
            }

            if (group.Status == StatusProcessing)
                throw new InvalidOperationException($"Triage group '{group.GroupId}' is still processing and cannot be retried yet.");
        }

        await ClearStaleCreatedTodoReferenceForRetryAsync(group, now, cancellationToken).ConfigureAwait(false);

        group.Status = StatusCollecting;
        group.LastError = null;
        group.QuietDeadlineUtc = now;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await ToGroupDetailAsync(group, includeReports: true, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TriageGroupDeleteResult> DeleteGroupAsync(
        string groupId,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var group = await GetGroupEntityAsync(groupId, cancellationToken).ConfigureAwait(false);
        var reports = await _db.TriageReports
            .Include(r => r.ListItems)
            .Where(r => r.GroupId == group.GroupId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var now = _timeProvider.GetUtcNow();
        var trimmedReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        if (trimmedReason is not null)
        {
            _db.Entry(group).Property("DeleteReason").CurrentValue = trimmedReason;
            foreach (var report in reports)
                _db.Entry(report).Property("DeleteReason").CurrentValue = trimmedReason;
        }

        // Remove is intercepted by McpDbContext into a soft-delete (IsDeleted + DeletedAtUtc).
        // Decomposed report list-items carry a required ReportId, so remove them explicitly to
        // avoid a set-null on those dependent rows.
        foreach (var report in reports)
            _db.RemoveRange(report.ListItems);
        _db.TriageReports.RemoveRange(reports);
        _db.TriageGroups.Remove(group);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new TriageGroupDeleteResult
        {
            GroupId = group.GroupId,
            DeletedReportCount = reports.Count,
            DeletedAtUtc = now,
        };
    }

    private async Task ForceFailProcessingRunsAsync(
        TriageGroupEntity group,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var runs = await _db.TriageResearchRuns
            .Where(run => run.GroupId == group.GroupId && run.Status == StatusProcessing)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var error = "Triage research run was force retried before completion.";
        foreach (var run in runs)
        {
            run.Status = StatusFailed;
            run.Error = string.IsNullOrWhiteSpace(run.Error)
                ? error
                : $"{run.Error}{Environment.NewLine}{error}";
            run.CompletedUtc = now;
        }

        group.Status = StatusFailed;
        group.LastError = error;
    }

    /// <inheritdoc />
    public async Task<TriageGroupEditResult> CreateGroupFromSelectionAsync(
        TriageGroupSelectionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var selection = await ResolveSelectionAsync(request, excludedGroupId: null, cancellationToken).ConfigureAwait(false);
        await EnsureEditableGroupsAsync(selection.SourceGroups, cancellationToken).ConfigureAwait(false);
        var workspacePath = EnsureSingleWorkspace(selection.Reports);
        _db.OverrideWorkspaceId(workspacePath);

        var now = _timeProvider.GetUtcNow();
        var groupKey = Hash($"{workspacePath}|manual|{Guid.NewGuid():N}");
        var group = new TriageGroupEntity
        {
            WorkspaceId = workspacePath,
            GroupId = $"triage-group-{groupKey[..16]}",
            GroupKey = groupKey,
            EffectiveWorkspacePath = workspacePath,
            Title = TrimOrNull(request.Title) ?? selection.Reports.OrderBy(report => report.CreatedUtc).First().Title,
            Summary = TrimOrNull(request.Summary) ?? selection.Reports.OrderBy(report => report.CreatedUtc).First().Summary,
            Status = StatusQueued,
            ReportCount = 0,
            FirstReportAtUtc = now,
            LastReportAtUtc = now,
            QuietDeadlineUtc = now,
            IsMcpServerRelated = selection.SourceGroups.Any(group => group.IsMcpServerRelated),
        };
        _db.TriageGroups.Add(group);

        var movedCount = MoveReportsToGroup(selection.Reports, group);
        RefreshGroupAggregate(group, selection.Reports, group.Title, group.Summary, group.QuietDeadlineUtc);
        QueueManualGroup(group, now);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        var removedGroupIds = await RemoveEmptySourceGroupsAsync(
            selection.SourceGroups,
            group.GroupId,
            cancellationToken).ConfigureAwait(false);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new TriageGroupEditResult
        {
            Group = await ToGroupDetailAsync(group, includeReports: true, cancellationToken).ConfigureAwait(false),
            RemovedGroupIds = removedGroupIds,
            MovedReportCount = movedCount,
        };
    }

    /// <inheritdoc />
    public async Task<TriageGroupEditResult> ConsolidateIntoGroupAsync(
        string targetGroupId,
        TriageGroupSelectionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var targetGroup = await GetGroupEntityAsync(targetGroupId, cancellationToken).ConfigureAwait(false);
        var selection = await ResolveSelectionAsync(request, targetGroup.GroupId, cancellationToken).ConfigureAwait(false);
        await EnsureEditableGroupsAsync(selection.SourceGroups.Append(targetGroup).DistinctBy(group => group.GroupId).ToList(), cancellationToken)
            .ConfigureAwait(false);

        var workspacePath = EnsureSingleWorkspace(selection.Reports);
        if (!string.Equals(workspacePath, targetGroup.EffectiveWorkspacePath, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Selected triage reports must belong to the target group workspace.");

        var now = _timeProvider.GetUtcNow();
        var movedCount = MoveReportsToGroup(selection.Reports, targetGroup);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var targetReports = await _db.TriageReports
            .Where(report => report.GroupId == targetGroup.GroupId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        RefreshGroupAggregate(
            targetGroup,
            targetReports,
            targetGroup.Title,
            targetGroup.Summary,
            now.Add(_options.QuietPeriod));
        QueueManualGroup(targetGroup, now);
        var removedGroupIds = await RemoveEmptySourceGroupsAsync(
            selection.SourceGroups,
            targetGroup.GroupId,
            cancellationToken).ConfigureAwait(false);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new TriageGroupEditResult
        {
            Group = await ToGroupDetailAsync(targetGroup, includeReports: true, cancellationToken).ConfigureAwait(false),
            RemovedGroupIds = removedGroupIds,
            MovedReportCount = movedCount,
        };
    }

    /// <inheritdoc />
    public Task<TriageGroupEditResult> MergeGroupsAsync(
        string targetGroupId,
        TriageGroupSelectionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var groupIds = NormalizeIds(request.GroupIds).ToArray();
        if (groupIds.Length == 0)
            throw new ArgumentException("At least one source group id is required.", nameof(request));

        return ConsolidateIntoGroupAsync(
            targetGroupId,
            request with { GroupIds = groupIds },
            cancellationToken);
    }

    private async Task<TriageSelection> ResolveSelectionAsync(
        TriageGroupSelectionRequest request,
        string? excludedGroupId,
        CancellationToken cancellationToken)
    {
        var requestedGroupIds = NormalizeIds(request.GroupIds)
            .Where(groupId => !string.Equals(groupId, excludedGroupId, StringComparison.Ordinal))
            .ToArray();
        var requestedReportIds = NormalizeIds(request.ReportIds).ToArray();
        if (requestedGroupIds.Length == 0 && requestedReportIds.Length == 0)
            throw new ArgumentException("At least one group or report id is required.", nameof(request));

        var groups = requestedGroupIds.Length == 0
            ? []
            : await _db.TriageGroups
                .Where(group => requestedGroupIds.Contains(group.GroupId))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        var missingGroupIds = requestedGroupIds
            .Except(groups.Select(group => group.GroupId), StringComparer.Ordinal)
            .ToArray();
        if (missingGroupIds.Length > 0)
            throw new KeyNotFoundException($"Triage group '{missingGroupIds[0]}' was not found.");

        var reports = await _db.TriageReports
            .Where(report =>
                requestedGroupIds.Contains(report.GroupId)
                || requestedReportIds.Contains(report.ReportId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(excludedGroupId))
            reports = reports
                .Where(report => !string.Equals(report.GroupId, excludedGroupId, StringComparison.Ordinal))
                .ToList();

        var foundReportIds = reports.Select(report => report.ReportId).ToHashSet(StringComparer.Ordinal);
        var missingReportIds = requestedReportIds
            .Where(reportId => !foundReportIds.Contains(reportId))
            .ToArray();
        if (missingReportIds.Length > 0)
            throw new KeyNotFoundException($"Triage report '{missingReportIds[0]}' was not found.");
        if (reports.Count == 0)
            throw new InvalidOperationException("Selection did not include any source reports.");

        var reportGroupIds = reports
            .Select(report => report.GroupId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var knownGroupIds = groups.Select(group => group.GroupId).ToHashSet(StringComparer.Ordinal);
        var missingReportGroupIds = reportGroupIds
            .Where(groupId => !knownGroupIds.Contains(groupId))
            .ToArray();
        if (missingReportGroupIds.Length > 0)
        {
            var reportGroups = await _db.TriageGroups
                .Where(group => missingReportGroupIds.Contains(group.GroupId))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            groups.AddRange(reportGroups);
        }

        return new TriageSelection(reports, groups);
    }

    private async Task EnsureEditableGroupsAsync(
        IReadOnlyList<TriageGroupEntity> groups,
        CancellationToken cancellationToken)
    {
        var groupIds = groups
            .Select(group => group.GroupId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (groupIds.Length == 0)
            return;

        var blockedGroup = groups.FirstOrDefault(group =>
            !string.IsNullOrWhiteSpace(group.CreatedTodoId)
            || string.Equals(group.Status, StatusProcessing, StringComparison.OrdinalIgnoreCase)
            || string.Equals(group.Status, StatusCompleted, StringComparison.OrdinalIgnoreCase));
        if (blockedGroup is not null)
            throw new InvalidOperationException($"Triage group '{blockedGroup.GroupId}' cannot be edited after processing has started.");

        var hasRuns = await _db.TriageResearchRuns
            .AnyAsync(run => groupIds.Contains(run.GroupId), cancellationToken)
            .ConfigureAwait(false);
        if (hasRuns)
            throw new InvalidOperationException("Triage groups with run history cannot be edited.");
    }

    private static string EnsureSingleWorkspace(IEnumerable<TriageReportEntity> reports)
    {
        var workspacePaths = reports
            .Select(report => report.EffectiveWorkspacePath)
            .Where(static workspacePath => !string.IsNullOrWhiteSpace(workspacePath))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return workspacePaths.Length switch
        {
            0 => throw new InvalidOperationException("Selected triage reports do not have a workspace."),
            1 => workspacePaths[0],
            _ => throw new InvalidOperationException("Selected triage reports must belong to the same workspace."),
        };
    }

    private static int MoveReportsToGroup(IReadOnlyList<TriageReportEntity> reports, TriageGroupEntity targetGroup)
    {
        var moved = 0;
        foreach (var report in reports)
        {
            if (string.Equals(report.GroupId, targetGroup.GroupId, StringComparison.Ordinal))
                continue;

            report.GroupId = targetGroup.GroupId;
            moved++;
        }

        return moved;
    }

    private static void RefreshGroupAggregate(
        TriageGroupEntity group,
        IReadOnlyList<TriageReportEntity> reports,
        string title,
        string summary,
        DateTimeOffset quietDeadline)
    {
        if (reports.Count == 0)
            throw new InvalidOperationException($"Triage group '{group.GroupId}' has no reports.");

        var orderedReports = reports.OrderBy(report => report.CreatedUtc).ToList();
        group.Title = TrimOrNull(title) ?? orderedReports[0].Title;
        group.Summary = TrimOrNull(summary) ?? orderedReports[0].Summary;
        group.ReportCount = orderedReports.Count;
        group.FirstReportAtUtc = orderedReports[0].CreatedUtc;
        group.LastReportAtUtc = orderedReports[^1].CreatedUtc;
        group.QuietDeadlineUtc = quietDeadline;
        group.Status = StatusCollecting;
        group.LastError = null;
    }

    private static void QueueManualGroup(TriageGroupEntity group, DateTimeOffset now)
    {
        group.Status = StatusQueued;
        group.QuietDeadlineUtc = now;
        group.LastError = null;
    }

    private async Task<IReadOnlyList<string>> RemoveEmptySourceGroupsAsync(
        IReadOnlyList<TriageGroupEntity> sourceGroups,
        string targetGroupId,
        CancellationToken cancellationToken)
    {
        var removedGroupIds = new List<string>();
        foreach (var sourceGroup in sourceGroups
            .Where(group => !string.Equals(group.GroupId, targetGroupId, StringComparison.Ordinal))
            .DistinctBy(group => group.GroupId))
        {
            var remainingReports = await _db.TriageReports
                .Where(report => report.GroupId == sourceGroup.GroupId)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            if (remainingReports.Count == 0)
            {
                _db.TriageGroups.Remove(sourceGroup);
                removedGroupIds.Add(sourceGroup.GroupId);
                continue;
            }

            RefreshGroupAggregate(
                sourceGroup,
                remainingReports,
                sourceGroup.Title,
                sourceGroup.Summary,
                sourceGroup.QuietDeadlineUtc);
        }

        return removedGroupIds;
    }

    private static IEnumerable<string> NormalizeIds(IEnumerable<string>? values)
        => values?
            .Select(TrimOrNull)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .Cast<string>()
        ?? [];

    /// <inheritdoc />
    public async Task<TriageSweepResult> ProcessDueGroupsAsync(CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        await FailStaleProcessingRunsAsync(now, groupId: null, cancellationToken).ConfigureAwait(false);

        // Cross-tenant sweep (IgnoreQueryFilters is intentional; each group's own
        // workspace context is applied below). Deadline predicate + ordering now push
        // to SQL thanks to the DateTimeOffset->UTC-DateTime converter.
        var due = await _db.TriageGroups
            .IgnoreQueryFilters()
            .Where(g => (g.Status == StatusCollecting || g.Status == StatusQueued)
                && g.QuietDeadlineUtc <= now)
            .OrderBy(g => g.QuietDeadlineUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var processed = 0;
        var originalWorkspaceId = _db.CurrentWorkspaceId;
        var originalWorkspacePath = _workspaceContext.WorkspacePath;
        var originalWorkspaceName = _workspaceContext.WorkspaceName;
        foreach (var group in due)
        {
            try
            {
                ApplyGroupWorkspaceContext(group);
                await ProcessGroupAsync(group, cancellationToken).ConfigureAwait(false);
                processed++;
            }
            finally
            {
                _db.OverrideWorkspaceId(originalWorkspaceId);
                _workspaceContext.WorkspacePath = originalWorkspacePath;
                _workspaceContext.WorkspaceName = originalWorkspaceName;
            }
        }

        return new TriageSweepResult(processed);
    }

    private void ApplyGroupWorkspaceContext(TriageGroupEntity group)
    {
        var workspaceId = string.IsNullOrWhiteSpace(group.WorkspaceId)
            ? group.EffectiveWorkspacePath
            : group.WorkspaceId;
        _db.OverrideWorkspaceId(workspaceId);
        _workspaceContext.WorkspacePath = string.IsNullOrWhiteSpace(group.EffectiveWorkspacePath)
            ? workspaceId
            : group.EffectiveWorkspacePath;
        _workspaceContext.WorkspaceName = string.IsNullOrWhiteSpace(_workspaceContext.WorkspacePath)
            ? null
            : Path.GetFileName(_workspaceContext.WorkspacePath);
    }

    private async Task ProcessGroupAsync(TriageGroupEntity group, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(group.CreatedTodoId))
            return;

        group.Status = StatusProcessing;
        group.LastError = null;
        var run = new TriageResearchRunEntity
        {
            WorkspaceId = group.WorkspaceId,
            RunId = $"triage-run-{Guid.NewGuid():N}",
            GroupId = group.GroupId,
            Status = StatusProcessing,
            PromptTemplateId = _options.PromptTemplateId,
            StartedUtc = _timeProvider.GetUtcNow(),
        };
        _db.TriageResearchRuns.Add(run);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        using var outputLock = new SemaphoreSlim(1, 1);
        async Task AppendAgentOutputAsync(TriageResearchOutputUpdate update)
        {
            if (string.IsNullOrEmpty(update.Text))
                return;

            await outputLock.WaitAsync(CancellationToken.None).ConfigureAwait(false);
            try
            {
                if (string.Equals(update.StreamName, "stderr", StringComparison.OrdinalIgnoreCase))
                {
                    run.AgentStderr = AppendRunOutput(run.AgentStderr, update.Text);
                }
                else
                {
                    run.AgentStdout = AppendRunOutput(run.AgentStdout, update.Text);
                }

                await _db.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
            }
            finally
            {
                outputLock.Release();
            }
        }

        try
        {
            var detail = await ToGroupDetailAsync(group, includeReports: true, cancellationToken).ConfigureAwait(false);
            var groupJson = JsonSerializer.Serialize(detail, typeof(TriageGroupDetail), McpServicesJsonContext.Default);
            var prompt = await RenderPromptAsync(detail, groupJson, cancellationToken).ConfigureAwait(false);
            run.GroupJson = groupJson;
            run.Prompt = prompt;

            var rawResult = await _researchRunner.RunAsync(
                new TriageResearchRequest(detail, groupJson, prompt, group.EffectiveWorkspacePath, AppendAgentOutputAsync),
                cancellationToken).ConfigureAwait(false);
            run.RawOutput = rawResult.OutputJson;
            run.AgentStdout = MergeRunOutput(run.AgentStdout, rawResult.AgentStdout);
            run.AgentStderr = MergeRunOutput(run.AgentStderr, rawResult.AgentStderr);
            run.AgentExitCode = rawResult.AgentExitCode;

            if (!rawResult.Success)
            {
                MarkResearchFailure(group, run, rawResult.Error ?? "Triage research runner failed.");
                await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return;
            }

            var schema = ValidateResearchOutput(rawResult.OutputJson);
            var researchOutput = schema.Output;
            var schemaError = schema.Error;
            if ((!schema.Valid || researchOutput is null) && !ContainsJsonObject(rawResult.OutputJson))
            {
                researchOutput = BuildFallbackResearchOutput(
                    detail,
                    schemaError ?? "Triage research output failed schema validation.",
                    run.RunId);
            }

            if (researchOutput is null)
            {
                MarkResearchFailure(group, run, schemaError ?? "Triage research output failed schema validation.");
                await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return;
            }

            TriageTodoCreationAttempt creationAttempt;
            await TodoCreationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                creationAttempt = await CreateTriageTodoWithRetryAsync(group, researchOutput, cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                TodoCreationLock.Release();
            }

            var todoId = creationAttempt.RequestedTodoId;
            var createResult = creationAttempt.Result;
            var todoCreatedWithWarning = creationAttempt.CreatedWithWarning;
            if (createResult is null)
                throw new InvalidOperationException("Triage TODO creation did not return a result.");

            if (!createResult.Success && !todoCreatedWithWarning)
            {
                MarkResearchFailure(group, run, createResult.Error ?? "TODO creation failed.");
                await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return;
            }

            var createdTodoId = createResult.Item?.Id ?? todoId;
            group.CreatedTodoId = createdTodoId;
            group.Status = StatusCompleted;
            group.LastError = todoCreatedWithWarning ? createResult.Error : null;
            run.CreatedTodoId = createdTodoId;
            run.Status = StatusCompleted;
            run.Error = todoCreatedWithWarning ? createResult.Error : null;
            run.ResponseJson = JsonSerializer.Serialize(researchOutput, typeof(ResearchOutput), McpServicesJsonContext.Default);
            run.CompletedUtc = _timeProvider.GetUtcNow();
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Triage research failed for group {GroupId}.", group.GroupId);
            MarkResearchFailure(group, run, ex.Message);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<int> FailStaleProcessingRunsAsync(
        DateTimeOffset now,
        string? groupId,
        CancellationToken cancellationToken)
    {
        var maxRunTime = GetEffectiveMaxRunTime();
        var staleStartedBeforeUtc = now - maxRunTime;
        var query = _db.TriageResearchRuns
            .IgnoreQueryFilters()
            .Where(run => run.Status == StatusProcessing && run.StartedUtc <= staleStartedBeforeUtc);
        if (!string.IsNullOrWhiteSpace(groupId))
        {
            var trimmedGroupId = groupId.Trim();
            query = query.Where(run => run.GroupId == trimmedGroupId);
        }

        var staleRuns = await query
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        if (staleRuns.Count == 0)
            return 0;

        var staleGroupIds = staleRuns
            .Select(run => run.GroupId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var staleGroups = await _db.TriageGroups
            .IgnoreQueryFilters()
            .Where(group => staleGroupIds.Contains(group.GroupId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var error = $"Triage research run exceeded the configured maximum duration ({maxRunTime}) and was marked failed for retry.";

        foreach (var run in staleRuns)
        {
            run.Status = StatusFailed;
            run.Error = error;
            run.CompletedUtc = now;

            var group = staleGroups.FirstOrDefault(candidate =>
                string.Equals(candidate.GroupId, run.GroupId, StringComparison.Ordinal) &&
                string.Equals(candidate.WorkspaceId, run.WorkspaceId, StringComparison.Ordinal));
            if (group is null ||
                !string.Equals(group.Status, StatusProcessing, StringComparison.OrdinalIgnoreCase) ||
                !string.IsNullOrWhiteSpace(group.CreatedTodoId))
            {
                continue;
            }

            group.Status = StatusFailed;
            group.LastError = error;
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return staleRuns.Count;
    }

    private TimeSpan GetEffectiveMaxRunTime()
        => _options.MaxRunTime <= TimeSpan.Zero ? TimeSpan.FromMinutes(30) : _options.MaxRunTime;

    private void MarkResearchFailure(TriageGroupEntity group, TriageResearchRunEntity run, string error)
    {
        group.Status = StatusFailed;
        group.LastError = error;
        run.Status = StatusFailed;
        run.Error = error;
        run.CompletedUtc = _timeProvider.GetUtcNow();
    }

    private async Task ClearStaleCreatedTodoReferenceForRetryAsync(
        TriageGroupEntity group,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(group.CreatedTodoId))
            return;

        var createdTodoId = group.CreatedTodoId.Trim();
        var workspaceId = string.IsNullOrWhiteSpace(group.WorkspaceId)
            ? group.EffectiveWorkspacePath
            : group.WorkspaceId;
        var todoExists = await _db.TodoItems
            .IgnoreQueryFilters()
            .AnyAsync(todo =>
                    todo.Id == createdTodoId &&
                    todo.WorkspaceId == workspaceId,
                cancellationToken)
            .ConfigureAwait(false);
        if (todoExists)
        {
            throw new InvalidOperationException(
                $"Triage group '{group.GroupId}' already created TODO '{createdTodoId}' and cannot be retried.");
        }

        var warning = $"Created TODO id '{createdTodoId}' is not readable; retry cleared the stale created TODO reference.";
        group.CreatedTodoId = null;
        var runs = await _db.TriageResearchRuns
            .Where(run => run.GroupId == group.GroupId && run.CreatedTodoId == createdTodoId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        foreach (var run in runs)
        {
            run.CreatedTodoId = null;
            if (string.Equals(run.Status, StatusCompleted, StringComparison.OrdinalIgnoreCase))
                run.Status = StatusFailed;
            run.Error = string.IsNullOrWhiteSpace(run.Error)
                ? warning
                : $"{run.Error}{Environment.NewLine}{warning}";
            run.CompletedUtc ??= now;
        }
    }

    private async Task<TriageTodoCreationAttempt> CreateTriageTodoWithRetryAsync(
        TriageGroupEntity group,
        ResearchOutput researchOutput,
        CancellationToken cancellationToken)
    {
        var minimumBugNumber = 0;
        TodoMutationResult? lastResult = null;
        var lastTodoId = string.Empty;

        for (var attempt = 0; attempt < 25; attempt++)
        {
            var todoId = await GenerateNextTodoIdAsync(cancellationToken, minimumBugNumber).ConfigureAwait(false);
            var createResult = await _triageTodoCreator.CreateAsync(new TodoCreateRequest
            {
                Id = todoId,
                Title = researchOutput.Title.Trim(),
                Section = "Backlog",
                Priority = NormalizeTodoPriority(researchOutput.Severity),
                Description =
                [
                    researchOutput.Summary.Trim(),
                    $"Triage group: {group.GroupId}",
                    $"Reports: {group.ReportCount.ToString(CultureInfo.InvariantCulture)}",
                ],
                TechnicalDetails = BuildTechnicalDetails(researchOutput),
                FunctionalRequirements = ["FR-MCP-TRIAGE-002"],
                TechnicalRequirements = ["TR-MCP-TRIAGE-004"],
            }, cancellationToken).ConfigureAwait(false);

            lastTodoId = todoId;
            var todoCreatedWithWarning = await ConfirmTodoCreatedWithWarningAsync(createResult, todoId, cancellationToken)
                .ConfigureAwait(false);
            if (createResult.Success || todoCreatedWithWarning || !IsTodoIdConflict(createResult, todoId))
                return new TriageTodoCreationAttempt(todoId, createResult, todoCreatedWithWarning);

            minimumBugNumber = Math.Max(minimumBugNumber, ParseBugNumber(todoId));
            lastResult = createResult;
        }

        return new TriageTodoCreationAttempt(
            lastTodoId,
            lastResult ?? new TodoMutationResult(
                false,
                "TODO creation failed after repeated id collisions.",
                null,
                TodoMutationFailureKind.Conflict),
            false);
    }

    private static bool IsTodoIdConflict(TodoMutationResult result, string todoId)
    {
        return !result.Success &&
            result.FailureKind == TodoMutationFailureKind.Conflict &&
            !string.IsNullOrWhiteSpace(result.Error) &&
            result.Error.Contains(todoId, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<bool> ConfirmTodoCreatedWithWarningAsync(
        TodoMutationResult result,
        string requestedTodoId,
        CancellationToken cancellationToken)
    {
        if (result.Success)
            return false;
        if (result.Item is null)
            return false;
        if (result.FailureKind is not (TodoMutationFailureKind.ProjectionFailed or TodoMutationFailureKind.ExternalSyncFailed))
            return false;

        var createdTodoId = string.IsNullOrWhiteSpace(result.Item.Id)
            ? requestedTodoId
            : result.Item.Id;
        var workspaceId = string.IsNullOrWhiteSpace(_db.CurrentWorkspaceId)
            ? _workspaceContext.WorkspacePath
            : _db.CurrentWorkspaceId;
        if (string.IsNullOrWhiteSpace(workspaceId))
            return false;

        return await _db.TodoItems
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(todo =>
                    todo.Id == createdTodoId &&
                    todo.WorkspaceId == workspaceId,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private static string AppendRunOutput(string? current, string chunk)
    {
        if (string.IsNullOrEmpty(current))
            return chunk;
        if (current.EndsWith(Environment.NewLine, StringComparison.Ordinal) ||
            chunk.StartsWith(Environment.NewLine, StringComparison.Ordinal))
            return string.Concat(current, chunk);
        return string.Concat(current, Environment.NewLine, chunk);
    }

    private static string? MergeRunOutput(string? streamed, string? final)
    {
        return string.IsNullOrWhiteSpace(final) ? streamed : final;
    }

    private async Task<string> GenerateNextTodoIdAsync(CancellationToken cancellationToken, int minimumBugNumber = 0)
    {
        var max = Math.Max(0, minimumBugNumber);
        var existingGroups = await _db.TriageGroups
            .IgnoreQueryFilters()
            .Where(g => g.CreatedTodoId != null && g.CreatedTodoId.StartsWith(TodoPrefix))
            .Select(g => g.CreatedTodoId!)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var id in existingGroups)
            max = Math.Max(max, ParseBugNumber(id));

        var existingTodos = await _db.TodoItems
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(todo => todo.Id.StartsWith(TodoPrefix))
            .Select(todo => todo.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var id in existingTodos)
            max = Math.Max(max, ParseBugNumber(id));

        try
        {
            var todos = await _todoService.QueryAsync(new TodoQueryRequest { Keyword = TodoPrefix }, cancellationToken).ConfigureAwait(false);
            foreach (var item in todos.Items)
                max = Math.Max(max, ParseBugNumber(item.Id));
        }
        catch (NotSupportedException)
        {
            // The EF group scan still preserves idempotence for triage-created rows.
        }

        return $"{TodoPrefix}{(max + 1).ToString("000", CultureInfo.InvariantCulture)}";
    }

    private static int ParseBugNumber(string value)
    {
        var match = Regex.Match(value, "^BUG-TRIAGE-(\\d+)$", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
        return match.Success && int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0;
    }

    private async Task<TriageGroupEntity> GetGroupEntityAsync(string groupId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(groupId))
            throw new ArgumentException("Group id is required.", nameof(groupId));

        var group = await _db.TriageGroups
            .FirstOrDefaultAsync(g => g.GroupId == groupId, cancellationToken)
            .ConfigureAwait(false);
        return group ?? throw new KeyNotFoundException($"Triage group '{groupId}' was not found.");
    }

    private async Task<TriageGroupDetail> ToGroupDetailAsync(
        TriageGroupEntity group,
        bool includeReports,
        CancellationToken cancellationToken)
    {
        var reports = includeReports
            ? (await _db.TriageReports
                .Where(r => r.GroupId == group.GroupId)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false))
                .OrderBy(r => r.CreatedUtc)
                .Select(ToReportDetail)
                .ToList()
            : [];

        return new TriageGroupDetail
        {
            GroupId = group.GroupId,
            Status = group.Status,
            ReportCount = group.ReportCount,
            WorkspacePath = group.EffectiveWorkspacePath,
            Title = group.Title,
            Summary = group.Summary,
            QuietDeadlineUtc = group.QuietDeadlineUtc,
            CreatedTodoId = group.CreatedTodoId,
            LastError = group.LastError,
            Reports = reports,
        };
    }

    private static TriageReportDetail ToReportDetail(TriageReportEntity report) => new()
    {
        ReportId = report.ReportId,
        GroupId = report.GroupId,
        Status = report.Status,
        Title = report.Title,
        Summary = report.Summary,
        OriginalWorkspacePath = report.OriginalWorkspacePath,
        WorkspacePath = report.EffectiveWorkspacePath,
        CreatedUtc = report.CreatedUtc,
    };

    private async Task<IReadOnlyList<TriageResearchRunDetail>> ToRunDetailsAsync(
        IReadOnlyList<TriageResearchRunEntity> runs,
        CancellationToken cancellationToken)
    {
        var groupIds = runs
            .Select(run => run.GroupId)
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var groups = await _db.TriageGroups
            .Where(group => groupIds.Contains(group.GroupId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var groupsById = groups.ToDictionary(group => group.GroupId, StringComparer.Ordinal);

        return runs.Select(run =>
        {
            groupsById.TryGetValue(run.GroupId, out var group);
            return new TriageResearchRunDetail
            {
                RunId = run.RunId,
                GroupId = run.GroupId,
                Status = run.Status,
                WorkspacePath = group?.EffectiveWorkspacePath ?? run.WorkspaceId,
                GroupStatus = group?.Status,
                GroupTitle = group?.Title,
                GroupSummary = group?.Summary,
                ReportCount = group?.ReportCount ?? 0,
                PromptTemplateId = run.PromptTemplateId,
                Prompt = run.Prompt,
                GroupJson = run.GroupJson,
                RawOutput = run.RawOutput,
                AgentStdout = run.AgentStdout,
                AgentStderr = run.AgentStderr,
                AgentExitCode = run.AgentExitCode,
                ResponseJson = run.ResponseJson,
                Error = run.Error,
                CreatedTodoId = run.CreatedTodoId,
                StartedUtc = run.StartedUtc,
                CompletedUtc = run.CompletedUtc,
            };
        }).ToList();
    }

    private static TriageReportSubmitResult Accepted(
        string reportId,
        string groupId,
        string status,
        DateTimeOffset quietDeadline,
        string workspacePath) => new()
        {
            Success = true,
            ReportId = reportId,
            GroupId = groupId,
            Status = status,
            QuietDeadlineUtc = quietDeadline,
            WorkspacePath = workspacePath,
        };

    private static string? ValidateReport(TriageReportRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return "title is required.";
        if (string.IsNullOrWhiteSpace(request.Summary))
            return "summary is required.";
        return null;
    }

    private static bool HasStatus(string status, IReadOnlyList<string> statuses)
        => statuses.Contains(status, StringComparer.OrdinalIgnoreCase);

    private string ResolveSubmittingWorkspace(TriageReportRequest request)
    {
        var candidate = TrimOrNull(request.WorkspacePath)
            ?? TrimOrNull(_workspaceContext.WorkspacePath)
            ?? Environment.CurrentDirectory;
        return Path.GetFullPath(candidate);
    }

    private async Task<string> ResolveEffectiveWorkspaceAsync(
        string originalWorkspacePath,
        bool mcpServerRelated,
        CancellationToken cancellationToken)
    {
        if (!mcpServerRelated)
            return originalWorkspacePath;

        var workspaces = await _workspaceService.ListAsync(cancellationToken).ConfigureAwait(false);
        var target = workspaces.Items.FirstOrDefault(IsMcpServerWorkspace);
        return string.IsNullOrWhiteSpace(target?.WorkspacePath)
            ? originalWorkspacePath
            : Path.GetFullPath(target.WorkspacePath);
    }

    private static bool IsMcpServerWorkspace(WorkspaceDto workspace)
    {
        if (string.Equals(workspace.Name, "McpServer", StringComparison.OrdinalIgnoreCase))
            return true;

        var name = Path.GetFileName(workspace.WorkspacePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return string.Equals(name, "McpServer", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsMcpServerRelated(TriageReportRequest request)
    {
        var values = new List<string?>
        {
            request.Title,
            request.Summary,
            request.Component,
            request.ErrorSignature,
            request.DedupeKey,
        };
        values.AddRange(request.AffectedPaths ?? []);
        values.AddRange(request.AffectedSymbols ?? []);
        values.AddRange(request.Tags ?? []);
        if (request.Evidence is not null)
            values.AddRange(request.Evidence.SelectMany(pair => new[] { pair.Key, pair.Value }));

        var joined = string.Join(" ", values.Where(v => !string.IsNullOrWhiteSpace(v))).ToLowerInvariant();
        return joined.Contains("mcpserver", StringComparison.Ordinal)
            || joined.Contains("mcp server", StringComparison.Ordinal)
            || joined.Contains("mcp-server", StringComparison.Ordinal)
            || joined.Contains("/mcpserver/", StringComparison.Ordinal)
            || joined.Contains("mcp-transport", StringComparison.Ordinal)
            || joined.Contains("agents-readme-first", StringComparison.Ordinal)
            || joined.Contains("workflow.", StringComparison.Ordinal)
            || joined.Contains("invoke-codexmcpplugin", StringComparison.Ordinal);
    }

    private static string BuildFingerprint(TriageReportRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.DedupeKey))
            return $"dedupe:{NormalizeToken(request.DedupeKey)}";

        var fields = new[]
        {
            request.Component,
            request.AffectedPaths?.FirstOrDefault(),
            request.AffectedSymbols?.FirstOrDefault(),
            request.ErrorSignature,
            NormalizeTitleTokens(request.Title),
        };

        return Hash(string.Join("|", fields.Select(NormalizeToken)));
    }

    private static string NormalizeTitleTokens(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return string.Empty;

        return string.Join(
            " ",
            Regex.Matches(title.ToLowerInvariant(), "[a-z0-9]+", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100))
                .Select(match => match.Value)
                .Where(token => token.Length > 2)
                .OrderBy(token => token, StringComparer.Ordinal)
                .Take(8));
    }

    private static string NormalizeToken(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : Regex.Replace(value.Trim().ToLowerInvariant(), "\\s+", " ", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

    private static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static List<TriageReportListItemEntity> BuildTriageListItems(TriageReportRequest request, string workspaceId)
    {
        var items = new List<TriageReportListItemEntity>();
        AddTriageListItems(items, "AffectedPath", request.AffectedPaths, workspaceId);
        AddTriageListItems(items, "AffectedSymbol", request.AffectedSymbols, workspaceId);
        AddTriageListItems(items, "ReproductionHint", request.ReproductionHints, workspaceId);
        AddTriageListItems(items, "Tag", request.Tags, workspaceId);
        return items;
    }

    private static void AddTriageListItems(List<TriageReportListItemEntity> items, string listType, IReadOnlyList<string>? values, string workspaceId)
    {
        var normalized = values?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .ToArray();
        if (normalized is not { Length: > 0 })
            return;
        for (var i = 0; i < normalized.Length; i++)
        {
            items.Add(new TriageReportListItemEntity
            {
                WorkspaceId = workspaceId,
                ListType = listType,
                Ordinal = i,
                Value = normalized[i],
            });
        }
    }

    private static string? SerializeMap(IReadOnlyDictionary<string, string>? values)
        => values is { Count: > 0 }
            ? JsonSerializer.Serialize(
                values.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal),
                typeof(Dictionary<string, string>),
                McpServicesJsonContext.Default)
            : null;

    private static string? TrimOrNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task<string> RenderPromptAsync(
        TriageGroupDetail detail,
        string groupJson,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_options.PromptTemplateId))
        {
            var rendered = await _promptTemplateService.TestAsync(
                _options.PromptTemplateId,
                new PromptTemplateTestRequest
                {
                    Variables = new Dictionary<string, object?>
                    {
                        ["groupJson"] = groupJson,
                        ["groupId"] = detail.GroupId,
                        ["status"] = detail.Status,
                        ["workspacePath"] = detail.WorkspacePath,
                        ["title"] = detail.Title,
                        ["summary"] = detail.Summary,
                        ["reportCount"] = detail.ReportCount,
                    },
                },
                cancellationToken).ConfigureAwait(false);

            if (rendered.Success && !string.IsNullOrWhiteSpace(rendered.RenderedContent))
                return AddRuntimeInstructions(rendered.RenderedContent);

            _logger.LogWarning(
                "Triage prompt template {PromptTemplateId} could not be rendered: {Error}",
                _options.PromptTemplateId,
                rendered.Error ?? "empty rendered content");
        }

        return AddRuntimeInstructions(BuildFallbackPrompt(groupJson));
    }

    private static string BuildFallbackPrompt(string groupJson)
        => """
           You are a bug triage agent. Research the following grouped incidental bug reports.
           Return only schema-valid JSON with fields title, summary, severity, acceptanceCriteria, and implementationNotes.
           Do not create TODOs yourself.

           Group JSON:
           """ + Environment.NewLine + groupJson;

    private static string AddRuntimeInstructions(string prompt)
    {
        if (prompt.Contains(TriageRuntimeInstructions, StringComparison.Ordinal))
            return prompt;

        return string.Concat(prompt.TrimEnd(), Environment.NewLine, Environment.NewLine, TriageRuntimeInstructions);
    }

    private static (bool Valid, ResearchOutput? Output, string? Error) ValidateResearchOutput(string? outputJson)
    {
        if (string.IsNullOrWhiteSpace(outputJson))
            return (false, null, "Triage research output failed schema validation: empty output.");

        var candidateJson = ExtractResearchOutputJson(outputJson);
        try
        {
            var output = (ResearchOutput?)JsonSerializer.Deserialize(candidateJson, typeof(ResearchOutput), McpServicesJsonContext.Default);
            if (output is null ||
                string.IsNullOrWhiteSpace(output.Title) ||
                string.IsNullOrWhiteSpace(output.Summary) ||
                output.AcceptanceCriteria is not { Count: > 0 })
            {
                return (false, null, "Triage research output failed schema validation.");
            }

            return (true, output, null);
        }
        catch (JsonException ex)
        {
            return (false, null, $"Triage research output failed schema validation: {ex.Message}");
        }
    }

    private static string ExtractResearchOutputJson(string outputJson)
    {
        var trimmed = outputJson.Trim();
        var start = trimmed.IndexOf('{', StringComparison.Ordinal);
        if (start < 0)
            return trimmed;

        var inString = false;
        var escaped = false;
        var depth = 0;
        for (var i = start; i < trimmed.Length; i++)
        {
            var current = trimmed[i];
            if (inString)
            {
                if (escaped)
                {
                    escaped = false;
                }
                else if (current == '\\')
                {
                    escaped = true;
                }
                else if (current == '"')
                {
                    inString = false;
                }

                continue;
            }

            if (current == '"')
            {
                inString = true;
            }
            else if (current == '{')
            {
                depth++;
            }
            else if (current == '}')
            {
                depth--;
                if (depth == 0)
                    return trimmed[start..(i + 1)];
            }
        }

        return trimmed;
    }

    private static bool ContainsJsonObject(string? outputJson)
        => !string.IsNullOrWhiteSpace(outputJson) && outputJson.Contains('{', StringComparison.Ordinal);

    private static ResearchOutput BuildFallbackResearchOutput(
        TriageGroupDetail detail,
        string validationError,
        string runId)
    {
        var title = FirstNonEmpty(
            detail.Title,
            detail.Reports.FirstOrDefault()?.Title,
            $"Process triage group {detail.GroupId}");
        var summary = FirstNonEmpty(
            detail.Summary,
            detail.Reports.FirstOrDefault()?.Summary,
            $"Grouped triage reports require investigation for {detail.GroupId}.");

        return new ResearchOutput
        {
            Title = title,
            Summary = summary,
            Severity = "medium",
            AcceptanceCriteria =
            [
                "Investigate and resolve the grouped triage report.",
                "Add or update validation that proves the triage issue is fixed.",
                "Verify the workflow no longer reproduces this triage report.",
            ],
            ImplementationNotes =
            [
                "Fallback TODO created because successful triage research output contained no JSON object.",
                $"Research validation error: {validationError}",
                $"Raw triage research output is preserved on run {runId}.",
            ],
        };
    }

    private static string FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;

    private static string NormalizeTodoPriority(string? severity)
    {
        if (string.Equals(severity, "critical", StringComparison.OrdinalIgnoreCase))
            return "high";
        if (string.Equals(severity, "high", StringComparison.OrdinalIgnoreCase))
            return "high";
        if (string.Equals(severity, "low", StringComparison.OrdinalIgnoreCase))
            return "low";
        return "medium";
    }

    private static IReadOnlyList<string> BuildTechnicalDetails(ResearchOutput output)
    {
        var details = new List<string>();
        details.Add("Acceptance criteria:");
        details.AddRange(output.AcceptanceCriteria.Select(item => $"- {item}"));
        if (output.ImplementationNotes is { Count: > 0 })
        {
            details.Add("Implementation notes:");
            details.AddRange(output.ImplementationNotes.Select(item => $"- {item}"));
        }

        return details;
    }

    internal sealed record ResearchOutput
    {
        public string Title { get; init; } = string.Empty;

        public string Summary { get; init; } = string.Empty;

        public string? Severity { get; init; }

        public IReadOnlyList<string> AcceptanceCriteria { get; init; } = [];

        public IReadOnlyList<string> ImplementationNotes { get; init; } = [];
    }

    private sealed record TriageTodoCreationAttempt(
        string RequestedTodoId,
        TodoMutationResult Result,
        bool CreatedWithWarning);

    private sealed record TriageSelection(
        IReadOnlyList<TriageReportEntity> Reports,
        IReadOnlyList<TriageGroupEntity> SourceGroups);
}
