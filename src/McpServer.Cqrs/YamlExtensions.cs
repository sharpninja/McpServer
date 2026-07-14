using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace McpServer.Cqrs;

/// <summary>
/// TR-MCP-CQRS-007: Extension method for serialization of CQRS request and result objects.
/// Uses YamlDotNet directly for diagnostic rendering and falls back to <see cref="object.ToString"/>
/// when an object graph cannot be represented safely.
/// </summary>
public static class YamlExtensions
{
    private static readonly ISerializer s_yamlSerializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .DisableAliases()
        .Build();

    /// <summary>Serializes the object to YAML for diagnostic output.</summary>
    /// <param name="obj">The object to serialize.</param>
    /// <returns>A YAML representation, or the object's <see cref="object.ToString"/> fallback on error.</returns>
    public static string ToYaml(this object? obj)
    {
        if (obj is null) return string.Empty;

        try
        {
            var yaml = s_yamlSerializer.Serialize(obj);
            return string.Join('\n', yaml.Split('\n')
                .Where(static l => !string.IsNullOrWhiteSpace(l)));
        }
        catch
        {
            return obj.ToString() ?? string.Empty;
        }
    }
}
