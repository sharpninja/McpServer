using McpServer.Client;
using McpServer.Client.Models;

namespace McpServer.Repl.Core;

/// <summary>
/// Default REPL transcript workflow backed by <see cref="SessionLogClient"/>.
/// </summary>
public sealed class TranscriptIngestionWorkflow : ITranscriptIngestionWorkflow
{
    private readonly SessionLogClient _sessionLogClient;

    /// <summary>
    /// Initializes a transcript ingestion workflow.
    /// </summary>
    /// <param name="sessionLogClient">Typed session-log client.</param>
    public TranscriptIngestionWorkflow(SessionLogClient sessionLogClient)
    {
        _sessionLogClient = sessionLogClient ?? throw new ArgumentNullException(nameof(sessionLogClient));
    }

    /// <inheritdoc />
    public Task<TranscriptIngestRunResponse> IngestTranscriptsAsync(
        TranscriptIngestPathRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return IngestLocalOrPathAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<TranscriptIngestRunResponse> NormalizeTranscriptsAsync(
        TranscriptIngestPathRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return IngestLocalOrPathAsync(request, cancellationToken);
    }

    private async Task<TranscriptIngestRunResponse> IngestLocalOrPathAsync(
        TranscriptIngestPathRequest request,
        CancellationToken cancellationToken)
    {
        var uploadRequest = await TryCreateUploadRequestAsync(request, cancellationToken).ConfigureAwait(false);
        if (uploadRequest is not null)
            return await _sessionLogClient.IngestTranscriptUploadAsync(uploadRequest, cancellationToken).ConfigureAwait(false);

        return await _sessionLogClient.IngestTranscriptPathAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<TranscriptIngestUploadRequest?> TryCreateUploadRequestAsync(
        TranscriptIngestPathRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Path))
            return null;

        var fullPath = Path.GetFullPath(request.Path);
        List<TranscriptUploadFile> files;
        if (File.Exists(fullPath))
        {
            files = [await CreateUploadFileAsync(fullPath, Path.GetFileName(fullPath), cancellationToken).ConfigureAwait(false)];
        }
        else if (Directory.Exists(fullPath))
        {
            var searchOption = request.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var discovered = Directory.EnumerateFiles(fullPath, "*", searchOption)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            files = new List<TranscriptUploadFile>(discovered.Length);
            foreach (var file in discovered)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var relativeName = NormalizeRelativeFileName(Path.GetRelativePath(fullPath, file));
                files.Add(await CreateUploadFileAsync(file, relativeName, cancellationToken).ConfigureAwait(false));
            }
        }
        else
        {
            return null;
        }

        return new TranscriptIngestUploadRequest
        {
            Agent = request.Agent,
            Source = request.Source,
            Recursive = request.Recursive,
            Strict = request.Strict,
            Persist = request.Persist,
            CompatibilityProfile = request.CompatibilityProfile,
            EmitNormalizedProfile = request.EmitNormalizedProfile,
            Files = files,
        };
    }

    private static async Task<TranscriptUploadFile> CreateUploadFileAsync(
        string path,
        string fileName,
        CancellationToken cancellationToken)
    {
        return new TranscriptUploadFile
        {
            FileName = fileName,
            ContentType = GetContentType(path),
            Content = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false),
        };
    }

    private static string NormalizeRelativeFileName(string fileName)
    {
        return fileName
            .Replace(Path.DirectorySeparatorChar, '/')
            .Replace(Path.AltDirectorySeparatorChar, '/');
    }

    private static string GetContentType(string path)
    {
        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".json" => "application/json",
            ".jsonl" => "application/jsonl",
            ".zip" => "application/zip",
            ".db" or ".sqlite" or ".sqlite3" => "application/vnd.sqlite3",
            _ => "application/octet-stream",
        };
    }
}
