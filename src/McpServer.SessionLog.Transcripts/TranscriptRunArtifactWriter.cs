using System.Globalization;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace McpServer.SessionLog.Transcripts;

internal static class TranscriptRunArtifactWriter
{
    private static readonly ISerializer RecoverySerializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    internal static void ValidatePersistenceRequest(TranscriptIngestionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(request.Agent))
            missing.Add(nameof(request.Agent));
        if (string.IsNullOrWhiteSpace(request.WorkspacePath))
            missing.Add(nameof(request.WorkspacePath));

        if (missing.Count > 0)
            throw new ArgumentException("Transcript persistence requires " + string.Join(" and ", missing) + ".", nameof(request));
    }

    internal static async Task<TranscriptIngestionResult> WritePendingAsync(
        TranscriptIngestionRequest request,
        IReadOnlyList<TranscriptSession> sessions,
        IReadOnlyList<TranscriptDiagnostic> diagnostics,
        CancellationToken cancellationToken)
    {
        ValidatePersistenceRequest(request);
        var workspacePath = Path.GetFullPath(request.WorkspacePath!);
        var agent = SanitizePathSegment(request.Agent!);
        var runId = SanitizePathSegment(string.IsNullOrWhiteSpace(request.RunId) ? CreateRunId(sessions) : request.RunId!);
        var agentRoot = Path.Combine(workspacePath, ".mcpServer", agent);
        var artifactRoot = Path.Combine(agentRoot, "transcripts", "runs", runId);
        var recoveryRoot = Path.Combine(agentRoot, "failsafe", "pending");
        Directory.CreateDirectory(artifactRoot);
        Directory.CreateDirectory(recoveryRoot);

        var receipts = new List<TranscriptSessionReceipt>();
        var recoveryPaths = new List<string>();
        foreach (var session in sessions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var rootId = string.IsNullOrWhiteSpace(session.NativeSessionId) ? session.SessionId : session.NativeSessionId!;
            var sourceHash = TranscriptUtilities.ComputeShortHash(string.Join("|", session.SourceFiles.Select(Path.GetFullPath).Order(StringComparer.OrdinalIgnoreCase)));
            var artifactPrefix = SanitizePathSegment(session.SessionId) + "." + sourceHash;
            var yamlPath = Path.Combine(artifactRoot, artifactPrefix + ".sessionlog.yaml");
            await File.WriteAllTextAsync(yamlPath, session.CanonicalYaml, cancellationToken).ConfigureAwait(false);

            string? compatibilityPath = null;
            if (session.CompatibilityArtifact is not null)
            {
                compatibilityPath = Path.Combine(artifactRoot, SanitizePathSegment(session.CompatibilityArtifact.FileName));
                await File.WriteAllTextAsync(compatibilityPath, session.CompatibilityArtifact.Content, cancellationToken).ConfigureAwait(false);
            }

            var recoveryPath = Path.Combine(recoveryRoot, SanitizePathSegment(rootId) + "." + sourceHash + ".importRecovery.yaml");
            var receipt = new TranscriptSessionReceipt(
                session.SourceKind,
                rootId,
                session.SessionId,
                sourceHash,
                "pending",
                yamlPath,
                recoveryPath,
                compatibilityPath,
                session.Diagnostics);
            var recoveryEnvelope = CreateRecoveryEnvelope(request, runId, receipt, session);
            await File.WriteAllTextAsync(recoveryPath, RecoverySerializer.Serialize(recoveryEnvelope), cancellationToken).ConfigureAwait(false);
            receipts.Add(receipt);
            recoveryPaths.Add(recoveryPath);
        }

        return new TranscriptIngestionResult(
            sessions,
            diagnostics,
            runId,
            artifactRoot,
            recoveryPaths,
            persisted: false,
            degraded: true,
            receipts);
    }

    private static IReadOnlyDictionary<string, object?> CreateRecoveryEnvelope(
        TranscriptIngestionRequest request,
        string runId,
        TranscriptSessionReceipt receipt,
        TranscriptSession session)
    {
        return new Dictionary<string, object?>
        {
            ["importRecovery"] = new Dictionary<string, object?>
            {
                ["runId"] = runId,
                ["agent"] = request.Agent,
                ["workspacePath"] = Path.GetFullPath(request.WorkspacePath!),
                ["rootId"] = receipt.RootId,
                ["sessionId"] = receipt.SessionId,
                ["sourceKind"] = session.SourceKind.ToString(),
                ["sourceHash"] = receipt.SourceHash,
                ["persisted"] = false,
                ["degraded"] = true,
                ["status"] = receipt.Status,
                ["yamlArtifactPath"] = receipt.YamlArtifactPath,
                ["compatibilityArtifactPath"] = receipt.CompatibilityArtifactPath,
                ["sourceFiles"] = session.SourceFiles.Select(TranscriptUtilities.NormalizePath).ToArray(),
                ["diagnostics"] = session.Diagnostics.Select(diagnostic => new Dictionary<string, object?>
                {
                    ["code"] = diagnostic.Code,
                    ["severity"] = diagnostic.Severity,
                    ["message"] = diagnostic.Message,
                    ["path"] = diagnostic.Path
                }).ToArray()
            }
        };
    }

    private static string CreateRunId(IReadOnlyList<TranscriptSession> sessions)
    {
        var seed = string.Join("|", sessions.Select(session => session.SessionId).Order(StringComparer.Ordinal));
        return "run-" + DateTimeOffset.UtcNow.ToString("yyyyMMddTHHmmssfffZ", CultureInfo.InvariantCulture) + "-" + TranscriptUtilities.ComputeShortHash(seed);
    }

    private static string SanitizePathSegment(string value)
    {
        var invalid = Path.GetInvalidFileNameChars().ToHashSet();
        var chars = value.Select(ch => invalid.Contains(ch) || char.IsControl(ch) ? '_' : ch).ToArray();
        var sanitized = new string(chars).Trim('.', ' ');
        return string.IsNullOrWhiteSpace(sanitized) ? "unnamed" : sanitized;
    }
}
