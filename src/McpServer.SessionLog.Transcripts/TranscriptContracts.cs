namespace McpServer.SessionLog.Transcripts;

/// <summary>Identifies a supported transcript source format.</summary>
public enum TranscriptSourceKind
{
    /// <summary>Automatically detect the transcript source kind.</summary>
    Auto = 0,

    /// <summary>Claude Code JSONL transcript format.</summary>
    Claude = 1,

    /// <summary>OpenAI Codex JSONL transcript format.</summary>
    Codex = 2,

    /// <summary>Grok chat or event transcript format.</summary>
    Grok = 3,

    /// <summary>Cline paired session/messages JSON or JSONL export format.</summary>
    Cline = 4,

    /// <summary>GitHub Copilot CLI event-stream format.</summary>
    Copilot = 5,

    /// <summary>OpenCode JSONL, export JSON, or read-only store snapshot format.</summary>
    OpenCode = 6
}

/// <summary>Optional compatibility profile emitted alongside canonical Session Log YAML.</summary>
public enum TranscriptCompatibilityProfile
{
    /// <summary>No compatibility JSONL artifact is requested.</summary>
    None = 0,

    /// <summary>Project normalized events into Claude-compatible JSONL.</summary>
    Claude = 1,

    /// <summary>Project normalized events into Codex-compatible JSONL.</summary>
    Codex = 2,

    /// <summary>Project normalized events into Grok-compatible JSONL.</summary>
    Grok = 3
}

/// <summary>Compatibility artifact emitted from a normalized transcript session.</summary>
public sealed class TranscriptCompatibilityArtifact
{
    /// <summary>Initializes a compatibility artifact.</summary>
    /// <param name="profile">Profile used to project the artifact.</param>
    /// <param name="fileName">Deterministic artifact file name.</param>
    /// <param name="content">Projected JSONL content.</param>
    public TranscriptCompatibilityArtifact(TranscriptCompatibilityProfile profile, string fileName, string content)
    {
        Profile = profile;
        FileName = fileName;
        Content = content;
    }

    /// <summary>Profile used to project the artifact.</summary>
    public TranscriptCompatibilityProfile Profile { get; }

    /// <summary>Deterministic artifact file name.</summary>
    public string FileName { get; }

    /// <summary>Projected JSONL content.</summary>
    public string Content { get; }
}

/// <summary>Diagnostic emitted while detecting or normalizing transcript input.</summary>
public sealed class TranscriptDiagnostic
{
    /// <summary>Initializes a transcript diagnostic.</summary>
    /// <param name="code">Stable diagnostic code.</param>
    /// <param name="message">Human-readable diagnostic message.</param>
    /// <param name="severity">Diagnostic severity, such as warning or error.</param>
    /// <param name="path">Optional source path associated with the diagnostic.</param>
    public TranscriptDiagnostic(string code, string message, string severity = "warning", string? path = null)
    {
        Code = code;
        Message = message;
        Severity = severity;
        Path = path;
    }

    /// <summary>Stable diagnostic code.</summary>
    public string Code { get; }

    /// <summary>Human-readable diagnostic message.</summary>
    public string Message { get; }

    /// <summary>Diagnostic severity, such as warning or error.</summary>
    public string Severity { get; }

    /// <summary>Optional source path associated with the diagnostic.</summary>
    public string? Path { get; }
}

/// <summary>One normalized content block in a transcript event.</summary>
public sealed class TranscriptContentBlock
{
    /// <summary>Initializes a normalized content block.</summary>
    /// <param name="type">Native or normalized block type.</param>
    /// <param name="text">Optional text payload.</param>
    public TranscriptContentBlock(string type, string? text)
    {
        Type = type;
        Text = text;
    }

    /// <summary>Native or normalized block type.</summary>
    public string Type { get; }

    /// <summary>Optional text payload.</summary>
    public string? Text { get; }
}

/// <summary>One normalized event in a transcript session.</summary>
public sealed class TranscriptEvent
{
    /// <summary>Initializes a normalized transcript event.</summary>
    /// <param name="id">Stable event identifier.</param>
    /// <param name="order">One-based ordering value.</param>
    /// <param name="role">Normalized role.</param>
    /// <param name="nativeType">Native provider event type.</param>
    /// <param name="content">Normalized content blocks.</param>
    /// <param name="timestampUtc">Optional event timestamp in UTC.</param>
    /// <param name="metadata">Provider metadata retained as simple strings.</param>
    public TranscriptEvent(
        string id,
        int order,
        string role,
        string nativeType,
        IReadOnlyList<TranscriptContentBlock> content,
        DateTimeOffset? timestampUtc = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        Id = id;
        Order = order;
        Role = role;
        NativeType = nativeType;
        Content = content;
        TimestampUtc = timestampUtc;
        Metadata = metadata ?? new Dictionary<string, string>(StringComparer.Ordinal);
    }

