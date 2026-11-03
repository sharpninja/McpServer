namespace QBrainAi.Repl.Core.Tests.TestSupport;

/// <summary>
/// FR-MCP-TESTRESOURCE-001 ac-4 and the generated-artifact criterion, scripted reference for the path contracts: a
/// path's identity is its root (a UNC server and share, written with two leading separators of either kind; a drive
/// letter; or the POSIX root) and its segments, split on either separator. Containment and equality require the same
/// root kind and the same root identity before any segment is compared; UNC and drive-letter paths compare ignoring
/// case, POSIX paths ordinally. Two flags turn it into the contracts' wrong checks: one that flattens the root into the
/// segments, and one that compares UNC paths ordinally. Strings only: no path is resolved, created or read.
/// </summary>
/// <remarks>
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-03</term><description>FR-MCP-TESTRESOURCE-001 ac-4 (appended "Strict containment in the owned root also requires the same root") and the generated-artifact criterion (appended "A path's root is part of its identity"): added as the independent reference and the wrong checks of the start, database, cache and output contracts, because hostile validation round 10 finding R10-F2 showed the shared checks flattened the root and compared forward-slash UNC paths ordinally (operator 2026-11-03: "don't gate on me between rounds").</description></item>
/// </list>
/// </remarks>
internal static class ScriptedPathIdentity
{
    /// <summary>The kind of a path's root.</summary>
    internal enum RootKind
    {
        /// <summary>Not an absolute path this reference recognizes.</summary>
        None,

        /// <summary>The POSIX root, one leading '/'.</summary>
        Posix,

        /// <summary>A drive letter, a colon and a separator.</summary>
        Drive,

        /// <summary>Two leading separators of either kind, a server and a share.</summary>
        Unc,
    }

    /// <summary>A path's root kind, root text and segments.</summary>
    /// <param name="Kind">The root kind.</param>
    /// <param name="Root">The root text: "/" for POSIX, the drive letter, or "server/share".</param>
    /// <param name="Segments">The segments after the root.</param>
    internal readonly record struct Identity(RootKind Kind, string Root, string[] Segments);

    /// <summary>Parses a path into its root and segments.</summary>
    /// <param name="path">The path.</param>
    /// <returns>The identity; <see cref="RootKind.None"/> when the path is not absolute.</returns>
    internal static Identity Parse(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (path.Length > 2 && IsSeparator(path[0]) && IsSeparator(path[1]) && !IsSeparator(path[2]))
        {
            var parts = Split(path[2..]);
            return parts.Length >= 2 ? new Identity(RootKind.Unc, parts[0] + "/" + parts[1], parts[2..]) : default;
        }

        if (path.Length >= 3 && char.IsAsciiLetter(path[0]) && path[1] == ':' && IsSeparator(path[2]))
            return new Identity(RootKind.Drive, path[..1], Split(path[3..]));
        return path.StartsWith('/') ? new Identity(RootKind.Posix, "/", Split(path[1..])) : default;
    }

    /// <summary>Whether a path is absolute under this reference.</summary>
    /// <param name="path">The path.</param>
    /// <returns>True for a UNC, drive-letter or POSIX path.</returns>
    internal static bool IsAbsolute(string path) => Parse(path).Kind != RootKind.None;

    /// <summary>Whether a path lies strictly inside a root, with no '.' or '..' segment.</summary>
    /// <param name="path">The path.</param>
    /// <param name="root">The root.</param>
    /// <param name="flattenRoot">Wrong check: ignore root kind and identity and compare every part as one segment list.</param>
    /// <param name="ordinalUnc">Wrong check: compare UNC paths ordinally.</param>
    /// <returns>True when the path is strictly inside the root.</returns>
    internal static bool Inside(string path, string root, bool flattenRoot = false, bool ordinalUnc = false)
    {
        var p = Parse(path);
        var r = Parse(root);
        if (p.Kind == RootKind.None || r.Kind == RootKind.None || p.Segments.Any(segment => segment is "." or ".."))
            return false;
        var comparison = Comparison(p, r, ordinalUnc);
        if (!flattenRoot && !SameRoot(p, r, comparison))
            return false;
        var pathParts = flattenRoot ? Flatten(p) : p.Segments;
        var rootParts = flattenRoot ? Flatten(r) : r.Segments;
        return pathParts.Length > rootParts.Length
            && rootParts.Select((segment, index) => string.Equals(segment, pathParts[index], comparison)).All(same => same);
    }

