/// <summary>
/// FR-MCP-107 / TR-MCP-PLAN-001: Identifies the independently validated session-life report lane.
/// </summary>
internal enum SessionLifeUnitGateScope
{
    /// <summary>
    /// FR-MCP-107 / TR-MCP-PLAN-001: The cumulative unit-test lane.
    /// </summary>
    Unit = 1,

    /// <summary>
    /// FR-MCP-107 / TR-MCP-PLAN-001: The provider and integration-test lane.
    /// </summary>
    Provider = 2,
}

/// <summary>
/// FR-MCP-107 / TR-MCP-PLAN-001: Describes one run-scoped session-life gate validation request.
/// </summary>
internal sealed class SessionLifeUnitGateValidationRequest
{
    /// <summary>
    /// FR-MCP-107 / TR-MCP-PLAN-001: Gets or initializes the report lane to validate.
    /// </summary>
    public SessionLifeUnitGateScope Scope { get; init; }

    /// <summary>
    /// FR-MCP-107 / TR-MCP-PLAN-001: Gets or initializes the unique test run identifier.
    /// </summary>
    public string RunId { get; init; } = string.Empty;

    /// <summary>
    /// FR-MCP-107 / TR-MCP-PLAN-001: Gets or initializes the absolute root containing the run artifacts.
    /// </summary>
    public string ResultsRoot { get; init; } = string.Empty;

    /// <summary>
    /// FR-MCP-107 / TR-MCP-PLAN-001: Gets or initializes the UTC instant at which the run began.
    /// </summary>
    public DateTimeOffset RunStartedAtUtc { get; init; }

    /// <summary>
    /// FR-MCP-107 / TR-MCP-PLAN-001: Gets or initializes the source and tool manifest path for the run.
    /// </summary>
    public string SourceManifestPath { get; init; } = string.Empty;
}

/// <summary>
/// FR-MCP-107 / TR-MCP-PLAN-001: Validates the machine-readable artifacts for one session-life gate lane.
/// </summary>
internal interface ISessionLifeUnitGateValidator
{
    /// <summary>
    /// FR-MCP-107 / TR-MCP-PLAN-001: Validates the artifacts bound to the supplied run request.
    /// </summary>
    /// <param name="request">The run-scoped validation request.</param>
    void Validate(SessionLifeUnitGateValidationRequest request);
}

/// <summary>
/// FR-MCP-107 / TR-MCP-PLAN-001: Represents the concrete machine-readable session-life gate validator.
/// </summary>
internal sealed class SessionLifeUnitGateValidator : ISessionLifeUnitGateValidator
{
    /// <summary>
    /// FR-MCP-107 / TR-MCP-PLAN-001: Initializes a validator with the canonical repository root and tool probe.
    /// </summary>
    /// <param name="repositoryRoot">The canonical repository root supplied by Nuke.</param>
    /// <param name="toolVersionProbe">A probe that returns the current observation for a named tool.</param>
    internal SessionLifeUnitGateValidator(
        string repositoryRoot,
        Func<string, string> toolVersionProbe)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        ArgumentNullException.ThrowIfNull(toolVersionProbe);
    }

    /// <inheritdoc />
    public void Validate(SessionLifeUnitGateValidationRequest request)
    {
        throw new NotImplementedException("Session-life report validation is intentionally absent in the behavior-Red scaffold.");
    }
}

partial class Build
{
    /// <summary>
    /// FR-MCP-107 / TR-MCP-PLAN-001: Delegates the same run-scoped request to the injected validator exactly once.
    /// </summary>
    /// <param name="validator">The validator boundary consumed by the build.</param>
    /// <param name="request">The exact request instance to validate.</param>
    internal static void ValidateSessionLifeUnitGate(
        ISessionLifeUnitGateValidator validator,
        SessionLifeUnitGateValidationRequest request)
    {
        validator.Validate(request);
    }
}
