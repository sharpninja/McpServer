using QBrainAi.Cqrs.Mvvm;
using QBrainAi.Support.Mcp.Models;

namespace QBrainAi.Support.Mcp.Services;

/// <summary>FR-MCP-HYGIENE-005: Director executor that delegates to IWorkspaceValidationService.</summary>
public sealed class WorkspaceValidationDirectorExecutor : IWorkspaceValidationDirectorExecutor
{
    private readonly IWorkspaceValidationService _service;

    /// <summary>TR-MCP-HYGIENE-005: Constructor.</summary>
    public WorkspaceValidationDirectorExecutor(IWorkspaceValidationService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    /// <inheritdoc />
    public async Task<object?> ValidateAsync(double? staleTurnThresholdHours, bool authenticated, CancellationToken cancellationToken)
        => await _service.ValidateAsync(new WorkspaceValidationRequest
        {
            Authenticated = authenticated,
            StaleTurnThresholdHours = staleTurnThresholdHours,
        }, cancellationToken).ConfigureAwait(false);
}
