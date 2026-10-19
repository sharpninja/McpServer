using System.Security.Cryptography;
using System.Text;

namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-017 (AC-1, AC-2, AC-6 and the wiring of AC-3 to AC-5): Contract scenarios and assertions for
/// <see cref="IArchitectureDiagramsRunner"/>. Each scenario builds a temporary repository and a recording fake
/// of the Structurizr CLI and Chromium boundary; expectations come from the acceptance criteria. The same
/// assertions run against the scripted mock (BDPv4 mock gate), wrong mocks (negative controls), and the
/// production runner.
/// </summary>
internal static class ArchitectureDiagramsRunnerContract
{
    /// <summary>Every scenario name, in the order the contract checks them.</summary>
    internal static readonly string[] ScenarioNames =
        ["validation-fails", "inspection-errors", "render", "deterministic", "whatif", "render-failure", "page-and-mermaid"];

    /// <summary>Repository-relative workspace file every scenario contains.</summary>
    internal const string WorkspaceFile = ArchitectureDiagramPaths.WorkspaceDirectory + "/workspace.dsl";

    /// <summary>Compliant render produced by the fake tooling for view A.</summary>
    internal static readonly string ToolSvgA = ArchitectureDiagramRulesContract.Svg(2000, 1500, [24, 33.6]);

    /// <summary>Compliant render produced by the fake tooling for view B.</summary>
    internal static readonly string ToolSvgB = ArchitectureDiagramRulesContract.Svg(1800, 1400, [24, 33.6]);

    /// <summary>Compliant but outdated render of view A committed before the run.</summary>
    internal static readonly string OldSvgA = ArchitectureDiagramRulesContract.Svg(1990, 1500, [24, 33.6]);

    /// <summary>Render that exceeds a letter page at 8 to 16 pt text.</summary>
    internal static readonly string OversizeSvg = ArchitectureDiagramRulesContract.Svg(4000, 3000, [24, 33.6]);

    /// <summary>Builds the exported workspace JSON the fake tooling returns.</summary>
    internal static string Json(string[] keys, int inspectionErrors = 0)
    {
        var views = string.Join(", ", keys.Select(key => "{ \"key\": \"" + key + "\" }"));
        var properties = inspectionErrors > 0 ? ", \"properties\": { \"structurizr.inspection.error\": \"" + inspectionErrors + "\" }" : string.Empty;
        return "{ \"name\": \"scenario\", \"views\": { \"customViews\": [ " + views + " ] }" + properties + " }";
    }

    /// <summary>Repository-relative rendered path of a view key.</summary>
    internal static string Rendered(string key) => ArchitectureDiagramPaths.RenderedDirectory + "/" + key + ".svg";

    /// <summary>Runs one named scenario against the runner built by <paramref name="factory"/> and asserts its contract.</summary>
    internal static void AssertScenario(string name, Func<RecordingTooling, IArchitectureDiagramsRunner> factory)
    {
        using var scenario = RunnerScenario.Create(name);
        var runner = factory(scenario.Tooling);
        var before = scenario.HashRendered();
        var report = runner.Run(scenario.Request);
        switch (name)
        {
            case "validation-fails":
                ArchitectureDiagramRulesContract.AssertFindings([(ArchitectureDiagramFindingCodes.ValidationFailed, WorkspaceFile)], report.Findings);
                Assert.Empty(scenario.Tooling.RenderCalls);
                Assert.Equal(before, scenario.HashRendered());
                break;
            case "inspection-errors":
                ArchitectureDiagramRulesContract.AssertFindings([(ArchitectureDiagramFindingCodes.InspectionErrors, WorkspaceFile)], report.Findings);
                Assert.Empty(scenario.Tooling.RenderCalls);
                Assert.Equal(before, scenario.HashRendered());
                break;
            case "render":
                AssertRendered(scenario, report);
                break;
            case "deterministic":
                AssertRendered(scenario, report);
                var first = scenario.HashRendered();
                var second = runner.Run(scenario.Request);
                Assert.True(second.Succeeded, "Second run failed: " + string.Join("; ", second.Findings));
                Assert.Equal(first, scenario.HashRendered());
                Assert.Equal(ToolSvgA, scenario.Read(Rendered("A")));
                break;
            case "whatif":
                ArchitectureDiagramRulesContract.AssertFindings(
                    [(ArchitectureDiagramFindingCodes.OrphanRender, Rendered("Stale")), (ArchitectureDiagramFindingCodes.MissingRender, "B")],
                    report.Findings);
                Assert.Empty(scenario.Tooling.RenderCalls);
                Assert.Equal(1, scenario.Tooling.ValidateCalls);
                Assert.Equal(before, scenario.HashRendered());
                Assert.Contains("WHATIF render A -> " + Rendered("A"), report.Actions);
                Assert.Contains("WHATIF render B -> " + Rendered("B"), report.Actions);
                Assert.Contains("WHATIF remove " + Rendered("Stale"), report.Actions);
                break;
            case "render-failure":
                ArchitectureDiagramRulesContract.AssertFindings(
                    [(ArchitectureDiagramFindingCodes.RenderFailed, "B"), (ArchitectureDiagramFindingCodes.MissingRender, "B")],
                    report.Findings);
                Assert.Equal(ToolSvgA, scenario.Read(Rendered("A")));
                break;
            case "page-and-mermaid":
                ArchitectureDiagramRulesContract.AssertFindings(
                    [(ArchitectureDiagramFindingCodes.PageOverflow, "A"), (ArchitectureDiagramFindingCodes.MermaidFence, "docs/m.md:1")],
                    report.Findings);
                break;
            default:
                throw new InvalidOperationException("Unknown scenario " + name);
        }
    }

