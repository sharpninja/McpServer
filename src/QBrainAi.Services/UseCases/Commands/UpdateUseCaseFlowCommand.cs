using Microsoft.EntityFrameworkCore;
using QBrainAi.Cqrs;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.UseCases.Models;

namespace QBrainAi.Support.Mcp.UseCases.Commands;

/// <summary>
/// FR-MCP-USECASE-018 / TR-MCP-USECASE-020: Updates an existing use-case flow in place.
/// </summary>
/// <param name="WorkspacePath">Workspace path override.</param>
/// <param name="UseCaseId">Parent use case id.</param>
/// <param name="FlowId">Existing flow id.</param>
/// <param name="Request">Full replacement payload.</param>
public sealed record UpdateUseCaseFlowCommand(
    string WorkspacePath,
    long UseCaseId,
    long FlowId,
    UpdateUseCaseFlowRequest Request) : ICommand<UseCaseFlowDto>;

/// <summary>
/// FR-MCP-USECASE-018 / TR-MCP-USECASE-020: Handles <see cref="UpdateUseCaseFlowCommand"/>.
/// </summary>
public sealed class UpdateUseCaseFlowCommandHandler(
    McpDbContext db,
    WorkspaceContext workspaceContext)
    : ICommandHandler<UpdateUseCaseFlowCommand, UseCaseFlowDto>
{
    /// <inheritdoc />
    public async Task<Result<UseCaseFlowDto>> HandleAsync(UpdateUseCaseFlowCommand command, CallContext context)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Request);

        try
        {
            _ = UseCaseCqrsHelpers.ResolveWorkspaceId(db, workspaceContext, command.WorkspacePath);

            var flowType = UseCaseConstants.CanonicalizeFlowType(command.Request.FlowType);
            if (flowType is null)
                return Result<UseCaseFlowDto>.Failure(UseCaseResultCodes.ValidationMsg("FlowType is invalid. Expected Basic, Alternative, or Exception."));

            var name = UseCaseCqrsHelpers.NormalizeOptional(command.Request.Name);
            if (name?.Length > 100)
                return Result<UseCaseFlowDto>.Failure(UseCaseResultCodes.ValidationMsg("Flow Name must be 100 characters or fewer."));
            if (command.Request.SequenceNumber <= 0)
                return Result<UseCaseFlowDto>.Failure(UseCaseResultCodes.ValidationMsg("SequenceNumber must be positive."));

            var useCase = await db.UseCases
                .FirstOrDefaultAsync(u => u.UseCaseId == command.UseCaseId, context.CancellationToken)
                .ConfigureAwait(false);
            if (useCase is null)
                return Result<UseCaseFlowDto>.Failure(UseCaseResultCodes.NotFoundMsg($"Use case '{command.UseCaseId}' was not found."));

            var flow = await db.UseCaseFlows
                .Include(f => f.Steps)
                .ThenInclude(s => s.Actor)
                .FirstOrDefaultAsync(
                    f => f.FlowId == command.FlowId && f.UseCaseId == command.UseCaseId,
                    context.CancellationToken)
                .ConfigureAwait(false);
            if (flow is null)
            {
                return Result<UseCaseFlowDto>.Failure(UseCaseResultCodes.NotFoundMsg(
                    $"Flow '{command.FlowId}' was not found on use case '{command.UseCaseId}'."));
            }

            flow.FlowType = flowType;
            flow.Name = name;
            flow.SequenceNumber = command.Request.SequenceNumber;
            useCase.UpdatedAtUtc = DateTimeOffset.UtcNow;

            await db.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);
            return Result<UseCaseFlowDto>.Success(UseCaseCqrsHelpers.ToFlowDto(flow));
        }
        catch (Exception ex)
        {
            return Result<UseCaseFlowDto>.Failure(ex.Message, ex);
        }
    }
}
