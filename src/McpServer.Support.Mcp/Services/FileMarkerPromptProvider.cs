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
    public async Task<string?> GetGlobalPromptTemplateAsync(CancellationToken cancellationToken = default)
    {
        if (_loaded)
            return _cached;

        try
        {
            var template = await _templateService.GetByIdAsync(TemplateId, cancellationToken).ConfigureAwait(false);
            _cached = template?.Content;
            _loaded = true;

            if (_cached is not null)
                _logger.LogInformation("Loaded marker prompt template '{Id}' ({Length} chars)", TemplateId, _cached.Length);
            else
                _logger.LogDebug("Marker prompt template '{Id}' not found, using built-in default", TemplateId);

            return _cached;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load marker prompt template '{Id}'", TemplateId);
            _loaded = true;
            return null;
        }
    }
}