    private static void AssertRendered(RunnerScenario scenario, ArchitectureDiagramsReport report)
    {
        Assert.True(report.Succeeded, "Run failed: " + string.Join("; ", report.Findings));
        Assert.Equal(ToolSvgA, scenario.Read(Rendered("A")));
        Assert.Equal(ToolSvgB, scenario.Read(Rendered("B")));
        Assert.False(scenario.Exists(Rendered("Stale")), "Stale render was not removed.");
        var renderCall = Assert.Single(scenario.Tooling.RenderCalls);
        Assert.Equal(new[] { "A", "B" }, renderCall);
        Assert.Contains("validate " + WorkspaceFile, report.Actions);
        Assert.Contains("render A -> " + Rendered("A"), report.Actions);
        Assert.Contains("render B -> " + Rendered("B"), report.Actions);
        Assert.Contains("remove " + Rendered("Stale"), report.Actions);
    }

    /// <summary>Recording fake of the Structurizr CLI and Chromium boundary.</summary>
    internal sealed class RecordingTooling : IArchitectureDiagramTooling
    {
        /// <summary>Result returned by <see cref="Validate"/>.</summary>
        public ArchitectureToolResult ValidateResult { get; set; } = new(0, "The workspace is valid.");

        /// <summary>JSON returned by <see cref="ExportWorkspaceJson"/>.</summary>
        public string WorkspaceJson { get; set; } = Json(["A"]);

        /// <summary>SVG returned per view key by <see cref="RenderViews"/>; a key without an entry fails to render.</summary>
        public Dictionary<string, string> Svgs { get; } = new(StringComparer.Ordinal);

        /// <summary>Number of validate calls.</summary>
        public int ValidateCalls { get; private set; }

        /// <summary>Number of export calls.</summary>
        public int ExportCalls { get; private set; }

        /// <summary>View keys of each render call.</summary>
        public List<IReadOnlyList<string>> RenderCalls { get; } = [];

        /// <inheritdoc />
        public ArchitectureToolResult Validate(string workspaceDirectory)
        {
            ValidateCalls++;
            return ValidateResult;
        }

        /// <inheritdoc />
        public string ExportWorkspaceJson(string workspaceDirectory)
        {
            ExportCalls++;
            return WorkspaceJson;
        }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, string> RenderViews(string workspaceDirectory, IReadOnlyList<string> viewKeys)
        {
            RenderCalls.Add(viewKeys.ToArray());
            return viewKeys.Where(Svgs.ContainsKey).ToDictionary(key => key, key => Svgs[key], StringComparer.Ordinal);
        }
    }

    /// <summary>A temporary repository for one scenario.</summary>
    internal sealed class RunnerScenario : IDisposable
    {
        private RunnerScenario(string name, string root, bool whatIf)
        {
            Name = name;
            Root = root;
            WhatIf = whatIf;
        }

        /// <summary>Scenario name.</summary>
        public string Name { get; }

        /// <summary>Absolute temporary repository root.</summary>
        public string Root { get; }

        /// <summary>True for the WhatIf scenario.</summary>
        public bool WhatIf { get; }

        /// <summary>Recording tooling configured for the scenario.</summary>
        public RecordingTooling Tooling { get; } = new();

