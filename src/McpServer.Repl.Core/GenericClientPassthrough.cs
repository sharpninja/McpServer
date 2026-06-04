// FR-MCP-REPL-001: YAML Protocol STDIO REPL Host - Generic client passthrough operations
// FR-MCP-REPL-003: Command Namespace Parity - Workspace and context operation forwarding
// TR-MCP-REPL-005: Namespace Organization and Handler Parity - Client passthrough implementation
// TEST-MCP-REPL-010: Workspace management REPL commands match REST endpoints

using System;
using System.Collections.Generic;
using System.Linq;
// FR-MCP-REPL-001: YAML Protocol STDIO REPL Host - Generic client passthrough implementation
// FR-MCP-REPL-003: Command Namespace Parity - Client operation forwarding implementation
// FR-MCP-REPL-005: Orchestration State Visibility - State query implementation
// TR-MCP-REPL-004: Command Registry and Dispatcher - Generic passthrough handler
// TR-MCP-REPL-005: Namespace Organization and Handler Parity - Client passthrough delegation
// TR-MCP-REPL-007: State Query Commands - Client passthrough dynamic binding
// TEST-MCP-REPL-008: Context REPL operations match REST endpoints
// TEST-MCP-REPL-011: Generic client passthrough delegates to correct client methods

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Client;

namespace McpServer.Repl.Core;

