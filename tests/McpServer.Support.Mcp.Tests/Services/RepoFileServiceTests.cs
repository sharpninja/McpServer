using McpServer.Support.Mcp.Ingestion;
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
    private readonly RepoFileService _sut;

    public RepoFileServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"repo_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        File.WriteAllText(Path.Combine(_tempDir, "readme.md"), "# Hello");
        Directory.CreateDirectory(Path.Combine(_tempDir, "src"));
        File.WriteAllText(Path.Combine(_tempDir, "src", "code.cs"), "class Foo {}");

        var options = Microsoft.Extensions.Options.Options.Create(new IngestionOptions { RepoRoot = _tempDir });
        _sut = new RepoFileService(options, _auditLog, NullLogger<RepoFileService>.Instance);
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
        var sut = new RepoFileService(options, _auditLog, NullLogger<RepoFileService>.Instance);

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
    }

    [Fact]
    public async Task WriteAsync_DisallowedPath_ReturnsFailure()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new IngestionOptions
        {
            RepoRoot = _tempDir,
            RepoAllowlist = new[] { "*.md" }
        });
        var sut = new RepoFileService(options, _auditLog, NullLogger<RepoFileService>.Instance);

        var result = await sut.WriteAsync("secret.txt", "data").ConfigureAwait(true);

        Assert.False(result.Written);
    }
}