    /// <summary>Stable event identifier.</summary>
    public string Id { get; }

    /// <summary>One-based ordering value.</summary>
    public int Order { get; }

    /// <summary>Normalized role.</summary>
    public string Role { get; }

    /// <summary>Native provider event type.</summary>
    public string NativeType { get; }

    /// <summary>Normalized content blocks.</summary>
    public IReadOnlyList<TranscriptContentBlock> Content { get; }

    /// <summary>Optional event timestamp in UTC.</summary>
    public DateTimeOffset? TimestampUtc { get; }

    /// <summary>Provider metadata retained as simple strings.</summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }
}

/// <summary>Normalized transcript session with canonical YAML projection.</summary>
public sealed class TranscriptSession
{
    /// <summary>Initializes a normalized transcript session.</summary>
    /// <param name="sourceKind">Source transcript kind.</param>
    /// <param name="sessionId">Canonical session identifier.</param>
    /// <param name="events">Normalized events.</param>
    /// <param name="canonicalYaml">Canonical Session Log YAML projection.</param>
    /// <param name="nativeSessionId">Original provider session identifier.</param>
    /// <param name="model">Optional model identifier.</param>
    /// <param name="workspacePath">Optional source workspace path.</param>
    /// <param name="diagnostics">Diagnostics emitted while normalizing this session.</param>
    /// <param name="sourceFiles">Source files that contributed to the session.</param>
    /// <param name="compatibilityArtifact">Optional compatibility JSONL artifact.</param>
    public TranscriptSession(
        TranscriptSourceKind sourceKind,
        string sessionId,
        IReadOnlyList<TranscriptEvent> events,
        string canonicalYaml,
        string? nativeSessionId = null,
        string? model = null,
        string? workspacePath = null,
        IReadOnlyList<TranscriptDiagnostic>? diagnostics = null,
        IReadOnlyList<string>? sourceFiles = null,
        TranscriptCompatibilityArtifact? compatibilityArtifact = null)
    {
        SourceKind = sourceKind;
        SessionId = sessionId;
        NativeSessionId = nativeSessionId;
        Model = model;
        WorkspacePath = workspacePath;
        Events = events;
        CanonicalYaml = canonicalYaml;
        Diagnostics = diagnostics ?? [];
        SourceFiles = sourceFiles ?? [];
        CompatibilityArtifact = compatibilityArtifact;
    }

    /// <summary>Source transcript kind.</summary>
    public TranscriptSourceKind SourceKind { get; }

    /// <summary>Canonical session identifier.</summary>
    public string SessionId { get; }

    /// <summary>Original provider session identifier.</summary>
    public string? NativeSessionId { get; }

    /// <summary>Optional model identifier.</summary>
    public string? Model { get; }

    /// <summary>Optional source workspace path.</summary>
    public string? WorkspacePath { get; }

    /// <summary>Normalized events.</summary>
    public IReadOnlyList<TranscriptEvent> Events { get; }

    /// <summary>Canonical Session Log YAML projection.</summary>
    public string CanonicalYaml { get; }

    /// <summary>Diagnostics emitted while normalizing this session.</summary>
    public IReadOnlyList<TranscriptDiagnostic> Diagnostics { get; }

    /// <summary>Source files that contributed to the session.</summary>
    public IReadOnlyList<string> SourceFiles { get; }

    /// <summary>Optional compatibility JSONL artifact projected from the neutral session model.</summary>
    public TranscriptCompatibilityArtifact? CompatibilityArtifact { get; }
}

/// <summary>Detected source bundle that should normalize as one transcript session.</summary>
public sealed class TranscriptBundle
{
    /// <summary>Initializes a detected transcript bundle.</summary>
    /// <param name="rootPath">Bundle root path.</param>
    /// <param name="sourceKind">Detected source kind.</param>
    /// <param name="files">Files in the bundle.</param>
    public TranscriptBundle(string rootPath, TranscriptSourceKind sourceKind, IReadOnlyList<string> files)
    {
        RootPath = rootPath;
        SourceKind = sourceKind;
        Files = files;
    }

    /// <summary>Bundle root path.</summary>
    public string RootPath { get; }

    /// <summary>Detected source kind.</summary>
    public TranscriptSourceKind SourceKind { get; }

    /// <summary>Files in the bundle.</summary>
    public IReadOnlyList<string> Files { get; }
}

