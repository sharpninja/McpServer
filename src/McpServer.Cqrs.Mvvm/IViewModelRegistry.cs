using System.Reflection;
using System.Text.Json;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace McpServer.Cqrs.Mvvm;

/// <summary>
/// TR-MCP-DIR-003: Registry for ViewModels discoverable by the Director <c>exec</c> command.
/// Maps ViewModel names and CLI aliases to types, resolves instances from DI,
/// and provides property population and primary command discovery.
/// </summary>
public interface IViewModelRegistry
{
    /// <summary>All registered ViewModel types keyed by name (class name and CLI alias).</summary>
    IReadOnlyDictionary<string, Type> ViewModels { get; }

    /// <summary>Resolves a ViewModel instance by name or alias from the DI container.</summary>
    /// <param name="name">The ViewModel class name or CLI alias.</param>
    /// <returns>The resolved ViewModel instance.</returns>
    object Resolve(string name);

    /// <summary>Gets the primary <see cref="IAsyncRelayCommand"/> from a ViewModel instance.</summary>
    /// <param name="viewModel">The ViewModel instance.</param>
    /// <returns>The primary async relay command.</returns>
    IAsyncRelayCommand GetPrimaryCommand(object viewModel);

    /// <summary>Sets ViewModel properties from a JSON element.</summary>
    /// <param name="viewModel">The ViewModel instance.</param>
    /// <param name="input">The JSON input containing property values.</param>
    void SetProperties(object viewModel, JsonElement input);

    /// <summary>Gets the result value from a ViewModel after command execution.</summary>
    /// <param name="viewModel">The ViewModel instance.</param>
    /// <returns>The result object, or <c>null</c>.</returns>
    object? GetResult(object viewModel);
}

