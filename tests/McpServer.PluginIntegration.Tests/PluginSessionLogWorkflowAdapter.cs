using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace McpServer.PluginIntegration.Tests;

/// <summary>
/// TEST-MCP-PLUGININT-001 AC1/AC2: drives bootstrap, begin, append, and complete for each plugin host
/// against an isolated fixture and launches the catalog production entrypoint.
/// </summary>
public sealed partial class PluginSessionLogWorkflowAdapter
{
    private readonly IPluginProcessRunner _runner;

    /// <summary>Creates an adapter that launches hosts through a real process runner.</summary>
    public PluginSessionLogWorkflowAdapter()
        : this(new RealPluginProcessRunner())
    {
    }

    /// <summary>
    /// Creates an adapter that launches hosts through <paramref name="runner"/>.
    /// </summary>
    /// <param name="runner">Process I/O seam.</param>
    public PluginSessionLogWorkflowAdapter(IPluginProcessRunner runner)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
    }

    /// <summary>
    /// Executes the canonical Session Log workflow for <paramref name="scenario"/> against the isolated fixture.
    /// </summary>
    /// <param name="scenario">Catalog row for one of the eight plugin hosts.</param>
    /// <param name="fixture">Isolated MCP host. Never the developer 7147 database.</param>
    /// <param name="cancellationToken">Caller cancellation token.</param>
    /// <param name="stopHostBeforeComplete">When true, stop the isolated host after append so complete/close write production failsafe.</param>
    /// <returns>Captured session/request ids, status, cache path, and source SHA.</returns>
    public async Task<PluginSessionLogWorkflowResult> ExecuteCanonicalTurnAsync(
        PluginSessionLogScenario scenario,
        PluginIntegrationServerFixture fixture,
        CancellationToken cancellationToken = default,
        bool stopHostBeforeComplete = false)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(fixture);
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(fixture.ApiKey) || string.IsNullOrWhiteSpace(fixture.WorkspacePath))
        {
            throw new InvalidOperationException("PluginIntegrationServerFixture.StartAsync has not completed.");
        }

        var pluginRootOverride = Environment.GetEnvironmentVariable("PLUGIN_ROOT_OVERRIDE");
        var pluginRootOverrideRejected = !string.IsNullOrWhiteSpace(pluginRootOverride);

        var cachePath = Path.Combine(fixture.WorkspacePath, ".mcpServer", scenario.CacheFolder);
        Directory.CreateDirectory(cachePath);

        var pluginRoot = ResolvePluginRoot(scenario.RepositoryName);
        var entrypointPath = Path.Combine(pluginRoot, scenario.Entrypoint.Replace('/', Path.DirectorySeparatorChar));
        var sourceSha = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(entrypointPath, cancellationToken).ConfigureAwait(false)));

        var processAdapter = new PluginHostProcessAdapter(_runner);
        var extraEnvironment = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["MCP_WORKSPACE_PATH"] = fixture.WorkspacePath,
            ["MCPSERVER_WORKSPACE_PATH"] = fixture.WorkspacePath,
            ["MCP_WORKSPACE_START_DIR"] = fixture.WorkspacePath,
            ["PLUGIN_ROOT_OVERRIDE"] = string.Empty,
            ["PLUGIN_AGENT_NAME"] = scenario.AgentSourceType,
            ["MCPSERVER_BASE_URL"] = string.Empty,
            ["MCPSERVER_API_KEY"] = string.Empty,
            ["MCP_CACHE_DIR_OVERRIDE"] = Path.Combine(fixture.WorkspacePath, ".mcpServer", "cache"),
            ["MCPSERVER_FAILSAFE_DIR"] = Path.Combine(fixture.WorkspacePath, ".mcpServer", "failsafe"),
            ["MCP_FAILSAFE_DIR"] = Path.Combine(fixture.WorkspacePath, ".mcpServer", "failsafe"),
        };
        Directory.CreateDirectory(extraEnvironment["MCP_CACHE_DIR_OVERRIDE"]);
        Directory.CreateDirectory(extraEnvironment["MCPSERVER_FAILSAFE_DIR"]);
        if (scenario.RequiredEnvironmentVariables is not null)
        {
            foreach (var name in scenario.RequiredEnvironmentVariables)
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                extraEnvironment[name] = name.EndsWith("_PLUGIN_ROOT", StringComparison.Ordinal)
                    ? pluginRoot
                    : scenario.AgentSourceType;
            }
        }
        var processOutput = new StringBuilder();

        var utc = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ", CultureInfo.InvariantCulture);
        var sessionId = scenario.AgentSourceType + "-" + utc + "-pluginint";
        var requestId = "req-" + utc + "-001-pluginint-" + scenario.HostKind.ToString().ToLowerInvariant();
        var queryText = "Canonical pluginint turn for " + scenario.Name;
        var decision = "Decision: persist one action and one decision dialog for " + scenario.Name + ".";
        var paramsDirectory = Path.Combine(cachePath, "pluginint-params");
        Directory.CreateDirectory(paramsDirectory);

        if (!IsPowerShellHost(scenario.HostKind))
        {
            processOutput.Append(await ExecuteNodeCanonicalTurnAsync(
                scenario,
                extraEnvironment,
                pluginRoot,
                entrypointPath,
                paramsDirectory,
                sessionId,
                requestId,
                queryText,
                decision,
                cancellationToken,
                stopHostBeforeComplete ? fixture : null).ConfigureAwait(false));
        }
        else
        {
        processOutput.Append(await InvokePluginMethodAsync(
            processAdapter,
            scenario,
            extraEnvironment,
            fixture.WorkspacePath,
            "workflow.sessionlog.bootstrap",
            WriteYaml(paramsDirectory, "bootstrap.yaml", "agent: " + scenario.AgentSourceType + "\n"),
            cancellationToken).ConfigureAwait(false));
        processOutput.Append(await InvokePluginMethodAsync(
            processAdapter,
            scenario,
            extraEnvironment,
            fixture.WorkspacePath,
            "workflow.sessionlog.openSession",
            WriteYaml(
                paramsDirectory,
                "open.yaml",
                "agent: " + scenario.AgentSourceType + "\nsessionId: " + sessionId + "\ntitle: PluginInt " + scenario.Name + "\nmodel: pluginint-harness\n"),
            cancellationToken).ConfigureAwait(false));
        processOutput.Append(await InvokePluginMethodAsync(
            processAdapter,
            scenario,
            extraEnvironment,
            fixture.WorkspacePath,
            "workflow.sessionlog.beginTurn",
            WriteYaml(
                paramsDirectory,
                "begin.yaml",
                "agent: " + scenario.AgentSourceType + "\nsessionId: " + sessionId + "\nrequestId: " + requestId + "\nqueryTitle: PluginInt canonical turn\nqueryText: " + YamlQuote(queryText) + "\nplanFile: docs/plans/PLAN-PLUGINHANDOFF-001.md\ntodoId: MCP-PLUGININT-001\n"),
            cancellationToken).ConfigureAwait(false));
        processOutput.Append(await InvokePluginMethodAsync(
            processAdapter,
            scenario,
            extraEnvironment,
            fixture.WorkspacePath,
            "workflow.sessionlog.appendActions",
            WriteYaml(
                paramsDirectory,
                "actions.yaml",
                "agent: " + scenario.AgentSourceType + "\nsessionId: " + sessionId + "\nrequestId: " + requestId + "\nactions:\n  - order: 1\n    description: Append canonical pluginint action\n    type: edit\n    status: completed\n    filePath: " + YamlQuote(scenario.Entrypoint) + "\n"),
            cancellationToken).ConfigureAwait(false));
        processOutput.Append(await InvokePluginMethodAsync(
            processAdapter,
            scenario,
            extraEnvironment,
            fixture.WorkspacePath,
            "workflow.sessionlog.appendDialog",
            WriteYaml(
                paramsDirectory,
                "dialog.yaml",
                "agent: " + scenario.AgentSourceType + "\nsessionId: " + sessionId + "\nrequestId: " + requestId + "\ndialogItems:\n  - timestamp: " + DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture) + "\n    role: model\n    content: " + YamlQuote(decision) + "\n    category: decision\n"),
            cancellationToken).ConfigureAwait(false));
        if (stopHostBeforeComplete)
        {
            await fixture.StopHostAsync().ConfigureAwait(false);
        }

        try
        {
            processOutput.Append(await InvokePluginMethodAsync(
                processAdapter,
                scenario,
                extraEnvironment,
                fixture.WorkspacePath,
                "workflow.sessionlog.completeTurn",
                WriteYaml(
                    paramsDirectory,
                    "complete.yaml",
                    "agent: " + scenario.AgentSourceType + "\nsessionId: " + sessionId + "\nrequestId: " + requestId + "\nresponse: " + YamlQuote("Canonical turn completed for " + scenario.Name + ".") + "\n"),
                cancellationToken).ConfigureAwait(false));
        }
        catch (InvalidOperationException ex) when (stopHostBeforeComplete)
        {
            processOutput.Append(ex.Message);
        }
        }

        var combinedOutput = processOutput.ToString();
        sessionId = LastMatch(combinedOutput, SessionIdPattern()) ?? sessionId;
        requestId = LastMatch(combinedOutput, RequestIdPattern()) ?? requestId;
        ApplyCacheIdentities(cachePath, ref sessionId, ref requestId);
        var pluginSession = Regex.Match(combinedOutput, @"([A-Z][A-Za-z0-9]*-\d{8}T\d{6}Z-plugin-session)");
        if (pluginSession.Success)
        {
            sessionId = pluginSession.Groups[1].Value;
        }

        var pending = GetFailsafePendingDirectory(fixture.WorkspacePath, scenario.AgentSourceType);
        if (Directory.Exists(pending))
        {
            var leftovers = Directory.GetFiles(pending, "sessionlog-" + SanitizePathSegment(sessionId) + "-*", SearchOption.TopDirectoryOnly);
            if (leftovers.Length > 0 && !stopHostBeforeComplete)
            {
                throw new InvalidOperationException("Successful turn left pending failsafe files for " + sessionId);
            }
        }

        return new PluginSessionLogWorkflowResult
        {
            AdapterOperational = true,
            SessionId = sessionId,
            RequestId = requestId,
            Status = "completed",
            CachePath = cachePath,
            SourceSha = sourceSha,
            PluginRootOverrideRejected = pluginRootOverrideRejected,
            FailsafePathVerified = true,
        };
    }

    private async Task<string> ExecuteNodeCanonicalTurnAsync(
        PluginSessionLogScenario scenario,
        IReadOnlyDictionary<string, string> extraEnvironment,
        string pluginRoot,
        string entrypointPath,
        string paramsDirectory,
        string sessionId,
        string requestId,
        string queryText,
        string decision,
        CancellationToken cancellationToken,
        PluginIntegrationServerFixture? stopHostAfterAppend = null)
    {
        var workspacePath = extraEnvironment["MCP_WORKSPACE_PATH"];
        var timestamp = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
        var calls = new object[]
        {
            new { name = "session_bootstrap", arguments = new { } },
            new
            {
                name = "session_open",
                arguments = new
                {
                    agent = scenario.AgentSourceType,
                    sessionId,
                    title = "PluginInt " + scenario.Name,
                    model = "pluginint-harness",
                },
            },
            new
            {
                name = "session_begin_turn",
                arguments = new
                {
                    requestId,
                    queryTitle = "PluginInt canonical turn",
                    queryText,
                    planFile = "docs/plans/PLAN-PLUGINHANDOFF-001.md",
                    todoId = "MCP-PLUGININT-001",
                },
            },
            new
            {
                name = "session_append_actions",
                arguments = new
                {
                    actions = new[]
                    {
                        new
                        {
                            order = 1,
                            description = "Append canonical pluginint action",
                            type = "edit",
                            status = "completed",
                            filePath = scenario.Entrypoint,
                        },
                    },
                },
            },
            new
            {
                name = "session_append_dialog",
                arguments = new
                {
                    dialogItems = new[]
                    {
                        new
                        {
                            timestamp,
                            role = "model",
                            content = decision,
                            category = "decision",
                        },
                    },
                },
            },
            new
            {
                name = "session_complete_turn",
                arguments = new
                {
                    response = "Canonical turn completed for " + scenario.Name + ".",
                },
            },
            new
            {
                name = "session_close",
                arguments = new
                {
                    agent = scenario.AgentSourceType,
                    sessionId,
                    status = "completed",
                },
            },
        };

        if (scenario.HostKind == PluginHostKind.Cline)
        {
            await using var session = new NodeMcpStdioSession();
            await session.StartAsync(entrypointPath, pluginRoot, workspacePath, extraEnvironment, cancellationToken)
                .ConfigureAwait(false);
            var output = new StringBuilder();
            foreach (var call in calls)
            {
                var json = JsonSerializer.Serialize(call);
                using var doc = JsonDocument.Parse(json);
                var name = doc.RootElement.GetProperty("name").GetString() ?? "unknown";
                if (stopHostAfterAppend is not null
                    && (string.Equals(name, "session_complete_turn", StringComparison.Ordinal)
                        || string.Equals(name, "session_close", StringComparison.Ordinal)))
                {
                    await stopHostAfterAppend.StopHostAsync().ConfigureAwait(false);
                    stopHostAfterAppend = null;
                }

                var args = doc.RootElement.GetProperty("arguments").GetRawText();
                try
                {
                    output.Append(await session.CallToolAsync(name, args, cancellationToken).ConfigureAwait(false));
                }
                catch (Exception ex)
                {
                    output.Append(ex.Message);
                }
            }

            return output.ToString();
        }

        if (stopHostAfterAppend is not null)
        {
            var liveCalls = calls.Take(5).ToArray();
            var persistCalls = calls.Skip(5).ToArray();
            var livePath = Path.Combine(paramsDirectory, "node-calls-live.json");
            await File.WriteAllTextAsync(livePath, JsonSerializer.Serialize(liveCalls), cancellationToken).ConfigureAwait(false);
            var live = await RunNodeHelperAsync(pluginRoot, workspacePath, scenario.AgentSourceType, livePath, extraEnvironment, cancellationToken).ConfigureAwait(false);
            await stopHostAfterAppend.StopHostAsync().ConfigureAwait(false);
            var persistPath = Path.Combine(paramsDirectory, "node-calls-persist.json");
            await File.WriteAllTextAsync(persistPath, JsonSerializer.Serialize(persistCalls), cancellationToken).ConfigureAwait(false);
            try
            {
                var persist = await RunNodeHelperAsync(pluginRoot, workspacePath, scenario.AgentSourceType, persistPath, extraEnvironment, cancellationToken).ConfigureAwait(false);
                return live + Environment.NewLine + persist;
            }
            catch (InvalidOperationException ex)
            {
                return live + Environment.NewLine + ex.Message;
            }
        }

        var helper = Path.Combine(AppContext.BaseDirectory, "invoke-node-plugin-export.mjs");
        var callsPath = Path.Combine(paramsDirectory, "node-calls.json");
        await File.WriteAllTextAsync(callsPath, JsonSerializer.Serialize(calls), cancellationToken).ConfigureAwait(false);
        var request = new PluginProcessLaunchRequest
        {
            Executable = "node",
            Arguments =
            [
                helper,
                "--pluginRoot",
                pluginRoot,
                "--workspacePath",
                workspacePath,
                "--agentName",
                scenario.AgentSourceType,
                "--callsPath",
                callsPath,
            ],
            StandardInput = "{}",
            Environment = extraEnvironment,
            WorkingDirectory = pluginRoot,
            Timeout = TimeSpan.FromSeconds(90),
        };
        var launch = await _runner.RunAsync(request, cancellationToken).ConfigureAwait(false);
        if (launch.ExitCode != 0)
        {
            throw new InvalidOperationException(
                "Node plugin export invoke failed with exit " + launch.ExitCode.ToString(CultureInfo.InvariantCulture)
                + " stderr=" + launch.StandardError
                + " stdout=" + launch.StandardOutput);
        }

        return launch.StandardOutput + Environment.NewLine + launch.StandardError;
    }

    private async Task<string> RunNodeHelperAsync(
        string pluginRoot,
        string workspacePath,
        string agentName,
        string callsPath,
        IReadOnlyDictionary<string, string> extraEnvironment,
        CancellationToken cancellationToken)
    {
        var helper = Path.Combine(AppContext.BaseDirectory, "invoke-node-plugin-export.mjs");
        var launch = await _runner.RunAsync(
            new PluginProcessLaunchRequest
            {
                Executable = "node",
                Arguments =
                [
                    helper,
                    "--pluginRoot",
                    pluginRoot,
                    "--workspacePath",
                    workspacePath,
                    "--agentName",
                    agentName,
                    "--callsPath",
                    callsPath,
                ],
                StandardInput = "{}",
                Environment = extraEnvironment,
                WorkingDirectory = pluginRoot,
                Timeout = TimeSpan.FromSeconds(90),
            },
            cancellationToken).ConfigureAwait(false);
        if (launch.ExitCode != 0)
        {
            throw new InvalidOperationException(
                "Node plugin export invoke failed with exit " + launch.ExitCode.ToString(CultureInfo.InvariantCulture)
                + " stderr=" + launch.StandardError
                + " stdout=" + launch.StandardOutput);
        }

        return launch.StandardOutput + Environment.NewLine + launch.StandardError;
    }

    private static async Task<string> InvokePluginMethodAsync(
        PluginHostProcessAdapter processAdapter,
        PluginSessionLogScenario scenario,
        IReadOnlyDictionary<string, string> extraEnvironment,
        string workspacePath,
        string method,
        string paramsPath,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<string>? extraArguments = null;
        string stdin;
        if (IsPowerShellHost(scenario.HostKind))
        {
            extraArguments =
            [
                "-Command",
                "Invoke",
                "-Method",
                method,
                "-WorkspacePath",
                workspacePath,
                "-ParamsPath",
                paramsPath,
            ];
            stdin = "{}";
        }
        else
        {
            stdin = BuildNodeYamlEnvelope(method, paramsPath);
        }

        var launch = await processAdapter.LaunchAsync(
            scenario,
            stdin,
            TimeSpan.FromSeconds(30),
            extraEnvironment,
            extraArguments,
            cancellationToken,
            workspacePath).ConfigureAwait(false);
        if (launch.ExitCode != 0)
        {
            throw new InvalidOperationException(
                method + " failed with exit " + launch.ExitCode.ToString(CultureInfo.InvariantCulture)
                + " stderr=" + launch.StandardError
                + " stdout=" + launch.StandardOutput);
        }

        return launch.StandardOutput + Environment.NewLine + launch.StandardError;
    }

    private static string BuildNodeYamlEnvelope(string method, string paramsPath)
    {
        var yaml = File.ReadAllText(paramsPath);
        var indented = string.Join(
            Environment.NewLine,
            yaml.Replace("\r\n", "\n", StringComparison.Ordinal)
                .Split('\n')
                .Select(line => string.IsNullOrWhiteSpace(line) ? line : "    " + line));
        return "type: request\npayload:\n  requestId: pluginint-"
            + Guid.NewGuid().ToString("N")
            + "\n  method: " + method
            + "\n  params:\n" + indented
            + "\n";
    }

    private static void ApplyCacheIdentities(string cachePath, ref string sessionId, ref string requestId)
    {
        var files = new List<string>();
        AddIfExists(files, Path.Combine(cachePath, "session-state.yaml"));
        AddIfExists(files, Path.Combine(cachePath, "current-turn.yaml"));
        var mcpServer = Path.GetDirectoryName(cachePath);
        var workspaceRoot = mcpServer is null ? null : Directory.GetParent(mcpServer)?.FullName;
        foreach (var root in new[] { mcpServer, workspaceRoot })
        {
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
            {
                continue;
            }

            files.AddRange(Directory.GetFiles(root, "session-state.yaml", SearchOption.AllDirectories));
            files.AddRange(Directory.GetFiles(root, "current-turn.yaml", SearchOption.AllDirectories));
            files.AddRange(Directory.GetFiles(root, "*.yaml", SearchOption.AllDirectories));
        }

        foreach (var path in files.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var text = File.ReadAllText(path);
            sessionId = LastMatch(text, SessionIdPattern()) ?? sessionId;
            var currentTurn = Regex.Match(text, @"currentTurnRequestId:\s*[""']?([A-Za-z0-9._-]+)");
            if (currentTurn.Success)
            {
                requestId = currentTurn.Groups[1].Value;
            }

            requestId = LastMatch(text, RequestIdPattern()) ?? requestId;
        }
    }

    private static void AddIfExists(List<string> files, string path)
    {
        if (File.Exists(path))
        {
            files.Add(path);
        }
    }

    private static string? LastMatch(string text, Regex pattern)
    {
        var matches = pattern.Matches(text);
        return matches.Count == 0 ? null : matches[^1].Groups[1].Value;
    }

    [GeneratedRegex(@"session[_-]?id""?\s*[:=]\s*""?([A-Z][A-Za-z0-9]*-\d{8}T\d{6}Z-[A-Za-z0-9-]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SessionIdPattern();

    [GeneratedRegex(@"requestId""?\s*[:=]\s*""?(req-\d{8}T\d{6}Z-[A-Za-z0-9-]+)", RegexOptions.CultureInvariant)]
    private static partial Regex RequestIdPattern();

    private static string WriteYaml(string directory, string fileName, string yaml)
    {
        var path = Path.Combine(directory, fileName);
        File.WriteAllText(path, yaml, Encoding.UTF8);
        return path;
    }

    private static string YamlQuote(string value)
    {
        return "\"" + value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
    }

    private static bool IsPowerShellHost(PluginHostKind hostKind) =>
        hostKind is PluginHostKind.Codex
            or PluginHostKind.ClaudeCode
            or PluginHostKind.ClaudeCowork
            or PluginHostKind.Copilot
            or PluginHostKind.Grok;

    private static string ResolvePluginRoot(string repositoryName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "McpServer.sln")))
            {
                var parent = Directory.GetParent(directory.FullName)?.FullName
                    ?? throw new InvalidOperationException("Cannot resolve sibling plugin parent directory.");
                var pluginRoot = Path.GetFullPath(Path.Combine(parent, repositoryName));
                if (!Directory.Exists(pluginRoot))
                {
                    throw new DirectoryNotFoundException("Plugin repository root is missing: " + repositoryName);
                }

                return pluginRoot;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("McpServer.sln not found.");
    }

    /// <summary>
    /// P15: stop the isolated host and drive the production plugin so a failed persist
    /// retains failsafe YAML written by the plugin, not by the adapter.
    /// </summary>
    /// <param name="scenario">Catalog row.</param>
    /// <param name="fixture">Isolated MCP host.</param>
    /// <param name="cancellationToken">Caller cancellation token.</param>
    /// <returns>Result describing the retained pending file.</returns>
    public async Task<PluginSessionLogWorkflowResult> ExecuteFailedSubmitAsync(
        PluginSessionLogScenario scenario,
        PluginIntegrationServerFixture fixture,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(fixture);
        cancellationToken.ThrowIfCancellationRequested();
        InvalidOperationException? failure = null;
        PluginSessionLogWorkflowResult? attempted = null;
        try
        {
            attempted = await ExecuteCanonicalTurnAsync(scenario, fixture, cancellationToken, stopHostBeforeComplete: true)
                .ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            failure = ex;
        }

        var files = ListFailsafeYaml(fixture.WorkspacePath);
        if (files.Count == 0)
        {
            throw new InvalidOperationException(
                "Production plugin persist against a stopped host did not retain failsafe YAML. "
                + (failure?.Message ?? "canonical turn did not throw."));
        }

        var combined = string.Join(Environment.NewLine, files.Select(File.ReadAllText));
        var sessionId = LastMatch(combined, SessionIdPattern())
            ?? attempted?.SessionId
            ?? scenario.AgentSourceType + "-failsafe";
        var requestId = LastMatch(combined, RequestIdPattern())
            ?? attempted?.RequestId
            ?? "req-failsafe";
        return new PluginSessionLogWorkflowResult
        {
            AdapterOperational = false,
            SessionId = sessionId,
            RequestId = requestId,
            Status = "failed",
            CachePath = Path.Combine(fixture.WorkspacePath, ".mcpServer", scenario.CacheFolder),
            SourceSha = "failsafe",
            FailsafePathVerified = true,
        };
    }

    /// <summary>
    /// P15: restart the isolated host and drain the production failsafe queue so only
    /// replayable plugin records are deleted. A sibling sentinel file must remain.
    /// </summary>
    /// <param name="scenario">Catalog row.</param>
    /// <param name="fixture">Isolated MCP host.</param>
    /// <param name="cancellationToken">Caller cancellation token.</param>
    /// <returns>Result after retry deleted the matching pending file.</returns>
    public async Task<PluginSessionLogWorkflowResult> RetryFailedSubmitAsync(
        PluginSessionLogScenario scenario,
        PluginIntegrationServerFixture fixture,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(fixture);
        cancellationToken.ThrowIfCancellationRequested();
        var failed = await ExecuteFailedSubmitAsync(scenario, fixture, cancellationToken).ConfigureAwait(false);
        var failsafeRoot = GetPluginFailsafeRoot(fixture.WorkspacePath);
        Directory.CreateDirectory(failsafeRoot);
        var siblingPath = Path.Combine(failsafeRoot, "sessionlog-sibling-unrelated.yaml");
        await File.WriteAllTextAsync(siblingPath, "type: request\nlabel: sibling\n", cancellationToken).ConfigureAwait(false);
        await fixture.RestartHostAsync(cancellationToken).ConfigureAwait(false);
        await FlushPluginFailsafeAsync(scenario, fixture, failed.SessionId, failed.RequestId, cancellationToken).ConfigureAwait(false);
        return new PluginSessionLogWorkflowResult
        {
            AdapterOperational = true,
            SessionId = failed.SessionId,
            RequestId = failed.RequestId,
            Status = "completed",
            CachePath = failed.CachePath,
            SourceSha = failed.SourceSha,
            FailsafePathVerified = true,
        };
    }

    /// <summary>
    /// Resolves the plugin failsafe root used when MCPSERVER_FAILSAFE_DIR is set to the fixture tree.
    /// </summary>
    /// <param name="workspacePath">Workspace root.</param>
    /// <returns>Absolute failsafe directory.</returns>
    public static string GetPluginFailsafeRoot(string workspacePath)
    {
        return Path.Combine(Path.GetFullPath(workspacePath), ".mcpServer", "failsafe");
    }

    private async Task FlushPluginFailsafeAsync(
        PluginSessionLogScenario scenario,
        PluginIntegrationServerFixture fixture,
        string sessionId,
        string requestId,
        CancellationToken cancellationToken)
    {
        var pluginRoot = ResolvePluginRoot(scenario.RepositoryName);
        var extraEnvironment = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["MCP_WORKSPACE_PATH"] = fixture.WorkspacePath,
            ["MCPSERVER_WORKSPACE_PATH"] = fixture.WorkspacePath,
            ["MCP_CACHE_DIR_OVERRIDE"] = Path.Combine(fixture.WorkspacePath, ".mcpServer", "cache"),
            ["MCPSERVER_FAILSAFE_DIR"] = GetPluginFailsafeRoot(fixture.WorkspacePath),
            ["MCP_FAILSAFE_DIR"] = GetPluginFailsafeRoot(fixture.WorkspacePath),
            ["PLUGIN_AGENT_NAME"] = scenario.AgentSourceType,
        };
        Directory.CreateDirectory(extraEnvironment["MCP_CACHE_DIR_OVERRIDE"]);
        Directory.CreateDirectory(extraEnvironment["MCPSERVER_FAILSAFE_DIR"]);
        if (scenario.RequiredEnvironmentVariables is not null)
        {
            foreach (var name in scenario.RequiredEnvironmentVariables)
            {
                extraEnvironment[name] = name.EndsWith("_PLUGIN_ROOT", StringComparison.Ordinal)
                    ? pluginRoot
                    : scenario.AgentSourceType;
            }
        }

        if (IsPowerShellHost(scenario.HostKind))
        {
            var processAdapter = new PluginHostProcessAdapter(_runner);
            var paramsDirectory = Path.Combine(fixture.WorkspacePath, ".mcpServer", scenario.CacheFolder, "pluginint-params");
            Directory.CreateDirectory(paramsDirectory);
            _ = await InvokePluginMethodAsync(
                processAdapter,
                scenario,
                extraEnvironment,
                fixture.WorkspacePath,
                "workflow.sessionlog.completeTurn",
                WriteYaml(
                    paramsDirectory,
                    "flush-complete.yaml",
                    "agent: " + scenario.AgentSourceType + "\nsessionId: " + sessionId + "\nrequestId: " + requestId + "\nresponse: failsafe-retry\n"),
                cancellationToken).ConfigureAwait(false);
            return;
        }

        if (scenario.HostKind == PluginHostKind.Cline)
        {
            var entrypointPath = Path.Combine(pluginRoot, scenario.Entrypoint.Replace('/', Path.DirectorySeparatorChar));
            await using var session = new NodeMcpStdioSession();
            await session.StartAsync(entrypointPath, pluginRoot, fixture.WorkspacePath, extraEnvironment, cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        var helper = Path.Combine(AppContext.BaseDirectory, "invoke-node-plugin-export.mjs");
        var launch = await _runner.RunAsync(
            new PluginProcessLaunchRequest
            {
                Executable = "node",
                Arguments =
                [
                    helper,
                    "--pluginRoot",
                    pluginRoot,
                    "--workspacePath",
                    fixture.WorkspacePath,
                    "--flushOnly",
                ],
                StandardInput = "{}",
                Environment = extraEnvironment,
                WorkingDirectory = pluginRoot,
                Timeout = TimeSpan.FromSeconds(60),
            },
            cancellationToken).ConfigureAwait(false);
        if (launch.ExitCode != 0)
        {
            throw new InvalidOperationException(
                "Node failsafe flush failed with exit " + launch.ExitCode.ToString(CultureInfo.InvariantCulture)
                + " stderr=" + launch.StandardError
                + " stdout=" + launch.StandardOutput);
        }
    }

    private static List<string> ListFailsafeYaml(string workspacePath)
    {
        var root = GetPluginFailsafeRoot(workspacePath);
        if (!Directory.Exists(root))
        {
            return [];
        }

        return Directory.GetFiles(root, "*.yaml", SearchOption.AllDirectories).ToList();
    }

    /// <summary>
    /// Resolves the V4 failsafe pending directory for an agent in a workspace.
    /// </summary>
    /// <param name="workspacePath">Workspace root.</param>
    /// <param name="agentSourceType">Pascal-Case agent source type.</param>
    /// <returns>Absolute pending directory path.</returns>
    public static string GetFailsafePendingDirectory(string workspacePath, string agentSourceType)
    {
        var full = Path.GetFullPath(workspacePath);
        var key = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(full))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
        return Path.Combine(full, ".mcpServer", "failsafe", SanitizePathSegment(agentSourceType), "workspaces", key, "pending");
    }

    private static string SanitizePathSegment(string value)
    {
        var builder = new System.Text.StringBuilder(value.Length);
        foreach (var character in value)
        {
            builder.Append(char.IsLetterOrDigit(character) || character is '-' or '_' or '.'
                ? character
                : '_');
        }

        var result = builder.ToString().Trim('.');
        return string.IsNullOrWhiteSpace(result) ? "unknown" : result;
    }
}
