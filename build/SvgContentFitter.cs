using System.Globalization;
using System.Text.RegularExpressions;

/// <summary>
/// TR-MCP-NUKE-017 AC-2 ("the content fitted"): crops an exported view to its drawn content by replacing only the
/// root viewBox value. The document is edited as text, never re-serialized, so every other byte is unchanged and
/// renders stay byte-identical across runs.
/// </summary>
sealed partial class SvgContentFitter : ISvgContentFitter
{
    /// <summary>Margin around the content, the one the Structurizr viewer uses when it crops a diagram.</summary>
    public const double Margin = 50;

    /// <inheritdoc />
    public string Fit(string svg, SvgContentBox content)
    {
        ArgumentNullException.ThrowIfNull(svg);
        ArgumentNullException.ThrowIfNull(content);

        var match = RootViewBox().Match(svg);
        if (!match.Success)
        {
            throw new ArgumentException("The document is not an SVG with a root viewBox.", nameof(svg));
        }

        var parts = match.Groups[1].Value.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4
            || !double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var paperX)
            || !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var paperY)
            || !double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var paperWidth)
            || !double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var paperHeight)
            || paperWidth <= 0
            || paperHeight <= 0)
        {
            throw new ArgumentException("The root viewBox is not four numbers with a positive size.", nameof(svg));
        }

        var finite = double.IsFinite(content.MinX) && double.IsFinite(content.MinY) && double.IsFinite(content.MaxX) && double.IsFinite(content.MaxY);
        if (!finite || content.MaxX <= content.MinX || content.MaxY <= content.MinY)
        {
            throw new ArgumentException("The content box is empty.", nameof(content));
        }

        var minX = Math.Max(paperX, Math.Floor(content.MinX - Margin));
        var minY = Math.Max(paperY, Math.Floor(content.MinY - Margin));
        var maxX = Math.Min(paperX + paperWidth, Math.Ceiling(content.MaxX + Margin));
        var maxY = Math.Min(paperY + paperHeight, Math.Ceiling(content.MaxY + Margin));
        var value = string.Join(' ', new[] { minX, minY, maxX - minX, maxY - minY }.Select(Format));

        var group = match.Groups[1];
        return string.Concat(svg.AsSpan(0, group.Index), value, svg.AsSpan(group.Index + group.Length));
    }

    private static string Format(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    [GeneratedRegex("^\\s*<svg\\b[^>]*?\\sviewBox=\"([^\"]*)\"", RegexOptions.CultureInvariant)]
    private static partial Regex RootViewBox();
}
