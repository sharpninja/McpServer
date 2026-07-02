using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// FR-MCP-REQAC-001 / TR-MCP-REQAC-001 / TEST-MCP-REQAC-001..002 (Gate 2): validates the
/// strict-4NF persistence layer for requirement acceptance criteria
/// (<see cref="RequirementAcceptanceCriterionEntity"/> child rows), which replaced the former
/// <c>AcceptanceCriteriaJson</c> column. Child rows are written/read from the dependent side; the
/// requirement's <c>AcceptanceCriteria</c> holder is explicitly loaded, not an EF navigation. The
/// fixture uses an in-memory EF context so each test runs in isolation.
/// </summary>
public sealed class RequirementAcceptanceCriteriaTests
{
    /// <summary>
    /// The 4NF child entity is mapped with its own table, the legacy JSON column is gone, and the
    /// requirement's <c>AcceptanceCriteria</c> holder is not an EF navigation (loaded explicitly).
    /// </summary>
    [Fact]
    public void RequirementAcceptanceCriteria_ChildEntityMapped_AndLegacyJsonColumnRemoved()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"reqac-meta-{Guid.NewGuid():N}")
            .Options;
        using var ctx = new McpDbContext(options);
        var entity = ctx.Model.FindEntityType(typeof(RequirementEntity));
        Assert.NotNull(entity);
        Assert.Null(entity!.FindProperty("AcceptanceCriteriaJson"));
        Assert.Null(entity.FindNavigation(nameof(RequirementEntity.AcceptanceCriteria)));

        Assert.NotNull(ctx.Model.FindEntityType(typeof(RequirementAcceptanceCriterionEntity)));
    }

    /// <summary>
    /// TEST-MCP-REQAC-001: acceptance criteria persisted as child rows round-trip the exact values
    /// (Id, Text, IsSatisfied, Evidence) and ordinal order on read.
    /// </summary>
    [Fact]
    public async Task RequirementAcceptanceCriteria_ChildRows_RoundTripAllFieldsInOrder()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"reqac-rt-{Guid.NewGuid():N}")
            .Options;

        await using (var write = new McpDbContext(options))
        {
            write.OverrideWorkspaceId("ws://test");
            write.Requirements.Add(new RequirementEntity
            {
                WorkspaceId = "ws://test",
                Kind = "test",
                Id = "TEST-REQAC-RT-001",
                Title = "RT",
                Body = "RT condition",
                Priority = "high",
                Status = "pending",
                Notes = null,
                CreatedAtUtc = DateTimeOffset.UtcNow.ToString("O"),
                UpdatedAtUtc = DateTimeOffset.UtcNow.ToString("O"),
            });
            write.RequirementAcceptanceCriteria.AddRange(
                new RequirementAcceptanceCriterionEntity { WorkspaceId = "ws://test", RequirementKind = "test", RequirementId = "TEST-REQAC-RT-001", Ordinal = 0, CriterionId = "ac-1", Text = "Multi-line\ncriterion text", IsSatisfied = false, Evidence = null },
                new RequirementAcceptanceCriterionEntity { WorkspaceId = "ws://test", RequirementKind = "test", RequirementId = "TEST-REQAC-RT-001", Ordinal = 1, CriterionId = "ac-2", Text = "Second criterion", IsSatisfied = true, Evidence = "validated by integration test" });
            await write.SaveChangesAsync().ConfigureAwait(true);
        }

        await using var read = new McpDbContext(options);
        read.OverrideWorkspaceId("ws://test");
        var loaded = await read.RequirementAcceptanceCriteria
            .Where(c => c.RequirementId == "TEST-REQAC-RT-001")
            .OrderBy(c => c.Ordinal)
            .ToListAsync().ConfigureAwait(true);
        Assert.Equal(2, loaded.Count);
        Assert.Equal("ac-1", loaded[0].CriterionId);
        Assert.Equal("Multi-line\ncriterion text", loaded[0].Text);
        Assert.False(loaded[0].IsSatisfied);
        Assert.Null(loaded[0].Evidence);
        Assert.Equal("ac-2", loaded[1].CriterionId);
        Assert.Equal("Second criterion", loaded[1].Text);
        Assert.True(loaded[1].IsSatisfied);
        Assert.Equal("validated by integration test", loaded[1].Evidence);
    }

    /// <summary>
    /// TEST-MCP-REQAC-002: a requirement with no acceptance criteria persists and reads back no
    /// child rows.
    /// </summary>
    [Fact]
    public async Task RequirementAcceptanceCriteria_NoCriteria_ReadsAsEmpty()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"reqac-empty-{Guid.NewGuid():N}")
            .Options;

        await using (var write = new McpDbContext(options))
        {
            write.OverrideWorkspaceId("ws://test");
            write.Requirements.Add(new RequirementEntity
            {
                WorkspaceId = "ws://test",
                Kind = "fr",
                Id = "FR-REQAC-EMPTY-001",
                Title = "Empty",
                Body = "Empty",
                Priority = "medium",
                Status = "pending",
                Notes = null,
                CreatedAtUtc = DateTimeOffset.UtcNow.ToString("O"),
                UpdatedAtUtc = DateTimeOffset.UtcNow.ToString("O"),
            });
            await write.SaveChangesAsync().ConfigureAwait(true);
        }

        await using var read = new McpDbContext(options);
        read.OverrideWorkspaceId("ws://test");
        var loaded = await read.RequirementAcceptanceCriteria
            .Where(c => c.RequirementId == "FR-REQAC-EMPTY-001")
            .ToListAsync().ConfigureAwait(true);
        Assert.Empty(loaded);
    }
}
