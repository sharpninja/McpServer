using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
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

        var dataProtection = DataProtectionProvider.Create(new DirectoryInfo(_tempRoot));
        _sut = new FileGitHubWorkspaceTokenStore(options, dataProtection, NullLogger<FileGitHubWorkspaceTokenStore>.Instance);
    }

    [Fact]
    public async Task UpsertAndGetAsync_RoundTripsToken()
    {
        var workspacePath = Path.Combine(_tempRoot, "workspace-a");
        var expiresAt = DateTimeOffset.UtcNow.AddHours(2);

        await _sut.UpsertAsync(workspacePath, "gho_test_token", expiresAt, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var record = await _sut.GetAsync(workspacePath, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.NotNull(record);
        Assert.Equal("gho_test_token", record.AccessToken);
        Assert.Equal(expiresAt, record.ExpiresAtUtc);
        Assert.False(string.IsNullOrWhiteSpace(record.WorkspacePath));
    }

    [Fact]
    public async Task UpsertAsync_SecondWriteReplacesToken()
    {
        var workspacePath = Path.Combine(_tempRoot, "workspace-b");

        await _sut.UpsertAsync(workspacePath, "gho_first", ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        await _sut.UpsertAsync(workspacePath, "gho_second", ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var record = await _sut.GetAsync(workspacePath, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.NotNull(record);
        Assert.Equal("gho_second", record.AccessToken);
    }

    [Fact]
    public async Task DeleteAsync_RemovesTokenRecord()
    {
        var workspacePath = Path.Combine(_tempRoot, "workspace-c");

        await _sut.UpsertAsync(workspacePath, "gho_delete_me", ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var removed = await _sut.DeleteAsync(workspacePath, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var record = await _sut.GetAsync(workspacePath, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(removed);
        Assert.Null(record);
    }

    [Fact]
    public async Task UpsertAsync_WhenStoreLockIsHeld_WaitsForRelease()
    {
        var workspacePath = Path.Combine(_tempRoot, "workspace-locked");
        var lockPath = Path.Combine(_tempRoot, "github-token-store.json.lock");
        using var lockStream = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

        var upsertTask = _sut.UpsertAsync(workspacePath, "gho_wait_for_lock", ct: TestContext.Current.CancellationToken);

        await Task.Delay(100, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.False(upsertTask.IsCompleted);

        lockStream.Dispose();
        await upsertTask.ConfigureAwait(true);

        var record = await _sut.GetAsync(workspacePath, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.NotNull(record);
        Assert.Equal("gho_wait_for_lock", record.AccessToken);
    }

    [Fact]
    public async Task UpsertAsync_HardensStoreAndLockFilePermissions()
    {
        var workspacePath = Path.Combine(_tempRoot, "workspace-permissions");
        var storePath = Path.Combine(_tempRoot, "github-token-store.json");
        var lockPath = storePath + ".lock";

        await _sut.UpsertAsync(workspacePath, "gho_permissions", ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(File.Exists(storePath));
        Assert.True(File.Exists(lockPath));

        if (OperatingSystem.IsWindows())
        {
            AssertRestrictedWindowsFile(storePath);
            AssertRestrictedWindowsFile(lockPath);
            return;
        }

        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            var expected = UnixFileMode.UserRead | UnixFileMode.UserWrite;
            Assert.Equal(expected, File.GetUnixFileMode(storePath));
            Assert.Equal(expected, File.GetUnixFileMode(lockPath));
            return;
        }

        throw new PlatformNotSupportedException("The token-store permission test only supports Windows ACL or Unix file-mode assertions.");
    }

    [SupportedOSPlatform("windows")]
    private static void AssertRestrictedWindowsFile(string path)
    {
        var security = new FileInfo(path).GetAccessControl();
        Assert.True(security.AreAccessRulesProtected);

        var rules = security
            .GetAccessRules(includeExplicit: true, includeInherited: true, targetType: typeof(SecurityIdentifier))
            .OfType<FileSystemAccessRule>()
            .ToArray();

        using var identity = WindowsIdentity.GetCurrent();
        var currentUser = identity.User
            ?? throw new InvalidOperationException("The current Windows identity did not expose a user SID for ACL assertions.");

        AssertContainsAllowFullControl(rules, currentUser);
        AssertContainsAllowFullControl(rules, new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null));
        AssertContainsAllowFullControl(rules, new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null));
    }

    [SupportedOSPlatform("windows")]
    private static void AssertContainsAllowFullControl(IEnumerable<FileSystemAccessRule> rules, SecurityIdentifier expectedSid)
    {
        Assert.Contains(rules, rule =>
            rule.AccessControlType == AccessControlType.Allow
            && rule.IdentityReference is SecurityIdentifier actualSid
            && string.Equals(actualSid.Value, expectedSid.Value, StringComparison.OrdinalIgnoreCase)
            && rule.FileSystemRights.HasFlag(FileSystemRights.FullControl));
    }

    public void Dispose()
    {
        _sut.Dispose();
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, recursive: true);
    }
}
