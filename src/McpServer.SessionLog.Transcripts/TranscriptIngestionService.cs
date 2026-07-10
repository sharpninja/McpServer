namespace McpServer.SessionLog.Transcripts;

/// <summary>Default transcript ingestion service backed by source adapters.</summary>
public sealed class TranscriptIngestionService : ITranscriptIngestionService
{
    private readonly ITranscriptBundleDetector _detector;
    private readonly IReadOnlyDictionary<TranscriptSourceKind, ITranscriptSourceAdapter> _adapters;
    private readonly IReadOnlyDictionary<TranscriptCompatibilityProfile, ITranscriptProfileProjector> _projectors;

    /// <summary>Initializes a transcript ingestion service.</summary>
    /// <param name="detector">Bundle detector.</param>
    /// <param name="adapters">Source adapters.</param>
    /// <param name="projectors">Optional compatibility profile projectors.</param>
    public TranscriptIngestionService(
        ITranscriptBundleDetector detector,
        IEnumerable<ITranscriptSourceAdapter> adapters,
        IEnumerable<ITranscriptProfileProjector>? projectors = null)
    {
        _detector = detector ?? throw new ArgumentNullException(nameof(detector));
        _adapters = adapters?.ToDictionary(adapter => adapter.SourceKind) ?? throw new ArgumentNullException(nameof(adapters));
        _projectors = projectors?.ToDictionary(projector => projector.Profile) ?? new Dictionary<TranscriptCompatibilityProfile, ITranscriptProfileProjector>();
    }

    /// <summary>Creates the default detector and source adapter set.</summary>
    /// <returns>A default transcript ingestion service.</returns>
    public static TranscriptIngestionService CreateDefault()
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
            ]);
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
            return await TranscriptRunArtifactWriter.WritePendingAsync(request, sessions, diagnostics, cancellationToken).ConfigureAwait(false);

        return new TranscriptIngestionResult(sessions, diagnostics);
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
