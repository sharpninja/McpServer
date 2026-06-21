using McpServer.McpAgent;
using McpServer.QBAgent;

namespace McpServer.QBAgent.Tests;

/// <summary>
/// TEST-MCP-QBAGENT-001: Verifies QBAgent marker bootstrap (FR-MCP-QBAGENT-001) - it binds the QuadBrain
/// endpoint and API key from the AGENTS-README-FIRST.yaml marker, applies the QBAgent profile, rejects an
/// invalid marker, and reports a graceful no-marker exit when the marker is absent.
/// </summary>
public sealed class QBAgentBootstrapperTests
{
    /// <summary>With no marker in the start directory, bootstrap reports a graceful no-marker exit and binds nothing.</summary>
    [Fact]
    public void Bootstrap_NoMarker_ReportsGracefulExit()
    {
        using var dir = new TempDirectory();

        var result = QBAgentBootstrapper.Bootstrap(dir.Path);

        Assert.Equal(QBAgentBootstrapStatus.NoMarker, result.Status);
        Assert.Null(result.Options);
        Assert.Contains("exiting gracefully", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>A valid marker binds baseUrl and apiKey from the marker and applies the QBAgent (QuadBrain-only) profile.</summary>
    [Fact]
    public void Bootstrap_ValidMarker_BindsQuadBrainEndpointAndProfile()
    {
        using var dir = new TempDirectory();
        dir.WriteMarker(
            "port: 7147",
            "baseUrl: http://PAYTON-LEGION2:7147",
            "apiKey: test-key-123",
            "workspace: McpServer",
            "workspacePath: F:\\GitHub\\McpServer",
            "prompt: |",
            "  This block must be ignored: baseUrl http://wrong:9999 apiKey wrong");

        var result = QBAgentBootstrapper.Bootstrap(dir.Path);

        Assert.Equal(QBAgentBootstrapStatus.Started, result.Status);
        Assert.NotNull(result.Options);
        Assert.Equal(new Uri("http://PAYTON-LEGION2:7147"), result.Options!.BaseUrl);
        Assert.Equal("test-key-123", result.Options.ApiKey);
        Assert.Equal("F:\\GitHub\\McpServer", result.Options.WorkspacePath);
        // QBAgent identity is applied; the standard (non-ACID) profile keeps action tools available.
        Assert.Equal("QBAgent", result.Options.SourceType);
        Assert.NotEqual(McpAgentExecutionProfile.AcidTightlyCoupled, result.Options.ExecutionProfile);
    }

    /// <summary>A marker missing the apiKey is rejected as invalid (QBAgent cannot bind to QuadBrain).</summary>
    [Fact]
    public void Bootstrap_MarkerMissingApiKey_ReturnsInvalid()
    {
        using var dir = new TempDirectory();
        dir.WriteMarker("baseUrl: http://localhost:7147");

        var result = QBAgentBootstrapper.Bootstrap(dir.Path);

        Assert.Equal(QBAgentBootstrapStatus.InvalidMarker, result.Status);
        Assert.Null(result.Options);
    }

    /// <summary>A marker with a non-absolute baseUrl is rejected as invalid.</summary>
    [Fact]
    public void Bootstrap_MarkerInvalidBaseUrl_ReturnsInvalid()
    {
        using var dir = new TempDirectory();
        dir.WriteMarker("baseUrl: not-a-uri", "apiKey: test-key");

        var result = QBAgentBootstrapper.Bootstrap(dir.Path);

        Assert.Equal(QBAgentBootstrapStatus.InvalidMarker, result.Status);
        Assert.Null(result.Options);
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "qbagent-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void WriteMarker(params string[] lines)
            => File.WriteAllLines(System.IO.Path.Combine(Path, QBAgentBootstrapper.MarkerFileName), lines);

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}
