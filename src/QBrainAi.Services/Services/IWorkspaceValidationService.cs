using QBrainAi.Support.Mcp.Models;

namespace QBrainAi.Support.Mcp.Services;

/// <summary>FR-MCP-HYGIENE-001: Read-only workspace hygiene validation. Never repairs.</summary>
public interface IWorkspaceValidationService
{
    /// <summary>Run registered hygiene rules against the current workspace.</summary>
    Task<WorkspaceValidationResult> ValidateAsync(WorkspaceValidationRequest request, CancellationToken cancellationToken = default);
}
