using QBrainAi.Client;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Tests.Storage;
using QBrainAi.Tests.Shared;

namespace QBrainAi.Support.Mcp.Tests.Infrastructure;

/// <summary>
/// M1 production bindings of the exact explicit-input mock-gated cases. Repository predicates and listings remain
/// scripted; selection, SQL defaults and canonical no-follow reachability execute the real implementations.
/// </summary>
public sealed class PickupIsolationBindingTests : PickupIsolationContractTests
{
    /// <inheritdoc />
    protected override string Locate(string? configured, string? buildRoot, string?[] starts,
        Func<string, string> normalize, Func<string, bool> isRoot, Func<string, string?> parent) =>
        TestRepositoryRoot.Locate(configured, buildRoot, starts, normalize, isRoot, parent);

    /// <inheritdoc />
    protected override string ResolveSql(string? value) => SqlServerTestConnection.Resolve(value);

    /// <inheritdoc />
    private protected override bool Reach(string boundary, string root, WorkspacePathPlatform platform,
        Func<string, IReadOnlyList<DirectoryListingEntry>> list) =>
        new DirectoryTreeWalker(list, platform).IsReachable(boundary, root);
}
