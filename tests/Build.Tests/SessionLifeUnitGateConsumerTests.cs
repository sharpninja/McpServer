using NSubstitute;

namespace NukeBuild.Tests;

/// <summary>
/// FR-MCP-107 / TR-MCP-PLAN-001 / TEST-MCP-143 consumer-boundary tests for
/// the machine-readable session-life gate.
/// </summary>
public sealed class SessionLifeUnitGateConsumerTests
{
    /// <summary>
    /// Requires the Build consumer to delegate the exact request exactly once.
    /// </summary>
    [Fact]
    public void ValidateSessionLifeUnitGate_DelegatesExactRequestExactlyOnce()
    {
        var validator = Substitute.For<ISessionLifeUnitGateValidator>();
        var request = new SessionLifeUnitGateValidationRequest
        {
            Scope = SessionLifeUnitGateScope.Unit,
            RunId = "unit-red-001",
            ResultsRoot = @"C:\repo\TestResults\unit-red-001",
            RunStartedAtUtc = new DateTimeOffset(2026, 9, 28, 7, 0, 0, TimeSpan.Zero),
            SourceManifestPath = @"C:\repo\TestResults\unit-red-001\source-manifest.json",
        };

        Build.ValidateSessionLifeUnitGate(validator, request);

        validator.Received(1).Validate(
            Arg.Is<SessionLifeUnitGateValidationRequest>(actual => ReferenceEquals(actual, request)));
        Assert.Single(validator.ReceivedCalls());
    }
}
