using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Notifications;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

public sealed class SqliteTodoServiceTests : IDisposable
{
    private readonly string _tempDbPath;
    private readonly IWriteAuditLog _auditLog = Substitute.For<IWriteAuditLog>();
    private readonly IChangeEventBus _eventBus = Substitute.For<IChangeEventBus>();
    private readonly SqliteTodoService _sut;

    public SqliteTodoServiceTests()
    {
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"todo_test_{Guid.NewGuid():N}.db");
        _sut = new SqliteTodoService(_tempDbPath, _auditLog, NullLogger<SqliteTodoService>.Instance, _eventBus);
    }

    public void Dispose()
    {
        _sut.Dispose();
        if (File.Exists(_tempDbPath))
            File.Delete(_tempDbPath);
    }

    [Fact]
    public async Task CreateAndGetById_Works()
    {
        var result = await _sut.CreateAsync(new TodoCreateRequest
        {
            Id = "SQL-TODO-001",
            Title = "SQLite TODO",
            Section = "mvp-support",
            Priority = "high",
            Note = "sqlite note",
            Remaining = "sqlite remaining",
            Description = ["stored in sqlite"],
        }).ConfigureAwait(true);

        Assert.True(result.Success);
        await _eventBus.Received(1).PublishAsync(
            Arg.Is<ChangeEvent>(e => e.Category == ChangeEventCategories.Todo
                                     && e.Action == ChangeEventActions.Created
                                     && e.EntityId == "SQL-TODO-001"),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);

        var item = await _sut.GetByIdAsync("SQL-TODO-001").ConfigureAwait(true);
        Assert.NotNull(item);
        Assert.Equal("SQLite TODO", item.Title);
        Assert.Equal("mvp-support", item.Section);
        Assert.Equal("high", item.Priority);
        Assert.Equal("sqlite note", item.Note);
        Assert.Equal("sqlite remaining", item.Remaining);
        Assert.Equal("stored in sqlite", item.Description![0]);
    }

    [Fact]
    public async Task UpdateAndDelete_Works()
    {
        await _sut.CreateAsync(new TodoCreateRequest
        {
            Id = "SQL-TODO-002",
            Title = "Before",
            Section = "mvp-support",
            Priority = "medium",
        }).ConfigureAwait(true);

        var update = await _sut.UpdateAsync("SQL-TODO-002", new TodoUpdateRequest
        {
            Title = "After",
            Done = true,
            Priority = "low",
        }).ConfigureAwait(true);

        Assert.True(update.Success);
        var updated = await _sut.GetByIdAsync("SQL-TODO-002").ConfigureAwait(true);
        Assert.NotNull(updated);
        Assert.Equal("After", updated.Title);
        Assert.True(updated.Done);
        Assert.Equal("low", updated.Priority);
        await _eventBus.Received(1).PublishAsync(
            Arg.Is<ChangeEvent>(e => e.Category == ChangeEventCategories.Todo
                                     && e.Action == ChangeEventActions.Updated
                                     && e.EntityId == "SQL-TODO-002"),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);

        var deleted = await _sut.DeleteAsync("SQL-TODO-002").ConfigureAwait(true);
        Assert.True(deleted.Success);
        Assert.Null(await _sut.GetByIdAsync("SQL-TODO-002").ConfigureAwait(true));
        await _eventBus.Received(1).PublishAsync(
            Arg.Is<ChangeEvent>(e => e.Category == ChangeEventCategories.Todo
                                     && e.Action == ChangeEventActions.Deleted
                                     && e.EntityId == "SQL-TODO-002"),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Fact]
    public async Task Query_Filters_BySectionAndDone()
    {
        await _sut.CreateAsync(new TodoCreateRequest
        {
            Id = "SQL-TODO-003",
            Title = "Open item",
            Section = "mvp-app",
            Priority = "high",
        }).ConfigureAwait(true);

        await _sut.CreateAsync(new TodoCreateRequest
        {
            Id = "SQL-TODO-004",
            Title = "Done item",
            Section = "mvp-app",
            Priority = "high",
        }).ConfigureAwait(true);
        await _sut.UpdateAsync("SQL-TODO-004", new TodoUpdateRequest { Done = true }).ConfigureAwait(true);

        var result = await _sut.QueryAsync(new TodoQueryRequest
        {
            Section = "mvp-app",
            Done = true,
        }).ConfigureAwait(true);

        Assert.Single(result.Items);
        Assert.Equal("SQL-TODO-004", result.Items[0].Id);
    }

    [Fact]
    public async Task Create_InvalidTodoId_ReturnsError()
    {
        var result = await _sut.CreateAsync(new TodoCreateRequest
        {
            Id = "sql-001",
            Title = "Invalid TODO ID",
            Section = "mvp-app",
            Priority = "high",
        }).ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Contains("Todo id must match", result.Error ?? string.Empty);
    }

    [Fact]
    public async Task Update_InvalidDependsOnId_ReturnsError()
    {
        await _sut.CreateAsync(new TodoCreateRequest
        {
            Id = "MCP-SQL-001",
            Title = "Base",
            Section = "mvp-app",
            Priority = "high",
        }).ConfigureAwait(true);

        var result = await _sut.UpdateAsync("MCP-SQL-001", new TodoUpdateRequest
        {
            DependsOn = ["not-valid"]
        }).ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Contains("dependsOn contains invalid TODO id", result.Error ?? string.Empty);
    }
}