/// <summary>
/// Production implementation of <see cref="IGenericClientPassthrough"/> that uses reflection to
/// dynamically invoke methods on <see cref="McpServerClient"/> sub-clients.
/// </summary>
/// <remarks>
/// This implementation resolves client properties by name (case-insensitive), resolves methods by name,
/// coerces YAML dictionary arguments to method parameter types using <see cref="System.Text.Json"/>,
/// invokes methods via reflection, and returns serialized results.
/// </remarks>
public sealed class GenericClientPassthrough : IGenericClientPassthrough
{
    private readonly McpServerClient _client;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new FlexibleBooleanJsonConverter() }
    };

    /// <summary>
    /// Initializes a new instance of <see cref="GenericClientPassthrough"/> with the specified client.
    /// </summary>
    /// <param name="client">The MCP server client containing all sub-clients.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="client"/> is null.</exception>
    public GenericClientPassthrough(McpServerClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    /// <inheritdoc />
    public async Task<object?> InvokeAsync(
        string clientName,
        string methodName,
        Dictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(clientName);
        ArgumentNullException.ThrowIfNull(methodName);
        ArgumentNullException.ThrowIfNull(arguments);

        // Step 1: Resolve client by name (case-insensitive)
        var clientProperty = ResolveClientProperty(clientName);
        var clientInstance = clientProperty.GetValue(_client);

        if (clientInstance is null)
        {
            throw new InvalidOperationException(
                $"Client property '{clientName}' resolved to null. Valid clients: {GetValidClientNames()}");
        }

        // Step 2: Resolve method by name
        var method = ResolveMethod(clientInstance.GetType(), methodName);

        // Step 3: Bind arguments to method parameters
        var parameters = method.GetParameters();
        var boundArgs = BindArguments(parameters, arguments, cancellationToken);

        // Step 4: Invoke method via reflection
        try
        {
            var result = method.Invoke(clientInstance, boundArgs);

            // Step 5: Await if the result is a Task
            if (result is Task task)
            {
                await task.ConfigureAwait(false);

                // Extract result from Task<T>
                var resultType = task.GetType();
                if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    var resultProperty = resultType.GetProperty("Result");
                    return resultProperty?.GetValue(task);
                }

                return null;
            }

            return result;
        }
        catch (TargetInvocationException ex)
        {
            throw new InvalidOperationException(
                $"Method invocation failed for {clientName}.{methodName}: {ex.InnerException?.Message ?? ex.Message}",
                ex.InnerException ?? ex);
        }
    }

    /// <summary>
    /// Resolves a client property from <see cref="McpServerClient"/> by name (case-insensitive).
    /// </summary>
    private PropertyInfo ResolveClientProperty(string clientName)
    {
        var clientType = typeof(McpServerClient);
        var properties = clientType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        var match = properties.FirstOrDefault(p =>
            string.Equals(p.Name, clientName, StringComparison.OrdinalIgnoreCase));

        if (match is null)
        {
            throw new InvalidOperationException(
                $"Unknown client: {clientName}. Valid clients: {GetValidClientNames()}");
        }

        return match;
    }

    /// <summary>
    /// Resolves a public async method on the client by name.
    /// </summary>
    private MethodInfo ResolveMethod(Type clientType, string methodName)
    {
        var methods = clientType.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        // Try case-sensitive first
        var match = methods.FirstOrDefault(m => m.Name == methodName);

        // Fall back to case-insensitive
        if (match is null)
        {
            match = methods.FirstOrDefault(m =>
                string.Equals(m.Name, methodName, StringComparison.OrdinalIgnoreCase));
        }

        if (match is null)
        {
            var validMethods = string.Join(", ", methods
                .Where(m => m.ReturnType.IsGenericType &&
                           m.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                .Select(m => m.Name)
                .Distinct()
                .OrderBy(n => n));

            throw new InvalidOperationException(
                $"Unknown method: {methodName} on client: {clientType.Name}. Valid methods: {validMethods}");
        }

        return match;
    }

    /// <summary>
    /// Binds arguments from the dictionary to method parameters.
    /// </summary>
    private object?[] BindArguments(
        ParameterInfo[] parameters,
        Dictionary<string, object?> arguments,
        CancellationToken cancellationToken)
    {
        var boundArgs = new object?[parameters.Length];
        var nullabilityContext = new NullabilityInfoContext();

        for (int i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];

            // CancellationToken is always passed from the method parameter
            if (parameter.ParameterType == typeof(CancellationToken))
            {
                boundArgs[i] = cancellationToken;
                continue;
            }

            // Try to find matching argument (case-insensitive)
            var argKey = arguments.Keys.FirstOrDefault(k =>
                string.Equals(k, parameter.Name, StringComparison.OrdinalIgnoreCase));

            if (argKey is null)
            {
                // Check if parameter is optional
                if (parameter.HasDefaultValue)
                {
                    boundArgs[i] = parameter.DefaultValue;
                    continue;
                }

                // Required parameter missing
                throw new ArgumentException(
                    $"Missing required parameter: {parameter.Name} (type: {parameter.ParameterType.Name})");
            }

            var argValue = arguments[argKey];

            // Get nullability info for the parameter
            var nullabilityInfo = nullabilityContext.Create(parameter);

            // Coerce argument to target type
            boundArgs[i] = CoerceArgument(argValue, parameter.ParameterType, parameter.Name, nullabilityInfo);
        }

        return boundArgs;
    }

    /// <summary>
    /// Coerces an argument value to the target parameter type.
    /// </summary>
    private object? CoerceArgument(object? value, Type targetType, string? parameterName, NullabilityInfo nullabilityInfo)
    {
        // Handle null values
        if (value is null)
        {
            // Check if the parameter accepts null
            bool acceptsNull = nullabilityInfo.WriteState == NullabilityState.Nullable ||
                              (targetType.IsValueType && Nullable.GetUnderlyingType(targetType) is not null);

            if (!acceptsNull)
            {
                throw new ArgumentException(
                    $"Null value provided for non-nullable parameter: {parameterName} (type: {targetType.Name})");
            }

            return null;
        }

        // If already correct type, return as-is
        if (targetType.IsInstanceOfType(value))
        {
            return value;
        }

        // Unwrap nullable types
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        // Handle enums
        if (underlyingType.IsEnum)
        {
            try
            {
                if (value is string stringValue)
                {
                    return Enum.Parse(underlyingType, stringValue, ignoreCase: true);
                }

                return Enum.ToObject(underlyingType, value);
            }
            catch (Exception ex)
            {
                var validValues = string.Join(", ", Enum.GetNames(underlyingType));
                throw new ArgumentException(
                    $"Invalid enum value for parameter '{parameterName}': {value}. Valid values: {validValues}",
                    ex);
            }
        }

        // Handle primitive types
        if (underlyingType.IsPrimitive || underlyingType == typeof(string) ||
            underlyingType == typeof(decimal) || underlyingType == typeof(DateTime) ||
            underlyingType == typeof(DateTimeOffset) || underlyingType == typeof(Guid))
        {
            try
            {
                return Convert.ChangeType(value, underlyingType);
            }
            catch (Exception ex)
            {
                throw new ArgumentException(
                    $"Type conversion error for parameter '{parameterName}': cannot convert '{value}' to {underlyingType.Name}",
                    ex);
            }
        }

        // Handle collections
        if (IsCollectionType(underlyingType))
        {
            try
            {
                return CoerceCollection(value, underlyingType, parameterName);
            }
            catch (Exception ex)
            {
                throw new ArgumentException(
                    $"Collection conversion error for parameter '{parameterName}': {ex.Message}",
                    ex);
            }
        }

        // Handle complex objects via JSON serialization
        try
        {
            var json = JsonSerializer.Serialize(NormalizeForJson(value), _jsonOptions);
            return JsonSerializer.Deserialize(json, underlyingType, _jsonOptions);
        }
        catch (Exception ex)
        {
            throw new ArgumentException(
                $"JSON deserialization error for parameter '{parameterName}': cannot deserialize to {underlyingType.Name}. {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Coerces a value to a collection type.
    /// </summary>
    private object? CoerceCollection(object? value, Type targetType, string? parameterName)
    {
        var json = JsonSerializer.Serialize(NormalizeForJson(value), _jsonOptions);
        return JsonSerializer.Deserialize(json, targetType, _jsonOptions);
    }

    /// <summary>
    /// Normalizes raw YAML structures into JSON-compatible dictionaries and lists before model binding.
    /// </summary>
    private static object? NormalizeForJson(object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value is JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Object => NormalizeJsonObject(element),
                JsonValueKind.Array => element.EnumerateArray().Select(v => NormalizeForJson(v)).ToList(),
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number when element.TryGetInt64(out var integer) => integer,
                JsonValueKind.Number when element.TryGetDouble(out var number) => number,
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => element.ToString(),
            };
        }

        if (value is System.Collections.IDictionary dictionary)
        {
            var normalized = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (System.Collections.DictionaryEntry entry in dictionary)
            {
                var key = Convert.ToString(entry.Key, System.Globalization.CultureInfo.InvariantCulture);
                if (!string.IsNullOrWhiteSpace(key))
                {
                    normalized[key] = NormalizeForJson(entry.Value);
                }
            }

            return normalized;
        }

        if (value is System.Collections.IEnumerable sequence and not string)
        {
            var normalized = new List<object?>();
            foreach (var item in sequence)
            {
                normalized.Add(NormalizeForJson(item));
            }

            return normalized;
        }

        return value;
    }

    private static Dictionary<string, object?> NormalizeJsonObject(JsonElement element)
    {
        var normalized = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in element.EnumerateObject())
        {
            normalized[property.Name] = NormalizeForJson(property.Value);
        }

        return normalized;
    }

    private sealed class FlexibleBooleanJsonConverter : JsonConverter<bool>
    {
        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.True => true,
                JsonTokenType.False => false,
                JsonTokenType.String when bool.TryParse(reader.GetString(), out var value) => value,
                _ => throw new JsonException($"The JSON value could not be converted to {typeToConvert}.")
            };
        }

        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
        {
            writer.WriteBooleanValue(value);
        }
    }

    /// <summary>
    /// Checks if a type is a collection type.
    /// </summary>
    private static bool IsCollectionType(Type type)
    {
        if (type.IsArray) return true;
        if (type.IsGenericType)
        {
            var genericDef = type.GetGenericTypeDefinition();
            return genericDef == typeof(List<>) ||
                   genericDef == typeof(IList<>) ||
                   genericDef == typeof(IReadOnlyList<>) ||
                   genericDef == typeof(IEnumerable<>) ||
                   genericDef == typeof(ICollection<>) ||
                   genericDef == typeof(IReadOnlyCollection<>);
        }
        return false;
    }

    /// <summary>
    /// Gets a comma-separated list of valid client names.
    /// </summary>
    private static string GetValidClientNames()
    {
        var clientType = typeof(McpServerClient);
        var properties = clientType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var names = properties
            .Where(p => p.PropertyType.Name.EndsWith("Client"))
            .Select(p => p.Name)
            .OrderBy(n => n);
        return string.Join(", ", names);
    }
}
