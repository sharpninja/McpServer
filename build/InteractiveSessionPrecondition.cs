/// <summary>
/// TR-MCP-NUKE-019 AC-5: production <see cref="IIntegrationLanePrecondition"/>. On Windows the integration lane runs
/// only from an interactive desktop session of the logged-on user; a WSMan, SSH or service session is refused
/// before any test runs, with a message that names the missing session. No session requirement applies elsewhere.
/// </summary>
sealed class InteractiveSessionPrecondition : IIntegrationLanePrecondition
{
    /// <inheritdoc />
    public IntegrationLaneReadiness Evaluate(IntegrationLaneHost host, bool userInteractive) =>
        host == IntegrationLaneHost.Windows && !userInteractive
            ? new IntegrationLaneReadiness(
                false,
                "MigrationIntegrationTests needs an interactive desktop session on Windows (TR-MCP-NUKE-019 AC-5): this process "
                + "is not interactive (for example a WSMan, SSH or service session). Run the target from a console in the "
                + "logged-on user's interactive desktop session.")
            : new IntegrationLaneReadiness(true, null);
}
