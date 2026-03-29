using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace McpServer.Support.Mcp.IntegrationTests.Controllers;

/// <summary>
/// Integration tests verifying that marker files are regenerated when
/// the global prompt template or a workspace prompt template changes.
/// Uses <see cref="FileSystemWatcher"/> latches to synchronize on disk writes
/// rather than polling, ensuring deterministic test flow.
/// </summary>
public sealed class MarkerRegenerationIntegrationTests : IAsyncLifetime
{
    private readonly string _tempRoot;
    private readonly string _workspacePath;
    private readonly string _appsettingsPath;
    private readonly string _markerPath;
    private MarkerRegenerationFactory _factory = null!;
    private HttpClient _client = null!;
    private FileSystemWatcher _settingsWatcher = null!;
    private FileSystemWatcher _markerWatcher = null!;
    private int _temporaryPort;

    public MarkerRegenerationIntegrationTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"mcp_marker_test_{Guid.NewGuid():N}");
        _workspacePath = Path.Combine(_tempRoot, "test-workspace");
        // appsettings.json must live in the workspace folder because Program.cs sets
        // ContentRootPath = primary workspace path, and controllers resolve appsettings
        // relative to ContentRootPath.
        _appsettingsPath = Path.Combine(_workspacePath, "appsettings.json");
        _markerPath = Path.Combine(_workspacePath, MarkerFileService.MarkerFileName);
    }

    /// <inheritdoc />
    public ValueTask InitializeAsync()
    {
        Directory.CreateDirectory(_tempRoot);
        Directory.CreateDirectory(_workspacePath);

        // Seed appsettings.json with a primary workspace. The factory injects a temporary non-standard
        // MCP port so generated marker content never falls back to the default service port.
        var settings = new
        {
            Mcp = new
            {
                DataSource = ":memory:",
                RepoRoot = _workspacePath,
                TodoFilePath = "docs/todo.yaml",
                TodoStorage = new
                {
                    Provider = "sqlite",
                    SqliteDataSource = "mcp.db",
                },
                Workspaces = new[]
                {
                    new
                    {
                        WorkspacePath = _workspacePath,
                        Name = "marker-test",
                        TodoPath = "docs/todo.yaml",
                        IsPrimary = true,
                        IsEnabled = true,
                    }
                }
            }
        };
        File.WriteAllText(_appsettingsPath, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));

        // FileSystemWatcher on appsettings.json — fires when config writes complete.
        _settingsWatcher = new FileSystemWatcher(_workspacePath, "appsettings.json")
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime | NotifyFilters.FileName,
            EnableRaisingEvents = true,
        };

        // FileSystemWatcher on the marker file — fires when marker is written/rewritten.
        _markerWatcher = new FileSystemWatcher(_workspacePath, MarkerFileService.MarkerFileName)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime,
            EnableRaisingEvents = true,
        };

        _factory = new MarkerRegenerationFactory(_workspacePath);
        _temporaryPort = _factory.TemporaryPort;
        _client = _factory.CreateClient();
        TestAuthHelper.AddAuthHeader(_client, _factory.Services);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _settingsWatcher.Dispose();
        _markerWatcher.Dispose();
        _client.Dispose();
        await _factory.DisposeAsync().ConfigureAwait(false);
        try { Directory.Delete(_tempRoot, recursive: true); } catch { /* best-effort cleanup */ }
    }

    [Fact]
    public async Task GlobalPromptUpdate_RegeneratesMarkerFile()
    {
        var key = EncodeKey(Path.GetFullPath(_workspacePath));
        await EnsureWorkspaceSeededAsync().ConfigureAwait(true);
        await StartWorkspaceAndWaitForMarkerAsync(key).ConfigureAwait(true);
        var initialContent = await File.ReadAllTextAsync(_markerPath).ConfigureAwait(true);
        Assert.Contains("prompt:", initialContent);

        // 2. Update the global prompt — latch on both appsettings.json write AND marker rewrite.
        var settingsChanged = WatchForSettingsChange();
        var markerChanged = WatchForMarkerChange();

        var customPrompt = "CUSTOM GLOBAL PROMPT for testing marker regeneration {baseUrl}";
        var updateResponse = await _client.PutAsJsonAsync(
            new Uri("/mcpserver/workspace/prompt", UriKind.Relative),
            new { template = customPrompt }).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var promptResult = await updateResponse.Content.ReadFromJsonAsync<GlobalPromptResult>().ConfigureAwait(true);
        Assert.NotNull(promptResult);
        Assert.False(promptResult.IsDefault);
        Assert.Equal(customPrompt, promptResult.Template);

        // 3. Wait for FSW latches then validate.
        await settingsChanged.ConfigureAwait(true);
        await markerChanged.ConfigureAwait(true);

        var updatedContent = await File.ReadAllTextAsync(_markerPath).ConfigureAwait(true);
        Assert.Contains("CUSTOM GLOBAL PROMPT for testing marker regeneration", updatedContent);
        Assert.Contains(IntegrationTestPortAllocator.BuildHostBaseUrl(_temporaryPort), updatedContent);
    }

    [Fact]
    public async Task WorkspacePromptUpdate_RegeneratesMarkerFile()
    {
        var key = EncodeKey(Path.GetFullPath(_workspacePath));
        await EnsureWorkspaceSeededAsync().ConfigureAwait(true);
        await StartWorkspaceAndWaitForMarkerAsync(key).ConfigureAwait(true);

        // 2. Update the workspace prompt — latch on marker rewrite.
        var markerChanged = WatchForMarkerChange();
        var workspacePrompt = "WORKSPACE SPECIFIC PROMPT for {{baseUrl}}";
        var updateResponse = await _client.PutAsJsonAsync(
            new Uri($"/mcpserver/workspace/{key}", UriKind.Relative),
            new { promptTemplate = workspacePrompt }).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        // 3. Wait for FSW latch then validate.
        await markerChanged.ConfigureAwait(true);
        var updatedContent = await File.ReadAllTextAsync(_markerPath).ConfigureAwait(true);
        Assert.Contains("WORKSPACE SPECIFIC PROMPT for", updatedContent);
    }

    [Fact]
    public async Task GlobalAndWorkspacePrompts_CombineInMarkerFile()
    {
        var key = EncodeKey(Path.GetFullPath(_workspacePath));
        await EnsureWorkspaceSeededAsync().ConfigureAwait(true);
        await StartWorkspaceAndWaitForMarkerAsync(key).ConfigureAwait(true);

        // 2. Set a custom global prompt — latch on settings + marker writes.
        var settingsChanged = WatchForSettingsChange();
        var markerGlobal = WatchForMarkerChange();
        var globalPrompt = "GLOBAL SECTION {{baseUrl}}";
        var globalResponse = await _client.PutAsJsonAsync(
            new Uri("/mcpserver/workspace/prompt", UriKind.Relative),
            new { template = globalPrompt }).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, globalResponse.StatusCode);
        await settingsChanged.ConfigureAwait(true);
        await markerGlobal.ConfigureAwait(true);

        // Verify global prompt was persisted.
        var settingsJson = await File.ReadAllTextAsync(_appsettingsPath).ConfigureAwait(true);
        Assert.Contains("GLOBAL SECTION", settingsJson);

        // 3. Set a workspace prompt — latch on settings + marker writes.
        var settingsChanged2 = WatchForSettingsChange();
        var markerWs = WatchForMarkerChange();
        var workspacePrompt = "WORKSPACE SECTION {{baseUrl}}";
        var wsResponse = await _client.PutAsJsonAsync(
            new Uri($"/mcpserver/workspace/{key}", UriKind.Relative),
            new { promptTemplate = workspacePrompt }).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, wsResponse.StatusCode);
        await settingsChanged2.ConfigureAwait(true);
        await markerWs.ConfigureAwait(true);

        // Verify workspace update preserved the global prompt.
        var settingsAfter = await File.ReadAllTextAsync(_appsettingsPath).ConfigureAwait(true);
        Assert.Contains("GLOBAL SECTION", settingsAfter);

        // 4. Read the final marker — both prompts should be present.
        var finalContent = await File.ReadAllTextAsync(_markerPath).ConfigureAwait(true);
        Assert.Contains($"GLOBAL SECTION {IntegrationTestPortAllocator.BuildHostBaseUrl(_temporaryPort)}", finalContent);
        Assert.Contains($"WORKSPACE SECTION {IntegrationTestPortAllocator.BuildHostBaseUrl(_temporaryPort)}", finalContent);
    }

    /// <summary>
    /// Returns a <see cref="Task"/> that completes when <c>appsettings.json</c> is written.
    /// The <see cref="FileSystemWatcher"/> releases the latch on the first change/create/rename event so
    /// atomic temp-file replace writes are observed deterministically.
    /// </summary>
    private Task WatchForSettingsChange()
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        FileSystemEventHandler? handler = null;
        RenamedEventHandler? renamedHandler = null;

        void Complete()
        {
            _settingsWatcher.Changed -= handler;
            _settingsWatcher.Created -= handler;
            _settingsWatcher.Renamed -= renamedHandler;
            tcs.TrySetResult();
        }

        handler = (_, _) => Complete();
        renamedHandler = (_, _) => Complete();
        _settingsWatcher.Changed += handler;
        _settingsWatcher.Created += handler;
        _settingsWatcher.Renamed += renamedHandler;

        // Guard against the write completing before the handler was attached.
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        cts.Token.Register(() => tcs.TrySetException(
            new TimeoutException("appsettings.json was not written within 10 s")));
        return tcs.Task;
    }

    /// <summary>
    /// Returns a <see cref="Task"/> that completes when the marker file is created or changed.
    /// </summary>
    private Task WatchForMarkerChange()
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        FileSystemEventHandler? changedHandler = null;
        FileSystemEventHandler? createdHandler = null;

        void Complete()
        {
            _markerWatcher.Changed -= changedHandler;
            _markerWatcher.Created -= createdHandler;
            tcs.TrySetResult();
        }

        changedHandler = (_, _) => Complete();
        createdHandler = (_, _) => Complete();
        _markerWatcher.Changed += changedHandler;
        _markerWatcher.Created += createdHandler;

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        cts.Token.Register(() => tcs.TrySetException(
            new TimeoutException($"Marker file was not written within 10 s at {_markerPath}")));
        return tcs.Task;
    }

    private async Task EnsureWorkspaceSeededAsync()
    {
        var listResponse = await _client.GetAsync(new Uri("/mcpserver/workspace", UriKind.Relative)).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var listBody = await listResponse.Content.ReadAsStringAsync().ConfigureAwait(true);
        Assert.Contains("marker-test", listBody);
    }

    private async Task StartWorkspaceAndWaitForMarkerAsync(string key)
    {
        var startResponse = await _client.PostAsync(
            new Uri($"/mcpserver/workspace/{key}/start", UriKind.Relative), null).ConfigureAwait(true);
        var startBody = await startResponse.Content.ReadAsStringAsync().ConfigureAwait(true);
        Assert.True(startResponse.StatusCode == HttpStatusCode.OK,
            $"Start failed ({startResponse.StatusCode}): {startBody}");

        var status = JsonSerializer.Deserialize<WorkspaceProcessStatus>(
            startBody,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.True(status?.IsRunning == true, $"IsRunning=false: {startBody}");

        await WaitForMarkerFileAsync().ConfigureAwait(true);
    }

    private async Task WaitForMarkerFileAsync()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        try
        {
            while (true)
            {
                if (File.Exists(_markerPath))
                    return;

                await Task.Delay(100, cts.Token).ConfigureAwait(true);
            }
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException($"Marker file was not written within 10 s at {_markerPath}");
        }
    }

    private static string EncodeKey(string path)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(path.Trim());
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    /// <summary>
    /// Factory that pre-seeds appsettings.json in the workspace folder (which Program.cs
    /// sets as ContentRootPath) with a primary workspace matching the test server port.
    /// </summary>
    private sealed class MarkerRegenerationFactory : WebApplicationFactory<McpApiEntryPoint>
    {
        private readonly string _workspacePath;
        private readonly int _temporaryPort = IntegrationTestPortAllocator.AllocateTemporaryPort();

        public MarkerRegenerationFactory(string workspacePath)
        {
            _workspacePath = workspacePath;
        }

        /// <summary>Gets the temporary MCP port assigned to this marker-regeneration host.</summary>
        public int TemporaryPort => _temporaryPort;

        /// <inheritdoc />
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            var templateStoragePath = Path.Combine(ResolveSolutionRoot(), "templates", "prompt-templates.yaml");
            builder.UseEnvironment("Test");
            // Program.cs sets ContentRootPath to the primary workspace from the repo's
            // appsettings.json.  Override to point at the test workspace folder so
            // ResolveAppsettingsPath() finds OUR appsettings.json.
            builder.UseContentRoot(_workspacePath);
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.Sources.Clear();
                config.AddJsonFile(Path.Combine(_workspacePath, "appsettings.json"), optional: false, reloadOnChange: true);
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Mcp:DataSource", ":memory:" },
                    { "Mcp:Port", _temporaryPort.ToString(System.Globalization.CultureInfo.InvariantCulture) },
                    { "Mcp:Tunnel:Port", _temporaryPort.ToString(System.Globalization.CultureInfo.InvariantCulture) },
                    { "Mcp:RepoRoot", _workspacePath },
                    { "Mcp:TodoFilePath", "docs/todo.yaml" },
                    { "Mcp:TodoStorage:Provider", "sqlite" },
                    { "Mcp:TodoStorage:SqliteDataSource", "mcp.db" },
                    { "Mcp:TemplateStorage:FilePath", templateStoragePath },
                });
            });
            // Program.cs skips WorkspaceProcessManager hosted service in "Test" env.
            // Re-register it so auto-start writes the initial marker file.
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ServerRuntimeInfo>();
                services.AddSingleton(new ServerRuntimeInfo(DateTimeOffset.UtcNow, _temporaryPort));
                services.PostConfigure<TodoPromptOptions>(options => options.BaseUrl = IntegrationTestPortAllocator.BuildHostBaseUrl(_temporaryPort));
                services.PostConfigure<TunnelOptions>(options => options.Port = _temporaryPort);
                services.AddHostedService(sp =>
                    (WorkspaceProcessManager)sp.GetRequiredService<IWorkspaceProcessManager>());
            });
        }

        private static string ResolveSolutionRoot()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current is not null)
            {
                var solutionPath = Path.Combine(current.FullName, "McpServer.sln");
                if (File.Exists(solutionPath))
                    return current.FullName;

                current = current.Parent;
            }

            throw new DirectoryNotFoundException("Could not locate the solution root for marker regeneration integration tests.");
        }
    }
}
