using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.IntegrationTests;

/// <summary>
/// TEST-MCP-REQSCOPE-REPL-001: REPL-driven integration coverage for requirement scope layers.
/// </summary>
[Trait("Category", "Integration")]
public sealed class RequirementScopeLayerReplIntegrationTests
{
    private static readonly TimeSpan MarkerTimeout = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan ServerHealthTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan ReplRequestTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// TEST-MCP-REQSCOPE-REPL-001: creates layers, starts a requirement in a later layer,
    /// sunsets a layer and a requirement, then queries current effective requirements before
    /// and after the new layer through a real marker-backed REPL process.
    /// </summary>
    [Fact]
    public async Task RequirementScopeLayer_ReplWorkflow_ExercisesCurrentEffectiveRequirementsBeforeAndAfterLayer()
    {
        await using var scratch = await ScratchMcpServer.StartAsync().ConfigureAwait(true);
        await using var repl = await ReplProcess.StartAsync(
            scratch.ReplHostAssemblyPath,
            scratch.WorkspacePath,
            scratch.MarkerPath).ConfigureAwait(true);

        await RunReplAsync(repl, "reqscope-int-create-layer-2", """
            type: request
            payload:
              requestId: reqscope-int-create-layer-2
              method: workflow.requirements.createLayer
              params:
                key: layer-2
                order: 2
                name: Layer 2

            """).ConfigureAwait(true);
        await RunReplAsync(repl, "reqscope-int-create-layer-3", """
            type: request
            payload:
              requestId: reqscope-int-create-layer-3
              method: workflow.requirements.createLayer
              params:
                key: layer-3
                order: 3
                name: Layer 3

            """).ConfigureAwait(true);
        await RunReplAsync(repl, "reqscope-int-create-layer-4", """
            type: request
            payload:
              requestId: reqscope-int-create-layer-4
              method: workflow.requirements.createLayer
              params:
                key: layer-4
                order: 4
                name: Layer 4

            """).ConfigureAwait(true);
        await RunReplAsync(repl, "reqscope-int-create-new-fr", """
            type: request
            payload:
              requestId: reqscope-int-create-new-fr
              method: workflow.requirements.createFr
              params:
                id: FR-MCP-REQSCOPE-951
                title: New layer requirement
                description: Applies from layer 2 until the layer 2 sunset.
                priority: high
                area: MCP
                scopeStartLayerKey: layer-2
                acceptanceCriteria:
                  - text: TEST-MCP-REQSCOPE-REPL-001 verifies this requirement becomes visible at layer 2.

            """).ConfigureAwait(true);
        await RunReplAsync(repl, "reqscope-int-create-old-fr", """
            type: request
            payload:
              requestId: reqscope-int-create-old-fr
              method: workflow.requirements.createFr
              params:
                id: FR-MCP-REQSCOPE-950
                title: Sunset before layer
                description: Starts before layer 2 and is sunset by update.
                priority: high
                area: MCP
                scopeStartLayerKey: layer-1
                acceptanceCriteria:
                  - text: TEST-MCP-REQSCOPE-REPL-001 verifies this requirement is hidden after layer 1.

            """).ConfigureAwait(true);
        await RunReplAsync(repl, "reqscope-int-sunset-fr-before-layer", """
            type: request
            payload:
              requestId: reqscope-int-sunset-fr-before-layer
              method: workflow.requirements.updateFr
              params:
                id: FR-MCP-REQSCOPE-950
                scopeEndLayerKey: layer-1

            """).ConfigureAwait(true);
        await RunReplAsync(repl, "reqscope-int-sunset-layer", """
            type: request
            payload:
              requestId: reqscope-int-sunset-layer
              method: workflow.requirements.updateLayer
              params:
                key: layer-2
                scopeEndLayerKey: layer-3

            """).ConfigureAwait(true);

        await SetCurrentLayerAsync(repl, "layer-1").ConfigureAwait(true);
        var beforeOutput = await QueryCurrentEffectiveAsync(repl, "reqscope-int-effective-before").ConfigureAwait(true);

        await SetCurrentLayerAsync(repl, "layer-2").ConfigureAwait(true);
        var afterOutput = await QueryCurrentEffectiveAsync(repl, "reqscope-int-effective-after").ConfigureAwait(true);

        await SetCurrentLayerAsync(repl, "layer-3").ConfigureAwait(true);
        var sunsetInclusiveOutput = await QueryCurrentEffectiveAsync(
            repl,
            "reqscope-int-effective-sunset-inclusive").ConfigureAwait(true);

        await SetCurrentLayerAsync(repl, "layer-4").ConfigureAwait(true);
        var afterSunsetOutput = await QueryCurrentEffectiveAsync(repl, "reqscope-int-effective-after-sunset").ConfigureAwait(true);

        Assert.True(File.Exists(scratch.MarkerPath), $"Expected generated marker at '{scratch.MarkerPath}'.");
        Assert.Contains("FR-MCP-REQSCOPE-950", beforeOutput, StringComparison.Ordinal);
        Assert.DoesNotContain("FR-MCP-REQSCOPE-951", beforeOutput, StringComparison.Ordinal);
        Assert.Contains("FR-MCP-REQSCOPE-951", afterOutput, StringComparison.Ordinal);
        Assert.DoesNotContain("FR-MCP-REQSCOPE-950", afterOutput, StringComparison.Ordinal);
        Assert.Contains("FR-MCP-REQSCOPE-951", sunsetInclusiveOutput, StringComparison.Ordinal);
        Assert.DoesNotContain("FR-MCP-REQSCOPE-951", afterSunsetOutput, StringComparison.Ordinal);
    }

