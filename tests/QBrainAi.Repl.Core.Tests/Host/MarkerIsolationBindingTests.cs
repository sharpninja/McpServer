using QBrainAi.Client;
using QBrainAi.Repl.Host;

namespace QBrainAi.Repl.Core.Tests.Host;

/// <summary>
/// M2 binding of the same owned-tree and signed-marker cache assertions to the real resolver. The filesystem owns
/// every path and cache interaction; explicit workspace, marker and agent inputs prevent ambient fallback reads.
/// </summary>
public sealed class MarkerIsolationBindingTests : MarkerIsolationContractTests
{
    /// <inheritdoc />
    private protected override string? Walk(string start, RecordingMarkerFileSystem files, out IReadOnlyList<string> searched) =>
        MarkerFileClientOptionsResolver.FindMarkerFile(start, out searched, files);

    /// <inheritdoc />
    private protected override bool Resolve(RecordingMarkerFileSystem files, out QBrainAiClientOptions? options, out string error) =>
        MarkerFileClientOptionsResolver.TryResolveWithDiagnostics(RecordingMarkerFileSystem.Root,
            files.GetMarkerPath(RecordingMarkerFileSystem.Root), out options, out error, "Codex", files);
}
