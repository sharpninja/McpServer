using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Nuke.Common.IO;

/// <summary>
/// FR-MCP-107 / TR-MCP-PLAN-001: Writes the session-life lane inventory and canonical TRX paths.
/// </summary>
internal static class SessionLifeUnitGateReports
{
    /// <summary>
    /// FR-MCP-107 / TR-MCP-PLAN-001: Returns a single-segment run id, or null when the parameter was omitted.
    /// </summary>
    /// <param name="testRunId">The raw <c>--test-run-id</c> value.</param>
    /// <returns>The normalized run id, or null when gate reports are not requested.</returns>
    internal static string? NormalizeRunId(string testRunId)
    {
        if (string.IsNullOrWhiteSpace(testRunId))
            return null;

        if (testRunId.IndexOfAny(['/', '\\', ':']) >= 0 || testRunId.Contains("..", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Session-life gate --test-run-id must be a single path segment.");
        }

        return testRunId;
    }

    /// <summary>
    /// FR-MCP-107 / TR-MCP-PLAN-001: Resolves <c>TestResults/&lt;id&gt;/&lt;lane&gt;/&lt;project&gt;</c>.
    /// </summary>
    /// <param name="repositoryRoot">The repository root.</param>
    /// <param name="runId">The gate run id.</param>
    /// <param name="lane">The lane directory name.</param>
    /// <param name="projectName">The selected project name.</param>
    /// <returns>The absolute project report directory.</returns>
    internal static AbsolutePath LaneProjectDirectory(
        AbsolutePath repositoryRoot,
        string runId,
        string lane,
        string projectName) =>
        repositoryRoot / "TestResults" / runId / lane / projectName;

    /// <summary>
    /// FR-MCP-107 / TR-MCP-PLAN-001: Converts an absolute project path to a repository-relative forward-slash path.
    /// </summary>
    /// <param name="repositoryRoot">The repository root.</param>
    /// <param name="projectPath">The absolute project path.</param>
    /// <returns>The repository-relative project path.</returns>
    internal static string RelativePath(AbsolutePath repositoryRoot, AbsolutePath projectPath) =>
        Path.GetRelativePath(repositoryRoot, projectPath).Replace('\\', '/');

    /// <summary>
    /// FR-MCP-107 / TR-MCP-PLAN-001: Writes <c>&lt;lane&gt;/selected-projects.json</c> for the projects Nuke selected.
    /// </summary>
    /// <param name="repositoryRoot">The repository root.</param>
    /// <param name="runId">The gate run id.</param>
    /// <param name="lane">The lane directory name.</param>
    /// <param name="projects">The selected project names and repository-relative project paths.</param>
    internal static void WriteInventory(
        AbsolutePath repositoryRoot,
        string runId,
        string lane,
        IEnumerable<(string Name, string ProjectPath)> projects)
    {
        var inventoryDirectory = repositoryRoot / "TestResults" / runId / lane;
        Directory.CreateDirectory(inventoryDirectory);
        var projectArray = new JsonArray();
        foreach (var project in projects)
        {
            projectArray.Add(new JsonObject
            {
                ["name"] = project.Name,
                ["projectPath"] = project.ProjectPath,
                ["reportPath"] = $"{lane}/{project.Name}/{project.Name}.trx",
            });
        }

        var document = new JsonObject
        {
            ["schemaVersion"] = 1,
            ["runId"] = runId,
            ["scope"] = lane,
            ["generatedAtUtc"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            ["projects"] = projectArray,
        };
        File.WriteAllText(
            inventoryDirectory / "selected-projects.json",
            document.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine);
    }

    /// <summary>
    /// FR-MCP-107 / TR-MCP-PLAN-001: Keeps exactly one canonical TRX in the project report directory.
    /// </summary>
    /// <param name="directory">The project report directory.</param>
    /// <param name="fileName">The canonical TRX file name.</param>
    internal static void EnsureCanonicalTrx(AbsolutePath directory, string fileName)
    {
        var canonical = Path.Combine(directory, fileName);
        var reports = Directory.Exists(directory)
            ? Directory.GetFiles(directory, "*.trx", SearchOption.AllDirectories)
            : Array.Empty<string>();
        var match = reports.FirstOrDefault(path =>
            string.Equals(Path.GetFileName(path), fileName, StringComparison.Ordinal));
        if (match is null)
        {
            throw new FileNotFoundException(
                $"Session-life gate did not find {fileName} under {directory}.",
                canonical);
        }

        if (!string.Equals(match, canonical, StringComparison.Ordinal))
            File.Move(match, canonical, overwrite: true);

        foreach (var extra in Directory.GetFiles(directory, "*.trx", SearchOption.AllDirectories))
        {
            if (!string.Equals(extra, canonical, StringComparison.Ordinal))
                File.Delete(extra);
        }
    }
}
