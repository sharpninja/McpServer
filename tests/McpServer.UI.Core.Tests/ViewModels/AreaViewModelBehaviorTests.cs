using System.Collections;
using System.Reflection;
using McpServer.Cqrs;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using McpServer.UI.Core.Tests.TestInfrastructure;
using McpServer.UI.Core.ViewModels;
using McpServer.UI.Core.ViewModels.Base;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace McpServer.UI.Core.Tests.ViewModels;

public sealed class AreaViewModelBehaviorTests
{
    private static readonly Assembly UiCoreAssembly = typeof(McpServer.UI.Core.ServiceCollectionExtensions).Assembly;
    private static readonly Type AreaListBaseType = typeof(AreaListViewModelBase<>);
    private static readonly Type AreaDetailBaseType = typeof(AreaDetailViewModelBase<>);

    public static TheoryData<Type> AreaListViewModels =>
        BuildTheoryData(DiscoverDerivedTypes(AreaListBaseType));

    public static TheoryData<Type> AreaDetailViewModels =>
        BuildTheoryData(DiscoverDerivedTypes(AreaDetailBaseType));

    public static TheoryData<Type> MutableAreaDetailViewModels =>
        BuildTheoryData(DiscoverDerivedTypes(AreaDetailBaseType).Where(t => ResolveMutationMethod(t) is not null));

    public static TheoryData<Type> ReadOnlyAreaDetailViewModels =>
        BuildTheoryData(DiscoverDerivedTypes(AreaDetailBaseType).Where(t => ResolveMutationMethod(t) is null));

    [Theory]
    [MemberData(nameof(AreaListViewModels))]
    public async Task AreaListViewModel_LoadPath_PopulatesItems_AndClearsLoading(Type viewModelType)
    {
        using var host = CreateHost(successBehavior: true);
        SeedWorkspaceContext(host);
        await PrimeDispatcherAsync(host);

        var viewModel = host.GetRequiredService(viewModelType);
        PrimeInputProperties(viewModel);
        var loadMethod = ResolveListLoadMethod(viewModelType);

        await InvokeAsync(viewModel, loadMethod);

        var isLoading = (bool)(viewModelType.GetProperty("IsLoading")?.GetValue(viewModel) ?? false);
        var items = (IEnumerable?)viewModelType.GetProperty("Items")?.GetValue(viewModel);
        var count = items?.Cast<object?>().Count() ?? 0;

        Assert.False(isLoading);
        Assert.True(count > 0, $"Expected {viewModelType.Name} to populate Items.");
    }

    [Theory]
    [MemberData(nameof(AreaListViewModels))]
    public async Task AreaListViewModel_FailurePath_SetsError_AndClearsLoading(Type viewModelType)
    {
        using var host = CreateHost(successBehavior: false);
        SeedWorkspaceContext(host);

        var viewModel = host.GetRequiredService(viewModelType);
        PrimeInputProperties(viewModel);
        var loadMethod = ResolveListLoadMethod(viewModelType);

        await InvokeAsync(viewModel, loadMethod);

        var isLoading = (bool)(viewModelType.GetProperty("IsLoading")?.GetValue(viewModel) ?? false);
        var errorMessage = viewModelType.GetProperty("ErrorMessage")?.GetValue(viewModel) as string;

        Assert.False(isLoading);
        if (viewModelType == typeof(DispatcherLogsViewModel))
            return;

        Assert.False(string.IsNullOrWhiteSpace(errorMessage), $"{viewModelType.Name} should set ErrorMessage on failure.");
    }

    [Theory]
    [MemberData(nameof(AreaDetailViewModels))]
    public async Task AreaDetailViewModel_LoadPath_PopulatesDetail_AndClearsBusy(Type viewModelType)
    {
        using var host = CreateHost(successBehavior: true);
        SeedWorkspaceContext(host);

        var viewModel = host.GetRequiredService(viewModelType);
        PrimeInputProperties(viewModel);
        var loadMethod = ResolveDetailLoadMethod(viewModelType);

        await InvokeAsync(viewModel, loadMethod);

        var isBusy = (bool)(viewModelType.GetProperty("IsBusy")?.GetValue(viewModel) ?? false);

        Assert.False(isBusy);
        if (string.Equals(loadMethod.Name, "BrowseAsync", StringComparison.Ordinal))
        {
            var browsedTools = (IEnumerable?)viewModelType.GetProperty("BrowsedTools")?.GetValue(viewModel);
            var count = browsedTools?.Cast<object?>().Count() ?? 0;
            Assert.True(count > 0, $"Expected {viewModelType.Name} browse path to populate tools.");
            return;
        }

        var detail = viewModelType.GetProperty("Detail")?.GetValue(viewModel);
        Assert.NotNull(detail);
    }

