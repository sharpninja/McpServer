using Microsoft.EntityFrameworkCore;
using QBrainAi.Cqrs;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.UseCases.Models;

namespace QBrainAi.Support.Mcp.UseCases.Commands;

/// <summary>
/// FR-MCP-USECASE-018 / TR-MCP-USECASE-020: Updates an existing actor association in place.
/// </summary>
/// <param name="WorkspacePath">Workspace path override.</param>
/// <param name="UseCaseId">Parent use case id.</param>
/// <param name="ActorId">Existing actor id.</param>
/// <param name="Request">Full replacement payload.</param>
public sealed record UpdateUseCaseActorCommand(
    string WorkspacePath,
    long UseCaseId,
    long ActorId,
    UpdateUseCaseActorRequest Request) : ICommand<UseCaseActorDto>;

/// <summary>
/// FR-MCP-USECASE-018 / TR-MCP-USECASE-020: Handles <see cref="UpdateUseCaseActorCommand"/>.
/// </summary>
public sealed class UpdateUseCaseActorCommandHandler(
    McpDbContext db,
    WorkspaceContext workspaceContext)
    : ICommandHandler<UpdateUseCaseActorCommand, UseCaseActorDto>
{
    /// <inheritdoc />
    public async Task<Result<UseCaseActorDto>> HandleAsync(UpdateUseCaseActorCommand command, CallContext context)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Request);

        try
        {
            _ = UseCaseCqrsHelpers.ResolveWorkspaceId(db, workspaceContext, command.WorkspacePath);

            var name = UseCaseCqrsHelpers.NormalizeOptional(command.Request.Name);
            if (name is null)
                return Result<UseCaseActorDto>.Failure(UseCaseResultCodes.ValidationMsg("Actor Name is required."));
            if (name.Length > 100)
                return Result<UseCaseActorDto>.Failure(UseCaseResultCodes.ValidationMsg("Actor Name must be 100 characters or fewer."));

            var actorType = UseCaseConstants.CanonicalizeActorType(command.Request.Type);
            if (actorType is null)
                return Result<UseCaseActorDto>.Failure(UseCaseResultCodes.ValidationMsg("Actor Type is invalid. Expected Primary, Secondary, System, or External."));

            var useCase = await db.UseCases
                .FirstOrDefaultAsync(u => u.UseCaseId == command.UseCaseId, context.CancellationToken)
                .ConfigureAwait(false);
            if (useCase is null)
                return Result<UseCaseActorDto>.Failure(UseCaseResultCodes.NotFoundMsg($"Use case '{command.UseCaseId}' was not found."));

            var association = await db.UseCaseActors
                .Include(a => a.Actor)
                .FirstOrDefaultAsync(
                    a => a.UseCaseId == command.UseCaseId && a.ActorId == command.ActorId,
                    context.CancellationToken)
                .ConfigureAwait(false);
            if (association is null || association.Actor is null)
            {
                return Result<UseCaseActorDto>.Failure(UseCaseResultCodes.NotFoundMsg(
                    $"Actor '{command.ActorId}' was not found on use case '{command.UseCaseId}'."));
            }

            association.Actor.Name = name;
            association.Actor.Description = UseCaseCqrsHelpers.NormalizeOptional(command.Request.Description);
            association.Actor.Type = actorType;
            association.IsPrimary = command.Request.IsPrimary;

            if (command.Request.IsPrimary)
            {
                var otherPrimaryAssociations = await db.UseCaseActors
                    .Where(a => a.UseCaseId == command.UseCaseId && a.ActorId != command.ActorId && a.IsPrimary)
                    .ToListAsync(context.CancellationToken)
                    .ConfigureAwait(false);
                foreach (var other in otherPrimaryAssociations)
                    other.IsPrimary = false;
            }

            useCase.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);

            return Result<UseCaseActorDto>.Success(new UseCaseActorDto
            {
                ActorId = association.ActorId,
                UseCaseId = association.UseCaseId,
                Name = association.Actor.Name,
                Description = association.Actor.Description,
                Type = association.Actor.Type,
                IsPrimary = association.IsPrimary,
            });
        }
        catch (Exception ex)
        {
            return Result<UseCaseActorDto>.Failure(ex.Message, ex);
        }
    }
}
