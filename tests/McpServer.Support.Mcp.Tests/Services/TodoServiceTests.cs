using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Notifications;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>TR-PLANNED-013: Unit tests for TodoService CRUD operations and query filters.</summary>
public sealed class TodoServiceTests : IDisposable
{
    private readonly string _tempFile;
    private readonly IWriteAuditLog _auditLog = Substitute.For<IWriteAuditLog>();
    private readonly IChangeEventBus _eventBus = Substitute.For<IChangeEventBus>();
    private readonly TodoService _sut;

    public TodoServiceTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"todo_test_{Guid.NewGuid():N}.yaml");
        File.WriteAllText(_tempFile, SampleYaml);
        _sut = new TodoService(_tempFile, _auditLog, NullLogger<TodoService>.Instance, _eventBus);
    }

    public void Dispose()
    {
        _sut.Dispose();
        if (File.Exists(_tempFile)) File.Delete(_tempFile);
    }

    private const string SampleYaml = """
        mvp-support:
          high-priority:
            - id: TEST-001
              title: "Test item one"
              done: false
              estimate: "2h"
              description:
                - "First test item"
              technical-details:
                - "Uses xUnit"
            - id: TEST-002
              title: "Test item two"
              done: true
          medium-priority:
            - id: TEST-003
              title: "Medium priority item"
              done: false
        mvp-app:
          high-priority:
            - id: APP-001
              title: "App item"
              done: false
        """;

    [Fact]
    public async Task QueryAsync_NoFilters_ReturnsAllItems()
    {
        var result = await _sut.QueryAsync(new TodoQueryRequest()).ConfigureAwait(true);

        Assert.True(result.TotalCount >= 4);
    }

    [Fact]
    public async Task QueryAsync_WithSectionFilter_ReturnsMatchingItems()
    {
        var result = await _sut.QueryAsync(new TodoQueryRequest { Section = "mvp-support" }).ConfigureAwait(true);

        Assert.All(result.Items, item => Assert.Equal("mvp-support", item.Section));
    }

    [Fact]
    public async Task QueryAsync_WithPriorityFilter_ReturnsMatchingItems()
    {
        var result = await _sut.QueryAsync(new TodoQueryRequest { Priority = "high" }).ConfigureAwait(true);

        Assert.All(result.Items, item => Assert.Equal("high", item.Priority));
    }

    [Fact]
    public async Task QueryAsync_WithDoneFilter_ReturnsMatchingItems()
    {
        var result = await _sut.QueryAsync(new TodoQueryRequest { Done = true }).ConfigureAwait(true);

        Assert.All(result.Items, item => Assert.True(item.Done));
        Assert.Contains(result.Items, item => item.Id == "TEST-002");
    }

    [Fact]
    public async Task QueryAsync_WithKeyword_SearchesTitleAndDescription()
    {
        var result = await _sut.QueryAsync(new TodoQueryRequest { Keyword = "medium" }).ConfigureAwait(true);

        Assert.Contains(result.Items, item => item.Id == "TEST-003");
    }

    [Fact]
    public async Task QueryAsync_WithBooleanKeyword_CanMatchAcrossFields()
    {
        var result = await _sut.QueryAsync(new TodoQueryRequest { Keyword = "\"Test item\" && xUnit" }).ConfigureAwait(true);

        var item = Assert.Single(result.Items);
        Assert.Equal("TEST-001", item.Id);
    }

    [Fact]
    public async Task GetByIdAsync_UnquotedColonInDescriptionItem_ParsesAsString()
    {
        const string yaml = """
            mvp-support:
              high-priority:
                - id: TEST-COLON-001
                  title: "Colon item"
                  done: false
                  description:
                    - Current implementation status: imported and indexed
            """;

        var tempFile = Path.Combine(Path.GetTempPath(), $"todo_test_{Guid.NewGuid():N}.yaml");
        await File.WriteAllTextAsync(tempFile, yaml).ConfigureAwait(true);

        try
        {
            using var sut = new TodoService(tempFile, _auditLog, NullLogger<TodoService>.Instance, _eventBus);
            var item = await sut.GetByIdAsync("TEST-COLON-001").ConfigureAwait(true);

            Assert.NotNull(item);
            Assert.Equal("Colon item", item.Title);
            Assert.Equal(["Current implementation status: imported and indexed"], item.Description);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task GetByIdAsync_LegacyFlatTodosFormat_IsImportedIntoSectionedModel()
    {
        const string yaml = """
            todos:
              - id: PLAN-AIVOICE-001
                title: Add voice commands and integrate BitNet-b1.58-sharp SLM
                section: Architecture
                priority: High
                estimate: 4-6 weeks
                status: Open
                phase: Planning
                description:
                  - Support natural trucker voice workflows
                technicalDetails:
                  - Keep deterministic parser as fallback
                implementationTasks:
                  - task: Define trucking voice intent taxonomy and entity schema
                    done: false
              - id: TODO-UI-NAV-001
                title: Group nav menu items
                section: UI
                priority: Medium
                status: Done
                completedDate: 2026-04-09
            """;

        var tempFile = Path.Combine(Path.GetTempPath(), $"todo_test_{Guid.NewGuid():N}.yaml");
        await File.WriteAllTextAsync(tempFile, yaml).ConfigureAwait(true);

        try
        {
            using var sut = new TodoService(tempFile, _auditLog, NullLogger<TodoService>.Instance, _eventBus);

            var architectureItem = await sut.GetByIdAsync("PLAN-AIVOICE-001").ConfigureAwait(true);
            Assert.NotNull(architectureItem);
            Assert.Equal("Architecture", architectureItem.Section);
            Assert.Equal("high", architectureItem.Priority);
            Assert.False(architectureItem.Done);
            Assert.Equal(["Keep deterministic parser as fallback"], architectureItem.TechnicalDetails);

            var uiItem = await sut.GetByIdAsync("TODO-UI-NAV-001").ConfigureAwait(true);
            Assert.NotNull(uiItem);
            Assert.Equal("UI", uiItem.Section);
            Assert.Equal("medium", uiItem.Priority);
            Assert.True(uiItem.Done);
            Assert.Equal("2026-04-09", uiItem.CompletedDate);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsItem()
    {
        var item = await _sut.GetByIdAsync("TEST-001").ConfigureAwait(true);

        Assert.NotNull(item);
        Assert.Equal("TEST-001", item.Id);
        Assert.Equal("Test item one", item.Title);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        var item = await _sut.GetByIdAsync("NONEXISTENT-999").ConfigureAwait(true);

        Assert.Null(item);
    }

    [Fact]
    public async Task CreateAsync_ValidItem_PersistsToYaml()
    {
        var request = new TodoCreateRequest
        {
            Id = "TEST-NEW-001",
            Title = "New test item",
            Section = "mvp-support",
            Priority = "low",
            Note = "created note",
            Remaining = "created remaining"
        };

        var result = await _sut.CreateAsync(request).ConfigureAwait(true);

        Assert.True(result.Success);
        var retrieved = await _sut.GetByIdAsync("TEST-NEW-001").ConfigureAwait(true);
        Assert.NotNull(retrieved);
        Assert.Equal("New test item", retrieved.Title);
        Assert.Equal("created note", retrieved.Note);
        Assert.Equal("created remaining", retrieved.Remaining);
        await _eventBus.Received(1).PublishAsync(
            Arg.Is<ChangeEvent>(e => e != null
                                     && e.Category == ChangeEventCategories.Todo
                                     && e.Action == ChangeEventActions.Created
                                     && e.EntityId == "TEST-NEW-001"),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Fact]
    public async Task CreateAsync_DuplicateId_ReturnsError()
    {
        var request = new TodoCreateRequest
        {
            Id = "TEST-001",
            Title = "Duplicate",
            Section = "mvp-support",
            Priority = "high"
        };

        var result = await _sut.CreateAsync(request).ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task CreateAsync_InvalidTodoId_ReturnsError()
    {
        var request = new TodoCreateRequest
        {
            Id = "bad-id",
            Title = "Invalid id",
            Section = "mvp-support",
            Priority = "high"
        };

        var result = await _sut.CreateAsync(request).ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Contains("Todo id must match", result.Error ?? string.Empty);
    }

    [Fact]
    public async Task CreateAsync_ValidThreeSegmentUppercaseId_Succeeds()
    {
        var request = new TodoCreateRequest
        {
            Id = "MCP-AUTH-001",
            Title = "Valid id",
            Section = "mvp-support",
            Priority = "medium"
        };

        var result = await _sut.CreateAsync(request).ConfigureAwait(true);

        Assert.True(result.Success);
        var stored = await _sut.GetByIdAsync("MCP-AUTH-001").ConfigureAwait(true);
        Assert.NotNull(stored);
    }

    [Theory]
    [InlineData("PHASE0-REMOTE-001")]
    [InlineData("MCP-TODO-CREATE-001")]
    public async Task CreateAsync_ValidUppercaseKebabIdWithDigitsAndExtraSegments_Succeeds(string id)
    {
        var request = new TodoCreateRequest
        {
            Id = id,
            Title = "Valid import id",
            Section = "mvp-support",
            Priority = "medium"
        };

        var result = await _sut.CreateAsync(request).ConfigureAwait(true);

        Assert.True(result.Success);
        var stored = await _sut.GetByIdAsync(id).ConfigureAwait(true);
        Assert.NotNull(stored);
        Assert.Equal(id, stored.Id);
    }

    [Fact]
    public async Task CreateAsync_ValidIssueNumberId_Succeeds()
    {
        var request = new TodoCreateRequest
        {
            Id = "ISSUE-42",
            Title = "GitHub-synced id",
            Section = "issues",
            Priority = "low"
        };

        var result = await _sut.CreateAsync(request).ConfigureAwait(true);

        Assert.True(result.Success);
        var stored = await _sut.GetByIdAsync("ISSUE-42").ConfigureAwait(true);
        Assert.NotNull(stored);
        Assert.Equal("GitHub-synced id", stored.Title);
    }

    [Fact]
    public async Task UpdateAsync_ExistingId_UpdatesFields()
    {
        var request = new TodoUpdateRequest { Title = "Updated title", Done = true };

        var result = await _sut.UpdateAsync("TEST-001", request).ConfigureAwait(true);

        Assert.True(result.Success);
        var updated = await _sut.GetByIdAsync("TEST-001").ConfigureAwait(true);
        Assert.NotNull(updated);
        Assert.Equal("Updated title", updated.Title);
        Assert.True(updated.Done);
        await _eventBus.Received(1).PublishAsync(
            Arg.Is<ChangeEvent>(e => e != null
                                     && e.Category == ChangeEventCategories.Todo
                                     && e.Action == ChangeEventActions.Updated
                                     && e.EntityId == "TEST-001"),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Fact]
    public async Task DeleteAsync_ExistingId_RemovesFromYaml()
    {
        var result = await _sut.DeleteAsync("TEST-001").ConfigureAwait(true);

        Assert.True(result.Success);
        var deleted = await _sut.GetByIdAsync("TEST-001").ConfigureAwait(true);
        Assert.Null(deleted);
        await _eventBus.Received(1).PublishAsync(
            Arg.Is<ChangeEvent>(e => e != null
                                     && e.Category == ChangeEventCategories.Todo
                                     && e.Action == ChangeEventActions.Deleted
                                     && e.EntityId == "TEST-001"),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Fact]
    public async Task UpdateAsync_InvalidDependsOnId_ReturnsError()
    {
        var result = await _sut.UpdateAsync("TEST-001", new TodoUpdateRequest
        {
            DependsOn = ["bad-id"]
        }).ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Contains("dependsOn contains invalid TODO id", result.Error ?? string.Empty);
    }
}