    /// <summary>Whether two paths name the same location.</summary>
    /// <param name="path">The first path.</param>
    /// <param name="other">The second path.</param>
    /// <param name="flattenRoot">Wrong check: ignore root kind and identity.</param>
    /// <param name="ordinalUnc">Wrong check: compare UNC paths ordinally.</param>
    /// <returns>True when both have the same root and the same segments.</returns>
    internal static bool Same(string path, string other, bool flattenRoot = false, bool ordinalUnc = false)
    {
        var p = Parse(path);
        var o = Parse(other);
        if (p.Kind == RootKind.None || o.Kind == RootKind.None)
            return false;
        var comparison = Comparison(p, o, ordinalUnc);
        if (!flattenRoot && !SameRoot(p, o, comparison))
            return false;
        var left = flattenRoot ? Flatten(p) : p.Segments;
        var right = flattenRoot ? Flatten(o) : o.Segments;
        return left.Length == right.Length && left.Zip(right).All(pair => string.Equals(pair.First, pair.Second, comparison));
    }

    /// <summary>FR-MCP-TESTRESOURCE-001 ac-4: whether two identities share root kind and root text.</summary>
    /// <param name="left">The first identity.</param>
    /// <param name="right">The second identity.</param>
    /// <param name="comparison">How the root text compares.</param>
    /// <returns>True for the same root.</returns>
    /// <remarks>2026-11-03, FR-MCP-TESTRESOURCE-001 ac-4: added for hostile validation round 10 finding R10-F2.</remarks>
    private static bool SameRoot(Identity left, Identity right, StringComparison comparison) =>
        left.Kind == right.Kind && string.Equals(left.Root, right.Root, comparison);

    /// <summary>FR-MCP-TESTRESOURCE-001 ac-4: ignoring case when both roots are Windows syntax (drive or UNC), ordinally otherwise.</summary>
    /// <param name="left">The first identity.</param>
    /// <param name="right">The second identity.</param>
    /// <param name="ordinalUnc">Wrong check: compare UNC paths ordinally.</param>
    /// <returns>The comparison.</returns>
    /// <remarks>2026-11-03, FR-MCP-TESTRESOURCE-001 ac-4 and the generated-artifact criterion: added for hostile validation round 10 finding R10-F2.</remarks>
    private static StringComparison Comparison(Identity left, Identity right, bool ordinalUnc)
    {
        static bool Windows(RootKind kind) => kind is RootKind.Drive or RootKind.Unc;
        if (!Windows(left.Kind) || !Windows(right.Kind))
            return StringComparison.Ordinal;
        return ordinalUnc && (left.Kind == RootKind.Unc || right.Kind == RootKind.Unc) ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
    }

    /// <summary>For the wrong check "ignores the root": the root's parts followed by the segments, as one list.</summary>
    /// <param name="identity">The identity.</param>
    /// <returns>The flattened parts.</returns>
    /// <remarks>2026-11-03, FR-MCP-TESTRESOURCE-001 ac-4: added for hostile validation round 10 finding R10-F2.</remarks>
    private static string[] Flatten(Identity identity) => identity.Kind switch
    {
        RootKind.Unc => [.. identity.Root.Split('/'), .. identity.Segments],
        RootKind.Drive => [identity.Root + ":", .. identity.Segments],
        _ => identity.Segments,
    };

    /// <summary>Whether a character is a path separator of either kind.</summary>
    /// <param name="value">The character.</param>
    /// <returns>True for '/' or '\'.</returns>
    /// <remarks>2026-11-03, FR-MCP-TESTRESOURCE-001 ac-4: added for hostile validation round 10 finding R10-F2.</remarks>
    private static bool IsSeparator(char value) => value is '/' or '\\';

    /// <summary>The non-empty parts of a text split on either separator.</summary>
    /// <param name="text">The text after the root.</param>
    /// <returns>The parts.</returns>
    /// <remarks>2026-11-03, FR-MCP-TESTRESOURCE-001 ac-4: added for hostile validation round 10 finding R10-F2.</remarks>
    private static string[] Split(string text) => text.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
}
