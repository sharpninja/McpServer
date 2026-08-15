using McpServer.Cqrs;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.UseCases.Models;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.UseCases.Commands;

/// <summary>
/// FR-MCP-USECASE-002 / TR-MCP-USECASE-002: Add a step to a use case flow.
/// </summary>
/// <param name="WorkspacePath">Workspace path override.</param>
/// <param name="UseCaseId">Parent use case id (validated for ownership).</param>
/// <param name="FlowId">Parent flow id.</param>
/// <param name="Request">Step create payload.</param>
public sealed record AddUseCaseStepCommand(
    string WorkspacePath,
    long UseCaseId,
    long FlowId,
    CreateUseCaseStepRequest Request) : ICommand<UseCaseStepDto>;

/// <summary>
/// FR-MCP-USECASE-002 / TR-MCP-USECASE-002: Handles <see cref="AddUseCaseStepCommand"/>.
/// </summary>
public sealed class AddUseCaseStepCommandHandler(
    McpDbContext db,
    WorkspaceContext workspaceContext)
    : ICommandHandler<AddUseCaseStepCommand, UseCaseStepDto>
{
    /// <inheritdoc />
    public async Task<Result<UseCaseStepDto>> HandleAsync(AddUseCaseStepCommand command, CallContext context)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Request);

        try
        {
            var workspaceId = UseCaseCqrsHelpers.ResolveWorkspaceId(db, workspaceContext, command.WorkspacePath);

            if (string.IsNullOrWhiteSpace(command.Request.Action))
                return Result<UseCaseStepDto>.Failure("Action is required.");

            var flow = await db.UseCaseFlows
                .FirstOrDefaultAsync(
                    f => f.FlowId == command.FlowId && f.UseCaseId == command.UseCaseId,
                    context.CancellationToken)
                .ConfigureAwait(false);
            if (flow is null)
                return Result<UseCaseStepDto>.Failure(
                    $"Flow '{command.FlowId}' was not found on use case '{command.UseCaseId}'.");

            if (command.Request.ActorId is long actorId)
            {
                var actorExists = await db.Actors
                    .AnyAsync(a => a.ActorId == actorId, context.CancellationToken)
                    .ConfigureAwait(false);
                if (!actorExists)
                    return Result<UseCaseStepDto>.Failure($"Actor '{actorId}' was not found.");
            }

            int stepNumber;
            if (command.Request.StepNumber is int explicitStep)
            {
                stepNumber = explicitStep;
            }
            else
            {
                var max = await db.UseCaseSteps
                    .Where(s => s.FlowId == command.FlowId)
                    .Select(s => (int?)s.StepNumber)
                    .MaxAsync(context.CancellationToken)
                    .ConfigureAwait(false);
                stepNumber = (max ?? 0) + 1;
            }

            var step = new UseCaseStepEntity
            {
                WorkspaceId = workspaceId,
                FlowId = command.FlowId,
                StepNumber = stepNumber,
                ActorId = command.Request.ActorId,
                Action = command.Request.Action.Trim(),
                SystemResponse = UseCaseCqrsHelpers.NormalizeOptional(command.Request.SystemResponse),
                DataEntities = UseCaseCqrsHelpers.NormalizeOptional(command.Request.DataEntities),
            };
            db.UseCaseSteps.Add(step);

            var useCase = await db.UseCases
                .FirstOrDefaultAsync(u => u.UseCaseId == command.UseCaseId, context.CancellationToken)
                .ConfigureAwait(false);
            if (useCase is not null)
                useCase.UpdatedAtUtc = DateTimeOffset.UtcNow;

            await db.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);

            if (step.ActorId is not null)
            {
                await db.Entry(step).Reference(s => s.Actor).LoadAsync(context.CancellationToken).ConfigureAwait(false);
            }

            return Result<UseCaseStepDto>.Success(UseCaseCqrsHelpers.ToStepDto(step));
        }
        catch (Exception ex)
        {
            return Result<UseCaseStepDto>.Failure(ex.Message, ex);
        }
    }
}
