namespace QBrainAi.Support.Mcp;

/// <summary>
/// TR-MCP-QBRAIN-004: Shared matchers for the canonical <c>/qbrainai</c> prefix and the 1.x <c>/mcpserver</c> prefix.
/// </summary>
public static class ProductApiPaths
{
    /// <summary>Canonical product API prefix.</summary>
    public const string CanonicalPrefix = "/qbrainai";

    /// <summary>1.x product API prefix.</summary>
    public const string LegacyPrefix = "/mcpserver";

    /// <summary>True when the path is under <c>/qbrainai/</c> or <c>/mcpserver/</c>.</summary>
    /// <param name="path">Request path.</param>
    /// <returns>True when the path is a product API route.</returns>
    public static bool IsProductApi(PathString path)
    {
        if (!path.HasValue || string.IsNullOrEmpty(path.Value))
            return false;

        return IsProductApi(path.Value);
    }

    /// <summary>True when the path is under <c>/qbrainai/</c> or <c>/mcpserver/</c>.</summary>
    /// <param name="path">Request path.</param>
    /// <returns>True when the path is a product API route.</returns>
    public static bool IsProductApi(string path)
        => StartsWithProductPrefix(path, CanonicalPrefix) || StartsWithProductPrefix(path, LegacyPrefix);

    /// <summary>True when the first path segment is <c>qbrainai</c> or <c>mcpserver</c>.</summary>
    /// <param name="segment">A single path segment.</param>
    /// <returns>True for either product root segment.</returns>
    public static bool IsProductRootSegment(string? segment)
        => string.Equals(segment, "qbrainai", StringComparison.OrdinalIgnoreCase)
           || string.Equals(segment, "mcpserver", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// True when <paramref name="path"/> is exactly <c>/qbrainai/{suffix}</c> or <c>/mcpserver/{suffix}</c>.
    /// </summary>
    /// <param name="path">Request path, with or without a trailing slash.</param>
    /// <param name="suffix">Path after the product prefix, without a leading slash.</param>
    /// <returns>True when the path equals either product form of the suffix.</returns>
    public static bool EqualsProductPath(string path, string suffix)
    {
        var value = TrimTrailingSlash(path);
        var tail = NormalizeSuffix(suffix);
        return string.Equals(value, CanonicalPrefix + tail, StringComparison.OrdinalIgnoreCase)
               || string.Equals(value, LegacyPrefix + tail, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// True when <paramref name="path"/> is the product suffix or a child of it, for either prefix.
    /// </summary>
    /// <param name="path">Request path.</param>
    /// <param name="suffix">Path after the product prefix, without a leading slash.</param>
    /// <returns>True for either prefix.</returns>
    public static bool StartsWithProductSuffix(string path, string suffix)
    {
        var tail = NormalizeSuffix(suffix);
        return StartsWithBounded(path, CanonicalPrefix + tail)
               || StartsWithBounded(path, LegacyPrefix + tail);
    }

    /// <summary>
    /// Reads the third segment when the path is <c>/{prefix}/{resource}/{id}</c> for either product prefix.
    /// </summary>
    /// <param name="path">Request path.</param>
    /// <param name="resource">Second segment, such as <c>todo</c> or <c>memory</c>.</param>
    /// <param name="id">The decoded id when the shape matches.</param>
    /// <returns>True when both the prefix and the resource match and an id is present.</returns>
    public static bool TryReadResourceId(string path, string resource, out string? id)
    {
        id = null;
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length != 3
            || !IsProductRootSegment(segments[0])
            || !string.Equals(segments[1], resource, StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(segments[2]))
        {
            return false;
        }

        id = Uri.UnescapeDataString(segments[2]);
        return !string.IsNullOrWhiteSpace(id);
    }

    /// <summary>
    /// Returns the text after <c>/qbrainai/{resource}/</c> or <c>/mcpserver/{resource}/</c>.
    /// </summary>
    /// <param name="path">Path or resource id that may contain a product route.</param>
    /// <param name="resource">Resource segment.</param>
    /// <param name="tail">Decoded remainder after the marker.</param>
    /// <returns>True when either marker is present.</returns>
    public static bool TryTakeResourceTail(string path, string resource, out string tail)
    {
        var resourceName = resource.Trim('/');
        foreach (var prefix in new[] { CanonicalPrefix, LegacyPrefix })
        {
            var marker = prefix + "/" + resourceName + "/";
            var index = path.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
                continue;

            tail = Uri.UnescapeDataString(path[(index + marker.Length)..]);
            return true;
        }

        tail = string.Empty;
        return false;
    }

    /// <summary>Builds a set containing both product prefixes for each suffix.</summary>
    /// <param name="suffixes">Suffixes such as <c>/todo</c>.</param>
    /// <returns>A case-insensitive set of full prefixes.</returns>
    public static HashSet<string> ExpandSuffixes(params string[] suffixes)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var suffix in suffixes)
        {
            var tail = suffix.StartsWith('/') ? suffix : "/" + suffix;
            set.Add(CanonicalPrefix + tail);
            set.Add(LegacyPrefix + tail);
        }

        return set;
    }

    private static bool StartsWithProductPrefix(string path, string prefix)
        => path.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase);

    private static bool StartsWithBounded(string path, string prefix)
    {
        if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        return path.Length == prefix.Length || path[prefix.Length] == '/';
    }

    private static string NormalizeSuffix(string suffix)
        => suffix.StartsWith('/') ? suffix.TrimEnd('/') : "/" + suffix.Trim('/');

    private static string TrimTrailingSlash(string path)
        => path.Length > 1 ? path.TrimEnd('/') : path;
}
