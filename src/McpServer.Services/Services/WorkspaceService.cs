using System.Text.Json;
using System.Text.Json.Nodes;
using McpServer.Support.Mcp.Notifications;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-009 / TR-MCP-WS-002: Workspace CRUD backed by <c>appsettings.json</c> or <c>appsettings.yaml</c>.
/// Workspaces are stored at <c>Mcp:Workspaces</c> and persisted to the appsettings file in the content root.
/// Prefers <c>appsettings.yaml</c> when present.
/// </summary>
public sealed class WorkspaceService : IWorkspaceService
{
    private static readonly SemaphoreSlim s_writeLock = new(1, 1);
    private const string DefaultTodoPath = "docs/todo.yaml";

    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _env;
    private readonly IProcessRunner _processRunner;
    private readonly IChangeEventBus? _eventBus;
    private readonly ILogger<WorkspaceService> _logger;

    /// <summary>Initializes a new instance of the <see cref="WorkspaceService"/> class.</summary>
    public WorkspaceService(IConfiguration configuration, IHostEnvironment env, IProcessRunner processRunner, ILogger<WorkspaceService> logger, IChangeEventBus? eventBus = null)
    {
        _configuration = configuration;
        _env = env;
        _processRunner = processRunner;
        _eventBus = eventBus;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<WorkspaceListResult> ListAsync(CancellationToken ct = default)
    {
        var all = ReadAll();
        var dtos = new List<WorkspaceDto>(all.Count);
        foreach (var entry in all.OrderBy(w => w.Name))
        {
            dtos.Add(await ToDtoAsync(entry, ct).ConfigureAwait(false));
        }
        return new WorkspaceListResult(dtos, dtos.Count);
    }

    /// <inheritdoc />
    public async Task<WorkspaceDto?> GetAsync(string workspacePath, CancellationToken ct = default)
    {
        var normalized = NormalizePath(workspacePath);
        var entry = ReadAll().FirstOrDefault(w => NormalizePath(w.WorkspacePath) == normalized);
        return entry is null ? null : await ToDtoAsync(entry, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<WorkspaceMutationResult> CreateAsync(WorkspaceCreateRequest request, CancellationToken ct = default)
    {
        var normalized = NormalizePath(request.WorkspacePath);
        if (string.IsNullOrWhiteSpace(normalized))
            return new WorkspaceMutationResult(false, "WorkspacePath is required.");
        if (!Path.IsPathRooted(normalized))
            return new WorkspaceMutationResult(false, "WorkspacePath must be an absolute path.");

        await s_writeLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var all = ReadAll();
            if (all.Any(w => NormalizePath(w.WorkspacePath) == normalized))
                return new WorkspaceMutationResult(false, $"Workspace already registered: {normalized}");

            var now = DateTimeOffset.UtcNow;
            var entry = new WorkspaceConfigEntry
            {
                WorkspacePath = normalized,
                Name = !string.IsNullOrWhiteSpace(request.Name) ? request.Name.Trim() : DeriveNameFromPath(normalized),
                TodoPath = !string.IsNullOrWhiteSpace(request.TodoPath) ? request.TodoPath.Trim() : DefaultTodoPath,
                DataDirectory = string.IsNullOrWhiteSpace(request.DataDirectory) ? null : Path.GetFullPath(request.DataDirectory.Trim()),
                TunnelProvider = string.IsNullOrWhiteSpace(request.TunnelProvider) ? null : request.TunnelProvider.Trim(),
                RunAs = string.IsNullOrWhiteSpace(request.RunAs) ? null : request.RunAs.Trim(),
                PromptTemplate = string.IsNullOrWhiteSpace(request.PromptTemplate) ? null : request.PromptTemplate.Trim(),
                StatusPrompt = StripIfDefault(nameof(TodoPromptDefaults.StatusPrompt), request.StatusPrompt),
                ImplementPrompt = StripIfDefault(nameof(TodoPromptDefaults.ImplementPrompt), request.ImplementPrompt),
                PlanPrompt = StripIfDefault(nameof(TodoPromptDefaults.PlanPrompt), request.PlanPrompt),
                BannedLicenses = NormalizePolicyList(request.BannedLicenses),
                BannedCountriesOfOrigin = NormalizePolicyList(request.BannedCountriesOfOrigin, toUpperInvariant: true),
                BannedOrganizations = NormalizePolicyList(request.BannedOrganizations),
                BannedIndividuals = NormalizePolicyList(request.BannedIndividuals),
                IsPrimary = request.IsPrimary,
                IsEnabled = request.IsEnabled,
                DateTimeCreated = now,
                DateTimeModified = now,
            };
            all.Add(entry);
            await WriteAllAsync(all, ct).ConfigureAwait(false);
            _logger.LogInformation("Workspace created: {Name} at {Path}", entry.Name, entry.WorkspacePath);
            await PublishChangeSafeAsync(ChangeEventActions.Created, normalized, ct).ConfigureAwait(false);
            return new WorkspaceMutationResult(true, Workspace: await ToDtoAsync(entry, ct).ConfigureAwait(false));
        }
        finally
        {
            s_writeLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<WorkspaceMutationResult> UpdateAsync(string workspacePath, WorkspaceUpdateRequest request, CancellationToken ct = default)
    {
        var normalized = NormalizePath(workspacePath);
        await s_writeLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var all = ReadAll();
            var entry = all.FirstOrDefault(w => NormalizePath(w.WorkspacePath) == normalized);
            if (entry is null)
                return new WorkspaceMutationResult(false, $"Workspace not found: {normalized}");

            if (request.Name is not null)
                entry.Name = string.IsNullOrWhiteSpace(request.Name) ? DeriveNameFromPath(normalized) : request.Name.Trim();
            if (request.TodoPath is not null)
                entry.TodoPath = string.IsNullOrWhiteSpace(request.TodoPath) ? DefaultTodoPath : request.TodoPath.Trim();
            if (request.TunnelProvider is not null)
                entry.TunnelProvider = string.IsNullOrWhiteSpace(request.TunnelProvider) ? null : request.TunnelProvider.Trim();
            if (request.RunAs is not null)
                entry.RunAs = string.IsNullOrWhiteSpace(request.RunAs) ? null : request.RunAs.Trim();
            if (request.DataDirectory is not null)
                entry.DataDirectory = string.IsNullOrWhiteSpace(request.DataDirectory) ? null : Path.GetFullPath(request.DataDirectory.Trim());
            if (request.IsPrimary is not null)
                entry.IsPrimary = request.IsPrimary.Value;
            if (request.IsEnabled is not null)
                entry.IsEnabled = request.IsEnabled.Value;
            if (request.PromptTemplate is not null)
                entry.PromptTemplate = string.IsNullOrWhiteSpace(request.PromptTemplate) ? null : request.PromptTemplate.Trim();
            if (request.StatusPrompt is not null)
                entry.StatusPrompt = StripIfDefault(nameof(TodoPromptDefaults.StatusPrompt), request.StatusPrompt);
            if (request.ImplementPrompt is not null)
                entry.ImplementPrompt = StripIfDefault(nameof(TodoPromptDefaults.ImplementPrompt), request.ImplementPrompt);
            if (request.PlanPrompt is not null)
                entry.PlanPrompt = StripIfDefault(nameof(TodoPromptDefaults.PlanPrompt), request.PlanPrompt);
            if (request.BannedLicenses is not null)
                entry.BannedLicenses = NormalizePolicyList(request.BannedLicenses);
            if (request.BannedCountriesOfOrigin is not null)
                entry.BannedCountriesOfOrigin = NormalizePolicyList(request.BannedCountriesOfOrigin, toUpperInvariant: true);
            if (request.BannedOrganizations is not null)
                entry.BannedOrganizations = NormalizePolicyList(request.BannedOrganizations);
            if (request.BannedIndividuals is not null)
                entry.BannedIndividuals = NormalizePolicyList(request.BannedIndividuals);
            entry.DateTimeModified = DateTimeOffset.UtcNow;

            await WriteAllAsync(all, ct).ConfigureAwait(false);
            _logger.LogInformation("Workspace updated: {Name} at {Path}", entry.Name, entry.WorkspacePath);
            await PublishChangeSafeAsync(ChangeEventActions.Updated, normalized, ct).ConfigureAwait(false);
            return new WorkspaceMutationResult(true, Workspace: await ToDtoAsync(entry, ct).ConfigureAwait(false));
        }
        finally
        {
            s_writeLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<WorkspaceMutationResult> DeleteAsync(string workspacePath, CancellationToken ct = default)
    {
        var normalized = NormalizePath(workspacePath);
        await s_writeLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var all = ReadAll();
            var entry = all.FirstOrDefault(w => NormalizePath(w.WorkspacePath) == normalized);
            if (entry is null)
                return new WorkspaceMutationResult(false, $"Workspace not found: {normalized}");
            var dto = await ToDtoAsync(entry, ct).ConfigureAwait(false);
            all.Remove(entry);
            await WriteAllAsync(all, ct).ConfigureAwait(false);
            _logger.LogInformation("Workspace deleted: {Name} at {Path}", dto.Name, dto.WorkspacePath);
            await PublishChangeSafeAsync(ChangeEventActions.Deleted, normalized, ct).ConfigureAwait(false);
            return new WorkspaceMutationResult(true, Workspace: dto);
        }
        finally
        {
            s_writeLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<WorkspaceInitResult> InitAsync(string workspacePath, CancellationToken ct = default)
    {
        var normalized = NormalizePath(workspacePath);
        var entry = ReadAll().FirstOrDefault(w => NormalizePath(w.WorkspacePath) == normalized);
        if (entry is null)
            return new WorkspaceInitResult(false, $"Workspace not found: {normalized}");

        var filesCreated = new List<string>();
        try
        {
            if (!Directory.Exists(normalized))
            {
                Directory.CreateDirectory(normalized);
                filesCreated.Add(normalized);
            }
            var todoFullPath = Path.GetFullPath(Path.Combine(normalized, entry.TodoPath));
            var todoDir = Path.GetDirectoryName(todoFullPath);
            if (!string.IsNullOrEmpty(todoDir) && !Directory.Exists(todoDir))
            {
                Directory.CreateDirectory(todoDir);
                filesCreated.Add(todoDir);
            }
            if (!File.Exists(todoFullPath))
            {
                await File.WriteAllTextAsync(todoFullPath, "# TODO items for this workspace\n", ct).ConfigureAwait(false);
                filesCreated.Add(todoFullPath);
            }
            var dataDir = string.IsNullOrWhiteSpace(entry.DataDirectory) ? normalized : Path.GetFullPath(entry.DataDirectory);
            if (!Directory.Exists(dataDir))
                Directory.CreateDirectory(dataDir);
            var dbPath = Path.Combine(dataDir, "mcp.db");
            if (!File.Exists(dbPath))
            {
                await File.WriteAllBytesAsync(dbPath, [], ct).ConfigureAwait(false);
                filesCreated.Add(dbPath);
            }
            _logger.LogInformation("Workspace initialized: {Path}, {Count} files created", normalized, filesCreated.Count);
            await PublishChangeSafeAsync(ChangeEventActions.Updated, normalized, ct).ConfigureAwait(false);
            return new WorkspaceInitResult(true, FilesCreated: filesCreated);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "Failed to initialize workspace: {Path}", normalized);
            return new WorkspaceInitResult(false, ex.Message, filesCreated);
        }
    }

    private List<WorkspaceConfigEntry> ReadAll()
    {
        var configured = _configuration.GetSection("Mcp:Workspaces").Get<List<WorkspaceConfigEntry>>() ?? [];

        // TR-MCP-TODO-008: always consider Mcp:RepoRoot as an implicit workspace so
        // legacy deployments and integration-test fixtures (which set RepoRoot but not
        // Mcp:Workspaces) still have a tenant identity. De-dup against configured entries.
        var repoRoot = _configuration["Mcp:RepoRoot"];
        if (!string.IsNullOrWhiteSpace(repoRoot))
        {
            // Resolve relative paths against ContentRoot (not CWD) so the synthesized
            // workspace path matches what token seeding (NormalizeWorkspacePathForToken)
            // stores. In test hosts CWD can diverge from ContentRoot.
            var absolute = (Path.IsPathRooted(repoRoot)
                    ? Path.GetFullPath(repoRoot)
                    : Path.GetFullPath(Path.Combine(_env.ContentRootPath, repoRoot)))
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (!configured.Any(w => string.Equals(NormalizePath(w.WorkspacePath), absolute, StringComparison.OrdinalIgnoreCase)))
            {
                var todoRel = _configuration["Mcp:TodoFilePath"];
                configured.Insert(0, new WorkspaceConfigEntry
                {
                    WorkspacePath = absolute,
                    Name = DeriveNameFromPath(absolute),
                    TodoPath = string.IsNullOrWhiteSpace(todoRel) ? DefaultTodoPath : todoRel,
                    IsPrimary = true,
                    IsEnabled = true,
                });
            }
        }

        return configured;
    }

    private async Task WriteAllAsync(List<WorkspaceConfigEntry> workspaces, CancellationToken ct)
    {
        var path = ResolveAppsettingsPath();
        if (path.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase))
        {
            await WriteAllYamlAsync(path, workspaces, ct).ConfigureAwait(false);
        }
        else
        {
            var jsonText = await File.ReadAllTextAsync(path, ct).ConfigureAwait(false);
            var doc = JsonNode.Parse(jsonText, new JsonNodeOptions { PropertyNameCaseInsensitive = true })!;
            var mcp = doc["Mcp"] as JsonObject ?? new JsonObject();
            mcp["Workspaces"] = JsonSerializer.SerializeToNode(workspaces, s_jsonOptions);
            doc["Mcp"] = mcp;
            await File.WriteAllTextAsync(path, doc.ToJsonString(s_jsonOptions), ct).ConfigureAwait(false);
        }

        if (_configuration is IConfigurationRoot root)
            root.Reload();
    }

    private static async Task WriteAllYamlAsync(string path, List<WorkspaceConfigEntry> workspaces, CancellationToken ct)
    {
        var yamlText = await File.ReadAllTextAsync(path, ct).ConfigureAwait(false);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(NullNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
        var serializer = new SerializerBuilder()
            .WithNamingConvention(NullNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .Build();

        var data = deserializer.Deserialize<Dictionary<string, object>>(yamlText);
        if (!data.TryGetValue("Mcp", out var mcpObj) || mcpObj is not IDictionary<object, object> mcpDict)
        {
            data["Mcp"] = mcpDict = new Dictionary<object, object>();
        }

        mcpDict["Workspaces"] = workspaces;
        var output = serializer.Serialize(data);
        await File.WriteAllTextAsync(path, output, ct).ConfigureAwait(false);
    }

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Resolves the path to <c>appsettings.yaml</c> or <c>appsettings.json</c>, preferring YAML when present.
    /// Falls back to the application base directory when the file does not exist under the content root.
    /// </summary>
    internal string ResolveAppsettingsPath()
    {
        var contentRoot = _env.ContentRootPath;
        var baseDir = AppContext.BaseDirectory;

        var yamlContentRoot = Path.Combine(contentRoot, "appsettings.yaml");
        if (File.Exists(yamlContentRoot)) return yamlContentRoot;

        var yamlBaseDir = Path.Combine(baseDir, "appsettings.yaml");
        if (File.Exists(yamlBaseDir)) return yamlBaseDir;

        var jsonContentRoot = Path.Combine(contentRoot, "appsettings.json");
        if (File.Exists(jsonContentRoot)) return jsonContentRoot;

        var jsonBaseDir = Path.Combine(baseDir, "appsettings.json");
        if (File.Exists(jsonBaseDir)) return jsonBaseDir;

        return jsonContentRoot; // fallback — will throw a clear error on read
    }

    private static string DeriveNameFromPath(string path)
    {
        var name = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return string.IsNullOrWhiteSpace(name) ? "workspace" : name;
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        return Path.GetFullPath(path.Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
    }

    /// <summary>Returns null when the prompt value is empty/whitespace or matches the built-in default.</summary>
    private static string? StripIfDefault(string promptName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return TodoPromptDefaults.IsDefault(promptName, value) ? null : value.Trim();
    }

    private static List<string>? NormalizePolicyList(List<string>? source, bool toUpperInvariant = false)
    {
        if (source is null)
            return null;

        var comparer = toUpperInvariant ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
        var seen = new HashSet<string>(comparer);
        var normalized = new List<string>(source.Count);

        foreach (var value in source)
        {
            if (string.IsNullOrWhiteSpace(value))
                continue;

            var candidate = value.Trim();
            if (toUpperInvariant)
                candidate = candidate.ToUpperInvariant();

            if (seen.Add(candidate))
                normalized.Add(candidate);
        }

        return normalized;
    }

    private async Task<WorkspaceDto> ToDtoAsync(WorkspaceConfigEntry e, CancellationToken ct)
    {
        var dto = new WorkspaceDto
        {
            WorkspacePath = e.WorkspacePath,
            Name = e.Name,
            TodoPath = e.TodoPath,
            DataDirectory = string.IsNullOrWhiteSpace(e.DataDirectory) ? null : e.DataDirectory,
            TunnelProvider = string.IsNullOrWhiteSpace(e.TunnelProvider) ? null : e.TunnelProvider,
            IsPrimary = e.IsPrimary,
            IsEnabled = e.IsEnabled,
            DateTimeCreated = e.DateTimeCreated,
            DateTimeModified = e.DateTimeModified,
            RunAs = string.IsNullOrWhiteSpace(e.RunAs) ? null : e.RunAs,
            PromptTemplate = string.IsNullOrWhiteSpace(e.PromptTemplate) ? null : e.PromptTemplate,
            StatusPrompt = e.StatusPrompt ?? TodoPromptDefaults.StatusPrompt,
            ImplementPrompt = e.ImplementPrompt ?? TodoPromptDefaults.ImplementPrompt,
            PlanPrompt = e.PlanPrompt ?? TodoPromptDefaults.PlanPrompt,
            BannedLicenses = NormalizePolicyList(e.BannedLicenses) ?? [],
            BannedCountriesOfOrigin = NormalizePolicyList(e.BannedCountriesOfOrigin, toUpperInvariant: true) ?? [],
            BannedOrganizations = NormalizePolicyList(e.BannedOrganizations) ?? [],
            BannedIndividuals = NormalizePolicyList(e.BannedIndividuals) ?? [],
            GitRemoteUrl = await GetGitRemoteUrlAsync(e.WorkspacePath, ct).ConfigureAwait(false),
        };
        return dto;
    }

    private async Task<string?> GetGitRemoteUrlAsync(string workspacePath, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(workspacePath) || !Directory.Exists(workspacePath))
            return null;

        try
        {
            // Use IProcessRunner to call: git -C <path> config --get remote.origin.url
            var request = new ProcessRunRequest("git", $"-C \"{workspacePath}\" config --get remote.origin.url");

            var result = await _processRunner.RunAsync(request, ct).ConfigureAwait(false);
            if (result.ExitCode != 0)
                return null;

            return result.Stdout?.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve git remote URL for workspace {Path}", workspacePath);
            return null;
        }
    }

    private async Task PublishChangeSafeAsync(string action, string entityId, CancellationToken ct)
    {
        if (_eventBus is null)
            return;

        try
        {
            await _eventBus.PublishAsync(
                new ChangeEvent
                {
                    Category = ChangeEventCategories.Workspace,
                    Action = action,
                    EntityId = entityId,
                    ResourceUri = $"mcp://workspace/workspace/{entityId}",
                },
                ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed publishing workspace change event for {EntityId}", entityId);
        }
    }
}

/// <summary>Workspace entry as stored in <c>appsettings.json</c> under <c>Mcp:Workspaces</c>.</summary>
public sealed class WorkspaceConfigEntry
{
    /// <summary>Absolute path to the workspace root folder (primary key).</summary>
    public string WorkspacePath { get; set; } = string.Empty;

    /// <summary>Human-readable workspace name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Relative path to the todo file within the workspace.</summary>
    public string TodoPath { get; set; } = "docs/todo.yaml";

    /// <summary>
    /// Override directory for <c>mcp.db</c> and related data files.
    /// Null = <see cref="WorkspacePath"/> is used as the data directory.
    /// </summary>
    public string? DataDirectory { get; set; }

    /// <summary>Tunnel provider key (ngrok, cloudflare, frp) or null if disabled.</summary>
    public string? TunnelProvider { get; set; }

    /// <summary>Identity for child process (null = current Windows user).</summary>
    public string? RunAs { get; set; }

    /// <summary>
    /// GitHub personal access token or OAuth token passed as <c>GH_TOKEN</c> to
    /// the Copilot CLI process. Required when the service runs as a system account
    /// that cannot access the user's Windows keyring. Null = default auth discovery.
    /// </summary>
    public string? GitHubToken { get; set; }

    /// <summary>
    /// When true, this workspace is the primary instance — the host process serves it directly
    /// and no child app is spun up.
    /// </summary>
    public bool IsPrimary { get; set; }

    /// <summary>
    /// When false, the workspace is skipped during auto-start. Default: true.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Optional markdown prompt template appended to the global marker prompt for this workspace.
    /// Supports <c>{baseUrl}</c> placeholder. When <see langword="null"/>, only the global prompt is used.
    /// </summary>
    public string? PromptTemplate { get; set; }

    /// <summary>Override for the Copilot status prompt. Null = use built-in default.</summary>
    public string? StatusPrompt { get; set; }

    /// <summary>Override for the Copilot implement prompt. Null = use built-in default.</summary>
    public string? ImplementPrompt { get; set; }

    /// <summary>Override for the Copilot plan prompt. Null = use built-in default.</summary>
    public string? PlanPrompt { get; set; }

    /// <summary>SPDX license identifiers banned in this workspace (e.g. "GPL-3.0", "AGPL-3.0").</summary>
    public List<string>? BannedLicenses { get; set; }

    /// <summary>ISO 3166-1 alpha-2 country codes banned as dependency origin (e.g. "CN", "RU").</summary>
    public List<string>? BannedCountriesOfOrigin { get; set; }

    /// <summary>Organization/company names whose code and libraries are banned.</summary>
    public List<string>? BannedOrganizations { get; set; }

    /// <summary>Individual names/handles whose code and libraries are banned.</summary>
    public List<string>? BannedIndividuals { get; set; }

    /// <summary>
    /// Absolute path to the Copilot CLI agent executable.
    /// Null = use the default (<c>copilot</c>).
    /// </summary>
    public string? AgentPath { get; set; }

    /// <summary>When the workspace was registered.</summary>
    public DateTimeOffset DateTimeCreated { get; set; }

    /// <summary>When the workspace was last updated.</summary>
    public DateTimeOffset DateTimeModified { get; set; }
}
