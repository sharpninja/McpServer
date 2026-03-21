using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-096: Validates the new mixed-backend contract where SQLite is authoritative and the YAML
/// store sees SQLite mutations only through the projected TODO.yaml file. The tests use one shared temp
/// YAML file plus one SQLite database so projection replaces the old isolation assumption.
/// </summary>
public sealed class MixedTodoStorageIsolationTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _tempYamlPath;
    private readonly string _tempDbPath;
    private readonly TodoService _yamlStore;
    private readonly SqliteTodoService _sqliteStore;

    /// <summary>
    /// TEST-MCP-096: Creates an isolated temp workspace with one shared projected YAML file and one SQLite
    /// database so the YAML-backed reader can observe the SQLite-backed writer through file projection only.
    /// </summary>
    public MixedTodoStorageIsolationTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"mcp_mixed_{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(_tempRoot, "docs", "Project"));
        _tempYamlPath = Path.Combine(_tempRoot, "docs", "Project", "TODO.yaml");
        _tempDbPath = Path.Combine(_tempRoot, "todo.db");

        var audit = Substitute.For<IWriteAuditLog>();
        _yamlStore = new TodoService(_tempYamlPath, audit, NullLogger<TodoService>.Instance);
        _sqliteStore = new SqliteTodoService(_tempDbPath, _tempYamlPath, audit, NullLogger<SqliteTodoService>.Instance);
    }

    /// <summary>
    /// TEST-MCP-096: Disposes both stores and removes temp artifacts so later tests do not inherit projected
    /// file state from earlier mixed-backend assertions.
    /// </summary>
    public void Dispose()
    {
        _yamlStore.Dispose();
        _sqliteStore.Dispose();
        TryDelete(_tempRoot);
    }

    /// <summary>
    /// TEST-MCP-096: Verifies that SQLite create, update, and delete operations become visible to the YAML
    /// store through the shared projected TODO.yaml file. The fixture uses the same TODO id across all three
    /// mutations to prove the YAML view reflects authoritative DB state instead of diverging independently.
    /// </summary>
    [Fact]
    public async Task MixedBackends_SqliteProjection_UpdatesYamlView()
    {
        var created = await _sqliteStore.CreateAsync(new TodoCreateRequest
        {
            Id = "MIXED-TODO-001",
            Title = "SQLite item",
            Section = "mvp-app",
            Priority = "low",
        }).ConfigureAwait(true);

        Assert.True(created.Success);

        var yamlItem = await _yamlStore.GetByIdAsync("MIXED-TODO-001").ConfigureAwait(true);
        Assert.NotNull(yamlItem);
        Assert.Equal("SQLite item", yamlItem!.Title);
        Assert.Equal("mvp-app", yamlItem.Section);
        Assert.Equal("low", yamlItem.Priority);

        var updated = await _sqliteStore.UpdateAsync("MIXED-TODO-001", new TodoUpdateRequest
        {
            Title = "Projected update",
            Done = true,
            Priority = "high",
        }).ConfigureAwait(true);

        Assert.True(updated.Success);

        var refreshedYamlItem = await _yamlStore.GetByIdAsync("MIXED-TODO-001").ConfigureAwait(true);
        Assert.NotNull(refreshedYamlItem);
        Assert.Equal("Projected update", refreshedYamlItem!.Title);
        Assert.True(refreshedYamlItem.Done);
        Assert.Equal("high", refreshedYamlItem.Priority);

        var deleted = await _sqliteStore.DeleteAsync("MIXED-TODO-001").ConfigureAwait(true);
        Assert.True(deleted.Success);
        Assert.Null(await _yamlStore.GetByIdAsync("MIXED-TODO-001").ConfigureAwait(true));
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
            else if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // Best-effort cleanup for temp test artifacts.
        }
    }
}
