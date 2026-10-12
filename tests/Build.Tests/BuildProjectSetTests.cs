// SPDX-License-Identifier: Apache-2.0
using System.Xml.Linq;

namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-002 AC-002 and AC-003 (BUG-BUILD-002): verifies that the Nuke Compile and Restore targets use one project set,
/// so no project that Compile builds with <c>--no-restore</c> is left unrestored (NETSDK1004 on the 1.x alias projects under
/// <c>src/Compatibility</c>, which are outside <c>QBrainAi.sln</c>). Build-infrastructure test, accountable through TR only.
/// Uses the repository's own <c>src/</c>, <c>tests/</c>, and <c>build/</c> files; no fixtures or mocks.
/// </summary>
public sealed class BuildProjectSetTests
{
    /// <summary>
    /// <c>Build.GetBuildProjectFiles</c> returns every <c>*.csproj</c> under <c>src/</c> and <c>tests/</c> except
    /// <c>Build.Tests</c>, the Compatibility alias projects included (TR-MCP-NUKE-002 AC-002; BUG-BUILD-002).
    /// </summary>
    [Fact]
    public void GetBuildProjectFiles_CoversEverySourceAndTestProject_ExceptBuildTests()
    {
        var root = FindRepositoryRoot();
        var source = Path.Combine(root, "src");
        var tests = Path.Combine(root, "tests");
        var expected = Directory.EnumerateFiles(source, "*.csproj", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(tests, "*.csproj", SearchOption.AllDirectories))
            .Where(path => !path.EndsWith(Path.Combine("Build.Tests", "Build.Tests.csproj"), StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var actual = Build.GetBuildProjectFiles(source, tests);

        Assert.Equal(expected, actual);
        Assert.Contains(actual, path => path.EndsWith(Path.Combine("SharpNinja.McpServer.Client", "SharpNinja.McpServer.Client.csproj"), StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(actual, path => path.EndsWith(Path.Combine("Build.Tests", "Build.Tests.csproj"), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// The Restore and Compile targets both draw their projects from <c>GetBuildProjectFiles</c>, so Restore covers every project
    /// Compile builds (TR-MCP-NUKE-002 AC-002 and AC-003; BUG-BUILD-002).
    /// </summary>
    [Fact]
    public void RestoreAndCompile_UseTheSharedProjectSet()
    {
        var root = FindRepositoryRoot();
        var restore = File.ReadAllText(Path.Combine(root, "build", "Build.Restore.cs"));
        var compile = File.ReadAllText(Path.Combine(root, "build", "Build.Compile.cs"));

        Assert.Contains("GetBuildProjectFiles(SourceDirectory, TestsDirectory)", restore, StringComparison.Ordinal);
        Assert.Contains("GetBuildProjectFiles(SourceDirectory, TestsDirectory)", compile, StringComparison.Ordinal);
    }

    /// <summary>
    /// Operator directive 2026-10-12 ("The Directory.Build.props need to be located where it will not apply to the Nuke
    /// project"): the repository root holds no <c>Directory.Build.props</c>; the product properties live in
    /// <c>src/Directory.Build.props</c>, and <c>tests/Directory.Build.props</c> imports that file. Reads the repository files.
    /// </summary>
    [Fact]
    public void ProductPropsLiveUnderSrcAndTestsNotAtRepositoryRoot()
    {
        var root = FindRepositoryRoot();

        Assert.False(File.Exists(Path.Combine(root, "Directory.Build.props")), "the repository root must not hold Directory.Build.props");
        var productProps = XDocument.Load(Path.Combine(root, "src", "Directory.Build.props"));
        Assert.Equal("true", UnconditionalProperty(productProps, "TreatWarningsAsErrors"));
        var testProps = XDocument.Load(Path.Combine(root, "tests", "Directory.Build.props"));
        var imports = testProps.Root!.Elements("Import")
            .Select(element => ((string?)element.Attribute("Project") ?? string.Empty).Replace('\\', '/'))
            .ToArray();
        Assert.Contains(imports, project => project.EndsWith("../src/Directory.Build.props", StringComparison.Ordinal));
    }

    /// <summary>
    /// Operator directive 2026-10-12: <c>build/Directory.Build.props</c> ends MSBuild's upward search for the Nuke project, so
    /// neither the product properties nor an enclosing checkout's <c>Directory.Build.props</c> (worktrees nest inside the main
    /// checkout under <c>.claude/worktrees</c>) apply to <c>build/_build.csproj</c>. The stop file imports and sets nothing.
    /// </summary>
    [Fact]
    public void NukeProjectPropsSearchStopsAtTheBuildFolder()
    {
        var stop = XDocument.Load(Path.Combine(FindRepositoryRoot(), "build", "Directory.Build.props"));

        Assert.Empty(stop.Root!.Elements());
    }

    /// <summary>
    /// TR-MCP-QUALITY-001 ("TreatWarningsAsErrors false is not approved. The build project warning bypass must remain
    /// removed"): with the product properties out of reach, <c>build/_build.csproj</c> sets TreatWarningsAsErrors itself, and
    /// ImplicitUsings, which its sources rely on. Reads the project file.
    /// </summary>
    [Fact]
    public void NukeProjectKeepsWarningsAsErrorsOnItsOwn()
    {
        var project = XDocument.Load(Path.Combine(FindRepositoryRoot(), "build", "_build.csproj"));

        Assert.Equal("true", UnconditionalProperty(project, "TreatWarningsAsErrors"));
        Assert.Equal("enable", UnconditionalProperty(project, "ImplicitUsings"));
    }

    /// <summary>Returns the last value an unconditional PropertyGroup assigns to <paramref name="name"/>, or null.</summary>
    private static string? UnconditionalProperty(XDocument project, string name) =>
        project.Root!.Elements("PropertyGroup")
            .Where(group => group.Attribute("Condition") is null)
            .Elements(name)
            .Select(element => element.Value.Trim())
            .LastOrDefault();

    /// <summary>Finds the repository root by the <c>.nuke/parameters.json</c> file.</summary>
    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, ".nuke", "parameters.json")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new InvalidOperationException(".nuke/parameters.json not found above " + AppContext.BaseDirectory);
    }
}
