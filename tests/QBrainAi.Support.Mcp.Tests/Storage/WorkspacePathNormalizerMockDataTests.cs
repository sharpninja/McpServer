using QBrainAi.Client;

namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>
/// TEST-MCP-FED-PATH-001, BDP v4 step 2 (plan section 2a): runs the
/// <see cref="WorkspacePathNormalizerContractTests"/> against <see cref="ScriptedWorkspacePathNormalizer"/>,
/// a test-only fake that answers from a literal data table. Green here proves the contract tests
/// and their expected data are correct independently of the production implementation.
/// </summary>
public sealed class WorkspacePathNormalizerMockDataTests : WorkspacePathNormalizerContractTests
{
    /// <inheritdoc />
    protected override IWorkspacePathNormalizer CreateSut(IWorkspaceHostEnvironment host) =>
        new ScriptedWorkspacePathNormalizer(host);

    /// <summary>
    /// TEST-MCP-FED-PATH-001 negative controls for TR-MCP-FED-PATH-001 (BDP v4 step 3): the assertion
    /// shared by the root-relative Combine theories accepts the correct result from a fixed-result fake
    /// and rejects each deliberately wrong one, while this harness stays green. Wrong results: the
    /// segment appended instead of replacing the path (C:/content/repo, and //srv/share/content/repo
    /// from the PR #82 review), the drive dropped (/repo), and the UNC share dropped (//srv/repo).
    /// </summary>
    [Theory]
    [InlineData("C:/content", null, "C:/repo", "C:/content/repo")]
    [InlineData("C:/content", null, "C:/repo", "/repo")]
    [InlineData(@"\\srv\share\content", null, "//srv/share/repo", "//srv/share/content/repo")]
    [InlineData(@"\\srv\share\content", null, "//srv/share/repo", "//srv/repo")]
    [InlineData("//srv/share/content", "sub", "//srv/share/repo/sub", "//srv/share/content/repo/sub")]
    public void RootRelativeNegativeControls_AreRejectedInsidePassingHarness(string root, string? next, string expected, string wrongResult)
    {
        AssertRootRelativeCombine(new FixedCombineNormalizer(expected), root, @"\repo", next, expected);

        Assert.NotNull(Record.Exception(() =>
            AssertRootRelativeCombine(new FixedCombineNormalizer(wrongResult), root, @"\repo", next, expected)));
    }

    /// <summary>Fake whose Combine always returns one fixed result; every other member is unused.</summary>
    private sealed class FixedCombineNormalizer(string combineResult) : IWorkspacePathNormalizer
    {
        public WorkspacePathPlatform DetectPlatform(string workspacePath) => throw new NotSupportedException();

        public string Normalize(string workspacePath) => throw new NotSupportedException();

        public string GetLeafName(string workspacePath) => throw new NotSupportedException();

        public bool IsHostNative(string workspacePath) => throw new NotSupportedException();

        public string Combine(string rootPath, params string[] segments) => combineResult;
    }
}
