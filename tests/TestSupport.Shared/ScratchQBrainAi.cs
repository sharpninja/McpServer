using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.EntityFrameworkCore;
using QBrainAi.Support.Mcp.IntegrationTests;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Entities;
using QBrainAi.Tests.Shared;

namespace QBrainAi.TestSupport.TemporaryInstance;

/// <summary>
/// TR-MCP-NUKE-019 AC-6: one temporary QBrain.AI instance an integration case owns: a QBrainAi.Support.Mcp process on
/// a free high port, started from the built assembly in a test-owned root under the temporary folder, with its own
/// workspace, SQLite database, appsettings and generated marker, never the live server, its marker or its API key.
/// <see cref="StartAsync"/> returns once the instance wrote its marker and answers health; disposing it stops the
/// process tree and removes the root. Shared by the integration projects that start REPL children against it
/// (formerly private to RequirementScopeLayerReplIntegrationTests, behavior unchanged).
/// </summary>
internal sealed class ScratchQBrainAi : IAsyncDisposable
{
    private static readonly TimeSpan MarkerTimeout = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan ServerHealthTimeout = TimeSpan.FromSeconds(30);

    private readonly List<string> _stdout = new();
    private readonly List<string> _stderr = new();
    private readonly object _lock = new();
    private Process? _process;

    private ScratchQBrainAi(string rootPath, int port, string supportAssemblyPath, string replHostAssemblyPath)
    {
        RootPath = rootPath;
        Port = port;
        WorkspacePath = Path.Combine(rootPath, "workspace");
        DataPath = Path.Combine(rootPath, "data");
        DatabasePath = Path.Combine(DataPath, "mcp.db");
        MarkerPath = Path.Combine(WorkspacePath, MarkerFileService.MarkerFileName);
        SupportAssemblyPath = supportAssemblyPath;
        ReplHostAssemblyPath = replHostAssemblyPath;
    }

    /// <summary>Gets the test-owned root folder that holds everything the instance writes.</summary>
    public string RootPath { get; }

    /// <summary>Gets the loopback port the instance listens on.</summary>
    public int Port { get; }

    /// <summary>Gets the instance's workspace folder.</summary>
    public string WorkspacePath { get; }

    /// <summary>Gets the instance's data folder.</summary>
    public string DataPath { get; }

    /// <summary>Gets the instance's SQLite database path.</summary>
    public string DatabasePath { get; }

    /// <summary>Gets the marker the instance generates in its workspace.</summary>
    public string MarkerPath { get; }

    /// <summary>Gets the built QBrainAi.Support.Mcp assembly the instance runs.</summary>
    public string SupportAssemblyPath { get; }

    /// <summary>Gets the built QBrainAi.Repl.Host assembly REPL children run against the instance.</summary>
    public string ReplHostAssemblyPath { get; }

    /// <summary>Gets the base URL the instance's marker names, set once <see cref="StartAsync"/> returns.</summary>
    public Uri BaseUrl { get; private set; } = new("http://127.0.0.1/", UriKind.Absolute);

    /// <summary>
    /// Starts a new temporary instance and returns once it wrote its marker for its own port and answers health. On
    /// any failure the partial instance is disposed and the exception carries the process output.
    /// </summary>
    public static async Task<ScratchQBrainAi> StartAsync()
    {
        var repoRoot = TestRepositoryRoot.Resolve();
        var outputDirectory = new DirectoryInfo(AppContext.BaseDirectory.TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar));
        var targetFramework = outputDirectory.Name;
        var configuration = outputDirectory.Parent?.Name ?? "Debug";
        var supportAssembly = ResolveAssemblyPath(repoRoot, configuration, targetFramework, "QBrainAi.Support.Mcp");
        var replAssembly = ResolveAssemblyPath(repoRoot, configuration, targetFramework, "QBrainAi.Repl.Host");
        var root = Path.Combine(Path.GetTempPath(), $"mcp-scratch-instance-{Guid.NewGuid():N}");
        var server = new ScratchQBrainAi(root, AllocateHighPort(), supportAssembly, replAssembly);

