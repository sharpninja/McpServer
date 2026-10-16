namespace QBrainAi.TransactionSecurity.IntegrationTests;

/// <summary>
/// TEST-MCP-159, TEST-MCP-160, TEST-MCP-ACID-001, TEST-MCP-ACID-002 and TEST-MCP-ACID-006 (FR-MCP-173 ac-2; PLAN-SESSIONTRIAGEFIX-001 A12,
/// triage-report-ff1be8ee): the operation the TransactionSecurity and Acid integration tests drive the turn transaction coordinator with,
/// and the assertion that the coordinator gates it. One definition serves both projects: QBrainAi.Acid.IntegrationTests compiles this file
/// as a link.
/// </summary>
internal static class TransactionOperationScope
{
    /// <summary>The QuadBrain brain-slot invocation, an operation FR-MCP-173 ac-2 keeps coordinator-gated.</summary>
    public const string BrainSlotInvoke = "brain-slot.invoke";

    /// <summary>
    /// Asserts that <paramref name="operationName"/> reaches the coordinator: <see cref="TurnTransactionKeyserverScope"/> requires the
    /// keyserver for it, so a TransactionGated adapter with a coordinator does not route around it (FR-MCP-173; TR-MCP-TXNKEY-001).
    /// </summary>
    /// <param name="operationName">The operation a test sends to the coordinator.</param>
    public static void AssertCoordinatorGated(string? operationName)
        => Assert.True(
            TurnTransactionKeyserverScope.RequiresKeyserver(publisherPartyId: null, operationName),
            $"Operation '{operationName}' is routed around the coordinator (FR-MCP-173 ac-1), so it cannot exercise the transaction lifecycle.");
}
