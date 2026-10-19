namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-017 (AC-1, AC-2, AC-6 and the wiring of AC-3 to AC-5), BDPv4 mock gate: proves the
/// <see cref="ArchitectureDiagramsRunnerContract"/> scenario assertions accept a scripted runner that produces
/// the contract-derived effects, and reject deliberately wrong runners (negative controls), before any
/// production runner code is written.
/// </summary>
public sealed class ArchitectureDiagramsRunnerMockGateTests
{
    /// <summary>TR-MCP-NUKE-017: every scenario passes against the scripted runner.</summary>
    /// <param name="scenario">Scenario name from <see cref="ArchitectureDiagramsRunnerContract.ScenarioNames"/>.</param>
    [Theory]
    [InlineData("validation-fails")]
    [InlineData("inspection-errors")]
    [InlineData("render")]
    [InlineData("deterministic")]
    [InlineData("whatif")]
    [InlineData("render-failure")]
    [InlineData("page-and-mermaid")]
    public void ScriptedRunner_PassesScenario(string scenario)
        => ArchitectureDiagramsRunnerContract.AssertScenario(scenario, tooling => new ScriptedRunner(tooling, Fault.None));

    /// <summary>TR-MCP-NUKE-017: the theory above covers every contract scenario.</summary>
    [Fact]
    public void ScriptedRunnerTheory_CoversEveryScenario()
        => Assert.Equal(7, ArchitectureDiagramsRunnerContract.ScenarioNames.Length);

    /// <summary>
    /// TR-MCP-NUKE-017 negative controls: each wrong runner is rejected by the scenario it violates
    /// (AC-6 WhatIf writes, AC-1 ignored validation, AC-2 stale render kept, AC-2 nondeterministic render,
    /// AC-1 render despite inspection errors, AC-2 hidden render failure, AC-5 missed page check).
    /// </summary>
    /// <param name="scenario">Scenario the fault violates.</param>
    /// <param name="fault">Injected fault.</param>
    [Theory]
    [InlineData("whatif", Fault.WritesInWhatIf)]
    [InlineData("validation-fails", Fault.IgnoresValidation)]
    [InlineData("render", Fault.KeepsStale)]
    [InlineData("deterministic", Fault.StampsTime)]
    [InlineData("inspection-errors", Fault.RendersDespiteInspection)]
    [InlineData("render-failure", Fault.HidesRenderFailure)]
    [InlineData("page-and-mermaid", Fault.MissesPageCheck)]
    public void WrongRunner_IsRejected(string scenario, Fault fault)
        => Assert.ThrowsAny<Xunit.Sdk.XunitException>(
            () => ArchitectureDiagramsRunnerContract.AssertScenario(scenario, tooling => new ScriptedRunner(tooling, fault)));

    /// <summary>Faults injected into the scripted runner for negative controls.</summary>
    public enum Fault
    {
        /// <summary>No fault: the contract-derived behavior.</summary>
        None,

        /// <summary>Writes a render in a WhatIf run.</summary>
        WritesInWhatIf,

        /// <summary>Reports nothing for a failed validation and renders anyway.</summary>
        IgnoresValidation,

        /// <summary>Leaves a render that has no view.</summary>
        KeepsStale,

        /// <summary>Adds a changing timestamp to every render.</summary>
        StampsTime,

        /// <summary>Renders although the workspace has inspection errors.</summary>
        RendersDespiteInspection,

        /// <summary>Reports a missing render but not the render failure.</summary>
        HidesRenderFailure,

        /// <summary>Reports the Mermaid fence but not the page overflow.</summary>
        MissesPageCheck,
    }

    /// <summary>
    /// Scripted mock runner: performs the contract-derived effects of the scenario named in scenario.txt
    /// through the recording tooling, optionally with one injected fault.
    /// </summary>
    private sealed class ScriptedRunner(ArchitectureDiagramsRunnerContract.RecordingTooling tooling, Fault fault) : IArchitectureDiagramsRunner
    {
        private static int stamp;

