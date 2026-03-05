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
}
