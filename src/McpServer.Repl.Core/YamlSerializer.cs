// FR-MCP-REPL-001: YAML Protocol STDIO REPL Host - Production YAML serializer
// TR-MCP-REPL-001: YAML Envelope Protocol - YamlDotNet-backed serializer
// TEST-MCP-REPL-001: Well-formed YAML envelopes parse to typed payloads

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace McpServer.Repl.Core;

/// <summary>
/// Production <see cref="IYamlSerializer"/> implementation backed by YamlDotNet.
/// Parses YAML envelopes into typed <see cref="YamlEnvelope"/> instances whose <c>Payload</c>
/// is one of <see cref="HelloPayload"/>, <see cref="RequestPayload"/>, <see cref="ResultPayload"/>,
/// <see cref="ErrorPayload"/>, or <see cref="EventPayload"/> based on the <c>type</c> discriminator.
/// Params for request envelopes are preserved as <see cref="IReadOnlyDictionary{TKey, TValue}"/>
/// so the dispatcher can pass them through to <see cref="IGenericClientPassthrough"/>.
/// </summary>
public sealed class YamlSerializer : IYamlSerializer
{
    private readonly ISerializer _serializer;
    private readonly IDeserializer _deserializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlSerializer"/> class with a
    /// camelCase-compatible YamlDotNet configuration.
    /// </summary>
    public YamlSerializer()
    {
        _serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .Build();

        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    /// <inheritdoc />
    public string Serialize(IYamlEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        var wire = new Dictionary<string, object?>
        {
            ["type"] = envelope.Type,
            ["payload"] = NormalizeForSerialization(envelope.Payload),
        };

        return _serializer.Serialize(wire);
    }

    /// <inheritdoc />
    public IYamlEnvelope Deserialize(string yaml)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(yaml);

        Dictionary<string, object?> root;
        try
        {
            root = _deserializer.Deserialize<Dictionary<string, object?>>(yaml)
                   ?? throw new InvalidOperationException("Empty YAML document");
        }
        catch (YamlDotNet.Core.YamlException ex)
        {
            throw new FormatException("Malformed YAML envelope", ex);
        }

        if (!root.TryGetValue("type", out var typeObj) || typeObj is null)
        {
            throw new InvalidOperationException("Envelope missing 'type' discriminator");
        }

        var type = typeObj.ToString() ?? throw new InvalidOperationException("Envelope 'type' is null");
        root.TryGetValue("payload", out var payloadObj);

        var payload = MaterializePayload(type, payloadObj);
        return new YamlEnvelope { Type = type, Payload = payload };
    }

    /// <inheritdoc />
    public bool TryDeserialize(string yaml, out IYamlEnvelope? envelope)
    {
        try
        {
            envelope = Deserialize(yaml);
            return true;
        }
        catch
        {
            envelope = null;
            return false;
        }
    }

    /// <inheritdoc />
    public string SerializeStream(IEnumerable<IYamlEnvelope> envelopes)
    {
        ArgumentNullException.ThrowIfNull(envelopes);

        var builder = new System.Text.StringBuilder();
        foreach (var envelope in envelopes)
        {
            builder.Append("---\n");
            builder.Append(Serialize(envelope));
        }

        return builder.ToString();
    }

    /// <inheritdoc />
    public IReadOnlyList<IYamlEnvelope> DeserializeStream(string yamlStream)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(yamlStream);

        var documents = yamlStream.Split(
            new[] { "\n---\n", "\r\n---\r\n", "\n---\r\n", "\r\n---\n" },
            StringSplitOptions.RemoveEmptyEntries);

        var envelopes = new List<IYamlEnvelope>();
        foreach (var raw in documents)
        {
            var trimmed = raw.Trim();
            if (trimmed.StartsWith("---"))
            {
                trimmed = trimmed[3..].TrimStart('\r', '\n');
            }

            if (!string.IsNullOrWhiteSpace(trimmed))
            {
                envelopes.Add(Deserialize(trimmed));
            }
        }

