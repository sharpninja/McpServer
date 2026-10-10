namespace McpServer.Support.Mcp.Requirements;

/// <summary>
/// FR-MCP-REQRECOVERY-001 / TR-MCP-REQRECOVERY-001: atomic requirements recovery.
/// Dry-run writes nothing. Apply commits every requirement row and the run row in one serializable transaction.
/// </summary>
public interface IRequirementsRecoveryService
{
    /// <summary>Validates the payload and returns create versus update actions without writing.</summary>
    /// <param name="request">Recovery payload.</param>
    /// <param name="cancellationToken">Caller cancellation.</param>
    /// <returns>A planned result. No run row is stored.</returns>
    Task<RequirementsRecoveryResult> PlanAsync(RequirementsRecoveryRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies the payload once. A matching stored hash returns the stored result.
    /// A different payload for the same key fails without changing rows.
    /// </summary>
    /// <param name="request">Recovery payload.</param>
    /// <param name="cancellationToken">Caller cancellation.</param>
    /// <returns>The applied or replayed result.</returns>
    Task<RequirementsRecoveryResult> ApplyAsync(RequirementsRecoveryRequest request, CancellationToken cancellationToken = default);

    /// <summary>Loads a previously applied run by idempotency key.</summary>
    /// <param name="idempotencyKey">The key stored with the run.</param>
    /// <param name="cancellationToken">Caller cancellation.</param>
    /// <returns>The stored result.</returns>
    Task<RequirementsRecoveryResult> GetAsync(string idempotencyKey, CancellationToken cancellationToken = default);
}
