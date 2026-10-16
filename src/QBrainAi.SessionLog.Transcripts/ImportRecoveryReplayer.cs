using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace QBrainAi.SessionLog.Transcripts;

/// <summary>
/// FR-MCP-SESSIONLIFE-004: replays pending import-recovery envelopes and deletes only the
/// envelope file after a durable persist.
/// </summary>
public static class ImportRecoveryReplayer
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    /// <summary>
    /// Replays each <c>*.importRecovery.yaml</c> in <paramref name="pendingDirectory"/>.
    /// Deletes that envelope only when <paramref name="persist"/> returns true.
    /// Other files in the directory are left in place.
    /// </summary>
    /// <param name="pendingDirectory">Directory that holds recovery envelopes.</param>
    /// <param name="persist">Persists one envelope. The first argument is the envelope path. True means durable success.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>How many envelopes were deleted.</returns>
    public static async Task<int> ReplayDirectoryAsync(
        string pendingDirectory,
        Func<string, string, CancellationToken, Task<bool>> persist,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pendingDirectory);
        ArgumentNullException.ThrowIfNull(persist);
        if (!Directory.Exists(pendingDirectory))
            return 0;

        var deleted = 0;
        foreach (var file in Directory.GetFiles(pendingDirectory, "*.importRecovery.yaml"))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var yaml = await File.ReadAllTextAsync(file, cancellationToken).ConfigureAwait(false);
            if (ReadBody(yaml) is null)
                continue;
            var ok = await persist(file, yaml, cancellationToken).ConfigureAwait(false);
            if (!ok)
                continue;
            File.Delete(file);
            deleted++;
        }

        return deleted;
    }

    /// <summary>Reads the importRecovery body, or null when the YAML is not that envelope.</summary>
    /// <param name="yaml">Envelope YAML text.</param>
    /// <returns>The body, or null.</returns>
    public static ImportRecoveryEnvelopeBody? ReadBody(string yaml)
    {
        if (string.IsNullOrWhiteSpace(yaml))
            return null;
        try
        {
            var document = Deserializer.Deserialize<ImportRecoveryEnvelopeDocument>(yaml);
            return document?.ImportRecovery;
        }
        catch (YamlDotNet.Core.YamlException)
        {
            return null;
        }
    }
}

/// <summary>Root document for an import-recovery envelope.</summary>
public sealed class ImportRecoveryEnvelopeDocument
{
    /// <summary>Envelope body.</summary>
    public ImportRecoveryEnvelopeBody? ImportRecovery { get; set; }
}

/// <summary>Fields ingestion uses to replay a pending transcript without submitting the envelope.</summary>
public sealed class ImportRecoveryEnvelopeBody
{
    /// <summary>Canonical session YAML artifact. Replay must not delete this file.</summary>
    public string? YamlArtifactPath { get; set; }

    /// <summary>Source kind name, such as Codex.</summary>
    public string? SourceKind { get; set; }

    /// <summary>Canonical session id.</summary>
    public string? SessionId { get; set; }

    /// <summary>Root identifier.</summary>
    public string? RootId { get; set; }

    /// <summary>Source hash.</summary>
    public string? SourceHash { get; set; }

    /// <summary>Whether a previous attempt already persisted.</summary>
    public bool Persisted { get; set; }

    /// <summary>Whether the previous attempt was degraded.</summary>
    public bool Degraded { get; set; } = true;

    /// <summary>Original transcript files to normalize again.</summary>
    public List<string>? SourceFiles { get; set; }
}
