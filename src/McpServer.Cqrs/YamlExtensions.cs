using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace McpServer.Cqrs;

/// <summary>
/// TR-MCP-CQRS-007: Extension method for serialization of CQRS request and result objects.
/// Uses Newtonsoft.Json with <see cref="ReferenceLoopHandling.Ignore"/> for safe circular
/// reference handling, then converts the safe object graph to YAML.
/// </summary>
public static class YamlExtensions
{
    private static readonly JsonSerializerSettings s_jsonSettings = new()
    {
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        MaxDepth = 10,
        NullValueHandling = NullValueHandling.Ignore,
    };

    private static readonly ISerializer s_yamlSerializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .DisableAliases()
        .Build();

    /// <summary>Serializes the object to YAML via a Newtonsoft.Json safe intermediate.</summary>
    /// <param name="obj">The object to serialize.</param>
    /// <returns>A YAML representation, or the object's <see cref="object.ToString"/> fallback on error.</returns>
    public static string ToYaml(this object? obj)
    {
        if (obj is null) return string.Empty;

        try
        {
            var json = JsonConvert.SerializeObject(obj, s_jsonSettings);
            var plain = ToPlainObject(JToken.Parse(json));
            var yaml = s_yamlSerializer.Serialize(plain!);
            return string.Join('\n', yaml.Split('\n')
                .Where(static l => !string.IsNullOrWhiteSpace(l)));
        }
        catch
        {
            return obj.ToString() ?? string.Empty;
        }
    }

    /// <summary>Converts a <see cref="JToken"/> tree to plain .NET types that YamlDotNet can serialize.</summary>
    private static object? ToPlainObject(JToken token) => token.Type switch
    {
        JTokenType.Object => ((JObject)token).Properties()
            .ToDictionary(p => p.Name, p => ToPlainObject(p.Value)),
        JTokenType.Array => ((JArray)token).Select(ToPlainObject).ToList(),
        JTokenType.Integer => token.Value<long>(),
        JTokenType.Float => token.Value<double>(),
        JTokenType.Boolean => token.Value<bool>(),
        JTokenType.String => token.Value<string>(),
        JTokenType.Null => null,
        _ => token.ToString(),
    };
}
