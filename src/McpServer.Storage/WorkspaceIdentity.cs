using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using McpServer.Client;
using McpServer.Common.AgentCli;

namespace McpServer.Support.Mcp.Storage;

/// <summary>Filesystem identity semantics used for persisted workspace keys.</summary>
public enum WorkspaceIdentityPlatform
{
    /// <summary>Windows drive/UNC paths compare without case and normalize separators.</summary>
    Windows,

    /// <summary>Case-sensitive paths preserve case and slash semantics.</summary>
    CaseSensitive,
}

/// <summary>
/// Persistence-facing workspace identity helpers backed by the repository-wide client policy.
/// </summary>
public static class WorkspaceIdentity
{
    /// <summary>Fixed hexadecimal length of every persisted SHA-256 identity key.</summary>
    public const int HashLength = 64;

    private static readonly TimeSpan s_defaultPhysicalResolutionTimeout = TimeSpan.FromSeconds(5);

    /// <summary>Comparer implementing the repository-wide workspace identity policy.</summary>
    public static IEqualityComparer<string> Comparer => WorkspaceIdentityPath.Comparer;

    /// <summary>
    /// Returns whether a value contains only well-formed UTF-16 code-unit sequences.
    /// </summary>
    /// <param name="value">Value to validate.</param>
    /// <returns><see langword="true"/> when every surrogate is paired correctly.</returns>
    public static bool IsWellFormedUtf16(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return McpServer.Client.Models.UseCaseCreationTextValidator.IsWellFormedUtf16(value);
    }

    /// <summary>
    /// Applies the required Unicode-dash normalization to a value before it is persisted or hashed.
    /// </summary>
    /// <param name="value">Durable identifier value.</param>
    /// <returns>The exact identifier representation used for persistence and identity hashing.</returns>
    public static string NormalizePersistedIdentifier(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!IsWellFormedUtf16(value))
        {
            throw new ArgumentException(
                "Persisted identifier must be well-formed UTF-16.",
                nameof(value));
        }

