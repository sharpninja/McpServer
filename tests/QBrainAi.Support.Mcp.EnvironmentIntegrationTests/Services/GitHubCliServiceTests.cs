using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Options;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Tests;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Services;

/// <summary>TR-PLANNED-CORE-013 (historical), TR-GH-013-001 (historical): Unit tests for GitHubCliService with mocked IProcessRunner.</summary>
/// <remarks>2026-11-07, FR-MCP-FED-PATH-001 AC5; TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Services/GitHubCliServiceTests.cs), where the unit cases stay in the class of the same name (slice S8, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"). It holds only the helper AssertForeignWorkspaceWithoutRepositoryIsRefusedAsync, which the Windows and Linux classes below call by this class name; the helper reads the setting name from the linked GitHubCliInvocationPlanContractTestsSupport (see the entry on it), and TestWorkspaceAccessorHelper is linked into this assembly.</remarks>
public sealed class GitHubCliServiceTests
{
    /// <summary>
    /// FR-MCP-FED-PATH-001 AC5 real-host binding (operator 2026-10-29: "gh foreign-workspace AC (Recommended)"; holistic review
    /// findings SRC-1 and SRC-4): on the running host, through the process path normalizer, a foreign-platform workspace (a Windows
    /// path on a POSIX host, a POSIX path on Windows) with no configured repository never starts gh, and the failure names the
    /// workspace and Mcp:GitHub:Repository. Which path is foreign depends on the running host, so the cases live in the Windows and
    /// Linux integration classes below; the unit contract is GitHubCliInvocationPlanContractTests over simulated hosts.
    /// </summary>
    /// <param name="foreignWorkspacePath">A workspace path of the other platform than the running host.</param>
    /// <remarks>2026-11-07, FR-MCP-FED-PATH-001 AC5; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: moved from the unit half in QBrainAi.Support.Mcp.Tests with the two classes that call it (slice S8, hostile validation finding R14-F4); the setting name it asserts is now read from GitHubCliInvocationPlanContractTestsSupport.RepositorySetting, the one definition GitHubCliInvocationPlanContractTests also uses, instead of through that unit test class, which this project cannot reach; the assertions are unchanged.</remarks>
    internal static async Task AssertForeignWorkspaceWithoutRepositoryIsRefusedAsync(string foreignWorkspacePath)
    {
        var processRunner = Substitute.For<IProcessRunner>();
        var workspaceAccessor = TestWorkspaceAccessorHelper.Create(Substitute.For<ITodoService>(), repoRoot: foreignWorkspacePath);
        var sut = new GitHubCliService(processRunner, NullLogger<GitHubCliService>.Instance, workspaceAccessor: workspaceAccessor);

        var result = await sut.ListIssuesAsync("open", 5, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Contains(workspaceAccessor.GetWorkspacePath()!, result.Error, StringComparison.Ordinal);
        Assert.Contains(GitHubCliInvocationPlanContractTestsSupport.RepositorySetting, result.Error, StringComparison.Ordinal);
        Assert.Empty(processRunner.ReceivedCalls());
    }
}

/// <summary>
/// FR-MCP-FED-PATH-001 AC5 real-host binding on a Windows host: a POSIX workspace path is foreign there (TR-MCP-USECASE-018
/// AC-006, TR-MCP-NUKE-019).
/// </summary>
/// <remarks>2026-11-07, FR-MCP-FED-PATH-001 AC5; TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: moved unchanged from QBrainAi.Support.Mcp.Tests (Services/GitHubCliServiceTests.cs) into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests with its Windows-only Category=Integration case, beside the integration half of GitHubCliServiceTests whose helper it calls by class name, so the unit-owned project holds only unit cases (slice S8, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); only the namespace changed.</remarks>
[Trait("Category", "Integration")]
[Trait("Platform", "Windows")]
public sealed class GitHubCliServiceWindowsTests
{
    /// <summary>gh is not started for a POSIX workspace with no configured repository, and the failure names the setting.</summary>
    [Fact]
    public Task ListIssuesAsync_WithForeignWorkspaceAndNoRepository_IsRefused() =>
        GitHubCliServiceTests.AssertForeignWorkspaceWithoutRepositoryIsRefusedAsync("/srv/foreign/workspace");
}

/// <summary>
/// FR-MCP-FED-PATH-001 AC5 real-host binding on a Linux host: a Windows drive workspace path is foreign there
/// (TR-MCP-USECASE-018 AC-006, TR-MCP-NUKE-019).
/// </summary>
/// <remarks>2026-11-07, FR-MCP-FED-PATH-001 AC5; TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: moved unchanged from QBrainAi.Support.Mcp.Tests (Services/GitHubCliServiceTests.cs) into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests with its Linux-only Category=Integration case, beside the integration half of GitHubCliServiceTests whose helper it calls by class name, so the unit-owned project holds only unit cases (slice S8, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); only the namespace changed.</remarks>
[Trait("Category", "Integration")]
[Trait("Platform", "Linux")]
public sealed class GitHubCliServiceLinuxTests
{
    /// <summary>gh is not started for a Windows drive workspace with no configured repository, and the failure names the setting.</summary>
    [Fact]
    public Task ListIssuesAsync_WithForeignWorkspaceAndNoRepository_IsRefused() =>
        GitHubCliServiceTests.AssertForeignWorkspaceWithoutRepositoryIsRefusedAsync(@"C:\foreign\workspace");
}
