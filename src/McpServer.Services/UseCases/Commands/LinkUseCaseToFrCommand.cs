using McpServer.Cqrs;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.UseCases.Models;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.UseCases.Commands;

/// <summary>
/// FR-MCP-USECASE-003 / TR-MCP-USECASE-002: Link a use case to a functional requirement (string FrId).
/// Default LinkType is Realizes; validates FR Kind=fr; conflict on active duplicate.
/// </summary>
/// <param name="WorkspacePath">Workspace path override.</param>
/// <param name="UseCaseId">Use case id.</param>
/// <param name="FrId">Functional requirement id (string).</param>
/// <param name="LinkType">Optional link type (default Realizes).</param>
/// <param name="LinkOrder">Link order.</param>
/// <param name="Notes">Optional notes.</param>
public sealed record LinkUseCaseToFrCommand(
    string WorkspacePath,
    long UseCaseId,
    string FrId,
    string? LinkType,
    int LinkOrder,
    string? Notes) : ICommand<UseCaseFrLinkDto>;

/// <summary>
/// FR-MCP-USECASE-003 / TR-MCP-USECASE-002: Handles <see cref="LinkUseCaseToFrCommand"/>.
/// </summary>
public sealed class LinkUseCaseToFrCommandHandler(
    McpDbContext db,
    WorkspaceContext workspaceContext)
    : ICommandHandler<LinkUseCaseToFrCommand, UseCaseFrLinkDto>
{
    /// <inheritdoc />
    public async Task<Result<UseCaseFrLinkDto>> HandleAsync(LinkUseCaseToFrCommand command, CallContext context)
    {
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            var workspaceId = UseCaseCqrsHelpers.ResolveWorkspaceId(db, workspaceContext, command.WorkspacePath);
            var frId = UseCaseCqrsHelpers.NormalizeOptional(command.FrId);
            if (frId is null)
                return Result<UseCaseFrLinkDto>.Failure("FrId is required.");

            var useCase = await db.UseCases
                .FirstOrDefaultAsync(u => u.UseCaseId == command.UseCaseId, context.CancellationToken)
                .ConfigureAwait(false);
            if (useCase is null)
                return Result<UseCaseFrLinkDto>.Failure($"Use case '{command.UseCaseId}' was not found.");

            var frError = await UseCaseCqrsHelpers.ValidateFrExistsAsync(db, workspaceId, frId, context.CancellationToken)
                .ConfigureAwait(false);
            if (frError is not null)
                return Result<UseCaseFrLinkDto>.Failure(frError);

            var linkType = UseCaseConstants.CanonicalizeLinkType(command.LinkType);

            var existing = await db.UseCaseFrLinks
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(
                    l => l.WorkspaceId == workspaceId
                         && l.UseCaseId == command.UseCaseId
                         && l.FrId == frId,
                    context.CancellationToken)
                .ConfigureAwait(false);

            if (existing is not null)
            {
                var isDeleted = db.Entry(existing).Property("IsDeleted").CurrentValue is true;
                if (!isDeleted)
                    return Result<UseCaseFrLinkDto>.Failure(
                        $"Conflict: use case '{command.UseCaseId}' is already linked to FR '{frId}'.");

                UseCaseCqrsHelpers.ClearSoftDelete(db, existing);
                existing.LinkType = linkType;
                existing.LinkOrder = command.LinkOrder;
                existing.Notes = UseCaseCqrsHelpers.NormalizeOptional(command.Notes);
                existing.FrKind = UseCaseConstants.FrKind;
                useCase.UpdatedAtUtc = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);
                return Result<UseCaseFrLinkDto>.Success(UseCaseCqrsHelpers.ToFrLinkDto(existing));
            }

            var link = new UseCaseFrLinkEntity
            {
                WorkspaceId = workspaceId,
                UseCaseId = command.UseCaseId,
                FrId = frId,
                FrKind = UseCaseConstants.FrKind,
                LinkType = linkType,
                LinkOrder = command.LinkOrder,
                Notes = UseCaseCqrsHelpers.NormalizeOptional(command.Notes),
                CreatedAtUtc = DateTimeOffset.UtcNow,
            };
            db.UseCaseFrLinks.Add(link);
            useCase.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);

            return Result<UseCaseFrLinkDto>.Success(UseCaseCqrsHelpers.ToFrLinkDto(link));
        }
        catch (Exception ex)
        {
            return Result<UseCaseFrLinkDto>.Failure(ex.Message, ex);
        }
    }
}
