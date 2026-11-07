using QBrainAi.Support.Mcp.Options;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Services;

/// <summary>Unit tests for hub-and-spoke federation topology persistence.</summary>
/// <remarks>2026-11-07, TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Services/FederationTopologyServiceTests.cs), where its unit cases stay in the class of the same name (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"): it holds no case, only the helper AssertEnrollmentPersistsInventoryAsync, unchanged, which the moved Windows and Linux platform classes call by this class name; the state and helpers both halves need come from the abstract base class FederationTopologyServiceTestsBase in Services/FederationTopologyServiceTestsBase.cs of QBrainAi.Support.Mcp.Tests, linked into this project (DRY); only the namespace changed.</remarks>
public sealed class FederationTopologyServiceTests : FederationTopologyServiceTestsBase, IDisposable
{
    /// <summary>
    /// Enrollment persists the proxy and hosted workspaces for hub inventory. The stored workspace key of a
    /// real host directory is compared with the host's path comparison, so the cases live in the Windows
    /// and Linux integration classes below (operator 2026-10-19: "Integration + Platform (Recommended)";
    /// TR-MCP-USECASE-018 AC-006).
    /// </summary>
    /// <param name="workspaceComparison">The running host's path comparison.</param>
    internal static async Task AssertEnrollmentPersistsInventoryAsync(StringComparison workspaceComparison)
    {
        var workspacePath = Path.Combine(
            Path.GetTempPath(), "QBrainAi.Tests", nameof(FederationTopologyServiceTests), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspacePath);
        try
        {
            using var provider = CreateProvider();
            var sut = provider.GetRequiredService<IFederationTopologyService>();

            var response = await sut.EnrollAsync(new FederationEnrollmentRequest
            {
                ProxyId = "TEST-NODE-A",
                DisplayName = "TEST-NODE-A",
                BaseUrl = "http://test-node-a:7147/",
                Workspaces =
                [
                    new FederationWorkspaceRegistrationRequest
                    {
                        WorkspaceName = "QBrainAi",
                        WorkspacePath = workspacePath,
                    },
                ],
            }, CancellationToken.None).ConfigureAwait(true);

            Assert.True(response.Accepted);
            Assert.Equal("TEST-NODE-A", response.ProxyId);
            Assert.Equal(5, response.HeartbeatSeconds);

            var proxies = await sut.ListProxiesAsync(CancellationToken.None).ConfigureAwait(true);
            var proxy = Assert.Single(proxies);
            Assert.Equal("TEST-NODE-A", proxy.ProxyId);
            Assert.Equal(1, proxy.WorkspaceCount);

            var workspaces = await sut.ListWorkspacesAsync("TEST-NODE-A", CancellationToken.None).ConfigureAwait(true);
            var workspace = Assert.Single(workspaces);
            Assert.True(string.Equals(ExpectedWorkspaceKey.Of(workspacePath), workspace.WorkspacePath, workspaceComparison));
            Assert.NotEmpty(workspace.GlobalWorkspaceId);
        }
        finally
        {
            if (Directory.Exists(workspacePath))
                Directory.Delete(workspacePath, recursive: true);
        }
    }
}

/// <summary>
/// Hub-and-spoke enrollment on a Windows host: the stored key of a real host directory matches with the
/// Windows path comparison (TR-MCP-USECASE-018 AC-006, TR-MCP-NUKE-019).
/// </summary>
/// <remarks>2026-11-07, TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: moved whole with its 1 Windows-only Category=Integration case from QBrainAi.Support.Mcp.Tests (Services/FederationTopologyServiceTests.cs), whose unit classes stay there, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); only the namespace changed.</remarks>
[Trait("Category", "Integration")]
[Trait("Platform", "Windows")]
public sealed class FederationTopologyServiceWindowsTests
{
    /// <summary>Enrollment persists the proxy and hosted workspaces for hub inventory.</summary>
    [Fact]
    public Task EnrollAsync_PersistsProxyAndWorkspaceInventory() =>
        FederationTopologyServiceTests.AssertEnrollmentPersistsInventoryAsync(StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Hub-and-spoke enrollment on a Linux host: the stored key of a real host directory matches with the
/// case-sensitive path comparison (TR-MCP-USECASE-018 AC-006, TR-MCP-NUKE-019).
/// </summary>
/// <remarks>2026-11-07, TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: moved whole with its 1 Linux-only Category=Integration case from QBrainAi.Support.Mcp.Tests (Services/FederationTopologyServiceTests.cs), whose unit classes stay there, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); only the namespace changed.</remarks>
[Trait("Category", "Integration")]
[Trait("Platform", "Linux")]
public sealed class FederationTopologyServiceLinuxTests
{
    /// <summary>Enrollment persists the proxy and hosted workspaces for hub inventory.</summary>
    [Fact]
    public Task EnrollAsync_PersistsProxyAndWorkspaceInventory() =>
        FederationTopologyServiceTests.AssertEnrollmentPersistsInventoryAsync(StringComparison.Ordinal);
}
