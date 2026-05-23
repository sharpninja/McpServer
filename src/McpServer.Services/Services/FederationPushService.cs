using McpServer.Support.Mcp.Models;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-085: Service interface for pushing local data to a remote federation target.
/// </summary>
public interface IFederationPushService
{
    /// <summary>Push all local TODO items to the remote target.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Push result with success/failure counts.</returns>
    Task<FederationPushResult> PushTodosAsync(CancellationToken ct = default);

    /// <summary>Push all local session logs to the remote target.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Push result with success/failure counts.</returns>
    Task<FederationPushResult> PushSessionLogsAsync(CancellationToken ct = default);

    /// <summary>Push all local TODO items and session logs to the remote target.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Combined push result with success/failure counts.</returns>
    Task<FederationPushResult> PushAllAsync(CancellationToken ct = default);
}

/// <summary>
/// FR-MCP-085: Implementation that queries local services for data and pushes it
/// to a resolved federation target via <see cref="IFederationDataClient"/>.
/// </summary>
public sealed class FederationPushService : IFederationPushService
{
    private readonly ITodoService _todoService;
    private readonly ISessionLogService _sessionLogService;
    private readonly IFederationDataClient _client;
    private readonly FederationRegistry _registry;
    private readonly ILogger<FederationPushService> _logger;

    /// <summary>Initializes a new instance of the <see cref="FederationPushService"/> class.</summary>
    /// <param name="todoService">Local TODO service to query items from.</param>
    /// <param name="sessionLogService">Local session log service to query logs from.</param>
    /// <param name="client">Federation data client for pushing to remote.</param>
    /// <param name="registry">Federation registry for target resolution.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public FederationPushService(
        ITodoService todoService,
        ISessionLogService sessionLogService,
        IFederationDataClient client,
        FederationRegistry registry,
        ILogger<FederationPushService> logger)
    {
        _todoService = todoService;
        _sessionLogService = sessionLogService;
        _client = client;
        _registry = registry;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<FederationPushResult> PushTodosAsync(CancellationToken ct = default)
    {
        var target = ResolveTargetOrFail();
        if (target is null)
            return MakeError("No federation target resolved");

        try
        {
            var query = await _todoService.QueryAsync(new TodoQueryRequest(), ct).ConfigureAwait(false);
            return await _client.PushTodosAsync(target, query.Items, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to push TODOs to {Target}", target.Name);
            return new FederationPushResult(0, 1, [ex.Message]);
        }
    }

    /// <inheritdoc />
    public async Task<FederationPushResult> PushSessionLogsAsync(CancellationToken ct = default)
    {
        var target = ResolveTargetOrFail();
        if (target is null)
            return MakeError("No federation target resolved");

        try
        {
            var query = await _sessionLogService.QueryAsync(new SessionLogQueryRequest(), ct).ConfigureAwait(false);
            return await _client.PushSessionLogsAsync(target, query.Items, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to push session logs to {Target}", target.Name);
            return new FederationPushResult(0, 1, [ex.Message]);
        }
    }

    /// <inheritdoc />
    public async Task<FederationPushResult> PushAllAsync(CancellationToken ct = default)
    {
        if (!_registry.IsEnabled)
            return new FederationPushResult(0, 1, ["Federation is disabled"]);

        var target = _registry.ResolveTarget(null);
        if (target is null)
            return new FederationPushResult(0, 1, ["No federation target resolved"]);

        var todoResult = await PushTodosAsync(ct).ConfigureAwait(false);
        var sessionResult = await PushSessionLogsAsync(ct).ConfigureAwait(false);

        var errors = new List<string>(todoResult.Errors);
        errors.AddRange(sessionResult.Errors);

        return new FederationPushResult(
            todoResult.Succeeded + sessionResult.Succeeded,
            todoResult.Failed + sessionResult.Failed,
            errors);
    }

    private FederationTarget? ResolveTargetOrFail()
    {
        if (!_registry.IsEnabled)
            return null;

        return _registry.ResolveTarget(null);
    }

    private static FederationPushResult MakeError(string message)
        => new(0, 1, [message]);
}
