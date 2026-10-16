namespace QBrainAi.Support.Mcp.Requirements;

/// <summary>
/// FR-MCP-REQRECOVERY-001: the idempotency key is already stored with a different payload.
/// Mapped to HTTP 409. No requirement rows are changed.
/// </summary>
public sealed class RequirementsRecoveryConflictException : InvalidOperationException
{
    /// <summary>Initializes the conflict.</summary>
    /// <param name="message">Conflict description. Must not describe the run as missing.</param>
    public RequirementsRecoveryConflictException(string message)
        : base(message)
    {
    }
}

/// <summary>
/// FR-MCP-REQRECOVERY-001: no recovery run exists for the idempotency key.
/// Mapped to HTTP 404. Dry-run does not create a run, so GET after dry-run only is not found.
/// </summary>
public sealed class RequirementsRecoveryNotFoundException : KeyNotFoundException
{
    /// <summary>Initializes the missing-run error.</summary>
    /// <param name="message">Message containing the words "not found".</param>
    public RequirementsRecoveryNotFoundException(string message)
        : base(message)
    {
    }
}
