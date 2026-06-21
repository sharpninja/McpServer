using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Notifications;
using McpServer.Support.Mcp.Services;
using NSubstitute;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>TR-PLANNED-CORE-013: Unit tests for RepoFileService path security and audit.</summary>
public sealed class RepoFileServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly IWriteAuditLog _auditLog = Substitute.For<IWriteAuditLog>();
    private readonly IChangeEventBus _eventBus = Substitute.For<IChangeEventBus>();
    private readonly RepoFileService _sut;

    public RepoFileServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"repo_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        File.WriteAllText(Path.Combine(_tempDir, "readme.md"), "# Hello");
        Directory.CreateDirectory(Path.Combine(_tempDir, "src"));
        File.WriteAllText(Path.Combine(_tempDir, "src", "code.cs"), "class Foo {}");
        File.WriteAllText(Path.Combine(_tempDir, "src", "notes.txt"), "notes");
        Directory.CreateDirectory(Path.Combine(_tempDir, "src", "nested"));
        File.WriteAllText(Path.Combine(_tempDir, "src", "nested", "deep.cs"), "class Deep {}");

        var options = Microsoft.Extensions.Options.Options.Create(new IngestionOptions { RepoRoot = _tempDir });
        var workspaceContext = new WorkspaceContext();
        _sut = new RepoFileService(options, workspaceContext, _auditLog, NullLogger<RepoFileService>.Instance, _eventBus);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public async Task ReadAsync_AllowedPath_ReturnsContent()
    {
        var result = await _sut.ReadAsync("readme.md").ConfigureAwait(true);

        Assert.NotNull(result);
        Assert.True(result.Exists);
        Assert.Contains("Hello", result.Content);
    }

    [Fact]
    public async Task ReadAsync_PathTraversal_ReturnsNull()
    {
        var result = await _sut.ReadAsync("../../etc/passwd").ConfigureAwait(true);

        Assert.Null(result);
    }

    [Fact]
    public async Task ReadAsync_DisallowedPath_ReturnsNull()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new IngestionOptions
        {
            RepoRoot = _tempDir,
            RepoAllowlist = new[] { "*.md" }
        });
        var sut = new RepoFileService(options, new WorkspaceContext(), _auditLog, NullLogger<RepoFileService>.Instance);

        var result = await sut.ReadAsync("src/code.cs").ConfigureAwait(true);

        Assert.Null(result);
    }

    [Fact]
    public async Task ListAsync_ValidDirectory_ReturnsEntries()
    {
        var result = await _sut.ListAsync(".").ConfigureAwait(true);

        Assert.NotEmpty(result.Entries);
        Assert.Contains(result.Entries, e => e.Name == "readme.md");
        Assert.Contains(result.Entries, e => e.Name == "src" && e.IsDirectory);
    }

    [Fact]
    public async Task ListAsync_NonexistentPath_ReturnsEmpty()
    {
        var result = await _sut.ListAsync("nonexistent_dir").ConfigureAwait(true);

        Assert.Empty(result.Entries);
    }

    [Fact]
    public async Task WriteAsync_AllowedPath_WritesAndAudits()
    {
        var result = await _sut.WriteAsync("test_output.txt", "test content").ConfigureAwait(true);

        Assert.True(result.Written);
        Assert.True(File.Exists(Path.Combine(_tempDir, "test_output.txt")));
        _auditLog.Received(1).RecordWrite("test_output.txt", Arg.Any<DateTime>());
        await _eventBus.Received(1).PublishAsync(
            Arg.Is<ChangeEvent>(e => e != null
                                     && e.Category == ChangeEventCategories.Repo
                                     && e.Action == ChangeEventActions.Created
                                     && e.EntityId == "test_output.txt"),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Fact]
    public async Task WriteAsync_DisallowedPath_ReturnsFailure()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new IngestionOptions
        {
            RepoRoot = _tempDir,
            RepoAllowlist = new[] { "*.md" }
        });
        var sut = new RepoFileService(options, new WorkspaceContext(), _auditLog, NullLogger<RepoFileService>.Instance);

        var result = await sut.WriteAsync("secret.txt", "data").ConfigureAwait(true);

        Assert.False(result.Written);
    }

    [Fact]
    public async Task CaptureForWriteAsync_ExistingPath_ReturnsSnapshot()
    {
        var result = await _sut.CaptureForWriteAsync("readme.md").ConfigureAwait(true);

        Assert.NotNull(result);
        Assert.True(result.Exists);
        Assert.Equal("readme.md", result.RelativePath);
        Assert.Equal("# Hello", result.Content);
        Assert.False(string.IsNullOrWhiteSpace(result.ContentSha256));
    }

    [Fact]
    public async Task RestoreWriteAsync_ExistingSnapshot_RestoresPriorContent()
    {
        var snapshot = await _sut.CaptureForWriteAsync("readme.md").ConfigureAwait(true);
        Assert.NotNull(snapshot);
        var write = await _sut.WriteAsync("readme.md", "changed").ConfigureAwait(true);
        Assert.True(write.Written);

        await _sut.RestoreWriteAsync(snapshot!, "changed").ConfigureAwait(true);

        Assert.Equal("# Hello", File.ReadAllText(Path.Combine(_tempDir, "readme.md")));
    }

    [Fact]
    public async Task RestoreWriteAsync_NewFileSnapshot_DeletesCreatedFile()
    {
        var snapshot = await _sut.CaptureForWriteAsync("created.md").ConfigureAwait(true);
        Assert.NotNull(snapshot);
        var write = await _sut.WriteAsync("created.md", "created").ConfigureAwait(true);
        Assert.True(write.Written);

        await _sut.RestoreWriteAsync(snapshot!, "created").ConfigureAwait(true);

        Assert.False(File.Exists(Path.Combine(_tempDir, "created.md")));
    }

    [Fact]
    public async Task RestoreWriteAsync_WhenFileChangedAfterWrite_RefusesOverwrite()
    {
        var snapshot = await _sut.CaptureForWriteAsync("readme.md").ConfigureAwait(true);
        Assert.NotNull(snapshot);
        var write = await _sut.WriteAsync("readme.md", "transaction").ConfigureAwait(true);
        Assert.True(write.Written);
        File.WriteAllText(Path.Combine(_tempDir, "readme.md"), "human edit");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _sut.RestoreWriteAsync(snapshot!, "transaction"))
            .ConfigureAwait(true);

        Assert.Contains("changed after transactional write", ex.Message, StringComparison.Ordinal);
        Assert.Equal("human edit", File.ReadAllText(Path.Combine(_tempDir, "readme.md")));
    }

    /// <summary>FR-MCP-QBTOOLS-006: A unique oldString is replaced and the file content updated.</summary>
    [Fact]
    public async Task EditAsync_UniqueOccurrence_Replaces()
    {
        var result = await _sut.EditAsync("readme.md", "Hello", "World").ConfigureAwait(true);

        Assert.True(result.Written);
        Assert.Equal(1, result.Replacements);
        Assert.Equal("# World", File.ReadAllText(Path.Combine(_tempDir, "readme.md")));
    }

    /// <summary>FR-MCP-QBTOOLS-006: A missing oldString fails without changing the file.</summary>
    [Fact]
    public async Task EditAsync_MissingOldString_Fails()
    {
        var result = await _sut.EditAsync("readme.md", "absent", "x").ConfigureAwait(true);

        Assert.False(result.Written);
        Assert.Contains("not found", result.Error!, StringComparison.Ordinal);
        Assert.Equal("# Hello", File.ReadAllText(Path.Combine(_tempDir, "readme.md")));
    }

    /// <summary>FR-MCP-QBTOOLS-006: An ambiguous match fails unless replaceAll is set.</summary>
    [Fact]
    public async Task EditAsync_Ambiguous_FailsWithoutReplaceAll()
    {
        await _sut.WriteAsync("multi.txt", "a a a").ConfigureAwait(true);

        var result = await _sut.EditAsync("multi.txt", "a", "b").ConfigureAwait(true);

        Assert.False(result.Written);
        Assert.Contains("ambiguous", result.Error!, StringComparison.Ordinal);
        Assert.Equal("a a a", File.ReadAllText(Path.Combine(_tempDir, "multi.txt")));
    }

    /// <summary>FR-MCP-QBTOOLS-006: replaceAll replaces every occurrence and reports the count.</summary>
    [Fact]
    public async Task EditAsync_ReplaceAll_ReplacesEvery()
    {
        await _sut.WriteAsync("multi.txt", "a a a").ConfigureAwait(true);

        var result = await _sut.EditAsync("multi.txt", "a", "b", replaceAll: true).ConfigureAwait(true);

        Assert.True(result.Written);
        Assert.Equal(3, result.Replacements);
        Assert.Equal("b b b", File.ReadAllText(Path.Combine(_tempDir, "multi.txt")));
    }

    /// <summary>FR-MCP-QBTOOLS-006: An expectedOccurrences mismatch fails.</summary>
    [Fact]
    public async Task EditAsync_ExpectedOccurrencesMismatch_Fails()
    {
        await _sut.WriteAsync("multi.txt", "a a a").ConfigureAwait(true);

        var result = await _sut.EditAsync("multi.txt", "a", "b", replaceAll: true, expectedOccurrences: 2).ConfigureAwait(true);

        Assert.False(result.Written);
        Assert.Contains("expected 2", result.Error!, StringComparison.Ordinal);
    }

    /// <summary>FR-MCP-QBTOOLS-006: An empty oldString is rejected.</summary>
    [Fact]
    public async Task EditAsync_EmptyOldString_Fails()
    {
        var result = await _sut.EditAsync("readme.md", string.Empty, "x").ConfigureAwait(true);

        Assert.False(result.Written);
        Assert.Contains("must not be empty", result.Error!, StringComparison.Ordinal);
    }

    /// <summary>FR-MCP-QBTOOLS-006: Identical oldString and newString are rejected.</summary>
    [Fact]
    public async Task EditAsync_SameOldAndNew_Fails()
    {
        var result = await _sut.EditAsync("readme.md", "Hello", "Hello").ConfigureAwait(true);

        Assert.False(result.Written);
        Assert.Contains("must differ", result.Error!, StringComparison.Ordinal);
    }

    /// <summary>FR-MCP-QBTOOLS-006: Editing a nonexistent file fails.</summary>
    [Fact]
    public async Task EditAsync_NonexistentFile_Fails()
    {
        var result = await _sut.EditAsync("does-not-exist.txt", "a", "b").ConfigureAwait(true);

        Assert.False(result.Written);
        Assert.Contains("file not found", result.Error!, StringComparison.Ordinal);
    }

    /// <summary>FR-MCP-QBTOOLS-006: A path-traversal edit is rejected.</summary>
    [Fact]
    public async Task EditAsync_PathTraversal_Rejected()
    {
        var result = await _sut.EditAsync("../../etc/passwd", "a", "b").ConfigureAwait(true);

        Assert.False(result.Written);
        Assert.Contains("not allowed", result.Error!, StringComparison.Ordinal);
    }

    /// <summary>FR-MCP-QBTOOLS-006: A disallowed path is rejected under an allowlist.</summary>
    [Fact]
    public async Task EditAsync_DisallowedPath_Rejected()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new IngestionOptions
        {
            RepoRoot = _tempDir,
            RepoAllowlist = new[] { "*.md" }
        });
        var sut = new RepoFileService(options, new WorkspaceContext(), _auditLog, NullLogger<RepoFileService>.Instance);

        var result = await sut.EditAsync("src/code.cs", "Foo", "Bar").ConfigureAwait(true);

        Assert.False(result.Written);
    }

    /// <summary>FR-MCP-QBTOOLS-006: A successful edit records an audit entry and publishes an Updated event.</summary>
    [Fact]
    public async Task EditAsync_Success_AuditsAndPublishesUpdated()
    {
        var result = await _sut.EditAsync("readme.md", "Hello", "World").ConfigureAwait(true);

        Assert.True(result.Written);
        _auditLog.Received(1).RecordWrite("readme.md", Arg.Any<DateTime>());
        await _eventBus.Received(1).PublishAsync(
            Arg.Is<ChangeEvent>(e => e != null
                                     && e.Category == ChangeEventCategories.Repo
                                     && e.Action == ChangeEventActions.Updated
                                     && e.EntityId == "readme.md"),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Fact]
    public async Task ReadAsync_GlobAllowlist_RequiresActualPatternMatch()
    {
        var allowedRoot = Path.Combine(_tempDir, "src", "McpServer.Cqrs");
        Directory.CreateDirectory(allowedRoot);
        File.WriteAllText(Path.Combine(allowedRoot, "inside.cs"), "class Inside {}");

        var prefixLookalikeRoot = Path.Combine(_tempDir, "src", "McpServer.Cqrs.Bad");
        Directory.CreateDirectory(prefixLookalikeRoot);
        File.WriteAllText(Path.Combine(prefixLookalikeRoot, "escape.cs"), "class Escape {}");

        var options = Microsoft.Extensions.Options.Options.Create(new IngestionOptions
        {
            RepoRoot = _tempDir,
            RepoAllowlist = new[] { "src/McpServer.Cqrs/**/*.cs" }
        });
        var sut = new RepoFileService(options, new WorkspaceContext(), _auditLog, NullLogger<RepoFileService>.Instance);

        var allowed = await sut.ReadAsync("src/McpServer.Cqrs/inside.cs").ConfigureAwait(true);
        var disallowed = await sut.ReadAsync("src/McpServer.Cqrs.Bad/escape.cs").ConfigureAwait(true);

        Assert.NotNull(allowed);
        Assert.Null(disallowed);
    }

    [Fact]
    public async Task ReadAsync_GlobAllowlist_DeniesExtensionMismatchWithinAllowedTree()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new IngestionOptions
        {
            RepoRoot = _tempDir,
            RepoAllowlist = new[] { "src/**/*.cs" }
        });
        var sut = new RepoFileService(options, new WorkspaceContext(), _auditLog, NullLogger<RepoFileService>.Instance);

        var allowed = await sut.ReadAsync("src/nested/deep.cs").ConfigureAwait(true);
        var disallowed = await sut.ReadAsync("src/notes.txt").ConfigureAwait(true);

        Assert.NotNull(allowed);
        Assert.Null(disallowed);
    }

    [Fact]
    public async Task ListAsync_GlobAllowlist_RootIncludesRelevantAncestorDirectoriesOnly()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new IngestionOptions
        {
            RepoRoot = _tempDir,
            RepoAllowlist = new[] { "src/**/*.cs" }
        });
        var sut = new RepoFileService(options, new WorkspaceContext(), _auditLog, NullLogger<RepoFileService>.Instance);

        var root = await sut.ListAsync(".").ConfigureAwait(true);
        var src = await sut.ListAsync("src").ConfigureAwait(true);

        Assert.Contains(root.Entries, entry => entry.Name == "src" && entry.IsDirectory);
        Assert.DoesNotContain(root.Entries, entry => entry.Name == "readme.md");
        Assert.Contains(src.Entries, entry => entry.Name == "code.cs" && !entry.IsDirectory);
        Assert.Contains(src.Entries, entry => entry.Name == "nested" && entry.IsDirectory);
        Assert.DoesNotContain(src.Entries, entry => entry.Name == "notes.txt");
    }

    [Fact]
    public async Task ReadAsync_PathThroughSymlinkOutsideRepo_ReturnsNull()
    {
        var outsideDir = Path.Combine(Path.GetTempPath(), $"repo_escape_{Guid.NewGuid():N}");
        Directory.CreateDirectory(outsideDir);
        File.WriteAllText(Path.Combine(outsideDir, "outside.cs"), "class Outside {}");

        var linkPath = Path.Combine(_tempDir, "linked-outside");
        try
        {
            if (!TryCreateDirectoryLink(linkPath, outsideDir))
                return;

            var options = Microsoft.Extensions.Options.Options.Create(new IngestionOptions
            {
                RepoRoot = _tempDir,
                RepoAllowlist = new[] { "linked-outside/**/*.cs" }
            });
            var sut = new RepoFileService(options, new WorkspaceContext(), _auditLog, NullLogger<RepoFileService>.Instance);

            var result = await sut.ReadAsync("linked-outside/outside.cs").ConfigureAwait(true);

            Assert.Null(result);
        }
        finally
        {
            try
            {
                if (Directory.Exists(linkPath))
                    Directory.Delete(linkPath);
            }
            catch
            {
            }

            if (Directory.Exists(outsideDir))
                Directory.Delete(outsideDir, recursive: true);
        }
    }

    private static bool TryCreateDirectoryLink(string linkPath, string targetPath)
    {
        try
        {
            Directory.CreateSymbolicLink(linkPath, targetPath);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (PlatformNotSupportedException)
        {
            return false;
        }
    }
}
