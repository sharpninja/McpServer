/// <summary>
/// TR-MCP-NUKE-019: Production <see cref="IIntegrationLaneFilter"/>. Selects the Category=Integration cases of one
/// host by excluding the other host's Platform trait, so untagged integration cases run on both hosts.
/// </summary>
sealed class IntegrationLaneFilter : IIntegrationLaneFilter
{
    /// <inheritdoc />
    public string Compose(IntegrationLaneHost host) => host switch
    {
        IntegrationLaneHost.Windows => "Category=Integration&Platform!=Linux",
        IntegrationLaneHost.Linux => "Category=Integration&Platform!=Windows",
        _ => throw new ArgumentOutOfRangeException(nameof(host), host, "Unknown integration lane host."),
    };

    /// <summary>TR-MCP-NUKE-019: The host the running build is on.</summary>
    public static IntegrationLaneHost CurrentHost()
        => OperatingSystem.IsWindows() ? IntegrationLaneHost.Windows : IntegrationLaneHost.Linux;
}
