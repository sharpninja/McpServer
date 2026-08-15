using McpServer.Cqrs;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.UseCases.Models;
using McpServer.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.UseCases.Commands;

/// <summary>
/// FR-MCP-USECASE-003 / TR-MCP-USECASE-002: Soft-delete a use case to FR link.
/// </summary>
/// <param name="WorkspacePath">Workspace path override.</param>
/// <param name="UseCaseId">Use case id.</param>
/// <param name="FrId">Functional requirement id (string).</param>
public sealed record UnlinkUseCaseFromFrCommand(string WorkspacePath, long UseCaseId, string FrId)
    : ICommand<bool>;

/// <summary>
/// FR-MCP-USECASE-003 / TR-MCP-USECASE-002: Handles <see cref="UnlinkUseCaseFromFrCommand"/>.
/// </summary>
public sealed class UnlinkUseCaseFromFrCommandHandler(
    McpDbContext db,
    WorkspaceContext workspaceContext)
    : ICommandHandler<UnlinkUseCaseFromFrCommand, bool>
{
    /// <inheritdoc />
    public async Task<Result<bool>> HandleAsync(UnlinkUseCaseFromFrCommand command, CallContext context)
    {
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            UseCaseCqrsHelpers.ResolveWorkspaceId(db, workspaceContext, command.WorkspacePath);
            var frId = UseCaseCqrsHelpers.NormalizeOptional(command.FrId);
            if (frId is null)
                return Result<bool>.Failure("FrId is required.");

            var link = await db.UseCaseFrLinks
                .FirstOrDefaultAsync(
                    l => l.UseCaseId == command.UseCaseId && l.FrId == frId,
                    context.CancellationToken)
                .ConfigureAwait(false);
            if (link is null)
                return Result<bool>.Failure(
                    $"Link from use case '{command.UseCaseId}' to FR '{frId}' was not found.");

            db.UseCaseFrLinks.Remove(link);

            var useCase = await db.UseCases
                .FirstOrDefaultAsync(u => u.UseCaseId == command.UseCaseId, context.CancellationToken)
                .ConfigureAwait(false);
            if (useCase is not null)
                useCase.UpdatedAtUtc = DateTimeOffset.UtcNow;

            await db.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure(ex.Message, ex);
        }
    }
}
