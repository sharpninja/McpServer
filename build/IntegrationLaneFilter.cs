/// <summary>
/// TR-MCP-NUKE-019: Production <see cref="IIntegrationLaneFilter"/>. Selects the Category=Integration cases of one
/// host by excluding the other host's Platform trait, so untagged integration cases run on both hosts.
/// </summary>
sealed class IntegrationLaneFilter : IIntegrationLaneFilter
{
    /// <inheritdoc />
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
    /// <item><term>2026-11-02</term><description>FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-1; TR-MCP-NUKE-020 AC-3: now built from the shared platform clause; the lane filter text is unchanged.</description></item>
    /// </list>
    /// </remarks>
    public string Compose(IntegrationLaneHost host) => "Category=Integration&" + PlatformClause(host);

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