/// <summary>Options for one transcript ingestion request.</summary>
public sealed class TranscriptIngestionRequest
{
    /// <summary>Initializes an ingestion request for a local path.</summary>
    /// <param name="path">File or directory path to ingest.</param>
    public TranscriptIngestionRequest(string path)
    {
        Path = path;
    }

    /// <summary>File or directory path to ingest.</summary>
    public string Path { get; }

    /// <summary>Requested source kind, or Auto to detect.</summary>
    public TranscriptSourceKind SourceKind { get; init; } = TranscriptSourceKind.Auto;

    /// <summary>Whether recursive folder discovery is enabled.</summary>
    public bool Recursive { get; init; } = true;

    /// <summary>Whether malformed records should fail the bundle.</summary>
    public bool Strict { get; init; } = true;

    /// <summary>Whether the result should be persisted by a higher layer.</summary>
    public bool Persist { get; init; } = true;

    /// <summary>Optional compatibility profile to emit.</summary>
    public TranscriptCompatibilityProfile CompatibilityProfile { get; init; } = TranscriptCompatibilityProfile.None;

    /// <summary>Agent name that owns workspace transcript cache artifacts.</summary>
    public string? Agent { get; init; }

    /// <summary>Workspace root where `.mcpServer/{agent}` artifacts are stored.</summary>
    public string? WorkspacePath { get; init; }

    /// <summary>Additional transcript roots that may be read outside the active workspace.</summary>
    public IReadOnlyList<string> ProviderTranscriptRoots { get; init; } = [];

    /// <summary>Optional caller-provided run identifier for deterministic tests and idempotent callers.</summary>
    public string? RunId { get; init; }
}

/// <summary>Receipt for one normalized transcript session in an ingestion run.</summary>
public sealed class TranscriptSessionReceipt
{
    /// <summary>Initializes a transcript session receipt.</summary>
    /// <param name="sourceKind">Detected source kind.</param>
    /// <param name="rootId">Root identifier of the source data unit.</param>
    /// <param name="sessionId">Canonical session identifier.</param>
    /// <param name="sourceHash">Deterministic hash of source file identities.</param>
    /// <param name="status">Import status for this session.</param>
    /// <param name="yamlArtifactPath">Canonical Session Log YAML artifact path.</param>
    /// <param name="importRecoveryPath">Pending import recovery envelope path named from the root identifier.</param>
    /// <param name="compatibilityArtifactPath">Optional compatibility JSONL artifact path.</param>
    /// <param name="diagnostics">Session diagnostics.</param>
    /// <param name="persistenceReceipt">Receipt returned by the primary session-log persistence path.</param>
    public TranscriptSessionReceipt(
        TranscriptSourceKind sourceKind,
        string rootId,
        string sessionId,
        string sourceHash,
        string status,
        string yamlArtifactPath,
        string importRecoveryPath,
        string? compatibilityArtifactPath = null,
        IReadOnlyList<TranscriptDiagnostic>? diagnostics = null,
        string? persistenceReceipt = null)
    {
        SourceKind = sourceKind;
        RootId = rootId;
        SessionId = sessionId;
        SourceHash = sourceHash;
        Status = status;
        YamlArtifactPath = yamlArtifactPath;
        ImportRecoveryPath = importRecoveryPath;
        CompatibilityArtifactPath = compatibilityArtifactPath;
        Diagnostics = diagnostics ?? [];
        PersistenceReceipt = persistenceReceipt;
    }

    /// <summary>Detected source kind.</summary>
    public TranscriptSourceKind SourceKind { get; }

    /// <summary>Root identifier of the source data unit.</summary>
    public string RootId { get; }

    /// <summary>Canonical session identifier.</summary>
    public string SessionId { get; }

    /// <summary>Deterministic hash of source file identities.</summary>
    public string SourceHash { get; }

    /// <summary>Import status for this session.</summary>
    public string Status { get; }

    /// <summary>Canonical Session Log YAML artifact path.</summary>
    public string YamlArtifactPath { get; }

    /// <summary>Pending import recovery envelope path named from the root identifier.</summary>
    public string ImportRecoveryPath { get; }

    /// <summary>Optional compatibility JSONL artifact path.</summary>
    public string? CompatibilityArtifactPath { get; }

    /// <summary>Session diagnostics.</summary>
    public IReadOnlyList<TranscriptDiagnostic> Diagnostics { get; }

    /// <summary>Receipt returned by the primary session-log persistence path.</summary>
    public string? PersistenceReceipt { get; }
}

