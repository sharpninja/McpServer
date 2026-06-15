namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-131 and TR-MCP-QUAD-004: Explicit fail-closed guard for deferred quad branches.
/// </summary>
public sealed class BrainSlotContainmentService : IBrainSlotContainmentService
{
    /// <inheritdoc />
    public BrainSlotInvokeResponse ExecuteAotReconciliation(BrainSlotDeferredRequest request)
        => Deferred("aot-reconciliation", request);

    /// <inheritdoc />
    public BrainSlotInvokeResponse ExecuteWeightUpdate(BrainSlotDeferredRequest request)
        => Deferred("weight-update", request);

    /// <inheritdoc />
    public BrainSlotInvokeResponse ExecuteFullOrchestration(BrainSlotDeferredRequest request)
        => Deferred("quad-orchestration", request);

    private static BrainSlotInvokeResponse Deferred(string operation, BrainSlotDeferredRequest request)
    {
        var now = DateTimeOffset.UtcNow;
        return new BrainSlotInvokeResponse
        {
            Status = "rejected",
            Reason = BrainSlotReasonCodes.DeferredFeatureDisabled,
            SlotId = operation,
            Role = "DeferredQuadBranch",
            StartedAtUtc = now,
            CompletedAtUtc = now,
        };
    }
}
