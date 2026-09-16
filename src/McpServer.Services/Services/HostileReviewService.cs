using System.Text;
using System.Text.Json;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.Services;

/// <summary>FR-MCP-HOSTILEREVIEW-001 through 006: Queue-and-record hostile review service.</summary>
public sealed class HostileReviewService : IHostileReviewService, IHostileReviewWorker
{
    private static readonly HashSet<string> AllowedVerdicts = new(StringComparer.OrdinalIgnoreCase)
    {
        "AGREE", "DISAGREE", "UNKNOWN",
    };

    private static readonly HashSet<string> AllowedCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "defect", "risk", "missing-tests", "unclear-requirements", "insufficient-disclosure", "out-of-scope", "uncertainty",
    };

    private readonly McpDbContext _db;
    private readonly string _workspaceId;
    private readonly HostileReviewWorkerOptions _options;
    private readonly TimeProvider _time;

    /// <summary>TR-MCP-HOSTILEREVIEW-001: Constructor.</summary>
    public HostileReviewService(McpDbContext db, HostileReviewWorkerOptions? options = null, TimeProvider? time = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _workspaceId = string.IsNullOrWhiteSpace(db.CurrentWorkspaceId) ? string.Empty : db.CurrentWorkspaceId;
        _options = options ?? new HostileReviewWorkerOptions();
        _options.Validate();
        _time = time ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async Task<HostileReviewResult> SubmitAsync(HostileReviewSubmitRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (!string.IsNullOrWhiteSpace(request.WorkspacePath)
            && !string.Equals(Path.GetFullPath(request.WorkspacePath), Path.GetFullPath(_workspaceId), StringComparison.OrdinalIgnoreCase)
            && !string.Equals(request.WorkspacePath, _workspaceId, StringComparison.OrdinalIgnoreCase))
        {
            return new HostileReviewResult
            {
                Success = false,
                HttpStatus = 403,
                ErrorCode = "foreign_workspace",
                Error = "The request workspace does not match the authenticated workspace.",
            };
        }

        var payload = request.SerializedPayload ?? JsonSerializer.Serialize(request);
        var payloadBytes = Encoding.UTF8.GetByteCount(payload);
        if (payloadBytes > _options.MaximumSubmitBytes)
        {
            return new HostileReviewResult
            {
                Success = false,
                HttpStatus = 400,
                ErrorCode = "payload_too_large",
                Error = "Serialized request exceeds 1,048,576 bytes.",
            };
        }

        if (request.Links.Count > _options.MaximumLinks)
        {
            return new HostileReviewResult
            {
                Success = false,
                HttpStatus = 400,
                ErrorCode = "too_many_links",
                Error = "More than 64 artifact links is rejected.",
            };
        }

        var active = await _db.HostileReviewRequests
            .CountAsync(item => item.Status == "Queued" || item.Status == "Claimed" || item.Status == "Running", cancellationToken)
            .ConfigureAwait(false);
        if (active >= _options.MaximumActiveQueue)
        {
            return new HostileReviewResult
            {
                Success = false,
                HttpStatus = 429,
                ErrorCode = "queue_full",
                Error = "Hostile review queue is at capacity.",
            };
        }

        var now = _time.GetUtcNow();
        var requestId = $"hr-{now:yyyyMMddHHmmss}-{Guid.NewGuid():N}";
        var entity = new HostileReviewRequestEntity
        {
            RequestId = requestId,
            WorkspaceId = _workspaceId,
            TargetType = string.IsNullOrWhiteSpace(request.TargetType) ? "code" : request.TargetType.Trim(),
            Mode = string.IsNullOrWhiteSpace(request.Mode) ? "adversarial" : request.Mode.Trim(),
            ScopeStatement = request.ScopeStatement ?? string.Empty,
            RequestingAgent = string.IsNullOrWhiteSpace(request.RequestingAgent) ? "unknown" : request.RequestingAgent.Trim(),
            Status = "Queued",
            CreatedUtc = now,
            UpdatedUtc = now,
        };

        foreach (var link in request.Links)
        {
            var resolution = ResolveLink(link);
            entity.Links.Add(new HostileReviewArtifactLinkEntity
            {
                RequestId = requestId,
                WorkspaceId = _workspaceId,
                ArtifactType = string.IsNullOrWhiteSpace(link.ArtifactType) ? "unknown" : link.ArtifactType.Trim(),
                ArtifactId = link.ArtifactId ?? string.Empty,
                ContentSha256 = resolution.Hash,
                Resolved = resolution.Resolved,
            });
            if (!string.IsNullOrWhiteSpace(resolution.DiagnosticCode))
            {
                entity.Diagnostics.Add(new HostileReviewDiagnosticEntity
                {
                    RequestId = requestId,
                    WorkspaceId = _workspaceId,
                    Code = resolution.DiagnosticCode,
                    Message = resolution.DiagnosticMessage ?? resolution.DiagnosticCode,
                });
            }
        }

        _db.HostileReviewRequests.Add(entity);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Map(entity);
    }

    /// <inheritdoc />
    public async Task<HostileReviewResult> StatusAsync(string requestId, CancellationToken cancellationToken = default)
    {
        var entity = await LoadAsync(requestId, cancellationToken).ConfigureAwait(false);
        if (entity is null)
            return NotFound(requestId);
        return Map(entity);
    }

    /// <inheritdoc />
    public Task<HostileReviewResult> GetAsync(string requestId, CancellationToken cancellationToken = default)
        => StatusAsync(requestId, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<HostileReviewResult>> QueryAsync(HostileReviewQueryRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var limit = request.Limit <= 0 ? 50 : Math.Min(request.Limit, 200);
        var query = _db.HostileReviewRequests
            .Include(item => item.Links)
            .Include(item => item.Executions)
            .Include(item => item.Findings)
            .Include(item => item.Diagnostics)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.RequestingAgent))
            query = query.Where(item => item.RequestingAgent == request.RequestingAgent);
        if (!string.IsNullOrWhiteSpace(request.TargetType))
            query = query.Where(item => item.TargetType == request.TargetType);
        if (!string.IsNullOrWhiteSpace(request.Model))
            query = query.Where(item => item.Executions.Any(execution => execution.Model == request.Model));
        if (!string.IsNullOrWhiteSpace(request.Effort))
            query = query.Where(item => item.Executions.Any(execution => execution.Effort == request.Effort));
        if (!string.IsNullOrWhiteSpace(request.ReviewerAgent))
            query = query.Where(item => item.Executions.Any(execution => execution.ReviewerAgent == request.ReviewerAgent));
        if (!string.IsNullOrWhiteSpace(request.ArtifactType))
            query = query.Where(item => item.Links.Any(link => link.ArtifactType == request.ArtifactType));
        if (!string.IsNullOrWhiteSpace(request.Severity))
            query = query.Where(item => item.Findings.Any(finding => finding.Severity == request.Severity));
        if (!string.IsNullOrWhiteSpace(request.Category))
            query = query.Where(item => item.Findings.Any(finding => finding.Category == request.Category));

        var rows = await query
            .OrderBy(item => item.CreatedUtc)
            .Skip(Math.Max(0, request.Offset))
            .Take(limit)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return rows.Select(Map).ToArray();
    }

    /// <inheritdoc />
    public async Task<HostileReviewResult> AcceptReviewerOutputAsync(string requestId, HostileReviewerOutput output, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(output);
        var entity = await LoadAsync(requestId, cancellationToken).ConfigureAwait(false);
        if (entity is null)
            return NotFound(requestId);

        if (!string.Equals(output.Model, HostileReviewPromptDefaults.RequiredModel, StringComparison.Ordinal)
            || !string.Equals(output.Effort, HostileReviewPromptDefaults.RequiredEffort, StringComparison.Ordinal))
        {
            entity.Status = "Failed";
            entity.LastErrorCode = "ModelUnavailable";
            entity.UpdatedUtc = _time.GetUtcNow();
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            var failed = Map(entity);
            failed.ErrorCode = "ModelUnavailable";
            failed.Success = false;
            return failed;
        }

        if (!AllowedVerdicts.Contains(output.Verdict))
        {
            entity.Status = "Failed";
            entity.LastErrorCode = "malformed_output";
            entity.UpdatedUtc = _time.GetUtcNow();
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            var failed = Map(entity);
            failed.ErrorCode = "malformed_output";
            failed.Success = false;
            return failed;
        }

        var now = _time.GetUtcNow();
        entity.Executions.Add(new HostileReviewExecutionEntity
        {
            ExecutionId = $"hrex-{Guid.NewGuid():N}",
            RequestId = entity.RequestId,
            WorkspaceId = entity.WorkspaceId,
            ReviewerAgent = output.ReviewerAgent,
            Model = output.Model,
            Effort = output.Effort,
            SourceSurface = output.SourceSurface,
            PromptTemplateId = output.PromptTemplateId,
            PromptVersion = output.PromptVersion,
            StartedUtc = now,
            EndedUtc = now,
            InputTokens = output.InputTokens,
            OutputTokens = output.OutputTokens,
        });

        foreach (var finding in output.Findings)
        {
            var category = AllowedCategories.Contains(finding.Category) ? finding.Category : "uncertainty";
            entity.Findings.Add(new HostileReviewFindingEntity
            {
                RequestId = entity.RequestId,
                WorkspaceId = entity.WorkspaceId,
                Category = category,
                Severity = string.IsNullOrWhiteSpace(finding.Severity) ? "medium" : finding.Severity,
                ArtifactId = finding.ArtifactId,
                Location = finding.Location,
                RequirementId = finding.RequirementId,
                EvidenceSummary = finding.EvidenceSummary,
                Recommendation = finding.Recommendation,
            });
        }

        entity.Verdict = output.Verdict.ToUpperInvariant();
        entity.QualityDisclosure = output.RequestQuality.Disclosure;
        entity.QualityScopeClarity = output.RequestQuality.ScopeClarity;
        entity.QualityObjectiveClarity = output.RequestQuality.ObjectiveClarity;
        entity.QualityConfidence = output.RequestQuality.Confidence;
        entity.QualityEnoughContext = output.RequestQuality.EnoughContext;
        entity.Status = "Succeeded";
        entity.UpdatedUtc = now;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Map(entity);
    }

    private async Task<HostileReviewRequestEntity?> LoadAsync(string requestId, CancellationToken cancellationToken)
        => await _db.HostileReviewRequests
            .Include(item => item.Links)
            .Include(item => item.Executions)
            .Include(item => item.Findings)
            .Include(item => item.Diagnostics)
            .FirstOrDefaultAsync(item => item.RequestId == requestId, cancellationToken)
            .ConfigureAwait(false);

    private static HostileReviewResult NotFound(string requestId)
        => new()
        {
            Success = false,
            HttpStatus = 404,
            ErrorCode = "not_found",
            Error = "Hostile review request was not found.",
            RequestId = requestId,
        };

    private (bool Resolved, string? Hash, string? DiagnosticCode, string? DiagnosticMessage) ResolveLink(HostileReviewArtifactLink link)
    {
        var type = string.IsNullOrWhiteSpace(link.ArtifactType) ? "todo" : link.ArtifactType.Trim();
        var id = link.ArtifactId ?? string.Empty;
        if (!string.Equals(type, "todo", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(id))
            return (false, null, "artifact_missing", "The artifact was not found through a supported MCP store.");

        var matches = _db.TodoItems.IgnoreQueryFilters()
            .Where(item => item.Id == id || item.Title == id)
            .ToList();
        if (matches.Count == 0)
            return (false, null, "artifact_missing", "The artifact was not found through the TODO store.");
        if (matches.Count > 1)
            return (false, null, "artifact_ambiguous", "The artifact locator matched more than one TODO.");
        var todo = matches[0];
        if (!string.Equals(todo.WorkspaceId, _workspaceId, StringComparison.OrdinalIgnoreCase))
            return (false, null, "artifact_unauthorized", "The artifact is not authorized in this workspace.");

        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(todo.Id))).ToLowerInvariant();
        return (true, hash, null, null);
    }

    private static HostileReviewResult Map(HostileReviewRequestEntity entity)
        => new()
        {
            Success = entity.HttpStatus is 0 or 200 && entity.LastErrorCode is null,
            HttpStatus = entity.HttpStatus == 0 ? 200 : entity.HttpStatus,
            RequestId = entity.RequestId,
            Status = entity.Status,
            Verdict = entity.Verdict,
            TargetType = entity.TargetType,
            RequestingAgent = entity.RequestingAgent,
            ErrorCode = entity.LastErrorCode,
            Diagnostics = entity.Diagnostics.Select(item => item.Code + ": " + item.Message).ToList(),
            Links = entity.Links.Select(link => new HostileReviewArtifactLink
            {
                ArtifactType = link.ArtifactType,
                ArtifactId = link.ArtifactId,
            }).ToList(),
            Executions = entity.Executions.Select(execution => new HostileReviewExecution
            {
                ExecutionId = execution.ExecutionId,
                ReviewerAgent = execution.ReviewerAgent,
                Model = execution.Model,
                Effort = execution.Effort,
                SourceSurface = execution.SourceSurface,
                PromptTemplateId = execution.PromptTemplateId,
                PromptVersion = execution.PromptVersion,
                StartedUtc = execution.StartedUtc,
                EndedUtc = execution.EndedUtc,
                InputTokens = execution.InputTokens,
                OutputTokens = execution.OutputTokens,
            }).ToList(),
            Findings = entity.Findings.Select(finding => new HostileReviewFinding
            {
                Category = finding.Category,
                Severity = finding.Severity,
                ArtifactId = finding.ArtifactId,
                Location = finding.Location,
                RequirementId = finding.RequirementId,
                EvidenceSummary = finding.EvidenceSummary,
                Recommendation = finding.Recommendation,
            }).ToList(),
            RequestQuality = entity.QualityDisclosure is null
                ? null
                : new HostileReviewRequestQuality
                {
                    Disclosure = entity.QualityDisclosure.Value,
                    ScopeClarity = entity.QualityScopeClarity ?? 0,
                    ObjectiveClarity = entity.QualityObjectiveClarity ?? 0,
                    Confidence = entity.QualityConfidence ?? 0,
                    EnoughContext = entity.QualityEnoughContext ?? false,
                },
        };
}
