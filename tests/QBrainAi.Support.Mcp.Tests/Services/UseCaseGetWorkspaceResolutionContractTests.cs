using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QBrainAi.Cqrs;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Entities;
using QBrainAi.Support.Mcp.UseCases.Models;
using QBrainAi.Support.Mcp.UseCases.Queries;
using Xunit;
using Xunit.Sdk;
using static QBrainAi.Support.Mcp.Tests.Services.UseCaseGetWorkspaceResolutionContractTestsSupport;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-USECASE-002 / FR-MCP-USECASE-001 / TR-MCP-USECASE-002:
/// proves the get-by-id workspace contract rejects raw-path lookup behavior before production binding.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-USECASE-002; FR-MCP-USECASE-001; TR-MCP-USECASE-002; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the unit half of this class: its 1 Category=Integration case (ProductionGate_EquivalentRawPath_UsesCanonicalWorkspace) moved unchanged to the class of the same name in the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S7, hostile validation finding R14-F4), so this unit-owned project holds only unit cases; the helpers both halves use (RawWorkspacePath, CanonicalWorkspaceId and AssertCanonicalWorkspaceContractAsync) moved once into Services/UseCaseGetWorkspaceResolutionContractTestsSupport.cs beside this file, which the integration project links (DRY); the unit cases and the class attributes are unchanged.</remarks>
public sealed class UseCaseGetWorkspaceResolutionContractTests
{
    /// <summary>Correct canonical workspace output satisfies every contract assertion.</summary>
    [Fact]
    public async Task MockGate_CanonicalWorkspaceResult_PassesContract()
    {
        await AssertCanonicalWorkspaceContractAsync(
            () => Task.FromResult(Result<UseCaseDetailDto>.Success(new UseCaseDetailDto
            {
                UseCaseId = 125,
                WorkspaceId = CanonicalWorkspaceId,
                Title = "Certify fixture services",
            }))).ConfigureAwait(true);
    }

    /// <summary>Raw-path lookup failure is a deliberate negative control rejected by the same assertions.</summary>
    [Fact]
    public async Task MockGate_RawWorkspaceLookupFailure_IsRejected()
    {
        var rejection = await Assert.ThrowsAsync<TrueException>(
            () => AssertCanonicalWorkspaceContractAsync(
                () => Task.FromResult(Result<UseCaseDetailDto>.Failure(
                    "Use case '125' was not found under the raw workspace path.")))).ConfigureAwait(true);

        Assert.Contains("raw workspace path", rejection.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// TEST-MCP-PATHNORM-001 negative control: a result that returns the caller's backslash spelling
    /// instead of the canonical key is rejected by the same assertions.
    /// </summary>
    [Fact]
    public async Task MockGate_BackslashWorkspaceId_IsRejected()
    {
        await Assert.ThrowsAsync<EqualException>(
            () => AssertCanonicalWorkspaceContractAsync(
                () => Task.FromResult(Result<UseCaseDetailDto>.Success(new UseCaseDetailDto
                {
                    UseCaseId = 125,
                    WorkspaceId = RawWorkspacePath,
                    Title = "Certify fixture services",
                })))).ConfigureAwait(true);
    }
}
