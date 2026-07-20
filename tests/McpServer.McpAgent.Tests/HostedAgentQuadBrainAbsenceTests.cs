using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using McpServer.Client;
using McpServer.McpAgent.Hosting;
using McpServer.Repl.Core;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace McpServer.McpAgent.Tests;

/// <summary>
/// TEST-MCP-QBABSENCE-002: Absence tests proving that no QuadBrain-named tool leaks into the shared
/// hosted-agent tool catalog. The catalog produced by <see cref="McpHostedAgentToolAdapter"/> is used
/// by every <c>McpServer.McpAgent</c> host, not only by QBAgent, so any QuadBrain tool name there is
/// exposed to general agents. The tests build a real hosted agent with an in-memory transport and scan
/// every model-visible tool name plus the published ACID profile tool lists.
/// </summary>
public sealed class HostedAgentQuadBrainAbsenceTests
{
    private static readonly string TestWorkspacePath =
        Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory);

    /// <summary>
    /// TEST-MCP-QBABSENCE-002: Verifies the default hosted-agent registration exposes no tool whose
    /// name contains "quadbrain" in any casing. The test uses a hosted agent built over a throwing
    /// HTTP handler because building the tool catalog must not require any transport call.
    /// </summary>
    [Fact]
    public void Registration_Functions_ContainNoQuadBrainToolName()
    {
        var hostedAgent = CreateHostedAgent();

        var toolNames = hostedAgent.Registration.Functions
            .Select(static function => function.Name)
            .ToArray();

        Assert.DoesNotContain(
            toolNames,
            static name => name.Contains("quadbrain", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// TEST-MCP-QBABSENCE-002: Verifies the model-visible run-option tool surface exposes no
    /// QuadBrain-named tool for the default execution profile used by general hosted agents.
    /// </summary>
    [Fact]
    public void CreateRunOptions_AttachedTools_ContainNoQuadBrainToolName()
    {
        var hostedAgent = CreateHostedAgent();

        var runOptions = hostedAgent.CreateRunOptions();
        var toolNames = runOptions.ChatOptions?.Tools?
            .Select(static tool => tool.Name)
            .ToArray() ?? [];

        Assert.NotEmpty(toolNames);
        Assert.DoesNotContain(
            toolNames,
            static name => name.Contains("quadbrain", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// TEST-MCP-QBABSENCE-002: Verifies that the published ACID hosted-agent profile lists no
    /// QuadBrain-named tool in either its allowed or blocked tool names, so the name is absent from
    /// the shared catalog contract rather than merely filtered out of it.
    /// </summary>
    [Fact]
    public void QBAgentDefinition_ToolNameLists_ContainNoQuadBrainToolName()
    {
        var definition = QBAgentDefinition.Instance;

        Assert.DoesNotContain(
            definition.AllowedToolNames,
            static name => name.Contains("quadbrain", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(
            definition.BlockedToolNames,
            static name => name.Contains("quadbrain", StringComparison.OrdinalIgnoreCase));
    }

    private static McpHostedAgent CreateHostedAgent()
    {
        var handler = new ThrowingHttpMessageHandler();
        var httpClient = new HttpClient(handler);
        var client = new McpServerClient(
            httpClient,
            new McpServerClientOptions
            {
                ApiKey = "test-key",
                BaseUrl = new Uri("http://localhost:7147"),
                WorkspacePath = TestWorkspacePath,
            });
        var timeProvider = new FixedUtcTimeProvider(new DateTimeOffset(2026, 03, 09, 15, 01, 05, TimeSpan.Zero));
        var configuredOptions = new McpAgentOptions
        {
            ApiKey = "test-key",
            BaseUrl = new Uri("http://localhost:7147"),
            SourceType = "Codex",
            WorkspacePath = TestWorkspacePath,
        };
        var options = Options.Create(configuredOptions);
        var identifiers = new McpSessionIdentifierFactory(options, timeProvider);
        var sessionLog = new McpServer.McpAgent.SessionLog.SessionLogWorkflow(client, identifiers, timeProvider);
        var todo = new McpServer.McpAgent.Todo.TodoWorkflow(client);
        var requirements = new RequirementsWorkflow(client.Requirements);
        var clientPassthrough = new GenericClientPassthrough(client);
        var replSessionLogAdapter = new SessionLogClientAdapter(client.SessionLog);
        var replSessionLog = new McpServer.Repl.Core.SessionLogWorkflow(replSessionLogAdapter, timeProvider);
        var serviceProvider = new ServiceCollection().BuildServiceProvider();

        return new McpHostedAgent(
            client,
            identifiers,
            new ChatClientAgentOptions
            {
                Description = configuredOptions.Description,
                Id = configuredOptions.AgentId,
                Name = configuredOptions.AgentName,
            },
            options,
            sessionLog,
            todo,
            requirements,
            clientPassthrough,
            replSessionLog,
            serviceProvider);
    }

    /// <summary>
    /// TEST-MCP-QBABSENCE-002: Fails the test if the tool catalog build attempts any HTTP call.
    /// </summary>
    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        /// <summary>
        /// TEST-MCP-QBABSENCE-002: Throws because catalog construction must not emit transport calls.
        /// </summary>
        /// <param name="request">The unexpected outbound request.</param>
        /// <param name="cancellationToken">Ignored.</param>
        /// <returns>Never returns successfully.</returns>
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException(
                $"Unexpected MCP request '{request.RequestUri}' during tool catalog construction.");
    }

    /// <summary>
    /// TEST-MCP-QBABSENCE-002: Provides a deterministic clock for the hosted-agent construction.
    /// </summary>
    private sealed class FixedUtcTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        /// <summary>
        /// TEST-MCP-QBABSENCE-002: Initializes the deterministic clock.
        /// </summary>
        /// <param name="utcNow">The fixed UTC timestamp.</param>
        public FixedUtcTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow.ToUniversalTime();
        }

        /// <summary>
        /// TEST-MCP-QBABSENCE-002: Returns the fixed UTC timestamp.
        /// </summary>
        /// <returns>The fixed UTC timestamp.</returns>
        public override DateTimeOffset GetUtcNow() => _utcNow;
    }
}
