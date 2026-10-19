using System.Text;

/// <summary>
/// TR-MCP-NUKE-017: Production orchestration of one ArchitectureDiagrams run. Validates the workspace (AC-1),
/// renders every view and removes renders without a view (AC-2), then runs the Mermaid, link and page checks
/// (AC-3 to AC-5) on the resulting tree. A WhatIf run writes nothing and reports WHATIF actions (AC-6).
/// A failed validation stops the run after the Mermaid check, because the view keys are not trustworthy.
/// Inspection errors are reported and block rendering; the checks still run on the committed renders.
/// </summary>
/// <param name="tooling">Structurizr CLI and Chromium boundary.</param>
/// <param name="rules">Pure checks.</param>
sealed class ArchitectureDiagramsRunner(IArchitectureDiagramTooling tooling, IArchitectureDiagramRules rules) : IArchitectureDiagramsRunner
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    /// <inheritdoc />
    public ArchitectureDiagramsReport Run(ArchitectureDiagramsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var root = Path.GetFullPath(request.RepositoryRoot);
        var workspaceDirectory = Path.Combine(root, ArchitectureDiagramPaths.WorkspaceDirectory);
        var renderedDirectory = Path.Combine(root, ArchitectureDiagramPaths.RenderedDirectory);
        var workspaceFile = ArchitectureDiagramPaths.WorkspaceDirectory + "/workspace.dsl";
        var findings = new List<ArchitectureDiagramFinding>();
        var actions = new List<string>();
        var trackedFiles = ReadTrackedFiles(root, request.TrackedRelativePaths);

        actions.Add("validate " + workspaceFile);
        var validation = tooling.Validate(workspaceDirectory);
        if (validation.ExitCode != 0)
        {
            findings.Add(new(ArchitectureDiagramFindingCodes.ValidationFailed, workspaceFile, "Structurizr CLI validate exited " + validation.ExitCode + ": " + Summarize(validation.Output)));
            findings.AddRange(rules.FindMermaid(trackedFiles));
            return new ArchitectureDiagramsReport(findings, actions);
        }

        actions.Add("export workspace json");
        var workspaceJson = tooling.ExportWorkspaceJson(workspaceDirectory);
        var viewKeys = rules.ReadViewKeys(workspaceJson);
        var inspectionErrors = rules.ReadInspectionErrorCount(workspaceJson);
        if (inspectionErrors > 0)
        {
            findings.Add(new(ArchitectureDiagramFindingCodes.InspectionErrors, workspaceFile, inspectionErrors + " Structurizr inspection error(s); fix them before rendering."));
        }
        else
        {
            Render(request.WhatIf, workspaceDirectory, renderedDirectory, viewKeys, findings, actions);
        }

        findings.AddRange(rules.FindMermaid(trackedFiles));
        var renderedNow = ListRendered(root, renderedDirectory);
        findings.AddRange(rules.FindLinkProblems(viewKeys, trackedFiles.Where(file => file.Content is not null), renderedNow));
        foreach (var key in viewKeys.Distinct(StringComparer.Ordinal))
        {
            var path = Path.Combine(renderedDirectory, key + ".svg");
            if (File.Exists(path))
            {
                findings.AddRange(rules.FindPageProblems(key, File.ReadAllText(path, Utf8NoBom)));
            }
        }

        return new ArchitectureDiagramsReport(findings, actions);
    }

    private void Render(
        bool whatIf,
        string workspaceDirectory,
        string renderedDirectory,
        IReadOnlyList<string> viewKeys,
        List<ArchitectureDiagramFinding> findings,
        List<string> actions)
    {
        var keySet = new HashSet<string>(viewKeys, StringComparer.Ordinal);
        var stale = Directory.Exists(renderedDirectory)
            ? Directory.GetFiles(renderedDirectory)
                .Where(path => !path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) || !keySet.Contains(Path.GetFileNameWithoutExtension(path)))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray()
            : [];

        if (whatIf)
        {
            foreach (var key in viewKeys.Distinct(StringComparer.Ordinal))
            {
                actions.Add("WHATIF render " + key + " -> " + RenderedRelative(key));
            }

            foreach (var path in stale)
            {
                actions.Add("WHATIF remove " + ArchitectureDiagramPaths.RenderedDirectory + "/" + Path.GetFileName(path));
            }

            return;
        }

        var svgs = tooling.RenderViews(workspaceDirectory, viewKeys);
        Directory.CreateDirectory(renderedDirectory);
        foreach (var key in viewKeys.Distinct(StringComparer.Ordinal))
        {
            if (!svgs.TryGetValue(key, out var svg) || string.IsNullOrWhiteSpace(svg))
            {
                findings.Add(new(ArchitectureDiagramFindingCodes.RenderFailed, key, "Chromium returned no SVG for the view."));
                continue;
            }

            var path = Path.Combine(renderedDirectory, key + ".svg");
            if (!File.Exists(path) || !string.Equals(File.ReadAllText(path, Utf8NoBom), svg, StringComparison.Ordinal))
            {
                File.WriteAllText(path, svg, Utf8NoBom);
            }

            actions.Add("render " + key + " -> " + RenderedRelative(key));
        }

        foreach (var path in stale)
        {
            File.Delete(path);
            actions.Add("remove " + ArchitectureDiagramPaths.RenderedDirectory + "/" + Path.GetFileName(path));
        }
    }

    private static string RenderedRelative(string key) => ArchitectureDiagramPaths.RenderedDirectory + "/" + key + ".svg";

    private static TrackedTextFile[] ReadTrackedFiles(string root, IReadOnlyList<string> trackedRelativePaths)
        => trackedRelativePaths
            .Select(path => path.Replace('\\', '/'))
            .Distinct(StringComparer.Ordinal)
            .Select(path =>
            {
                var isMarkdown = path.EndsWith(".md", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".markdown", StringComparison.OrdinalIgnoreCase);
                var full = Path.Combine(root, path);
                return new TrackedTextFile(path, isMarkdown && File.Exists(full) ? File.ReadAllText(full) : null);
            })
            .ToArray();

    private static string[] ListRendered(string root, string renderedDirectory)
        => Directory.Exists(renderedDirectory)
            ? Directory.GetFiles(renderedDirectory)
                .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/'))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray()
            : [];

    private static string Summarize(string output)
    {
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return lines.Length == 0 ? "(no output)" : string.Join(" | ", lines.Take(5));
    }
}
