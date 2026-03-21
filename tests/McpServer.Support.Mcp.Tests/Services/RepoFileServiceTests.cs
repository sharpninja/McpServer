using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Notifications;
using McpServer.Support.Mcp.Services;
using NSubstitute;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>TR-PLANNED-013: Unit tests for RepoFileService path security and audit.</summary>
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