    [Theory]
    [MemberData(nameof(MutableAreaDetailViewModels))]
    public async Task AreaDetailViewModel_SaveLikePath_DispatchesMutation(Type viewModelType)
    {
        using var host = CreateHost(successBehavior: true);
        SeedWorkspaceContext(host);

        var dispatcher = host.GetRequiredService<Dispatcher>();
        var before = dispatcher.RecentDispatches.Count;

        var viewModel = host.GetRequiredService(viewModelType);
        PrimeInputProperties(viewModel);
        var mutationMethod = ResolveMutationMethod(viewModelType)
            ?? throw new InvalidOperationException($"No mutation method found for {viewModelType.FullName}.");

        await InvokeAsync(viewModel, mutationMethod);

        var after = dispatcher.RecentDispatches.Count;
        var isBusy = (bool)(viewModelType.GetProperty("IsBusy")?.GetValue(viewModel) ?? false);

        Assert.True(after > before, $"Expected {viewModelType.Name} mutation path to dispatch through CQRS.");
        Assert.False(isBusy);
    }

    [Theory]
    [MemberData(nameof(ReadOnlyAreaDetailViewModels))]
    public void AreaDetailViewModel_NoMutationMethod_IsExplicitlyReadOnly(Type viewModelType)
    {
        Assert.Null(ResolveMutationMethod(viewModelType));
    }

    private static ServiceProvider CreateHost(bool successBehavior)
        => UiCoreTestHost.Create(services =>
        {
            RegisterApiClientSubstitutes(services);
            services.AddSingleton<IPipelineBehavior>(
                successBehavior ? new SuccessSamplePipelineBehavior() : new FailurePipelineBehavior());
        });

    private static void RegisterApiClientSubstitutes(IServiceCollection services)
    {
        var interfaces = typeof(ITodoApiClient).Assembly
            .GetTypes()
            .Where(t =>
                t is { IsInterface: true } &&
                t.Namespace == "McpServer.UI.Core.Services" &&
                t.Name.EndsWith("ApiClient", StringComparison.Ordinal))
            .OrderBy(t => t.FullName, StringComparer.Ordinal);

        foreach (var apiInterface in interfaces)
        {
            var substitute = Substitute.For([apiInterface], Array.Empty<object>());
            services.AddSingleton(apiInterface, substitute);
        }
    }

    private static void SeedWorkspaceContext(IServiceProvider provider)
    {
        var workspaceContext = provider.GetRequiredService<WorkspaceContextViewModel>();
        workspaceContext.ActiveWorkspacePath = "/tmp/mcpserver-ui-core-tests";
    }

    private static async Task PrimeDispatcherAsync(IServiceProvider provider)
    {
        var dispatcher = provider.GetRequiredService<Dispatcher>();
        await dispatcher.QueryAsync(new GetAuthConfigQuery()).ConfigureAwait(false);
    }

    private static void PrimeInputProperties(object viewModel)
    {
        var properties = viewModel
            .GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(static p => p.CanWrite && p.GetIndexParameters().Length == 0);

        foreach (var property in properties)
        {
            if (property.PropertyType == typeof(string) && LooksLikeInputProperty(property.Name))
            {
                property.SetValue(viewModel, property.Name.Contains("Path", StringComparison.OrdinalIgnoreCase)
                    ? "/tmp/mcpserver-ui-core-tests"
                    : "sample");
                continue;
            }

            if ((property.PropertyType == typeof(int) || property.PropertyType == typeof(int?))
                && LooksLikeInputProperty(property.Name))
            {
                property.SetValue(viewModel, 1);
            }
        }
    }

    private static bool LooksLikeInputProperty(string propertyName)
        => propertyName.Contains("Id", StringComparison.OrdinalIgnoreCase)
           || propertyName.Contains("Path", StringComparison.OrdinalIgnoreCase)
           || propertyName.Contains("Filter", StringComparison.OrdinalIgnoreCase)
           || propertyName.Contains("Keyword", StringComparison.OrdinalIgnoreCase)
           || propertyName.Contains("Title", StringComparison.OrdinalIgnoreCase)
           || propertyName.Contains("Body", StringComparison.OrdinalIgnoreCase)
           || propertyName.Contains("Text", StringComparison.OrdinalIgnoreCase)
           || propertyName.Contains("Name", StringComparison.OrdinalIgnoreCase)
           || propertyName.Contains("Number", StringComparison.OrdinalIgnoreCase);

