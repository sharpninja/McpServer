using QBrainAi.Tests.Shared;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Configuration;

/// <summary>
/// TR-MCP-USECASE-018 (AC-006), TR-MCP-USECASE-019 (AC-008), BDPv4 mock gate for the trait policy of
/// <see cref="TestRepositoryRoot"/>, the one repository-root locator: scripted trait sets (mock data) pass only when
/// they hold <c>Category=Integration</c>, matching the scanner's ordinal trait test; negative controls show the shared
/// assertion rejects a policy that always passes, one that always refuses, and one that refuses with another reason.
/// The ambient case shows a unit test is refused before the checkout is touched; the integration case is the
/// production binding and reads the checkout.
/// </summary>
/// <remarks>2026-11-07, TR-MCP-USECASE-018; TR-MCP-USECASE-019; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Configuration/TestRepositoryRootPolicyTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 1 Category=Integration case moved unchanged with the class attributes; only the namespace changed.</remarks>
public sealed class TestRepositoryRootPolicyTests
{
    /// <summary>Production binding: in an integration test the locator returns the checkout root.</summary>
    [Fact]
    [Trait("Category", "Integration")]
    public void Resolve_InAnIntegrationTest_ReturnsTheCheckoutRoot()
    {
        var root = TestRepositoryRoot.Resolve();
        Assert.True(File.Exists(Path.Combine(root, "QBrainAi.sln")), root);
        Assert.Equal(Path.Combine(root, "tests"), TestRepositoryRoot.Combine("tests"));
        Assert.True(File.Exists(TestRepositoryRoot.Combine("tests", "Shared", "TestRepositoryRoot.cs")));
        Assert.False(string.IsNullOrWhiteSpace(TestRepositoryRoot.BuildMetadataRoot));
    }
}
