namespace McpServer.PluginIntegration.Tests;

/// <summary>
/// TR-MCP-PLUGININT-001 AC3: maps each PluginHostKind onto a process launch through IPluginProcessRunner.
/// PowerShell hosts use pwsh.exe -File on the catalog entrypoint. Node hosts use node plus dist/index.js.
/// </summary>
public sealed class PluginHostProcessAdapter
{
    private readonly IPluginProcessRunner _runner;

    /// <summary>
    /// Creates an adapter that will launch plugin hosts through <paramref name="runner"/>.
    /// </summary>
    /// <param name="runner">Process I/O seam (fake in unit tests).</param>
    public PluginHostProcessAdapter(IPluginProcessRunner runner)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
    }

    /// <summary>
    /// Launches the production entrypoint for <paramref name="scenario"/> and returns captured process I/O.
    /// </summary>
    /// <param name="scenario">Catalog row that declares host kind, entrypoint, and required environment.</param>
    /// <param name="standardInput">Stdin envelope forwarded to the host process.</param>
    /// <param name="timeout">Launch timeout forwarded to the process runner.</param>
    /// <param name="cancellationToken">Caller cancellation token.</param>
    /// <returns>Captured exit code, stdout, and stderr from the process runner.</returns>
    public Task<PluginProcessLaunchResult> LaunchAsync(
        PluginSessionLogScenario scenario,
        string standardInput,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentException.ThrowIfNullOrWhiteSpace(standardInput);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero);
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(scenario.RepositoryName) || string.IsNullOrWhiteSpace(scenario.Entrypoint))
        {
            throw new InvalidOperationException("Catalog scenario is missing repository or entrypoint: " + scenario.Name);
        }

        var pluginRoot = ResolvePluginRoot(scenario.RepositoryName);
        var entrypointPath = Path.Combine(pluginRoot, scenario.Entrypoint.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(entrypointPath))
        {
            throw new FileNotFoundException("Plugin entrypoint is missing: " + scenario.Name + " " + scenario.Entrypoint, entrypointPath);
        }

        var environment = new Dictionary<string, string>(StringComparer.Ordinal);
        if (scenario.RequiredEnvironmentVariables is not null)
        {
            foreach (var name in scenario.RequiredEnvironmentVariables)
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                if (name.EndsWith("_PLUGIN_ROOT", StringComparison.Ordinal))
                {
                    environment[name] = pluginRoot;
                }
                else if (string.Equals(name, "PLUGIN_AGENT_NAME", StringComparison.Ordinal))
                {
                    environment[name] = scenario.AgentSourceType;
                }
                else
                {
                    environment[name] = scenario.AgentSourceType;
                }
            }
        }

        environment["PLUGIN_AGENT_NAME"] = scenario.AgentSourceType;

        var arguments = new List<string>();
        string executable;
        if (IsPowerShellHost(scenario.HostKind))
        {
            executable = "pwsh.exe";
            arguments.Add("-NoProfile");
            arguments.Add("-NonInteractive");
            arguments.Add("-File");
            arguments.Add(entrypointPath);
        }
        else
        {
            executable = "node";
            arguments.Add(entrypointPath);
        }

        var request = new PluginProcessLaunchRequest
        {
            Executable = executable,
            Arguments = arguments,
            StandardInput = standardInput,
            Environment = environment,
            WorkingDirectory = pluginRoot,
            Timeout = timeout,
        };

        return _runner.RunAsync(request, cancellationToken);
    }

    /// <summary>
    /// Launches the production entrypoint with extra environment and argument overlays.
    /// </summary>
    /// <param name="scenario">Catalog row.</param>
    /// <param name="standardInput">Stdin envelope.</param>
    /// <param name="timeout">Launch timeout.</param>
    /// <param name="extraEnvironment">Optional extra environment values (for example MCP_WORKSPACE_PATH).</param>
    /// <param name="extraArguments">Optional extra arguments appended after the catalog entrypoint.</param>
    /// <param name="cancellationToken">Caller cancellation token.</param>
    /// <param name="workingDirectory">Optional working directory overlay (fixture workspace).</param>
    /// <returns>Captured process result.</returns>
    public async Task<PluginProcessLaunchResult> LaunchAsync(
        PluginSessionLogScenario scenario,
        string standardInput,
        TimeSpan timeout,
        IReadOnlyDictionary<string, string>? extraEnvironment,
        IReadOnlyList<string>? extraArguments,
        CancellationToken cancellationToken = default,
        string? workingDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentException.ThrowIfNullOrWhiteSpace(standardInput);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero);
        cancellationToken.ThrowIfCancellationRequested();

        var inner = new CapturingProcessRunner();
        var innerAdapter = new PluginHostProcessAdapter(inner);
        await innerAdapter.LaunchAsync(scenario, standardInput, timeout, cancellationToken).ConfigureAwait(false);
        var captured = inner.LastRequest ?? throw new InvalidOperationException("Adapter did not build a launch request.");

        var environment = new Dictionary<string, string>(captured.Environment, StringComparer.Ordinal);
        if (extraEnvironment is not null)
        {
            foreach (var pair in extraEnvironment)
            {
                environment[pair.Key] = pair.Value;
            }
        }

        var arguments = captured.Arguments.ToList();
        if (extraArguments is not null)
        {
            arguments.AddRange(extraArguments);
        }

        var request = new PluginProcessLaunchRequest
        {
            Executable = captured.Executable,
            Arguments = arguments,
            StandardInput = captured.StandardInput,
            Environment = environment,
            WorkingDirectory = string.IsNullOrWhiteSpace(workingDirectory) ? captured.WorkingDirectory : workingDirectory,
            Timeout = captured.Timeout,
        };
        return await _runner.RunAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private sealed class CapturingProcessRunner : IPluginProcessRunner
    {
        public PluginProcessLaunchRequest? LastRequest { get; private set; }

        public Task<PluginProcessLaunchResult> RunAsync(PluginProcessLaunchRequest request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(new PluginProcessLaunchResult
            {
                ExitCode = 0,
                StandardOutput = string.Empty,
                StandardError = string.Empty,
            });
        }
    }

    private static bool IsPowerShellHost(PluginHostKind hostKind) =>
        hostKind is PluginHostKind.Codex
            or PluginHostKind.ClaudeCode
            or PluginHostKind.ClaudeCowork
            or PluginHostKind.Copilot
            or PluginHostKind.Grok;

    private static string ResolvePluginRoot(string repositoryName)
    {
        var repoRoot = FindRepositoryRoot();
        var parent = Directory.GetParent(repoRoot)?.FullName
            ?? throw new InvalidOperationException("Cannot resolve sibling plugin parent directory.");
        var pluginRoot = Path.GetFullPath(Path.Combine(parent, repositoryName));
        if (!Directory.Exists(pluginRoot))
        {
            throw new DirectoryNotFoundException("Plugin repository root is missing: " + repositoryName);
        }

        return pluginRoot;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "McpServer.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("McpServer.sln not found.");
    }
}
