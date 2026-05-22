namespace McpServer.Support.Mcp.Services;

/// <summary>FR-MCP-103: Applies synchronized federation operations through state adapters.</summary>
public interface IFederationOperationApplyService
{
    /// <summary>Applies one synchronized operation to local state.</summary>
    /// <param name="operation">Operation to apply.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Adapter apply result.</returns>
    ValueTask<FederationApplyResult> ApplyAsync(FederationOperationRequest operation, CancellationToken cancellationToken);
}
