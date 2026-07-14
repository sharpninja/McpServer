using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using McpServer.Support.Mcp.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-103: LocalProxy background worker that polls hub fanout rows,
/// applies signed state operations locally, and acknowledges recipient rows.
/// </summary>
public sealed class FederationFanoutSyncService : BackgroundService
{
    private readonly FederationRegistry _registry;
    private readonly IFederationOperationApplyService _applyService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<FederationOptions> _options;
    private readonly IFederationEnvelopeSigner? _envelopeSigner;
    private readonly IFederationLocalExecutionService? _localExecutionService;
    private readonly ILogger<FederationFanoutSyncService> _logger;
    private long _lastSequence;

    /// <summary>Initializes a new instance of the <see cref="FederationFanoutSyncService"/> class.</summary>
    /// <param name="registry">Federation registry.</param>
    /// <param name="applyService">Operation apply service.</param>
    /// <param name="httpClientFactory">HTTP client factory.</param>
    /// <param name="options">Federation options.</param>
    /// <param name="envelopeSigner">Optional envelope verifier.</param>
    /// <param name="localExecutionService">Optional local execution service for signed hub envelopes.</param>
    /// <param name="logger">Logger.</param>
    public FederationFanoutSyncService(
        FederationRegistry registry,
        IFederationOperationApplyService applyService,
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<FederationOptions> options,
        IFederationEnvelopeSigner? envelopeSigner,
        IFederationLocalExecutionService? localExecutionService,
        ILogger<FederationFanoutSyncService> logger)
    {
        _registry = registry;
        _applyService = applyService;
        _httpClientFactory = httpClientFactory;
        _options = options;
        _envelopeSigner = envelopeSigner;
        _localExecutionService = localExecutionService;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncOnceAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Federation fanout sync cycle failed.");
            }

