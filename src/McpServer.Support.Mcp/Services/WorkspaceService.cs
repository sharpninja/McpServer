using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-009 / TR-MCP-WS-002: Workspace CRUD backed by <c>appsettings.json</c>.
/// Workspaces are stored as an array at <c>Mcp:Workspaces</c> and persisted to
/// <c>appsettings.json</c> in the content root on every mutation.
/// </summary>
public sealed class WorkspaceService : IWorkspaceService
{
    private static readonly SemaphoreSlim s_writeLock = new(1, 1);
    private const string DefaultTodoPath = "docs/todo.yaml";

    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _env;
    private readonly ILogger<WorkspaceService> _logger;

    /// <summary>Initializes a new instance of the <see cref="WorkspaceService"/> class.</summary>
    public WorkspaceService(IConfiguration configuration, IHostEnvironment env, ILogger<WorkspaceService> logger)
    {
        _configuration = configuration;
        _env = env;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<WorkspaceListResult> ListAsync(CancellationToken ct = default)
    {
        var items = ReadAll().Select(ToDto).OrderBy(w => w.Name).ToList();
        return Task.FromResult(new WorkspaceListResult(items, items.Count));
    }

    /// <inheritdoc />
    public Task<WorkspaceDto?> GetAsync(string workspacePath, CancellationToken ct = default)
    {
        var normalized = NormalizePath(workspacePath);
        var entry = ReadAll().FirstOrDefault(w => NormalizePath(w.WorkspacePath) == normalized);
        return Task.FromResult(entry is null ? (WorkspaceDto?)null : ToDto(entry));
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
                IsPrimary = request.IsPrimary,
                IsEnabled = request.IsEnabled,
                DateTimeCreated = now,
                DateTimeModified = now,
            };
            all.Add(entry);
            await WriteAllAsync(all, ct).ConfigureAwait(false);
            _logger.LogInformation("Workspace created: {Name} at {Path}", entry.Name, entry.WorkspacePath);
            return new WorkspaceMutationResult(true, Workspace: ToDto(entry));
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
            entry.DateTimeModified = DateTimeOffset.UtcNow;

            await WriteAllAsync(all, ct).ConfigureAwait(false);
            _logger.LogInformation("Workspace updated: {Name} at {Path}", entry.Name, entry.WorkspacePath);
            return new WorkspaceMutationResult(true, Workspace: ToDto(entry));
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
            var dto = ToDto(entry);
            all.Remove(entry);
            await WriteAllAsync(all, ct).ConfigureAwait(false);
            _logger.LogInformation("Workspace deleted: {Name} at {Path}", dto.Name, dto.WorkspacePath);
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
            return new WorkspaceInitResult(true, FilesCreated: filesCreated);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "Failed to initialize workspace: {Path}", normalized);
            return new WorkspaceInitResult(false, ex.Message, filesCreated);
        }
    }

    private List<WorkspaceConfigEntry> ReadAll()
        => _configuration.GetSection("Mcp:Workspaces").Get<List<WorkspaceConfigEntry>>() ?? [];

    private async Task WriteAllAsync(List<WorkspaceConfigEntry> workspaces, CancellationToken ct)
    {
        var path = ResolveAppsettingsPath();
        var jsonText = await File.ReadAllTextAsync(path, ct).ConfigureAwait(false);
        var doc = JsonNode.Parse(jsonText, new JsonNodeOptions { PropertyNameCaseInsensitive = true })!;
        var mcp = doc["Mcp"] as JsonObject ?? new JsonObject();
        mcp["Workspaces"] = JsonSerializer.SerializeToNode(workspaces, s_jsonOptions);
        doc["Mcp"] = mcp;
        await File.WriteAllTextAsync(path, doc.ToJsonString(s_jsonOptions), ct).ConfigureAwait(false);
        if (_configuration is IConfigurationRoot root)
            root.Reload();
    }

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Resolves the path to <c>appsettings.json</c>, falling back to the application base directory
    /// when the file does not exist under <see cref="IHostEnvironment.ContentRootPath"/>
    /// (which may point to a workspace root rather than the install directory).
    /// </summary>
    internal string ResolveAppsettingsPath()
    {
        var fromContentRoot = Path.Combine(_env.ContentRootPath, "appsettings.json");
        if (File.Exists(fromContentRoot)) return fromContentRoot;

        var fromBaseDir = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (File.Exists(fromBaseDir)) return fromBaseDir;

        return fromContentRoot; // fallback — will throw a clear error on read
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

    private static WorkspaceDto ToDto(WorkspaceConfigEntry e) => new()
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
    };
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

