using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

public sealed class SqliteTodoServiceTests : IDisposable
{
    private readonly string _tempDbPath;
    private readonly IWriteAuditLog _auditLog = Substitute.For<IWriteAuditLog>();
    private readonly SqliteTodoService _sut;

    public SqliteTodoServiceTests()
    {
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"todo_test_{Guid.NewGuid():N}.db");
        _sut = new SqliteTodoService(_tempDbPath, _auditLog, NullLogger<SqliteTodoService>.Instance);
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
            Id = "SQL-001",
            Title = "SQLite TODO",
            Section = "mvp-support",
            Priority = "high",
            Note = "sqlite note",
            Remaining = "sqlite remaining",
            Description = ["stored in sqlite"],
        }).ConfigureAwait(true);

        Assert.True(result.Success);

        var item = await _sut.GetByIdAsync("SQL-001").ConfigureAwait(true);
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
            Id = "SQL-002",
            Title = "Before",
            Section = "mvp-support",
            Priority = "medium",
        }).ConfigureAwait(true);

        var update = await _sut.UpdateAsync("SQL-002", new TodoUpdateRequest
        {
            Title = "After",
            Done = true,
            Priority = "low",
        }).ConfigureAwait(true);

        Assert.True(update.Success);
        var updated = await _sut.GetByIdAsync("SQL-002").ConfigureAwait(true);
        Assert.NotNull(updated);
        Assert.Equal("After", updated.Title);
        Assert.True(updated.Done);
        Assert.Equal("low", updated.Priority);

        var deleted = await _sut.DeleteAsync("SQL-002").ConfigureAwait(true);
        Assert.True(deleted.Success);
        Assert.Null(await _sut.GetByIdAsync("SQL-002").ConfigureAwait(true));
    }

    [Fact]
    public async Task Query_Filters_BySectionAndDone()
    {
        await _sut.CreateAsync(new TodoCreateRequest
        {
            Id = "SQL-003",
            Title = "Open item",
            Section = "mvp-app",
            Priority = "high",
        }).ConfigureAwait(true);

        await _sut.CreateAsync(new TodoCreateRequest
        {
            Id = "SQL-004",
            Title = "Done item",
            Section = "mvp-app",
            Priority = "high",
        }).ConfigureAwait(true);
        await _sut.UpdateAsync("SQL-004", new TodoUpdateRequest { Done = true }).ConfigureAwait(true);

        var result = await _sut.QueryAsync(new TodoQueryRequest
        {
            Section = "mvp-app",
            Done = true,
        }).ConfigureAwait(true);

        Assert.Single(result.Items);
        Assert.Equal("SQL-004", result.Items[0].Id);
    }
}
