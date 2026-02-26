using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Models;
using Microsoft.Extensions.Options;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-PLANNED-013: Reads/writes TODO items from docs/Project/TODO.yaml.
/// Provides flat search by keyword, priority, id and full CRUD.
/// </summary>
internal sealed class TodoService : ITodoService, ITodoStore, IDisposable
{
    private const string DefaultTodoRelativePath = "docs/Project/TODO.yaml";

    private readonly string _todoFilePath;
    private readonly string _todoAuditPath;
    private readonly IWriteAuditLog _auditLog;
    private readonly ILogger<TodoService> _logger;
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(HyphenatedNamingConvention.Instance)
        .WithTypeConverter(new TodoFileYamlConverter())
        .IgnoreUnmatchedProperties()
        .Build();

    private static readonly ISerializer Serializer = new SerializerBuilder()
        .WithNamingConvention(HyphenatedNamingConvention.Instance)
        .WithTypeConverter(new TodoFileYamlConverter())
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
        .Build();

    /// <summary>TR-PLANNED-013: Constructor.</summary>
    public TodoService(IOptions<IngestionOptions> options, IWriteAuditLog auditLog, ILogger<TodoService> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _auditLog = auditLog ?? throw new ArgumentNullException(nameof(auditLog));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        var repoRoot = options.Value.RepoRoot ?? ".";
        var todoPath = string.IsNullOrWhiteSpace(options.Value.TodoFilePath) ? DefaultTodoRelativePath : options.Value.TodoFilePath;
        _todoFilePath = Path.GetFullPath(Path.IsPathRooted(todoPath) ? todoPath : Path.Combine(repoRoot, todoPath));
        _todoAuditPath = todoPath;
    }

