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
    /// <summary>The workspace path as a Windows caller spells it.</summary>
    private const string RawWorkspacePath = @"Q:\fixture\workspace";

    /// <summary>FR-MCP-PATHNORM-001: the stored and returned workspace key uses '/' (test-side oracle).</summary>
    private static readonly string CanonicalWorkspaceId = ExpectedWorkspaceKey.Of(RawWorkspacePath);

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

    /// <summary>Production binding resolves an equivalent dot-segment spelling of its own temporary workspace.</summary>
    /// <remarks>Integration: the real handler resolves physical workspace paths (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task ProductionGate_EquivalentRawPath_UsesCanonicalWorkspace()
    {
        var workspaceRoot = Path.Combine(Path.GetTempPath(), "usecase-owned-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspaceRoot);
        try
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
            var options = new DbContextOptionsBuilder<McpDbContext>()
                .UseSqlite(connection)
                .Options;
            // Seeded with the caller's spelling on purpose: the store canonicalizes workspace keys on save.
            var workspaceContext = new WorkspaceContext
            {
                WorkspacePath = workspaceRoot,
                WorkspaceName = "Fixture",
                DataDirectory = workspaceRoot,
            };

            await using var db = new McpDbContext(options, workspaceContext);
            await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
            db.OverrideWorkspaceId(workspaceRoot);
            db.Workspaces.Add(new WorkspaceEntity
            {
                WorkspaceId = workspaceRoot,
                WorkspacePath = workspaceRoot,
                Name = "Fixture",
            });
            db.UseCases.Add(new UseCaseEntity
            {
                UseCaseId = 125,
                WorkspaceId = workspaceRoot,
                Title = "Certify fixture services",
                CreatedAtUtc = DateTimeOffset.UtcNow,
                UpdatedAtUtc = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            var equivalentRawPath = workspaceRoot + "/.";
            var handler = new GetUseCaseQueryHandler(db, workspaceContext);

            await AssertCanonicalWorkspaceContractAsync(
                () => handler.HandleAsync(
                    new GetUseCaseQuery(equivalentRawPath, 125),
                    new CallContext { CancellationToken = TestContext.Current.CancellationToken }), ExpectedWorkspaceKey.Of(workspaceRoot)).ConfigureAwait(true);
        }
        finally
        {
            Directory.Delete(workspaceRoot, recursive: true);
        }
    }

    private static async Task AssertCanonicalWorkspaceContractAsync(
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
