using McpServer.Cqrs;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.UseCases.Models;
using McpServer.Support.Mcp.Storage;

namespace McpServer.Support.Mcp.UseCases.Commands;

/// <summary>
/// FR-MCP-USECASE-009: Assign or clear product membership key for multi-workspace sharing hooks
/// (aligns with MCP-PRODUCTS-001 without requiring a full products subsystem).
/// </summary>
/// <param name="WorkspacePath">Workspace path override.</param>
/// <param name="UseCaseId">Target use case id.</param>
/// <param name="ProductKey">Product key, or null/empty to clear.</param>
public sealed record SetUseCaseProductKeyCommand(
    string WorkspacePath,
    long UseCaseId,
    string? ProductKey) : ICommand<UseCaseDetailDto>;

/// <summary>FR-MCP-USECASE-009: Handles <see cref="SetUseCaseProductKeyCommand"/>.</summary>
public sealed class SetUseCaseProductKeyCommandHandler(
    McpDbContext db,
    WorkspaceContext workspaceContext)
    : ICommandHandler<SetUseCaseProductKeyCommand, UseCaseDetailDto>
{
    /// <inheritdoc />
    public async Task<Result<UseCaseDetailDto>> HandleAsync(SetUseCaseProductKeyCommand command, CallContext context)
    {
        ArgumentNullException.ThrowIfNull(command);
        try
        {
            UseCaseCqrsHelpers.ResolveWorkspaceId(db, workspaceContext, command.WorkspacePath);
            var entity = await UseCaseCqrsHelpers.LoadAggregateAsync(db, command.UseCaseId, context.CancellationToken)
                .ConfigureAwait(false);
            if (entity is null)
                return Result<UseCaseDetailDto>.Failure($"Use case '{command.UseCaseId}' was not found.");

            entity.ProductKey = string.IsNullOrWhiteSpace(command.ProductKey) ? null : command.ProductKey.Trim();
            entity.UpdatedAtUtc = DateTimeOffset.UtcNow;
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
