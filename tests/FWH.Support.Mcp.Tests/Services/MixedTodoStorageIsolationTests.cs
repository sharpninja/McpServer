using FWH.Support.Mcp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace FWH.Support.Mcp.Tests.Services;

public sealed class MixedTodoStorageIsolationTests : IDisposable
{
    private readonly string _tempYamlPath;
    private readonly string _tempDbPath;
    private readonly TodoService _yamlStore;
    private readonly SqliteTodoService _sqliteStore;

    public MixedTodoStorageIsolationTests()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"mcp_mixed_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        _tempYamlPath = Path.Combine(tempRoot, "todo.yaml");
        _tempDbPath = Path.Combine(tempRoot, "todo.db");

        var audit = Substitute.For<IWriteAuditLog>();
        _yamlStore = new TodoService(_tempYamlPath, audit, NullLogger<TodoService>.Instance);
        _sqliteStore = new SqliteTodoService(_tempDbPath, audit, NullLogger<SqliteTodoService>.Instance);
    }

    public void Dispose()
    {
        _yamlStore.Dispose();
        _sqliteStore.Dispose();
        TryDelete(_tempYamlPath);
        TryDelete(_tempDbPath);
        TryDelete(Path.GetDirectoryName(_tempYamlPath)!);
    }

    [Fact]
    public async Task MixedBackends_ConcurrentWrites_AreIsolated()
    {
        var yamlCreate = _yamlStore.CreateAsync(new TodoCreateRequest
        {
            Id = "MIXED-001",
            Title = "YAML item",
            Section = "mvp-support",
            Priority = "high",
        });

        var sqliteCreate = _sqliteStore.CreateAsync(new TodoCreateRequest
        {
            Id = "MIXED-001",
            Title = "SQLite item",
            Section = "mvp-app",
            Priority = "low",
        });

        await Task.WhenAll(yamlCreate, sqliteCreate).ConfigureAwait(true);

        var yamlItem = await _yamlStore.GetByIdAsync("MIXED-001").ConfigureAwait(true);
        var sqliteItem = await _sqliteStore.GetByIdAsync("MIXED-001").ConfigureAwait(true);

        Assert.NotNull(yamlItem);
        Assert.NotNull(sqliteItem);
        Assert.Equal("YAML item", yamlItem!.Title);
        Assert.Equal("SQLite item", sqliteItem!.Title);
        Assert.Equal("mvp-support", yamlItem.Section);
        Assert.Equal("mvp-app", sqliteItem.Section);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
            if (Directory.Exists(path)) Directory.Delete(path, true);
        }
        catch
        {
            // Best-effort cleanup for temp test artifacts.
        }
    }
}
