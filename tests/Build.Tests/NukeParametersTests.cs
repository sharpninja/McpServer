// SPDX-License-Identifier: Apache-2.0
using System.Text.Json;

namespace NukeBuild.Tests;

/// <summary>
/// BUG-BUILD-001 (TEST-NUKE-001 family): verifies that the solution named by the checked-in <c>.nuke/parameters.json</c>
/// exists at the repository root, so Nuke can inject <c>Build.Solution</c> and the Restore target, and every target that
/// depends on it, can run. Reads the repository's own <c>.nuke/parameters.json</c>; no fixtures or mocks.
/// </summary>
public sealed class NukeParametersTests
{
    /// <summary>
    /// The <c>Solution</c> value in <c>.nuke/parameters.json</c> names a solution file that exists at the repository root
    /// (BUG-BUILD-001: the QBrainAi rename left it pointing at the removed <c>McpServer.sln</c>, so Restore threw a
    /// NullReferenceException).
    /// </summary>
    [Fact]
    public void NukeParameters_Solution_NamesExistingSolutionFile()
    {
        var root = FindRepositoryRoot();
        using var parameters = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, ".nuke", "parameters.json")));
        var solution = parameters.RootElement.GetProperty("Solution").GetString();

        Assert.False(string.IsNullOrWhiteSpace(solution), ".nuke/parameters.json has no Solution value.");
        Assert.True(
            File.Exists(Path.Combine(root, solution!)),
            $".nuke/parameters.json names Solution '{solution}', which does not exist at the repository root {root}.");
    }

    /// <summary>Finds the repository root by the <c>.nuke/parameters.json</c> file, independent of the solution name under test.</summary>
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
