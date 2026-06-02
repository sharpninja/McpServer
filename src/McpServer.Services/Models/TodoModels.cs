using System.Diagnostics.CodeAnalysis;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

#pragma warning disable CA1002 // YamlDotNet requires mutable List<T> for deserialization
#pragma warning disable CA2227 // YamlDotNet requires settable collection properties

namespace McpServer.Support.Mcp.Models;

/// <summary>
/// TR-PLANNED-013: Root model for TODO.yaml file.
/// Sections are arbitrary string keys with no semantic meaning to the service;
/// they are informational for agents only.
/// Serialization is handled by <see cref="TodoFileYamlConverter"/>.
/// </summary>
public sealed class TodoFile
{
    /// <summary>Arbitrary-named sections, each containing priority-bucketed item lists.</summary>
    public Dictionary<string, TodoSection> Sections { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Code review remediation section.</summary>
    public CodeReviewSection? CodeReviewRemediation { get; set; }

    /// <summary>Groups of completed items by date.</summary>
    public List<CompletedGroup>? Completed { get; set; }

    /// <summary>Free-form notes.</summary>
    public List<string>? Notes { get; set; }
}

/// <summary>TR-PLANNED-013: A section grouping TODO items by priority level.</summary>
public sealed class TodoSection
{
    /// <summary>High-priority TODO items.</summary>
    [YamlMember(Alias = "high-priority")]
    public List<TodoItem>? HighPriority { get; set; }

    /// <summary>Medium-priority TODO items.</summary>
    [YamlMember(Alias = "medium-priority")]
    public List<TodoItem>? MediumPriority { get; set; }

    /// <summary>Low-priority TODO items.</summary>
    [YamlMember(Alias = "low-priority")]
    public List<TodoItem>? LowPriority { get; set; }
}

/// <summary>TR-PLANNED-013: A single TODO item with metadata and implementation tasks.</summary>
public sealed class TodoItem
{
    /// <summary>Unique identifier for the TODO item (e.g. <c>APP-001</c>).</summary>
    [YamlMember(Alias = "id")]
    public string? Id { get; set; }

    /// <summary>Short title of the TODO item.</summary>
    [YamlMember(Alias = "title")]
    public string? Title { get; set; }

    /// <summary>Time estimate for completing the item.</summary>
    [YamlMember(Alias = "estimate")]
    public string? Estimate { get; set; }

    /// <summary>Optional note or comment on the item.</summary>
    [YamlMember(Alias = "note")]
    public string? Note { get; set; }

    /// <summary>Whether the item is completed.</summary>
    [YamlMember(Alias = "done")]
    public bool Done { get; set; }

    /// <summary>Date the item was completed.</summary>
    [YamlMember(Alias = "completed")]
    public string? CompletedDate { get; set; }

    /// <summary>Multi-line description of the item.</summary>
    [YamlMember(Alias = "description")]
    public List<string>? Description { get; set; }

    /// <summary>Summary of what was done when the item was completed.</summary>
    [YamlMember(Alias = "done-summary")]
    public string? DoneSummary { get; set; }

    /// <summary>Description of remaining work.</summary>
    [YamlMember(Alias = "remaining")]
    public string? Remaining { get; set; }

    /// <summary>Technical details or implementation notes.</summary>
    [YamlMember(Alias = "technical-details")]
    public List<string>? TechnicalDetails { get; set; }

    /// <summary>Note explaining the priority level.</summary>
    [YamlMember(Alias = "priority-note")]
    public string? PriorityNote { get; set; }

    /// <summary>Reference link or identifier (e.g. a GitHub issue).</summary>
    [YamlMember(Alias = "reference")]
    public string? Reference { get; set; }

    /// <summary>IDs of TODO items this item depends on.</summary>
    [YamlMember(Alias = "depends-on")]
    public List<string>? DependsOn { get; set; }