    /// <summary>TR-PLANNED-013: Constructor accepting explicit file path (for testing).</summary>
    internal TodoService(string todoFilePath, IWriteAuditLog auditLog, ILogger<TodoService> logger)
    {
        _todoFilePath = todoFilePath ?? throw new ArgumentNullException(nameof(todoFilePath));
        _todoAuditPath = todoFilePath;
        _auditLog = auditLog ?? throw new ArgumentNullException(nameof(auditLog));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public void Dispose() => _fileLock.Dispose();

    /// <inheritdoc />
    public async Task<TodoQueryResult> QueryAsync(TodoQueryRequest request, CancellationToken cancellationToken = default)
    {
        var file = await ReadFileAsync(cancellationToken).ConfigureAwait(false);
        if (file is null)
            return new TodoQueryResult([], 0);

        var allItems = FlattenAll(file);
        var filtered = ApplyFilters(allItems, request);
        return new TodoQueryResult(filtered, filtered.Count);
    }

    /// <inheritdoc />
    public async Task<TodoFlatItem?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(id);
        var file = await ReadFileAsync(cancellationToken).ConfigureAwait(false);
        if (file is null) return null;
        return FlattenAll(file).Find(i => string.Equals(i.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public async Task<TodoMutationResult> CreateAsync(TodoCreateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await _fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var file = await ReadFileAsync(cancellationToken).ConfigureAwait(false) ?? new TodoFile();

            // Check for duplicate id
            var existing = FlattenAll(file).Find(i => string.Equals(i.Id, request.Id, StringComparison.OrdinalIgnoreCase));
            if (existing is not null)
                return new TodoMutationResult(false, $"Item with id '{request.Id}' already exists.");

            var priorityError = TodoValidator.ValidatePriority(request.Priority);
            if (priorityError is not null)
                return new TodoMutationResult(false, priorityError);

            var section = GetOrCreateSection(file, request.Section);
            var list = GetPriorityList(section, request.Priority)!;

            var item = new TodoItem
            {
                Id = request.Id,
                Title = request.Title,
                Done = false,
                Estimate = request.Estimate,
                Description = request.Description?.ToList(),
                TechnicalDetails = request.TechnicalDetails?.ToList(),
                Note = request.Note,
                Remaining = request.Remaining,
                DependsOn = request.DependsOn?.ToList(),
                FunctionalRequirements = request.FunctionalRequirements?.ToList(),
                TechnicalRequirements = request.TechnicalRequirements?.ToList(),
                ImplementationTasks = request.ImplementationTasks?.Select(t => new ImplementationTask { Task = t.Task, Done = t.Done }).ToList()
            };

            // Validate dependencies exist and no circular dependency would result
            if (item.DependsOn is { Count: > 0 })
            {
                var allItems = FlattenAll(file);
                var depError = TodoValidator.ValidateDependencies(request.Id, item.DependsOn, allItems);
                if (depError is not null)
                    return new TodoMutationResult(false, depError);
            }

            list.Add(item);
            await WriteFileAsync(file, cancellationToken).ConfigureAwait(false);
            _auditLog.RecordWrite(_todoAuditPath, DateTime.UtcNow);
            _logger.LogInformation("Created TODO item {Id} in {Section}/{Priority}", request.Id, request.Section, request.Priority);

            var flat = ToFlat(item, request.Section, request.Priority);
            return new TodoMutationResult(true, Item: flat);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<TodoMutationResult> UpdateAsync(string id, TodoUpdateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(request);

        await _fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var file = await ReadFileAsync(cancellationToken).ConfigureAwait(false);
            if (file is null)
                return new TodoMutationResult(false, "TODO file not found.");

            var (item, section, priority) = FindItemInFile(file, id);
            if (item is null)
                return new TodoMutationResult(false, $"Item with id '{id}' not found.");

            if (request.Title is not null) item.Title = request.Title;
            if (request.Done.HasValue) item.Done = request.Done.Value;
            if (request.Estimate is not null) item.Estimate = request.Estimate;
            if (request.Description is not null) item.Description = request.Description.ToList();
            if (request.TechnicalDetails is not null) item.TechnicalDetails = request.TechnicalDetails.ToList();
            if (request.Note is not null) item.Note = request.Note;
            if (request.CompletedDate is not null) item.CompletedDate = request.CompletedDate;
            if (request.DoneSummary is not null) item.DoneSummary = request.DoneSummary;
            if (request.Remaining is not null) item.Remaining = request.Remaining;
            if (request.ImplementationTasks is not null)
            {
                item.ImplementationTasks = request.ImplementationTasks
                    .Select(t => new ImplementationTask { Task = t.Task, Done = t.Done })
                    .ToList();
            }
            if (request.DependsOn is not null)
            {
                var allItems = FlattenAll(file);
                var proposedDeps = request.DependsOn.ToList();
                var depError = TodoValidator.ValidateDependencies(id, proposedDeps, allItems);
                if (depError is not null)
                    return new TodoMutationResult(false, depError);
                item.DependsOn = proposedDeps;
            }
            if (request.FunctionalRequirements is not null)
                item.FunctionalRequirements = request.FunctionalRequirements.ToList();
            if (request.TechnicalRequirements is not null)
                item.TechnicalRequirements = request.TechnicalRequirements.ToList();

            // Handle priority change: move item between priority lists
            var newPriority = priority!;
            if (request.Priority is not null && !string.Equals(request.Priority, priority, StringComparison.OrdinalIgnoreCase))
            {
                var sectionObj = GetOrCreateSection(file, section!);
                RemoveFromPriorityList(sectionObj, priority!, id);
                AddToPriorityList(sectionObj, request.Priority, item);
                newPriority = request.Priority;
            }

            // Handle section change: move item between sections
            if (request.Section is not null && !string.Equals(request.Section, section, StringComparison.OrdinalIgnoreCase))
            {
                var oldSection = GetOrCreateSection(file, section!);
                var newSection = GetOrCreateSection(file, request.Section);
                RemoveFromPriorityList(oldSection, newPriority, id);
                AddToPriorityList(newSection, newPriority, item);
                section = request.Section;
            }

            await WriteFileAsync(file, cancellationToken).ConfigureAwait(false);
            _auditLog.RecordWrite(_todoAuditPath, DateTime.UtcNow);
            _logger.LogInformation("Updated TODO item {Id}", id);

            var flat = ToFlat(item, section!, newPriority);
            return new TodoMutationResult(true, Item: flat);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<TodoMutationResult> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(id);

        await _fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var file = await ReadFileAsync(cancellationToken).ConfigureAwait(false);
            if (file is null)
                return new TodoMutationResult(false, "TODO file not found.");

            var removed = RemoveItemFromFile(file, id);
            if (!removed)
                return new TodoMutationResult(false, $"Item with id '{id}' not found.");

            await WriteFileAsync(file, cancellationToken).ConfigureAwait(false);
            _auditLog.RecordWrite(_todoAuditPath, DateTime.UtcNow);
            _logger.LogInformation("Deleted TODO item {Id}", id);

            return new TodoMutationResult(true);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    private async Task<TodoFile?> ReadFileAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(_todoFilePath))
            {
                _logger.LogWarning("TODO file not found at {Path}", _todoFilePath);
                return null;
            }

            var yaml = await File.ReadAllTextAsync(_todoFilePath, cancellationToken).ConfigureAwait(false);
            return Deserializer.Deserialize<TodoFile>(yaml);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read/deserialize TODO file at {Path}", _todoFilePath);
            return null;
        }
    }

    private async Task WriteFileAsync(TodoFile file, CancellationToken cancellationToken)
    {
        var yaml = Serializer.Serialize(file);
        var dir = Path.GetDirectoryName(_todoFilePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(_todoFilePath, yaml, cancellationToken).ConfigureAwait(false);
    }

    private static List<TodoFlatItem> FlattenAll(TodoFile file)
    {
        var result = new List<TodoFlatItem>();
        foreach (var (key, section) in file.Sections)
            FlattenSection(result, section, key);
        FlattenCodeReview(result, file.CodeReviewRemediation);
        return result;
    }

    private static void FlattenSection(List<TodoFlatItem> result, TodoSection? section, string sectionKey)
    {
        if (section is null) return;
        FlattenList(result, section.HighPriority, sectionKey, "high");
        FlattenList(result, section.MediumPriority, sectionKey, "medium");
        FlattenList(result, section.LowPriority, sectionKey, "low");
    }

    private static void FlattenList(List<TodoFlatItem> result, List<TodoItem>? items, string section, string priority)
    {
        if (items is null) return;
        foreach (var item in items)
        {
            if (item is null || item.Id is null) continue;
            result.Add(ToFlat(item, section, priority));
        }
    }

    private static void FlattenCodeReview(List<TodoFlatItem> result, CodeReviewSection? section)
    {
        if (section?.Phases is null) return;
        foreach (var phase in section.Phases)
        {
            if (phase is null || phase.Id is null) continue;
            result.Add(new TodoFlatItem
            {
                Id = phase.Id,
                Title = phase.Title ?? phase.Phase ?? "",
                Section = "code-review-remediation",
                Priority = "high",
                Done = phase.Done,
                Estimate = phase.Estimate,
                ImplementationTasks = phase.ImplementationTasks?
                    .Where(t => t is not null)
                    .Select(t => new TodoFlatTask(t.Task ?? "", t.Done))
                    .ToList()
            });
        }
    }

    private static TodoFlatItem ToFlat(TodoItem item, string section, string priority) => new()
    {
        Id = item.Id ?? "",
        Title = item.Title ?? "",
        Section = section,
        Priority = priority,
        Done = item.Done,
        Estimate = item.Estimate,
        Note = item.Note,
        Description = item.Description,
        TechnicalDetails = item.TechnicalDetails,
        CompletedDate = item.CompletedDate,
        DoneSummary = item.DoneSummary,
        Remaining = item.Remaining,
        PriorityNote = item.PriorityNote,
        Reference = item.Reference,
        DependsOn = item.DependsOn,
        FunctionalRequirements = item.FunctionalRequirements,
        TechnicalRequirements = item.TechnicalRequirements,
        ImplementationTasks = item.ImplementationTasks?
            .Where(t => t is not null)
            .Select(t => new TodoFlatTask(t.Task ?? "", t.Done))
            .ToList()
    };

    private static List<TodoFlatItem> ApplyFilters(List<TodoFlatItem> items, TodoQueryRequest request)
    {
        IEnumerable<TodoFlatItem> filtered = items;

        if (!string.IsNullOrWhiteSpace(request.Id))
            filtered = filtered.Where(i => string.Equals(i.Id, request.Id, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(request.Section))
            filtered = filtered.Where(i => string.Equals(i.Section, request.Section, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(request.Priority))
            filtered = filtered.Where(i => string.Equals(i.Priority, request.Priority, StringComparison.OrdinalIgnoreCase));

        if (request.Done.HasValue)
            filtered = filtered.Where(i => i.Done == request.Done.Value);

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var kw = request.Keyword;
            filtered = filtered.Where(i => MatchesKeyword(i, kw));
        }

        return filtered.ToList();
    }

    private static bool MatchesKeyword(TodoFlatItem item, string keyword)
    {
        if (item.Id.Contains(keyword, StringComparison.OrdinalIgnoreCase)) return true;
        if (item.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase)) return true;
        if (item.Description?.Any(d => d.Contains(keyword, StringComparison.OrdinalIgnoreCase)) == true) return true;
        if (item.TechnicalDetails?.Any(d => d.Contains(keyword, StringComparison.OrdinalIgnoreCase)) == true) return true;
        if (item.Note?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true) return true;
        if (item.DoneSummary?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true) return true;
        if (item.Remaining?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true) return true;
        if (item.ImplementationTasks?.Any(t => t.Task.Contains(keyword, StringComparison.OrdinalIgnoreCase)) == true) return true;
        return false;
    }

    private static (TodoItem? Item, string? Section, string? Priority) FindItemInFile(TodoFile file, string id)
    {
        foreach (var (key, section) in file.Sections)
        {
            var priorities = new (List<TodoItem>? List, string Name)[]
            {
                (section.HighPriority, "high"),
                (section.MediumPriority, "medium"),
                (section.LowPriority, "low"),
            };
            foreach (var (list, pName) in priorities)
            {
                var found = list?.FirstOrDefault(i => i is not null && string.Equals(i.Id, id, StringComparison.OrdinalIgnoreCase));
                if (found is not null)
                    return (found, key, pName);
            }
        }

        // Check code review phases
        if (file.CodeReviewRemediation?.Phases is not null)
        {
            var phase = file.CodeReviewRemediation.Phases.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));
            if (phase is not null)
            {
                var synth = new TodoItem
                {
                    Id = phase.Id,
                    Title = phase.Title,
                    Done = phase.Done,
                    Estimate = phase.Estimate,
                    ImplementationTasks = phase.ImplementationTasks
                };
                return (synth, "code-review-remediation", "high");
            }
        }

        return (null, null, null);
    }

    private static bool RemoveItemFromFile(TodoFile file, string id)
    {
        foreach (var section in file.Sections.Values)
        {
            var lists = new[] { section.HighPriority, section.MediumPriority, section.LowPriority };
            foreach (var list in lists)
            {
                var item = list?.FirstOrDefault(i => i is not null && string.Equals(i.Id, id, StringComparison.OrdinalIgnoreCase));
                if (item is not null)
                {
                    list!.Remove(item);
                    return true;
                }
            }
        }

        if (file.CodeReviewRemediation?.Phases is not null)
        {
            var phase = file.CodeReviewRemediation.Phases.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));
            if (phase is not null)
            {
                file.CodeReviewRemediation.Phases.Remove(phase);
                return true;
            }
        }

        return false;
    }

    private static TodoSection GetOrCreateSection(TodoFile file, string sectionKey)
    {
        if (!file.Sections.TryGetValue(sectionKey, out var section))
        {
            section = new TodoSection();
            file.Sections[sectionKey] = section;
        }
        return section;
    }

    private static List<TodoItem>? GetPriorityList(TodoSection section, string priority)
    {
        if (string.Equals(priority, "high", StringComparison.OrdinalIgnoreCase)) return section.HighPriority ??= [];
        if (string.Equals(priority, "medium", StringComparison.OrdinalIgnoreCase)) return section.MediumPriority ??= [];
        if (string.Equals(priority, "low", StringComparison.OrdinalIgnoreCase)) return section.LowPriority ??= [];
        return null;
    }

    private static void RemoveFromPriorityList(TodoSection section, string priority, string id)
    {
        var list = GetPriorityList(section, priority);
        var item = list?.FirstOrDefault(i => i is not null && string.Equals(i.Id, id, StringComparison.OrdinalIgnoreCase));
        if (item is not null) list!.Remove(item);
    }

    private static void AddToPriorityList(TodoSection section, string priority, TodoItem item)
    {
        var list = GetPriorityList(section, priority);
        list?.Add(item);
    }
}
