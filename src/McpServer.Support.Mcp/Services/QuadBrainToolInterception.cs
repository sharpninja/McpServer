using McpServer.Support.Mcp.Models;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-QBEXEC-001: Classifies a tool the model elected to call as MCP-internal (executed server-side by
/// QuadBrain) or external (emitted to the agent for execution).
/// </summary>
public interface IQuadBrainToolClassifier
{
    /// <summary>Returns true when the tool is exposed by McpServer itself and should run server-side.</summary>
    /// <param name="toolName">The elected tool name.</param>
    bool IsInternal(string toolName);
}

/// <summary>
/// FR-MCP-QBEXEC-001: Default classifier. MCP-internal tools are those exposed by McpServer, named with the
/// <c>mcp_</c> prefix (session, todo, requirements, repo, graphrag, memory, brain-slot, etc.). Everything else
/// is external and is left for the agent to execute.
/// </summary>
public sealed class QuadBrainToolClassifier : IQuadBrainToolClassifier
{
    /// <summary>The prefix identifying McpServer-exposed tools.</summary>
    public const string InternalPrefix = "mcp_";

    /// <inheritdoc />
    public bool IsInternal(string toolName)
        => !string.IsNullOrWhiteSpace(toolName)
           && toolName.Trim().StartsWith(InternalPrefix, StringComparison.OrdinalIgnoreCase);
}

/// <summary>FR-MCP-QBEXEC-001: Outcome of attempting to execute an MCP-internal tool server-side.</summary>
/// <param name="Handled">Whether the executor recognized and attempted the tool.</param>
/// <param name="Success">Whether server-side execution (and any required transaction commit) succeeded.</param>
/// <param name="ResultJson">Optional JSON result of the executed tool.</param>
/// <param name="Error">Optional failure detail.</param>
public sealed record InternalToolExecutionOutcome(bool Handled, bool Success, string? ResultJson, string? Error)
{
    /// <summary>An outcome indicating the executor does not handle this tool (it must be left for the agent).</summary>
    public static InternalToolExecutionOutcome Unhandled { get; } = new(false, false, null, null);

    /// <summary>Creates a successful outcome.</summary>
    public static InternalToolExecutionOutcome Ok(string? resultJson = null) => new(true, true, resultJson, null);

    /// <summary>Creates a handled-but-failed outcome.</summary>
    public static InternalToolExecutionOutcome Fail(string error) => new(true, false, null, error);
}

/// <summary>
/// FR-MCP-QBEXEC-001: Executes an MCP-internal tool call server-side. Mutating TODO/Requirements calls are
/// gated through the turn transaction and applied on commit; other internal tools are applied directly.
/// </summary>
public interface IQuadBrainInternalToolExecutor
{
    /// <summary>Attempts to execute an internal tool call server-side.</summary>
    /// <param name="toolCall">The elected tool call.</param>
    /// <param name="turnId">Owning turn id, when present.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The execution outcome; <see cref="InternalToolExecutionOutcome.Unhandled"/> when not supported.</returns>
    Task<InternalToolExecutionOutcome> TryExecuteAsync(
        OpenAiToolCall toolCall,
        string? turnId,
        CancellationToken cancellationToken = default);
}

/// <summary>FR-MCP-QBEXEC-001: Executor that handles nothing - every internal tool is left for the agent. Used
/// as the safe default until a concrete server-side executor is registered.</summary>
public sealed class NoopInternalToolExecutor : IQuadBrainInternalToolExecutor
{
    /// <summary>Singleton instance.</summary>
    public static NoopInternalToolExecutor Instance { get; } = new();

    /// <inheritdoc />
    public Task<InternalToolExecutionOutcome> TryExecuteAsync(
        OpenAiToolCall toolCall,
        string? turnId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(InternalToolExecutionOutcome.Unhandled);
}

/// <summary>FR-MCP-QBEXEC-001: One internal tool call executed server-side, retained for session-log/audit.</summary>
/// <param name="ToolCall">The executed call.</param>
/// <param name="Outcome">The execution outcome.</param>
public sealed record ExecutedInternalTool(OpenAiToolCall ToolCall, InternalToolExecutionOutcome Outcome);

/// <summary>FR-MCP-QBEXEC-001: Result of partitioning + executing the AoT-elected tool calls.</summary>
/// <param name="RemainingToolCalls">External calls ONLY - emitted to the agent as tool commands.</param>
/// <param name="Executed">Internal calls executed server-side successfully and stripped from the response.</param>
/// <param name="Failed">Internal calls that failed or had no server-side executor - never sent to the agent as tool commands; logged as Session Log failures and surfaced to the agent as a note.</param>
public sealed record ToolInterceptionResult(
    IReadOnlyList<OpenAiToolCall> RemainingToolCalls,
    IReadOnlyList<ExecutedInternalTool> Executed,
    IReadOnlyList<ExecutedInternalTool> Failed);

/// <summary>
/// FR-MCP-QBEXEC-001: Partitions AoT-elected tool calls into MCP-internal vs external, executes the internal
/// ones server-side via <see cref="IQuadBrainInternalToolExecutor"/>, and strips the successfully executed
/// internal calls so only external (and any unhandled internal) calls are emitted to the agent.
/// </summary>
public sealed class QuadBrainToolInterceptor
{
    private readonly IQuadBrainToolClassifier _classifier;
    private readonly IQuadBrainInternalToolExecutor _executor;

    /// <summary>Initializes a new instance of the <see cref="QuadBrainToolInterceptor"/> class.</summary>
    /// <param name="classifier">Internal/external tool classifier.</param>
    /// <param name="executor">Server-side internal tool executor.</param>
    public QuadBrainToolInterceptor(IQuadBrainToolClassifier classifier, IQuadBrainInternalToolExecutor executor)
    {
        _classifier = classifier ?? throw new ArgumentNullException(nameof(classifier));
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
    }

    /// <summary>Intercepts the elected tool calls, executing internal ones server-side and stripping them.</summary>
    /// <param name="toolCalls">The tool calls elected by QuadBrain.</param>
    /// <param name="turnId">Owning turn id, when present.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The remaining (external/unhandled) calls and the executed internal calls.</returns>
    public async Task<ToolInterceptionResult> InterceptAsync(
        IReadOnlyList<OpenAiToolCall> toolCalls,
        string? turnId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(toolCalls);

        var remaining = new List<OpenAiToolCall>();
        var executed = new List<ExecutedInternalTool>();
        var failed = new List<ExecutedInternalTool>();

        foreach (var call in toolCalls)
        {
            if (!_classifier.IsInternal(call.Function.Name))
            {
                remaining.Add(call); // external -> emitted to the agent as a tool command
                continue;
            }

            var outcome = await _executor.TryExecuteAsync(call, turnId, cancellationToken).ConfigureAwait(false);
            if (outcome.Handled && outcome.Success)
            {
                executed.Add(new ExecutedInternalTool(call, outcome)); // stripped from response
            }
            else
            {
                // Internal tool failure / no executor: NEVER send to the agent as a tool command. It is logged
                // as a Session Log failure and surfaced to the agent as a note instead.
                var failure = outcome.Handled
                    ? outcome
                    : InternalToolExecutionOutcome.Fail($"No server-side executor is registered for internal tool '{call.Function.Name}'.");
                failed.Add(new ExecutedInternalTool(call, failure));
            }
        }

        return new ToolInterceptionResult(remaining, executed, failed);
    }
}
