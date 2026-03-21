using McpServer.Common.Copilot;

namespace McpServer.Support.Mcp.Services;

internal sealed class CopilotCliAgentExecutionStrategy(ICopilotClient copilotClient) : IAgentExecutionStrategy
{
    public string Name => AgentExecutionStrategyNames.CopilotCli;

    public ValueTask<IAgentExecutionSession> CreateSessionAsync(
        AgentExecutionSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var session = copilotClient.CreateInteractiveSession(request.InitialPrompt, request.Options);
        return ValueTask.FromResult<IAgentExecutionSession>(new CopilotCliAgentExecutionSession(session));
    }

    private sealed class CopilotCliAgentExecutionSession(CopilotInteractiveSession session) : IAgentExecutionSession
    {
        public bool IsAlive => session.IsAlive;

        public int? ProcessId => session.ProcessId;

        public Task<CopilotResult> ReadInitialResponseAsync(CancellationToken cancellationToken = default) =>
            session.ReadInitialResponseAsync(cancellationToken);

        public IAsyncEnumerable<string> ReadInitialResponseStreamingAsync(CancellationToken cancellationToken = default) =>
            session.ReadInitialResponseStreamingAsync(cancellationToken);

        public Task<CopilotResult> SendAsync(string prompt, CancellationToken cancellationToken = default) =>
            session.SendAsync(prompt, cancellationToken);

        public IAsyncEnumerable<string> SendStreamingAsync(string prompt, CancellationToken cancellationToken = default) =>
            session.SendStreamingAsync(prompt, cancellationToken);

        public Task SendEscapeAsync(CancellationToken cancellationToken = default) =>
            session.SendEscapeAsync(cancellationToken);

        public Task EndAsync(TimeSpan timeout) => session.EndAsync(timeout);

        public ValueTask DisposeAsync() => session.DisposeAsync();
    }
}
