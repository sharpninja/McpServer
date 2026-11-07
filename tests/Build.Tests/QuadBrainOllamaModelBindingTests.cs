using QBrainAi.Build.Shared;

namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-016 AC-001 binding: the shared model rule <see cref="QuadBrainOllamaModel.Select"/> satisfies the rows of
/// <see cref="QuadBrainOllamaModelContractTests"/>, and both consumers, InstallOllama's RequiredOllamaModel and
/// QuadBrainOllamaEndpointIntegrationTests, choose the model through it and read neither variable another way.
/// </summary>
/// <remarks>
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-02</term><description>FR-MCP-BUILD-001; TR-MCP-NUKE-016 AC-001 (appended: one rule both use): added after mock gate ug-r10-mockgate-t-1, for hostile validation finding R9-F5 (operator 2026-11-02: "Strengthen 5 ACs, fix all 8 (Recommended)").</description></item>
/// <item><term>2026-11-07</term><description>FR-MCP-BUILD-001; TR-MCP-NUKE-016 AC-001; TR-MCP-NUKE-019 AC-7: the infrastructure half of this class; its Category=Integration case BothConsumers_UseTheSharedRule and FindRepositoryRoot, which only that case used, moved unchanged to the class of the same name in the integration-owned Build.IntegrationTests, so this infrastructure-owned project holds no integration case (slice S4, hostile validation finding R14-F4); SharedRule_SatisfiesMockGatedContract is unchanged.</description></item>
/// </list>
/// </remarks>
public sealed class QuadBrainOllamaModelBindingTests
{
    /// <summary>The shared rule satisfies every contract row and names the contract's default model.</summary>
    [Fact]
    public void SharedRule_SatisfiesMockGatedContract()
    {
        Assert.Equal(QuadBrainOllamaModelContractTests.DefaultModel, QuadBrainOllamaModel.DefaultModel);
        QuadBrainOllamaModelContractTests.AssertSelection(QuadBrainOllamaModel.Select);
    }
}
