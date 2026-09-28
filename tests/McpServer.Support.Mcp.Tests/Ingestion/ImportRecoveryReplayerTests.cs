using McpServer.SessionLog.Transcripts;

namespace McpServer.Support.Mcp.Tests.Ingestion;

/// <summary>
/// TEST-MCP-SESSIONLIFE-004: import-recovery replay deletes only the envelope after durable persist.
/// </summary>
public sealed class ImportRecoveryReplayerTests
{
    /// <summary>A successful persist deletes the envelope and leaves the canonical artifact.</summary>
    [Fact]
    public async Task ReplayDirectory_DeletesOnlySuccessfulEnvelope()
    {
        var dir = Path.Combine(Path.GetTempPath(), "import-recovery-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var artifact = Path.Combine(dir, "session.yaml");
            await File.WriteAllTextAsync(artifact, "session: keep\n", TestContext.Current.CancellationToken);
            var success = Path.Combine(dir, "ok.importRecovery.yaml");
            var failure = Path.Combine(dir, "no.importRecovery.yaml");
            await File.WriteAllTextAsync(success, "importRecovery:\n  yamlArtifactPath: session.yaml\n  persisted: false\n  degraded: true\n", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(failure, "importRecovery:\n  yamlArtifactPath: session.yaml\n  persisted: false\n  degraded: true\n", TestContext.Current.CancellationToken);

            var deleted = await ImportRecoveryReplayer.ReplayDirectoryAsync(
                dir,
                (path, yaml, _) => Task.FromResult(path.EndsWith("ok.importRecovery.yaml", StringComparison.Ordinal) && yaml.Contains("yamlArtifactPath:", StringComparison.Ordinal)),
                TestContext.Current.CancellationToken);

            Assert.Equal(1, deleted);
            Assert.False(File.Exists(success));
            Assert.True(File.Exists(failure));
            Assert.True(File.Exists(artifact));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
