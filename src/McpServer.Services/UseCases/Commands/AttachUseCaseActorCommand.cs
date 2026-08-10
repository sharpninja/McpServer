using McpServer.Cqrs;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.UseCases.Models;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.UseCases.Commands;

/// <summary>
/// FR-MCP-USECASE-002 / TR-MCP-USECASE-002: Attach an actor to a use case (create actor when needed).
/// </summary>
/// <param name="WorkspacePath">Workspace path override.</param>
/// <param name="UseCaseId">Parent use case id.</param>
/// <param name="Request">Actor attach payload.</param>
public sealed record AttachUseCaseActorCommand(
    string WorkspacePath,
    long UseCaseId,
    AttachUseCaseActorRequest Request) : ICommand<UseCaseActorDto>;

/// <summary>
/// FR-MCP-USECASE-002 / TR-MCP-USECASE-002: Handles <see cref="AttachUseCaseActorCommand"/>.
/// </summary>
public sealed class AttachUseCaseActorCommandHandler(
    McpDbContext db,
    WorkspaceContext workspaceContext)
    : ICommandHandler<AttachUseCaseActorCommand, UseCaseActorDto>
{
    /// <inheritdoc />
    public async Task<Result<UseCaseActorDto>> HandleAsync(AttachUseCaseActorCommand command, CallContext context)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Request);

        try
        {
            var workspaceId = UseCaseCqrsHelpers.ResolveWorkspaceId(db, workspaceContext, command.WorkspacePath);
            var actorType = UseCaseConstants.CanonicalizeActorType(command.Request.Type);
            if (actorType is null)
                return Result<UseCaseActorDto>.Failure("Actor Type must be Primary, Secondary, System, or External.");

            var useCase = await db.UseCases
                .FirstOrDefaultAsync(u => u.UseCaseId == command.UseCaseId, context.CancellationToken)
                .ConfigureAwait(false);
            if (useCase is null)
                return Result<UseCaseActorDto>.Failure($"Use case '{command.UseCaseId}' was not found.");

            ActorEntity actor;
            if (command.Request.ActorId is long actorId)
            {
                var existing = await db.Actors
                    .FirstOrDefaultAsync(a => a.ActorId == actorId, context.CancellationToken)
                    .ConfigureAwait(false);
                if (existing is null)
                    return Result<UseCaseActorDto>.Failure($"Actor '{actorId}' was not found.");
                actor = existing;
            }
            else
            {
                var name = UseCaseCqrsHelpers.NormalizeOptional(command.Request.Name);
                if (name is null)
                    return Result<UseCaseActorDto>.Failure("Actor Name is required when ActorId is not provided.");
                if (name.Length > 100)
                    return Result<UseCaseActorDto>.Failure("Actor Name must be 100 characters or fewer.");

                var existingByName = await db.Actors
                    .FirstOrDefaultAsync(a => a.Name == name, context.CancellationToken)
                    .ConfigureAwait(false);
                if (existingByName is not null)
                {
                    actor = existingByName;
                }
                else
                {
                    actor = new ActorEntity
                    {
                        WorkspaceId = workspaceId,
                        Name = name,
                        Description = UseCaseCqrsHelpers.NormalizeOptional(command.Request.Description),
                        Type = actorType,
                    };
                    db.Actors.Add(actor);
                    await db.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);
                }
            }

            var join = await db.UseCaseActors
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(
                    a => a.WorkspaceId == workspaceId
                         && a.UseCaseId == command.UseCaseId
                         && a.ActorId == actor.ActorId,
                    context.CancellationToken)
                .ConfigureAwait(false);

            if (join is null)
            {
                join = new UseCaseActorEntity
                {
                    WorkspaceId = workspaceId,
                    UseCaseId = command.UseCaseId,
                    ActorId = actor.ActorId,
                    IsPrimary = command.Request.IsPrimary,
                };
                db.UseCaseActors.Add(join);
            }
            else
            {
                UseCaseCqrsHelpers.ClearSoftDelete(db, join);
                join.IsPrimary = command.Request.IsPrimary;
            }

            if (command.Request.IsPrimary)
            {
                var others = await db.UseCaseActors
                    .Where(a => a.UseCaseId == command.UseCaseId && a.ActorId != actor.ActorId && a.IsPrimary)
                    .ToListAsync(context.CancellationToken)
                    .ConfigureAwait(false);
                foreach (var other in others)
                    other.IsPrimary = false;
            }

            useCase.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);

            return Result<UseCaseActorDto>.Success(new UseCaseActorDto
            {
                ActorId = actor.ActorId,
                Name = actor.Name,
                Description = actor.Description,
                Type = actor.Type,
                IsPrimary = join.IsPrimary,
            });
        }
        catch (Exception ex)
        {
            return Result<UseCaseActorDto>.Failure(ex.Message, ex);
        }
    }
}
