using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Microsoft.Extensions.Options;
using McpServer.Support.Mcp.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-049, TR-MCP-TPL-001: Reads the marker prompt template from
/// <c>templates/default-marker-prompt.hbs.yaml</c> and caches the result.
/// Returns <see langword="null"/> when the file is missing, allowing the caller
/// to fall back to <see cref="MarkerFileService.DefaultPromptTemplate"/>.
/// </summary>
public sealed class FileMarkerPromptProvider : IMarkerPromptProvider
{
    private static readonly IDeserializer s_deserializer = new DeserializerBuilder()
        .WithNamingConvention(HyphenatedNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    private readonly string _filePath;
    private readonly ILogger<FileMarkerPromptProvider> _logger;
    private string? _cached;
    private bool _loaded;

    /// <summary>Initializes a new instance of the <see cref="FileMarkerPromptProvider"/> class.</summary>
    public FileMarkerPromptProvider(ILogger<FileMarkerPromptProvider> logger)
        : this(Microsoft.Extensions.Options.Options.Create(new TemplateStorageOptions()), logger)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="FileMarkerPromptProvider"/> class.</summary>
    public FileMarkerPromptProvider(IOptions<TemplateStorageOptions> options, ILogger<FileMarkerPromptProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _logger = logger;

        var templateFilePath = options.Value.FilePath;
        if (!Path.IsPathRooted(templateFilePath))
            templateFilePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, templateFilePath));

        var templateDirectory = Path.GetDirectoryName(templateFilePath) ?? Path.Combine(AppContext.BaseDirectory, "templates");
        _filePath = Path.Combine(templateDirectory, "default-marker-prompt.hbs.yaml");
    }

    /// <inheritdoc />
    public async Task<string?> GetGlobalPromptTemplateAsync(CancellationToken cancellationToken = default)
    {
        if (_loaded)
            return _cached;

        if (!File.Exists(_filePath))
        {
            _logger.LogDebug("Marker prompt template file not found at {Path}, using built-in default", _filePath);
            _loaded = true;
            return null;
        }

        try
        {
            var yaml = await File.ReadAllTextAsync(_filePath, cancellationToken).ConfigureAwait(false);
            var doc = s_deserializer.Deserialize<MarkerTemplateFile>(yaml);
            _cached = doc?.Template;
            _loaded = true;

            if (_cached is not null)
                _logger.LogInformation("Loaded marker prompt template from {Path} ({Length} chars)", _filePath, _cached.Length);
            else
                _logger.LogWarning("Marker prompt template file at {Path} has no 'template' key", _filePath);

            return _cached;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to read marker prompt template from {Path}: {Error}", _filePath, ex.ToString());
            _loaded = true;
            return null;
        }
    }

    /// <summary>YAML deserialization target for the marker template file.</summary>
    internal sealed class MarkerTemplateFile
    {
        /// <summary>The template content.</summary>
        public string? Template { get; set; }
    }
}
