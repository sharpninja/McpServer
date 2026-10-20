using System.Globalization;
using System.Text.RegularExpressions;

/// <summary>
/// TR-MCP-NUKE-017 AC-2: renumbers the element ids the diagram library generates (JointJS <c>j_N</c>, <c>v-N</c> and
/// <c>v_N</c>) from 1 per prefix in definition order, so the bytes of a render depend only on its own view. The
/// document is edited as text in one pass, never re-serialized, so every other byte is unchanged.
/// </summary>
sealed partial class SvgIdRenumberer : ISvgIdRenumberer
{
    /// <inheritdoc />
    public string Renumber(string svg)
    {
        ArgumentNullException.ThrowIfNull(svg);
        if (!RootElement().IsMatch(svg))
        {
            throw new ArgumentException("The document is not an SVG.", nameof(svg));
        }

        var renamed = new Dictionary<string, string>(StringComparer.Ordinal);
        var counters = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (Match definition in Definition().Matches(svg))
        {
            var id = definition.Groups["id"].Value;
            if (renamed.ContainsKey(id))
            {
                continue;
            }

            var prefix = definition.Groups["prefix"].Value;
            var next = counters.GetValueOrDefault(prefix) + 1;
            counters[prefix] = next;
            renamed[id] = prefix + next.ToString(CultureInfo.InvariantCulture);
        }

        if (renamed.Count == 0)
        {
            return svg;
        }

        return Occurrence().Replace(svg, match =>
            renamed.TryGetValue(match.Groups["id"].Value, out var replacement)
                ? match.Groups["lead"].Value + replacement
                : match.Value);
    }

    [GeneratedRegex("^\\s*<svg\\b", RegexOptions.CultureInvariant)]
    private static partial Regex RootElement();

    /// <summary>An id attribute whose whole value is a generated id.</summary>
    [GeneratedRegex("\\sid=\"(?<id>(?<prefix>j_|v-|v_)\\d+)\"", RegexOptions.CultureInvariant)]
    private static partial Regex Definition();

    /// <summary>A generated id in a definition, a url(#id) reference or an href="#id" reference.</summary>
    [GeneratedRegex("(?<lead>\\sid=\"|url\\(#|[\\s:]href=\"#)(?<id>(?:j_|v-|v_)\\d+)(?=[\")])", RegexOptions.CultureInvariant)]
    private static partial Regex Occurrence();
}