    private static MethodInfo ResolveListLoadMethod(Type viewModelType)
    {
        var candidates = new[] { "LoadAsync", "CheckAsync", "StartAsync" };
        foreach (var candidate in candidates)
        {
            var method = viewModelType.GetMethod(candidate, BindingFlags.Instance | BindingFlags.Public);
            if (method is not null && typeof(Task).IsAssignableFrom(method.ReturnType))
                return method;
        }

        throw new InvalidOperationException($"No list-load method found for {viewModelType.FullName}.");
    }

    private static MethodInfo ResolveDetailLoadMethod(Type viewModelType)
    {
        var load = viewModelType.GetMethod("LoadAsync", BindingFlags.Instance | BindingFlags.Public);
        if (load is not null)
            return load;

        var browse = viewModelType.GetMethod("BrowseAsync", BindingFlags.Instance | BindingFlags.Public);
        if (browse is not null)
            return browse;

        throw new InvalidOperationException($"No detail load/browse method found for {viewModelType.FullName}.");
    }

    private static MethodInfo? ResolveMutationMethod(Type viewModelType)
    {
        var candidateNames = new[]
        {
            "SaveAsync",
            "UpsertAsync",
            "CreateAsync",
            "UpdateAsync",
            "AssignAsync",
            "AddAsync",
            "SyncAsync",
            "InstallAsync",
        };

        foreach (var name in candidateNames)
        {
            var method = viewModelType.GetMethod(name, BindingFlags.Instance | BindingFlags.Public);
            if (method is not null && typeof(Task).IsAssignableFrom(method.ReturnType))
                return method;
        }

        return null;
    }

    private static async Task InvokeAsync(object viewModel, MethodInfo method)
    {
        var args = method.GetParameters().Select(CreateArgument).ToArray();
        var result = method.Invoke(viewModel, args);

        if (result is not Task task)
            throw new InvalidOperationException($"Method {viewModel.GetType().Name}.{method.Name} must return Task.");

        await task.ConfigureAwait(false);
    }

    private static object? CreateArgument(ParameterInfo parameter)
    {
        var type = parameter.ParameterType;
        if (type == typeof(CancellationToken))
            return CancellationToken.None;
        if (type == typeof(string))
            return parameter.Name?.Contains("path", StringComparison.OrdinalIgnoreCase) == true
                ? "/tmp/mcpserver-ui-core-tests"
                : "sample";
        if (type == typeof(int))
            return 1;
        if (type == typeof(bool))
            return true;

        var nullable = Nullable.GetUnderlyingType(type);
        if (nullable is not null)
            return SampleFactory.Create(nullable);

        if (parameter.HasDefaultValue)
            return parameter.DefaultValue;

        return SampleFactory.Create(type);
    }

    private static TheoryData<Type> BuildTheoryData(IEnumerable<Type> types)
    {
        var data = new TheoryData<Type>();
        foreach (var type in types.OrderBy(t => t.FullName, StringComparer.Ordinal))
            data.Add(type);
        return data;
    }

    private static IEnumerable<Type> DiscoverDerivedTypes(Type openGenericBaseType)
    {
        return UiCoreAssembly
            .GetTypes()
            .Where(t =>
                t is { IsAbstract: false, IsInterface: false } &&
                IsDerivedFromOpenGeneric(t, openGenericBaseType));
    }

