namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Shared validation rules for TODO item fields.
/// Both YAML and SQLite backends use this as the single source of truth.
/// </summary>
internal static class TodoValidator
{
    private static readonly HashSet<string> s_validPriorities =
        new(StringComparer.OrdinalIgnoreCase) { "high", "medium", "low" };

    /// <summary>Returns <see langword="true"/> when <paramref name="priority"/> is high, medium, or low.</summary>
    public static bool IsValidPriority(string? priority)
        => !string.IsNullOrWhiteSpace(priority) && s_validPriorities.Contains(priority);

    /// <summary>Returns an error message if priority is invalid, otherwise <see langword="null"/>.</summary>
    public static string? ValidatePriority(string? priority)
        => IsValidPriority(priority) ? null : "Unknown priority. Use high, medium, or low.";

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
}
