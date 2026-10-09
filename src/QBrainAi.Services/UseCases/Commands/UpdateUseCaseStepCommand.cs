using Microsoft.EntityFrameworkCore;
using QBrainAi.Cqrs;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.UseCases.Models;

namespace QBrainAi.Support.Mcp.UseCases.Commands;

/// <summary>
/// FR-MCP-USECASE-018 / TR-MCP-USECASE-020: Updates an existing use-case step in place.
/// </summary>
/// <param name="WorkspacePath">Workspace path override.</param>
/// <param name="UseCaseId">Parent use case id.</param>
/// <param name="FlowId">Parent flow id.</param>
/// <param name="StepId">Existing step id.</param>
/// <param name="Request">Full replacement payload.</param>
public sealed record UpdateUseCaseStepCommand(
    string WorkspacePath,
    long UseCaseId,
    long FlowId,
    long StepId,
    UpdateUseCaseStepRequest Request) : ICommand<UseCaseStepDto>;

/// <summary>
/// FR-MCP-USECASE-018 / TR-MCP-USECASE-020: Handles <see cref="UpdateUseCaseStepCommand"/>.
/// </summary>
public sealed class UpdateUseCaseStepCommandHandler(
    McpDbContext db,
    WorkspaceContext workspaceContext)
    : ICommandHandler<UpdateUseCaseStepCommand, UseCaseStepDto>
{
    /// <inheritdoc />
    public async Task<Result<UseCaseStepDto>> HandleAsync(UpdateUseCaseStepCommand command, CallContext context)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Request);

        try
        {
            _ = UseCaseCqrsHelpers.ResolveWorkspaceId(db, workspaceContext, command.WorkspacePath);

            if (command.Request.StepNumber <= 0)
                return Result<UseCaseStepDto>.Failure(UseCaseResultCodes.ValidationMsg("StepNumber must be positive."));
            if (string.IsNullOrWhiteSpace(command.Request.Action))
                return Result<UseCaseStepDto>.Failure(UseCaseResultCodes.ValidationMsg("Action is required."));

            var useCase = await db.UseCases
                .FirstOrDefaultAsync(u => u.UseCaseId == command.UseCaseId, context.CancellationToken)
                .ConfigureAwait(false);
            if (useCase is null)
                return Result<UseCaseStepDto>.Failure(UseCaseResultCodes.NotFoundMsg($"Use case '{command.UseCaseId}' was not found."));

            var flowExists = await db.UseCaseFlows
                .AnyAsync(
                    f => f.FlowId == command.FlowId && f.UseCaseId == command.UseCaseId,
                    context.CancellationToken)
                .ConfigureAwait(false);
            if (!flowExists)
            {
                return Result<UseCaseStepDto>.Failure(UseCaseResultCodes.NotFoundMsg(
                    $"Flow '{command.FlowId}' was not found on use case '{command.UseCaseId}'."));
            }

            var step = await db.UseCaseSteps
                .FirstOrDefaultAsync(
                    s => s.StepId == command.StepId && s.FlowId == command.FlowId,
                    context.CancellationToken)
                .ConfigureAwait(false);
            if (step is null)
            {
                return Result<UseCaseStepDto>.Failure(UseCaseResultCodes.NotFoundMsg(
                    $"Step '{command.StepId}' was not found on flow '{command.FlowId}'."));
            }

            if (command.Request.ActorId is long actorId)
            {
                var actorAssociated = await db.UseCaseActors
                    .AnyAsync(
                        a => a.UseCaseId == command.UseCaseId && a.ActorId == actorId,
                        context.CancellationToken)
                    .ConfigureAwait(false);
                if (!actorAssociated)
                {
                    return Result<UseCaseStepDto>.Failure(UseCaseResultCodes.NotFoundMsg(
                        $"Actor '{actorId}' was not found on use case '{command.UseCaseId}'."));
                }
            }

            step.StepNumber = command.Request.StepNumber;
            step.ActorId = command.Request.ActorId;
            step.Action = command.Request.Action.Trim();
            step.SystemResponse = UseCaseCqrsHelpers.NormalizeOptional(command.Request.SystemResponse);
            step.DataEntities = UseCaseCqrsHelpers.NormalizeOptional(command.Request.DataEntities);
            useCase.UpdatedAtUtc = DateTimeOffset.UtcNow;

            await db.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);

            if (step.ActorId is not null)
            {
                await db.Entry(step)
                    .Reference(s => s.Actor)
                    .LoadAsync(context.CancellationToken)
                    .ConfigureAwait(false);
            }

            return Result<UseCaseStepDto>.Success(UseCaseCqrsHelpers.ToStepDto(step));
        }
        catch (Exception ex)
        {
            return Result<UseCaseStepDto>.Failure(ex.Message, ex);
        }
    }
}