    private static bool IsDerivedFromOpenGeneric(Type candidate, Type openGenericBaseType)
    {
        for (var current = candidate; current is not null && current != typeof(object); current = current.BaseType!)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == openGenericBaseType)
                return true;
        }

        return false;
    }

    private sealed class SuccessSamplePipelineBehavior : IPipelineBehavior
    {
        public Task<Result<T>> HandleAsync<T>(object request, CallContext context, Func<Task<Result<T>>> next)
        {
            var sample = SampleFactory.Create(typeof(T));
            if (sample is null)
            {
                if (typeof(T).IsValueType)
                    return Task.FromResult(Result<T>.Success((T)Activator.CreateInstance(typeof(T))!));

                return Task.FromResult(Result<T>.Failure($"No sample value for '{typeof(T).Name}'."));
            }

            return Task.FromResult(Result<T>.Success((T)sample));
        }
    }

    private sealed class FailurePipelineBehavior : IPipelineBehavior
    {
        public Task<Result<T>> HandleAsync<T>(object request, CallContext context, Func<Task<Result<T>>> next)
            => Task.FromResult(Result<T>.Failure("Simulated pipeline failure."));
    }

    private static class SampleFactory
    {
        public static object? Create(Type type, int depth = 0)
        {
            if (depth > 4)
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
            if (type == typeof(DateTime))
                return DateTime.UtcNow;
            if (type == typeof(DateTimeOffset))
                return DateTimeOffset.UtcNow;
            if (type == typeof(Guid))
                return Guid.NewGuid();
            if (type == typeof(CancellationToken))
                return CancellationToken.None;

            var nullableType = Nullable.GetUnderlyingType(type);
            if (nullableType is not null)
                return Create(nullableType, depth + 1);

            if (type.IsEnum)
            {
                var values = Enum.GetValues(type);
                return values.Length > 0 ? values.GetValue(0) : Activator.CreateInstance(type);
            }

            if (type.IsArray)
            {
                var elementType = type.GetElementType()!;
                var array = Array.CreateInstance(elementType, 1);
                array.SetValue(Create(elementType, depth + 1), 0);
                return array;
            }

            if (type.IsGenericType)
            {
                var definition = type.GetGenericTypeDefinition();
                var arguments = type.GetGenericArguments();

                if (definition == typeof(List<>) ||
                    definition == typeof(IReadOnlyList<>) ||
                    definition == typeof(IEnumerable<>))
                {
                    var listType = typeof(List<>).MakeGenericType(arguments[0]);
                    var list = (IList)Activator.CreateInstance(listType)!;
                    list.Add(Create(arguments[0], depth + 1));
                    return list;
                }

                if (definition == typeof(IAsyncEnumerable<>))
                {
                    var method = typeof(SampleFactory)
                        .GetMethod(nameof(CreateAsyncEnumerable), BindingFlags.Static | BindingFlags.NonPublic)!
                        .MakeGenericMethod(arguments[0]);
                    return method.Invoke(null, [Create(arguments[0], depth + 1)]);
                }

                if (definition == typeof(Dictionary<,>) ||
                    definition == typeof(IReadOnlyDictionary<,>) ||
                    definition == typeof(IDictionary<,>))
                {
                    var dictType = typeof(Dictionary<,>).MakeGenericType(arguments);
                    var dict = Activator.CreateInstance(dictType)!;
                    var addMethod = dictType.GetMethod("Add", arguments)!;
                    var key = Create(arguments[0], depth + 1);
                    var value = Create(arguments[1], depth + 1);
                    if (key is not null)
                        addMethod.Invoke(dict, [key, value]);
                    return dict;
                }
            }

            if (type.IsValueType)
                return Activator.CreateInstance(type);

            var parameterless = type.GetConstructor(Type.EmptyTypes);
            if (parameterless is not null)
            {
                var instance = parameterless.Invoke([]);
                PopulateWritableProperties(type, instance, depth + 1);
                return instance;
            }

            var constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
            if (constructors.Length > 0)
            {
                var constructor = constructors.OrderByDescending(c => c.GetParameters().Length).First();
                var args = constructor.GetParameters()
                    .Select(p =>
                    {
                        if (p.ParameterType == typeof(string) && IsErrorLikeName(p.Name))
                            return null;
                        return Create(p.ParameterType, depth + 1);
                    })
                    .ToArray();
                var instance = constructor.Invoke(args);
                PopulateWritableProperties(type, instance, depth + 1);
                return instance;
            }

            return Activator.CreateInstance(type, nonPublic: true);
        }

        private static async IAsyncEnumerable<T> CreateAsyncEnumerable<T>(object? item)
        {
            if (item is T typed)
                yield return typed;
            await Task.CompletedTask;
        }

        private static void PopulateWritableProperties(Type type, object instance, int depth)
        {
            foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (!property.CanWrite || property.GetIndexParameters().Length > 0)
                    continue;

                var value = property.PropertyType == typeof(string) && IsErrorLikeName(property.Name)
                    ? null
                    : Create(property.PropertyType, depth + 1);
                property.SetValue(instance, value);
            }
        }

        private static bool IsErrorLikeName(string? name)
            => !string.IsNullOrWhiteSpace(name) &&
               (name.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("failure", StringComparison.OrdinalIgnoreCase));
    }
}
