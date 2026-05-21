using System.Net.Http.Json;
using McpServer.Support.Mcp.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-103: LocalProxy background worker that replays durable queued writes to the hub.
/// </summary>
public sealed class FederationQueuedOperationReplayService : BackgroundService
{
    private const int BatchSize = 25;

    private readonly FederationRegistry _registry;
    private readonly IFederationTopologyService _topologyService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<FederationOptions> _options;
    private readonly ILogger<FederationQueuedOperationReplayService> _logger;

    /// <summary>Initializes a new instance of the <see cref="FederationQueuedOperationReplayService"/> class.</summary>
    /// <param name="registry">Federation registry.</param>
    /// <param name="topologyService">Topology and durable operation service.</param>
    /// <param name="httpClientFactory">HTTP client factory.</param>
    /// <param name="options">Federation options.</param>
    /// <param name="logger">Logger.</param>
    public FederationQueuedOperationReplayService(
        FederationRegistry registry,
        IFederationTopologyService topologyService,
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<FederationOptions> options,
        ILogger<FederationQueuedOperationReplayService> logger)
    {
        _registry = registry;
        _topologyService = topologyService;
        _httpClientFactory = httpClientFactory;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ReplayOnceAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Federation queued-operation replay cycle failed.");
            }

            var interval = Math.Max(1, _options.CurrentValue.Sync.ReplayIntervalSeconds);
            await Task.Delay(TimeSpan.FromSeconds(interval), stoppingToken).ConfigureAwait(false);
        }
    }

    /// <summary>Runs one replay cycle. Exposed for focused tests and operational probes.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task ReplayOnceAsync(CancellationToken cancellationToken)
    {
        var options = _options.CurrentValue;
        if (!_registry.IsEnabled ||
            _registry.EffectiveRole != FederationRole.LocalProxy ||
            !options.Queue.Enabled ||
            string.IsNullOrWhiteSpace(_registry.HubBaseUrl) ||
            string.IsNullOrWhiteSpace(_registry.ProxyId))
        {
            return;
        }

        var pending = await _topologyService
            .ListPendingOperationsAsync(_registry.ProxyId, BatchSize, options.Queue.MaxReplayAttempts, cancellationToken)
            .ConfigureAwait(false);

        foreach (var operation in pending)
            await ReplayOperationAsync(operation, options.Queue.MaxReplayAttempts, cancellationToken).ConfigureAwait(false);
    }

    private async Task ReplayOperationAsync(
        FederationOperationReplayItem operation,
        int maxAttempts,
        CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{_registry.HubBaseUrl}/mcpserver/federation/operations")
            {
                Content = JsonContent.Create(operation.ToRequest()),
            };
            request.Headers.TryAddWithoutValidation(FederationHeaders.ProxyId, operation.ProxyId);
            request.Headers.TryAddWithoutValidation(FederationHeaders.OperationId, operation.OperationId);

            using var client = _httpClientFactory.CreateClient(FederationProxyService.HttpClientName);
            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                await _topologyService.MarkReplayFailureAsync(
                        operation.OperationId,
                        $"Hub returned {(int)response.StatusCode}.",
                        maxAttempts,
                        cancellationToken)
                    .ConfigureAwait(false);
                return;
            }

            var hubResponse = await response.Content
                .ReadFromJsonAsync<FederationOperationResponse>(cancellationToken)
                .ConfigureAwait(false);
            var status = string.Equals(hubResponse?.Status, "conflict", StringComparison.OrdinalIgnoreCase)
                ? "conflict"
                : "acknowledged";

            await _topologyService.AcknowledgeOperationAsync(
                    operation.OperationId,
                    new FederationOperationAckRequest { Status = status },
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            await _topologyService.MarkReplayFailureAsync(
                    operation.OperationId,
                    ex.Message,
                    maxAttempts,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
