// FR-MCP-REPL-001: YAML Protocol STDIO REPL Host - Service registration extensions
// TR-MCP-REPL-002: DI-Integrated REPL Host - Service collection configuration
// TR-MCP-REPL-005: Namespace Organization and Handler Parity - Workflow DI registration

// FR-MCP-REPL-001: YAML Protocol STDIO REPL Host - DI service registration
// TR-MCP-REPL-002: DI-Integrated REPL Host - Service composition root
// TR-MCP-REPL-004: Command Registry and Dispatcher - Workflow handler registration
// TEST-MCP-REPL-016: All dependencies resolved from DI container

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using QBrainAi.Repl.Core;
using QBrainAi.TransactionSecurity.Options;
using QBrainAi.TransactionSecurity.Services;

namespace QBrainAi.Repl.Host;

/// <summary>
/// Extension methods for configuring REPL services in the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds REPL core services to the service collection.
    /// Registers protocol handlers, workspace selectors, marker file readers, and auth rotation handlers.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="workspacePathOverride">Optional workspace root used for per-workspace workflow state.</param>
    /// <param name="agentOverride">Optional agent identifier used for per-agent workflow state.</param>
    /// <returns>The configured service collection for fluent chaining.</returns>
    public static IServiceCollection AddReplCoreServices(this IServiceCollection services, string? workspacePathOverride = null, string? agentOverride = null)
    {
        // Register TODO workflow (implementation lives in QBrainAi.Repl.Core).
        services.AddSingleton<ITodoSelectionStore>(_ =>
            FileTodoSelectionStore.CreateForWorkspace(workspacePathOverride, agentOverride));
        services.AddSingleton<TodoWorkflow>(sp =>
        {
            var clientFactory = sp.GetRequiredService<QBrainAi.Client.QBrainAiClient>();
            return new QBrainAi.Repl.Core.TodoWorkflow(
                clientFactory.Todo,
                sp.GetRequiredService<ITodoSelectionStore>());
        });
        services.AddSingleton<ITodoWorkflow>(sp =>
        {
            var clientFactory = sp.GetRequiredService<QBrainAi.Client.QBrainAiClient>();
            return new TransactionalTodoWorkflow(
                sp.GetRequiredService<TodoWorkflow>(),
                clientFactory.Todo,
                sp.GetService<ITurnTransactionCoordinator>(),
                sp.GetService<IOptions<TurnTransactionOptions>>());
        });

        // Register GraphRAG workflow (implementation lives in QBrainAi.Repl.Core)
        // FR-MCP-078/079/080, TR-GRAPHRAG-ADHOC-001/002/003
        services.AddSingleton<IGraphRagWorkflow>(sp =>
        {
            var clientFactory = sp.GetRequiredService<QBrainAi.Client.QBrainAiClient>();
            return new QBrainAi.Repl.Core.GraphRagWorkflow(clientFactory.Context);
        });

        // Register memory workflow for the workflow.memory namespace.
        services.AddSingleton<IMemoryWorkflow>(sp =>
        {
            var clientFactory = sp.GetRequiredService<QBrainAi.Client.QBrainAiClient>();
            return new QBrainAi.Repl.Core.MemoryWorkflow(clientFactory.Memory);
        });

        // Register triage workflow for the workflow.triage namespace.
        services.AddSingleton<ITriageWorkflow>(sp =>
        {
            var clientFactory = sp.GetRequiredService<QBrainAi.Client.QBrainAiClient>();
            return new QBrainAi.Repl.Core.TriageWorkflow(clientFactory.Triage);
        });

        services.AddSingleton<IHandoffWorkflow>(sp =>
        {
            var clientFactory = sp.GetRequiredService<QBrainAi.Client.QBrainAiClient>();
            return new QBrainAi.Repl.Core.HandoffWorkflow(clientFactory.Handoff);
        });

        services.AddSingleton<IHostileReviewWorkflow>(sp =>
        {
            var clientFactory = sp.GetRequiredService<QBrainAi.Client.QBrainAiClient>();
            return new QBrainAi.Repl.Core.HostileReviewWorkflow(clientFactory.HostileReview);
        });

        services.AddSingleton<IWorkspaceValidationWorkflow>(sp =>
        {
            var clientFactory = sp.GetRequiredService<QBrainAi.Client.QBrainAiClient>();
            return new QBrainAi.Repl.Core.WorkspaceValidationWorkflow(clientFactory.WorkspaceValidation);
        });

        // Register Agent Help workflow for the workflow.agenthelp namespace.
        services.AddSingleton<IAgentHelpWorkflow>(sp =>
        {
            var clientFactory = sp.GetRequiredService<QBrainAi.Client.QBrainAiClient>();
            return new QBrainAi.Repl.Core.AgentHelpWorkflow(clientFactory.AgentHelp);
        });

        // Register requirements workflow so agent plugins can invoke the
        // workflow.requirements namespace without falling back to raw REST.
        services.AddSingleton<IRequirementsWorkflow>(sp =>
        {
            var clientFactory = sp.GetRequiredService<QBrainAi.Client.QBrainAiClient>();
            return new QBrainAi.Repl.Core.RequirementsWorkflow(clientFactory.Requirements);
        });

        // Register session-log workflow for the workflow.sessionlog namespace and recovery import.
        services.AddSingleton<ISessionLogWorkflow>(sp =>
        {
            var clientFactory = sp.GetRequiredService<QBrainAi.Client.QBrainAiClient>();
            return new QBrainAi.Repl.Core.SessionLogWorkflow(clientFactory.SessionLog, TimeProvider.System);
        });

        // Register transcript ingestion workflow for repl.sessionlog.ingestTranscripts and normalizeTranscripts.
        services.AddSingleton<ITranscriptIngestionWorkflow>(sp =>
        {
            var clientFactory = sp.GetRequiredService<QBrainAi.Client.QBrainAiClient>();
            return new QBrainAi.Repl.Core.TranscriptIngestionWorkflow(clientFactory.SessionLog);
        });
        // Register YAML protocol primitives used by agent-stdio mode.
        // FR-MCP-REPL-001, TR-MCP-REPL-001/003/004: YAML envelope serialization,
        // generic client passthrough, command dispatcher, and stream-level protocol loop.
        services.AddSingleton<IYamlSerializer, YamlSerializer>();
        services.AddSingleton<ISessionLogPersistenceStrategy>(sp =>
        {
            var clientFactory = sp.GetRequiredService<QBrainAi.Client.QBrainAiClient>();
            var primary = new McpSessionLogPersistenceStrategy(
                new SessionLogClientAdapter(clientFactory.SessionLog));
            var failsafe = new FilesystemSessionLogPersistenceStrategy(
                MarkerFileClientOptionsResolver.ResolveWorkspacePath(),
                sp.GetRequiredService<IYamlSerializer>(),
                TimeProvider.System);
            return new FailoverSessionLogPersistenceStrategy(primary, failsafe);
        });
        services.AddSingleton<IClientMutationPolicy>(sp =>
            new KnownUnsafeClientMutationPolicy(() =>
            {
                var coordinator = sp.GetService<ITurnTransactionCoordinator>();
                if (coordinator is null)
                    return new ClientMutationPolicyState(RequiredForMutations: false);

                var status = coordinator.GetStatus();
                var options = sp.GetService<IOptions<TurnTransactionOptions>>()?.Value;
                var required = status.Enabled && (options?.RequiredForMutations ?? true);
                return new ClientMutationPolicyState(required, status.Degraded, status.Message);
            }));
        services.AddSingleton<IGenericClientPassthrough>(sp =>
            new GenericClientPassthrough(
                sp.GetRequiredService<QBrainAi.Client.QBrainAiClient>(),
                sp.GetRequiredService<IClientMutationPolicy>()));
        services.AddSingleton<IReplCommandDispatcher>(sp =>
            new ReplCommandDispatcher(
                sp.GetRequiredService<IGenericClientPassthrough>(),
                sp.GetRequiredService<ISessionLogWorkflow>(),
                sp.GetRequiredService<IRequirementsWorkflow>(),
                sp.GetRequiredService<ITodoWorkflow>(),
                sp.GetRequiredService<IMemoryWorkflow>(),
                sp.GetRequiredService<IClientMutationPolicy>(),
                sp.GetRequiredService<IGraphRagWorkflow>(),
                sp.GetRequiredService<ITriageWorkflow>(),
                sp.GetRequiredService<IAgentHelpWorkflow>(),
                sp.GetRequiredService<ISessionLogPersistenceStrategy>(),
                sp.GetRequiredService<ITranscriptIngestionWorkflow>(),
                sp.GetRequiredService<IHandoffWorkflow>(),
                sp.GetRequiredService<IHostileReviewWorkflow>(),
                sp.GetRequiredService<IWorkspaceValidationWorkflow>()));
        services.AddSingleton<IAgentStdioProtocol>(sp =>
            new AgentStdioProtocol(
                sp.GetRequiredService<IYamlSerializer>(),
                sp.GetRequiredService<IReplCommandDispatcher>()));

        return services;
    }
}
