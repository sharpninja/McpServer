using System.Net.Http.Json;
using System.Text.Json;
using McpServer.Support.Mcp.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-103 / TR-MCP-FED-001: Background worker that keeps a LocalProxy
/// enrolled with its hub and refreshes heartbeat workspace inventory.
/// </summary>
public sealed class FederationLocalProxyEnrollmentService : BackgroundService
{
    private readonly FederationRegistry _registry;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<FederationOptions> _options;
    private readonly IConfiguration _configuration;
    private readonly ServerRuntimeInfo _runtimeInfo;
    private readonly ILogger<FederationLocalProxyEnrollmentService> _logger;
    private bool _enrolled;

    /// <summary>Initializes a new instance of the <see cref="FederationLocalProxyEnrollmentService"/> class.</summary>
    /// <param name="registry">Federation registry seeded from configuration.</param>
    /// <param name="httpClientFactory">HTTP client factory.</param>
    /// <param name="options">Federation options monitor.</param>
    /// <param name="configuration">Application configuration used to read local workspaces.</param>
    /// <param name="runtimeInfo">Current server runtime information.</param>
    /// <param name="logger">Logger.</param>
    public FederationLocalProxyEnrollmentService(
        FederationRegistry registry,
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<FederationOptions> options,
        IConfiguration configuration,
        ServerRuntimeInfo runtimeInfo,
        ILogger<FederationLocalProxyEnrollmentService> logger)
    {
        _registry = registry;
        _httpClientFactory = httpClientFactory;
        _options = options;
        _configuration = configuration;
        _runtimeInfo = runtimeInfo;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await EnrollOrHeartbeatOnceAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Federation LocalProxy enrollment/heartbeat cycle failed.");
                _enrolled = false;
            }

            var interval = Math.Max(1, _options.CurrentValue.Sync.HeartbeatSeconds);
            await Task.Delay(TimeSpan.FromSeconds(interval), stoppingToken).ConfigureAwait(false);
        }
    }

    /// <summary>Runs one enrollment or heartbeat cycle for focused tests and operator probes.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task EnrollOrHeartbeatOnceAsync(CancellationToken cancellationToken)
    {
        if (!CanContactHub())
            return;

        try
        {
            if (!_enrolled)
            {
                await EnrollAsync(cancellationToken).ConfigureAwait(false);
                _enrolled = true;
                return;
            }

            await HeartbeatAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Federation LocalProxy enrollment or heartbeat failed; the next cycle will retry enrollment.");
            _enrolled = false;
        }
    }

    private bool CanContactHub()
        => _registry.IsEnabled &&
           _registry.EffectiveRole == FederationRole.LocalProxy &&
           !string.IsNullOrWhiteSpace(_registry.HubBaseUrl) &&
           !string.IsNullOrWhiteSpace(_registry.ProxyId);

    private async Task EnrollAsync(CancellationToken cancellationToken)
    {
        var request = new FederationEnrollmentRequest
        {
            ProxyId = _registry.ProxyId,
            DisplayName = Environment.MachineName,
            BaseUrl = BuildCallbackBaseUrl(),
            EnrollmentToken = _options.CurrentValue.EnrollmentToken,
            MetadataJson = BuildMetadataJson("enroll"),
            Workspaces = BuildWorkspaceInventory(),
        };

        var client = CreateHubClient();
        using var requestMessage = CreateHubPostRequest(
            $"{_registry.HubBaseUrl}/mcpserver/federation/proxies/enroll",
            request);
        using var response = await client.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var result = await response.Content
            .ReadFromJsonAsync<FederationEnrollmentResponse>(cancellationToken)
            .ConfigureAwait(false);
        if (result is null || !result.Accepted)
            throw new InvalidOperationException("Federation hub did not accept LocalProxy enrollment.");
    }

    private async Task HeartbeatAsync(CancellationToken cancellationToken)
    {
        var request = new FederationHeartbeatRequest
        {
            Status = "online",
            MetadataJson = BuildMetadataJson("heartbeat"),
            Workspaces = BuildWorkspaceInventory(),
        };

        var client = CreateHubClient();
        using var requestMessage = CreateHubPostRequest(
            $"{_registry.HubBaseUrl}/mcpserver/federation/proxies/{Uri.EscapeDataString(_registry.ProxyId!)}/heartbeat",
            request);
        using var response = await client.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    private HttpClient CreateHubClient()
        => _httpClientFactory.CreateClient(FederationProxyService.HttpClientName);

    private HttpRequestMessage CreateHubPostRequest<TRequest>(string requestUri, TRequest request)
    {
        var message = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = JsonContent.Create(request),
        };

        if (!string.IsNullOrWhiteSpace(_registry.HubAccessToken))
            message.Headers.TryAddWithoutValidation("X-Api-Key", _registry.HubAccessToken);

        return message;
    }

    private string BuildCallbackBaseUrl()
        => $"http://{Environment.MachineName}:{_runtimeInfo.ListenPort}";

    private string BuildMetadataJson(string cycle)
        => JsonSerializer.Serialize(new
        {
            cycle,
            machineName = Environment.MachineName,
            processId = Environment.ProcessId,
            serverStartedAtUtc = _runtimeInfo.StartedAtUtc,
        }, new JsonSerializerOptions(JsonSerializerDefaults.Web));

    private IReadOnlyList<FederationWorkspaceRegistrationRequest> BuildWorkspaceInventory()
    {
        var workspaces = _configuration.GetSection("Mcp:Workspaces").Get<List<WorkspaceConfigEntry>>() ?? [];
        return workspaces
            .Where(w => w.IsEnabled && !string.IsNullOrWhiteSpace(w.WorkspacePath))
            .Select(w => new FederationWorkspaceRegistrationRequest
            {
                WorkspaceName = string.IsNullOrWhiteSpace(w.Name) ? Path.GetFileName(w.WorkspacePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)) : w.Name,
                WorkspacePath = w.WorkspacePath,
                IsEnabled = w.IsEnabled,
                MetadataJson = JsonSerializer.Serialize(new
                {
                    isPrimary = w.IsPrimary,
                    dataDirectory = w.DataDirectory,
                    tunnelProvider = w.TunnelProvider,
                }, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            })
            .ToList();
    }
}
