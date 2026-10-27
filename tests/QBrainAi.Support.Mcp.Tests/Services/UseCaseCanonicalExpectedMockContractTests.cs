using QBrainAi.Cqrs;
using QBrainAi.Support.Mcp.UseCases.Models;
using Xunit;
using Xunit.Sdk;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// FR-MCP-PATHNORM-001 / TEST-MCP-USECASE-002: independently pinned canonical DTO results
/// exercise the original assertion body before repairing the original production test binding.
/// </summary>
public sealed class UseCaseCanonicalExpectedMockContractTests
{
    private const string CanonicalWorkspaceId = "F:/GitHub/TruckMate";

    /// <summary>The required canonical DTO passes every original get-by-id assertion.</summary>
    [Fact]
    public async Task CorrectCanonicalDto_PassesOriginalAssertions()
    {
        await AssertCanonicalWorkspaceContractAsync(() => Task.FromResult(Result<UseCaseDetailDto>.Success(new UseCaseDetailDto
        {
            UseCaseId = 125,
            WorkspaceId = "F:/GitHub/TruckMate",
            Title = "Start and certify LAB-OMARCHY TruckMate services",
        }))).ConfigureAwait(true);
    }

    /// <summary>The legacy raw backslash DTO must fail the same canonical identity assertion.</summary>
    [Fact]
    public async Task WrongRawDto_IsRejectedByOriginalAssertions()
    {
        await Assert.ThrowsAsync<EqualException>(() => AssertCanonicalWorkspaceContractAsync(
            () => Task.FromResult(Result<UseCaseDetailDto>.Success(new UseCaseDetailDto
            {
                UseCaseId = 125,
                WorkspaceId = @"F:\GitHub\TruckMate",
                Title = "Start and certify LAB-OMARCHY TruckMate services",
            })))).ConfigureAwait(true);
    }

    /// <summary>A missing raw-path result must still fail the original success assertion.</summary>
    [Fact]
    public async Task MissingResult_IsRejectedByOriginalAssertions()
    {
        await Assert.ThrowsAsync<TrueException>(() => AssertCanonicalWorkspaceContractAsync(
            () => Task.FromResult(Result<UseCaseDetailDto>.Failure("Use case '125' was not found under the raw workspace path.")))).ConfigureAwait(true);
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
