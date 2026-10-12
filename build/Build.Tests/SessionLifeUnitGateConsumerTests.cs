using NSubstitute;

namespace NukeBuild.Tests;

/// <summary>
/// FR-MCP-107 / TR-MCP-PLAN-001 / TEST-MCP-143 consumer-boundary tests for
/// the machine-readable session-life gate. Delegation tests use NSubstitute
/// mocks and do not exercise report validation.
/// </summary>
public sealed class SessionLifeUnitGateConsumerTests
{
    /// <summary>
    /// Requires the Build consumer to delegate the exact request exactly once.
    /// Fails when <c>Validate</c> is not called, is called more than once, or
    /// receives a different request instance.
    /// </summary>
    [Fact]
    public void ValidateSessionLifeUnitGate_DelegatesExactRequestExactlyOnce()
    {
        var validator = Substitute.For<ISessionLifeUnitGateValidator>();
        var request = CreateRequest("unit-red-001");

        Build.ValidateSessionLifeUnitGate(validator, request);

        validator.Received(1).Validate(
            Arg.Is<SessionLifeUnitGateValidationRequest>(actual => ReferenceEquals(actual, request)));
        Assert.Single(validator.ReceivedCalls());
    }

    /// <summary>
    /// Requires the injected validator, and not a second unused validator, to
    /// receive the same request once. A value-equal copy must not be passed.
    /// Fails on no call, multiple calls, a different request instance, or the wrong dependency.
    /// Uses two NSubstitute mocks and one concrete request plus an equal copy.
    /// </summary>
    [Fact]
    public void ValidateSessionLifeUnitGate_RejectsMissingRepeatedCopiedOrWrongTargetCall()
    {
        var injected = Substitute.For<ISessionLifeUnitGateValidator>();
        var other = Substitute.For<ISessionLifeUnitGateValidator>();
        var request = CreateRequest("unit-red-002");
        var equalCopy = CreateRequest("unit-red-002");

        Build.ValidateSessionLifeUnitGate(injected, request);

        injected.Received(1).Validate(
            Arg.Is<SessionLifeUnitGateValidationRequest>(actual => ReferenceEquals(actual, request)));
        injected.DidNotReceive().Validate(
            Arg.Is<SessionLifeUnitGateValidationRequest>(actual => ReferenceEquals(actual, equalCopy)));
        Assert.Single(injected.ReceivedCalls());
        Assert.Empty(other.ReceivedCalls());
    }

    /// <summary>
    /// Builds one concrete five-property request. The values are fixture data only.
    /// </summary>
    /// <param name="runId">The run identifier carried by the request.</param>
    /// <returns>The request instance the consumer must forward unchanged.</returns>
    private static SessionLifeUnitGateValidationRequest CreateRequest(string runId) =>
        new()
        {
            Scope = SessionLifeUnitGateScope.Unit,
            RunId = runId,
            ResultsRoot = $@"C:\repo\TestResults\{runId}",
            RunStartedAtUtc = new DateTimeOffset(2026, 9, 28, 7, 0, 0, TimeSpan.Zero),
            SourceManifestPath = $@"C:\repo\TestResults\{runId}\source-manifest.json",
        };
}
