using System.Text.RegularExpressions;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage.Entities;
using QBrainAi.Tests.Shared;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// P3: ReplayOfRunId and HandoffReviewState.Approved stay removed.
/// Reflection, schema, and source inventory fail if either dead contract is reintroduced.
/// </summary>
/// <remarks>2026-11-07, TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the unit half of this class: its 1 Category=Integration case (SourceAndSchema_DoNotReintroduceReplayOfRunIdOrApproved) and the helpers only it uses (ApprovedEnumMember and FindRepoRoot) moved unchanged to the class of the same name in the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S7, hostile validation finding R14-F4), so this unit-owned project holds only unit cases; the unit cases and the class attributes are unchanged.</remarks>
public sealed class HandoffDeadContractInventoryTests
{
    /// <summary>P3: neither service nor client HandoffReviewState defines Approved.</summary>
    [Fact]
    public void HandoffReviewState_DoesNotDefineApproved()
    {
        Assert.DoesNotContain(Enum.GetNames<HandoffReviewState>(), name => string.Equals(name, "Approved", StringComparison.Ordinal));
        Assert.DoesNotContain(Enum.GetNames<QBrainAi.Client.Models.HandoffReviewState>(), name => string.Equals(name, "Approved", StringComparison.Ordinal));
        Assert.False(Enum.IsDefined(typeof(HandoffReviewState), 2));
        Assert.False(Enum.IsDefined(typeof(QBrainAi.Client.Models.HandoffReviewState), 2));
    }

    /// <summary>P3: the persisted run entity has no ReplayOfRunId column or property.</summary>
    [Fact]
    public void HandoffIngestionRunEntity_DoesNotExposeReplayOfRunId()
    {
        var names = typeof(HandoffIngestionRunEntity).GetProperties().Select(property => property.Name).ToArray();
        Assert.DoesNotContain(names, name => string.Equals(name, "ReplayOfRunId", StringComparison.Ordinal));
    }
}
