namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-019 (AC-5): contract scenarios and assertions for <see cref="IIntegrationLanePrecondition"/>. The
/// expected outcomes come from the criterion: on Windows the lane runs only from an interactive desktop session
/// and otherwise fails before any test, naming the missing session; on Linux no session requirement applies. The
/// same assertions run against the scripted precondition (BDPv4 mock gate), wrong preconditions (negative
/// controls), and the production precondition.
/// </summary>
internal static class IntegrationLanePreconditionContract
{
    /// <summary>Every scenario name, in the order the contract checks them.</summary>
    internal static readonly string[] ScenarioNames =
    [
        "windows-interactive",
        "windows-noninteractive",
        "linux-interactive",
        "linux-noninteractive",
    ];

    /// <summary>Asserts that <paramref name="precondition"/> meets the named scenario.</summary>
    /// <param name="scenario">Scenario name from <see cref="ScenarioNames"/>.</param>
    /// <param name="precondition">Precondition under test.</param>
    internal static void AssertScenario(string scenario, IIntegrationLanePrecondition precondition)
    {
        var (host, interactive, mayRun) = scenario switch
        {
            "windows-interactive" => (IntegrationLaneHost.Windows, true, true),
            "windows-noninteractive" => (IntegrationLaneHost.Windows, false, false),
            "linux-interactive" => (IntegrationLaneHost.Linux, true, true),
            "linux-noninteractive" => (IntegrationLaneHost.Linux, false, true),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, "Unknown contract scenario."),
        };

        var readiness = precondition.Evaluate(host, interactive);

        Assert.Equal(mayRun, readiness.CanRun);
        if (mayRun)
        {
            Assert.Null(readiness.FailureMessage);
            return;
        }

        Assert.False(string.IsNullOrWhiteSpace(readiness.FailureMessage), "A lane that cannot run must say why instead of stopping silently.");
        Assert.Contains("interactive desktop session", readiness.FailureMessage, StringComparison.Ordinal);
        Assert.Contains("MigrationIntegrationTests", readiness.FailureMessage, StringComparison.Ordinal);
    }
}
