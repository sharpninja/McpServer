using System;
using System.Threading;
using System.Threading.Tasks;

namespace McpServer.UI.Core.Services;

/// <summary>Service that sends a user message and current log context to a local/remote LLM and returns the assistant reply.</summary>
public interface ILogAgentService
{
    /// <summary>Sends the user message with the given context to the agent and returns the assistant's reply. Model is optional (e.g. Ollama model name).
    /// When <paramref name="contentProgress"/> is set, the implementation may stream and report accumulated content so the UI can show progress.</summary>
    Task<string> SendMessageAsync(string userMessage, string contextSummary, string? model = null, IProgress<string>? contentProgress = null, CancellationToken cancellationToken = default);
}

