using System.Diagnostics;
using System.Text;

namespace QBrainAi.TestSupport.TemporaryInstance;

/// <summary>FR-MCP-TESTRESOURCE-001 ac-4: one guarded REPL starter shared by both integration consumers.</summary>
internal static class OwnedReplStart
{
    /// <summary>Builds and validates the owned arguments and settings before any process can start.</summary>
    /// <param name="assemblyPath">The REPL host assembly.</param>
    /// <param name="workspacePath">The instance's workspace.</param>
    /// <param name="markerPath">The instance's marker.</param>
    /// <param name="ownedRoot">The instance's absolute root.</param>
    /// <param name="serverAddress">The instance's own server address.</param>
    /// <param name="agent">The invoking agent.</param>
    /// <param name="guard">An optional explicit guard used to prove consumer refusal propagation.</param>
    /// <returns>The validated start plan.</returns>
    internal static Plan CreatePlan(string assemblyPath, string workspacePath, string markerPath, string ownedRoot,
        string serverAddress, string agent = "default",
        Func<IReadOnlyList<string>, IReadOnlyDictionary<string, string?>, string?>? guard = null)
    {
        string[] arguments = [assemblyPath, "--agent-stdio", "--agent", agent,
            "--workspace-path", workspacePath, "--marker-file", markerPath];
        var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["MCP_WORKSPACE_PATH"] = workspacePath,
            ["MCPSERVER_WORKSPACE_PATH"] = workspacePath,
            ["MCP_SERVER_URL"] = serverAddress,
            ["QBRAINAI_REPL_CACHE_DIR"] = Path.Combine(ownedRoot, "repl-cache"),
            ["MCP_AGENT_NAME"] = agent,
            ["PLUGIN_AGENT_NAME"] = agent,
            ["MCPSERVER_REPL_COMMAND_TIMEOUT_SECONDS"] = "10",
            ["MCPSERVER_REPL_STREAM_COMMAND_TIMEOUT_SECONDS"] = "8",
            ["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1",
            ["DOTNET_NOLOGO"] = "1",
        };
        var environment = settings.ToDictionary(pair => pair.Key, pair => (string?)pair.Value, StringComparer.OrdinalIgnoreCase);
        var violation = guard is null
            ? TemporaryInstanceChecks.CheckReplStartContext(arguments, environment, ownedRoot, serverAddress)
            : guard(arguments, environment);
        if (violation is not null) throw new InvalidOperationException(violation);
        return new Plan(arguments, settings);
    }

    /// <summary>Composes the actual process start information and replaces its inherited environment before returning it.</summary>
    /// <param name="assemblyPath">The REPL host assembly.</param>
    /// <param name="workspacePath">The owned workspace.</param>
    /// <param name="markerPath">The owned marker.</param>
    /// <param name="ownedRoot">The owned absolute root.</param>
    /// <param name="serverAddress">The owned server address.</param>
    /// <param name="agent">The invoking agent.</param>
    /// <returns>The validated start information; this method starts no process.</returns>
    internal static ProcessStartInfo CreateStartInfo(string assemblyPath, string workspacePath, string markerPath,
        string ownedRoot, string serverAddress, string agent = "default")
    {
        var plan = CreatePlan(assemblyPath, workspacePath, markerPath, ownedRoot, serverAddress, agent);
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet", WorkingDirectory = workspacePath, UseShellExecute = false,
            RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true,
            CreateNoWindow = true, StandardInputEncoding = Encoding.UTF8,
            StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8,
        };
        foreach (var argument in plan.Arguments) startInfo.ArgumentList.Add(argument);
        OwnedInstanceProcess.ApplyOwnedEnvironment(startInfo, plan.Settings, ownedRoot);
        var environment = startInfo.Environment.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        var violation = TemporaryInstanceChecks.CheckReplStartContext(plan.Arguments, environment, ownedRoot, serverAddress);
        if (violation is not null) throw new InvalidOperationException(violation);
        return startInfo;
    }

    /// <summary>The explicit data a validated owned REPL start consumes.</summary>
    /// <param name="Arguments">The actual argument list.</param>
    /// <param name="Settings">The actual owned settings.</param>
    internal sealed record Plan(IReadOnlyList<string> Arguments, IReadOnlyDictionary<string, string> Settings);
}