/// <summary>Result from a transcript ingestion run.</summary>
public sealed class TranscriptIngestionResult
{
    /// <summary>Initializes an ingestion result.</summary>
    /// <param name="sessions">Normalized sessions.</param>
    /// <param name="diagnostics">Run diagnostics.</param>
    /// <param name="runId">Optional ingestion run identifier.</param>
    /// <param name="artifactRootPath">Optional run artifact root path.</param>
    /// <param name="importRecoveryPaths">Pending import recovery envelope paths.</param>
    /// <param name="persisted">Whether all sessions were imported through the session-log persistence path.</param>
    /// <param name="degraded">Whether persistence is degraded and pending recovery must be retained.</param>
    /// <param name="receipts">Per-session ingestion receipts.</param>
    public TranscriptIngestionResult(
        IReadOnlyList<TranscriptSession> sessions,
        IReadOnlyList<TranscriptDiagnostic> diagnostics,
        string? runId = null,
        string? artifactRootPath = null,
        IReadOnlyList<string>? importRecoveryPaths = null,
        bool persisted = false,
        bool degraded = false,
        IReadOnlyList<TranscriptSessionReceipt>? receipts = null)
    {
        Sessions = sessions;
        Diagnostics = diagnostics;
        RunId = runId;
        ArtifactRootPath = artifactRootPath;
        ImportRecoveryPaths = importRecoveryPaths ?? [];
        Persisted = persisted;
        Degraded = degraded;
        Receipts = receipts ?? [];
    }

    /// <summary>Normalized sessions.</summary>
    public IReadOnlyList<TranscriptSession> Sessions { get; }

    /// <summary>Run diagnostics.</summary>
    public IReadOnlyList<TranscriptDiagnostic> Diagnostics { get; }

    /// <summary>Optional ingestion run identifier.</summary>
    public string? RunId { get; }

    /// <summary>Optional run artifact root path.</summary>
    public string? ArtifactRootPath { get; }

    /// <summary>Pending import recovery envelope paths.</summary>
    public IReadOnlyList<string> ImportRecoveryPaths { get; }

    /// <summary>Whether all sessions were imported through the session-log persistence path.</summary>
    public bool Persisted { get; }

    /// <summary>Whether persistence is degraded and pending recovery must be retained.</summary>
    public bool Degraded { get; }

    /// <summary>Per-session ingestion receipts.</summary>
    public IReadOnlyList<TranscriptSessionReceipt> Receipts { get; }
}

/// <summary>Detects one or more transcript bundles under a path.</summary>
public interface ITranscriptBundleDetector
{
    /// <summary>Detects transcript bundles under the supplied path.</summary>
    /// <param name="path">File or directory path.</param>
    /// <param name="recursive">Whether directory detection should recurse.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Detected bundles.</returns>
    Task<IReadOnlyList<TranscriptBundle>> DetectAsync(string path, bool recursive, CancellationToken cancellationToken = default);
}

/// <summary>Normalizes a detected transcript bundle.</summary>
public interface ITranscriptSourceAdapter
{
    /// <summary>Gets the source kind supported by this adapter.</summary>
    TranscriptSourceKind SourceKind { get; }

    /// <summary>Normalizes a detected transcript bundle.</summary>
    /// <param name="bundle">Detected bundle.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Normalized transcript session.</returns>
    Task<TranscriptSession> NormalizeAsync(TranscriptBundle bundle, CancellationToken cancellationToken = default);
}

/// <summary>Projects a normalized transcript session to an optional provider compatibility profile.</summary>
public interface ITranscriptProfileProjector
{
    /// <summary>Gets the compatibility profile emitted by this projector.</summary>
    TranscriptCompatibilityProfile Profile { get; }

    /// <summary>Projects a normalized transcript session.</summary>
    /// <param name="session">Normalized transcript session.</param>
    /// <returns>Compatibility artifact content.</returns>
    string Project(TranscriptSession session);
}

/// <summary>Persists normalized transcript sessions through the primary session-log persistence path.</summary>
public interface ITranscriptSessionPersister
{
    /// <summary>Persists one normalized transcript session after its write-ahead recovery envelope has been created.</summary>
    /// <param name="request">Original ingestion request.</param>
    /// <param name="session">Normalized transcript session.</param>
    /// <param name="receipt">Pending session receipt whose recovery envelope is still present.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A persistence receipt from the primary session-log persistence path.</returns>
    Task<string> PersistAsync(
        TranscriptIngestionRequest request,
        TranscriptSession session,
        TranscriptSessionReceipt receipt,
        CancellationToken cancellationToken = default);
}

/// <summary>Coordinates transcript detection, normalization, projection, and later persistence.</summary>
public interface ITranscriptIngestionService
{
    /// <summary>Ingests transcript input from a file or directory path.</summary>
    /// <param name="request">Ingestion request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Ingestion result.</returns>
    Task<TranscriptIngestionResult> IngestPathAsync(TranscriptIngestionRequest request, CancellationToken cancellationToken = default);
}
