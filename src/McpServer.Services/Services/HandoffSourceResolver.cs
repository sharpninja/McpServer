using System.Security.Cryptography;
using System.Text;
using McpServer.Support.Mcp.Requirements;
using McpServer.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.Services;

/// <summary>TR-HANDOFF-SECURITY-001: Resolves bounded, workspace-contained handoff sources.</summary>
public interface IHandoffSourceResolver
{
    /// <summary>Resolve and hash source content without logging the raw text.</summary>
    Task<HandoffResolvedSource> ResolveAsync(HandoffIngestionRequest request, string workspacePath, CancellationToken cancellationToken);
}

/// <inheritdoc />
public sealed class HandoffSourceResolver : IHandoffSourceResolver
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".md",
        ".markdown",
        ".txt",
        ".text",
        ".json",
        ".yaml",
        ".yml",
    };

    private readonly McpDbContext _db;

    /// <summary>TR-HANDOFF-SECURITY-001: Constructor.</summary>
    public HandoffSourceResolver(McpDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    /// <inheritdoc />
    public async Task<HandoffResolvedSource> ResolveAsync(HandoffIngestionRequest request, string workspacePath, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(workspacePath))
        {
            return Fail(request.SourceKind, "workspace", "source_missing", "Workspace path is required.");
        }

        return request.SourceKind switch
        {
            HandoffSourceKind.Content => ResolveContent(request.Content),
            HandoffSourceKind.Path => await ResolvePathAsync(request.Path, workspacePath, cancellationToken).ConfigureAwait(false),
            HandoffSourceKind.Artifact => await ResolveArtifactAsync(request.ArtifactId, workspacePath, cancellationToken).ConfigureAwait(false),
            _ => Fail(request.SourceKind, request.SourceKind.ToString(), "source_unsupported", "Unsupported handoff source kind."),
        };
    }

    private static HandoffResolvedSource ResolveContent(string? content)
    {
        if (content is null)
            return Fail(HandoffSourceKind.Content, "content", "source_missing", "Caller-supplied content is required.");

        var bytes = Encoding.UTF8.GetByteCount(content);
        if (bytes > HandoffPromptDefaults.MaxDecodedBytes)
            return Fail(HandoffSourceKind.Content, "content", "source_oversized", "Decoded content exceeds the 8 MiB limit.");

        return Success(HandoffSourceKind.Content, "content", content);
    }

    private static async Task<HandoffResolvedSource> ResolvePathAsync(string? path, string workspacePath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path))
            return Fail(HandoffSourceKind.Path, "path", "source_missing", "A workspace-contained path is required.");

        if (path.Contains("..", StringComparison.Ordinal) || path.Contains("%2e%2e", StringComparison.OrdinalIgnoreCase))
            return Fail(HandoffSourceKind.Path, SanitizeLocator(path), "source_traversal", "Path traversal is not allowed.");

        string fullPath;
        try
        {
            var root = Path.GetFullPath(workspacePath);
            fullPath = Path.IsPathRooted(path)
                ? Path.GetFullPath(path)
                : Path.GetFullPath(Path.Combine(root, path));
        }
        catch (Exception)
        {
            return Fail(HandoffSourceKind.Path, SanitizeLocator(path), "source_external", "The path could not be resolved inside the workspace.");
        }

        var locator = SanitizeLocator(path);
        if (!RequirementsWikiPathSecurity.IsContainedByRoot(workspacePath, fullPath))
            return Fail(HandoffSourceKind.Path, locator, "source_external", "The path resolves outside the workspace.");

        if (RequirementsWikiPathSecurity.EscapesWorkspaceThroughReparsePoint(workspacePath, fullPath))
            return Fail(HandoffSourceKind.Path, locator, "source_reparse", "The path escapes the workspace through a reparse point.");

        if (!File.Exists(fullPath))
            return Fail(HandoffSourceKind.Path, locator, "source_missing", "The handoff file was not found.");

        var extension = Path.GetExtension(fullPath);
        if (!SupportedExtensions.Contains(extension))
            return Fail(HandoffSourceKind.Path, locator, "source_unsupported", "Only Markdown, text, JSON, and YAML handoff files are supported.");

        var read = await HandoffContainedFileReader.ReadAsync(workspacePath, fullPath, cancellationToken).ConfigureAwait(false);
        if (!read.Success)
            return Fail(HandoffSourceKind.Path, locator, read.Code ?? "source_missing", read.Message ?? "The handoff file could not be read.");

        return Success(HandoffSourceKind.Path, locator, read.Text!);
    }

    private async Task<HandoffResolvedSource> ResolveArtifactAsync(string? artifactId, string workspacePath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(artifactId))
            return Fail(HandoffSourceKind.Artifact, "artifact", "source_missing", "An MCP artifact identifier is required.");

        if (artifactId.Contains("..", StringComparison.Ordinal) || artifactId.Contains(':') && artifactId.Contains("..", StringComparison.Ordinal))
            return Fail(HandoffSourceKind.Artifact, SanitizeLocator(artifactId), "source_traversal", "Artifact identifiers may not traverse the workspace.");

        var locator = "artifact:" + SanitizeLocator(artifactId);
        var document = await _db.Documents
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == artifactId || d.SourceKey == artifactId, cancellationToken)
            .ConfigureAwait(false);

        if (document is not null)
        {
            var chunkQuery = _db.Chunks
                .AsNoTracking()
                .Where(c => c.DocumentId == document.Id)
                .OrderBy(c => c.ChunkIndex)
                .Select(c => c.Content);
            var builder = new StringBuilder();
            var decodedBytes = 0;
            await foreach (var chunk in chunkQuery.AsAsyncEnumerable().WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                var piece = chunk ?? string.Empty;
                decodedBytes += Encoding.UTF8.GetByteCount(piece);
                if (decodedBytes > HandoffPromptDefaults.MaxDecodedBytes)
                    return Fail(HandoffSourceKind.Artifact, locator, HandoffErrorCodes.SourceOversized, "Decoded content exceeds the 8 MiB limit.");
                builder.Append(piece);
            }

            var text = builder.ToString();
            if (string.IsNullOrEmpty(text))
                return Fail(HandoffSourceKind.Artifact, locator, "source_missing", "The referenced MCP artifact has no readable content.");

            return Success(HandoffSourceKind.Artifact, locator, text);
        }

        var candidateRelative = Path.Combine(".mcpServer", "artifacts", artifactId);
        var fileResult = await ResolvePathAsync(candidateRelative, workspacePath, cancellationToken).ConfigureAwait(false);
        if (fileResult.Success)
        {
            return new HandoffResolvedSource
            {
                Success = true,
                SourceKind = HandoffSourceKind.Artifact,
                Locator = locator,
                Text = fileResult.Text,
                ContentSha256 = fileResult.ContentSha256,
                Diagnostics = fileResult.Diagnostics,
            };
        }

        var unknown = new List<HandoffDiagnostic>(fileResult.Diagnostics)
        {
            new()
            {
                Code = "source_unknown",
                Severity = HandoffDiagnosticSeverity.Error,
                Field = "artifactId",
                Message = "The MCP artifact reference was not found in workspace documents or contained artifact storage.",
            },
        };
        return new HandoffResolvedSource
        {
            Success = false,
            SourceKind = HandoffSourceKind.Artifact,
            Locator = locator,
            Diagnostics = unknown,
        };
    }

    private static HandoffResolvedSource Success(HandoffSourceKind kind, string locator, string text)
        => new()
        {
            Success = true,
            SourceKind = kind,
            Locator = locator,
            Text = text,
            ContentSha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant(),
        };

    private static HandoffResolvedSource Fail(HandoffSourceKind kind, string locator, string code, string message)
        => new()
        {
            Success = false,
            SourceKind = kind,
            Locator = locator,
            Diagnostics =
            [
                new HandoffDiagnostic
                {
                    Code = code,
                    Severity = HandoffDiagnosticSeverity.Error,
                    Field = kind == HandoffSourceKind.Content ? "content" : kind == HandoffSourceKind.Artifact ? "artifactId" : "path",
                    Message = message,
                },
            ],
        };

    private static string SanitizeLocator(string value)
    {
        var trimmed = value.Replace('\\', '/').Trim();
        if (trimmed.Length > 240)
            trimmed = trimmed[..240];
        return trimmed;
    }
}