        public ArchitectureDiagramsReport Run(ArchitectureDiagramsRequest request)
        {
            var name = File.ReadAllText(Path.Combine(request.RepositoryRoot, "scenario.txt"));
            var workspace = Path.Combine(request.RepositoryRoot, ArchitectureDiagramPaths.WorkspaceDirectory);
            var actions = new List<string> { "validate " + ArchitectureDiagramsRunnerContract.WorkspaceFile };
            var findings = new List<ArchitectureDiagramFinding>();
            tooling.Validate(workspace);
            switch (name)
            {
                case "validation-fails":
                    if (fault == Fault.IgnoresValidation)
                    {
                        tooling.RenderViews(workspace, ["A"]);
                        break;
                    }

                    findings.Add(new(ArchitectureDiagramFindingCodes.ValidationFailed, ArchitectureDiagramsRunnerContract.WorkspaceFile, "exit 1"));
                    break;
                case "inspection-errors":
                    tooling.ExportWorkspaceJson(workspace);
                    findings.Add(new(ArchitectureDiagramFindingCodes.InspectionErrors, ArchitectureDiagramsRunnerContract.WorkspaceFile, "2 inspection errors"));
                    if (fault == Fault.RendersDespiteInspection)
                    {
                        tooling.RenderViews(workspace, ["A"]);
                    }

                    break;
                case "render":
                case "deterministic":
                    tooling.ExportWorkspaceJson(workspace);
                    var svgs = tooling.RenderViews(workspace, ["A", "B"]);
                    foreach (var (key, svg) in svgs)
                    {
                        var content = fault == Fault.StampsTime ? svg + "<!-- " + Interlocked.Increment(ref stamp) + " -->" : svg;
                        Write(request.RepositoryRoot, ArchitectureDiagramsRunnerContract.Rendered(key), content);
                        actions.Add("render " + key + " -> " + ArchitectureDiagramsRunnerContract.Rendered(key));
                    }

                    if (fault != Fault.KeepsStale)
                    {
                        File.Delete(Path.Combine(request.RepositoryRoot, ArchitectureDiagramsRunnerContract.Rendered("Stale")));
                    }

                    actions.Add("remove " + ArchitectureDiagramsRunnerContract.Rendered("Stale"));
                    break;
                case "whatif":
                    tooling.ExportWorkspaceJson(workspace);
                    actions.Add("WHATIF render A -> " + ArchitectureDiagramsRunnerContract.Rendered("A"));
                    actions.Add("WHATIF render B -> " + ArchitectureDiagramsRunnerContract.Rendered("B"));
                    actions.Add("WHATIF remove " + ArchitectureDiagramsRunnerContract.Rendered("Stale"));
                    if (fault == Fault.WritesInWhatIf)
                    {
                        Write(request.RepositoryRoot, ArchitectureDiagramsRunnerContract.Rendered("A"), ArchitectureDiagramsRunnerContract.ToolSvgA);
                    }

                    findings.Add(new(ArchitectureDiagramFindingCodes.OrphanRender, ArchitectureDiagramsRunnerContract.Rendered("Stale"), "no view"));
                    findings.Add(new(ArchitectureDiagramFindingCodes.MissingRender, "B", "no render"));
                    break;
                case "render-failure":
                    tooling.ExportWorkspaceJson(workspace);
                    foreach (var (key, svg) in tooling.RenderViews(workspace, ["A", "B"]))
                    {
                        Write(request.RepositoryRoot, ArchitectureDiagramsRunnerContract.Rendered(key), svg);
                    }

                    if (fault != Fault.HidesRenderFailure)
                    {
                        findings.Add(new(ArchitectureDiagramFindingCodes.RenderFailed, "B", "Chromium returned no SVG"));
                    }

                    findings.Add(new(ArchitectureDiagramFindingCodes.MissingRender, "B", "no render"));
                    break;
                case "page-and-mermaid":
                    tooling.ExportWorkspaceJson(workspace);
                    foreach (var (key, svg) in tooling.RenderViews(workspace, ["A"]))
                    {
                        Write(request.RepositoryRoot, ArchitectureDiagramsRunnerContract.Rendered(key), svg);
                    }

                    if (fault != Fault.MissesPageCheck)
                    {
                        findings.Add(new(ArchitectureDiagramFindingCodes.PageOverflow, "A", "4000 x 3000 at 0.444"));
                    }

                    findings.Add(new(ArchitectureDiagramFindingCodes.MermaidFence, "docs/m.md:1", "```mermaid"));
                    break;
                default:
                    throw new InvalidOperationException("Unknown scenario " + name);
            }

            return new ArchitectureDiagramsReport(findings, actions);
        }

        private static void Write(string root, string relativePath, string content)
        {
            var path = Path.Combine(root, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, content);
        }
    }
}
