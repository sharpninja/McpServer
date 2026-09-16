using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using McpServer.Client;

namespace McpServer.PluginIntegration.Tests;

/// <summary>
/// TEST-MCP-PLUGININT-001: disposable isolated MCP host for plugin Session Log theory rows.
/// Launches compiled <c>McpServer.Support.Mcp</c> on loopback. Never the developer 7147 database.
/// </summary>
public sealed class PluginIntegrationServerFixture : IAsyncDisposable
{
    private const int ReservedServicePort = 7147;
    private readonly List<string> _stdout = new();
    private readonly List<string> _stderr = new();
    private readonly object _logLock = new();
    private Process? _process;
    private string _rootPath = string.Empty;
    private string _repoRoot = string.Empty;
    private bool _started;

    /// <summary>Loopback port selected for this isolated host.</summary>
    public int Port { get; private set; }

    /// <summary>Temporary workspace directory created for this run.</summary>
    public string WorkspacePath { get; private set; } = string.Empty;

    /// <summary>Isolated database path, not the developer service database.</summary>
    public string DatabasePath { get; private set; } = string.Empty;

    /// <summary>Absolute marker file path written by the fixture.</summary>
    public string MarkerPath { get; private set; } = string.Empty;

    /// <summary>Full-access API key parsed from the generated marker.</summary>
    public string ApiKey { get; private set; } = string.Empty;

    /// <summary>Base URL of the isolated host.</summary>
    public Uri BaseUrl { get; private set; } = new("http://127.0.0.1");

    /// <summary>
    /// Starts an isolated compiled McpServer.Support.Mcp host on loopback.
    /// </summary>
    /// <param name="cancellationToken">Caller cancellation token.</param>
    /// <returns>A task that completes when the host is healthy and the marker exists.</returns>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_started)
        {
            throw new InvalidOperationException("PluginIntegrationServerFixture has already started.");
        }

        Port = AllocateFreePort();
        _rootPath = Path.Combine(Path.GetTempPath(), "mcp-pluginint-" + Guid.NewGuid().ToString("N"));
        WorkspacePath = Path.Combine(_rootPath, "workspace");
        var dataPath = Path.Combine(_rootPath, "data");
        DatabasePath = Path.Combine(dataPath, "mcp.db");
        MarkerPath = Path.Combine(WorkspacePath, "AGENTS-README-FIRST.yaml");
        BaseUrl = new Uri("http://127.0.0.1:" + Port.ToString(CultureInfo.InvariantCulture) + "/");

        Directory.CreateDirectory(WorkspacePath);
        Directory.CreateDirectory(dataPath);
        Directory.CreateDirectory(Path.Combine(WorkspacePath, "docs", "Project"));
        Directory.CreateDirectory(Path.Combine(WorkspacePath, "docs", "sessions"));
        Directory.CreateDirectory(Path.Combine(WorkspacePath, "docs", "external"));
        Directory.CreateDirectory(Path.Combine(WorkspacePath, "templates"));
        await File.WriteAllTextAsync(Path.Combine(WorkspacePath, "docs", "Project", "TODO.yaml"), "sections: []\n", cancellationToken).ConfigureAwait(false);
        await File.WriteAllTextAsync(Path.Combine(WorkspacePath, "docs", "Project", "Functional-Requirements.md"), "# Functional Requirements\n\n", cancellationToken).ConfigureAwait(false);
        await File.WriteAllTextAsync(Path.Combine(WorkspacePath, "docs", "Project", "Technical-Requirements.md"), "# Technical Requirements\n\n", cancellationToken).ConfigureAwait(false);
        await File.WriteAllTextAsync(Path.Combine(WorkspacePath, "docs", "Project", "Testing-Requirements.md"), "# Testing Requirements\n\n", cancellationToken).ConfigureAwait(false);
        await File.WriteAllTextAsync(Path.Combine(WorkspacePath, "docs", "Project", "TR-per-FR-Mapping.md"), "# TR per FR Mapping\n\n", cancellationToken).ConfigureAwait(false);
        await File.WriteAllTextAsync(Path.Combine(WorkspacePath, "docs", "Project", "Requirements-Matrix.md"), "# Requirements Matrix\n\n", cancellationToken).ConfigureAwait(false);
        await File.WriteAllTextAsync(Path.Combine(WorkspacePath, "docs", "unified-model-schema.json"), "{}\n", cancellationToken).ConfigureAwait(false);

        var repoRoot = FindRepositoryRoot();
        _repoRoot = repoRoot;
        var templateSource = Path.Combine(repoRoot, "templates", "prompt-templates.yaml");
        if (File.Exists(templateSource))
        {
            File.Copy(templateSource, Path.Combine(WorkspacePath, "templates", "prompt-templates.yaml"), overwrite: true);
        }

