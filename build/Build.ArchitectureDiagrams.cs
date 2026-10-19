using System.Diagnostics;
using Nuke.Common;
using Serilog;

partial class Build
{
    /// <summary>
    /// TR-MCP-NUKE-017 AC-6: Report what would change and write nothing. Named as PLAN-BUILDWHATIF-001 names it
    /// (--what-if); only ArchitectureDiagrams honors it until TR-MCP-NUKE-005 adds the build.ps1 pass-through.
    /// </summary>
    [Parameter("TR-MCP-NUKE-017 AC-6: report what would change and write nothing (honored by ArchitectureDiagrams).")]
    readonly bool WhatIf = false;

    /// <summary>TR-MCP-NUKE-017 AC-2: Chromium-family browser used to render Structurizr views (default: autodetect).</summary>
    [Parameter("TR-MCP-NUKE-017: Chromium-family browser used to render Structurizr views (default: autodetect).")]
    readonly string ChromiumPath = string.Empty;

    /// <summary>
    /// TR-MCP-NUKE-017 (FR-MCP-BUILD-001): Validates docs/architecture/c4/workspace.dsl with the pinned Structurizr CLI,
    /// renders every view to docs/architecture/c4/rendered/&lt;viewKey&gt;.svg, and fails when any documentation diagram
    /// does not come from that one workspace (Mermaid fences or .mmd files, dangling view links, missing or orphan
    /// renders, renders outside the 8 to 16 pt letter-page standard). --what-if writes nothing.
    /// </summary>
    public Target ArchitectureDiagrams => _ => _
        .Executes(() =>
        {
            var tooling = new StructurizrChromiumTooling(ResolveChromiumPath(ChromiumPath));
            var runner = new ArchitectureDiagramsRunner(tooling, new ArchitectureDiagramRules());
            var report = runner.Run(new ArchitectureDiagramsRequest(RootDirectory, ReadTrackedFiles(RootDirectory), WhatIf));

            foreach (var action in report.Actions)
            {
                Log.Information("{Action}", action);
            }

            foreach (var finding in report.Findings)
            {
                Log.Error("{Code} {Location}: {Message}", finding.Code, finding.Location, finding.Message);
            }

            if (!report.Succeeded)
            {
                throw new InvalidOperationException($"ArchitectureDiagrams failed with {report.Findings.Count} finding(s).");
            }

            Log.Information("ArchitectureDiagrams passed{Mode}.", WhatIf ? " (what-if: nothing written)" : string.Empty);
        });

    private static string ResolveChromiumPath(string configured)
    {
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        string[] candidates = OperatingSystem.IsWindows()
            ?
            [
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "chrome.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft", "Edge", "Application", "msedge.exe"),
            ]
            : ["/usr/bin/chromium", "/usr/bin/chromium-browser", "/usr/bin/google-chrome", "/usr/bin/google-chrome-stable"];
        return candidates.FirstOrDefault(File.Exists)
            ?? throw new InvalidOperationException("No Chromium-family browser found; pass --chromium-path.");
    }

    private static string[] ReadTrackedFiles(string root)
    {
        using var git = Process.Start(new ProcessStartInfo("git")
        {
            ArgumentList = { "-C", root, "ls-files", "-z" },
            RedirectStandardOutput = true,
            UseShellExecute = false,
        }) ?? throw new InvalidOperationException("Could not start git.");
        var output = git.StandardOutput.ReadToEnd();
        git.WaitForExit();
        if (git.ExitCode != 0)
        {
            throw new InvalidOperationException("git ls-files failed with exit code " + git.ExitCode + ".");
        }

        return output.Split('\0', StringSplitOptions.RemoveEmptyEntries);
    }
}
