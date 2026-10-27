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
/// TEST-MCP-USECASE-002 / FR-MCP-USECASE-001 / TR-MCP-USECASE-002:
/// proves the get-by-id workspace contract rejects raw-path lookup behavior before production binding.
/// </summary>
public sealed class UseCaseGetWorkspaceResolutionContractTests
{
    // FR/TR-MCP-PATHNORM-001: retain historical raw caller bytes and canonical DTO expectation separately.
    private const string RawWorkspacePath = @"F:\GitHub\TruckMate";
    private const string CanonicalWorkspaceId = "F:/GitHub/TruckMate";

    /// <summary>Correct canonical workspace output satisfies every contract assertion.</summary>
    [Fact]
    public async Task MockGate_CanonicalWorkspaceResult_PassesContract()
    {
        await AssertCanonicalWorkspaceContractAsync(
            () => Task.FromResult(Result<UseCaseDetailDto>.Success(new UseCaseDetailDto
            {
                UseCaseId = 125,
                WorkspaceId = CanonicalWorkspaceId,
                Title = "Start and certify LAB-OMARCHY TruckMate services",
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

    /// <summary>FR-MCP-PATHNORM-001: a DTO leaking the original raw key is rejected by every retained assertion.</summary>
    [Fact]
    public async Task MockGate_RawWorkspaceDto_IsRejected()
    {
        await Assert.ThrowsAsync<EqualException>(
            () => AssertCanonicalWorkspaceContractAsync(
                () => Task.FromResult(Result<UseCaseDetailDto>.Success(new UseCaseDetailDto
                {
                    UseCaseId = 125,
                    WorkspaceId = RawWorkspacePath,
                    Title = "Start and certify LAB-OMARCHY TruckMate services",
                })))).ConfigureAwait(true);
    }

    /// <summary>Production binding resolves an equivalent raw path to the canonical workspace before get-by-id.</summary>
    [Fact]
    public async Task ProductionGate_EquivalentRawPath_UsesCanonicalWorkspace()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseSqlite(connection)
            .Options;
        var workspaceContext = new WorkspaceContext
        {
            WorkspacePath = RawWorkspacePath,
            WorkspaceName = "TruckMate",
            DataDirectory = RawWorkspacePath,
        };

        await using var db = new McpDbContext(options, workspaceContext);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        db.OverrideWorkspaceId(RawWorkspacePath);
        db.Workspaces.Add(new WorkspaceEntity
        {
            WorkspaceId = RawWorkspacePath,
            WorkspacePath = RawWorkspacePath,
            Name = "TruckMate",
        });
        db.UseCases.Add(new UseCaseEntity
        {
            UseCaseId = 125,
            WorkspaceId = RawWorkspacePath,
            Title = "Start and certify LAB-OMARCHY TruckMate services",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        var equivalentRawPath = RawWorkspacePath + Path.DirectorySeparatorChar + ".";
        var handler = new GetUseCaseQueryHandler(db, workspaceContext);

        await AssertCanonicalWorkspaceContractAsync(
            () => handler.HandleAsync(
                new GetUseCaseQuery(equivalentRawPath, 125),
                new CallContext { CancellationToken = TestContext.Current.CancellationToken })).ConfigureAwait(true);
    }

    private static async Task AssertCanonicalWorkspaceContractAsync(
        Func<Task<Result<UseCaseDetailDto>>> invoke)
    {
        var result = await invoke().ConfigureAwait(true);

        Assert.True(result.IsSuccess, result.Error);
        Assert.NotNull(result.Value);
        Assert.Equal(125, result.Value!.UseCaseId);
        Assert.Equal(CanonicalWorkspaceId, result.Value.WorkspaceId);
        Assert.Equal("Start and certify LAB-OMARCHY TruckMate services", result.Value.Title);
    }
}
