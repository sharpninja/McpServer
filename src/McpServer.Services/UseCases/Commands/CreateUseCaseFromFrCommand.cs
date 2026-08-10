using McpServer.Cqrs;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.UseCases.Models;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.UseCases.Commands;

/// <summary>
/// FR-MCP-USECASE-004 / TR-MCP-USECASE-002: Create a shell use case from an FR title/body with Realizes link.
/// </summary>
/// <param name="WorkspacePath">Workspace path override.</param>
/// <param name="FrId">Functional requirement id (string).</param>
public sealed record CreateUseCaseFromFrCommand(string WorkspacePath, string FrId)
    : ICommand<UseCaseDetailDto>;

/// <summary>
/// FR-MCP-USECASE-004 / TR-MCP-USECASE-002: Handles <see cref="CreateUseCaseFromFrCommand"/>.
/// </summary>
public sealed class CreateUseCaseFromFrCommandHandler(
    McpDbContext db,
    WorkspaceContext workspaceContext)
    : ICommandHandler<CreateUseCaseFromFrCommand, UseCaseDetailDto>
{
    /// <inheritdoc />
    public async Task<Result<UseCaseDetailDto>> HandleAsync(CreateUseCaseFromFrCommand command, CallContext context)
    {
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            var workspaceId = UseCaseCqrsHelpers.ResolveWorkspaceId(db, workspaceContext, command.WorkspacePath);
            var frId = UseCaseCqrsHelpers.NormalizeOptional(command.FrId);
            if (frId is null)
                return Result<UseCaseDetailDto>.Failure("FrId is required.");

            var fr = await db.Requirements
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    r => r.WorkspaceId == workspaceId
                         && r.Kind == UseCaseConstants.FrKind
                         && r.Id == frId,
                    context.CancellationToken)
                .ConfigureAwait(false);
            if (fr is null)
                return Result<UseCaseDetailDto>.Failure(
                    $"Functional requirement '{frId}' was not found (Kind must be fr).");

            var titleSource = string.IsNullOrWhiteSpace(fr.Title) ? fr.Id : fr.Title.Trim();
            if (titleSource.Length > 200)
                titleSource = titleSource[..200];

            var now = DateTimeOffset.UtcNow;
            var entity = new UseCaseEntity
            {
                WorkspaceId = workspaceId,
                Title = titleSource,
                BriefDescription = UseCaseCqrsHelpers.NormalizeOptional(fr.Body),
                Priority = 0,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            };
            db.UseCases.Add(entity);
            await db.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);

            db.UseCaseFrLinks.Add(new UseCaseFrLinkEntity
            {
                WorkspaceId = workspaceId,
                UseCaseId = entity.UseCaseId,
                FrId = frId,
                FrKind = UseCaseConstants.FrKind,
                LinkType = UseCaseConstants.DefaultLinkType,
                LinkOrder = 0,
                Notes = null,
                CreatedAtUtc = now,
            });
            await db.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);

            var loaded = await UseCaseCqrsHelpers.LoadAggregateAsync(db, entity.UseCaseId, context.CancellationToken)
                .ConfigureAwait(false);
            return Result<UseCaseDetailDto>.Success(UseCaseCqrsHelpers.ToDetailDto(loaded!));
        }
        catch (Exception ex)
        {
            return Result<UseCaseDetailDto>.Failure(ex.Message, ex);
        }
    }
}
