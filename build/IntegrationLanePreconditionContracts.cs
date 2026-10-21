/// <summary>
/// TR-MCP-NUKE-019 AC-5: whether MigrationIntegrationTests may start its test processes, and why not.
/// </summary>
/// <param name="CanRun">True when the lane may run its tests.</param>
/// <param name="FailureMessage">When <paramref name="CanRun"/> is false, the message that names the missing session; otherwise null.</param>
sealed record IntegrationLaneReadiness(bool CanRun, string? FailureMessage);

/// <summary>
/// TR-MCP-NUKE-019 AC-5: decides, before any test runs, whether the host session can run the integration lane.
/// On Windows the lane runs in an interactive desktop session of the logged-on user; elsewhere no session
/// requirement applies.
/// </summary>
interface IIntegrationLanePrecondition
{
    /// <summary>Evaluates the precondition for one host and one session.</summary>
    /// <param name="host">Host the lane runs on.</param>
    /// <param name="userInteractive">Whether the running process is in an interactive desktop session.</param>
    IntegrationLaneReadiness Evaluate(IntegrationLaneHost host, bool userInteractive);
}
