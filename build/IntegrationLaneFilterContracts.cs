/// <summary>
/// TR-MCP-NUKE-019: Host operating system whose Category=Integration cases a MigrationIntegrationTests run selects.
/// </summary>
enum IntegrationLaneHost
{
    /// <summary>A Windows host (TEST-NODE-A).</summary>
    Windows,

    /// <summary>A Linux host (TEST-NODE-B).</summary>
    Linux,
}

/// <summary>
/// TR-MCP-NUKE-019: Composes the dotnet test filter of the host-scoped integration lane. Cases tagged
/// Platform=Linux run only on Linux hosts, cases tagged Platform=Windows only on Windows hosts, and untagged
/// integration cases on both; (AC-8) no case tagged Quarantine=NoMatchingCriterion runs on either.
/// </summary>
/// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-8: the summary states the quarantine exclusion the contract requires.</remarks>
interface IIntegrationLaneFilter
{
    /// <summary>Returns the test filter that selects the Category=Integration cases of <paramref name="host"/>.</summary>
    /// <param name="host">Host the lane runs on.</param>
    string Compose(IntegrationLaneHost host);
}
