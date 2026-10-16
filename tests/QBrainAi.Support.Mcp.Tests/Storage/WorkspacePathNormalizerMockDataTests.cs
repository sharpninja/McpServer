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
}