    /// <summary>Associated functional requirement IDs (e.g. FR-LOC-001).</summary>
    [YamlMember(Alias = "functional-requirements")]
    public List<string>? FunctionalRequirements { get; set; }

    /// <summary>Associated technical requirement IDs (e.g. TR-LOC-001).</summary>
    [YamlMember(Alias = "technical-requirements")]
    public List<string>? TechnicalRequirements { get; set; }

    /// <summary>Sub-tasks for implementing this item.</summary>
    [YamlMember(Alias = "implementation-tasks")]
    public List<ImplementationTask>? ImplementationTasks { get; set; }
}

/// <summary>
/// Legacy flat TODO item shape used by older workspace-local <c>docs/todo.yaml</c> files.
/// This model exists only to support backward-compatible deserialization into <see cref="TodoFile"/>.
/// </summary>
public sealed class LegacyTodoFlatItem
{
    /// <summary>Unique identifier for the TODO item.</summary>
    [YamlMember(Alias = "id")]
    public string? Id { get; set; }

    /// <summary>Short title of the TODO item.</summary>
    [YamlMember(Alias = "title")]
    public string? Title { get; set; }

    /// <summary>Legacy section key.</summary>
    [YamlMember(Alias = "section")]
    public string? Section { get; set; }

    /// <summary>Legacy priority value.</summary>
    [YamlMember(Alias = "priority")]
    public string? Priority { get; set; }

    /// <summary>Legacy effort estimate.</summary>
    [YamlMember(Alias = "estimate")]
    public string? Estimate { get; set; }

    /// <summary>Legacy workflow status text.</summary>
    [YamlMember(Alias = "status")]
    public string? Status { get; set; }

    /// <summary>Legacy phase label.</summary>
    [YamlMember(Alias = "phase")]
    public string? Phase { get; set; }

    /// <summary>Legacy multiline description.</summary>
    [YamlMember(Alias = "description")]
    public List<string>? Description { get; set; }

    /// <summary>Legacy note field.</summary>
    [YamlMember(Alias = "note")]
    public string? Note { get; set; }

    /// <summary>Legacy remaining-work field.</summary>
    [YamlMember(Alias = "remaining")]
    public string? Remaining { get; set; }

    /// <summary>Legacy technical details field.</summary>
    [YamlMember(Alias = "technicalDetails", ApplyNamingConventions = false)]
    public List<string>? TechnicalDetails { get; set; }

    /// <summary>Legacy functional requirements field.</summary>
    [YamlMember(Alias = "functionalRequirements", ApplyNamingConventions = false)]
    public List<string>? FunctionalRequirements { get; set; }

    /// <summary>Legacy technical requirements field.</summary>
    [YamlMember(Alias = "technicalRequirements", ApplyNamingConventions = false)]
    public List<string>? TechnicalRequirements { get; set; }

    /// <summary>Legacy dependency field.</summary>
    [YamlMember(Alias = "dependsOn", ApplyNamingConventions = false)]
    public List<string>? DependsOn { get; set; }

    /// <summary>Legacy implementation tasks field.</summary>
    [YamlMember(Alias = "implementationTasks", ApplyNamingConventions = false)]
    public List<ImplementationTask>? ImplementationTasks { get; set; }

    /// <summary>Legacy completion date field.</summary>
    [YamlMember(Alias = "completedDate", ApplyNamingConventions = false)]
    public string? CompletedDate { get; set; }

    /// <summary>Legacy done summary field.</summary>
    [YamlMember(Alias = "doneSummary", ApplyNamingConventions = false)]
    public string? DoneSummary { get; set; }

    /// <summary>Legacy reference field.</summary>
    [YamlMember(Alias = "reference")]
    public string? Reference { get; set; }
}

/// <summary>TR-PLANNED-013: A sub-task within a TODO item.</summary>
public sealed class ImplementationTask
{
    /// <summary>Description of the implementation task.</summary>
    [YamlMember(Alias = "task")]
    public string? Task { get; set; }

