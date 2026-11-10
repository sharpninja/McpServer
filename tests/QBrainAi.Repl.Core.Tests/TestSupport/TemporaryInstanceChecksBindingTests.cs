using QBrainAi.TestSupport.TemporaryInstance;
using Xunit;

namespace QBrainAi.Repl.Core.Tests.TestSupport;

/// <summary>
/// Binding of the temporary-instance contract in <see cref="TemporaryInstanceChecksTests"/>: the shared checks and the
/// shared REPL consumer satisfy the same rows the scripted references passed and the wrong checks failed. Start and
/// health assertions prove FR-MCP-TESTRESOURCE-001 ac-4 and ac-12; restart assertions prove retained plan design
/// (TR-MCP-NUKE-019), with no current criterion. Strings only: no file system, network or process environment.
/// </summary>
/// <remarks>
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-02</term><description>FR-MCP-TESTRESOURCE-001 ac-4 and ac-12: the two binding facts moved here unchanged from TemporaryInstanceChecksTests, so the contract class can run as a mock gate before the shared checks change (BDPv4; hostile validation findings R9-F2 and the sibling of R9-F3; operator 2026-11-02: "Strengthen 5 ACs, fix all 8 (Recommended)"). Their test names change class.</description></item>
/// </list>
/// </remarks>
public sealed class TemporaryInstanceChecksBindingTests
{
    /// <summary>
    /// Binding: the shared checks, <see cref="TemporaryInstanceChecks.CheckReplStartArguments"/>,
    /// <see cref="TemporaryInstanceChecks.CheckHealthAnswer"/> and
    /// <see cref="TemporaryInstanceChecks.ShouldRestartOnNewPort"/> (TR-MCP-NUKE-019), satisfy the same rows the
    /// scripted references passed and the wrong checks failed. Start and health assertions prove FR-MCP-TESTRESOURCE-001 ac-4 and ac-12; restart assertions prove retained plan design, with no current criterion.
    /// </summary>
    [Fact]
    public void SharedChecks_SatisfyTheContract()
    {
        Assert.Empty(TemporaryInstanceChecksTests.StartContractFailures(TemporaryInstanceChecks.CheckReplStartArguments));
        Assert.Empty(TemporaryInstanceChecksTests.HealthContractFailures(TemporaryInstanceChecks.CheckHealthAnswer));
        Assert.Empty(TemporaryInstanceChecksTests.RestartContractFailures(TemporaryInstanceChecks.ShouldRestartOnNewPort));
    }

    /// <summary>
    /// FR-MCP-TESTRESOURCE-001 ac-4: runs every original workspace/marker case through the shared REPL consumer
    /// used by both integration starters. The consumer returns a plan only for accepted rows and propagates the
    /// shared guard's refusal for the foreign, missing and escaping inputs; this binds a consumer, not the check again.
    /// </summary>
    [Fact]
    public void TemporaryInstanceConsumer_UsesExplicitOwnedWorkspaceAndMarker() =>
        OwnedInstanceIsolationBindingTests.AssertOriginalConsumerRows();
}
