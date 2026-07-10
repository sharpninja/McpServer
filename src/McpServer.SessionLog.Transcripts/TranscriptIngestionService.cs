namespace McpServer.SessionLog.Transcripts;

/// <summary>Default transcript ingestion service backed by source adapters.</summary>
public sealed class TranscriptIngestionService : ITranscriptIngestionService
{
    private readonly ITranscriptBundleDetector _detector;
    private readonly IReadOnlyDictionary<TranscriptSourceKind, ITranscriptSourceAdapter> _adapters;
    private readonly IReadOnlyDictionary<TranscriptCompatibilityProfile, ITranscriptProfileProjector> _projectors;
    private readonly ITranscriptSessionPersister? _persister;

    /// <summary>Initializes a transcript ingestion service.</summary>
    /// <param name="detector">Bundle detector.</param>
    /// <param name="adapters">Source adapters.</param>
    /// <param name="projectors">Optional compatibility profile projectors.</param>
    /// <param name="persister">Optional primary session-log persister.</param>
    public TranscriptIngestionService(
        ITranscriptBundleDetector detector,
        IEnumerable<ITranscriptSourceAdapter> adapters,
        IEnumerable<ITranscriptProfileProjector>? projectors = null,
        ITranscriptSessionPersister? persister = null)
    {
        _detector = detector ?? throw new ArgumentNullException(nameof(detector));
        _adapters = adapters?.ToDictionary(adapter => adapter.SourceKind) ?? throw new ArgumentNullException(nameof(adapters));
        _projectors = projectors?.ToDictionary(projector => projector.Profile) ?? new Dictionary<TranscriptCompatibilityProfile, ITranscriptProfileProjector>();
        _persister = persister;
    }

    /// <summary>Creates the default detector and source adapter set.</summary>
    /// <param name="persister">Optional primary session-log persister.</param>
    /// <returns>A default transcript ingestion service.</returns>
    public static TranscriptIngestionService CreateDefault(ITranscriptSessionPersister? persister = null)
    {
        return new TranscriptIngestionService(
            new TranscriptBundleDetector(),
            [
                new ClaudeTranscriptAdapter(),
                new CodexTranscriptAdapter(),
                new GrokTranscriptAdapter(),
                new ClineTranscriptAdapter(),
                new CopilotTranscriptAdapter(),
                new OpenCodeTranscriptAdapter()
            ],
            [
                new ClaudeTranscriptProfileProjector(),
                new CodexTranscriptProfileProjector(),
                new GrokTranscriptProfileProjector()
            ],
            persister);
    }

    /// <inheritdoc />
    public async Task<TranscriptIngestionResult> IngestPathAsync(TranscriptIngestionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Persist)
            TranscriptRunArtifactWriter.ValidatePersistenceRequest(request);
        var inputPath = TranscriptPathSecurity.ValidateReadablePath(request);

        var diagnostics = new List<TranscriptDiagnostic>();
        var bundles = request.SourceKind == TranscriptSourceKind.Auto
            ? await _detector.DetectAsync(inputPath, request.Recursive, cancellationToken).ConfigureAwait(false)
            : await BuildExplicitBundlesAsync(request, inputPath, cancellationToken).ConfigureAwait(false);

        var sessions = new List<TranscriptSession>();
        foreach (var bundle in bundles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_adapters.TryGetValue(bundle.SourceKind, out var adapter))
            {
                diagnostics.Add(new TranscriptDiagnostic("adapter_missing", "No transcript adapter is registered for " + bundle.SourceKind, "error", bundle.RootPath));
                continue;
            }

