/// <summary>
/// TR-MCP-NUKE-017: Paths and finding codes of the ArchitectureDiagrams target, which keeps every
/// documentation diagram inside the one Structurizr workspace (PLAN-C4MODEL-001).
/// </summary>
static class ArchitectureDiagramPaths
{
    /// <summary>Repository-relative directory holding workspace.dsl and its fragments.</summary>
    public const string WorkspaceDirectory = "docs/architecture/c4";

    /// <summary>Repository-relative directory holding one rendered SVG per view, named by view key.</summary>
    public const string RenderedDirectory = "docs/architecture/c4/rendered";

    /// <summary>Repository-relative prefix of historical receipts, which keep their recorded content.</summary>
    public const string ReceiptsPrefix = "docs/receipts/";

    /// <summary>
    /// AC-1: Pinned Structurizr CLI image (tag and digest read with docker image inspect on 2026-10-19);
    /// never :latest, which is an end-of-life stub that validates nothing.
    /// </summary>
    public const string StructurizrCliImage = "structurizr/cli:2025.11.09@sha256:0399ac8e24c16e41277cfcec22caa29e4775013e5395c0720016828426d62749";
}

/// <summary>
/// TR-MCP-NUKE-017: Stable finding codes reported by the ArchitectureDiagrams target.
/// </summary>
static class ArchitectureDiagramFindingCodes
{
    /// <summary>AC-1: the Structurizr CLI rejected the workspace.</summary>
    public const string ValidationFailed = "ValidationFailed";

    /// <summary>AC-1: the workspace has Structurizr inspection errors.</summary>
    public const string InspectionErrors = "InspectionErrors";

    /// <summary>AC-2: a view could not be rendered.</summary>
    public const string RenderFailed = "RenderFailed";

    /// <summary>AC-3: a markdown document contains a Mermaid fence.</summary>
    public const string MermaidFence = "MermaidFence";

    /// <summary>AC-3: a tracked file has the .mmd extension.</summary>
    public const string MermaidFile = "MermaidFile";

    /// <summary>AC-4: a markdown image points at a rendered view the workspace does not define.</summary>
    public const string DanglingViewLink = "DanglingViewLink";

    /// <summary>AC-2 and AC-4: a workspace view has no rendered file.</summary>
    public const string MissingRender = "MissingRender";

    /// <summary>AC-4: a rendered file has no workspace view.</summary>
    public const string OrphanRender = "OrphanRender";

    /// <summary>AC-5: no single scale puts every visible text size within 8 to 16 pt (amended 2026-10-19).</summary>
    public const string TextSizeSpread = "TextSizeSpread";

    /// <summary>AC-5: at a scale that puts the text within 8 to 16 pt, the render exceeds one letter page.</summary>
    public const string PageOverflow = "PageOverflow";

    /// <summary>AC-5: the rendered file is not an SVG with a viewBox and visible text.</summary>
    public const string InvalidSvg = "InvalidSvg";
}

/// <summary>
/// TR-MCP-NUKE-017: One problem found by the ArchitectureDiagrams target.
/// </summary>
/// <param name="Code">A value of <see cref="ArchitectureDiagramFindingCodes"/>.</param>
/// <param name="Location">Repository-relative path, view key, or path with line number.</param>
/// <param name="Message">Human-readable description with the evidence.</param>
sealed record ArchitectureDiagramFinding(string Code, string Location, string Message);

/// <summary>
/// TR-MCP-NUKE-017: A tracked repository file. Content is supplied for markdown files only.
/// </summary>
/// <param name="RelativePath">Repository-relative path with forward slashes.</param>
/// <param name="Content">File text for markdown files; null for other files.</param>
sealed record TrackedTextFile(string RelativePath, string? Content);

