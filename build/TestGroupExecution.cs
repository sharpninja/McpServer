using System.Xml.Linq;

/// <summary>TR-MCP-NUKE-020 AC-1 and AC-5: one TestGroups run: the lane-shaped run record and the classes it names.</summary>
/// <param name="Run">The project, path, filter, log line and TRX name, as the lane composes them.</param>
/// <param name="Classes">The classes this run's groups named, in first-seen order.</param>
internal sealed record TestGroupRun(IntegrationLaneRun Run, IReadOnlyList<string> Classes);

/// <summary>
/// TR-MCP-NUKE-020 AC-1, AC-2, AC-4 and AC-5: pure composition and result checks of the TestGroups target. Each
/// <c>--test-groups</c> value is <c>&lt;test project&gt;:&lt;Class&gt;[,&lt;Class&gt;...]</c>; groups that name one project merge
/// into one run in first-seen order, so each selected project writes one TRX. The filter is the lane's host platform
/// clause (<see cref="IntegrationLaneFilter.PlatformClause"/>) and class clause
/// (<see cref="IntegrationLaneClassFilter.ClassClause"/>), with no category clause; the run record, log line and TRX name
/// follow the lane's <see cref="IntegrationLaneRun"/>. Every refusal happens before any test runs.
/// </summary>
internal static class TestGroupExecution
{
    /// <summary>Composes the runs for the given groups against the solution's projects on one host.</summary>
    /// <param name="groups">The <c>--test-groups</c> values, in order.</param>
    /// <param name="projects">The solution's project names and paths.</param>
    /// <param name="host">The host the runs execute on.</param>
    /// <returns>One run per selected project, in the order the groups first name them.</returns>
    /// <exception cref="InvalidOperationException">No group, a malformed group, an unknown or blank project, or a class
    /// name that is empty or not a plain class name.</exception>
    internal static IReadOnlyList<TestGroupRun> Compose(IReadOnlyList<string> groups, IReadOnlyList<(string Name, string Path)> projects, IntegrationLaneHost host)
    {
        ArgumentNullException.ThrowIfNull(groups);
        ArgumentNullException.ThrowIfNull(projects);
        if (groups.Count == 0)
            throw new InvalidOperationException("TestGroups needs at least one --test-groups value '<test project>:<Class>[,<Class>...]'.");

        var platform = IntegrationLaneFilter.PlatformClause(host);
        var order = new List<string>();
        var classes = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var raw in groups)
        {
            var group = raw ?? string.Empty;
            var separator = group.IndexOf(':', StringComparison.Ordinal);
            if (separator < 0)
                throw new InvalidOperationException($"Test group '{group}' must be '<test project>:<Class>[,<Class>...]'.");

            var project = group[..separator].Trim();
            if (project.Length == 0)
                throw new InvalidOperationException($"Test group '{group}' names no test project.");
            if (!projects.Any(entry => string.Equals(entry.Name, project, StringComparison.Ordinal)))
                throw new InvalidOperationException($"Test group '{group}' names test project '{project}', which the solution does not contain.");

            var names = group[(separator + 1)..].Split(',').Select(name => name.Trim()).ToArray();
            if (names.Any(name => name.Length == 0))
                throw new InvalidOperationException($"Test group '{group}' has an empty class name.");
            foreach (var name in names)
            {
                try
                {
                    IntegrationLaneClassFilter.ClassClause([name]);
                }
                catch (ArgumentException)
                {
                    throw new InvalidOperationException($"Test group '{group}' names class '{name}', which is not a plain class name.");
                }
            }

            if (!classes.TryGetValue(project, out var selected))
            {
                selected = [];
                classes[project] = selected;
                order.Add(project);
            }

            foreach (var name in names.Where(name => !selected.Contains(name, StringComparer.Ordinal)))
                selected.Add(name);
        }

        return order.Select(name =>
        {
            var path = projects.First(entry => string.Equals(entry.Name, name, StringComparison.Ordinal)).Path;
            var filter = platform + "&" + IntegrationLaneClassFilter.ClassClause(classes[name]);
            return new TestGroupRun(
                new IntegrationLaneRun(name, path, filter, $"Test group filter for {name}: {filter}", name + ".trx"),
                classes[name].ToArray());
        }).ToArray();
    }

    /// <summary>TR-MCP-NUKE-020 AC-1: TestGroups always writes TRX receipts, so it requires a safe --test-run-id.</summary>
    /// <param name="testRunId">The raw <c>--test-run-id</c> value.</param>
    /// <returns>The run id.</returns>
    /// <exception cref="InvalidOperationException">The id is blank or not a single path segment.</exception>
    internal static string RequireRunId(string testRunId) =>
        SessionLifeUnitGateReports.NormalizeRunId(testRunId)
            ?? throw new InvalidOperationException("TestGroups requires --test-run-id, so that every selected project writes its TRX receipt.");

    /// <summary>
    /// TR-MCP-NUKE-020 AC-5: every named class ran at least one case. A result counts for a class when its test name holds
    /// the class as a whole dot-bounded segment, as the filter's <c>FullyQualifiedName~.Class.</c> selects it.
    /// </summary>
    /// <param name="project">The project whose TRX is checked.</param>
    /// <param name="classes">The classes the project's groups named.</param>
    /// <param name="xml">The complete TRX content.</param>
    /// <returns>Null when every class ran a case; otherwise the failure, naming each class that ran none.</returns>
    internal static string? CheckClasses(string project, IReadOnlyList<string> classes, string xml)
    {
        ArgumentNullException.ThrowIfNull(classes);
        string[] testNames;
        try
        {
            var document = XDocument.Parse(xml);
            // A result's display name normally starts with its class; a TestMethod definition names the class itself,
            // which still holds when a display name is customized.
            testNames = document.Descendants()
                .Where(element => element.Name.LocalName == "UnitTestResult")
                .Select(element => (string?)element.Attribute("testName") ?? string.Empty)
                .Concat(document.Descendants()
                    .Where(element => element.Name.LocalName == "TestMethod")
                    .Select(element => ((string?)element.Attribute("className") ?? string.Empty) + "."))
                .ToArray();
        }
        catch (System.Xml.XmlException error)
        {
            return $"Test group project '{project}' wrote unreadable TRX evidence, so classes {string.Join(", ", classes)} are unproven: {error.Message}";
        }

        var missing = classes.Where(name => !testNames.Any(test => test.Contains("." + name + ".", StringComparison.Ordinal))).ToArray();
        return missing.Length == 0
            ? null
            : $"Test group project '{project}' ran no case of class {string.Join(", ", missing)}.";
    }
}