    /// <summary>Whether the sub-task is completed.</summary>
    [YamlMember(Alias = "done")]
    public bool Done { get; set; }
}

/// <summary>TR-PLANNED-013: Code review remediation section with phases.</summary>
public sealed class CodeReviewSection
{
    /// <summary>Reference link for the code review.</summary>
    [YamlMember(Alias = "reference")]
    public string? Reference { get; set; }

    /// <summary>Remediation phases for the code review.</summary>
    [YamlMember(Alias = "phases")]
    public List<CodeReviewPhase>? Phases { get; set; }
}

/// <summary>TR-PLANNED-013: A code review remediation phase.</summary>
public sealed class CodeReviewPhase
{
    /// <summary>Unique identifier for the phase.</summary>
    [YamlMember(Alias = "id")]
    public string? Id { get; set; }

    /// <summary>Phase number or label.</summary>
    [YamlMember(Alias = "phase")]
    public string? Phase { get; set; }

    /// <summary>Time estimate for the phase.</summary>
    [YamlMember(Alias = "estimate")]
    public string? Estimate { get; set; }

    /// <summary>Whether the phase is completed.</summary>
    [YamlMember(Alias = "done")]
    public bool Done { get; set; }

    /// <summary>Title of the remediation phase.</summary>
    [YamlMember(Alias = "title")]
    public string? Title { get; set; }

    /// <summary>Sub-tasks for implementing this phase.</summary>
    [YamlMember(Alias = "implementation-tasks")]
    public List<ImplementationTask>? ImplementationTasks { get; set; }
}

/// <summary>TR-PLANNED-013: A group of completed items by date.</summary>
public sealed class CompletedGroup
{
    /// <summary>Completion date for this group.</summary>
    [YamlMember(Alias = "date")]
    public string? Date { get; set; }

    /// <summary>Items completed on this date.</summary>
    [YamlMember(Alias = "items")]
    public List<CompletedItem>? Items { get; set; }
}

/// <summary>TR-PLANNED-013: A completed item summary entry.</summary>
public sealed class CompletedItem
{
    /// <summary>Unique identifier for the completed item.</summary>
    [YamlMember(Alias = "id")]
    public string? Id { get; set; }

    /// <summary>Qualifier or category for the completed item.</summary>
    [YamlMember(Alias = "qualifier")]
    public string? Qualifier { get; set; }

    /// <summary>Summary of what was accomplished.</summary>
    [YamlMember(Alias = "summary")]
    public string? Summary { get; set; }
}

/// <summary>
/// Tolerates list-of-string TODO fields that contain mapping-shaped YAML entries,
/// such as unquoted sequence items with colons.
/// </summary>
internal sealed class TodoStringListYamlConverter : IYamlTypeConverter
{
    /// <inheritdoc />
    public bool Accepts(Type type) => type == typeof(List<string>);

    /// <inheritdoc />
    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        ArgumentNullException.ThrowIfNull(parser);

        var values = new List<string>();
        if (parser.TryConsume<SequenceStart>(out _))
        {
            while (!parser.TryConsume<SequenceEnd>(out _))
            {
                // ISS-TODO-001 (FR-MCP-108): a scalar list entry is one Markdown line and must be
                // preserved verbatim - including blank lines, indentation, and trailing whitespace -
                // so formatted descriptions survive the YAML projection/import round-trip. Only the
                // structured legacy forms (nested sequence/mapping) are collapsed and whitespace-filtered.
                if (parser.Current is Scalar)
                {
                    parser.TryConsume<Scalar>(out var scalar);
                    values.Add(scalar?.Value ?? string.Empty);
                    continue;
                }

                var value = ReadNodeAsText(parser);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    values.Add(value);
                }
            }

            return values;
        }

