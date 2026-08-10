using System.Text.Json;
using McpServer.Cqrs;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.UseCases.Models;
using McpServer.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.UseCases.Commands;

/// <summary>
/// FR-MCP-USECASE-012 / TR-MCP-USECASE-012: Replace the UML diagram graph for a use case.
/// </summary>
/// <param name="WorkspacePath">Workspace path override.</param>
/// <param name="UseCaseId">Target use case id.</param>
/// <param name="Graph">Graph payload (schema v1).</param>
public sealed record PutUseCaseDiagramGraphCommand(
    string WorkspacePath,
    long UseCaseId,
    UseCaseDiagramGraphDto Graph) : ICommand<UseCaseDiagramGraphDto>;

/// <summary>
/// FR-MCP-USECASE-012 / AC-012-2 / AC-012-5 / AC-012-6: Handles graph put with validation and audit via SaveChanges.
/// </summary>
public sealed class PutUseCaseDiagramGraphCommandHandler(
    McpDbContext db,
    WorkspaceContext workspaceContext)
    : ICommandHandler<PutUseCaseDiagramGraphCommand, UseCaseDiagramGraphDto>
{
    private static readonly HashSet<string> AllowedNodeTypes = new(StringComparer.OrdinalIgnoreCase) { "actor", "usecase" };
    private static readonly HashSet<string> AllowedEdgeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "association", "include", "extend", "generalization",
    };

    /// <inheritdoc />
    public async Task<Result<UseCaseDiagramGraphDto>> HandleAsync(
        PutUseCaseDiagramGraphCommand command,
        CallContext context)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Graph);

        try
        {
            UseCaseCqrsHelpers.ResolveWorkspaceId(db, workspaceContext, command.WorkspacePath);

            var entity = await db.UseCases
                .FirstOrDefaultAsync(u => u.UseCaseId == command.UseCaseId, context.CancellationToken)
                .ConfigureAwait(false);
            if (entity is null)
                return Result<UseCaseDiagramGraphDto>.Failure($"Use case '{command.UseCaseId}' was not found.");

            var validationError = ValidateGraph(command.Graph);
            if (validationError is not null)
                return Result<UseCaseDiagramGraphDto>.Failure(validationError);

            var normalized = NormalizeGraph(command.Graph);
            entity.DiagramGraphJson = JsonSerializer.Serialize(normalized);
            entity.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);

            return Result<UseCaseDiagramGraphDto>.Success(normalized);
        }
        catch (Exception ex)
        {
            return Result<UseCaseDiagramGraphDto>.Failure(ex.Message, ex);
        }
    }

    private static string? ValidateGraph(UseCaseDiagramGraphDto graph)
    {
        if (graph.SchemaVersion != 0 && graph.SchemaVersion != 1)
            return "SchemaVersion must be 1.";

        var nodes = graph.Nodes ?? [];
        var edges = graph.Edges ?? [];
        var ids = new HashSet<string>(StringComparer.Ordinal);

        foreach (var node in nodes)
        {
            if (string.IsNullOrWhiteSpace(node.Id))
                return "Node Id is required.";
            if (!AllowedNodeTypes.Contains(node.Type ?? string.Empty))
                return $"Unknown node type '{node.Type}'.";
            if (!ids.Add(node.Id))
                return $"Duplicate node id '{node.Id}'.";
        }

        foreach (var edge in edges)
        {
            if (string.IsNullOrWhiteSpace(edge.Id))
                return "Edge Id is required.";
            if (!AllowedEdgeTypes.Contains(edge.Type ?? string.Empty))
                return $"Unknown edge type '{edge.Type}'.";
            if (string.IsNullOrWhiteSpace(edge.Source) || string.IsNullOrWhiteSpace(edge.Target))
                return "Edge Source and Target are required.";
            if (!ids.Contains(edge.Source) || !ids.Contains(edge.Target))
                return $"Edge '{edge.Id}' references missing node.";
        }

        return null;
    }

    private static UseCaseDiagramGraphDto NormalizeGraph(UseCaseDiagramGraphDto graph)
        => new()
        {
            SchemaVersion = 1,
            Kind = string.IsNullOrWhiteSpace(graph.Kind) ? "uml-usecase" : graph.Kind.Trim(),
            SystemBoundary = graph.SystemBoundary,
            Nodes = (graph.Nodes ?? []).OrderBy(n => n.Id, StringComparer.Ordinal).ToList(),
            Edges = (graph.Edges ?? []).OrderBy(e => e.Id, StringComparer.Ordinal).ToList(),
        };
}
