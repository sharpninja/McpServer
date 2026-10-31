/// <summary>
/// TR-MCP-NUKE-019 AC-2: Composes the dotnet test filter of one lane project from the host filter and the classes the
/// lane runs from that project: the host filter alone when the lane runs every class; otherwise the host filter and a
/// parenthesized alternative of one fully qualified name match per class, each bounded by dots so that it selects that
/// class and no class whose name only contains it. A blank host filter, and a class name that is not a plain
/// identifier (letters, digits and underscores, not starting with a digit), are refused.
/// </summary>
interface IIntegrationLaneClassFilter
{
    /// <summary>
    /// Returns the filter for a project; throws <see cref="ArgumentException"/> for a blank
    /// <paramref name="hostFilter"/> or a class name that is not a plain identifier.
    /// </summary>
    /// <param name="hostFilter">The host-scoped lane filter.</param>
    /// <param name="classNames">The classes the lane runs from the project; empty for every class.</param>
    string Compose(string hostFilter, IReadOnlyList<string> classNames);
}