        if (parser.Current is Scalar)
        {
            parser.TryConsume<Scalar>(out var singleScalar);
            values.Add(singleScalar?.Value ?? string.Empty);
            return values;
        }

        var singleValue = ReadNodeAsText(parser);
        if (!string.IsNullOrWhiteSpace(singleValue))
        {
            values.Add(singleValue);
        }

        return values;
    }

    /// <inheritdoc />
    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        ArgumentNullException.ThrowIfNull(emitter);
        ArgumentNullException.ThrowIfNull(serializer);

        var values = (List<string>?)value ?? [];
        emitter.Emit(new SequenceStart(null, null, false, SequenceStyle.Block));
        foreach (var item in values)
        {
            var text = item ?? string.Empty;

            // ISS-TODO-001 (FR-MCP-108): plain YAML scalars drop trailing whitespace and cannot
            // represent an empty or leading-whitespace line. Force a double-quoted scalar whenever
            // the line is empty or its edges carry whitespace so blank lines, indentation, and
            // trailing content are preserved exactly through the projection. Normal lines stay plain.
            if (text.Length == 0 || char.IsWhiteSpace(text[0]) || char.IsWhiteSpace(text[^1]))
            {
                emitter.Emit(new Scalar(
                    anchor: null,
                    tag: null,
                    value: text,
                    style: ScalarStyle.DoubleQuoted,
                    isPlainImplicit: false,
                    isQuotedImplicit: true));
                continue;
            }

            serializer(text, typeof(string));
        }

        emitter.Emit(new SequenceEnd());
    }

    private static string ReadNodeAsText(IParser parser)
    {
        if (parser.TryConsume<Scalar>(out var scalar))
        {
            return scalar.Value ?? string.Empty;
        }

        if (parser.TryConsume<SequenceStart>(out _))
        {
            var values = new List<string>();
            while (!parser.TryConsume<SequenceEnd>(out _))
            {
                var value = ReadNodeAsText(parser);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    values.Add(value);
                }
            }

            return string.Join("; ", values);
        }

        if (parser.TryConsume<MappingStart>(out _))
        {
            var pairs = new List<string>();
            while (!parser.TryConsume<MappingEnd>(out _))
            {
                var key = ReadNodeAsText(parser).Trim();
                var value = ReadNodeAsText(parser).Trim();
                if (string.IsNullOrWhiteSpace(key) && string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                pairs.Add(string.IsNullOrWhiteSpace(value) ? key : $"{key}: {value}");
            }

            return string.Join("; ", pairs);
        }

        throw new YamlException($"Unexpected YAML event '{parser.Current?.GetType().Name ?? "null"}' while parsing a TODO string list.");
    }
}

/// <summary>
/// YamlDotNet type converter for <see cref="TodoFile"/>.
/// Arbitrary top-level keys become entries in <see cref="TodoFile.Sections"/>.
/// Reserved keys: <c>code-review-remediation</c>, <c>completed</c>, <c>notes</c>.
/// </summary>
internal sealed class TodoFileYamlConverter : IYamlTypeConverter
{
    /// <inheritdoc />
    public bool Accepts(Type type) => type == typeof(TodoFile);