/// <summary>
/// TR-MCP-NUKE-017: Pure checks behind AC-3, AC-4 and AC-5, and the workspace JSON readers behind AC-1 and AC-2.
/// </summary>
interface IArchitectureDiagramRules
{
    /// <summary>AC-3: Mermaid fences in markdown and .mmd files, outside docs/receipts.</summary>
    IReadOnlyList<ArchitectureDiagramFinding> FindMermaid(IEnumerable<TrackedTextFile> files);

    /// <summary>AC-2 and AC-4: dangling view links, views without renders, and renders without views.</summary>
    IReadOnlyList<ArchitectureDiagramFinding> FindLinkProblems(
        IReadOnlyCollection<string> viewKeys,
        IEnumerable<TrackedTextFile> markdownFiles,
        IEnumerable<string> renderedRelativePaths);

    /// <summary>AC-5: letter-page and 8 to 16 pt text standard for one rendered view.</summary>
    IReadOnlyList<ArchitectureDiagramFinding> FindPageProblems(string viewKey, string svg);

    /// <summary>AC-2: every view key of an exported workspace JSON document, in document order.</summary>
    IReadOnlyList<string> ReadViewKeys(string workspaceJson);

    /// <summary>AC-1: the Structurizr inspection error count recorded in an exported workspace JSON document.</summary>
    int ReadInspectionErrorCount(string workspaceJson);
}

/// <summary>
/// TR-MCP-NUKE-017: Result of one external tool invocation.
/// </summary>
/// <param name="ExitCode">Process exit code.</param>
/// <param name="Output">Combined standard output and error.</param>
sealed record ArchitectureToolResult(int ExitCode, string Output);

/// <summary>
/// TR-MCP-NUKE-017: External tooling boundary: the pinned Structurizr CLI and headless Chromium.
/// Implementations work on a temporary copy of the workspace and never write under docs.
/// </summary>
interface IArchitectureDiagramTooling
{
    /// <summary>AC-1: validates the workspace in the given directory.</summary>
    ArchitectureToolResult Validate(string workspaceDirectory);

    /// <summary>AC-1 and AC-2: exports the workspace to Structurizr JSON and returns the JSON text.</summary>
    string ExportWorkspaceJson(string workspaceDirectory);

    /// <summary>AC-2: renders each view key to SVG text; a key missing from the result failed to render.</summary>
    IReadOnlyDictionary<string, string> RenderViews(string workspaceDirectory, IReadOnlyList<string> viewKeys);
}

/// <summary>
/// TR-MCP-NUKE-017: Input of one ArchitectureDiagrams run.
/// </summary>
/// <param name="RepositoryRoot">Absolute repository root.</param>
/// <param name="TrackedRelativePaths">Repository-relative tracked paths (git ls-files).</param>
/// <param name="WhatIf">AC-6: report what would change and write nothing.</param>
sealed record ArchitectureDiagramsRequest(string RepositoryRoot, IReadOnlyList<string> TrackedRelativePaths, bool WhatIf);

/// <summary>
/// TR-MCP-NUKE-017: Orchestrates one ArchitectureDiagrams run: validate (AC-1), render (AC-2), then the
/// Mermaid, link and page checks (AC-3 to AC-5); a WhatIf run writes nothing (AC-6).
/// </summary>
interface IArchitectureDiagramsRunner
{
    /// <summary>Runs the target against the repository named by the request.</summary>
    ArchitectureDiagramsReport Run(ArchitectureDiagramsRequest request);
}

/// <summary>
/// TR-MCP-NUKE-017: Outcome of one ArchitectureDiagrams run.
/// </summary>
/// <param name="Findings">Every problem found; the run fails when any exists.</param>
/// <param name="Actions">Ordered actions taken, or in a WhatIf run the WHATIF lines of actions skipped.</param>
sealed record ArchitectureDiagramsReport(IReadOnlyList<ArchitectureDiagramFinding> Findings, IReadOnlyList<string> Actions)
{
    /// <summary>True when the run found no problem.</summary>
    public bool Succeeded => Findings.Count == 0;
}
