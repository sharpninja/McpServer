// SPDX-License-Identifier: Apache-2.0
using System.Collections;
using System.Text;
using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using QBrainAi.Client;
using QBrainAi.McpAgent.Hosting;
using QBrainAi.McpAgent.PowerShellSessions;
using QBrainAi.Repl.Core;

namespace QBrainAi.McpAgent.Tests;

/// <summary>
/// FR-QB-003/005, TR-QB-RUNTIME-001, TEST-QB-012/069: Bind the existing fidelity
/// vectors to the public hosted-agent PowerShell API without changing its lifecycle.
/// No HTTP request, model provider, alternative engine or MCP transport is created.
/// </summary>
public sealed class GovernedHostedPowerShellFixture : IDisposable
{
    private readonly IHostedPowerShellSessionManager _sessions;
    private readonly ServiceProvider _services;
    private readonly HttpClient _http;
    private readonly string _sessionId;
    private int _calls;

    private GovernedHostedPowerShellFixture(string root)
    {
        var directory = Path.GetFullPath(root);
        if (!directory.Contains(Path.DirectorySeparatorChar + ".mcpServer" + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Task-owned synthetic root required.", nameof(root));
        Directory.CreateDirectory(directory);
        _http = new HttpClient(new NoNetworkHandler());
        var client = new QBrainAiClient(_http, new QBrainAiClientOptions
        {
            BaseUrl = new Uri("http://hosted-fidelity.invalid/"),
            ApiKey = "synthetic-owned-fixture-key", WorkspacePath = directory,
        });
        var options = Options.Create(new McpAgentOptions
        {
            BaseUrl = new Uri("http://hosted-fidelity.invalid/"),
            ApiKey = "synthetic-owned-fixture-key", WorkspacePath = directory, SourceType = "Codex",
        });
        var identifiers = new McpSessionIdentifierFactory(options, TimeProvider.System);
        _services = new ServiceCollection().BuildServiceProvider();
        var agent = new McpHostedAgent(client, identifiers,
            new ChatClientAgentOptions { Id = "owned-fidelity", Name = "Owned fidelity" }, options,
            new QBrainAi.McpAgent.SessionLog.SessionLogWorkflow(client, identifiers, TimeProvider.System),
            new QBrainAi.McpAgent.Todo.TodoWorkflow(client), new RequirementsWorkflow(client.Requirements),
            new GenericClientPassthrough(client),
            new QBrainAi.Repl.Core.SessionLogWorkflow(new SessionLogClientAdapter(client.SessionLog), TimeProvider.System), _services);
        _sessions = agent.PowerShellSessions;
        var created = _sessions.CreateSession(directory);
        _sessionId = created.Success && !string.IsNullOrWhiteSpace(created.SessionId)
            ? created.SessionId : throw new InvalidOperationException(created.ErrorMessage ?? "Hosted session creation failed.");
    }

    /// <summary>Creates one actual public hosted agent and its persistent PowerShell session.</summary>
    /// <param name="root">Private synthetic artifact directory.</param>
    /// <returns>A fixture using the existing hosted PowerShell lifecycle.</returns>
    public static GovernedHostedPowerShellFixture Create(string root) => new(root);

    /// <summary>Passes literal JSON text through the actual existing PowerShell command API.</summary>
    /// <param name="json">Untrusted JSON content encoded as data, never executable script.</param>
    /// <returns>Full actual command result and invocation evidence as native JSON.</returns>
    public string RoundTrip(string json)
    {
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        // The existing display API normalizes text; a data envelope retains exact payload bytes.
        var command = "[Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes([Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('" + encoded + "'))))";
        var result = _sessions.ExecuteCommandAsync(_sessionId, command).GetAwaiter().GetResult();
        var payloadJson = result.Success && !result.HadErrors
            ? Encoding.UTF8.GetString(Convert.FromBase64String(result.Output)) : null;
        return JsonSerializer.Serialize(new { result, payloadJson, calls = ++_calls, sessionId = _sessionId,
            method = nameof(IHostedPowerShellSessionManager.ExecuteCommandAsync) });
    }

    /// <summary>Gets the actual existing persistent hosted PowerShell session identifier.</summary>
    public string HostedSessionId => _sessionId;

    /// <summary>Executes trusted script with native named objects through the existing manager.</summary>
    /// <param name="command">The common contract's trusted command text.</param>
    /// <param name="parameters">Named object values whose references must remain intact.</param>
    /// <param name="cancelled">Whether the token is cancelled before entering the existing gate.</param>
    /// <returns>The actual existing command result, including errors and literal output.</returns>
    public PowerShellSessionCommandResult ExecuteNamedParameters(
        string command, IDictionary parameters, bool cancelled)
    {
        if (_sessions is not IHostedPowerShellObjectSessionManager objectSessions)
            throw new InvalidOperationException("Existing hosted manager lacks named-object capability.");
        var named = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (DictionaryEntry entry in parameters)
        {
            if (entry.Key is not string name)
                throw new ArgumentException("Named parameter keys must be strings.", nameof(parameters));
            named.Add(name, entry.Value);
        }
        using var cancellation = new CancellationTokenSource();
        if (cancelled)
            cancellation.Cancel();
        return objectSessions.ExecuteCommandAsync(_sessionId, command, named, cancellation.Token)
            .GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _sessions.Dispose();
        _http.Dispose();
        _services.Dispose();
    }

    private sealed class NoNetworkHandler : HttpMessageHandler
    {
        /// <inheritdoc />
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => throw new InvalidOperationException("Hosted fidelity fixture forbids network requests.");
    }
}