            try
            {
                var session = await adapter.NormalizeAsync(bundle, cancellationToken).ConfigureAwait(false);
                sessions.Add(ProjectCompatibility(session, request.CompatibilityProfile));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                diagnostics.Add(new TranscriptDiagnostic("normalize_failed", ex.Message, request.Strict ? "error" : "warning", bundle.RootPath));
                if (request.Strict)
                    throw;
            }
        }

        if (request.Persist)
        {
            var pending = await TranscriptRunArtifactWriter.WritePendingAsync(request, sessions, diagnostics, cancellationToken).ConfigureAwait(false);
            return _persister is null
                ? pending
                : await PersistPendingAsync(request, pending, cancellationToken).ConfigureAwait(false);
        }

        return new TranscriptIngestionResult(sessions, diagnostics);
    }

    private async Task<TranscriptIngestionResult> PersistPendingAsync(
        TranscriptIngestionRequest request,
        TranscriptIngestionResult pending,
        CancellationToken cancellationToken)
    {
        var diagnostics = pending.Diagnostics.ToList();
        var receipts = new List<TranscriptSessionReceipt>();
        var retainedRecoveryPaths = new List<string>();
        var allPersisted = true;

        for (var i = 0; i < pending.Receipts.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var receipt = pending.Receipts[i];
            var session = pending.Sessions[i];
            try
            {
                var persistenceReceipt = await _persister!.PersistAsync(request, session, receipt, cancellationToken).ConfigureAwait(false);
                DeleteRecoveryFile(receipt.ImportRecoveryPath);
                receipts.Add(CloneReceipt(receipt, "persisted", persistenceReceipt));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                allPersisted = false;
                retainedRecoveryPaths.Add(receipt.ImportRecoveryPath);
                diagnostics.Add(new TranscriptDiagnostic("persistence_failed", ex.Message, "error", receipt.YamlArtifactPath));
                receipts.Add(receipt);
            }
        }

        return new TranscriptIngestionResult(
            pending.Sessions,
            diagnostics,
            pending.RunId,
            pending.ArtifactRootPath,
            retainedRecoveryPaths,
            persisted: allPersisted,
            degraded: !allPersisted,
            receipts);
    }

    private static TranscriptSessionReceipt CloneReceipt(TranscriptSessionReceipt receipt, string status, string? persistenceReceipt)
    {
        return new TranscriptSessionReceipt(
            receipt.SourceKind,
            receipt.RootId,
            receipt.SessionId,
            receipt.SourceHash,
            status,
            receipt.YamlArtifactPath,
            receipt.ImportRecoveryPath,
            receipt.CompatibilityArtifactPath,
            receipt.Diagnostics,
            persistenceReceipt);
    }

    private static void DeleteRecoveryFile(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }
    private TranscriptSession ProjectCompatibility(TranscriptSession session, TranscriptCompatibilityProfile profile)
    {
        if (profile == TranscriptCompatibilityProfile.None)
            return session;

        if (!_projectors.TryGetValue(profile, out var projector))
            throw new InvalidOperationException("No transcript compatibility projector is registered for " + profile + ".");

        var fileName = session.SessionId + "." + profile.ToString().ToLowerInvariant() + ".jsonl";
        var artifact = new TranscriptCompatibilityArtifact(profile, fileName, projector.Project(session));
        return new TranscriptSession(
            session.SourceKind,
            session.SessionId,
            session.Events,
            session.CanonicalYaml,
            session.NativeSessionId,
            session.Model,
            session.WorkspacePath,
            session.Diagnostics,
            session.SourceFiles,
            artifact);
    }

    private static async Task<IReadOnlyList<TranscriptBundle>> BuildExplicitBundlesAsync(TranscriptIngestionRequest request, string inputPath, CancellationToken cancellationToken)
    {
        var fullPath = Path.GetFullPath(inputPath);
        if (request.SourceKind == TranscriptSourceKind.Cline && Directory.Exists(fullPath))
        {
            var sessionPath = Path.Combine(fullPath, "session.json");
            var messagesPath = Path.Combine(fullPath, "messages.json");
            return [new TranscriptBundle(fullPath, TranscriptSourceKind.Cline, [sessionPath, messagesPath])];
        }

        if (File.Exists(fullPath))
            return [new TranscriptBundle(fullPath, request.SourceKind, [fullPath])];

        if (Directory.Exists(fullPath))
        {
            var detector = new TranscriptBundleDetector();
            var detected = await detector.DetectAsync(fullPath, request.Recursive, cancellationToken).ConfigureAwait(false);
            return detected.Where(bundle => bundle.SourceKind == request.SourceKind).ToArray();
        }

        throw new FileNotFoundException("Transcript path does not exist.", fullPath);
    }
}
