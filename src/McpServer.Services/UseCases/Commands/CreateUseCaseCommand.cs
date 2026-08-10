using McpServer.Cqrs;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.UseCases.Models;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.UseCases.Commands;

/// <summary>
/// FR-MCP-USECASE-001 / TR-MCP-USECASE-002: Create a workspace-scoped use case with optional FR link and basic flow.
/// </summary>
/// <param name="WorkspacePath">Workspace path (overrides ambient context when set).</param>
/// <param name="Request">Create payload.</param>
public sealed record CreateUseCaseCommand(string WorkspacePath, CreateUseCaseRequest Request)
    : ICommand<UseCaseDetailDto>;

/// <summary>
/// FR-MCP-USECASE-001 / TR-MCP-USECASE-002: Handles <see cref="CreateUseCaseCommand"/>.
/// </summary>
public sealed class CreateUseCaseCommandHandler(
    McpDbContext db,
    WorkspaceContext workspaceContext)
    : ICommandHandler<CreateUseCaseCommand, UseCaseDetailDto>
{
    /// <inheritdoc />
    public async Task<Result<UseCaseDetailDto>> HandleAsync(CreateUseCaseCommand command, CallContext context)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Request);

        try
        {
            var workspaceId = UseCaseCqrsHelpers.ResolveWorkspaceId(db, workspaceContext, command.WorkspacePath);
            var title = UseCaseCqrsHelpers.ValidateTitle(command.Request.Title, out var titleError);
            if (title is null)
                return Result<UseCaseDetailDto>.Failure(titleError!);

            string? linkType = null;
            string? frId = UseCaseCqrsHelpers.NormalizeOptional(command.Request.FrId);
            if (frId is not null)
            {
                linkType = UseCaseConstants.CanonicalizeLinkType(command.Request.LinkType);
                var frError = await UseCaseCqrsHelpers.ValidateFrExistsAsync(db, workspaceId, frId, context.CancellationToken)
                    .ConfigureAwait(false);
                if (frError is not null)
                    return Result<UseCaseDetailDto>.Failure(frError);
            }

            var now = DateTimeOffset.UtcNow;
            var entity = new UseCaseEntity
            {
                WorkspaceId = workspaceId,
                Title = title,
                BriefDescription = UseCaseCqrsHelpers.NormalizeOptional(command.Request.BriefDescription),
                Precondition = UseCaseCqrsHelpers.NormalizeOptional(command.Request.Precondition),
                Postcondition = UseCaseCqrsHelpers.NormalizeOptional(command.Request.Postcondition),
                Scope = UseCaseCqrsHelpers.NormalizeOptional(command.Request.Scope),
                Priority = command.Request.Priority,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            };

            db.UseCases.Add(entity);
            await db.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);

            if (frId is not null)
            {
                db.UseCaseFrLinks.Add(new UseCaseFrLinkEntity
                {
                    WorkspaceId = workspaceId,
                    UseCaseId = entity.UseCaseId,
                    FrId = frId,
                    FrKind = UseCaseConstants.FrKind,
                    LinkType = linkType!,
                    LinkOrder = command.Request.LinkOrder,
                    Notes = UseCaseCqrsHelpers.NormalizeOptional(command.Request.Notes),
                    CreatedAtUtc = now,
                });
            }

            var initialSteps = command.Request.InitialSteps;
            var createFlow = command.Request.CreateBasicFlow || (initialSteps is { Count: > 0 });
            if (createFlow)
            {
                var flow = new UseCaseFlowEntity
                {
                    WorkspaceId = workspaceId,
                    UseCaseId = entity.UseCaseId,
                    FlowType = "Basic",
                    Name = "Basic",
                    SequenceNumber = 1,
                };
                db.UseCaseFlows.Add(flow);
                await db.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);

                if (initialSteps is { Count: > 0 })
                {
                    var stepNumber = 1;
                    foreach (var stepReq in initialSteps)
                    {
                        if (string.IsNullOrWhiteSpace(stepReq.Action))
                            return Result<UseCaseDetailDto>.Failure("Initial step Action is required.");

                        db.UseCaseSteps.Add(new UseCaseStepEntity
                        {
                            WorkspaceId = workspaceId,
                            FlowId = flow.FlowId,
                            StepNumber = stepReq.StepNumber ?? stepNumber,
                            ActorId = stepReq.ActorId,
                            Action = stepReq.Action.Trim(),
                            SystemResponse = UseCaseCqrsHelpers.NormalizeOptional(stepReq.SystemResponse),
                            DataEntities = UseCaseCqrsHelpers.NormalizeOptional(stepReq.DataEntities),
                        });
                        stepNumber++;
                    }
                }
            }

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