    private static Task<string> QueryCurrentEffectiveAsync(ReplProcess repl, string requestId)
        => RunReplAsync(repl, requestId, $$"""
            type: request
            payload:
              requestId: {{requestId}}
              method: workflow.requirements.effective
              params: {}

            """);

    private static Task<string> SetCurrentLayerAsync(ReplProcess repl, string layerKey)
        => RunReplAsync(repl, $"reqscope-int-set-current-{layerKey}", $$"""
            type: request
            payload:
              requestId: reqscope-int-set-current-{{layerKey}}
              method: client.workspace.SetCurrentRequirementLayerAsync
              params:
                request:
                  layerKey: {{layerKey}}

            """);

    private static async Task<string> RunReplAsync(ReplProcess repl, string requestId, string yaml)
    {
        await repl.WriteLineAsync(yaml).ConfigureAwait(true);
        var received = await repl.WaitForResponseAsync(requestId, ReplRequestTimeout).ConfigureAwait(true);
        Assert.True(received, $"Timed out waiting for REPL response '{requestId}'.{Environment.NewLine}{repl.Diagnostics}");

        var document = repl.GetResponseDocument(requestId);
        Assert.False(string.IsNullOrWhiteSpace(document), $"Missing REPL document for '{requestId}'.{Environment.NewLine}{repl.Diagnostics}");
        Assert.True(
            document.Contains("type: result", StringComparison.Ordinal),
            $"Expected a result envelope for '{requestId}' but received:{Environment.NewLine}{document}");
        return document;
    }

    private sealed class ScratchMcpServer : IAsyncDisposable
    {
        private readonly List<string> _stdout = new();
        private readonly List<string> _stderr = new();
        private readonly object _lock = new();
        private Process? _process;

        private ScratchMcpServer(string rootPath, int port, string supportAssemblyPath, string replHostAssemblyPath)
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

        public string RootPath { get; }

        public int Port { get; }

        public string WorkspacePath { get; }

        public string DataPath { get; }

        public string DatabasePath { get; }

        public string MarkerPath { get; }

        public string SupportAssemblyPath { get; }

        public string ReplHostAssemblyPath { get; }

        public static async Task<ScratchMcpServer> StartAsync()
        {
            var repoRoot = FindRepoRoot(AppContext.BaseDirectory);
            var outputDirectory = new DirectoryInfo(AppContext.BaseDirectory.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar));
            var targetFramework = outputDirectory.Name;
            var configuration = outputDirectory.Parent?.Name ?? "Debug";
            var supportAssembly = ResolveAssemblyPath(repoRoot, configuration, targetFramework, "McpServer.Support.Mcp");
            var replAssembly = ResolveAssemblyPath(repoRoot, configuration, targetFramework, "McpServer.Repl.Host");
            var root = Path.Combine(Path.GetTempPath(), $"mcp-reqscope-repl-{Guid.NewGuid():N}");
            var server = new ScratchMcpServer(root, AllocateHighPort(), supportAssembly, replAssembly);

            try
            {
                await server.InitializeAsync(repoRoot).ConfigureAwait(false);
                server.StartProcess();
                var marker = await server.WaitForMarkerAsync().ConfigureAwait(false);
                Assert.Equal(server.Port, marker.Port);
                Assert.False(string.IsNullOrWhiteSpace(marker.ApiKey), $"Generated marker did not contain an API key.{Environment.NewLine}{server.Diagnostics}");
                await server.WaitForHealthAsync(marker.BaseUrl).ConfigureAwait(false);
                return server;
            }
            catch
            {
                await server.DisposeAsync().ConfigureAwait(false);
                throw;
            }
        }

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