            var interval = Math.Max(1, _options.CurrentValue.Sync.FanoutIntervalSeconds);
            await Task.Delay(TimeSpan.FromSeconds(interval), stoppingToken).ConfigureAwait(false);
        }
    }

    /// <summary>Runs one fanout sync cycle. Exposed for focused tests and operational probes.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task SyncOnceAsync(CancellationToken cancellationToken)
    {
        if (!_registry.IsEnabled ||
            _registry.EffectiveRole != FederationRole.LocalProxy ||
            string.IsNullOrWhiteSpace(_registry.HubBaseUrl) ||
            string.IsNullOrWhiteSpace(_registry.ProxyId))
        {
            return;
        }

        using var client = CreateHubClient();
        var syncUri = $"{_registry.HubBaseUrl}/mcpserver/federation/sync?proxyId={Uri.EscapeDataString(_registry.ProxyId)}&afterSequence={_lastSequence}";
        var items = await client.GetFromJsonAsync(syncUri, McpServicesJsonContext.Default.ListFederationSyncItem, cancellationToken)
            .ConfigureAwait(false) ?? [];

        foreach (var item in items.OrderBy(i => i.Sequence))
            await ApplyAndAckAsync(client, item, cancellationToken).ConfigureAwait(false);
    }

    private async Task ApplyAndAckAsync(HttpClient client, FederationSyncItem item, CancellationToken cancellationToken)
    {
        var signingRequired = _options.CurrentValue.Signing.Enabled;
        if (signingRequired && item.Envelope is null)
        {
            await AckAsync(client, item, "rejected", "Signed federation envelope is required for sync items.", cancellationToken)
                .ConfigureAwait(false);
            _lastSequence = Math.Max(_lastSequence, item.Sequence);
            return;
        }

        if (signingRequired && _envelopeSigner is not { IsConfigured: true })
        {
            await AckAsync(client, item, "rejected", "Federation envelope signer is not configured.", cancellationToken)
                .ConfigureAwait(false);
            _lastSequence = Math.Max(_lastSequence, item.Sequence);
            return;
        }

        var operation = item.Envelope?.Operation ?? item.ToRequest();
        if (item.Envelope is not null && _envelopeSigner is { IsConfigured: true })
        {
            var verification = _envelopeSigner.Verify(item.Envelope, _registry.ProxyId);
            if (!verification.IsValid)
            {
                await AckAsync(client, item, "rejected", verification.Error, cancellationToken).ConfigureAwait(false);
                _lastSequence = Math.Max(_lastSequence, item.Sequence);
                return;
            }
        }

        var result = item.Envelope is { } envelope &&
                     string.Equals(envelope.ApplyMode, "local_execution", StringComparison.OrdinalIgnoreCase)
            ? await ApplyLocalExecutionAsync(envelope, operation, cancellationToken).ConfigureAwait(false)
            : await _applyService.ApplyAsync(operation, cancellationToken).ConfigureAwait(false);
        var status = result.Conflict ? "conflict" : result.AlreadyApplied ? "already_applied" : "applied";
        await AckAsync(client, item, status, result.Message, cancellationToken).ConfigureAwait(false);
        _lastSequence = Math.Max(_lastSequence, item.Sequence);
    }

    private HttpClient CreateHubClient()
    {
        var client = _httpClientFactory.CreateClient(FederationProxyService.HttpClientName);
        if (!string.IsNullOrWhiteSpace(_registry.HubAccessToken))
            client.DefaultRequestHeaders.TryAddWithoutValidation("X-Api-Key", _registry.HubAccessToken);

        return client;
    }

    private async ValueTask<FederationApplyResult> ApplyLocalExecutionAsync(
        FederationExecutionEnvelope envelope,
        FederationOperationRequest operation,
        CancellationToken cancellationToken)
    {
        var options = _options.CurrentValue.LocalExecution;
        if (!options.Enabled)
            return Conflict("Federation local execution is disabled.");
        if (_localExecutionService is null)
            return Conflict("Federation local execution service is not configured.");
        if (!TryDecodeLocalExecutionRequest(operation, out var request, out var decodeError))
            return Conflict(decodeError ?? "Federation local execution payload is invalid.");

        var method = ResolveLocalExecutionMethod(envelope, operation, request);
        if (string.IsNullOrWhiteSpace(method))
            return Conflict("Federation local execution method is required.");
        if (!options.AllowedMethods.Contains(method, StringComparer.OrdinalIgnoreCase))
            return Conflict($"Federation local execution method '{method}' is not allowlisted.");

        request.Method = string.IsNullOrWhiteSpace(request.Method) ? method : request.Method.Trim();
        if (!string.Equals(request.Method, method, StringComparison.OrdinalIgnoreCase))
            return Conflict("Federation local execution method does not match the signed operation method.");

        var execution = await _localExecutionService.ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
        return new FederationApplyResult
        {
            Applied = execution.Success,
            Conflict = !execution.Success,
            Version = operation.OperationId,
            Message = execution.Message,
        };
    }

    private static string? ResolveLocalExecutionMethod(
        FederationExecutionEnvelope envelope,
        FederationOperationRequest operation,
        FederationLocalExecutionRequest request)
    {
        if (!string.IsNullOrWhiteSpace(operation.Method))
            return operation.Method.Trim();
        if (!string.IsNullOrWhiteSpace(request.Method))
            return request.Method.Trim();
        if (!string.IsNullOrWhiteSpace(operation.Domain) &&
            !string.Equals(operation.Domain, "local_execution", StringComparison.OrdinalIgnoreCase))
        {
            return operation.Domain.Trim();
        }

        return string.IsNullOrWhiteSpace(envelope.ApplyMode) ? null : envelope.ApplyMode.Trim();
    }

    private static bool TryDecodeLocalExecutionRequest(
        FederationOperationRequest operation,
        out FederationLocalExecutionRequest request,
        out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(operation.BodyBase64))
        {
            request = new FederationLocalExecutionRequest();
            return true;
        }

        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(operation.BodyBase64));
            request = JsonSerializer.Deserialize(json, McpServicesJsonContext.Default.FederationLocalExecutionRequest)
                ?? new FederationLocalExecutionRequest();
            return true;
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            request = new FederationLocalExecutionRequest();
            error = $"Federation local execution payload is invalid: {ex.Message}";
            return false;
        }
    }

    private static FederationApplyResult Conflict(string message)
        => new()
        {
            Applied = false,
            Conflict = true,
            Message = message,
        };

    private async Task AckAsync(
        HttpClient client,
        FederationSyncItem item,
        string status,
        string? error,
        CancellationToken cancellationToken)
    {
        using var response = await client.PostAsJsonAsync(
                $"{_registry.HubBaseUrl}/mcpserver/federation/sync/{item.Sequence}/ack",
                new FederationSyncAckRequest
                {
                    ProxyId = _registry.ProxyId,
                    Status = status,
                    HubVersion = item.HubVersion,
                    Error = error,
                },
                McpServicesJsonContext.Default.FederationSyncAckRequest,
                cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }
}
