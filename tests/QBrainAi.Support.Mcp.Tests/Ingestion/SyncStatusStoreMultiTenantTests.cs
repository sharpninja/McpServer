using QBrainAi.Support.Mcp.Ingestion;
using Xunit;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Ingestion;

namespace QBrainAi.Support.Mcp.Tests.Ingestion;

/// <summary>Tests for workspace-keyed SyncStatusStore (TR-MCP-MT-004).</summary>
public sealed class SyncStatusStoreMultiTenantTests
{
    private readonly SyncStatusStore _store = new();

    [Fact]
    public void GetLastWorkspaceIdReturnsNullForUnknownWorkspace()
    {
        var result = _store.GetLast("C:\\unknown");
        Assert.Null(result);
    }

    [Fact]
    public void SetLastWorkspaceIdThenGetReturnsSameResult()
    {
        var expected = new SyncRunResult
        {
            RunId = "r1",
            Status = "Completed",
            StartedAt = DateTime.UtcNow,
            DocumentsIngested = 5
        };

        _store.SetLast("C:\\ws\\a", expected);

        var actual = _store.GetLast("C:\\ws\\a");
        Assert.NotNull(actual);
        Assert.Equal("r1", actual.RunId);
        Assert.Equal(5, actual.DocumentsIngested);
    }

    [Fact]
    public void SetLastWorkspaceADoesNotAffectWorkspaceB()
    {
        var resultA = new SyncRunResult { RunId = "a1", Status = "Completed", StartedAt = DateTime.UtcNow };
        var resultB = new SyncRunResult { RunId = "b1", Status = "Failed", StartedAt = DateTime.UtcNow };

        _store.SetLast("C:\\ws\\a", resultA);
        _store.SetLast("C:\\ws\\b", resultB);

        Assert.Equal("a1", _store.GetLast("C:\\ws\\a")!.RunId);
        Assert.Equal("b1", _store.GetLast("C:\\ws\\b")!.RunId);
    }

    [Fact]
    public void SetLastWorkspaceKeyedDoesNotAffectGlobal()
    {
        var global = new SyncRunResult { RunId = "g1", Status = "Completed", StartedAt = DateTime.UtcNow };
        var ws = new SyncRunResult { RunId = "w1", Status = "Failed", StartedAt = DateTime.UtcNow };

        _store.SetLast(global);
        _store.SetLast("C:\\ws\\a", ws);

        Assert.Equal("g1", _store.GetLast()!.RunId);
        Assert.Equal("w1", _store.GetLast("C:\\ws\\a")!.RunId);
    }

    [Fact]
    public void GetLastWorkspaceIdCaseInsensitive()
    {
        var result = new SyncRunResult { RunId = "ci", Status = "Completed", StartedAt = DateTime.UtcNow };
        _store.SetLast("C:\\Workspace\\Test", result);

        Assert.Equal("ci", _store.GetLast("c:\\workspace\\test")!.RunId);
    }

    [Fact]
    public void SetLastWorkspaceIdNullOrWhitespaceThrows()
    {
        var result = new SyncRunResult { RunId = "x", Status = "Completed", StartedAt = DateTime.UtcNow };

        Assert.Throws<ArgumentException>(() => _store.SetLast("", result));
        Assert.Throws<ArgumentException>(() => _store.SetLast(" ", result));
    }

    [Fact]
    public void GetLastWorkspaceIdNullOrWhitespaceThrows()
    {
        Assert.Throws<ArgumentException>(() => _store.GetLast(""));
        Assert.Throws<ArgumentException>(() => _store.GetLast(" "));
    }
}