        private string Diagnostics
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
                    sqlite => sqlite.MigrationsAssembly("McpServer.Storage.SqliteMigrations"))
                .Options;

            await using var db = new McpDbContext(options);
            var now = DateTimeOffset.UtcNow;
            db.Workspaces.Add(new WorkspaceEntity
            {
                WorkspaceId = NormalizePath(WorkspacePath),
                WorkspacePath = NormalizePath(WorkspacePath),
                Name = "Requirement Scope Integration",
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
                throw new InvalidOperationException("Failed to start MCP Server process.");
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
                        $"MCP Server exited before generating '{MarkerPath}' with code {_process.ExitCode}.{Environment.NewLine}{Diagnostics}");
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
                        $"MCP Server exited before health was available with code {_process.ExitCode}.{Environment.NewLine}{Diagnostics}");
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
                $"Timed out after {ServerHealthTimeout} waiting for MCP Server health at '{baseUrl}'.{Environment.NewLine}{Diagnostics}");
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
                  DatabaseMigrationsAssembly: McpServer.Storage.SqliteMigrations
                  Database:
                    Provider: sqlite
                    MigrationsAssembly: McpServer.Storage.SqliteMigrations
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
    }

    private sealed class ReplProcess : IAsyncDisposable
    {
        private readonly List<string> _stdoutDocuments = new();
        private readonly List<string> _stderrLines = new();
        private readonly List<string> _currentDocument = new();
        private readonly object _lock = new();
        private int? _stdoutBlockScalarIndent;
        private Process? _process;

        private ReplProcess(Process process)
        {
            _process = process;
        }

        public string Diagnostics
        {
            get
            {
                lock (_lock)
                {
                    var pending = _currentDocument.Count == 0
                        ? string.Empty
                        : string.Join(Environment.NewLine, _currentDocument);
                    var stdout = _stdoutDocuments.Count == 0
                        ? "<none>"
                        : string.Join($"{Environment.NewLine}--- stdout document ---{Environment.NewLine}", _stdoutDocuments);
                    if (!string.IsNullOrWhiteSpace(pending))
                    {
                        stdout += $"{Environment.NewLine}--- pending stdout document ---{Environment.NewLine}{pending}";
                    }

                    var stderr = _stderrLines.Count == 0 ? "<none>" : string.Join(Environment.NewLine, _stderrLines);
                    return $"STDOUT:{Environment.NewLine}{stdout}{Environment.NewLine}STDERR:{Environment.NewLine}{stderr}";
                }
            }
        }

        public static async Task<ReplProcess> StartAsync(string assemblyPath, string workspacePath, string markerPath)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                WorkingDirectory = workspacePath,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardInputEncoding = Encoding.UTF8,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            };
            startInfo.ArgumentList.Add(assemblyPath);
            startInfo.ArgumentList.Add("--agent-stdio");
            startInfo.ArgumentList.Add("--workspace-path");
            startInfo.ArgumentList.Add(workspacePath);
            startInfo.ArgumentList.Add("--marker-file");
            startInfo.ArgumentList.Add(markerPath);
            startInfo.Environment["MCP_WORKSPACE_PATH"] = workspacePath;
            startInfo.Environment["MCPSERVER_WORKSPACE_PATH"] = workspacePath;
            startInfo.Environment["MCPSERVER_REPL_COMMAND_TIMEOUT_SECONDS"] = "10";
            startInfo.Environment["MCPSERVER_REPL_STREAM_COMMAND_TIMEOUT_SECONDS"] = "8";
            startInfo.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
            startInfo.Environment["DOTNET_NOLOGO"] = "1";

            var process = new Process { StartInfo = startInfo };
            var repl = new ReplProcess(process);
            process.OutputDataReceived += repl.OnStdoutDataReceived;
            process.ErrorDataReceived += repl.OnStderrDataReceived;
            if (!process.Start())
            {
                throw new InvalidOperationException("Failed to start mcpserver-repl host process.");
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await Task.Delay(TimeSpan.FromMilliseconds(250)).ConfigureAwait(false);
            if (process.HasExited)
            {
                throw new InvalidOperationException(
                    $"mcpserver-repl host exited during startup with code {process.ExitCode}.{Environment.NewLine}{repl.Diagnostics}");
            }

            return repl;
        }

        public async Task WriteLineAsync(string yaml)
        {
            if (_process is not { HasExited: false })
            {
                throw new InvalidOperationException($"REPL process is not running.{Environment.NewLine}{Diagnostics}");
            }

            await _process.StandardInput.WriteLineAsync(yaml.AsMemory()).ConfigureAwait(false);
            await _process.StandardInput.WriteLineAsync(string.Empty.AsMemory()).ConfigureAwait(false);
            await _process.StandardInput.FlushAsync().ConfigureAwait(false);
        }

        public async Task<bool> WaitForResponseAsync(string requestId, TimeSpan timeout)
        {
            var deadline = DateTimeOffset.UtcNow + timeout;
            while (DateTimeOffset.UtcNow < deadline)
            {
                lock (_lock)
                {
                    if (_stdoutDocuments.Any(document => IsFinalResponseForRequest(document, requestId)))
                    {
                        return true;
                    }
                }

                if (_process is { HasExited: true })
                {
                    throw new InvalidOperationException(
                        $"REPL process exited before stdout contained a final response for request '{requestId}'.{Environment.NewLine}{Diagnostics}");
                }

                await Task.Delay(TimeSpan.FromMilliseconds(50)).ConfigureAwait(false);
            }

            return false;
        }

        public string? GetResponseDocument(string requestId)
        {
            lock (_lock)
            {
                return _stdoutDocuments.FirstOrDefault(document => IsFinalResponseForRequest(document, requestId));
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_process is null)
            {
                return;
            }

            try
            {
                if (!_process.HasExited)
                {
                    _process.StandardInput.Close();
                    if (!_process.WaitForExit((int)TimeSpan.FromSeconds(2).TotalMilliseconds))
                    {
                        _process.Kill(entireProcessTree: true);
                    }
                }

                await _process.WaitForExitAsync().ConfigureAwait(false);
                _process.Dispose();
            }
            catch
            {
                // Best-effort cleanup for failed integration-test starts.
            }
        }

        private void OnStdoutDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data is null)
            {
                return;
            }

            lock (_lock)
            {
                var line = e.Data.TrimStart('\uFEFF');
                if (IsTopLevelDocumentStart(line) && _currentDocument.Count > 0)
                {
                    FlushStdoutDocument();
                }

                if (string.IsNullOrWhiteSpace(e.Data))
                {
                    if (_stdoutBlockScalarIndent is null)
                    {
                        FlushStdoutDocument();
                    }
                    else
                    {
                        _currentDocument.Add(line);
                    }

                    return;
                }

                if (_stdoutBlockScalarIndent is int blockIndent
                    && CountLeadingSpaces(line) <= blockIndent)
                {
                    _stdoutBlockScalarIndent = null;
                }

                _currentDocument.Add(line);
                if (StartsBlockScalar(line))
                {
                    _stdoutBlockScalarIndent = CountLeadingSpaces(line);
                }
            }
        }

        private void OnStderrDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data is null)
            {
                return;
            }

            lock (_lock)
            {
                _stderrLines.Add(e.Data);
            }
        }

        private void FlushStdoutDocument()
        {
            if (_currentDocument.Count == 0)
            {
                return;
            }

            _stdoutDocuments.Add(string.Join(Environment.NewLine, _currentDocument));
            _currentDocument.Clear();
            _stdoutBlockScalarIndent = null;
        }

        private static bool IsTopLevelDocumentStart(string line)
            => line.StartsWith("type:", StringComparison.Ordinal);

        private static bool IsFinalResponseForRequest(string document, string requestId)
        {
            return (document.Contains("type: result", StringComparison.Ordinal)
                    || document.Contains("type: error", StringComparison.Ordinal))
                && (document.Contains($"requestId: {requestId}", StringComparison.Ordinal)
                    || document.Contains($"requestId: \"{requestId}\"", StringComparison.Ordinal)
                    || document.Contains($"requestId: '{requestId}'", StringComparison.Ordinal));
        }

        private static int CountLeadingSpaces(string line)
        {
            var count = 0;
            while (count < line.Length && line[count] == ' ')
            {
                count++;
            }

            return count;
        }

        private static bool StartsBlockScalar(string line)
        {
            var trimmed = line.TrimEnd();
            var colonIndex = trimmed.LastIndexOf(':');
            if (colonIndex < 0 || colonIndex == trimmed.Length - 1)
            {
                return false;
            }

            var value = trimmed[(colonIndex + 1)..].TrimStart();
            return value.StartsWith('|') || value.StartsWith('>');
        }
    }

    private sealed record MarkerSettings(int Port, string BaseUrl, string ApiKey);

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

    private static string FindRepoRoot(string startPath)
    {
        var directory = new DirectoryInfo(startPath);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "McpServer.sln"))
                || File.Exists(Path.Combine(directory.FullName, "McpServer.slnx"))
                || Directory.Exists(Path.Combine(directory.FullName, ".git")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException($"Could not resolve repository root from '{startPath}'.");
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
}
