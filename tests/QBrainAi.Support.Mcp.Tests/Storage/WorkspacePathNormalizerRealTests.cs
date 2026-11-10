using QBrainAi.Client;

namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>
/// TEST-MCP-FED-PATH-001, BDP v4 step 3 (plan section 2a): runs the
/// <see cref="WorkspacePathNormalizerContractTests"/> against the production
/// <see cref="WorkspacePathNormalizer"/>, plus checks that only apply to the concrete type.
/// </summary>
public sealed class WorkspacePathNormalizerRealTests : WorkspacePathNormalizerContractTests
{
    /// <inheritdoc />
    protected override IWorkspacePathNormalizer CreateSut(IWorkspaceHostEnvironment host) =>
        new WorkspacePathNormalizer(host);

    /// <summary>TR-MCP-FED-PATH-001: A null host is rejected at construction.</summary>
    [Fact]
    public void Constructor_NullHost_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new WorkspacePathNormalizer(null!));
    }
}