    /// <inheritdoc />
    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        var file = new TodoFile();
        parser.Consume<MappingStart>();
        while (!parser.TryConsume<MappingEnd>(out _))
        {
            var key = parser.Consume<Scalar>().Value;
            if (string.Equals(key, "code-review-remediation", StringComparison.OrdinalIgnoreCase))
                file.CodeReviewRemediation = (CodeReviewSection?)rootDeserializer(typeof(CodeReviewSection));
            else if (string.Equals(key, "completed", StringComparison.OrdinalIgnoreCase))
                file.Completed = (List<CompletedGroup>?)rootDeserializer(typeof(List<CompletedGroup>));
            else if (string.Equals(key, "notes", StringComparison.OrdinalIgnoreCase))
                file.Notes = (List<string>?)rootDeserializer(typeof(List<string>));
            else if (string.Equals(key, "todos", StringComparison.OrdinalIgnoreCase))
                ImportLegacyFlatTodos(file, (List<LegacyTodoFlatItem>?)rootDeserializer(typeof(List<LegacyTodoFlatItem>)));
            else
                file.Sections[key] = (TodoSection?)rootDeserializer(typeof(TodoSection)) ?? new TodoSection();
        }
        return file;
    }

    /// <inheritdoc />
    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        var file = (TodoFile)(value ?? new TodoFile());
        emitter.Emit(new MappingStart(null, null, false, MappingStyle.Block));
        foreach (var (key, section) in file.Sections)
        {
            emitter.Emit(new Scalar(key));
            serializer(section, typeof(TodoSection));
        }
        if (file.CodeReviewRemediation is not null)
        {
            emitter.Emit(new Scalar("code-review-remediation"));
            serializer(file.CodeReviewRemediation, typeof(CodeReviewSection));
        }
        if (file.Completed is not null)
        {
            emitter.Emit(new Scalar("completed"));
            serializer(file.Completed, typeof(List<CompletedGroup>));
        }
        if (file.Notes is not null)
        {
            emitter.Emit(new Scalar("notes"));
            serializer(file.Notes, typeof(List<string>));
        }
        emitter.Emit(new MappingEnd());
    }

    private static void ImportLegacyFlatTodos(TodoFile file, List<LegacyTodoFlatItem>? legacyItems)
    {
        if (legacyItems is null)
            return;

        foreach (var legacyItem in legacyItems)
        {
            if (legacyItem is null || string.IsNullOrWhiteSpace(legacyItem.Id))
                continue;

            var sectionKey = string.IsNullOrWhiteSpace(legacyItem.Section) ? "general" : legacyItem.Section.Trim();
            var priorityKey = NormalizeLegacyPriority(legacyItem.Priority);
            if (!file.Sections.TryGetValue(sectionKey, out var section))
            {
                section = new TodoSection();
                file.Sections[sectionKey] = section;
            }

            var targetList = priorityKey switch
            {
                "high" => section.HighPriority ??= [],
                "low" => section.LowPriority ??= [],
                _ => section.MediumPriority ??= [],
            };

            targetList.Add(new TodoItem
            {
                Id = legacyItem.Id,
                Title = legacyItem.Title,
                Estimate = legacyItem.Estimate,
                Note = legacyItem.Note,
                Done = IsLegacyItemDone(legacyItem),
                CompletedDate = legacyItem.CompletedDate,
                Description = legacyItem.Description,
                DoneSummary = legacyItem.DoneSummary,
                Remaining = legacyItem.Remaining,
                TechnicalDetails = legacyItem.TechnicalDetails,
                Reference = legacyItem.Reference,
                DependsOn = legacyItem.DependsOn,
                FunctionalRequirements = legacyItem.FunctionalRequirements,
                TechnicalRequirements = legacyItem.TechnicalRequirements,
                ImplementationTasks = legacyItem.ImplementationTasks,
            });
        }
    }

    private static string NormalizeLegacyPriority(string? priority)
    {
        var normalized = priority?.Trim();
        if (string.Equals(normalized, "high", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalized, "critical", StringComparison.OrdinalIgnoreCase))
            return "high";
        if (string.Equals(normalized, "low", StringComparison.OrdinalIgnoreCase))
            return "low";
        return "medium";
    }

    private static bool IsLegacyItemDone(LegacyTodoFlatItem item)
    {
        if (!string.IsNullOrWhiteSpace(item.CompletedDate))
            return true;

        var status = item.Status?.Trim();
        return string.Equals(status, "done", StringComparison.OrdinalIgnoreCase)
               || string.Equals(status, "completed", StringComparison.OrdinalIgnoreCase)
               || string.Equals(status, "closed", StringComparison.OrdinalIgnoreCase);
    }
}
