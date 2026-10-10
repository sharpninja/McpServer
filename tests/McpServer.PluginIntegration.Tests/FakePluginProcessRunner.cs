namespace McpServer.PluginIntegration.Tests;

/// <summary>
/// PLAN-PLUGINHANDOFF-001 C-red-P7: fake process runner that records launch inputs and returns a configured result.
/// </summary>
public sealed class FakePluginProcessRunner : IPluginProcessRunner
{
    /// <summary>Last launch request observed by the fake, or null when unused.</summary>
    public PluginProcessLaunchRequest? LastRequest { get; private set; }

    /// <summary>Result returned from the next <see cref="RunAsync"/> call.</summary>
    public PluginProcessLaunchResult NextResult { get; set; } = new()
    {
        ExitCode = 0,
        StandardOutput = string.Empty,
        StandardError = string.Empty,
    };

    /// <inheritdoc />
    public Task<PluginProcessLaunchResult> RunAsync(PluginProcessLaunchRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        LastRequest = request;
        return Task.FromResult(NextResult);
    }
}
