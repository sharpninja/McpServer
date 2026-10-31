using System.Text.RegularExpressions;

/// <summary>
/// TR-MCP-NUKE-019 AC-2: Production <see cref="IIntegrationLaneClassFilter"/>. The host filter alone for a project the
/// lane runs whole; otherwise the host filter and, in parentheses ('&amp;' binds tighter than '|'), one
/// <c>FullyQualifiedName~.Class.</c> match per class, bounded by dots so it selects that class only.
/// </summary>
sealed class IntegrationLaneClassFilter : IIntegrationLaneClassFilter
{
    static readonly Regex PlainIdentifier = new("^[A-Za-z_][A-Za-z0-9_]*\\z", RegexOptions.CultureInvariant);

    /// <inheritdoc />
    public string Compose(string hostFilter, IReadOnlyList<string> classNames)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hostFilter);
        ArgumentNullException.ThrowIfNull(classNames);
        foreach (var name in classNames)
        {
            if (name is null || !PlainIdentifier.IsMatch(name))
                throw new ArgumentException($"Integration lane class '{name}' is not a plain class name.", nameof(classNames));
        }

        if (classNames.Count == 0)
            return hostFilter;

        return $"{hostFilter}&({string.Join("|", classNames.Select(name => $"FullyQualifiedName~.{name}."))})";
    }
}
