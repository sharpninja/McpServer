using System.Security.Cryptography;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.Json;
using McpServer.Support.Mcp.Options;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-GH-002: File-backed encrypted token storage keyed by normalized workspace path.
/// </summary>
public sealed class FileGitHubWorkspaceTokenStore : IGitHubWorkspaceTokenStore, IDisposable
{
    private const int StoreLockRetryDelayMilliseconds = 50;
    private static readonly TimeSpan[] s_atomicWriteRetryDelays =
    [
        TimeSpan.FromMilliseconds(20),
        TimeSpan.FromMilliseconds(50),
        TimeSpan.FromMilliseconds(100)
    ];

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private readonly IOptionsMonitor<GitHubIntegrationOptions> _options;
    private readonly IDataProtector _protector;
    private readonly ILogger<FileGitHubWorkspaceTokenStore> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>Initializes a new instance of the <see cref="FileGitHubWorkspaceTokenStore"/> class.</summary>
    public FileGitHubWorkspaceTokenStore(
        IOptionsMonitor<GitHubIntegrationOptions> options,
        IDataProtectionProvider dataProtectionProvider,
        ILogger<FileGitHubWorkspaceTokenStore> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _protector = (dataProtectionProvider ?? throw new ArgumentNullException(nameof(dataProtectionProvider)))
            .CreateProtector("McpServer.Support.Mcp.GitHubTokenStore.v1");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<GitHubWorkspaceTokenRecord?> GetAsync(string workspacePath, CancellationToken ct = default)
    {
        var normalized = NormalizeWorkspacePath(workspacePath);

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            using var storeLock = await AcquireStoreLockAsync(ct).ConfigureAwait(false);
            var doc = await ReadUnlockedAsync(ct).ConfigureAwait(false);
            var match = doc.Entries.FirstOrDefault(e => string.Equals(e.WorkspacePath, normalized, StringComparison.OrdinalIgnoreCase));
            if (match is null)
                return null;

            try
            {
                var token = _protector.Unprotect(match.AccessTokenProtected);
                return new GitHubWorkspaceTokenRecord(match.WorkspacePath, token, match.UpdatedAtUtc, match.ExpiresAtUtc);
            }
            catch (CryptographicException ex)
            {
                _logger.LogWarning(ex, "GitHub token decrypt failed for workspace {WorkspacePath}", normalized);
                return null;
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task UpsertAsync(string workspacePath, string accessToken, DateTimeOffset? expiresAtUtc = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            throw new ArgumentException("Access token is required.", nameof(accessToken));

        var normalized = NormalizeWorkspacePath(workspacePath);
        var now = DateTimeOffset.UtcNow;
        var encrypted = _protector.Protect(accessToken.Trim());

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            using var storeLock = await AcquireStoreLockAsync(ct).ConfigureAwait(false);
            var doc = await ReadUnlockedAsync(ct).ConfigureAwait(false);
            var existing = doc.Entries.FirstOrDefault(e => string.Equals(e.WorkspacePath, normalized, StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                doc.Entries.Add(new GitHubTokenStoreEntry
                {
                    WorkspacePath = normalized,
                    AccessTokenProtected = encrypted,
                    UpdatedAtUtc = now,
                    ExpiresAtUtc = expiresAtUtc,
                });
            }
            else
            {
                existing.AccessTokenProtected = encrypted;
                existing.UpdatedAtUtc = now;
                existing.ExpiresAtUtc = expiresAtUtc;
            }

            await WriteUnlockedAsync(doc, ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string workspacePath, CancellationToken ct = default)
    {
        var normalized = NormalizeWorkspacePath(workspacePath);

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            using var storeLock = await AcquireStoreLockAsync(ct).ConfigureAwait(false);
            var doc = await ReadUnlockedAsync(ct).ConfigureAwait(false);
            var removed = doc.Entries.RemoveAll(e => string.Equals(e.WorkspacePath, normalized, StringComparison.OrdinalIgnoreCase)) > 0;
            if (removed)
                await WriteUnlockedAsync(doc, ct).ConfigureAwait(false);
            return removed;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Disposes synchronization resources.</summary>
    public void Dispose()
    {
        _gate.Dispose();
    }

    private async Task<GitHubTokenStoreDocument> ReadUnlockedAsync(CancellationToken ct)
    {
        var path = ResolveStorePath();
        if (!File.Exists(path))
            return new GitHubTokenStoreDocument();

        try
        {
            EnsureRestrictedFilePermissions(path);
            var json = await File.ReadAllTextAsync(path, ct).ConfigureAwait(false);
            var doc = JsonSerializer.Deserialize<GitHubTokenStoreDocument>(json, s_jsonOptions);
            return doc ?? new GitHubTokenStoreDocument();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "GitHub token store is invalid JSON at {Path}. Using empty store.", path);
            return new GitHubTokenStoreDocument();
        }
    }

    private async Task WriteUnlockedAsync(GitHubTokenStoreDocument doc, CancellationToken ct)
    {
        var path = ResolveStorePath();
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(doc, s_jsonOptions);
        var tmp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllTextAsync(tmp, json, ct).ConfigureAwait(false);
            EnsureRestrictedFilePermissions(tmp);
            await ReplaceOrMoveWithRetryAsync(tmp, path, ct).ConfigureAwait(false);
            EnsureRestrictedFilePermissions(path);
        }
        finally
        {
            if (File.Exists(tmp))
                File.Delete(tmp);
        }
    }

    private static async Task ReplaceOrMoveWithRetryAsync(string tmp, string path, CancellationToken ct)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                ReplaceOrMove(tmp, path);
                return;
            }
            catch (Exception ex) when (ex is PlatformNotSupportedException or UnauthorizedAccessException)
            {
                File.Move(tmp, path, overwrite: true);
                return;
            }
            catch (IOException) when (attempt < s_atomicWriteRetryDelays.Length)
            {
                await Task.Delay(s_atomicWriteRetryDelays[attempt], ct).ConfigureAwait(false);
            }
            catch (IOException)
            {
                File.Move(tmp, path, overwrite: true);
                return;
            }
        }
    }

    private static void ReplaceOrMove(string tmp, string path)
    {
        if (File.Exists(path))
        {
            File.Replace(tmp, path, null, ignoreMetadataErrors: true);
        }
        else
        {
            File.Move(tmp, path);
        }
    }

    private string ResolveStorePath()
    {
        var configured = _options.CurrentValue.TokenStorePath;
        if (string.IsNullOrWhiteSpace(configured))
            configured = "mcp-data/github-token-store.json";

        return Path.IsPathRooted(configured)
            ? Path.GetFullPath(configured)
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configured));
    }

    private async Task<FileStream> AcquireStoreLockAsync(CancellationToken ct)
    {
        var lockPath = ResolveStorePath() + ".lock";
        var dir = Path.GetDirectoryName(lockPath);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var stream = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                try
                {
                    EnsureRestrictedFilePermissions(lockPath);
                    return stream;
                }
                catch
                {
                    stream.Dispose();
                    throw;
                }
            }
            catch (IOException)
            {
                await Task.Delay(StoreLockRetryDelayMilliseconds, ct).ConfigureAwait(false);
            }
        }
    }

    private static void EnsureRestrictedFilePermissions(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            EnsureRestrictedWindowsFilePermissions(path);
            return;
        }

        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            return;
        }

        throw new PlatformNotSupportedException("GitHub token-store permission hardening requires Windows ACL or Unix file-mode support.");
    }

    [SupportedOSPlatform("windows")]
    private static void EnsureRestrictedWindowsFilePermissions(string path)
    {
        using var identity = WindowsIdentity.GetCurrent();
        var currentUser = identity.User
            ?? throw new InvalidOperationException("The current Windows identity did not expose a user SID for token-store ACL hardening.");

        var fileInfo = new FileInfo(path);
        var security = fileInfo.GetAccessControl();
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);

        foreach (var existingRule in security
                     .GetAccessRules(includeExplicit: true, includeInherited: true, targetType: typeof(SecurityIdentifier))
                     .OfType<FileSystemAccessRule>()
                     .ToArray())
        {
            security.RemoveAccessRuleAll(existingRule);
        }

        AddAllowRule(security, currentUser);
        AddAllowRule(security, new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null));
        AddAllowRule(security, new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null));
        fileInfo.SetAccessControl(security);
    }

    [SupportedOSPlatform("windows")]
    private static void AddAllowRule(FileSecurity security, SecurityIdentifier identity)
    {
        security.AddAccessRule(new FileSystemAccessRule(
            identity,
            FileSystemRights.FullControl,
            AccessControlType.Allow));
    }

    private static string NormalizeWorkspacePath(string workspacePath)
    {
        if (string.IsNullOrWhiteSpace(workspacePath))
            throw new ArgumentException("Workspace path is required.", nameof(workspacePath));

        return Path.GetFullPath(workspacePath.Trim())
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private sealed class GitHubTokenStoreDocument
    {
        public List<GitHubTokenStoreEntry> Entries { get; set; } = [];
    }

    private sealed class GitHubTokenStoreEntry
    {
        public string WorkspacePath { get; set; } = string.Empty;

        public string AccessTokenProtected { get; set; } = string.Empty;

        public DateTimeOffset UpdatedAtUtc { get; set; }

        public DateTimeOffset? ExpiresAtUtc { get; set; }
    }
}