/// <summary>
/// TR-MCP-DIR-003: Default implementation of <see cref="IViewModelRegistry"/>.
/// Scans assemblies for ViewModels decorated with <see cref="ViewModelCommandAttribute"/>.
/// </summary>
public sealed class ViewModelRegistry : IViewModelRegistry
{
    private readonly IServiceProvider _services;
    private readonly Dictionary<string, Type> _viewModels = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Initializes a new <see cref="ViewModelRegistry"/> by scanning the specified assemblies.</summary>
    /// <param name="services">The DI service provider.</param>
    /// <param name="assemblies">Assemblies to scan for ViewModels with <see cref="ViewModelCommandAttribute"/>.</param>
    public ViewModelRegistry(IServiceProvider services, IEnumerable<Assembly> assemblies)
    {
        _services = services;

        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                var attr = type.GetCustomAttribute<ViewModelCommandAttribute>();
                if (attr is null) continue;

                // Register by class name and by alias
                _viewModels[type.Name] = type;
                _viewModels[attr.Alias] = type;
            }
        }
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, Type> ViewModels => _viewModels;

    /// <inheritdoc />
    public object Resolve(string name)
    {
        if (!_viewModels.TryGetValue(name, out var type))
            throw new InvalidOperationException(
                $"ViewModel '{name}' not found in registry. " +
                "Use 'director list-viewmodels' to discover aliases. " +
                $"Available: {string.Join(", ", _viewModels.Keys)}");

        return ActivatorUtilities.CreateInstance(_services, type);
    }

    /// <inheritdoc />
    public IAsyncRelayCommand GetPrimaryCommand(object viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        // Look for a property named "PrimaryCommand" or the first IAsyncRelayCommand property
        var vmType = viewModel.GetType();
        var primaryProp = vmType.GetProperty("PrimaryCommand", BindingFlags.Public | BindingFlags.Instance);
        if (primaryProp?.GetValue(viewModel) is IAsyncRelayCommand primary)
            return primary;

        // Fallback: find the first IAsyncRelayCommand property
        foreach (var prop in vmType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.GetValue(viewModel) is IAsyncRelayCommand cmd)
                return cmd;
        }

        // Fallback for convention-based ViewModels that expose async methods
        // (for example LoadAsync/CheckAsync) but no command property.
        var fallbackMethod = FindFallbackMethod(vmType);
        if (fallbackMethod is not null)
            return new AsyncRelayCommand(ct => InvokeFallbackAsync(viewModel, fallbackMethod, ct));

        throw new InvalidOperationException($"ViewModel '{vmType.Name}' does not expose an IAsyncRelayCommand property.");
    }

    /// <inheritdoc />
    public void SetProperties(object viewModel, JsonElement input)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        var vmType = viewModel.GetType();

        foreach (var jsonProp in input.EnumerateObject())
        {
            var prop = vmType.GetProperty(jsonProp.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop is null || !prop.CanWrite) continue;

            var value = JsonSerializer.Deserialize(jsonProp.Value.GetRawText(), prop.PropertyType);
            prop.SetValue(viewModel, value);
        }
    }

    /// <inheritdoc />
    public object? GetResult(object viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        var vmType = viewModel.GetType();

        // Look for a "Result" or "LastResult" property
        var resultProp = vmType.GetProperty("Result", BindingFlags.Public | BindingFlags.Instance)
            ?? vmType.GetProperty("LastResult", BindingFlags.Public | BindingFlags.Instance);

        return resultProp?.GetValue(viewModel);
    }

    private static MethodInfo? FindFallbackMethod(Type viewModelType)
    {
        var candidateNames = new[]
        {
            "LoadAsync",
            "CheckAsync",
            "RefreshAsync",
            "StartAsync",
        };

        foreach (var name in candidateNames)
        {
            var method = viewModelType
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m =>
                    string.Equals(m.Name, name, StringComparison.Ordinal) &&
                    typeof(Task).IsAssignableFrom(m.ReturnType));
            if (method is not null)
                return method;
        }

        return null;
    }

    private static async Task InvokeFallbackAsync(object viewModel, MethodInfo method, CancellationToken ct)
    {
        // Event stream subscriptions can be long-running; bound them for exec usage.
        using var timeoutCts = string.Equals(method.Name, "StartAsync", StringComparison.Ordinal)
            ? CancellationTokenSource.CreateLinkedTokenSource(ct)
            : null;

        if (timeoutCts is not null)
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(2));

        var effectiveCt = timeoutCts?.Token ?? ct;
        var args = BuildInvocationArguments(viewModel, method, effectiveCt);
        var invocationResult = method.Invoke(viewModel, args);

        if (invocationResult is not Task task)
        {
            throw new InvalidOperationException(
                $"Fallback method '{viewModel.GetType().Name}.{method.Name}' must return Task.");
        }

        await task.ConfigureAwait(true);
    }

    private static object?[] BuildInvocationArguments(object viewModel, MethodInfo method, CancellationToken ct)
    {
        var vmType = viewModel.GetType();
        var properties = vmType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(static p => p.CanRead && p.GetIndexParameters().Length == 0)
            .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

        return method.GetParameters()
            .Select(p =>
            {
                if (p.ParameterType == typeof(CancellationToken))
                    return (object?)ct;

                if (properties.TryGetValue(p.Name ?? string.Empty, out var prop)
                    && p.ParameterType.IsAssignableFrom(prop.PropertyType))
                {
                    var value = prop.GetValue(viewModel);
                    if (value is not null)
                        return value;
                }

                if (p.HasDefaultValue)
                    return p.DefaultValue;

                return CreateFallbackValue(p.ParameterType, depth: 0);
            })
            .ToArray();
    }

    private static object? CreateFallbackValue(Type type, int depth)
    {
        if (depth > 3)
            return type.IsValueType ? Activator.CreateInstance(type) : null;

        if (type == typeof(string))
            return "sample";
        if (type == typeof(bool))
            return true;
        if (type == typeof(int))
            return 1;
        if (type == typeof(long))
            return 1L;
        if (type == typeof(double))
            return 1d;
        if (type == typeof(decimal))
            return 1m;
        if (type == typeof(Guid))
            return Guid.Empty;
        if (type == typeof(DateTime))
            return DateTime.UtcNow;
        if (type == typeof(DateTimeOffset))
            return DateTimeOffset.UtcNow;

        var nullableUnderlying = Nullable.GetUnderlyingType(type);
        if (nullableUnderlying is not null)
            return CreateFallbackValue(nullableUnderlying, depth + 1);

        if (type.IsEnum)
        {
            var values = Enum.GetValues(type);
            return values.Length > 0 ? values.GetValue(0) : Activator.CreateInstance(type);
        }

        if (type.IsArray)
        {
            var elementType = type.GetElementType()!;
            var array = Array.CreateInstance(elementType, 1);
            array.SetValue(CreateFallbackValue(elementType, depth + 1), 0);
            return array;
        }

        if (type.IsGenericType)
        {
            var genericDefinition = type.GetGenericTypeDefinition();
            var genericArguments = type.GetGenericArguments();

            if (genericDefinition == typeof(List<>) ||
                genericDefinition == typeof(IReadOnlyList<>) ||
                genericDefinition == typeof(IEnumerable<>))
            {
                var listType = typeof(List<>).MakeGenericType(genericArguments[0]);
                var list = (System.Collections.IList)Activator.CreateInstance(listType)!;
                list.Add(CreateFallbackValue(genericArguments[0], depth + 1));
                return list;
            }
        }

        if (type.IsValueType)
            return Activator.CreateInstance(type);

        var parameterless = type.GetConstructor(Type.EmptyTypes);
        if (parameterless is not null)
            return parameterless.Invoke([]);

        var constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public)
            .OrderByDescending(c => c.GetParameters().Length)
            .ToArray();
        if (constructors.Length > 0)
        {
            var constructor = constructors[0];
            var args = constructor.GetParameters()
                .Select(p => CreateFallbackValue(p.ParameterType, depth + 1))
                .ToArray();
            return constructor.Invoke(args);
        }

        return null;
    }
}
