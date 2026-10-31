using System.Globalization;
using System.Xml.Linq;

/// <summary>TR-MCP-NUKE-019 AC-2/AC-3: one identity supplies the project path, filter, log line and TRX name.</summary>
/// <param name="Name">The solution project name, never inferred from the file stem.</param>
/// <param name="Path">The solution's project file path.</param>
/// <param name="Filter">The exact filter passed to the test runner.</param>
/// <param name="LogLine">The exact filter announcement.</param>
/// <param name="TrxName">The expected TRX filename.</param>
internal sealed record IntegrationLaneRun(string Name, string Path, string Filter, string LogLine, string TrxName);

/// <summary>TR-MCP-NUKE-019: pure lane composition, evidence validation and pre-provisioning policy.</summary>
internal static class IntegrationLaneExecution
{
    /// <summary>Composes the seven projects in contract order, using their solution identity throughout.</summary>
    /// <param name="projects">Explicit solution names and paths.</param>
    /// <param name="host">The supported lane host.</param>
    /// <returns>The runner inputs and their matching diagnostic/evidence outputs.</returns>
    internal static IReadOnlyList<IntegrationLaneRun> Compose(IReadOnlyList<(string Name, string Path)> projects, IntegrationLaneHost host)
    {
        var selector = new IntegrationLaneProjects();
        var hostFilter = new IntegrationLaneFilter().Compose(host);
        var classes = new IntegrationLaneClassFilter();
        return selector.Select(projects.Select(project => project.Name).ToArray()).Select(name =>
        {
            var path = projects.Single(project => project.Name == name).Path;
            var filter = classes.Compose(hostFilter, selector.ClassesOf(name));
            return new IntegrationLaneRun(name, path, filter, $"Integration lane filter for {name}: {filter}", name + ".trx");
        }).ToArray();
    }

    /// <summary>Refuses absent, malformed, empty, inconsistent or non-passing TRX counters even when the runner exits zero.</summary>
    /// <param name="project">The solution project whose evidence is checked.</param>
    /// <param name="xml">The complete TRX content.</param>
    /// <returns>Null only for a nonempty zero-failure, zero-skip result.</returns>
    internal static string? CheckTrx(string project, string xml)
    {
        try
        {
            var document = XDocument.Parse(xml);
            var counters = document.Root?.Name.LocalName == "TestRun"
                ? document.Descendants().SingleOrDefault(element => element.Name.LocalName == "Counters") : null;
            if (counters is not null && Counter(counters, "total", out var total) && Counter(counters, "passed", out var passed)
                && Counter(counters, "failed", out var failed) && Counter(counters, "notExecuted", out var skipped)
                && total > 0 && total == passed && failed == 0 && skipped == 0)
                return null;
        }
        catch (Exception error) when (error is System.Xml.XmlException or InvalidOperationException)
        {
            return $"Integration lane project '{project}' wrote invalid TRX evidence: {error.Message}";
        }
        return $"Integration lane project '{project}' did not execute a nonempty all-passing scope (zero failures and zero skips required).";
    }

    /// <summary>Checks the lane's interactive-session requirement before a provisioning callback can execute.</summary>
    /// <param name="laneInvoked">Whether MigrationIntegrationTests was explicitly invoked.</param>
    /// <param name="host">The supported host.</param>
    /// <param name="interactive">The explicit desktop-session state.</param>
    /// <param name="provision">The next permitted action.</param>
    internal static void BeforeProvisioning(bool laneInvoked, IntegrationLaneHost host, bool interactive, Action provision)
    {
        if (laneInvoked)
        {
            var readiness = new InteractiveSessionPrecondition().Evaluate(host, interactive);
            if (!readiness.CanRun) throw new InvalidOperationException(readiness.FailureMessage);
        }
        provision();
    }

    /// <summary>Names the explicit supported OS input without treating every non-Windows host as Linux.</summary>
    internal static string HostName(bool windows, bool linux) => windows ? "Windows" : linux ? "Linux" : "unsupported host";

    /// <summary>The portable release assets are amd64; an installed native executable does not need this fallback.</summary>
    internal static string? PortableArchitectureFailure(string architecture) => architecture == "X64"
        ? null : $"Ollama portable provisioning does not support architecture '{architecture}'; its release assets require X64.";

    private static bool Counter(XElement element, string name, out int value) =>
        int.TryParse((string?)element.Attribute(name), NumberStyles.None, CultureInfo.InvariantCulture, out value);
}
