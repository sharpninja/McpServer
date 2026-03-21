using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Options;

/// <summary>
/// Applies instance-aware prompt template storage path resolution for both HTTP and STDIO hosts.
/// </summary>
internal sealed class TemplateStorageOptionsPostConfigure : IPostConfigureOptions<TemplateStorageOptions>
{
    private readonly IConfiguration _configuration;
    private readonly string? _instanceName;

    /// <summary>
    /// Initializes a new instance of the <see cref="TemplateStorageOptionsPostConfigure"/> class.
    /// </summary>
    /// <param name="configuration">Application configuration used for instance-aware overrides.</param>
    /// <param name="instanceName">Optional MCP instance name.</param>
    public TemplateStorageOptionsPostConfigure(IConfiguration configuration, string? instanceName)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _instanceName = instanceName;
    }

    /// <inheritdoc />
    public void PostConfigure(string? name, TemplateStorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.FilePath = McpInstanceResolver.GetEffectiveMcpValue(_configuration, _instanceName, "TemplateStorage:FilePath") ?? options.FilePath;
        options.FilePath = McpInstanceResolver.ResolveDataPath(_configuration, _instanceName, options.FilePath);
    }
}
