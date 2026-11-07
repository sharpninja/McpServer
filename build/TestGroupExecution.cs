using System.Xml.Linq;

/// <summary>TR-MCP-NUKE-020 AC-1 and AC-5: one TestGroups run: the lane-shaped run record and the classes it names.</summary>
/// <param name="Run">The project, path, filter, log line and TRX name, as the lane composes them.</param>
/// <param name="Classes">The classes this run's groups named, in first-seen order.</param>
/// <remarks>
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-02</term><description>FR-MCP-BUILD-001; TR-MCP-NUKE-020 AC-1, AC-5: added so the target can check every named class of a project's merged groups against that project's TRX.</description></item>
/// </list>
/// </remarks>
internal sealed record TestGroupRun(IntegrationLaneRun Run, IReadOnlyList<string> Classes);

/// <summary>
/// TR-MCP-NUKE-020 AC-1, AC-2, AC-4 and AC-5: pure composition and result checks of the TestGroups target. Each
/// <c>--test-groups</c> value is <c>&lt;test project&gt;:&lt;Class&gt;[,&lt;Class&gt;...]</c>; groups that name one project merge
/// into one run in first-seen order, so each selected project writes one TRX. The filter is the lane's host platform
/// clause (<see cref="IntegrationLaneFilter.PlatformClause"/>) and class clause
/// (<see cref="IntegrationLaneClassFilter.ClassClause"/>), with no category clause; the run record, log line and TRX name
/// follow the lane's <see cref="IntegrationLaneRun"/>. Every refusal happens before any test runs.
/// </summary>
/// <remarks>
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-02</term><description>FR-MCP-BUILD-001; TR-MCP-NUKE-020 AC-1, AC-2, AC-3: added the TestGroups composition and run-id policy so targeted runs go through Nuke (operator 2026-11-02) and reuse the lane composition.</description></item>
/// <item><term>2026-11-02</term><description>FR-MCP-BUILD-001; TR-MCP-NUKE-020 AC-4, AC-5: added the host platform clause and the per-class result check after a Windows-only class was run on Linux.</description></item>
/// </list>
/// </remarks>
internal static class TestGroupExecution
{
    /// <summary>Composes the runs for the given groups against the solution's projects on one host.</summary>
    /// <param name="groups">The <c>--test-groups</c> values, in order.</param>
    /// <param name="projects">The solution's project names and paths.</param>
    /// <param name="host">The host the runs execute on.</param>
    /// <returns>One run per selected project, in the order the groups first name them.</returns>
    /// <exception cref="InvalidOperationException">No group, a malformed group, an unknown or blank project, or a class
    /// name that is empty or not a plain class name.</exception>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
    /// <item><term>2026-11-02</term><description>FR-MCP-BUILD-001; TR-MCP-NUKE-020 AC-1, AC-2: added; one run per project with the lane's class clause and run record.</description></item>
    /// <item><term>2026-11-02</term><description>FR-MCP-BUILD-001; TR-MCP-NUKE-020 AC-4: added the host parameter and the platform clause so other-platform cases never run.</description></item>
    /// </list>
    /// </remarks>
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
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
    /// <item><term>2026-11-02</term><description>FR-MCP-BUILD-001; TR-MCP-NUKE-020 AC-1: added so a targeted run always leaves TRX receipts.</description></item>
    /// </list>
    /// </remarks>
    internal static string RequireRunId(string testRunId) =>
        SessionLifeUnitGateReports.NormalizeRunId(testRunId)
            ?? throw new InvalidOperationException("TestGroups requires --test-run-id, so that every selected project writes its TRX receipt.");

    /// <summary>
    /// TR-MCP-NUKE-020 AC-5: every named class ran at least one case. A class counts only through an executed result (an
    /// outcome other than NotExecuted) whose test, joined to its definition by test id, belongs to the class: the
    /// definition's TestMethod class name is the class, as the whole name or its last segment ('.' or '+' separated). A
    /// definition with no executed result, a display name and a namespace segment never count; a custom display name
    /// counts through its test's class.
    /// </summary>
    /// <param name="project">The project whose TRX is checked.</param>
    /// <param name="classes">The classes the project's groups named.</param>
    /// <param name="xml">The complete TRX content.</param>
    /// <returns>Null when every class ran a case; otherwise the failure, naming each class that ran none.</returns>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
    /// <item><term>2026-11-02</term><description>FR-MCP-BUILD-001; TR-MCP-NUKE-020 AC-5: added so a misspelled class in a multi-class group fails instead of passing silently.</description></item>
    /// <item><term>2026-11-02</term><description>FR-MCP-BUILD-001; TR-MCP-NUKE-020 AC-5 (appended: a class counts only through an executed result joined to its definition by test id): joins each executed result to its definition and compares the definition's class name, instead of matching text in display names and every definition, because hostile validation finding R9-F1 showed a definition without a result, a display name or a namespace segment satisfied the check (operator 2026-11-02: "Strengthen 5 ACs, fix all 8 (Recommended)").</description></item>
    /// </list>
    /// </remarks>
    internal static string? CheckClasses(string project, IReadOnlyList<string> classes, string xml)
    {
        ArgumentNullException.ThrowIfNull(classes);
        string[] executedClasses;
        try
        {
            var document = XDocument.Parse(xml);
            var classById = document.Descendants()
                .Where(element => element.Name.LocalName == "UnitTest")
                .Select(test => (Id: (string?)test.Attribute("id"), ClassName: (string?)test.Elements().FirstOrDefault(child => child.Name.LocalName == "TestMethod")?.Attribute("className")))
                .Where(test => !string.IsNullOrEmpty(test.Id) && !string.IsNullOrEmpty(test.ClassName))
                .GroupBy(test => test.Id!, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First().ClassName!, StringComparer.Ordinal);
            executedClasses = document.Descendants()
                .Where(element => element.Name.LocalName == "UnitTestResult")
                .Where(result => !string.Equals((string?)result.Attribute("outcome"), "NotExecuted", StringComparison.Ordinal))
                .Select(result => (string?)result.Attribute("testId"))
                .Where(id => id is not null && classById.ContainsKey(id))
                .Select(id => classById[id!])
                .ToArray();
        }
        catch (System.Xml.XmlException error)
        {
            return $"Test group project '{project}' wrote unreadable TRX evidence, so classes {string.Join(", ", classes)} are unproven: {error.Message}";
        }

        var missing = classes.Where(name => !executedClasses.Any(className => IsClass(className, name))).ToArray();
        return missing.Length == 0
            ? null
            : $"Test group project '{project}' ran no case of class {string.Join(", ", missing)}.";
    }

    /// <summary>
    /// TR-MCP-NUKE-020 AC-5: whether a definition's class name is the named class, as the whole name or its last segment
    /// ('.' or '+' separated, '+' marking a nested class).
    /// </summary>
    /// <param name="className">The definition's TestMethod class name.</param>
    /// <param name="name">The class a group names.</param>
    /// <returns>True when the definition belongs to the named class.</returns>
    /// <remarks>2026-11-02, FR-MCP-BUILD-001; TR-MCP-NUKE-020 AC-5: added for hostile validation finding R9-F1.</remarks>
    private static bool IsClass(string className, string name) =>
        string.Equals(className, name, StringComparison.Ordinal)
        || className.EndsWith("." + name, StringComparison.Ordinal)
        || className.EndsWith("+" + name, StringComparison.Ordinal);
}
