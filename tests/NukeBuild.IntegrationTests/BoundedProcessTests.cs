// SPDX-License-Identifier: Apache-2.0
using NukeBuild.Tests;

namespace NukeBuild.IntegrationTests;

/// <summary>
/// TR-MCP-NUKE-009 (BUG-BUILD-007): checks that <see cref="BoundedProcess"/> stays the one way the build's tests (Build.Tests
/// and NukeBuild.IntegrationTests) run a child process with redirected output. Build integration test, accountable through TR only:
/// it scans the physical checkout's test sources; the process cases that start real <c>pwsh</c> children are the
/// infrastructure half of this class in Build.Tests.
/// </summary>
/// <remarks>
/// <para>2026-11-07, TR-MCP-NUKE-009 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of BoundedProcessTests, split from its five infrastructure cases in Build.Tests into the integration-owned Build.IntegrationTests (root namespace NukeBuild.IntegrationTests), so the infrastructure-owned Build.Tests holds no Category=Integration case (slice S4, hostile validation finding R14-F4; operator 2026-11-06: "Move them (Recommended)"). BuildTestsSources_ReadChildOutputOnlyThroughBoundedProcess, IsBuildOutput and FindRepositoryRoot, which only it uses, moved; the halves share no private helper; BoundedProcess is linked from Build.Tests. The scan now covers both folders of the build's tests, so the sources that moved with this slice stay under the AC-006 check.</para>
/// <para>2026-11-07, TR-MCP-NUKE-009 AC-006: the summary now describes this half (the source scan, an integration case) instead of the copied infrastructure text; documentation only (ClaudeCode review of slice S4).</para>
/// <para>2026-11-07, TR-MCP-NUKE-009 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: Build.IntegrationTests, the project the entries above name, is now NukeBuild.IntegrationTests, renamed to its root namespace (operator 2026-11-07: "Project names and root namespaces for the project should match, so fix the project name."); the summary names the new project and the scan reads its folder; the case is otherwise unchanged.</para>
/// </remarks>
public sealed class BoundedProcessTests
{
    /// <summary>
    /// TR-MCP-NUKE-009 AC-006: no source of the build's tests (Build.Tests and NukeBuild.IntegrationTests) other than
    /// <see cref="BoundedProcess"/> reads a child's redirected output or waits for a child to exit, so every child process
    /// those suites start is bounded. Scans the C# sources under tests/Build.Tests
    /// and tests/NukeBuild.IntegrationTests (build output excluded) for direct output reads and exit waits.
    /// </summary>
    /// <remarks>
    /// <para>TR-MCP-NUKE-019: integration because this case resolves or reads the physical checkout.</para>
    /// <para>2026-11-07, TR-MCP-NUKE-009 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the scan reads tests/Build.IntegrationTests as well as tests/Build.Tests, because slice S4 split the build's tests across the two projects; offending lines are named relative to the repository root; the patterns and the owner files are unchanged.</para>
    /// <para>2026-11-07, TR-MCP-NUKE-009 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the scan reads tests/NukeBuild.IntegrationTests, the folder of the renamed Build.IntegrationTests (operator 2026-11-07: "Project names and root namespaces for the project should match, so fix the project name."); the patterns, the owner files and the assertion are unchanged.</para>
    /// </remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void BuildTestsSources_ReadChildOutputOnlyThroughBoundedProcess()
    {
        var root = FindRepositoryRoot();
        string[] directories = [Path.Combine(root, "tests", "Build.Tests"), Path.Combine(root, "tests", "NukeBuild.IntegrationTests")];
        string[] patterns = [".StandardOutput", ".StandardError", "WaitForExit", "BeginOutputReadLine", "BeginErrorReadLine"];
        string[] owners = ["BoundedProcess.cs", "BoundedProcessTests.cs"];
        var offendingLines = directories
            .SelectMany(directory => Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories)
                .Where(path => !IsBuildOutput(Path.GetRelativePath(directory, path))))
            .Where(path => !owners.Contains(Path.GetFileName(path), StringComparer.Ordinal))
            .SelectMany(path => File.ReadLines(path).Select((line, index) => new { Path = path, Line = line, LineNumber = index + 1 }))
            .Where(entry => patterns.Any(pattern => entry.Line.Contains(pattern, StringComparison.Ordinal)))
            .Select(entry => $"{Path.GetRelativePath(root, entry.Path)}:{entry.LineNumber}: {entry.Line.Trim()}")
            .ToArray();

        Assert.True(
            offendingLines.Length == 0,
            "Read child output and wait for children only through BoundedProcess (TR-MCP-NUKE-009 AC-006):"
                + Environment.NewLine
                + string.Join(Environment.NewLine, offendingLines));
    }

    private static bool IsBuildOutput(string relativePath)
    {
        var first = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
        return first is "bin" or "obj";
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "QBrainAi.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root containing QBrainAi.sln.");
    }
}
