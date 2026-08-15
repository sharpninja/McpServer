using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.UseCases.Models;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.UseCases;

/// <summary>
/// TR-MCP-USECASE-002: Shared workspace resolution, soft-delete, mapping, and aggregate load helpers
/// for use case CQRS handlers.
/// </summary>
internal static class UseCaseCqrsHelpers
{
    /// <summary>Applies optional command/query workspace path override onto the scoped DbContext.</summary>
    public static string ResolveWorkspaceId(McpDbContext db, WorkspaceContext workspaceContext, string? workspacePath)
    {
        var resolved = !string.IsNullOrWhiteSpace(workspacePath)
            ? workspacePath.Trim()
            : workspaceContext.WorkspacePath;

        if (string.IsNullOrWhiteSpace(resolved))
            throw new InvalidOperationException("Workspace path is required for use case operations.");

        if (!string.Equals(db.CurrentWorkspaceId, resolved, StringComparison.OrdinalIgnoreCase))
            db.OverrideWorkspaceId(resolved);

        return resolved;
    }

    /// <summary>Clears soft-delete shadow properties so a durable row becomes visible again.</summary>
    public static void ClearSoftDelete(McpDbContext db, object entity)
    {
        var entry = db.Entry(entity);
        if (entry.Metadata.FindProperty("IsDeleted") is null)
            return;

        entry.State = EntityState.Modified;
        entry.Property("IsDeleted").CurrentValue = false;
        entry.Property("DeletedAtUtc").CurrentValue = null;
        entry.Property("DeletedBy").CurrentValue = null;
        entry.Property("DeleteReason").CurrentValue = null;
    }

