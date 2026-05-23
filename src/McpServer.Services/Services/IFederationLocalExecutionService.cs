namespace McpServer.Support.Mcp.Services;

/// <summary>FR-MCP-103: Executes signed hub-authorized operations that must run on a LocalProxy host.</summary>
public interface IFederationLocalExecutionService
{
    /// <summary>Executes one machine-local operation after federation signature and policy checks.</summary>
    /// <param name="request">Local execution request decoded from the signed envelope body.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Execution result for acknowledgement back to the hub.</returns>
    ValueTask<FederationLocalExecutionResult> ExecuteAsync(
        FederationLocalExecutionRequest request,
        CancellationToken cancellationToken);
}
