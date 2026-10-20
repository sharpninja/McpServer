using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

/// <summary>
/// TR-MCP-NUKE-017: Production rules that keep every documentation diagram inside the one Structurizr workspace
/// (AC-3 Mermaid, AC-2 and AC-4 links and renders, AC-5 page standard, AC-1 and AC-2 workspace JSON readers).
/// </summary>
sealed partial class ArchitectureDiagramRules : IArchitectureDiagramRules
{
    /// <summary>8 pt at 96 dpi.</summary>
    private const double MinTextPixels = 32.0 / 3.0;

    /// <summary>
    /// 16 pt at 96 dpi (AC-5 amended 2026-10-19): element names reach 16 pt while the 0.7x metadata line stays
    /// visible at 8 pt, a fixed 2:1 spread in Structurizr renders.
    /// </summary>
    private const double MaxTextPixels = 64.0 / 3.0;

    /// <summary>Letter long side at 96 dpi.</summary>
    private const double PageLongSide = 1056.0;

    /// <summary>Letter short side at 96 dpi.</summary>
    private const double PageShortSide = 816.0;

    private const double Tolerance = 1e-9;

    [GeneratedRegex(@"^ {0,3}(```|~~~)\s*mermaid\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MermaidFenceRegex();

    [GeneratedRegex(@"!\[[^\]]*\]\((?<target>[^)\s]+)(?:\s+""[^""]*"")?\)", RegexOptions.CultureInvariant)]
    private static partial Regex ImageLinkRegex();

    [GeneratedRegex(@"^\s*(?<value>[0-9]*\.?[0-9]+)\s*(px)?\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex FontSizeValueRegex();

    [GeneratedRegex(@"font-size\s*:\s*(?<value>[0-9]*\.?[0-9]+)\s*(px)?", RegexOptions.CultureInvariant)]
    private static partial Regex FontSizeStyleRegex();

    /// <inheritdoc />
    public IReadOnlyList<ArchitectureDiagramFinding> FindMermaid(IEnumerable<TrackedTextFile> files)
    {
        ArgumentNullException.ThrowIfNull(files);
        var findings = new List<ArchitectureDiagramFinding>();
        foreach (var file in files)
        {
            var path = NormalizePath(file.RelativePath);
            if (path.StartsWith(ArchitectureDiagramPaths.ReceiptsPrefix, StringComparison.Ordinal))
            {
                continue;
            }

            if (path.EndsWith(".mmd", StringComparison.OrdinalIgnoreCase))
            {
                findings.Add(new(ArchitectureDiagramFindingCodes.MermaidFile, path, "Mermaid source file; every diagram must be a view of " + ArchitectureDiagramPaths.WorkspaceDirectory + "/workspace.dsl."));
            }

            if (!IsMarkdown(path) || file.Content is null)
            {
                continue;
            }

            var lines = file.Content.Split('\n');
            for (var index = 0; index < lines.Length; index++)
            {
                var line = lines[index].TrimEnd('\r');
                if (MermaidFenceRegex().IsMatch(line))
                {
                    findings.Add(new(ArchitectureDiagramFindingCodes.MermaidFence, path + ":" + (index + 1).ToString(CultureInfo.InvariantCulture), "Mermaid fence '" + line.Trim() + "'; replace it with a rendered view of the workspace."));
                }
            }
        }

        return findings;
    }

    /// <inheritdoc />
    public IReadOnlyList<ArchitectureDiagramFinding> FindLinkProblems(
        IReadOnlyCollection<string> viewKeys,
        IEnumerable<TrackedTextFile> markdownFiles,
        IEnumerable<string> renderedRelativePaths)
    {
        ArgumentNullException.ThrowIfNull(viewKeys);
        ArgumentNullException.ThrowIfNull(markdownFiles);
        ArgumentNullException.ThrowIfNull(renderedRelativePaths);

        var keys = new HashSet<string>(viewKeys, StringComparer.Ordinal);
        var findings = new List<ArchitectureDiagramFinding>();
        var renderedPrefix = ArchitectureDiagramPaths.RenderedDirectory + "/";

        foreach (var file in markdownFiles)
        {
            var path = NormalizePath(file.RelativePath);
            if (file.Content is null || !IsMarkdown(path))
            {
                continue;
            }

            var directory = path.Contains('/', StringComparison.Ordinal) ? path[..path.LastIndexOf('/')] : string.Empty;
            var lines = file.Content.Split('\n');
            for (var index = 0; index < lines.Length; index++)
            {
                foreach (Match match in ImageLinkRegex().Matches(lines[index]))
                {
                    var target = match.Groups["target"].Value;
                    if (target.Contains("://", StringComparison.Ordinal) || target.StartsWith('/'))
                    {
                        continue;
                    }

                    var resolved = ResolveRelative(directory, target.Split('#', '?')[0]);
                    if (resolved is null || !resolved.StartsWith(renderedPrefix, StringComparison.Ordinal) || !resolved.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var key = resolved[renderedPrefix.Length..^".svg".Length];
                    if (!keys.Contains(key))
                    {
                        findings.Add(new(ArchitectureDiagramFindingCodes.DanglingViewLink, path + ":" + (index + 1).ToString(CultureInfo.InvariantCulture), "Image links view '" + key + "', which the workspace does not define."));
                    }
                }
            }
        }

        var renderedKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var rendered in renderedRelativePaths.Select(NormalizePath))
        {
            var isViewFile = rendered.StartsWith(renderedPrefix, StringComparison.Ordinal)
                && rendered.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)
                && !rendered[renderedPrefix.Length..].Contains('/', StringComparison.Ordinal);
            var key = isViewFile ? rendered[renderedPrefix.Length..^".svg".Length] : string.Empty;
            if (isViewFile && keys.Contains(key))
            {
                renderedKeys.Add(key);
            }
            else
            {
                findings.Add(new(ArchitectureDiagramFindingCodes.OrphanRender, rendered, "Rendered file has no workspace view."));
            }
        }

        foreach (var key in viewKeys.Distinct(StringComparer.Ordinal))
        {
            if (!renderedKeys.Contains(key))
            {
                findings.Add(new(ArchitectureDiagramFindingCodes.MissingRender, key, "View has no rendered file " + renderedPrefix + key + ".svg."));
            }
        }

        return findings;
    }

    /// <inheritdoc />
    public IReadOnlyList<ArchitectureDiagramFinding> FindPageProblems(string viewKey, string svg)
    {
        ArgumentException.ThrowIfNullOrEmpty(viewKey);
        XDocument document;
        try
        {
            document = XDocument.Parse(svg ?? string.Empty);
        }
        catch (XmlException exception)
        {
            return [new(ArchitectureDiagramFindingCodes.InvalidSvg, viewKey, "Render is not XML: " + exception.Message)];
        }

        var root = document.Root;
        if (root is null || root.Name.LocalName != "svg" || !TryReadViewBox(root, out var width, out var height))
        {
            return [new(ArchitectureDiagramFindingCodes.InvalidSvg, viewKey, "Render is not an SVG with a positive viewBox.")];
        }

        var sizes = root.Descendants()
            .Where(element => element.Name.LocalName == "text" && HasVisibleText(element))
            .Select(ReadFontSize)
            .Where(size => size is > 0)
            .Select(size => size!.Value)
            .ToArray();
        if (sizes.Length == 0)
        {
            return [new(ArchitectureDiagramFindingCodes.InvalidSvg, viewKey, "Render has no visible text with a font size.")];
        }

        var smallest = sizes.Min();
        var largest = sizes.Max();
        var lowestScale = MinTextPixels / smallest;
        var highestScale = MaxTextPixels / largest;
        if (lowestScale > highestScale + Tolerance)
        {
            return [new(
                ArchitectureDiagramFindingCodes.TextSizeSpread,
                viewKey,
                string.Create(CultureInfo.InvariantCulture, $"Visible text spans {smallest:0.##} to {largest:0.##} px (ratio {largest / smallest:0.00}); no scale keeps all text within 8 to 16 pt (ratio at most 2.00)."))];
        }

        var fitLandscape = Math.Min(PageLongSide / width, PageShortSide / height);
        var fitPortrait = Math.Min(PageShortSide / width, PageLongSide / height);
        var fit = Math.Max(fitLandscape, fitPortrait);
        if (lowestScale > fit + Tolerance)
        {
            return [new(
                ArchitectureDiagramFindingCodes.PageOverflow,
                viewKey,
                string.Create(CultureInfo.InvariantCulture, $"Render {width:0.##} x {height:0.##} needs scale {lowestScale:0.000} for 8 pt text but fits a letter page only at scale {fit:0.000}; split the view per functional group."))];
        }

        return [];
    }

    /// <inheritdoc />
    public IReadOnlyList<string> ReadViewKeys(string workspaceJson)
    {
        using var document = JsonDocument.Parse(workspaceJson);
        var keys = new List<string>();
        if (!document.RootElement.TryGetProperty("views", out var views) || views.ValueKind != JsonValueKind.Object)
        {
            return keys;
        }

        foreach (var property in views.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var view in property.Value.EnumerateArray())
            {
                if (view.ValueKind == JsonValueKind.Object && view.TryGetProperty("key", out var key) && key.ValueKind == JsonValueKind.String)
                {
                    keys.Add(key.GetString()!);
                }
            }
        }

        return keys;
    }

    /// <inheritdoc />
    public int ReadInspectionErrorCount(string workspaceJson)
    {
        using var document = JsonDocument.Parse(workspaceJson);
        if (!document.RootElement.TryGetProperty("properties", out var properties)
            || properties.ValueKind != JsonValueKind.Object
            || !properties.TryGetProperty("structurizr.inspection.error", out var value))
        {
            return 0;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number => value.GetInt32(),
            JsonValueKind.String => int.Parse(value.GetString()!, NumberStyles.Integer, CultureInfo.InvariantCulture),
            _ => 0,
        };
    }

    private static bool IsMarkdown(string path)
        => path.EndsWith(".md", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".markdown", StringComparison.OrdinalIgnoreCase);

    private static string NormalizePath(string path) => path.Replace('\\', '/');

    private static string? ResolveRelative(string directory, string target)
    {
        var segments = new List<string>(directory.Length == 0 ? [] : directory.Split('/'));
        foreach (var segment in target.Replace('\\', '/').Split('/'))
        {
            if (segment.Length == 0 || segment == ".")
            {
                continue;
            }

            if (segment == "..")
            {
                if (segments.Count == 0)
                {
                    return null;
                }

                segments.RemoveAt(segments.Count - 1);
                continue;
            }

            segments.Add(Uri.UnescapeDataString(segment));
        }

        return string.Join('/', segments);
    }

    private static bool TryReadViewBox(XElement root, out double width, out double height)
    {
        width = 0;
        height = 0;
        var parts = root.Attribute("viewBox")?.Value.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries);
        return parts is { Length: 4 }
            && double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out width)
            && double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out height)
            && width > 0
            && height > 0;
    }

    private static bool IsHidden(XElement element)
        => element.AncestorsAndSelf().Any(node => string.Equals(node.Attribute("display")?.Value, "none", StringComparison.Ordinal));

    private static bool HasVisibleText(XElement text)
    {
        if (IsHidden(text))
        {
            return false;
        }

        var spans = text.Elements().Where(element => element.Name.LocalName == "tspan").ToArray();
        return spans.Length == 0
            ? !string.IsNullOrWhiteSpace(text.Value)
            : spans.Any(span => !IsHidden(span) && !string.IsNullOrWhiteSpace(span.Value));
    }

    private static double? ReadFontSize(XElement text)
    {
        var attribute = text.Attribute("font-size")?.Value;
        if (attribute is not null)
        {
            var match = FontSizeValueRegex().Match(attribute);
            if (match.Success)
            {
                return double.Parse(match.Groups["value"].Value, NumberStyles.Float, CultureInfo.InvariantCulture);
            }
        }

        var style = text.Attribute("style")?.Value;
        if (style is not null)
        {
            var match = FontSizeStyleRegex().Match(style);
            if (match.Success)
            {
                return double.Parse(match.Groups["value"].Value, NumberStyles.Float, CultureInfo.InvariantCulture);
            }
        }

        return null;
    }
}
