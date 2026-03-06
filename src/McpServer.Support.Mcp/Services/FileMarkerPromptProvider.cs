namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-049, TR-MCP-TPL-001: Reads the marker prompt template from the
/// combined <c>prompt-templates.yaml</c> via <see cref="IPromptTemplateService"/>
/// using the <c>default-marker-prompt</c> template ID. Returns <see langword="null"/>
/// when the template is not found, allowing the caller to fall back to
/// <see cref="MarkerFileService.DefaultPromptTemplate"/>.
/// </summary>
public sealed class FileMarkerPromptProvider : IMarkerPromptProvider
{
    /// <summary>Well-known template ID for the marker prompt.</summary>
    internal const string TemplateId = "default-marker-prompt";

    private readonly IPromptTemplateService _templateService;
    private readonly ILogger<FileMarkerPromptProvider> _logger;
    private string? _cached;
    private bool _loaded;

    /// <summary>Initializes a new instance of the <see cref="FileMarkerPromptProvider"/> class.</summary>
    public FileMarkerPromptProvider(IPromptTemplateService templateService, ILogger<FileMarkerPromptProvider> logger)
    {
        _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> GetGlobalPromptTemplateAsync(CancellationToken cancellationToken = default)
    {
        if (_loaded)
            return _cached!;

        try
        {
            var template = await _templateService.GetByIdAsync(TemplateId, cancellationToken).ConfigureAwait(false);
            
            if (template is null)
            {
                var msg = $"CRITICAL: Marker prompt template '{TemplateId}' not found in prompt-templates.yaml. Server cannot start without it.";
                _logger.LogCritical(msg);
                throw new InvalidOperationException(msg);
            }

            _cached = template.Content;
            _loaded = true;

            _logger.LogInformation("Loaded marker prompt template '{Id}' ({Length} chars)", TemplateId, _cached.Length);
            return _cached;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogCritical(ex, "Failed to load marker prompt template '{Id}'", TemplateId);
            throw;
        }
    }
}
