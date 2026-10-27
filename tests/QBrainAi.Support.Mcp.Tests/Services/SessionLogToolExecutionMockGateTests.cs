// SPDX-License-Identifier: Apache-2.0
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>TEST-MCP-AUDITACTION-001: keep the independent assertion-discrimination gate in cumulative regressions.</summary>
public sealed class SessionLogToolExecutionMockGateTests
{
    /// <summary>All27 preserved scenarios and14 corrupted dependency paths execute the same original assertions.</summary>
    [Fact]
    public async Task IndependentModel_PassesEveryOriginalScenarioAndRejectsDamagedEffects()
    {
        var result = await SessionLogToolExecutionMockGate.RunAsync().ConfigureAwait(true);
        Assert.Equal(27, result.Cases);
        Assert.Equal(14, result.NegativeControls);
        Assert.Equal(41, result.Observations.Count);
    }
}
