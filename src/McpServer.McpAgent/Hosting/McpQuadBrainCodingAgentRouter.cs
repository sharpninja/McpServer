using McpServer.Client;
using McpServer.Client.Models;

namespace McpServer.McpAgent.Hosting;

internal static class McpQuadBrainCodingAgentRouter
{
    private const string DefaultTaskKind = "coding";

    internal static Task<QuadBrainOrchestrationResponse> ExecuteAsync(
        McpServerClient client,
        McpAgentOptions options,
        McpQuadBrainCodingAgentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(options);

        return client.BrainSlots.OrchestrateAsync(
            CreateOrchestrationRequest(options, request),
            cancellationToken);
    }

    internal static QuadBrainOrchestrationRequest CreateOrchestrationRequest(
        McpAgentOptions options,
        McpQuadBrainCodingAgentRequest request)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Prompt))
            throw new ArgumentException("A coding prompt is required.", nameof(request));

        var taskKind = string.IsNullOrWhiteSpace(request.TaskKind)
            ? DefaultTaskKind
            : request.TaskKind.Trim();
        var metadata = new Dictionary<string, string>(
            request.Metadata ?? new Dictionary<string, string>(),
            StringComparer.Ordinal)
        {
            ["codingAgent.surface"] = "Microsoft.AgentFramework",
            ["codingAgent.taskKind"] = taskKind,
            ["codingAgent.executionProfile"] = options.ExecutionProfile.ToString(),
            ["codingAgent.sourceType"] = options.SourceType,
            ["codingAgent.agentId"] = options.AgentId,
            ["codingAgent.agentName"] = options.AgentName,
        };

        return new QuadBrainOrchestrationRequest
        {
            Input = request.Prompt,
            TurnId = request.TurnId,
            AdmitCuriosityToGraphRag = request.AdmitCuriosityToGraphRag,
            Metadata = metadata,
        };
    }
}
