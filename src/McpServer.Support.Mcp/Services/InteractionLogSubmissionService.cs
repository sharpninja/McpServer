using System.Net.Http.Json;
using System.Text.Json;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Options;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-PLANNED-013: Background service that dequeues interaction log entries and submits them asynchronously to the configured logging service URL.
/// </summary>
public sealed class InteractionLogSubmissionService : BackgroundService
{
    private readonly IInteractionLogSubmissionChannel _channel;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<InteractionLogSubmissionService> _logger;
    private readonly McpInteractionLoggingOptions _options;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    /// <summary>TR-PLANNED-013: Constructor.</summary>
    /// <param name="channel">Channel for dequeuing log entries.</param>
    /// <param name="httpClientFactory">Factory for creating HTTP clients.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="options">Interaction logging options.</param>
    public InteractionLogSubmissionService(
        IInteractionLogSubmissionChannel channel,
        IHttpClientFactory httpClientFactory,
        ILogger<InteractionLogSubmissionService> logger,
        IOptions<McpInteractionLoggingOptions> options)
    {
        _channel = channel;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _options = options?.Value ?? new McpInteractionLoggingOptions();
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_options.LoggingServiceUrl))
        {
            _logger.LogDebug("Interaction log submission disabled: LoggingServiceUrl not configured");
            return;
        }

        var client = _httpClientFactory.CreateClient("InteractionLogSubmission");
        _logger.LogInformation("Interaction log submission started; posting to {Url}", _options.LoggingServiceUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var (success, entry) = await _channel.TryDequeueAsync(stoppingToken).ConfigureAwait(false);
                if (!success || entry == null)
                    continue;

                await PostEntryAsync(client, entry, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning("{ExceptionDetail}", ex.ToString());
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error in interaction log submission loop");
            }
        }

        _logger.LogInformation("Interaction log submission stopped");
    }

    private async Task PostEntryAsync(HttpClient client, InteractionLogEntry entry, CancellationToken cancellationToken)
    {
        try
        {
            var response = await client.PostAsJsonAsync(_options.LoggingServiceUrl!, entry, JsonOptions, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                _logger.LogWarning("Log submission returned {StatusCode} for {Path}", response.StatusCode, entry.Path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to submit interaction log for {Path}", entry.Path);
        }
    }
}
