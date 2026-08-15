using McpServer.Cqrs;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.UseCases.Models;
using McpServer.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.UseCases.Commands;

/// <summary>
/// FR-MCP-USECASE-001 / TR-MCP-USECASE-002: Update use case header fields only.
/// </summary>
/// <param name="WorkspacePath">Workspace path override.</param>
/// <param name="UseCaseId">Target use case id.</param>
/// <param name="Request">Header fields to update.</param>
public sealed record UpdateUseCaseCommand(string WorkspacePath, long UseCaseId, UpdateUseCaseRequest Request)
    : ICommand<UseCaseDetailDto>;

/// <summary>
/// FR-MCP-USECASE-001 / TR-MCP-USECASE-002: Handles <see cref="UpdateUseCaseCommand"/>.
/// </summary>
public sealed class UpdateUseCaseCommandHandler(
    McpDbContext db,
    WorkspaceContext workspaceContext)
    : ICommandHandler<UpdateUseCaseCommand, UseCaseDetailDto>
{
    /// <inheritdoc />
    public async Task<Result<UseCaseDetailDto>> HandleAsync(UpdateUseCaseCommand command, CallContext context)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Request);

        try
        {
            UseCaseCqrsHelpers.ResolveWorkspaceId(db, workspaceContext, command.WorkspacePath);

            var entity = await db.UseCases
                .FirstOrDefaultAsync(u => u.UseCaseId == command.UseCaseId, context.CancellationToken)
                .ConfigureAwait(false);
            if (entity is null)
                return Result<UseCaseDetailDto>.Failure($"Use case '{command.UseCaseId}' was not found.");

            if (command.Request.Title is not null)
            {
                var title = UseCaseCqrsHelpers.ValidateTitle(command.Request.Title, out var titleError);
                if (title is null)
                    return Result<UseCaseDetailDto>.Failure(titleError!);
                entity.Title = title;
            }

            if (command.Request.BriefDescription is not null)
                entity.BriefDescription = UseCaseCqrsHelpers.NormalizeOptional(command.Request.BriefDescription);
            if (command.Request.Precondition is not null)
                entity.Precondition = UseCaseCqrsHelpers.NormalizeOptional(command.Request.Precondition);
            if (command.Request.Postcondition is not null)
                entity.Postcondition = UseCaseCqrsHelpers.NormalizeOptional(command.Request.Postcondition);
            if (command.Request.Scope is not null)
                entity.Scope = UseCaseCqrsHelpers.NormalizeOptional(command.Request.Scope);
            if (command.Request.Priority is int priority)
                entity.Priority = priority;

            entity.UpdatedAtUtc = DateTimeOffset.UtcNow;
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
