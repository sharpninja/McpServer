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
}
