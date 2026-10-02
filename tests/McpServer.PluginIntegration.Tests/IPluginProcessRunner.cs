namespace McpServer.PluginIntegration.Tests;

/// <summary>
/// TR-MCP-PLUGININT-001 AC3: process I/O seam so adapter tests can use a fake runner.
/// </summary>
public interface IPluginProcessRunner
{
    /// <summary>
    /// Runs one plugin host process and returns captured exit/stdout/stderr.
    /// </summary>
    /// <param name="request">Launch inputs including executable, args, stdin, env, cwd, and timeout.</param>
    /// <param name="cancellationToken">Caller cancellation token.</param>
    /// <returns>Captured process result.</returns>
    Task<PluginProcessLaunchResult> RunAsync(PluginProcessLaunchRequest request, CancellationToken cancellationToken = default);
}
