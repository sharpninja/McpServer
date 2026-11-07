using QBrainAi.Tests.Shared;

namespace QBrainAi.Support.Mcp.Tests.Configuration;

/// <summary>
/// TR-MCP-USECASE-018 (AC-006), TR-MCP-USECASE-019 (AC-008), BDPv4 mock gate for the trait policy of
/// <see cref="TestRepositoryRoot"/>, the one repository-root locator: scripted trait sets (mock data) pass only when
/// they hold <c>Category=Integration</c>, matching the scanner's ordinal trait test; negative controls show the shared
/// assertion rejects a policy that always passes, one that always refuses, and one that refuses with another reason.
/// The ambient case shows a unit test is refused before the checkout is touched; the integration case is the
/// production binding and reads the checkout.
/// </summary>
public sealed class TestRepositoryRootPolicyTests
{
    private static readonly Dictionary<string, (IReadOnlyDictionary<string, IReadOnlyCollection<string>>? Traits, bool Allowed)> Cases =
        new(StringComparer.Ordinal)
        {
            ["integration"] = (Traits(("Category", ["Integration"])), true),
            ["integration-with-platform"] = (Traits(("Category", ["Integration"]), ("Platform", ["Linux"])), true),
            ["integration-among-categories"] = (Traits(("Category", ["Slow", "Integration"])), true),
            ["unit-category"] = (Traits(("Category", ["Unit"])), false),
            ["no-traits"] = (Traits(), false),
            ["no-test-context"] = (null, false),
            ["platform-only"] = (Traits(("Platform", ["Windows"])), false),
            ["lowercase-key"] = (Traits(("category", ["Integration"])), false),
            ["lowercase-value"] = (Traits(("Category", ["integration"])), false),
        };

    /// <summary>Every scripted trait set is allowed or refused as the rule requires.</summary>
    /// <param name="name">Case name.</param>
    [Theory]
    [InlineData("integration")]
    [InlineData("integration-with-platform")]
    [InlineData("integration-among-categories")]
    [InlineData("unit-category")]
    [InlineData("no-traits")]
    [InlineData("no-test-context")]
    [InlineData("platform-only")]
    [InlineData("lowercase-key")]
    [InlineData("lowercase-value")]
    public void ScriptedTraits_AreAllowedOnlyForIntegration(string name)
    {
        var (traits, allowed) = Cases[name];
        AssertPolicy(TestRepositoryRoot.EnsureIntegration, traits, allowed);
    }

    /// <summary>The theory above covers every scripted case.</summary>
    [Fact]
    public void ScriptedTheory_CoversEveryCase() => Assert.Equal(9, Cases.Count);

    /// <summary>
    /// Negative controls: the shared assertion rejects a policy that lets a unit test through, one that refuses an
    /// integration test, and one that refuses a unit test for a reason other than the rule.
    /// </summary>
    [Fact]
    public void PolicyAssertionNegativeControls_AreRejectedInsidePassingHarness()
    {
        var unit = Cases["unit-category"].Traits;
        var integration = Cases["integration"].Traits;
        Assert.NotNull(Record.Exception(() => AssertPolicy(_ => { }, unit, allowed: false)));
        Assert.NotNull(Record.Exception(() => AssertPolicy(_ => throw new InvalidOperationException("refused"), integration, allowed: true)));
        Assert.NotNull(Record.Exception(() => AssertPolicy(_ => throw new InvalidOperationException("another reason"), unit, allowed: false)));
        Assert.Null(Record.Exception(() => AssertPolicy(TestRepositoryRoot.EnsureIntegration, unit, allowed: false)));
        Assert.Null(Record.Exception(() => AssertPolicy(TestRepositoryRoot.EnsureIntegration, integration, allowed: true)));
    }

    /// <summary>A unit test that reaches the locator is refused before the checkout is located or read.</summary>
    [Fact]
    public void Resolve_InAUnitTest_IsRefusedWithTheRule()
    {
        var resolve = Assert.Throws<InvalidOperationException>(() => TestRepositoryRoot.Resolve());
        Assert.Contains("may not read the repository checkout", resolve.Message, StringComparison.Ordinal);
        var combine = Assert.Throws<InvalidOperationException>(() => TestRepositoryRoot.Combine("tests"));
        Assert.Contains("may not read the repository checkout", combine.Message, StringComparison.Ordinal);
        var metadata = Assert.Throws<InvalidOperationException>(() => TestRepositoryRoot.BuildMetadataRoot);
        Assert.Contains("may not read the repository checkout", metadata.Message, StringComparison.Ordinal);
    }

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

    /// <summary>Assertion shared by the scripted cases and the negative controls.</summary>
    /// <param name="policy">The policy under test.</param>
    /// <param name="traits">Scripted traits.</param>
    /// <param name="allowed">Whether the rule allows them.</param>
    private static void AssertPolicy(
        Action<IReadOnlyDictionary<string, IReadOnlyCollection<string>>?> policy,
        IReadOnlyDictionary<string, IReadOnlyCollection<string>>? traits,
        bool allowed)
    {
        if (allowed)
        {
            Assert.Null(Record.Exception(() => policy(traits)));
            return;
        }

        var refusal = Assert.Throws<InvalidOperationException>(() => policy(traits));
        Assert.Contains("may not read the repository checkout", refusal.Message, StringComparison.Ordinal);
    }

    private static IReadOnlyDictionary<string, IReadOnlyCollection<string>> Traits(params (string Name, string[] Values)[] traits) =>
        traits.ToDictionary(trait => trait.Name, trait => (IReadOnlyCollection<string>)trait.Values, StringComparer.Ordinal);
}
