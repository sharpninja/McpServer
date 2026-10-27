using QBrainAi.Support.Mcp.Ingestion;
using QBrainAi.Support.Mcp.Notifications;
using QBrainAi.Support.Mcp.Services;
using NSubstitute;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

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
    public async Task ReadAsync_AllowedPath_ReturnsContent()
    {
        var result = await _sut.ReadAsync("readme.md", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.NotNull(result);
        Assert.True(result.Exists);
        Assert.Contains("Hello", result.Content);
    }

    [Fact]
    public async Task ReadAsync_PathTraversal_ReturnsNull()
    {
        var result = await _sut.ReadAsync("../../etc/passwd", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Null(result);
    }

    /// <summary>
    /// TR-MCP-QBTOOLS-000 AC-001 (BUG-BASELINE-003): Rooted paths outside the active workspace are rejected
    /// before any repository read, list, write, edit, or transactional capture.
    /// </summary>
    [Fact]
    public async Task RootedExternalPath_AcrossRepoOperations_IsRejected()
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

    /// <summary>
    /// TR-MCP-QBTOOLS-000 AC-001 (BUG-BASELINE-003): an absolute path inside the repository root is accepted on every OS and
    /// resolves to the matching repo-relative path for read, list, write, edit, and transactional capture. Fixture: the
    /// temporary repository built by the constructor, addressed through its absolute paths.
    /// </summary>
    [Fact]
    public async Task RootedPathInsideRoot_AcrossRepoOperations_ResolvesRepoRelative()
    {
        var ct = TestContext.Current.CancellationToken;
        var code = Path.Combine(_tempDir, "src", "code.cs");
        var notes = Path.Combine(_tempDir, "src", "notes.txt");
        var created = Path.Combine(_tempDir, "src", "created.txt");

        var read = await _sut.ReadAsync(code, cancellationToken: ct).ConfigureAwait(true);
        var list = await _sut.ListAsync(Path.Combine(_tempDir, "src"), cancellationToken: ct).ConfigureAwait(true);
        var write = await _sut.WriteAsync(created, "created", cancellationToken: ct).ConfigureAwait(true);
        var edit = await _sut.EditAsync(notes, "notes", "edited", cancellationToken: ct).ConfigureAwait(true);
        var snapshot = await _sut.CaptureForWriteAsync(code, cancellationToken: ct).ConfigureAwait(true);

        Assert.NotNull(read);
        Assert.Equal("src/code.cs", read.RelativePath);
        Assert.Equal("class Foo {}", read.Content);
        Assert.Equal("src", list.Path);
        Assert.Contains(list.Entries, e => e.Name == "code.cs");
        Assert.True(write.Written);
        Assert.Equal("created", await File.ReadAllTextAsync(created, ct).ConfigureAwait(true));
        Assert.True(edit.Written);
        Assert.Equal("edited", await File.ReadAllTextAsync(notes, ct).ConfigureAwait(true));
        Assert.NotNull(snapshot);
        Assert.Equal("src/code.cs", snapshot.RelativePath);
    }

    /// <summary>
    /// TR-MCP-QBTOOLS-000 AC-001 (BUG-BASELINE-003): a leading separator marks an absolute path and is never reinterpreted as
    /// repo-relative, so "/src/code.cs" (a path outside the temporary root) is rejected and the repository's src/code.cs is
    /// neither read nor changed. Fixture: the temporary repository built by the constructor.
    /// </summary>
    [Fact]
    public async Task LeadingSeparatorPath_IsNotReinterpretedAsRepoRelative()
    {
        var ct = TestContext.Current.CancellationToken;

        var read = await _sut.ReadAsync("/src/code.cs", cancellationToken: ct).ConfigureAwait(true);
        var write = await _sut.WriteAsync("/src/code.cs", "changed", cancellationToken: ct).ConfigureAwait(true);

        Assert.Null(read);
        Assert.False(write.Written);
        Assert.Equal("class Foo {}", await File.ReadAllTextAsync(Path.Combine(_tempDir, "src", "code.cs"), ct).ConfigureAwait(true));
    }

    /// <summary>
    /// TR-MCP-QBTOOLS-000 AC-001 (BUG-BASELINE-003): an absolute path with a .. segment is rejected like a relative one, even
    /// when lexical normalization would land inside the root. Fixture: the temporary repository built by the constructor.
    /// </summary>
    [Fact]
    public async Task RootedPathWithParentSegment_IsRejected()
    {
        var ct = TestContext.Current.CancellationToken;
        var viaParent = _tempDir + "/src/../readme.md";

        var read = await _sut.ReadAsync(viaParent, cancellationToken: ct).ConfigureAwait(true);

        Assert.Null(read);
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

        var result = await sut.ReadAsync("src/code.cs", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Null(result);
    }

    [Fact]
    public async Task ListAsync_ValidDirectory_ReturnsEntries()
    {
        var result = await _sut.ListAsync(".", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.NotEmpty(result.Entries);
        Assert.Contains(result.Entries, e => e.Name == "readme.md");
        Assert.Contains(result.Entries, e => e.Name == "src" && e.IsDirectory);
    }

    [Fact]
    public async Task ListAsync_NonexistentPath_ReturnsEmpty()
    {
        var result = await _sut.ListAsync("nonexistent_dir", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Empty(result.Entries);
    }

    [Fact]
    public async Task WriteAsync_AllowedPath_WritesAndAudits()
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
    public async Task WriteAsync_DisallowedPath_ReturnsFailure()
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
    public async Task CaptureForWriteAsync_ExistingPath_ReturnsSnapshot()
    {
        var result = await _sut.CaptureForWriteAsync("readme.md", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.NotNull(result);
        Assert.True(result.Exists);
        Assert.Equal("readme.md", result.RelativePath);
        Assert.Equal("# Hello", result.Content);
        Assert.False(string.IsNullOrWhiteSpace(result.ContentSha256));
    }

    [Fact]
    public async Task RestoreWriteAsync_ExistingSnapshot_RestoresPriorContent()
    {
        var snapshot = await _sut.CaptureForWriteAsync("readme.md", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.NotNull(snapshot);
        var write = await _sut.WriteAsync("readme.md", "changed", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(write.Written);

        await _sut.RestoreWriteAsync(snapshot!, "changed", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal("# Hello", File.ReadAllText(Path.Combine(_tempDir, "readme.md")));
    }

    [Fact]
    public async Task RestoreWriteAsync_NewFileSnapshot_DeletesCreatedFile()
    {
        var snapshot = await _sut.CaptureForWriteAsync("created.md", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.NotNull(snapshot);
        var write = await _sut.WriteAsync("created.md", "created", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(write.Written);

        await _sut.RestoreWriteAsync(snapshot!, "created", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(File.Exists(Path.Combine(_tempDir, "created.md")));
    }

    [Fact]
    public async Task RestoreWriteAsync_WhenFileChangedAfterWrite_RefusesOverwrite()
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
    public async Task EditAsync_UniqueOccurrence_Replaces()
    {
        var result = await _sut.EditAsync("readme.md", "Hello", "World", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Written);
        Assert.Equal(1, result.Replacements);
        Assert.Equal("# World", File.ReadAllText(Path.Combine(_tempDir, "readme.md")));
    }

    /// <summary>FR-MCP-QBTOOLS-006: A missing oldString fails without changing the file.</summary>
    [Fact]
    public async Task EditAsync_MissingOldString_Fails()
    {
        var result = await _sut.EditAsync("readme.md", "absent", "x", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(result.Written);
        Assert.Contains("not found", result.Error!, StringComparison.Ordinal);
        Assert.Equal("# Hello", File.ReadAllText(Path.Combine(_tempDir, "readme.md")));
    }

    /// <summary>FR-MCP-QBTOOLS-006: An ambiguous match fails unless replaceAll is set.</summary>
    [Fact]
    public async Task EditAsync_Ambiguous_FailsWithoutReplaceAll()
    {
        await _sut.WriteAsync("multi.txt", "a a a", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var result = await _sut.EditAsync("multi.txt", "a", "b", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(result.Written);
        Assert.Contains("ambiguous", result.Error!, StringComparison.Ordinal);
        Assert.Equal("a a a", File.ReadAllText(Path.Combine(_tempDir, "multi.txt")));
    }

    /// <summary>FR-MCP-QBTOOLS-006: replaceAll replaces every occurrence and reports the count.</summary>
    [Fact]
    public async Task EditAsync_ReplaceAll_ReplacesEvery()
    {
        await _sut.WriteAsync("multi.txt", "a a a", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var result = await _sut.EditAsync("multi.txt", "a", "b", replaceAll: true, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Written);
        Assert.Equal(3, result.Replacements);
        Assert.Equal("b b b", File.ReadAllText(Path.Combine(_tempDir, "multi.txt")));
    }

    /// <summary>FR-MCP-QBTOOLS-006: An expectedOccurrences mismatch fails.</summary>
    [Fact]
    public async Task EditAsync_ExpectedOccurrencesMismatch_Fails()
    {
        await _sut.WriteAsync("multi.txt", "a a a", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var result = await _sut.EditAsync("multi.txt", "a", "b", replaceAll: true, expectedOccurrences: 2, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(result.Written);
        Assert.Contains("expected 2", result.Error!, StringComparison.Ordinal);
    }

    /// <summary>FR-MCP-QBTOOLS-006: An empty oldString is rejected.</summary>
    [Fact]
    public async Task EditAsync_EmptyOldString_Fails()
    {
        var result = await _sut.EditAsync("readme.md", string.Empty, "x", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(result.Written);
        Assert.Contains("must not be empty", result.Error!, StringComparison.Ordinal);
    }

    /// <summary>FR-MCP-QBTOOLS-006: Identical oldString and newString are rejected.</summary>
    [Fact]
    public async Task EditAsync_SameOldAndNew_Fails()
    {
        var result = await _sut.EditAsync("readme.md", "Hello", "Hello", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(result.Written);
        Assert.Contains("must differ", result.Error!, StringComparison.Ordinal);
    }

    /// <summary>FR-MCP-QBTOOLS-006: Editing a nonexistent file fails.</summary>
    [Fact]
    public async Task EditAsync_NonexistentFile_Fails()
    {
        var result = await _sut.EditAsync("does-not-exist.txt", "a", "b", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(result.Written);
        Assert.Contains("file not found", result.Error!, StringComparison.Ordinal);
    }

    /// <summary>FR-MCP-QBTOOLS-006: A path-traversal edit is rejected.</summary>
    [Fact]
    public async Task EditAsync_PathTraversal_Rejected()
    {
        var result = await _sut.EditAsync("../../etc/passwd", "a", "b", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

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

        var result = await sut.EditAsync("src/code.cs", "Foo", "Bar", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(result.Written);
    }

    /// <summary>FR-MCP-QBTOOLS-006: A successful edit records an audit entry and publishes an Updated event.</summary>
    [Fact]
    public async Task EditAsync_Success_AuditsAndPublishesUpdated()
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
    public async Task ReadAsync_GlobAllowlist_RequiresActualPatternMatch()
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
    public async Task ReadAsync_GlobAllowlist_DeniesExtensionMismatchWithinAllowedTree()
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
    public async Task ListAsync_GlobAllowlist_RootIncludesRelevantAncestorDirectoriesOnly()
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

    /// <summary>
    /// TR-MCP-QBTOOLS-000 AC-001: absolute paths still obey the relative allowlist. Temporary fixture
    /// files distinguish an allowed code file from forbidden notes without touching operator files.
    /// </summary>
    [Fact]
    public async Task RootedInsidePath_EnforcesAllowlistBeforeEveryOperation()
    {
        var ct = TestContext.Current.CancellationToken;
        var options = Microsoft.Extensions.Options.Options.Create(new IngestionOptions
        {
            RepoRoot = _tempDir,
            RepoAllowlist = new[] { "src/**/*.cs" }
        });
        var sut = new RepoFileService(options, new WorkspaceContext(), _auditLog, NullLogger<RepoFileService>.Instance);
        var allowed = Path.Combine(_tempDir, "src", "code.cs");
        var forbidden = Path.Combine(_tempDir, "src", "notes.txt");
        Assert.NotNull(await sut.ReadAsync(allowed, ct).ConfigureAwait(true));
        Assert.Null(await sut.ReadAsync(forbidden, ct).ConfigureAwait(true));
        Assert.False((await sut.WriteAsync(forbidden, "changed", ct).ConfigureAwait(true)).Written);
        Assert.False((await sut.EditAsync(forbidden, "notes", "changed", cancellationToken: ct).ConfigureAwait(true)).Written);
        Assert.Null(await sut.CaptureForWriteAsync(forbidden, ct).ConfigureAwait(true));
        var list = await sut.ListAsync(Path.Combine(_tempDir, "src"), ct).ConfigureAwait(true);
        Assert.Contains(list.Entries, entry => entry.Name == "code.cs");
        Assert.DoesNotContain(list.Entries, entry => entry.Name == "notes.txt");
        Assert.Equal("notes", await File.ReadAllTextAsync(forbidden, ct).ConfigureAwait(true));
    }

    /// <summary>
    /// TR-MCP-QBTOOLS-000 AC-001: a lexically contained absolute path through an external symlink
    /// remains rejected across operations. Isolated temporary roots prove outside bytes stay intact.
    /// </summary>
    [Fact]
    public async Task RootedPathThroughExternalSymlink_AcrossRepoOperations_IsRejected()
    {
        var ct = TestContext.Current.CancellationToken;
        var outside = Directory.CreateTempSubdirectory("repo-absolute-outside-").FullName;
        var link = Path.Combine(_tempDir, "absolute-link");
        var outsideFile = Path.Combine(outside, "outside.cs");
        await File.WriteAllTextAsync(outsideFile, "outside", ct).ConfigureAwait(true);
        try
        {
            Assert.True(TryCreateDirectoryLink(link, outside), "This native gate requires directory symlink creation.");
            var requested = Path.Combine(link, "outside.cs");
            Assert.Null(await _sut.ReadAsync(requested, ct).ConfigureAwait(true));
            Assert.Empty((await _sut.ListAsync(link, ct).ConfigureAwait(true)).Entries);
            Assert.False((await _sut.WriteAsync(requested, "changed", ct).ConfigureAwait(true)).Written);
            Assert.False((await _sut.EditAsync(requested, "outside", "changed", cancellationToken: ct).ConfigureAwait(true)).Written);
            Assert.Null(await _sut.CaptureForWriteAsync(requested, ct).ConfigureAwait(true));
            Assert.Equal("outside", await File.ReadAllTextAsync(outsideFile, ct).ConfigureAwait(true));
        }
        finally
        {
            if (Directory.Exists(link))
                Directory.Delete(link);
            Directory.Delete(outside, recursive: true);
        }
    }
    /// <summary>
    /// TR-MCP-QBTOOLS-000 AC-001: a filesystem root remains rooted during core path normalization.
    /// Reads only the owned temporary fixture file through absolute and root-relative addresses.
    /// </summary>
    /// <param name="absolute">Whether to use the absolute fixture address.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FileSystemRootWorkspace_ReadsOwnedFixtureWithoutLosingRoot(bool absolute)
    {
        var ct = TestContext.Current.CancellationToken;
        var root = Path.GetPathRoot(_tempDir)!;
        var owned = Path.Combine(_tempDir, "readme.md");
        var relative = Path.GetRelativePath(root, owned).Replace('\\', '/');
        var options = Microsoft.Extensions.Options.Options.Create(new IngestionOptions
        {
            RepoRoot = root,
            RepoAllowlist = new[] { relative }
        });
        var sut = new RepoFileService(options, new WorkspaceContext(), _auditLog, NullLogger<RepoFileService>.Instance);
        var result = await sut.ReadAsync(absolute ? owned : relative, ct).ConfigureAwait(true);
        Assert.NotNull(result);
        Assert.Equal("# Hello", result.Content);
        Assert.Equal(relative, result.RelativePath);
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