        await File.WriteAllTextAsync(
            Path.Combine(_rootPath, "appsettings.yaml"),
            BuildAppSettingsYaml(dataPath),
            cancellationToken).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();
        StartProcess(repoRoot);
        try
        {
            ApiKey = await WaitForMarkerApiKeyAsync(cancellationToken).ConfigureAwait(false);
            await WaitForHealthAsync(cancellationToken).ConfigureAwait(false);
            _started = true;
        }
        catch
        {
            await TryStopProcessAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// Stops the isolated host process while keeping the workspace and marker on disk.
    /// </summary>
    /// <returns>A task that completes when the process has been stopped.</returns>
    public async Task StopHostAsync()
    {
        await TryStopProcessAsync().ConfigureAwait(false);
        _started = false;
    }

    /// <summary>
    /// Restarts the isolated host against the existing workspace after <see cref="StopHostAsync"/>.
    /// </summary>
    /// <param name="cancellationToken">Caller cancellation token.</param>
    /// <returns>A task that completes when the host is healthy.</returns>
    public async Task RestartHostAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(_repoRoot) || string.IsNullOrWhiteSpace(_rootPath))
        {
            throw new InvalidOperationException("PluginIntegrationServerFixture.StartAsync has not completed.");
        }

        await TryStopProcessAsync().ConfigureAwait(false);
        await Task.Delay(300, cancellationToken).ConfigureAwait(false);
        StartProcess(_repoRoot);
        ApiKey = await WaitForMarkerApiKeyAsync(cancellationToken).ConfigureAwait(false);
        await WaitForHealthAsync(cancellationToken).ConfigureAwait(false);
        _started = true;
    }

    /// <summary>
    /// Creates an authenticated <see cref="McpServerClient"/> for this isolated host.
    /// </summary>
    /// <returns>A client bound to the generated marker API key and workspace path.</returns>
    public McpServerClient CreateTrustedClient()
    {
        if (!_started || string.IsNullOrWhiteSpace(ApiKey))
        {
            throw new InvalidOperationException("StartAsync has not completed.");
        }

        return McpServerClientFactory.Create(new McpServerClientOptions
        {
            BaseUrl = BaseUrl,
            ApiKey = ApiKey,
            WorkspacePath = WorkspacePath,
            Timeout = TimeSpan.FromSeconds(30),
        });
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await TryStopProcessAsync().ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(_rootPath) && Directory.Exists(_rootPath))
        {
            for (var attempt = 0; attempt < 8; attempt++)
            {
                try
                {
                    Directory.Delete(_rootPath, recursive: true);
                    break;
                }
                catch (IOException) when (attempt < 7)
                {
                    await Task.Delay(50).ConfigureAwait(false);
                }
                catch (UnauthorizedAccessException) when (attempt < 7)
                {
                    await Task.Delay(50).ConfigureAwait(false);
                }
            }
        }

        GC.SuppressFinalize(this);
    }

    private void StartProcess(string repoRoot)
    {
        var assemblyPath = ResolveSupportAssemblyPath(repoRoot);
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = _rootPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        startInfo.ArgumentList.Add(assemblyPath);
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
        startInfo.Environment["DOTNET_ENVIRONMENT"] = "Production";
        startInfo.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        startInfo.Environment["DOTNET_NOLOGO"] = "1";
        startInfo.Environment["PORT"] = Port.ToString(CultureInfo.InvariantCulture);
        startInfo.Environment["MCP_SQLITE_DATA_SOURCE"] = DatabasePath;
        startInfo.Environment["MCP_WORKSPACE_PATH"] = WorkspacePath;
        startInfo.Environment["MCPSERVER_WORKSPACE_PATH"] = WorkspacePath;

        _process = new Process { StartInfo = startInfo };
        _process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                lock (_logLock)
                {
                    _stdout.Add(e.Data);
                }
            }
        };
        _process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                lock (_logLock)
                {
                    _stderr.Add(e.Data);
                }
            }
        };

        if (!_process.Start())
        {
            throw new InvalidOperationException("Failed to start compiled McpServer.Support.Mcp.");
        }

        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
    }

    private async Task<string> WaitForMarkerApiKeyAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            ThrowIfProcessExited("marker");
            if (File.Exists(MarkerPath))
            {
                try
                {
                    var text = await File.ReadAllTextAsync(MarkerPath, cancellationToken).ConfigureAwait(false);
                    if (text.Contains("signature:", StringComparison.Ordinal)
                        && TryReadMarkerScalar(text, "apiKey", out var apiKey)
                        && !string.IsNullOrWhiteSpace(apiKey))
                    {
                        return apiKey;
                    }
                }
                catch (IOException)
                {
                }
            }

            await Task.Delay(150, cancellationToken).ConfigureAwait(false);
        }

        throw new OperationCanceledException(cancellationToken);
    }

    private async Task WaitForHealthAsync(CancellationToken cancellationToken)
    {
        using var http = new HttpClient { BaseAddress = BaseUrl, Timeout = TimeSpan.FromSeconds(2) };
        while (!cancellationToken.IsCancellationRequested)
        {
            ThrowIfProcessExited("health");
            try
            {
                var nonce = Guid.NewGuid().ToString("N");
                using var response = await http.GetAsync("/health?nonce=" + nonce, cancellationToken).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    if (body.Contains(nonce, StringComparison.Ordinal))
                    {
                        return;
                    }
                }
            }
            catch (HttpRequestException)
            {
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
            }

            await Task.Delay(150, cancellationToken).ConfigureAwait(false);
        }

        throw new OperationCanceledException(cancellationToken);
    }

    private void ThrowIfProcessExited(string stage)
    {
        if (_process is { HasExited: true })
        {
            throw new InvalidOperationException(
                "Compiled McpServer.Support.Mcp exited during " + stage
                + " with code " + _process.ExitCode + "." + Environment.NewLine + Diagnostics);
        }
    }

    private string Diagnostics
    {
        get
        {
            lock (_logLock)
            {
                var stdout = _stdout.Count == 0 ? "<none>" : string.Join(Environment.NewLine, _stdout);
                var stderr = _stderr.Count == 0 ? "<none>" : string.Join(Environment.NewLine, _stderr);
                return "STDOUT:" + Environment.NewLine + stdout + Environment.NewLine + "STDERR:" + Environment.NewLine + stderr;
            }
        }
    }

    private async Task TryStopProcessAsync()
    {
        if (_process is null)
        {
            return;
        }

        try
        {
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
                using var wait = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                try
                {
                    await _process.WaitForExitAsync(wait.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
            }
        }
        catch
        {
        }
        finally
        {
            _process.Dispose();
            _process = null;
        }
    }

    private string BuildAppSettingsYaml(string dataPath)
    {
        return
            "AllowedHosts: '*'" + Environment.NewLine
            + "DataFolder: " + YamlQuote(dataPath) + Environment.NewLine
            + "Serilog:" + Environment.NewLine
            + "  MinimumLevel:" + Environment.NewLine
            + "    Default: Warning" + Environment.NewLine
            + "Embedding:" + Environment.NewLine
            + "  AutoDownload: false" + Environment.NewLine
            + "Mcp:" + Environment.NewLine
            + "  Port: " + Port.ToString(CultureInfo.InvariantCulture) + Environment.NewLine
            + "  RepoRoot: " + YamlQuote(WorkspacePath) + Environment.NewLine
            + "  DataDirectory: " + YamlQuote(dataPath) + Environment.NewLine
            + "  DataSource: " + YamlQuote(DatabasePath) + Environment.NewLine
            + "  DatabaseProvider: sqlite" + Environment.NewLine
            + "  DatabaseMigrationsAssembly: McpServer.Storage.SqliteMigrations" + Environment.NewLine
            + "  Database:" + Environment.NewLine
            + "    Provider: sqlite" + Environment.NewLine
            + "    MigrationsAssembly: McpServer.Storage.SqliteMigrations" + Environment.NewLine
            + "    Sqlite:" + Environment.NewLine
            + "      DataSource: " + YamlQuote(DatabasePath) + Environment.NewLine
            + "  TodoFilePath: " + YamlQuote(Path.Combine(WorkspacePath, "docs", "Project", "TODO.yaml")) + Environment.NewLine
            + "  SessionsPath: " + YamlQuote(Path.Combine(WorkspacePath, "docs", "sessions")) + Environment.NewLine
            + "  ExternalDocsPath: " + YamlQuote(Path.Combine(WorkspacePath, "docs", "external")) + Environment.NewLine
            + "  UnifiedModelSchemaPath: " + YamlQuote(Path.Combine(WorkspacePath, "docs", "unified-model-schema.json")) + Environment.NewLine
            + "  Requirements:" + Environment.NewLine
            + "    FunctionalRequirementsPath: " + YamlQuote(Path.Combine(WorkspacePath, "docs", "Project", "Functional-Requirements.md")) + Environment.NewLine
            + "    TechnicalRequirementsPath: " + YamlQuote(Path.Combine(WorkspacePath, "docs", "Project", "Technical-Requirements.md")) + Environment.NewLine
            + "    TestingRequirementsPath: " + YamlQuote(Path.Combine(WorkspacePath, "docs", "Project", "Testing-Requirements.md")) + Environment.NewLine
            + "    MappingPath: " + YamlQuote(Path.Combine(WorkspacePath, "docs", "Project", "TR-per-FR-Mapping.md")) + Environment.NewLine
            + "    MatrixPath: " + YamlQuote(Path.Combine(WorkspacePath, "docs", "Project", "Requirements-Matrix.md")) + Environment.NewLine
            + "  TemplateStorage:" + Environment.NewLine
            + "    Provider: yaml" + Environment.NewLine
            + "    FilePath: " + YamlQuote(Path.Combine(WorkspacePath, "templates", "prompt-templates.yaml")) + Environment.NewLine
            + "  TodoStorage:" + Environment.NewLine
            + "    Provider: database" + Environment.NewLine
            + "    MigrateFromLegacySqlite: false" + Environment.NewLine
            + "  GraphRag:" + Environment.NewLine
            + "    Enabled: false" + Environment.NewLine
            + "    SeedCanonicalDocsOnStartup: false" + Environment.NewLine
            + "    IndexGlobalCorpusOnStartup: false" + Environment.NewLine
            + "    RootPath: " + YamlQuote(Path.Combine(dataPath, "graphrag")) + Environment.NewLine
            + "  Parseable:" + Environment.NewLine
            + "    Enabled: false" + Environment.NewLine
            + "  IdentityServer:" + Environment.NewLine
            + "    Enabled: false" + Environment.NewLine
            + "  Tunnel:" + Environment.NewLine
            + "    Port: " + Port.ToString(CultureInfo.InvariantCulture) + Environment.NewLine
            + "  Federation:" + Environment.NewLine
            + "    Role: Standalone" + Environment.NewLine
            + "  Workspaces:" + Environment.NewLine
            + "    - WorkspacePath: " + YamlQuote(WorkspacePath) + Environment.NewLine
            + "      Name: plugin-int" + Environment.NewLine
            + "      TodoPath: docs/Project/TODO.yaml" + Environment.NewLine
            + "      DataDirectory: " + YamlQuote(dataPath) + Environment.NewLine
            + "      IsPrimary: true" + Environment.NewLine
            + "      IsEnabled: true" + Environment.NewLine
            + "AgentPool:" + Environment.NewLine
            + "  Agents: []" + Environment.NewLine
            + "VoiceConversation:" + Environment.NewLine
            + "  Enabled: false" + Environment.NewLine
            + "Triage:" + Environment.NewLine
            + "  Enabled: false" + Environment.NewLine;
    }

    private static string YamlQuote(string value) => "'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";

    private static bool TryReadMarkerScalar(string text, string key, out string value)
    {
        value = string.Empty;
        using var reader = new StringReader(text);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith(key + ":", StringComparison.Ordinal))
            {
                continue;
            }

            var raw = trimmed[(key.Length + 1)..].Trim();
            if (raw.Length >= 2
                && ((raw[0] == '"' && raw[^1] == '"') || (raw[0] == '\'' && raw[^1] == '\'')))
            {
                raw = raw[1..^1];
            }

            value = raw;
            return !string.IsNullOrWhiteSpace(value);
        }

        return false;
    }

    private static int AllocateFreePort()
    {
        for (var attempt = 0; attempt < 16; attempt++)
        {
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            if (port != ReservedServicePort && port >= 1024)
            {
                return port;
            }
        }

        throw new InvalidOperationException("Could not allocate a free loopback port other than 7147.");
    }

    private static string ResolveSupportAssemblyPath(string repoRoot)
    {
        var copied = Path.Combine(AppContext.BaseDirectory, "McpServer.Support.Mcp.dll");
        if (File.Exists(copied))
        {
            return copied;
        }

        var outputDirectory = new DirectoryInfo(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var targetFramework = outputDirectory.Name;
        var configuration = outputDirectory.Parent?.Name ?? "Debug";
        var candidate = Path.Combine(repoRoot, "src", "McpServer.Support.Mcp", "bin", configuration, targetFramework, "McpServer.Support.Mcp.dll");
        if (File.Exists(candidate))
        {
            return candidate;
        }

        throw new FileNotFoundException(
            "Compiled McpServer.Support.Mcp.dll was not found. Build the PluginIntegration test project first.",
            candidate);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "McpServer.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("McpServer.sln not found.");
    }
}
