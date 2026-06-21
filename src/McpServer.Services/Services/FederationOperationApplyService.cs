namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-103: Applies hub fanout operations to local mutable state through
/// registered federation state adapters.
/// </summary>
public sealed class FederationOperationApplyService : IFederationOperationApplyService
{
    private readonly FederationStateAdapterRegistry _registry;

    /// <summary>Initializes a new instance of the <see cref="FederationOperationApplyService"/> class.</summary>
    /// <param name="registry">State adapter registry.</param>
    public FederationOperationApplyService(FederationStateAdapterRegistry registry)
    {
        _registry = registry;
    }

    /// <inheritdoc />
    public async ValueTask<FederationApplyResult> ApplyAsync(FederationOperationRequest operation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        if (!_registry.TryGet(operation.Domain, out var adapter))
        {
            return new FederationApplyResult
            {
                Applied = false,
                Conflict = true,
                Message = $"No federation adapter is registered for domain '{operation.Domain}'.",
            };
        }

        if (adapter.IsLocalOnly)
        {
            return new FederationApplyResult
            {
                Applied = false,
                Conflict = true,
                Message = $"Domain '{operation.Domain}' is local-only and cannot be applied from hub fanout.",
            };
        }

        var payloadJson = "{}";
        if (!string.IsNullOrWhiteSpace(operation.BodyBase64))
        {
            try
            {
                payloadJson = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(operation.BodyBase64));
            }
            catch (FormatException ex)
            {
                return new FederationApplyResult
                {
                    Applied = false,
                    Conflict = true,
                    Message = $"Operation body is not valid base64: {ex.Message}",
                };
            }
        }

        var stateOperation = new FederationStateOperation
        {
            OperationId = operation.OperationId ?? string.Empty,
            SourceOperationId = operation.SourceOperationId,
            Domain = operation.Domain,
            ResourceId = operation.ResourceId,
            GlobalWorkspaceId = operation.GlobalWorkspaceId,
            BaseVersion = operation.BaseVersion,
            PayloadJson = payloadJson,
            HttpMethod = operation.HttpMethod,
            Path = operation.Path,
            Method = operation.Method,
            HeadersJson = operation.HeadersJson,
        };
        if (adapter.IsEcho(stateOperation))
        {
            return new FederationApplyResult
            {
                Applied = false,
                AlreadyApplied = true,
                Version = await adapter.GetVersionAsync(operation.ResourceId ?? string.Empty, cancellationToken).ConfigureAwait(false),
            };
        }

        return await adapter.ApplyAsync(stateOperation, cancellationToken).ConfigureAwait(false);
    }
}
