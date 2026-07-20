using System.Diagnostics;
using McpServer.TestSupport.Ollama;

namespace McpServer.Support.Mcp.IntegrationTests.Controllers;

/// <summary>
/// TEST-MCP-QBOLLAMA-001 / TEST-MCP-QBOLLAMA-002: xUnit fixture that guarantees a reachable Ollama server
/// for the QuadBrain integration tests. Adopts an already-running server and leaves it alone at teardown;
/// otherwise launches 'ollama serve' from a discovered executable and terminates that process at teardown.
/// Implements FR-MCP-QBOLLAMA-002 over <see cref="OllamaServerController"/>.
/// </summary>
public sealed class OllamaServerFixture : IAsyncLifetime
{
    private const string TagsEndpoint = "http://localhost:11434/api/tags";
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(60);

    private readonly OllamaServerController _controller;

    /// <summary>Initializes the fixture with the real probe, executable resolver, and process launcher.</summary>
    public OllamaServerFixture()
        => _controller = new OllamaServerController(
            probeAsync: ProbeAsync,
            resolveExecutable: ResolveExecutable,
            startProcess: StartServerProcess,
            pollInterval: PollInterval,
            startupTimeout: StartupTimeout);

    /// <summary>Gets a value indicating whether this fixture launched the server it is using.</summary>
    public bool StartedByFixture => _controller.LastResult?.StartedByController ?? false;

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
        => await _controller.EnsureRunningAsync(CancellationToken.None).ConfigureAwait(false);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
        => await _controller.StopAsync(CancellationToken.None).ConfigureAwait(false);

    private static async Task<bool> ProbeAsync(CancellationToken cancellationToken)
    {
        using var client = new HttpClient { Timeout = ProbeTimeout };
        try
        {
            using var response = await client.GetAsync(new Uri(TagsEndpoint), cancellationToken).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return false;
        }
    }

    private static string? ResolveExecutable()
    {
        var candidates = new List<string>();

        var pathVariable = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var directory in pathVariable.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                candidates.Add(Path.Combine(directory.Trim(), OperatingSystem.IsWindows() ? "ollama.exe" : "ollama"));
            }
            catch (ArgumentException)
            {
                // Ignore malformed PATH entries rather than failing discovery.
            }
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        candidates.Add(Path.Combine(localAppData, "Programs", "Ollama", "ollama.exe"));
        // Matches the portable install location used by the InstallOllama Nuke target.
        candidates.Add(Path.Combine(localAppData, "McpServer", "test-tools", "ollama", "ollama.exe"));

        return candidates.FirstOrDefault(File.Exists);
    }

    private static IOllamaProcessHandle StartServerProcess(string executablePath)
    {
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = executablePath,
            Arguments = "serve",
            UseShellExecute = false,
            CreateNoWindow = true,
        }) ?? throw new InvalidOperationException($"Failed to start the Ollama server process from '{executablePath}'.");

        return new OllamaProcessHandle(process);
    }

    private sealed class OllamaProcessHandle : IOllamaProcessHandle
    {
        private readonly Process _process;

        internal OllamaProcessHandle(Process process) => _process = process;

        public bool HasExited => _process.HasExited;

        public void Kill() => _process.Kill(entireProcessTree: true);

        public void Dispose() => _process.Dispose();
    }
}