        try
        {
            await server.InitializeAsync(repoRoot).ConfigureAwait(false);
            server.StartProcess();
            var marker = await server.WaitForMarkerAsync().ConfigureAwait(false);
            Assert.Equal(server.Port, marker.Port);
            Assert.False(string.IsNullOrWhiteSpace(marker.ApiKey), $"Generated marker did not contain an API key.{Environment.NewLine}{server.Diagnostics}");
            await server.WaitForHealthAsync(marker.BaseUrl).ConfigureAwait(false);
            server.BaseUrl = new Uri(marker.BaseUrl + "/", UriKind.Absolute);
            return server;
        }
        catch
        {
            await server.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_process is not null)
        {
            try
            {
                if (!_process.HasExited)
                {
                    _process.Kill(entireProcessTree: true);
                }

                await _process.WaitForExitAsync().ConfigureAwait(false);
                _process.Dispose();
            }
            catch
            {
                // Best-effort cleanup for failed integration-test starts.
            }
        }

        try
        {
            if (Directory.Exists(RootPath))
            {
                Directory.Delete(RootPath, recursive: true);
            }
        }
        catch
        {
            // Temp cleanup is best effort; diagnostics above preserve the failure cause.
        }
    }

    /// <summary>Gets the instance process output captured so far, for assertion messages.</summary>
    public string Diagnostics
    {
        get
        {
            lock (_lock)
            {
                var stdout = _stdout.Count == 0 ? "<none>" : string.Join(Environment.NewLine, _stdout);
                var stderr = _stderr.Count == 0 ? "<none>" : string.Join(Environment.NewLine, _stderr);
                return $"STDOUT:{Environment.NewLine}{stdout}{Environment.NewLine}STDERR:{Environment.NewLine}{stderr}";
            }
        }
    }

    private async Task InitializeAsync(string repoRoot)
    {
        Directory.CreateDirectory(WorkspacePath);
        Directory.CreateDirectory(DataPath);
        Directory.CreateDirectory(Path.Combine(WorkspacePath, "docs", "Project"));
        Directory.CreateDirectory(Path.Combine(WorkspacePath, "docs", "sessions"));
        Directory.CreateDirectory(Path.Combine(WorkspacePath, "docs", "external"));
        Directory.CreateDirectory(Path.Combine(WorkspacePath, "templates"));
        Directory.CreateDirectory(Path.Combine(RootPath, "logs"));

        await File.WriteAllTextAsync(
            Path.Combine(WorkspacePath, "docs", "Project", "TODO.yaml"),
            "sections: []\n",
            Encoding.UTF8).ConfigureAwait(false);
        await File.WriteAllTextAsync(
            Path.Combine(WorkspacePath, "docs", "Project", "Functional-Requirements.md"),
            "# Functional Requirements\n\n",
            Encoding.UTF8).ConfigureAwait(false);
        await File.WriteAllTextAsync(
            Path.Combine(WorkspacePath, "docs", "Project", "Technical-Requirements.md"),
            "# Technical Requirements\n\n",
            Encoding.UTF8).ConfigureAwait(false);
        await File.WriteAllTextAsync(
            Path.Combine(WorkspacePath, "docs", "Project", "Testing-Requirements.md"),
            "# Testing Requirements\n\n",
            Encoding.UTF8).ConfigureAwait(false);
        await File.WriteAllTextAsync(
            Path.Combine(WorkspacePath, "docs", "Project", "TR-per-FR-Mapping.md"),
            "# TR per FR Mapping\n\n",
            Encoding.UTF8).ConfigureAwait(false);
        await File.WriteAllTextAsync(
            Path.Combine(WorkspacePath, "docs", "Project", "Requirements-Matrix.md"),
            "# Requirements Matrix\n\n",
            Encoding.UTF8).ConfigureAwait(false);

        File.Copy(
            Path.Combine(repoRoot, "templates", "prompt-templates.yaml"),
            Path.Combine(WorkspacePath, "templates", "prompt-templates.yaml"));

        await File.WriteAllTextAsync(Path.Combine(WorkspacePath, "docs", "unified-model-schema.json"), "{}\n", Encoding.UTF8)
            .ConfigureAwait(false);
        await File.WriteAllTextAsync(Path.Combine(RootPath, "appsettings.yaml"), BuildAppSettingsYaml(), Encoding.UTF8)
            .ConfigureAwait(false);

        await ScratchSqliteSchema.ApplyAndVerifyAsync(DatabasePath).ConfigureAwait(false);
        await StageWorkspaceDatabaseAsync().ConfigureAwait(false);
    }

    private async Task StageWorkspaceDatabaseAsync()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseSqlite(
                $"Data Source={DatabasePath}",
                sqlite => sqlite.MigrationsAssembly("QBrainAi.Storage.SqliteMigrations"))
            .Options;

        await using var db = new McpDbContext(options);
        var now = DateTimeOffset.UtcNow;
        db.Workspaces.Add(new WorkspaceEntity
        {
            WorkspaceId = NormalizePath(WorkspacePath),
            WorkspacePath = NormalizePath(WorkspacePath),
            Name = "Temporary integration instance",
            TodoPath = Path.Combine("docs", "Project", "TODO.yaml"),
            DataDirectory = DataPath,
            IsPrimary = true,
            IsEnabled = true,
            CurrentRequirementLayerKey = "layer-1",
            DateTimeCreated = now,
            DateTimeModified = now,
        });
        await db.SaveChangesAsync().ConfigureAwait(false);
    }

    private void StartProcess()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = RootPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        startInfo.ArgumentList.Add(SupportAssemblyPath);
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
                lock (_lock)
                {
                    _stdout.Add(e.Data);
                }
            }
        };
        _process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                lock (_lock)
                {
                    _stderr.Add(e.Data);
                }
            }
        };

        if (!_process.Start())
        {
            throw new InvalidOperationException("Failed to start QBrain.AI process.");
        }

        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
    }

    private async Task<MarkerSettings> WaitForMarkerAsync()
    {
        using var timeout = new CancellationTokenSource(MarkerTimeout);
        using var watcher = new FileSystemWatcher(WorkspacePath, MarkerFileService.MarkerFileName)
        {
            NotifyFilter = NotifyFilters.CreationTime
                | NotifyFilters.FileName
                | NotifyFilters.LastWrite
                | NotifyFilters.Size,
        };
        var signal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        FileSystemEventHandler onChanged = (_, _) => signal.TrySetResult();
        RenamedEventHandler onRenamed = (_, _) => signal.TrySetResult();
        watcher.Created += onChanged;
        watcher.Changed += onChanged;
        watcher.Renamed += onRenamed;
        watcher.EnableRaisingEvents = true;

        while (!timeout.IsCancellationRequested)
        {
            if (TryReadMarker(out var marker))
            {
                return marker!;
            }

            if (_process is { HasExited: true })
            {
                throw new InvalidOperationException(
                    $"QBrain.AI exited before generating '{MarkerPath}' with code {_process.ExitCode}.{Environment.NewLine}{Diagnostics}");
            }

            var delay = Task.Delay(TimeSpan.FromMilliseconds(250), timeout.Token);
            var completed = await Task.WhenAny(signal.Task, delay).ConfigureAwait(false);
            if (completed == signal.Task)
            {
                signal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            }
        }

        throw new TimeoutException(
            $"Timed out after {MarkerTimeout} waiting for generated marker '{MarkerPath}'.{Environment.NewLine}{Diagnostics}");
    }

    private bool TryReadMarker(out MarkerSettings? marker)
    {
        marker = null;
        if (!File.Exists(MarkerPath))
        {
            return false;
        }

        try
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var rawLine in File.ReadLines(MarkerPath))
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                var separator = line.IndexOf(':', StringComparison.Ordinal);
                if (separator <= 0)
                {
                    continue;
                }

                var key = line[..separator].Trim();
                if (key is not ("port" or "baseUrl" or "apiKey" or "workspacePath"))
                {
                    continue;
                }

                values[key] = Unquote(line[(separator + 1)..].Trim());
            }

            if (!values.TryGetValue("port", out var rawPort)
                || !int.TryParse(rawPort, NumberStyles.Integer, CultureInfo.InvariantCulture, out var port)
                || !values.TryGetValue("baseUrl", out var baseUrl)
                || string.IsNullOrWhiteSpace(baseUrl)
                || !values.TryGetValue("apiKey", out var apiKey)
                || string.IsNullOrWhiteSpace(apiKey))
            {
                return false;
            }

            marker = new MarkerSettings(port, baseUrl.TrimEnd('/'), apiKey);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private async Task WaitForHealthAsync(string baseUrl)
    {
        using var http = new HttpClient { BaseAddress = new Uri(baseUrl, UriKind.Absolute) };
        using var timeout = new CancellationTokenSource(ServerHealthTimeout);
        while (!timeout.IsCancellationRequested)
        {
            if (_process is { HasExited: true })
            {
                throw new InvalidOperationException(
                    $"QBrain.AI exited before health was available with code {_process.ExitCode}.{Environment.NewLine}{Diagnostics}");
            }

            try
            {
                using var response = await http.GetAsync("/health", timeout.Token).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
            }
            catch (TaskCanceledException) when (!timeout.IsCancellationRequested)
            {
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250), timeout.Token).ConfigureAwait(false);
        }

        throw new TimeoutException(
            $"Timed out after {ServerHealthTimeout} waiting for QBrain.AI health at '{baseUrl}'.{Environment.NewLine}{Diagnostics}");
    }

    private string BuildAppSettingsYaml()
    {
        return $"""
            AllowedHosts: '*'
            DataFolder: {YamlQuote(DataPath)}
            Serilog:
              MinimumLevel:
                Default: Warning
            Mcp:
              Port: {Port.ToString(CultureInfo.InvariantCulture)}
              RepoRoot: {YamlQuote(WorkspacePath)}
              DataDirectory: {YamlQuote(DataPath)}
              DataSource: {YamlQuote(DatabasePath)}
              DatabaseProvider: sqlite
              DatabaseMigrationsAssembly: QBrainAi.Storage.SqliteMigrations
              Database:
                Provider: sqlite
                MigrationsAssembly: QBrainAi.Storage.SqliteMigrations
                Sqlite:
                  DataSource: {YamlQuote(DatabasePath)}
              TodoFilePath: {YamlQuote(Path.Combine(WorkspacePath, "docs", "Project", "TODO.yaml"))}
              SessionsPath: {YamlQuote(Path.Combine(WorkspacePath, "docs", "sessions"))}
              ExternalDocsPath: {YamlQuote(Path.Combine(WorkspacePath, "docs", "external"))}
              UnifiedModelSchemaPath: {YamlQuote(Path.Combine(WorkspacePath, "docs", "unified-model-schema.json"))}
              Requirements:
                FunctionalRequirementsPath: {YamlQuote(Path.Combine(WorkspacePath, "docs", "Project", "Functional-Requirements.md"))}
                TechnicalRequirementsPath: {YamlQuote(Path.Combine(WorkspacePath, "docs", "Project", "Technical-Requirements.md"))}
                TestingRequirementsPath: {YamlQuote(Path.Combine(WorkspacePath, "docs", "Project", "Testing-Requirements.md"))}
                MappingPath: {YamlQuote(Path.Combine(WorkspacePath, "docs", "Project", "TR-per-FR-Mapping.md"))}
                MatrixPath: {YamlQuote(Path.Combine(WorkspacePath, "docs", "Project", "Requirements-Matrix.md"))}
              TemplateStorage:
                Provider: yaml
                FilePath: {YamlQuote(Path.Combine(WorkspacePath, "templates", "prompt-templates.yaml"))}
              TodoStorage:
                Provider: database
                MigrateFromLegacySqlite: false
              GraphRag:
                Enabled: false
                RootPath: {YamlQuote(Path.Combine(DataPath, "graphrag"))}
              Parseable:
                Enabled: false
              IdentityServer:
                Enabled: false
              Tunnel:
                Port: {Port.ToString(CultureInfo.InvariantCulture)}
              Federation:
                Role: Standalone
            AgentPool:
              Agents: []
            VoiceConversation:
              Enabled: false
            Triage:
              Enabled: false

            """;
    }

    private static string YamlQuote(string value) => $"'{value.Replace("'", "''", StringComparison.Ordinal)}'";

    private static string Unquote(string value)
    {
        if (value.Length >= 2
            && ((value[0] == '"' && value[^1] == '"')
                || (value[0] == '\'' && value[^1] == '\'')))
        {
            return value[1..^1];
        }

        return value;
    }

    private static int AllocateHighPort()
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            var port = Random.Shared.Next(20_000, 60_000);
            try
            {
                using var listener = new TcpListener(IPAddress.Loopback, port);
                listener.Start();
                return port;
            }
            catch (SocketException)
            {
            }
        }

        using var fallback = new TcpListener(IPAddress.Loopback, 0);
        fallback.Start();
        return ((IPEndPoint)fallback.LocalEndpoint).Port;
    }

    private static string ResolveAssemblyPath(string repoRoot, string configuration, string targetFramework, string projectName)
    {
        var candidate = Path.Combine(
            repoRoot,
            "src",
            projectName,
            "bin",
            configuration,
            targetFramework,
            $"{projectName}.dll");
        if (File.Exists(candidate))
        {
            return candidate;
        }

        var copiedCandidate = Path.Combine(AppContext.BaseDirectory, $"{projectName}.dll");
        if (File.Exists(copiedCandidate))
        {
            return copiedCandidate;
        }

        throw new FileNotFoundException(
            $"Could not find built {projectName} assembly. Build the test project before running REPL integration tests. Checked '{copiedCandidate}' and '{candidate}'.",
            candidate);
    }

    private static string NormalizePath(string path)
        => Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));

    private sealed record MarkerSettings(int Port, string BaseUrl, string ApiKey);
}
