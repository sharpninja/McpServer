using System.Text.RegularExpressions;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Shared validation rules for TODO item fields.
/// Both YAML and SQLite backends use this as the single source of truth.
/// </summary>
internal static class TodoValidator
{
    private const string ThreeSegmentTodoIdPattern = "^[A-Z]+-[A-Z0-9]+-\\d{3}$";
    private const string IssueTodoIdPattern = "^ISSUE-\\d+$";

    private static readonly Regex s_threeSegmentTodoIdRegex = new(
        ThreeSegmentTodoIdPattern,
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex s_issueTodoIdRegex = new(
        IssueTodoIdPattern,
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly HashSet<string> s_validPriorities =
        new(StringComparer.OrdinalIgnoreCase) { "high", "medium", "low" };

    /// <summary>Returns <see langword="true"/> when <paramref name="priority"/> is high, medium, or low.</summary>
    public static bool IsValidPriority(string? priority)
        => !string.IsNullOrWhiteSpace(priority) && s_validPriorities.Contains(priority);

    /// <summary>Returns an error message if priority is invalid, otherwise <see langword="null"/>.</summary>
    public static string? ValidatePriority(string? priority)
        => IsValidPriority(priority) ? null : "Unknown priority. Use high, medium, or low.";

    /// <summary>
    /// Returns an error message when the TODO identifier is null, empty, or does not match
    /// the canonical format <c>&lt;PHASE&gt;-&lt;AREA&gt;-###</c> or <c>ISSUE-{number}</c>.
    /// </summary>
    public static string? ValidateTodoId(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return "Todo id is required.";

        if (!IsCanonicalTodoId(id))
            return $"Todo id must match <PHASE>-<AREA>-### using uppercase kebab-case (regex: {ThreeSegmentTodoIdPattern}) or ISSUE-{{number}} (regex: {IssueTodoIdPattern}).";

        return null;
    }

    /// <summary>
    /// Validates an enumerable of TODO identifiers. Returns the first error found or <see langword="null"/>.
    /// </summary>
    public static string? ValidateTodoIds(IEnumerable<string>? ids, string fieldName)
    {
        if (ids is null)
            return null;

        foreach (var id in ids)
        {
            var error = ValidateTodoId(id);
            if (error is not null)
                return $"{fieldName} contains invalid TODO id '{id}'. {error}";
        }

        return null;
    }

    /// <summary>
    /// Validates dependency identifiers with backward compatibility:
    /// canonical IDs are always valid; legacy IDs are allowed only when they
    /// already exist in the current TODO set.
    /// </summary>
    public static string? ValidateDependencyIds(IEnumerable<string>? ids, IReadOnlyList<TodoFlatItem> allItems, string fieldName)
    {
        if (ids is null)
            return null;

        var knownIds = new HashSet<string>(allItems.Select(i => i.Id), StringComparer.OrdinalIgnoreCase);
        foreach (var id in ids)
        {
            if (ValidateTodoId(id) is null)
                continue;

            if (knownIds.Contains(id))
                continue;

            return $"{fieldName} contains invalid TODO id '{id}'. Todo id must match <PHASE>-<AREA>-### using uppercase kebab-case (regex: {ThreeSegmentTodoIdPattern}) or ISSUE-{{number}} (regex: {IssueTodoIdPattern}).";
        }

        return null;
    }

    /// <summary>
    /// Validates that proposed dependencies are not self-referential, all exist, and introduce no cycles.
    /// Returns an error message on failure, <see langword="null"/> on success.
    /// </summary>
    public static string? ValidateDependencies(string itemId, List<string> dependsOn, List<TodoFlatItem> allItems)
    {
        if (dependsOn.Any(d => string.Equals(d, itemId, StringComparison.OrdinalIgnoreCase)))
            return $"Item '{itemId}' cannot depend on itself.";

        var knownIds = new HashSet<string>(allItems.Select(i => i.Id), StringComparer.OrdinalIgnoreCase);
        foreach (var depId in dependsOn)
        {
            if (!knownIds.Contains(depId) && !string.Equals(depId, itemId, StringComparison.OrdinalIgnoreCase))
                return $"Dependency '{depId}' does not exist.";
        }

        var graph = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in allItems)
        {
            var deps = string.Equals(item.Id, itemId, StringComparison.OrdinalIgnoreCase)
                ? dependsOn
                : item.DependsOn?.ToList() ?? [];
            graph[item.Id] = deps;
        }

        if (!graph.ContainsKey(itemId))
            graph[itemId] = dependsOn;

        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var inStack = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (HasCycle(itemId, graph, visited, inStack))
            return $"Circular dependency detected involving '{itemId}'.";

        return null;
    }

    private static bool HasCycle(
        string node,
        Dictionary<string, List<string>> graph,
        HashSet<string> visited,
        HashSet<string> inStack)
    {
        if (inStack.Contains(node))
            return true;
        if (visited.Contains(node))
            return false;

        visited.Add(node);
        inStack.Add(node);

        if (graph.TryGetValue(node, out var deps))
        {
            foreach (var dep in deps)
            {
                if (HasCycle(dep, graph, visited, inStack))
                    return true;
            }
        }

        inStack.Remove(node);
        return false;
    }

    private static bool IsCanonicalTodoId(string id)
        => s_threeSegmentTodoIdRegex.IsMatch(id) || s_issueTodoIdRegex.IsMatch(id);
}
