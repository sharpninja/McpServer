using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>TR-MCP-GH-002: Unit tests for encrypted workspace GitHub token storage.</summary>
public sealed class FileGitHubWorkspaceTokenStoreTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly FileGitHubWorkspaceTokenStore _sut;

    public FileGitHubWorkspaceTokenStoreTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "mcp-github-token-store-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);

        var options = Substitute.For<IOptionsMonitor<GitHubIntegrationOptions>>();
        options.CurrentValue.Returns(new GitHubIntegrationOptions
        {
            TokenStorePath = Path.Combine(_tempRoot, "github-token-store.json"),
        });

        var dataProtection = DataProtectionProvider.Create(_tempRoot);
        _sut = new FileGitHubWorkspaceTokenStore(options, dataProtection, NullLogger<FileGitHubWorkspaceTokenStore>.Instance);
    }

    [Fact]
    public async Task UpsertAndGetAsync_RoundTripsToken()
    {
        var workspacePath = Path.Combine(_tempRoot, "workspace-a");
        var expiresAt = DateTimeOffset.UtcNow.AddHours(2);

        await _sut.UpsertAsync(workspacePath, "gho_test_token", expiresAt).ConfigureAwait(true);
        var record = await _sut.GetAsync(workspacePath).ConfigureAwait(true);

        Assert.NotNull(record);
        Assert.Equal("gho_test_token", record.AccessToken);
        Assert.Equal(expiresAt, record.ExpiresAtUtc);
        Assert.False(string.IsNullOrWhiteSpace(record.WorkspacePath));
    }

    [Fact]
    public async Task UpsertAsync_SecondWriteReplacesToken()
    {
        var workspacePath = Path.Combine(_tempRoot, "workspace-b");

        await _sut.UpsertAsync(workspacePath, "gho_first").ConfigureAwait(true);
        await _sut.UpsertAsync(workspacePath, "gho_second").ConfigureAwait(true);
        var record = await _sut.GetAsync(workspacePath).ConfigureAwait(true);

        Assert.NotNull(record);
        Assert.Equal("gho_second", record.AccessToken);
    }

    [Fact]
    public async Task DeleteAsync_RemovesTokenRecord()
    {
        var workspacePath = Path.Combine(_tempRoot, "workspace-c");

        await _sut.UpsertAsync(workspacePath, "gho_delete_me").ConfigureAwait(true);
        var removed = await _sut.DeleteAsync(workspacePath).ConfigureAwait(true);
        var record = await _sut.GetAsync(workspacePath).ConfigureAwait(true);

        Assert.True(removed);
        Assert.Null(record);
    }

    public void Dispose()
    {
        _sut.Dispose();
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, recursive: true);
    }
}
