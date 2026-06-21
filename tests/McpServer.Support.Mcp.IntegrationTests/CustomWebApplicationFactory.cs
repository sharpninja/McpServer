using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Database;
using McpServer.Support.Mcp;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace McpServer.Support.Mcp.IntegrationTests;

/// <summary>TR-PLANNED-CORE-013: Web application factory for MCP API integration tests.</summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<McpApiEntryPoint>
{
    private static readonly string[] GuardedRepositoryPaths =
    [
        Path.Combine("src", "McpServer.Support.Mcp", "appsettings.yaml"),
        Path.Combine("docs", "Project", "Functional-Requirements.md"),
        Path.Combine("docs", "Project", "Technical-Requirements.md"),
        Path.Combine("docs", "Project", "Testing-Requirements.md"),
        Path.Combine("docs", "Project", "TR-per-FR-Mapping.md"),
        Path.Combine("docs", "Project", "Requirements-Matrix.md"),
    ];

    private readonly Action<IServiceCollection>? _configureServices;
    private readonly IReadOnlyDictionary<string, string?> _configurationOverrides;
    private readonly int _temporaryPort = IntegrationTestPortAllocator.AllocateTemporaryPort();
    private readonly string _solutionRoot;
    private readonly string _workspacePath;
    private readonly string _dataPath;
    private readonly string _appSettingsPath;
    private readonly IReadOnlyDictionary<string, string> _guardedFileHashes;
    private readonly bool _configureDefaultTestDatabase;
    private bool _disposed;

    /// <summary>Initializes a new instance with no service overrides.</summary>
    public CustomWebApplicationFactory() : this(null, null) { }

    /// <summary>Initializes a new instance with optional service overrides.</summary>
    /// <param name="configureServices">Optional callback to register additional or replacement services.</param>
    /// <param name="configurationOverrides">Optional configuration values injected before startup binding.</param>
    internal CustomWebApplicationFactory(
        Action<IServiceCollection>? configureServices,
        IReadOnlyDictionary<string, string?>? configurationOverrides = null,
        bool configureDefaultTestDatabase = true)
    {
        _configureServices = configureServices;
        _configurationOverrides = configurationOverrides ?? new Dictionary<string, string?>();
        _configureDefaultTestDatabase = configureDefaultTestDatabase;
        _solutionRoot = ResolveSolutionRoot();
        _workspacePath = Path.Combine(Path.GetTempPath(), $"mcp-support-integration-{Guid.NewGuid():N}", "workspace");
        _dataPath = Path.Combine(Path.GetTempPath(), $"mcp-support-integration-data-{Guid.NewGuid():N}");
        _appSettingsPath = Path.Combine(_dataPath, "appsettings.yaml");
        Directory.CreateDirectory(_dataPath);
        File.Copy(Path.Combine(ResolveContentRoot(), "appsettings.yaml"), _appSettingsPath, overwrite: true);
        SeedWorkspaceFiles(_workspacePath);
        _guardedFileHashes = CaptureGuardedFileHashes(_solutionRoot);
    }

    /// <summary>Gets the temporary MCP port assigned to this integration-test host.</summary>
    internal int TemporaryPort => _temporaryPort;

    /// <summary>Gets the isolated workspace root used by this integration-test host.</summary>
    internal string WorkspacePath => _workspacePath;

    /// <summary>Gets the runtime base URL expected in hostname-based generated artifacts.</summary>
    internal string ExpectedRuntimeBaseUrl => IntegrationTestPortAllocator.BuildHostBaseUrl(_temporaryPort);

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        builder.UseContentRoot(ResolveContentRoot());
        builder.ConfigureAppConfiguration(config =>
        {
            config.AddYamlFile(_appSettingsPath, optional: false, reloadOnChange: false);
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "DataFolder", _dataPath },
                { "Mcp:RepoRoot", _workspacePath },
                { "Mcp:DataDirectory", _dataPath },
                { "Mcp:DataSource", Path.Combine(_dataPath, "mcp.db") },
                { "Mcp:Database:Provider", "sqlite" },
                { "Mcp:Database:Sqlite:DataSource", Path.Combine(_dataPath, "mcp.db") },
                { "Mcp:UseInMemoryDatabaseForTests", "false" },
                { "Mcp:Port", _temporaryPort.ToString(CultureInfo.InvariantCulture) },
                { "Mcp:Tunnel:Port", _temporaryPort.ToString(CultureInfo.InvariantCulture) },
                { "Mcp:TodoFilePath", Path.Combine(_workspacePath, "docs", "Project", "TODO.yaml") },
                { "Mcp:TodoStorage:Provider", "database" },
                { "Mcp:TodoStorage:SqliteDataSource", Path.Combine(_dataPath, "todo-legacy.db") },
                { "Mcp:SessionsPath", Path.Combine(_workspacePath, "docs", "sessions") },
                { "Mcp:ExternalDocsPath", Path.Combine(_workspacePath, "docs", "external") },
                { "Mcp:GraphRag:Enabled", "true" },
                { "Mcp:GraphRag:RootPath", Path.Combine(_dataPath, "graphrag") },
                { "Mcp:Requirements:FunctionalRequirementsPath", Path.Combine(_workspacePath, "docs", "Project", "Functional-Requirements.md") },
                { "Mcp:Requirements:TechnicalRequirementsPath", Path.Combine(_workspacePath, "docs", "Project", "Technical-Requirements.md") },
                { "Mcp:Requirements:TestingRequirementsPath", Path.Combine(_workspacePath, "docs", "Project", "Testing-Requirements.md") },
                { "Mcp:Requirements:MappingPath", Path.Combine(_workspacePath, "docs", "Project", "TR-per-FR-Mapping.md") },
                { "Mcp:Requirements:MatrixPath", Path.Combine(_workspacePath, "docs", "Project", "Requirements-Matrix.md") },
                { "Mcp:TemplateStorage:FilePath", Path.Combine(_workspacePath, "templates", "prompt-templates.yaml") },
                { "Mcp:Workspaces:0:WorkspacePath", _workspacePath },
                { "Mcp:Workspaces:0:Name", "support-integration-test" },
                { "Mcp:Workspaces:0:TodoPath", Path.Combine(_workspacePath, "docs", "Project", "TODO.yaml") },
                { "Mcp:Workspaces:0:DataDirectory", _dataPath },
                { "Mcp:Workspaces:0:IsPrimary", "true" },
                { "Mcp:Workspaces:0:IsEnabled", "true" },
            });

            if (_configurationOverrides.Count > 0)
                config.AddInMemoryCollection(_configurationOverrides);
        });

        builder.ConfigureTestServices(services =>
        {
            if (_configureDefaultTestDatabase)
                ConfigureTestDatabase(services, Path.Combine(_dataPath, "mcp.db"));

            services.RemoveAll<IWorkspaceProjectionWriter>();
            services.AddSingleton<IWorkspaceProjectionWriter, NoOpWorkspaceProjectionWriter>();
            services.RemoveAll<ServerRuntimeInfo>();
            services.AddSingleton(new ServerRuntimeInfo(DateTimeOffset.UtcNow, _temporaryPort));
            services.PostConfigure<TodoPromptOptions>(options => options.BaseUrl = ExpectedRuntimeBaseUrl);
            services.PostConfigure<TunnelOptions>(options => options.Port = _temporaryPort);

            if (_configureDefaultTestDatabase)
                services.AddHostedService<TestDatabaseInitializer>();
        });

        if (_configureServices is not null)
            builder.ConfigureTestServices(_configureServices);
    }

    private static void ConfigureTestDatabase(IServiceCollection services, string databasePath)
    {
        var connectionString = $"Data Source={databasePath}";
        var providerOptions = McpDatabaseProviderFactory.CreateOptions("sqlite", connectionString);

        services.RemoveAll<DbContextOptions>();
        services.RemoveAll<DbContextOptions<McpDbContext>>();
        services.RemoveAll<IDbContextOptionsConfiguration<McpDbContext>>();
        services.RemoveAll<McpDatabaseProviderOptions>();
        services.RemoveAll<McpDatabaseRuntimeOptions>();
        services.AddSingleton(providerOptions);
        services.AddSingleton(new McpDatabaseRuntimeOptions(
            providerOptions,
            new McpDatabaseEncryptionOptions(
                enabled: false,
                sqliteKey: null,
                sqliteSeeToolPath: null,
                postgreSqlKeyProvider: null,
                postgreSqlPrincipalKey: null,
                sqlServerCertificateName: null,
                sqlServerDatabaseEncryptionKeyName: null)));
        services.AddDbContext<McpDbContext>(options =>
        {
            McpDatabaseProviderFactory.Configure(options, providerOptions);
            options.EnableSensitiveDataLogging();
        }, ServiceLifetime.Scoped, ServiceLifetime.Scoped);
    }

    /// <summary>Creates a test client with workspace and API-key headers already applied.</summary>
    /// <returns>An authenticated client for this factory.</returns>
    internal HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();
        TestAuthHelper.AddAuthHeader(client, Services);
        return client;
    }

    /// <summary>Resolves the solution root for integration-test fixtures.</summary>
    /// <returns>The absolute solution root path.</returns>
    internal static string ResolveSolutionRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var solutionPath = Path.Combine(current.FullName, "McpServer.sln");
            if (File.Exists(solutionPath))
                return current.FullName;

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the solution root for McpServer integration tests.");
    }

    internal static string ResolveContentRoot()
    {
        return Path.Combine(ResolveSolutionRoot(), "src", "McpServer.Support.Mcp");
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            _disposed = true;
            if (disposing)
            {
                VerifyGuardedFilesUnchanged(_solutionRoot, _guardedFileHashes);
                TryDeleteDirectory(_workspacePath);
                TryDeleteDirectory(_dataPath);
                TryDeleteDirectory(Path.GetDirectoryName(_workspacePath));
            }
        }

        base.Dispose(disposing);
    }

    private static void SeedWorkspaceFiles(string workspacePath)
    {
        var projectPath = Path.Combine(workspacePath, "docs", "Project");
        Directory.CreateDirectory(projectPath);
        Directory.CreateDirectory(Path.Combine(workspacePath, "docs", "sessions"));
        Directory.CreateDirectory(Path.Combine(workspacePath, "docs", "external"));
        Directory.CreateDirectory(Path.Combine(workspacePath, "templates"));

        File.WriteAllText(Path.Combine(projectPath, "TODO.yaml"), """
            mvp-app:
              high-priority: []
            mvp-support:
              high-priority: []
            """);
        File.WriteAllText(Path.Combine(projectPath, "Functional-Requirements.md"), """
            # Functional Requirements (MCP Server)

            ## FR-MCP-001 Seed Entry

            Seed FR body.
            """);
        File.WriteAllText(Path.Combine(projectPath, "Technical-Requirements.md"), """
            # Technical Requirements (MCP Server)

            ## TR-MCP-001 Seed Entry

            Seed TR body.
            """);
        File.WriteAllText(Path.Combine(projectPath, "Testing-Requirements.md"), """
            # Testing Requirements (MCP Server)

            - TEST-MCP-001: Seed test requirement.
            """);
        File.WriteAllText(Path.Combine(projectPath, "TR-per-FR-Mapping.md"), """
            # TR per FR Mapping

            | FR | TR |
            | --- | --- |
            | FR-MCP-001 | TR-MCP-001 |
            """);
        File.WriteAllText(Path.Combine(projectPath, "Requirements-Matrix.md"), """
            # Requirements Matrix

            | ID | Status | Source |
            | --- | --- | --- |
            | FR-MCP-001 | Tracked | Functional-Requirements.md |
            """);
        File.WriteAllText(Path.Combine(workspacePath, "templates", "prompt-templates.yaml"), """
            templates:
              default-marker-prompt:
                title: Test Marker Prompt
                category: agent
                tags:
                - marker
                engine: handlebars
                content: |
                  Test marker prompt for {{baseUrl}}
            """);
    }

    private static IReadOnlyDictionary<string, string> CaptureGuardedFileHashes(string solutionRoot)
    {
        var hashes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var relativePath in GuardedRepositoryPaths)
        {
            var path = Path.Combine(solutionRoot, relativePath);
            if (File.Exists(path))
                hashes[relativePath] = ComputeHash(path);
        }

        return hashes;
    }

    private static void VerifyGuardedFilesUnchanged(string solutionRoot, IReadOnlyDictionary<string, string> expectedHashes)
    {
        foreach (var relativePath in GuardedRepositoryPaths)
        {
            var path = Path.Combine(solutionRoot, relativePath);
            var beforeExists = expectedHashes.TryGetValue(relativePath, out var expectedHash);
            if (!beforeExists && File.Exists(path))
                throw new InvalidOperationException($"Integration test modified guarded repository file '{relativePath}'.");

            if (beforeExists && (!File.Exists(path) || !string.Equals(expectedHash, ComputeHash(path), StringComparison.Ordinal)))
                throw new InvalidOperationException($"Integration test modified guarded repository file '{relativePath}'.");
        }
    }

    private static string ComputeHash(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    private static void TryDeleteDirectory(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
            // Best-effort cleanup only.
        }
    }

    private sealed class NoOpWorkspaceProjectionWriter : IWorkspaceProjectionWriter
    {
        public Task WriteProjectionAsync(IReadOnlyList<WorkspaceConfigEntry> workspaces, CancellationToken ct)
            => Task.CompletedTask;
    }

    private sealed class TestDatabaseInitializer : IHostedService
    {
        private readonly IServiceProvider _services;

        public TestDatabaseInitializer(IServiceProvider services)
        {
            _services = services;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
            await db.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
