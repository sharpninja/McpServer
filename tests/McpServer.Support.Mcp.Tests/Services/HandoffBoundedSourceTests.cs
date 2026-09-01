using System.Text;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>TEST-HANDOFF-002: streamed Path and Artifact size bounds.</summary>
public sealed class HandoffBoundedSourceTests : IDisposable
{
    private readonly string _workspace;
    private readonly SqliteConnection _connection;
    private readonly McpDbContext _db;

    /// <summary>Creates an isolated workspace and SQLite database.</summary>
    public HandoffBoundedSourceTests()
    {
        _workspace = Path.Combine(Path.GetTempPath(), "handoff-bound", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workspace);
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _db = new McpDbContext(new DbContextOptionsBuilder<McpDbContext>().UseSqlite(_connection).Options, new WorkspaceContext { WorkspacePath = _workspace });
        _db.Database.EnsureCreated();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
        if (Directory.Exists(_workspace))
            Directory.Delete(_workspace, recursive: true);
    }

    /// <summary>P2-1: an oversized Path source fails before extraction.</summary>
    [Fact]
    public async Task ResolveAsync_OversizedPath_FailsClosed()
    {
        var path = Path.Combine(_workspace, "big.md");
        await File.WriteAllTextAsync(path, new string('x', HandoffPromptDefaults.MaxDecodedBytes + 1), TestContext.Current.CancellationToken);
        var sut = new HandoffSourceResolver(_db);
        var result = await sut.ResolveAsync(new HandoffIngestionRequest
        {
            SourceKind = HandoffSourceKind.Path,
            Path = "big.md",
        }, _workspace, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains(result.Diagnostics, item => item.Code is "source_oversized" || item.Code == HandoffErrorCodes.SourceOversized);
        Assert.True(string.IsNullOrEmpty(result.Text));
    }

    /// <summary>P2-1: artifact chunks that exceed 8 MiB fail before full concatenation.</summary>
    [Fact]
    public async Task ResolveAsync_OversizedArtifactChunks_FailsBeforeJoin()
    {
        _db.Documents.Add(new ContextDocumentEntity
        {
            Id = "artifact-oversize",
            WorkspaceId = _workspace,
            SourceType = "handoff",
            SourceKey = "artifact-oversize",
            IngestedAt = DateTime.UtcNow,
            ContentHash = "abc",
        });
        _db.Chunks.AddRange(
            new ContextChunkEntity { Id = "c1", WorkspaceId = _workspace, DocumentId = "artifact-oversize", Content = new string('a', HandoffPromptDefaults.MaxDecodedBytes / 2), ChunkIndex = 0 },
            new ContextChunkEntity { Id = "c2", WorkspaceId = _workspace, DocumentId = "artifact-oversize", Content = new string('b', (HandoffPromptDefaults.MaxDecodedBytes / 2) + 2), ChunkIndex = 1 });
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var sut = new HandoffSourceResolver(_db);
        var result = await sut.ResolveAsync(new HandoffIngestionRequest
        {
            SourceKind = HandoffSourceKind.Artifact,
            ArtifactId = "artifact-oversize",
        }, _workspace, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains(result.Diagnostics, item => item.Code == HandoffErrorCodes.SourceOversized);
        Assert.True(string.IsNullOrEmpty(result.Text));
    }

    /// <summary>P2-1: an artifact whose chunks sum to the limit succeeds.</summary>
    [Fact]
    public async Task ResolveAsync_ArtifactExactlyAtLimit_Succeeds()
    {
        var half = HandoffPromptDefaults.MaxDecodedBytes / 2;
        _db.Documents.Add(new ContextDocumentEntity
        {
            Id = "artifact-limit",
            WorkspaceId = _workspace,
            SourceType = "handoff",
            SourceKey = "artifact-limit",
            IngestedAt = DateTime.UtcNow,
            ContentHash = "def",
        });
        _db.Chunks.AddRange(
            new ContextChunkEntity { Id = "l1", WorkspaceId = _workspace, DocumentId = "artifact-limit", Content = new string('a', half), ChunkIndex = 0 },
            new ContextChunkEntity { Id = "l2", WorkspaceId = _workspace, DocumentId = "artifact-limit", Content = new string('b', HandoffPromptDefaults.MaxDecodedBytes - half), ChunkIndex = 1 });
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var sut = new HandoffSourceResolver(_db);
        var result = await sut.ResolveAsync(new HandoffIngestionRequest
        {
            SourceKind = HandoffSourceKind.Artifact,
            ArtifactId = "artifact-limit",
        }, _workspace, TestContext.Current.CancellationToken);

        Assert.True(result.Success, result.Diagnostics.FirstOrDefault()?.Message);
        Assert.Equal(HandoffPromptDefaults.MaxDecodedBytes, Encoding.UTF8.GetByteCount(result.Text!));
    }

    /// <summary>P2-1: cancellation during artifact streaming throws.</summary>
    [Fact]
    public async Task ResolveAsync_ArtifactCancelled_Throws()
    {
        _db.Documents.Add(new ContextDocumentEntity
        {
            Id = "artifact-cancel",
            WorkspaceId = _workspace,
            SourceType = "handoff",
            SourceKey = "artifact-cancel",
            IngestedAt = DateTime.UtcNow,
            ContentHash = "ghi",
        });
        _db.Chunks.Add(new ContextChunkEntity { Id = "x1", WorkspaceId = _workspace, DocumentId = "artifact-cancel", Content = "chunk", ChunkIndex = 0 });
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var sut = new HandoffSourceResolver(_db);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sut.ResolveAsync(new HandoffIngestionRequest
        {
            SourceKind = HandoffSourceKind.Artifact,
            ArtifactId = "artifact-cancel",
        }, _workspace, cts.Token));
    }
}
