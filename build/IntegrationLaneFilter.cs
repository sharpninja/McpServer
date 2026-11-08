/// <summary>
/// TR-MCP-NUKE-019: Production <see cref="IIntegrationLaneFilter"/>. Selects the Category=Integration cases of one
/// host by excluding the other host's Platform trait, so untagged integration cases run on both hosts, and (AC-8)
/// excludes quarantined cases (<see cref="TestQuarantine.Exclusion"/>).
/// </summary>
/// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-8: the summary names the quarantine exclusion.</remarks>
sealed class IntegrationLaneFilter : IIntegrationLaneFilter
{
    /// <inheritdoc />
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
    /// <item><term>2026-11-02</term><description>FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-1; TR-MCP-NUKE-020 AC-3: now built from the shared platform clause; the lane filter text is unchanged.</description></item>
    /// <item><term>2026-11-07</term><description>FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-8: appends the quarantine exclusion, so the lane runs no quarantined case and every lane project filter and logged filter line shows the exclusion; PlatformClause, which TestGroups uses, is unchanged, so TestGroups still runs a named class's quarantined cases (TR-MCP-NUKE-020 AC-1). Added after mock gate ug-qr-mockgate-1.</description></item>
    /// </list>
    /// </remarks>
    public string Compose(IntegrationLaneHost host) => "Category=Integration&" + PlatformClause(host) + "&" + TestQuarantine.Exclusion;

    /// <summary>
    /// TR-MCP-NUKE-019 AC-1 and TR-MCP-NUKE-020 AC-4: the host platform clause the lane and TestGroups share. It excludes
    /// the other host's Platform trait, so untagged cases run on both hosts.
    /// </summary>
    /// <param name="host">The supported host.</param>
    /// <returns>The clause without a category.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The host is not Linux or Windows.</exception>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
    /// <item><term>2026-11-02</term><description>FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-1; TR-MCP-NUKE-020 AC-4: extracted so the lane and TestGroups exclude other-platform cases with one clause.</description></item>
    /// </list>
    /// </remarks>
    internal static string PlatformClause(IntegrationLaneHost host) => host switch
    {
        IntegrationLaneHost.Windows => "Platform!=Linux",
        IntegrationLaneHost.Linux => "Platform!=Windows",
        _ => throw new ArgumentOutOfRangeException(nameof(host), host, "Unknown integration lane host."),
    };

    /// <summary>TR-MCP-NUKE-019: The host the running build is on.</summary>
    public static IntegrationLaneHost CurrentHost()
        => IntegrationLaneExecution.HostName(OperatingSystem.IsWindows(), OperatingSystem.IsLinux()) switch
        {
            "Windows" => IntegrationLaneHost.Windows,
            "Linux" => IntegrationLaneHost.Linux,
            _ => throw new PlatformNotSupportedException($"Integration lanes do not support host '{System.Runtime.InteropServices.RuntimeInformation.OSDescription}'."),
        };
}
