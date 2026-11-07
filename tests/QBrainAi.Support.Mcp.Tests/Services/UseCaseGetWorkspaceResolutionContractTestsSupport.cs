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

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-USECASE-002; FR-MCP-USECASE-001; TR-MCP-USECASE-002: the helpers that both halves of UseCaseGetWorkspaceResolutionContractTests use (the unit half in QBrainAi.Support.Mcp.Tests, the integration half in QBrainAi.Support.Mcp.EnvironmentIntegrationTests), defined once and compiled into both projects.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-USECASE-002; FR-MCP-USECASE-001; TR-MCP-USECASE-002; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: extracted unchanged from UseCaseGetWorkspaceResolutionContractTests (RawWorkspacePath, CanonicalWorkspaceId and AssertCanonicalWorkspaceContractAsync) when that class split into its unit half and its integration half (slice S7, hostile validation finding R14-F4), so both halves use one definition instead of a copy (DRY); only the accessibility changed, from private to internal.</remarks>
internal static class UseCaseGetWorkspaceResolutionContractTestsSupport
{
    /// <summary>The workspace path as a Windows caller spells it.</summary>
    internal const string RawWorkspacePath = @"Q:\fixture\workspace";

    /// <summary>FR-MCP-PATHNORM-001: the stored and returned workspace key uses '/' (test-side oracle).</summary>
    internal static readonly string CanonicalWorkspaceId = ExpectedWorkspaceKey.Of(RawWorkspacePath);

    internal static async Task AssertCanonicalWorkspaceContractAsync(
        Func<Task<Result<UseCaseDetailDto>>> invoke, string? expectedWorkspace = null)
    {
        var result = await invoke().ConfigureAwait(true);

        Assert.True(result.IsSuccess, result.Error);
        Assert.NotNull(result.Value);
        Assert.Equal(125, result.Value!.UseCaseId);
        Assert.Equal(expectedWorkspace ?? CanonicalWorkspaceId, result.Value.WorkspaceId);
        Assert.Equal("Certify fixture services", result.Value.Title);
    }
}
