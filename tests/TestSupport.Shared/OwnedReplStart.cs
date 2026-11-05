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
    /// <remarks>2026-11-05, FR-MCP-TESTRESOURCE-001 ac-4 (appended "A name is matched exactly, as the process environment holds it"): the environment is replaced and checked through <see cref="ApplyOwnedStartEnvironment"/> (seventh holistic read before hostile validation round 13, finding g1).</remarks>
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
        ApplyOwnedStartEnvironment(startInfo.Environment, plan, ownedRoot, serverAddress);
        return startInfo;
    }

    /// <summary>
    /// FR-MCP-TESTRESOURCE-001 ac-4: replaces <paramref name="environment"/>, the environment a REPL child would inherit,
    /// with the one <see cref="OwnedInstanceProcess.ReplaceEnvironment"/> builds from it and the plan's settings, then
    /// checks the replaced environment's server address and trust cache (<see cref="TemporaryInstanceChecks.CheckReplStartContext"/>)
    /// through a read-only view of it, never a copy, so every name is read as the environment holds it and two
    /// names that differ only in case stay two names. Throws when either check refuses. The same boundary the process
    /// start uses, so unit tests need no ambient process environment.
    /// </summary>
    /// <param name="environment">The inherited environment, replaced in place.</param>
    /// <param name="plan">The validated start plan.</param>
    /// <param name="ownedRoot">The owned absolute root.</param>
    /// <param name="serverAddress">The owned server address.</param>
    /// <exception cref="InvalidOperationException">The owned environment or the start context is refused.</exception>
    /// <remarks>2026-11-05, FR-MCP-TESTRESOURCE-001 ac-4 (appended "A name is matched exactly, as the process environment holds it"): added in place of the ignore-case copy CreateStartInfo checked, which would throw on, or merge, two inherited names that differ only in case once the builder keeps them, because the seventh holistic read before hostile validation round 13 (finding g1) showed the builder merged such names (mock gate ug-r13z4c-mockgate-1).</remarks>
    internal static void ApplyOwnedStartEnvironment(IDictionary<string, string?> environment, Plan plan, string ownedRoot, string serverAddress)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(plan);
        OwnedInstanceProcess.ReplaceEnvironment(environment, plan.Settings, ownedRoot);
        var violation = TemporaryInstanceChecks.CheckReplStartContext(plan.Arguments,
            new System.Collections.ObjectModel.ReadOnlyDictionary<string, string?>(environment), ownedRoot, serverAddress);
        if (violation is not null) throw new InvalidOperationException(violation);
    }

    /// <summary>The explicit data a validated owned REPL start consumes.</summary>
    /// <param name="Arguments">The actual argument list.</param>
    /// <param name="Settings">The actual owned settings.</param>
    internal sealed record Plan(IReadOnlyList<string> Arguments, IReadOnlyDictionary<string, string> Settings);
}
