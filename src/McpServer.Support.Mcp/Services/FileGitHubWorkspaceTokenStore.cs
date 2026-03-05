using System.Security.Cryptography;
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
        await File.WriteAllTextAsync(tmp, json, ct).ConfigureAwait(false);
        File.Move(tmp, path, true);
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
