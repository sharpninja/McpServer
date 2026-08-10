using McpServer.Cqrs;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.UseCases.Models;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.UseCases.Commands;

/// <summary>
/// FR-MCP-USECASE-002 / TR-MCP-USECASE-002: Add a flow to a use case.
/// </summary>
/// <param name="WorkspacePath">Workspace path override.</param>
/// <param name="UseCaseId">Parent use case id.</param>
/// <param name="Request">Flow create payload.</param>
public sealed record AddUseCaseFlowCommand(string WorkspacePath, long UseCaseId, AddUseCaseFlowRequest Request)
    : ICommand<UseCaseFlowDto>;

/// <summary>
/// FR-MCP-USECASE-002 / TR-MCP-USECASE-002: Handles <see cref="AddUseCaseFlowCommand"/>.
/// </summary>
public sealed class AddUseCaseFlowCommandHandler(
    McpDbContext db,
    WorkspaceContext workspaceContext)
    : ICommandHandler<AddUseCaseFlowCommand, UseCaseFlowDto>
{
    /// <inheritdoc />
    public async Task<Result<UseCaseFlowDto>> HandleAsync(AddUseCaseFlowCommand command, CallContext context)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Request);

        try
        {
            var workspaceId = UseCaseCqrsHelpers.ResolveWorkspaceId(db, workspaceContext, command.WorkspacePath);
            var flowType = UseCaseConstants.CanonicalizeFlowType(command.Request.FlowType);
            if (flowType is null)
                return Result<UseCaseFlowDto>.Failure("FlowType must be Basic, Alternative, or Exception.");

            var useCase = await db.UseCases
                .FirstOrDefaultAsync(u => u.UseCaseId == command.UseCaseId, context.CancellationToken)
                .ConfigureAwait(false);
            if (useCase is null)
                return Result<UseCaseFlowDto>.Failure($"Use case '{command.UseCaseId}' was not found.");

            int sequence;
            if (command.Request.SequenceNumber is int explicitSequence)
            {
                sequence = explicitSequence;
            }
            else
            {
                var max = await db.UseCaseFlows
                    .Where(f => f.UseCaseId == command.UseCaseId)
                    .Select(f => (int?)f.SequenceNumber)
                    .MaxAsync(context.CancellationToken)
                    .ConfigureAwait(false);
                sequence = (max ?? 0) + 1;
            }

            var flow = new UseCaseFlowEntity
            {
                WorkspaceId = workspaceId,
                UseCaseId = command.UseCaseId,
                FlowType = flowType,
                Name = UseCaseCqrsHelpers.NormalizeOptional(command.Request.Name),
                SequenceNumber = sequence,
            };
            db.UseCaseFlows.Add(flow);
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
