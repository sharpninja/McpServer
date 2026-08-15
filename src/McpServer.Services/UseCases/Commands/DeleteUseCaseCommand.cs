using McpServer.Cqrs;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.UseCases.Models;
using McpServer.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.UseCases.Commands;

/// <summary>
/// FR-MCP-USECASE-001 / TR-MCP-USECASE-002: Soft-delete a use case aggregate and its children/links.
/// </summary>
/// <param name="WorkspacePath">Workspace path override.</param>
/// <param name="UseCaseId">Target use case id.</param>
public sealed record DeleteUseCaseCommand(string WorkspacePath, long UseCaseId) : ICommand<bool>;

/// <summary>
/// FR-MCP-USECASE-001 / TR-MCP-USECASE-002: Handles <see cref="DeleteUseCaseCommand"/> via <c>DbContext.Remove</c>
/// so soft-delete shadow properties are applied by <see cref="McpDbContext"/>.
/// </summary>
public sealed class DeleteUseCaseCommandHandler(
    McpDbContext db,
    WorkspaceContext workspaceContext)
    : ICommandHandler<DeleteUseCaseCommand, bool>
{
    /// <inheritdoc />
    public async Task<Result<bool>> HandleAsync(DeleteUseCaseCommand command, CallContext context)
    {
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            UseCaseCqrsHelpers.ResolveWorkspaceId(db, workspaceContext, command.WorkspacePath);

            var entity = await db.UseCases
                .Include(u => u.UseCaseActors)
                .Include(u => u.Flows)
                .ThenInclude(f => f.Steps)
                .Include(u => u.SpecialRequirements)
                .Include(u => u.ExtensionPoints)
                .Include(u => u.FrLinks)
                .FirstOrDefaultAsync(u => u.UseCaseId == command.UseCaseId, context.CancellationToken)
                .ConfigureAwait(false);

            if (entity is null)
                return Result<bool>.Failure($"Use case '{command.UseCaseId}' was not found.");

            foreach (var step in entity.Flows.SelectMany(f => f.Steps).ToList())
                db.UseCaseSteps.Remove(step);
            foreach (var flow in entity.Flows.ToList())
                db.UseCaseFlows.Remove(flow);
            foreach (var actor in entity.UseCaseActors.ToList())
                db.UseCaseActors.Remove(actor);
            foreach (var special in entity.SpecialRequirements.ToList())
                db.UseCaseSpecialRequirements.Remove(special);
            foreach (var extension in entity.ExtensionPoints.ToList())
                db.UseCaseExtensionPoints.Remove(extension);
            foreach (var link in entity.FrLinks.ToList())
                db.UseCaseFrLinks.Remove(link);

            db.UseCases.Remove(entity);
            await db.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure(ex.Message, ex);
        }
    }
}
