using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>TR-PLANNED-013: Unit tests for TodoService CRUD operations and query filters.</summary>
public sealed class TodoServiceTests : IDisposable
{
    private readonly string _tempFile;
    private readonly IWriteAuditLog _auditLog = Substitute.For<IWriteAuditLog>();
    private readonly TodoService _sut;

    public TodoServiceTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"todo_test_{Guid.NewGuid():N}.yaml");
        File.WriteAllText(_tempFile, SampleYaml);
        _sut = new TodoService(_tempFile, _auditLog, NullLogger<TodoService>.Instance);
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
    public async Task UpdateAsync_ExistingId_UpdatesFields()
    {
        var request = new TodoUpdateRequest { Title = "Updated title", Done = true };

        var result = await _sut.UpdateAsync("TEST-001", request).ConfigureAwait(true);

        Assert.True(result.Success);
        var updated = await _sut.GetByIdAsync("TEST-001").ConfigureAwait(true);
        Assert.NotNull(updated);
        Assert.Equal("Updated title", updated.Title);
        Assert.True(updated.Done);
    }

    [Fact]
    public async Task DeleteAsync_ExistingId_RemovesFromYaml()
    {
        var result = await _sut.DeleteAsync("TEST-001").ConfigureAwait(true);

        Assert.True(result.Success);
        var deleted = await _sut.GetByIdAsync("TEST-001").ConfigureAwait(true);
        Assert.Null(deleted);
    }
}
