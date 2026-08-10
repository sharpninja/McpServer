using McpServer.Cqrs;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.UseCases.Models;
using McpServer.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.UseCases.Commands;

/// <summary>
/// FR-MCP-USECASE-008: Transition use case approval status (Draft/Submitted/Approved/Rejected).
/// Approving increments <see cref="McpServer.Support.Mcp.Storage.Entities.UseCaseEntity.VersionNumber"/> when moving from Submitted.
/// </summary>
/// <param name="WorkspacePath">Workspace path override.</param>
/// <param name="UseCaseId">Target use case id.</param>
/// <param name="Status">Target status.</param>
public sealed record SetUseCaseApprovalStatusCommand(
    string WorkspacePath,
    long UseCaseId,
    string Status) : ICommand<UseCaseDetailDto>;

/// <summary>FR-MCP-USECASE-008: Handles <see cref="SetUseCaseApprovalStatusCommand"/>.</summary>
public sealed class SetUseCaseApprovalStatusCommandHandler(
    McpDbContext db,
    WorkspaceContext workspaceContext)
    : ICommandHandler<SetUseCaseApprovalStatusCommand, UseCaseDetailDto>
{
    private static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        "Draft", "Submitted", "Approved", "Rejected",
    };

    /// <inheritdoc />
    public async Task<Result<UseCaseDetailDto>> HandleAsync(SetUseCaseApprovalStatusCommand command, CallContext context)
    {
        ArgumentNullException.ThrowIfNull(command);
        try
        {
            UseCaseCqrsHelpers.ResolveWorkspaceId(db, workspaceContext, command.WorkspacePath);
            var status = command.Status?.Trim() ?? string.Empty;
            if (!Allowed.Contains(status))
                return Result<UseCaseDetailDto>.Failure($"Invalid approval status '{command.Status}'.");

            // Canonical casing
            status = Allowed.First(s => s.Equals(status, StringComparison.OrdinalIgnoreCase));

            var entity = await UseCaseCqrsHelpers.LoadAggregateAsync(db, command.UseCaseId, context.CancellationToken)
                .ConfigureAwait(false);
            if (entity is null)
                return Result<UseCaseDetailDto>.Failure($"Use case '{command.UseCaseId}' was not found.");

            var previous = entity.ApprovalStatus;
            entity.ApprovalStatus = status;
            entity.UpdatedAtUtc = DateTimeOffset.UtcNow;
            if (status.Equals("Approved", StringComparison.OrdinalIgnoreCase) &&
                !previous.Equals("Approved", StringComparison.OrdinalIgnoreCase))
            {
                entity.VersionNumber = Math.Max(1, entity.VersionNumber) + 1;
            }

            await db.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);
            var reloaded = await UseCaseCqrsHelpers.LoadAggregateAsync(db, entity.UseCaseId, context.CancellationToken)
                .ConfigureAwait(false);
            return Result<UseCaseDetailDto>.Success(UseCaseCqrsHelpers.ToDetailDto(reloaded!));
        }
        catch (Exception ex)
        {
            return Result<UseCaseDetailDto>.Failure(ex.Message, ex);
        }
    }
}
