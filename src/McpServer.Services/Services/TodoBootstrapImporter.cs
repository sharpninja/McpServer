using System.Collections.ObjectModel;
using System.Text.Json;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-TODO-008 Phase 4: per-workspace YAML bootstrap importer. On host
/// start, iterates every <c>Mcp:Workspaces</c> entry and, for each workspace
/// that has no TODO rows stamped with its workspace id and no marker file,
/// imports the workspace's <c>TodoPath</c> YAML into the authoritative
/// database with every row stamped via <see cref="McpDbContext.OverrideWorkspaceId"/>.
/// </summary>
/// <remarks>
/// <para>
/// Bootstrap is mirror-not-merge: it only fires when the workspace slice is
/// empty. The single source of truth thereafter is the database; subsequent
/// writes go through <see cref="ITodoService"/> and project back out to YAML
/// via TR-MCP-TODO-006 rather than the other way around.
/// </para>
/// <para>
/// Idempotency: a marker file <c>{dataDir}/todo-bootstrap.marker</c> prevents
/// re-import after a successful run. Clearing it is the supported way to
/// re-hydrate a workspace from YAML.
/// </para>
/// </remarks>
internal sealed class TodoBootstrapImporter : IHostedService
{
    internal const string MarkerFileName = "todo-bootstrap.marker";
    internal const string YamlBootstrapSource = "yaml-bootstrap";
    internal const string StandardItemKind = "standard";
    internal const string CodeReviewPhaseItemKind = "code_review_phase";
    internal const string CodeReviewSectionKey = "code-review-remediation";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TodoBootstrapImporter> _logger;
    private readonly JsonSerializerOptions _json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    /// <summary>Initializes a new instance of the <see cref="TodoBootstrapImporter"/> class.</summary>
    public TodoBootstrapImporter(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<TodoBootstrapImporter> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await RunAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TodoBootstrapImporter failed; continuing startup without bootstrap.");
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>Imports every configured workspace; surface for unit tests.</summary>
    internal async Task<BootstrapSummary> RunAsync(CancellationToken cancellationToken)
    {
        var workspaces = _configuration.GetSection("Mcp:Workspaces").Get<List<WorkspaceConfigEntry>>() ?? [];
        var outcomes = new List<BootstrapOutcome>(workspaces.Count);
        foreach (var entry in workspaces)
        {
            if (cancellationToken.IsCancellationRequested)
                break;
            if (entry is null || string.IsNullOrWhiteSpace(entry.WorkspacePath))
                continue;
            outcomes.Add(await BootstrapWorkspaceAsync(entry, cancellationToken).ConfigureAwait(false));
        }
        return new BootstrapSummary(new ReadOnlyCollection<BootstrapOutcome>(outcomes));
    }

    /// <summary>Bootstraps a single workspace; surface for unit tests.</summary>
    internal async Task<BootstrapOutcome> BootstrapWorkspaceAsync(WorkspaceConfigEntry entry, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var workspacePath = Path.GetFullPath(entry.WorkspacePath);
        var todoRel = string.IsNullOrWhiteSpace(entry.TodoPath) ? "docs/todo.yaml" : entry.TodoPath;
        var todoAbsolute = Path.IsPathRooted(todoRel)
            ? Path.GetFullPath(todoRel)
            : Path.GetFullPath(Path.Combine(workspacePath, todoRel));
        var dataDirectory = string.IsNullOrWhiteSpace(entry.DataDirectory) ? workspacePath : Path.GetFullPath(entry.DataDirectory);
        var markerPath = Path.Combine(dataDirectory, MarkerFileName);

        if (File.Exists(markerPath))
            return new BootstrapOutcome(workspacePath, BootstrapResult.SkippedMarkerPresent, 0);

        if (!File.Exists(todoAbsolute))
        {
            _logger.LogInformation("TodoBootstrapImporter: '{Workspace}' has no YAML at '{Path}'; no-op.", workspacePath, todoAbsolute);
            return new BootstrapOutcome(workspacePath, BootstrapResult.SkippedMissingYaml, 0);
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<McpDbContext>();
        ctx.OverrideWorkspaceId(workspacePath);

        if (await ctx.TodoItems.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            await WriteMarkerAsync(markerPath, "target non-empty", cancellationToken).ConfigureAwait(false);
            return new BootstrapOutcome(workspacePath, BootstrapResult.SkippedWorkspacePopulated, 0);
        }

        var file = await TodoYamlFileSerializer.ReadIfExistsAsync(todoAbsolute, cancellationToken).ConfigureAwait(false);
        if (file is null)
            return new BootstrapOutcome(workspacePath, BootstrapResult.SkippedMissingYaml, 0);

        var importedAtUtc = DateTime.UtcNow.ToString("O");
        var (items, history, metadata) = ProjectEntities(file, importedAtUtc);

        foreach (var item in items)
            ctx.TodoItems.Add(item);
        foreach (var row in history)
            ctx.TodoAuditHistory.Add(row);
        ctx.TodoDocumentMetadata.Add(metadata);

        await ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await WriteMarkerAsync(markerPath, $"imported items={items.Count}", cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "TodoBootstrapImporter: workspace '{Workspace}' bootstrapped {Items} items from '{Path}'.",
            workspacePath, items.Count, todoAbsolute);
        return new BootstrapOutcome(workspacePath, BootstrapResult.Imported, items.Count);
    }

    private (List<TodoItemEntity> Items, List<TodoAuditHistoryEntity> History, TodoDocumentMetadataEntity Metadata) ProjectEntities(
        TodoFile file, string importedAtUtc)
    {
        var items = new List<TodoItemEntity>();
        var history = new List<TodoAuditHistoryEntity>();

        var sectionOrder = 0;
        foreach (var (sectionKey, section) in file.Sections)
        {
            AppendPriorityList(items, history, sectionKey, section.HighPriority, "high", sectionOrder, importedAtUtc);
            AppendPriorityList(items, history, sectionKey, section.MediumPriority, "medium", sectionOrder, importedAtUtc);
            AppendPriorityList(items, history, sectionKey, section.LowPriority, "low", sectionOrder, importedAtUtc);
            sectionOrder++;
        }

        if (file.CodeReviewRemediation?.Phases is { Count: > 0 } phases)
        {
            for (var index = 0; index < phases.Count; index++)
            {
                var phase = phases[index];
                if (phase?.Id is null)
                    continue;
                var entity = new TodoItemEntity
                {
                    Id = phase.Id,
                    Title = phase.Title ?? phase.Phase ?? string.Empty,
                    Section = CodeReviewSectionKey,
                    Priority = "high",
                    Done = phase.Done,
                    Estimate = phase.Estimate,
                    ImplementationTasksJson = SerializeImplementationTasks(phase.ImplementationTasks),
                    ItemKind = CodeReviewPhaseItemKind,
                    SectionOrder = sectionOrder,
                    ItemOrder = index,
                    PhaseLabel = phase.Phase,
                };
                items.Add(entity);
                history.Add(BuildAuditRow(entity, importedAtUtc));
            }
        }

        var metadata = new TodoDocumentMetadataEntity
        {
            SingletonId = 1,
            NotesJson = file.Notes is null ? null : JsonSerializer.Serialize(file.Notes, _json),
            CompletedJson = file.Completed is null ? null : JsonSerializer.Serialize(file.Completed, _json),
            CodeReviewReference = file.CodeReviewRemediation?.Reference,
            LastImportedFromYamlUtc = importedAtUtc,
        };
        return (items, history, metadata);
    }

    private void AppendPriorityList(
        List<TodoItemEntity> items,
        List<TodoAuditHistoryEntity> history,
        string sectionKey,
        List<TodoItem>? source,
        string priority,
        int sectionOrder,
        string importedAtUtc)
    {
        if (source is null)
            return;

        for (var index = 0; index < source.Count; index++)
        {
            var item = source[index];
            if (item?.Id is null)
                continue;
            var entity = new TodoItemEntity
            {
                Id = item.Id,
                Title = item.Title ?? string.Empty,
                Section = sectionKey,
                Priority = priority,
                Done = item.Done,
                Estimate = item.Estimate,
                Note = item.Note,
                DescriptionJson = SerializeStringList(item.Description),
                TechnicalDetailsJson = SerializeStringList(item.TechnicalDetails),
                ImplementationTasksJson = SerializeImplementationTasks(item.ImplementationTasks),
                CompletedDate = item.CompletedDate,
                DoneSummary = item.DoneSummary,
                Remaining = item.Remaining,
                PriorityNote = item.PriorityNote,
                Reference = item.Reference,
                DependsOnJson = SerializeStringList(item.DependsOn),
                FunctionalRequirementsJson = SerializeStringList(item.FunctionalRequirements),
                TechnicalRequirementsJson = SerializeStringList(item.TechnicalRequirements),
                ItemKind = StandardItemKind,
                SectionOrder = sectionOrder,
                ItemOrder = index,
            };
            items.Add(entity);
            history.Add(BuildAuditRow(entity, importedAtUtc));
        }
    }

    private TodoAuditHistoryEntity BuildAuditRow(TodoItemEntity entity, string importedAtUtc)
        => new()
        {
            TodoId = entity.Id,
            Version = 1,
            Action = "imported",
            RecordedAtUtc = importedAtUtc,
            SnapshotJson = JsonSerializer.Serialize(entity, _json),
            PreviousSnapshotJson = null,
            Source = YamlBootstrapSource,
        };

    private string? SerializeStringList(List<string>? value)
        => value is null ? null : JsonSerializer.Serialize(value, _json);

    private string? SerializeImplementationTasks(List<ImplementationTask>? value)
    {
        if (value is null)
            return null;
        var tasks = value
            .Where(static t => t is not null)
            .Select(static t => new { task = t.Task ?? string.Empty, done = t.Done })
            .ToList();
        return JsonSerializer.Serialize(tasks, _json);
    }

    private static async Task WriteMarkerAsync(string markerPath, string reason, CancellationToken cancellationToken)
    {
        var dir = Path.GetDirectoryName(markerPath);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(
            markerPath,
            $"bootstrapped at {DateTime.UtcNow:O}; reason={reason}",
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Outcome of bootstrapping a single workspace.</summary>
    internal enum BootstrapResult
    {
        /// <summary>YAML imported successfully; marker written.</summary>
        Imported,

        /// <summary>Marker file present; no-op.</summary>
        SkippedMarkerPresent,

        /// <summary>Workspace already had rows; marker written, no import.</summary>
        SkippedWorkspacePopulated,

        /// <summary>YAML file absent; no marker, no-op.</summary>
        SkippedMissingYaml,
    }

    /// <summary>Per-workspace bootstrap outcome.</summary>
    internal sealed record BootstrapOutcome(string WorkspacePath, BootstrapResult Result, int ImportedCount);

    /// <summary>Aggregate bootstrap outcome across all configured workspaces.</summary>
    internal sealed record BootstrapSummary(IReadOnlyList<BootstrapOutcome> Outcomes);
}