    /// <summary>Loads a non-deleted use case aggregate with children, or null when not found.</summary>
    public static async Task<UseCaseEntity?> LoadAggregateAsync(
        McpDbContext db,
        long useCaseId,
        CancellationToken cancellationToken)
    {
        return await db.UseCases
            .Include(u => u.UseCaseActors)
            .ThenInclude(a => a.Actor)
            .Include(u => u.Flows)
            .ThenInclude(f => f.Steps)
            .ThenInclude(s => s.Actor)
            .Include(u => u.SpecialRequirements)
            .Include(u => u.ExtensionPoints)
            .Include(u => u.FrLinks)
            .FirstOrDefaultAsync(u => u.UseCaseId == useCaseId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Maps an entity aggregate to <see cref="UseCaseDetailDto"/>.</summary>
    public static UseCaseDetailDto ToDetailDto(UseCaseEntity entity)
    {
        var actors = entity.UseCaseActors
            .Where(a => a.Actor is not null)
            .OrderByDescending(a => a.IsPrimary)
            .ThenBy(a => a.Actor.Name, StringComparer.Ordinal)
            .Select(a => new UseCaseActorDto
            {
                ActorId = a.ActorId,
                Name = a.Actor.Name,
                Description = a.Actor.Description,
                Type = a.Actor.Type,
                IsPrimary = a.IsPrimary,
            })
            .ToList();

        var flows = entity.Flows
            .OrderBy(f => f.SequenceNumber)
            .ThenBy(f => f.FlowId)
            .Select(f => new UseCaseFlowDto
            {
                FlowId = f.FlowId,
                UseCaseId = f.UseCaseId,
                FlowType = f.FlowType,
                Name = f.Name,
                SequenceNumber = f.SequenceNumber,
                Steps = f.Steps
                    .OrderBy(s => s.StepNumber)
                    .ThenBy(s => s.StepId)
                    .Select(s => new UseCaseStepDto
                    {
                        StepId = s.StepId,
                        FlowId = s.FlowId,
                        StepNumber = s.StepNumber,
                        ActorId = s.ActorId,
                        ActorName = s.Actor?.Name,
                        Action = s.Action,
                        SystemResponse = s.SystemResponse,
                        DataEntities = s.DataEntities,
                    })
                    .ToList(),
            })
            .ToList();

        return new UseCaseDetailDto
        {
            UseCaseId = entity.UseCaseId,
            WorkspaceId = entity.WorkspaceId,
            Title = entity.Title,
            BriefDescription = entity.BriefDescription,
            Precondition = entity.Precondition,
            Postcondition = entity.Postcondition,
            Scope = entity.Scope,
            Priority = entity.Priority,
            VersionNumber = entity.VersionNumber,
            ApprovalStatus = entity.ApprovalStatus,
            ProductKey = entity.ProductKey,
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc,
            Actors = actors,
            Flows = flows,
            SpecialRequirements = entity.SpecialRequirements
                .OrderBy(s => s.SpecialReqId)
                .Select(s => new UseCaseSpecialRequirementDto
                {
                    SpecialReqId = s.SpecialReqId,
                    Category = s.Category,
                    RequirementText = s.RequirementText,
                    Priority = s.Priority,
                })
                .ToList(),
            ExtensionPoints = entity.ExtensionPoints
                .OrderBy(e => e.ExtensionPointId)
                .Select(e => new UseCaseExtensionPointDto
                {
                    ExtensionPointId = e.ExtensionPointId,
                    Name = e.Name,
                    Description = e.Description,
                })
                .ToList(),
            FrLinks = entity.FrLinks
                .OrderBy(l => l.LinkOrder)
                .ThenBy(l => l.LinkId)
                .Select(ToFrLinkDto)
                .ToList(),
        };
    }

    /// <summary>Maps a use case entity to a summary DTO.</summary>
    public static UseCaseSummaryDto ToSummaryDto(UseCaseEntity entity, int frLinkCount = 0)
        => new()
        {
            UseCaseId = entity.UseCaseId,
            Title = entity.Title,
            BriefDescription = entity.BriefDescription,
            Scope = entity.Scope,
            Priority = entity.Priority,
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc,
            FrLinkCount = frLinkCount,
        };

    /// <summary>Maps an FR link entity to a DTO.</summary>
    public static UseCaseFrLinkDto ToFrLinkDto(UseCaseFrLinkEntity link)
        => new()
        {
            LinkId = link.LinkId,
            UseCaseId = link.UseCaseId,
            FrId = link.FrId,
            LinkType = link.LinkType,
            LinkOrder = link.LinkOrder,
            Notes = link.Notes,
            CreatedAtUtc = link.CreatedAtUtc,
        };

    /// <summary>Maps a flow entity to a DTO (without reordering children).</summary>
    public static UseCaseFlowDto ToFlowDto(UseCaseFlowEntity flow)
        => new()
        {
            FlowId = flow.FlowId,
            UseCaseId = flow.UseCaseId,
            FlowType = flow.FlowType,
            Name = flow.Name,
            SequenceNumber = flow.SequenceNumber,
            Steps = flow.Steps
                .OrderBy(s => s.StepNumber)
                .ThenBy(s => s.StepId)
                .Select(s => new UseCaseStepDto
                {
                    StepId = s.StepId,
                    FlowId = s.FlowId,
                    StepNumber = s.StepNumber,
                    ActorId = s.ActorId,
                    ActorName = s.Actor?.Name,
                    Action = s.Action,
                    SystemResponse = s.SystemResponse,
                    DataEntities = s.DataEntities,
                })
                .ToList(),
        };

    /// <summary>Maps a step entity to a DTO.</summary>
    public static UseCaseStepDto ToStepDto(UseCaseStepEntity step)
        => new()
        {
            StepId = step.StepId,
            FlowId = step.FlowId,
            StepNumber = step.StepNumber,
            ActorId = step.ActorId,
            ActorName = step.Actor?.Name,
            Action = step.Action,
            SystemResponse = step.SystemResponse,
            DataEntities = step.DataEntities,
        };

    /// <summary>
    /// Ensures a functional requirement (kind fr) exists for the workspace; returns null error message when valid.
    /// </summary>
    public static async Task<string?> ValidateFrExistsAsync(
        McpDbContext db,
        string workspaceId,
        string frId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(frId))
            return "FrId is required.";

        var exists = await db.Requirements
            .AsNoTracking()
            .AnyAsync(
                r => r.WorkspaceId == workspaceId
                     && r.Kind == UseCaseConstants.FrKind
                     && r.Id == frId.Trim(),
                cancellationToken)
            .ConfigureAwait(false);

        return exists
            ? null
            : $"Functional requirement '{frId.Trim()}' was not found (Kind must be fr).";
    }

    /// <summary>Normalizes optional free-text fields (empty becomes null).</summary>
    public static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Validates and trims a required title.</summary>
    public static string? ValidateTitle(string? title, out string? error)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            error = "Title is required.";
            return null;
        }

        var trimmed = title.Trim();
        if (trimmed.Length > 200)
        {
            error = "Title must be 200 characters or fewer.";
            return null;
        }

        error = null;
        return trimmed;
    }
}
