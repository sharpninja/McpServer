// SPDX-License-Identifier: Apache-2.0
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