        return envelopes;
    }

    private static object? MaterializePayload(string type, object? payloadObj)
    {
        if (payloadObj is null)
        {
            return null;
        }

        var dict = payloadObj as IDictionary<object, object?>
                   ?? (payloadObj is IDictionary<string, object?> typed
                       ? typed.ToDictionary(kv => (object)kv.Key, kv => kv.Value)
                       : null);

        if (dict is null)
        {
            return payloadObj;
        }

        var normalized = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in dict)
        {
            var key = kv.Key?.ToString();
            if (key is not null)
            {
                normalized[key] = kv.Value;
            }
        }

        return type switch
        {
            "hello" => new HelloPayload
            {
                ProtocolVersion = normalized.TryGetValue("protocolVersion", out var pv) && pv is not null
                    ? pv.ToString() ?? "1.0"
                    : "1.0",
                Capabilities = ToStringList(normalized.GetValueOrDefault("capabilities")),
                Metadata = ToStringDict(normalized.GetValueOrDefault("metadata")),
            },
            "request" => new RequestPayload
            {
                RequestId = normalized.GetValueOrDefault("requestId")?.ToString() ?? "",
                Method = normalized.GetValueOrDefault("method")?.ToString() ?? "",
                Params = ToParamsDict(normalized.GetValueOrDefault("params")),
            },
            "result" => new ResultPayload
            {
                RequestId = normalized.GetValueOrDefault("requestId")?.ToString() ?? "",
                Result = normalized.GetValueOrDefault("result"),
            },
            "error" => new ErrorPayload
            {
                RequestId = normalized.GetValueOrDefault("requestId")?.ToString() ?? "",
                Code = normalized.GetValueOrDefault("code")?.ToString() ?? "",
                Message = normalized.GetValueOrDefault("message")?.ToString() ?? "",
                Retryable = ParseRetryable(normalized.GetValueOrDefault("retryable")),
                Details = ToParamsDict(normalized.GetValueOrDefault("details")),
            },
            "event" => new EventPayload
            {
                Event = normalized.GetValueOrDefault("event")?.ToString() ?? "",
                Data = normalized.GetValueOrDefault("data"),
                Timestamp = normalized.GetValueOrDefault("timestamp") is { } ts &&
                             DateTimeOffset.TryParse(ts.ToString(), out var parsed)
                    ? parsed
                    : null,
            },
            _ => normalized,
        };
    }

    private static IReadOnlyList<string>? ToStringList(object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value is IEnumerable<object?> seq)
        {
            return seq.Select(item => item?.ToString() ?? "").ToList();
        }

        if (value is System.Collections.IEnumerable untyped and not string)
        {
            var list = new List<string>();
            foreach (var item in untyped)
            {
                list.Add(item?.ToString() ?? "");
            }
            return list;
        }

        return new[] { value.ToString() ?? "" };
    }

    private static IReadOnlyDictionary<string, string>? ToStringDict(object? value)
    {
        if (value is null)
        {
            return null;
        }

        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (value is IDictionary<object, object?> objKeyed)
        {
            foreach (var kv in objKeyed)
            {
                var key = kv.Key?.ToString();
                if (key is not null)
                {
                    dict[key] = kv.Value?.ToString() ?? "";
                }
            }
            return dict;
        }

        if (value is IDictionary<string, object?> strKeyed)
        {
            foreach (var kv in strKeyed)
            {
                dict[kv.Key] = kv.Value?.ToString() ?? "";
            }
            return dict;
        }

        return null;
    }

    private static bool ParseRetryable(object? value)
    {
        if (value is bool flag)
            return flag;
        if (value is string text && bool.TryParse(text, out var parsed))
            return parsed;
        return false;
    }

    private static IReadOnlyDictionary<string, object?>? ToParamsDict(object? value)
    {
        if (value is null)
        {
            return null;
        }

        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (value is IDictionary<object, object?> objKeyed)
        {
            foreach (var kv in objKeyed)
            {
                var key = kv.Key?.ToString();
                if (key is not null)
                {
                    dict[key] = kv.Value;
                }
            }
            return dict;
        }

        if (value is IDictionary<string, object?> strKeyed)
        {
            foreach (var kv in strKeyed)
            {
                dict[kv.Key] = kv.Value;
            }
            return dict;
        }

        return null;
    }

    private static object? NormalizeForSerialization(object? payload)
    {
        if (payload is null)
        {
            return null;
        }

        return payload switch
        {
            IHelloPayload h => new Dictionary<string, object?>
            {
                ["protocolVersion"] = h.ProtocolVersion,
                ["capabilities"] = h.Capabilities,
                ["metadata"] = h.Metadata,
            },
            IRequestPayload r => new Dictionary<string, object?>
            {
                ["requestId"] = r.RequestId,
                ["method"] = r.Method,
                ["params"] = r.Params,
            },
            IResultPayload res => NormalizeResultPayload(res),
            IErrorPayload e => new Dictionary<string, object?>
            {
                ["requestId"] = e.RequestId,
                ["code"] = e.Code,
                ["message"] = e.Message,
                ["retryable"] = e.Retryable,
                ["details"] = e.Details,
            },
            IEventPayload ev => new Dictionary<string, object?>
            {
                ["event"] = ev.Event,
                ["data"] = ev.Data,
                ["timestamp"] = ev.Timestamp,
            },
            _ => payload,
        };
    }

    private static Dictionary<string, object?> NormalizeResultPayload(IResultPayload res)
    {
        var wire = new Dictionary<string, object?>
        {
            ["requestId"] = res.RequestId,
            ["result"] = res.Result,
        };

        // FR-MCP-REPL-006: deprecation marker for workflow.* responses; the key is
        // only present on the wire when explicitly set.
        if (res is ResultPayload { Deprecated: not null } payload)
        {
            wire["deprecated"] = payload.Deprecated;
        }

        return wire;
    }
}