        return LineSanitizer.Sanitize(value);
    }

    /// <summary>Returns a canonical, case-preserving lexical path without filesystem I/O.</summary>
    public static string NormalizePath(string workspacePath) =>
        WorkspaceIdentityPath.NormalizePath(workspacePath);

    /// <summary>Returns an empty identity for blank input; otherwise returns the canonical path.</summary>
    public static string NormalizePathOrEmpty(string? workspacePath) =>
        WorkspaceIdentityPath.NormalizePathOrEmpty(workspacePath);

    /// <summary>Returns the durable provider-independent lexical identity key selected by path syntax.</summary>
    public static string GetStorageKey(string workspacePath) =>
        LineSanitizer.Sanitize(WorkspaceIdentityPath.GetStorageKey(workspacePath));

    /// <summary>
    /// Returns the bounded physical identity for a persisted path row, or an opaque identity when a
    /// legacy blank-path row contains no explicit path syntax. This synchronous compatibility API
    /// uses the same finite resolver and distinction as <see cref="GetPersistedStorageKeyAsync"/>.
    /// </summary>
    public static string GetPersistedStorageKey(string? workspaceId, string? workspacePath) =>
        GetPersistedStorageKeyAsync(
                workspaceId,
                workspacePath,
                s_defaultPhysicalResolutionTimeout,
                CancellationToken.None)
            .GetAwaiter()
            .GetResult();

    /// <summary>
    /// Returns a persisted-row storage key while reserving physical resolution for an explicitly
    /// verified workspace path.
    /// </summary>
    /// <param name="workspaceId">Opaque workspace identifier.</param>
    /// <param name="workspacePath">Explicit verified physical workspace path, when available.</param>
    /// <param name="timeout">Finite physical-resolution timeout.</param>
    /// <param name="cancellationToken">Caller cancellation token.</param>
    /// <returns>The canonical persisted storage key with required identifier normalization.</returns>
    public static async Task<string> GetPersistedStorageKeyAsync(
        string? workspaceId,
        string? workspacePath,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(workspaceId) &&
            string.IsNullOrWhiteSpace(workspacePath))
        {
            return string.Empty;
        }

        if (string.IsNullOrWhiteSpace(workspacePath))
        {
            cancellationToken.ThrowIfCancellationRequested();
            return GetOpaqueStorageKey(workspaceId!);
        }

        var platform = WorkspaceIdentityPath.DetectPlatform(workspacePath);
        var storageKey = await WorkspaceIdentityPath.GetStorageKeyAsync(
                workspacePath,
                platform,
                timeout,
                cancellationToken)
            .ConfigureAwait(false);
        return LineSanitizer.Sanitize(storageKey);
    }

    /// <summary>
    /// Builds a durable key from a physical workspace path already verified by the request boundary.
    /// No second physical resolver invocation is performed.
    /// </summary>
    /// <param name="verifiedWorkspacePath">Explicitly verified physical workspace path.</param>
    /// <param name="cancellationToken">Cancellation token checked before producing the key.</param>
    /// <returns>The lexical durable storage key for the verified path.</returns>
    public static Task<string> GetVerifiedWorkspaceStorageKeyAsync(
        string verifiedWorkspacePath,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(verifiedWorkspacePath);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(GetStorageKey(verifiedWorkspacePath));
    }

    /// <summary>
    /// Builds a durable key from opaque workspace identifier bytes without filesystem probing,
    /// normalization, or physical identity resolution.
    /// </summary>
    /// <param name="workspaceId">Opaque workspace identifier.</param>
    /// <param name="cancellationToken">Cancellation token checked before producing the key.</param>
    /// <returns>The durable opaque storage key.</returns>
    public static Task<string> GetOpaqueStorageKeyAsync(
        string workspaceId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceId);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(GetOpaqueStorageKey(workspaceId));
    }

    /// <summary>Builds an opaque durable key without interpreting identifier bytes as a path.</summary>
    private static string GetOpaqueStorageKey(string value)
    {
        var normalized = NormalizePersistedIdentifier(value.Trim());
        return WorkspaceIdentityPath.DetectPlatform(normalized) == WorkspacePathPlatform.Windows
            ? "windows:" + normalized.ToUpperInvariant()
            : "case-sensitive:" + normalized;
    }

    /// <summary>
    /// Resolves a workspace path and storage key with finite, cancellable native physical work.
    /// </summary>
    /// <param name="workspacePath">Workspace path to resolve.</param>
    /// <param name="timeout">Finite physical-resolution timeout.</param>
    /// <param name="cancellationToken">Caller cancellation token.</param>
    /// <returns>The exact normalized path and durable key used for persistence.</returns>
    public static async Task<(string NormalizedPath, string StorageKey)> ResolveAsync(
        string workspacePath,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrEmpty(workspacePath))
            return (string.Empty, string.Empty);

        var platform = WorkspaceIdentityPath.DetectPlatform(workspacePath);
        var resolution = await WorkspaceIdentityPath.ResolveAsync(
                workspacePath,
                platform,
                timeout,
                cancellationToken)
            .ConfigureAwait(false);
        return (
            LineSanitizer.Sanitize(resolution.NormalizedPath),
            LineSanitizer.Sanitize(resolution.StorageKey));
    }

    /// <summary>Returns a durable lexical identity key using an explicit platform policy.</summary>
    public static string GetStorageKey(
        string workspacePath,
        WorkspaceIdentityPlatform platform) =>
        LineSanitizer.Sanitize(
            WorkspaceIdentityPath.GetStorageKey(
                workspacePath,
                platform == WorkspaceIdentityPlatform.Windows
                    ? WorkspacePathPlatform.Windows
                    : WorkspacePathPlatform.CaseSensitive));

    /// <summary>Returns a fixed-length SHA-256 hash for a canonical storage identity.</summary>
    public static string GetIdentityHash(string storageIdentity)
    {
        ArgumentNullException.ThrowIfNull(storageIdentity);
        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(storageIdentity)));
    }

    /// <summary>Returns the fixed-length hash for a lexical workspace path.</summary>
    public static string GetWorkspaceHash(string workspacePath) =>
        GetIdentityHash(GetStorageKey(workspacePath));

    /// <summary>
    /// Returns the fixed-length hash for a workspace path while bounding physical resolution.
    /// </summary>
    /// <param name="workspacePath">Workspace path to hash.</param>
    /// <param name="timeout">Finite physical-resolution timeout.</param>
    /// <param name="cancellationToken">Caller cancellation token.</param>
    /// <returns>The provider-safe workspace identity hash.</returns>
    public static async Task<string> GetWorkspaceHashAsync(
        string workspacePath,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var platform = WorkspaceIdentityPath.DetectPlatform(workspacePath);
        var storageIdentity = await WorkspaceIdentityPath.GetStorageKeyAsync(
                workspacePath,
                platform,
                timeout,
                cancellationToken)
            .ConfigureAwait(false);
        return GetIdentityHash(LineSanitizer.Sanitize(storageIdentity));
    }

    /// <summary>Returns the fixed-length hash for a persisted workspace row.</summary>
    public static string GetPersistedWorkspaceHash(string? workspaceId, string? workspacePath) =>
        GetIdentityHash(GetPersistedStorageKey(workspaceId, workspacePath));

    /// <summary>Returns a fixed-length identity hash for an opaque persisted workspace id.</summary>
    public static string GetOpaqueWorkspaceIdHash(string workspaceId)
    {
        ArgumentNullException.ThrowIfNull(workspaceId);
        if (workspaceId.Length > 0 && string.IsNullOrWhiteSpace(workspaceId))
        {
            throw new ArgumentException(
                "Workspace id cannot consist only of whitespace.",
                nameof(workspaceId));
        }
        if (!IsWellFormedUtf16(workspaceId))
            throw new ArgumentException("Workspace id must be well-formed UTF-16.", nameof(workspaceId));

        var persistedWorkspaceId = LineSanitizer.Sanitize(workspaceId);
        return GetIdentityHash(
            string.Concat("McpServer.OpaqueWorkspaceId.v1\0", persistedWorkspaceId));
    }

    /// <summary>Returns a fixed-length identity hash for an ordinal durable request id.</summary>
    public static string GetRequestIdHash(string requestId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestId);
        if (!IsWellFormedUtf16(requestId))
            throw new ArgumentException("Request id must be well-formed UTF-16.", nameof(requestId));

        var persistedRequestId = LineSanitizer.Sanitize(requestId);
        return GetIdentityHash(
            string.Concat("McpServer.UseCaseRequestId.v1\0", persistedRequestId));
    }

    /// <summary>
    /// Returns a fixed-length, case-insensitive federation proxy identity hash.
    /// </summary>
    /// <param name="proxyId">Opaque proxy identifier.</param>
    /// <returns>A provider-safe SHA-256 identity hash.</returns>
    public static string GetFederationProxyHash(string proxyId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(proxyId);
        if (!IsWellFormedUtf16(proxyId))
            throw new ArgumentException("Proxy id must be well-formed UTF-16.", nameof(proxyId));

        var canonical = LineSanitizer.Sanitize(proxyId).Trim().ToUpperInvariant();
        return GetIdentityHash(string.Concat("McpServer.FederationProxy.v1\0", canonical));
    }

    /// <summary>
    /// Returns a fixed-length request receipt identity scoped to the canonical workspace.
    /// Request ids compare ordinally after required persisted-identifier normalization.
    /// </summary>
    public static string GetRequestKeyHash(string workspacePath, string requestId) =>
        GetRequestKeyHashFromStorageIdentity(GetStorageKey(workspacePath), requestId);

    /// <summary>Returns a fixed-length request receipt key from an already canonical workspace identity.</summary>
    public static string GetRequestKeyHashFromStorageIdentity(
        string workspaceIdentity,
        string requestId)
    {
        ArgumentNullException.ThrowIfNull(workspaceIdentity);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestId);
        if (!IsWellFormedUtf16(workspaceIdentity))
        {
            throw new ArgumentException(
                "Workspace identity must be well-formed UTF-16.",
                nameof(workspaceIdentity));
        }
        if (!IsWellFormedUtf16(requestId))
            throw new ArgumentException("Request id must be well-formed UTF-16.", nameof(requestId));

        var persistedWorkspaceIdentity = LineSanitizer.Sanitize(workspaceIdentity);
        var persistedRequestId = LineSanitizer.Sanitize(requestId);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData(Encoding.ASCII.GetBytes("McpServer.UseCaseRequestKey.v2"));
        hash.AppendData([0]);
        AppendLengthPrefixed(hash, persistedWorkspaceIdentity);
        AppendLengthPrefixed(hash, persistedRequestId);
        return Convert.ToHexString(hash.GetHashAndReset());
    }

    /// <summary>
    /// Returns the delimiter-based request key used before the v2 length-prefixed contract.
    /// This compatibility key is used only to discover and upgrade existing durable receipts.
    /// </summary>
    public static string GetLegacyRequestKeyHashFromStorageIdentity(
        string workspaceIdentity,
        string requestId)
    {
        ArgumentNullException.ThrowIfNull(workspaceIdentity);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestId);
        if (!IsWellFormedUtf16(workspaceIdentity))
        {
            throw new ArgumentException(
                "Workspace identity must be well-formed UTF-16.",
                nameof(workspaceIdentity));
        }
        if (!IsWellFormedUtf16(requestId))
            throw new ArgumentException("Request id must be well-formed UTF-16.", nameof(requestId));

        var persistedWorkspaceIdentity = LineSanitizer.Sanitize(workspaceIdentity);
        var persistedRequestId = LineSanitizer.Sanitize(requestId);
        return GetIdentityHash(
            string.Concat(persistedWorkspaceIdentity, "\u001F", persistedRequestId));
    }

    private static void AppendLengthPrefixed(IncrementalHash hash, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        Span<byte> length = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(length, bytes.Length);
        hash.AppendData(length);
        hash.AppendData(bytes);
    }

    /// <summary>Compares two workspace paths using syntax-appropriate lexical identities.</summary>
    public static bool AreEquivalent(string? left, string? right) =>
        WorkspaceIdentityPath.AreEquivalent(left, right);

    /// <summary>Returns whether a candidate path is the root or is contained beneath it.</summary>
    public static bool IsWithinRoot(string rootPath, string candidatePath) =>
        WorkspaceIdentityPath.IsWithinRoot(rootPath, candidatePath);

    /// <summary>Detects Windows or case-sensitive semantics from absolute path syntax.</summary>
    public static WorkspaceIdentityPlatform DetectPlatform(string workspacePath) =>
        WorkspaceIdentityPath.DetectPlatform(workspacePath) == WorkspacePathPlatform.Windows
            ? WorkspaceIdentityPlatform.Windows
            : WorkspaceIdentityPlatform.CaseSensitive;
}
