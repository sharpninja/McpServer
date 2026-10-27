using System.Globalization;
using Nuke.Common;
using Serilog;

partial class Build
{
    /// <summary>
    /// FR-MCP-107 / TR-MCP-PLAN-001: Writes the pre-run source and tool manifest for one session-life gate run.
    /// </summary>
    public Target CaptureSessionLifeUnitGate => _ => _
        .Description("Write TestResults/<test-run-id>/source-manifest.json before the session-life unit gate")
        .Executes(() =>
        {
            var runId = SessionLifeUnitGateReports.NormalizeRunId(TestRunId)
                ?? throw new InvalidOperationException("CaptureSessionLifeUnitGate requires --test-run-id.");
            var manifestPath = RootDirectory / "TestResults" / runId / "source-manifest.json";
            var validator = new SessionLifeUnitGateValidator(RootDirectory, SessionLifeUnitGateValidator.ObserveTool);
            validator.WriteSourceManifest(runId, manifestPath);
            Log.Information("Wrote session-life source manifest {Path}", manifestPath);
        });

    /// <summary>
    /// FR-MCP-107 / TR-MCP-PLAN-001: Validates the unit-lane artifacts through the existing gate validator.
    /// </summary>
    public Target CheckSessionLifeUnitGate => _ => _
        .Description("Validate session-life unit-lane reports for --test-run-id")
        .Executes(() =>
        {
            var runId = SessionLifeUnitGateReports.NormalizeRunId(TestRunId)
                ?? throw new InvalidOperationException("CheckSessionLifeUnitGate requires --test-run-id.");
            if (!DateTimeOffset.TryParse(
                    GateStartedAtUtc,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var started))
            {
                throw new InvalidOperationException(
                    "CheckSessionLifeUnitGate requires --gate-started-at as a UTC timestamp.");
            }

            var resultsRoot = RootDirectory / "TestResults" / runId;
            var request = new SessionLifeUnitGateValidationRequest
            {
                Scope = SessionLifeUnitGateScope.Unit,
                RunId = runId,
                ResultsRoot = resultsRoot,
                RunStartedAtUtc = started,
                SourceManifestPath = resultsRoot / "source-manifest.json",
            };
            var validator = new SessionLifeUnitGateValidator(RootDirectory, SessionLifeUnitGateValidator.ObserveTool);
            ValidateSessionLifeUnitGate(validator, request);
            Log.Information("Session-life unit gate accepted run {RunId}", runId);
        });
}