        /// <summary>Tracked files of the scenario repository.</summary>
        public List<string> Tracked { get; } = [];

        /// <summary>Request for the scenario.</summary>
        public ArchitectureDiagramsRequest Request => new(Root, Tracked, WhatIf);

        /// <summary>Creates the repository, tooling and tracked list of a named scenario.</summary>
        public static RunnerScenario Create(string name)
        {
            var root = Path.Combine(Path.GetTempPath(), "c4-runner-" + name + "-" + Guid.NewGuid().ToString("N"));
            var scenario = new RunnerScenario(name, root, name == "whatif");
            scenario.WriteTracked(WorkspaceFile, "workspace { }\n");
            File.WriteAllText(Path.Combine(root, "scenario.txt"), name);
            switch (name)
            {
                case "validation-fails":
                    scenario.Tooling.ValidateResult = new(1, "Error: workspace.dsl line 3: unexpected token");
                    scenario.Tooling.WorkspaceJson = Json(["A"]);
                    scenario.Tooling.Svgs["A"] = ToolSvgA;
                    scenario.WriteUntracked(Rendered("A"), ToolSvgA);
                    scenario.WriteTracked("docs/index.md", "![A](architecture/c4/rendered/A.svg)\nView: A\n");
                    break;
                case "inspection-errors":
                    scenario.Tooling.WorkspaceJson = Json(["A"], inspectionErrors: 2);
                    scenario.Tooling.Svgs["A"] = ToolSvgA;
                    scenario.WriteUntracked(Rendered("A"), ToolSvgA);
                    scenario.WriteTracked("docs/index.md", "![A](architecture/c4/rendered/A.svg)\nView: A\n");
                    break;
                case "render":
                case "deterministic":
                case "whatif":
                    scenario.Tooling.WorkspaceJson = Json(["A", "B"]);
                    scenario.Tooling.Svgs["A"] = ToolSvgA;
                    scenario.Tooling.Svgs["B"] = ToolSvgB;
                    scenario.WriteUntracked(Rendered("A"), OldSvgA);
                    scenario.WriteUntracked(Rendered("Stale"), ToolSvgA);
                    scenario.WriteTracked("docs/index.md", "![A](architecture/c4/rendered/A.svg)\nView: A\n\n![B](architecture/c4/rendered/B.svg)\nView: B\n");
                    break;
                case "render-failure":
                    scenario.Tooling.WorkspaceJson = Json(["A", "B"]);
                    scenario.Tooling.Svgs["A"] = ToolSvgA;
                    scenario.WriteTracked("docs/index.md", "![A](architecture/c4/rendered/A.svg)\n![B](architecture/c4/rendered/B.svg)\n");
                    break;
                case "page-and-mermaid":
                    scenario.Tooling.WorkspaceJson = Json(["A"]);
                    scenario.Tooling.Svgs["A"] = OversizeSvg;
                    scenario.WriteTracked("docs/m.md", "```mermaid\nflowchart TD\n```\n\n![A](architecture/c4/rendered/A.svg)\n");
                    break;
                default:
                    throw new InvalidOperationException("Unknown scenario " + name);
            }

            return scenario;
        }

        /// <summary>Reads a repository-relative file.</summary>
        public string Read(string relativePath) => File.ReadAllText(Path.Combine(Root, relativePath));

        /// <summary>True when a repository-relative file exists.</summary>
        public bool Exists(string relativePath) => File.Exists(Path.Combine(Root, relativePath));

        /// <summary>SHA-256 over the sorted names and bytes of every file in the rendered directory.</summary>
        public string HashRendered()
        {
            var directory = Path.Combine(Root, ArchitectureDiagramPaths.RenderedDirectory);
            if (!Directory.Exists(directory))
            {
                return "empty";
            }

            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            foreach (var file in Directory.GetFiles(directory).OrderBy(path => path, StringComparer.Ordinal))
            {
                hash.AppendData(Encoding.UTF8.GetBytes(Path.GetFileName(file)));
                hash.AppendData(File.ReadAllBytes(file));
            }

            return Convert.ToHexString(hash.GetHashAndReset());
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }

        private void WriteTracked(string relativePath, string content)
        {
            WriteUntracked(relativePath, content);
            Tracked.Add(relativePath);
        }

        private void WriteUntracked(string relativePath, string content)
        {
            var path = Path.Combine(Root, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, content);
        }
    }
}
