using System.Text.Json;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Requirements;
using McpServer.Support.Mcp.Requirements.Models;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// FR-MCP-REQAC-001 / TR-MCP-REQAC-001 / TEST-MCP-REQAC-001..002 (Gate 2):
/// validates the persistence layer for the new <c>AcceptanceCriteriaJson</c> column on
/// <see cref="RequirementEntity"/> and the serialization round-trip used by
/// <c>RequirementsDatabaseDocumentService</c>. The fixture uses an in-memory EF
/// context so each test runs in isolation without touching the live database.
/// </summary>
public sealed class RequirementAcceptanceCriteriaTests
{
    /// <summary>EF should expose a nullable <c>AcceptanceCriteriaJson</c> string property on the entity.</summary>
    [Fact]
    public void RequirementEntity_HasNullableAcceptanceCriteriaJsonColumn()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"reqac-meta-{Guid.NewGuid():N}")
            .Options;
        using var ctx = new McpDbContext(options);
        var entity = ctx.Model.FindEntityType(typeof(RequirementEntity));
        Assert.NotNull(entity);
        var property = entity!.FindProperty("AcceptanceCriteriaJson");
        Assert.NotNull(property);
        Assert.True(property!.IsNullable);
        Assert.Equal(typeof(string), property.ClrType);
    }

    /// <summary>
    /// TEST-MCP-REQAC-001: an entity persisted with structured acceptance criteria round-trips
    /// the exact <see cref="AcceptanceCriterion"/> values (Id, Text, IsSatisfied, Evidence) on
    /// read. Mirrors the serialize/deserialize contract used by the production DB service.
    /// </summary>
    [Fact]
    public async Task RequirementEntity_AcceptanceCriteriaJson_RoundTripsAllFields()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"reqac-rt-{Guid.NewGuid():N}")
            .Options;
        var criteria = new List<AcceptanceCriterion>
        {
            new() { Id = "ac-1", Text = "Multi-line\ncriterion text", IsSatisfied = false, Evidence = null },
            new() { Id = "ac-2", Text = "Second criterion", IsSatisfied = true, Evidence = "validated by integration test" },
        };
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        await using (var write = new McpDbContext(options))
        {
            write.OverrideWorkspaceId("ws://test");
            write.Requirements.Add(new RequirementEntity
            {
                WorkspaceId = "ws://test",
                Kind = "test",
                Id = "TEST-REQAC-RT-001",
                Title = "RT title",
                Body = "RT condition",
                Priority = "high",
                Status = "pending",
                Notes = null,
                AcceptanceCriteriaJson = JsonSerializer.Serialize(criteria, jsonOptions),
                CreatedAtUtc = DateTimeOffset.UtcNow.ToString("O"),
                UpdatedAtUtc = DateTimeOffset.UtcNow.ToString("O")
            });
            await write.SaveChangesAsync().ConfigureAwait(true);
        }

        await using var read = new McpDbContext(options);
        read.OverrideWorkspaceId("ws://test");
        var row = await read.Requirements.FirstAsync(x => x.Id == "TEST-REQAC-RT-001").ConfigureAwait(true);
        Assert.NotNull(row.AcceptanceCriteriaJson);
        var loaded = JsonSerializer.Deserialize<List<AcceptanceCriterion>>(row.AcceptanceCriteriaJson!, jsonOptions);
        Assert.NotNull(loaded);
        Assert.Equal(2, loaded!.Count);
        Assert.Equal(criteria[0].Id, loaded[0].Id);
        Assert.Equal(criteria[0].Text, loaded[0].Text);
        Assert.Equal(criteria[0].IsSatisfied, loaded[0].IsSatisfied);
        Assert.Equal(criteria[0].Evidence, loaded[0].Evidence);
        Assert.Equal(criteria[1].Id, loaded[1].Id);
        Assert.Equal(criteria[1].Text, loaded[1].Text);
        Assert.Equal(criteria[1].IsSatisfied, loaded[1].IsSatisfied);
        Assert.Equal(criteria[1].Evidence, loaded[1].Evidence);
    }

    /// <summary>
    /// TEST-MCP-REQAC-002: a null <c>AcceptanceCriteriaJson</c> column value is treated as an
    /// empty list of acceptance criteria with no nulls leaking to callers.
    /// </summary>
    [Fact]
    public async Task RequirementEntity_NullAcceptanceCriteriaJson_DeserializesAsEmptyList()
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
                AcceptanceCriteriaJson = null,
                CreatedAtUtc = DateTimeOffset.UtcNow.ToString("O"),
                UpdatedAtUtc = DateTimeOffset.UtcNow.ToString("O")
            });
            await write.SaveChangesAsync().ConfigureAwait(true);
        }

        await using var read = new McpDbContext(options);
        read.OverrideWorkspaceId("ws://test");
        var row = await read.Requirements.FirstAsync(x => x.Id == "FR-REQAC-EMPTY-001").ConfigureAwait(true);
        Assert.Null(row.AcceptanceCriteriaJson);
        // Production service deserializer treats null/blank as empty list - mirror that here.
        IReadOnlyList<AcceptanceCriterion> loaded = string.IsNullOrWhiteSpace(row.AcceptanceCriteriaJson)
            ? []
            : JsonSerializer.Deserialize<List<AcceptanceCriterion>>(row.AcceptanceCriteriaJson!, new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? [];
        Assert.NotNull(loaded);
        Assert.Empty(loaded);
    }

    /// <summary>
    /// TEST-MCP-REQAC-003: rendering a TEST entry with criteria emits a deterministic
    /// "Acceptance Criteria" block with checklist markers and optional evidence.
    /// </summary>
    [Fact]
    public void RenderTesting_WithCriteria_EmitsAcceptanceCriteriaBlock()
    {
        var entry = new TestEntry(
            "TEST-REQAC-REND-001",
            "Condition body",
            Title: "Render test",
            AcceptanceCriteria: new[]
            {
                new AcceptanceCriterion { Id = "ac-1", Text = "First criterion", IsSatisfied = false },
                new AcceptanceCriterion { Id = "ac-2", Text = "Second criterion", IsSatisfied = true, Evidence = "TodoMarkdownPreservationTests" },
            });
        var rendered = RequirementsDocumentRenderer.RenderTesting([entry]);
        Assert.Contains("- TEST-REQAC-REND-001: Condition body", rendered, StringComparison.Ordinal);
        Assert.Contains("**Acceptance Criteria:**", rendered, StringComparison.Ordinal);
        Assert.Contains("- [ ] First criterion", rendered, StringComparison.Ordinal);
        Assert.Contains("- [x] Second criterion (evidence: TodoMarkdownPreservationTests)", rendered, StringComparison.Ordinal);
    }

    /// <summary>
    /// Renderer must omit the Acceptance Criteria block entirely for null/empty criteria so the
    /// existing markdown shape is preserved for entries that have no criteria.
    /// </summary>
    [Fact]
    public void RenderTesting_WithoutCriteria_OmitsBlock()
    {
        var entry = new TestEntry("TEST-REQAC-REND-002", "Condition only", Title: "No criteria");
        var rendered = RequirementsDocumentRenderer.RenderTesting([entry]);
        Assert.Contains("- TEST-REQAC-REND-002: Condition only", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("Acceptance Criteria", rendered, StringComparison.Ordinal);
    }
}
