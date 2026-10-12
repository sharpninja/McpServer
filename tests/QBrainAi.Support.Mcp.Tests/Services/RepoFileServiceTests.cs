using QBrainAi.Support.Mcp.Ingestion;
using QBrainAi.Support.Mcp.Notifications;
using QBrainAi.Support.Mcp.Services;
using NSubstitute;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using QBrainAi.Support.Mcp.Storage.Services;

namespace QBrainAi.Support.Mcp.Tests.Services;

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
    public async Task ReadAsyncAllowedPathReturnsContent()
    {
        var result = await _sut.ReadAsync("readme.md", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.NotNull(result);
        Assert.True(result.Exists);
        Assert.Contains("Hello", result.Content);
    }

    [Fact]
    public async Task ReadAsyncPathTraversalReturnsNull()
    {
        var result = await _sut.ReadAsync("../../etc/passwd", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Null(result);
    }

    /// <summary>
    /// TR-MCP-QUALITY-001: Rooted paths outside the active workspace are rejected
    /// before any repository read, list, write, edit, or transactional capture.
    /// </summary>
    [Fact]
    public async Task RootedExternalPathAcrossRepoOperationsIsRejected()
    {
        var outsideDir = Path.Combine(Path.GetTempPath(), $"repo_external_{Guid.NewGuid():N}");
        Directory.CreateDirectory(outsideDir);
        var outsideFile = Path.Combine(outsideDir, "outside.txt");
        await File.WriteAllTextAsync(outsideFile, "outside", TestContext.Current.CancellationToken).ConfigureAwait(true);
        try
        {
            var read = await _sut.ReadAsync(outsideFile, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
            var list = await _sut.ListAsync(outsideDir, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
            var write = await _sut.WriteAsync(outsideFile, "changed", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
            var edit = await _sut.EditAsync(outsideFile, "outside", "changed", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
            var snapshot = await _sut.CaptureForWriteAsync(outsideFile, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

            Assert.Null(read);
            Assert.Empty(list.Entries);
            Assert.False(write.Written);
            Assert.False(edit.Written);
            Assert.Null(snapshot);
            Assert.Equal("outside", await File.ReadAllTextAsync(outsideFile, TestContext.Current.CancellationToken).ConfigureAwait(true));
        }
        finally
        {
            Directory.Delete(outsideDir, recursive: true);
        }
    }

    [Fact]
    public async Task ReadAsyncDisallowedPathReturnsNull()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new IngestionOptions
        {
            RepoRoot = _tempDir,
            RepoAllowlist = new[] { "*.md" }
        });
        var sut = new RepoFileService(options, new WorkspaceContext(), _auditLog, NullLogger<RepoFileService>.Instance);

        var result = await sut.ReadAsync("src/code.cs", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Null(result);
    }

    [Fact]
    public async Task ListAsyncValidDirectoryReturnsEntries()
    {
        var result = await _sut.ListAsync(".", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.NotEmpty(result.Entries);
        Assert.Contains(result.Entries, e => e.Name == "readme.md");
        Assert.Contains(result.Entries, e => e.Name == "src" && e.IsDirectory);
    }

    [Fact]
    public async Task ListAsyncNonexistentPathReturnsEmpty()
    {
        var result = await _sut.ListAsync("nonexistent_dir", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Empty(result.Entries);
    }

    [Fact]
    public async Task WriteAsyncAllowedPathWritesAndAudits()
    {
        var result = await _sut.WriteAsync("test_output.txt", "test content", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

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
    public async Task WriteAsyncDisallowedPathReturnsFailure()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new IngestionOptions
        {
            RepoRoot = _tempDir,
            RepoAllowlist = new[] { "*.md" }
        });
        var sut = new RepoFileService(options, new WorkspaceContext(), _auditLog, NullLogger<RepoFileService>.Instance);

        var result = await sut.WriteAsync("secret.txt", "data", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(result.Written);
    }

    [Fact]
    public async Task CaptureForWriteAsyncExistingPathReturnsSnapshot()
    {
        var result = await _sut.CaptureForWriteAsync("readme.md", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.NotNull(result);
        Assert.True(result.Exists);
        Assert.Equal("readme.md", result.RelativePath);
        Assert.Equal("# Hello", result.Content);
        Assert.False(string.IsNullOrWhiteSpace(result.ContentSha256));
    }

    [Fact]
    public async Task RestoreWriteAsyncExistingSnapshotRestoresPriorContent()
    {
        var snapshot = await _sut.CaptureForWriteAsync("readme.md", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.NotNull(snapshot);
        var write = await _sut.WriteAsync("readme.md", "changed", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(write.Written);

        await _sut.RestoreWriteAsync(snapshot!, "changed", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal("# Hello", File.ReadAllText(Path.Combine(_tempDir, "readme.md")));
    }

    [Fact]
    public async Task RestoreWriteAsyncNewFileSnapshotDeletesCreatedFile()
    {
        var snapshot = await _sut.CaptureForWriteAsync("created.md", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.NotNull(snapshot);
        var write = await _sut.WriteAsync("created.md", "created", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(write.Written);

        await _sut.RestoreWriteAsync(snapshot!, "created", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(File.Exists(Path.Combine(_tempDir, "created.md")));
    }

    [Fact]
    public async Task RestoreWriteAsyncWhenFileChangedAfterWriteRefusesOverwrite()
    {
        var snapshot = await _sut.CaptureForWriteAsync("readme.md", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.NotNull(snapshot);
        var write = await _sut.WriteAsync("readme.md", "transaction", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(write.Written);
        File.WriteAllText(Path.Combine(_tempDir, "readme.md"), "human edit");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _sut.RestoreWriteAsync(snapshot!, "transaction", cancellationToken: TestContext.Current.CancellationToken))
            .ConfigureAwait(true);

        Assert.Contains("changed after transactional write", ex.Message, StringComparison.Ordinal);
        Assert.Equal("human edit", File.ReadAllText(Path.Combine(_tempDir, "readme.md")));
    }

    /// <summary>FR-MCP-QBTOOLS-006: A unique oldString is replaced and the file content updated.</summary>
    [Fact]
    public async Task EditAsyncUniqueOccurrenceReplaces()
    {
        var result = await _sut.EditAsync("readme.md", "Hello", "World", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Written);
        Assert.Equal(1, result.Replacements);
        Assert.Equal("# World", File.ReadAllText(Path.Combine(_tempDir, "readme.md")));
    }

    /// <summary>FR-MCP-QBTOOLS-006: A missing oldString fails without changing the file.</summary>
    [Fact]
    public async Task EditAsyncMissingOldStringFails()
    {
        var result = await _sut.EditAsync("readme.md", "absent", "x", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(result.Written);
        Assert.Contains("not found", result.Error!, StringComparison.Ordinal);
        Assert.Equal("# Hello", File.ReadAllText(Path.Combine(_tempDir, "readme.md")));
    }

    /// <summary>FR-MCP-QBTOOLS-006: An ambiguous match fails unless replaceAll is set.</summary>
    [Fact]
    public async Task EditAsyncAmbiguousFailsWithoutReplaceAll()
    {
        await _sut.WriteAsync("multi.txt", "a a a", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var result = await _sut.EditAsync("multi.txt", "a", "b", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(result.Written);
        Assert.Contains("ambiguous", result.Error!, StringComparison.Ordinal);
        Assert.Equal("a a a", File.ReadAllText(Path.Combine(_tempDir, "multi.txt")));
    }

    /// <summary>FR-MCP-QBTOOLS-006: replaceAll replaces every occurrence and reports the count.</summary>
    [Fact]
    public async Task EditAsyncReplaceAllReplacesEvery()
    {
        await _sut.WriteAsync("multi.txt", "a a a", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var result = await _sut.EditAsync("multi.txt", "a", "b", replaceAll: true, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Written);
        Assert.Equal(3, result.Replacements);
        Assert.Equal("b b b", File.ReadAllText(Path.Combine(_tempDir, "multi.txt")));
    }

    /// <summary>FR-MCP-QBTOOLS-006: An expectedOccurrences mismatch fails.</summary>
    [Fact]
    public async Task EditAsyncExpectedOccurrencesMismatchFails()
    {
        await _sut.WriteAsync("multi.txt", "a a a", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var result = await _sut.EditAsync("multi.txt", "a", "b", replaceAll: true, expectedOccurrences: 2, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(result.Written);
        Assert.Contains("expected 2", result.Error!, StringComparison.Ordinal);
    }

    /// <summary>FR-MCP-QBTOOLS-006: An empty oldString is rejected.</summary>
    [Fact]
    public async Task EditAsyncEmptyOldStringFails()
    {
        var result = await _sut.EditAsync("readme.md", string.Empty, "x", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(result.Written);
        Assert.Contains("must not be empty", result.Error!, StringComparison.Ordinal);
    }

    /// <summary>FR-MCP-QBTOOLS-006: Identical oldString and newString are rejected.</summary>
    [Fact]
    public async Task EditAsyncSameOldAndNewFails()
    {
        var result = await _sut.EditAsync("readme.md", "Hello", "Hello", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(result.Written);
        Assert.Contains("must differ", result.Error!, StringComparison.Ordinal);
    }

    /// <summary>FR-MCP-QBTOOLS-006: Editing a nonexistent file fails.</summary>
    [Fact]
    public async Task EditAsyncNonexistentFileFails()
    {
        var result = await _sut.EditAsync("does-not-exist.txt", "a", "b", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(result.Written);
        Assert.Contains("file not found", result.Error!, StringComparison.Ordinal);
    }

    /// <summary>FR-MCP-QBTOOLS-006: A path-traversal edit is rejected.</summary>
    [Fact]
    public async Task EditAsyncPathTraversalRejected()
    {
        var result = await _sut.EditAsync("../../etc/passwd", "a", "b", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(result.Written);
        Assert.Contains("not allowed", result.Error!, StringComparison.Ordinal);
    }

    /// <summary>FR-MCP-QBTOOLS-006: A disallowed path is rejected under an allowlist.</summary>
    [Fact]
    public async Task EditAsyncDisallowedPathRejected()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new IngestionOptions
        {
            RepoRoot = _tempDir,
            RepoAllowlist = new[] { "*.md" }
        });
        var sut = new RepoFileService(options, new WorkspaceContext(), _auditLog, NullLogger<RepoFileService>.Instance);

        var result = await sut.EditAsync("src/code.cs", "Foo", "Bar", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(result.Written);
    }

    /// <summary>FR-MCP-QBTOOLS-006: A successful edit records an audit entry and publishes an Updated event.</summary>
    [Fact]
    public async Task EditAsyncSuccessAuditsAndPublishesUpdated()
    {
        var result = await _sut.EditAsync("readme.md", "Hello", "World", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

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
    public async Task ReadAsyncGlobAllowlistRequiresActualPatternMatch()
    {
        var allowedRoot = Path.Combine(_tempDir, "src", "QBrainAi.Cqrs");
        Directory.CreateDirectory(allowedRoot);
        File.WriteAllText(Path.Combine(allowedRoot, "inside.cs"), "class Inside {}");

        var prefixLookalikeRoot = Path.Combine(_tempDir, "src", "QBrainAi.Cqrs.Bad");
        Directory.CreateDirectory(prefixLookalikeRoot);
        File.WriteAllText(Path.Combine(prefixLookalikeRoot, "escape.cs"), "class Escape {}");

        var options = Microsoft.Extensions.Options.Options.Create(new IngestionOptions
        {
            RepoRoot = _tempDir,
            RepoAllowlist = new[] { "src/QBrainAi.Cqrs/**/*.cs" }
        });
        var sut = new RepoFileService(options, new WorkspaceContext(), _auditLog, NullLogger<RepoFileService>.Instance);

        var allowed = await sut.ReadAsync("src/QBrainAi.Cqrs/inside.cs", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var disallowed = await sut.ReadAsync("src/QBrainAi.Cqrs.Bad/escape.cs", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.NotNull(allowed);
        Assert.Null(disallowed);
    }

    [Fact]
    public async Task ReadAsyncGlobAllowlistDeniesExtensionMismatchWithinAllowedTree()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new IngestionOptions
        {
            RepoRoot = _tempDir,
            RepoAllowlist = new[] { "src/**/*.cs" }
        });
        var sut = new RepoFileService(options, new WorkspaceContext(), _auditLog, NullLogger<RepoFileService>.Instance);

        var allowed = await sut.ReadAsync("src/nested/deep.cs", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var disallowed = await sut.ReadAsync("src/notes.txt", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.NotNull(allowed);
        Assert.Null(disallowed);
    }

    [Fact]
    public async Task ListAsyncGlobAllowlistRootIncludesRelevantAncestorDirectoriesOnly()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new IngestionOptions
        {
            RepoRoot = _tempDir,
            RepoAllowlist = new[] { "src/**/*.cs" }
        });
        var sut = new RepoFileService(options, new WorkspaceContext(), _auditLog, NullLogger<RepoFileService>.Instance);

        var root = await sut.ListAsync(".", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var src = await sut.ListAsync("src", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Contains(root.Entries, entry => entry.Name == "src" && entry.IsDirectory);
        Assert.DoesNotContain(root.Entries, entry => entry.Name == "readme.md");
        Assert.Contains(src.Entries, entry => entry.Name == "code.cs" && !entry.IsDirectory);
        Assert.Contains(src.Entries, entry => entry.Name == "nested" && entry.IsDirectory);
        Assert.DoesNotContain(src.Entries, entry => entry.Name == "notes.txt");
    }

    [Fact]
    public async Task ReadAsyncPathThroughSymlinkOutsideRepoReturnsNull()
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

            var result = await sut.ReadAsync("linked-outside/outside.cs", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

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
