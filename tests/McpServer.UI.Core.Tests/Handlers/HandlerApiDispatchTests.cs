using System.Reflection;
using McpServer.Cqrs;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Tests.TestInfrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace McpServer.UI.Core.Tests.Handlers;

public sealed class HandlerApiDispatchTests
{
    public static TheoryData<Type, Type> HandlerContracts
    {
        get
        {
            var data = new TheoryData<Type, Type>();
            var assembly = typeof(McpServer.UI.Core.ServiceCollectionExtensions).Assembly;

            var handlers = assembly.GetTypes()
                .Where(t =>
                    t is { IsAbstract: false, IsInterface: false } &&
                    t.Namespace == "McpServer.UI.Core.Handlers" &&
                    t.Name.EndsWith("Handler", StringComparison.Ordinal))
                .OrderBy(t => t.FullName, StringComparer.Ordinal);

            foreach (var handler in handlers)
            {
                var contract = handler.GetInterfaces()
                    .FirstOrDefault(i =>
                        i.IsGenericType &&
                        (i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>) ||
                         i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>)));

                if (contract is not null)
                    data.Add(handler, contract);
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(HandlerContracts))]
    public async Task Handler_AuthorizedPath_InvokesApiClient(Type handlerType, Type handlerContract)
    {
        var contractArgs = handlerContract.GetGenericArguments();
        var requestType = contractArgs[0];
        var request = SampleFactory.Create(requestType);

        var constructor = handlerType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .OrderByDescending(c => c.GetParameters().Length)
            .First();

        var apiClientSubstitutes = new List<object>();
        var args = constructor.GetParameters()
            .Select(p => ResolveDependency(p.ParameterType, apiClientSubstitutes))
            .ToArray();

        var handler = constructor.Invoke(args);
        var handleMethod = handlerType.GetMethod("HandleAsync", BindingFlags.Instance | BindingFlags.Public)!;

        var task = (Task?)handleMethod.Invoke(handler, [request, CallContextFactory.Create()]);
        Assert.NotNull(task);
        await task!;

        var resultProperty = task.GetType().GetProperty("Result");
        Assert.NotNull(resultProperty);
        var resultValue = resultProperty!.GetValue(task);
        Assert.NotNull(resultValue);
        Assert.StartsWith("McpServer.Cqrs.Result`1", resultValue!.GetType().FullName, StringComparison.Ordinal);

        var totalApiCalls = apiClientSubstitutes.Sum(s => SubstituteExtensions.ReceivedCalls(s).Count());
        Assert.True(totalApiCalls > 0, $"Expected API client call for {handlerType.FullName}.");
    }

    private static object ResolveDependency(Type dependencyType, List<object> apiClientSubstitutes)
    {
        if (dependencyType == typeof(IAuthorizationPolicyService))
            return new ConfigurableAuthorizationPolicyService(defaultAllow: true);

        if (dependencyType.IsGenericType &&
            dependencyType.GetGenericTypeDefinition() == typeof(ILogger<>))
        {
            var loggerType = typeof(Logger<>).MakeGenericType(dependencyType.GetGenericArguments()[0]);
            return Activator.CreateInstance(loggerType, NullLoggerFactory.Instance)!;
        }

        if (dependencyType.IsInterface && dependencyType.Name.EndsWith("ApiClient", StringComparison.Ordinal))
        {
            var substitute = Substitute.For([dependencyType], Array.Empty<object>());
            apiClientSubstitutes.Add(substitute);
            return substitute;
        }

        throw new InvalidOperationException($"Unhandled dependency '{dependencyType.FullName}'.");
    }

    private static class SampleFactory
    {
        public static object? Create(Type type)
        {
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

            if (type.IsEnum)
            {
                var values = Enum.GetValues(type);
                return values.Length > 0 ? values.GetValue(0) : Activator.CreateInstance(type);
            }

            if (type.IsArray)
            {
                var elementType = type.GetElementType()!;
                var array = Array.CreateInstance(elementType, 1);
                array.SetValue(Create(elementType), 0);
                return array;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                return null;

            if (type.IsGenericType)
            {
                var genericDefinition = type.GetGenericTypeDefinition();
                var genericArgs = type.GetGenericArguments();

                if (genericDefinition == typeof(List<>) ||
                    genericDefinition == typeof(IReadOnlyList<>) ||
                    genericDefinition == typeof(IEnumerable<>))
                {
                    var listType = typeof(List<>).MakeGenericType(genericArgs[0]);
                    var list = (System.Collections.IList)Activator.CreateInstance(listType)!;
                    list.Add(Create(genericArgs[0]));
                    return list;
                }

                if (genericDefinition == typeof(Dictionary<,>) ||
                    genericDefinition == typeof(IReadOnlyDictionary<,>) ||
                    genericDefinition == typeof(IDictionary<,>))
                {
                    var dictType = typeof(Dictionary<,>).MakeGenericType(genericArgs);
                    var dict = Activator.CreateInstance(dictType)!;
                    var addMethod = dictType.GetMethod("Add", genericArgs)!;
                    var key = Create(genericArgs[0]);
                    var value = Create(genericArgs[1]);
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
                var parameterlessInstance = parameterless.Invoke([]);
                PopulateWritableProperties(type, parameterlessInstance);
                return parameterlessInstance;
            }

            var constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
            if (constructors.Length > 0)
            {
                var constructor = constructors
                    .OrderByDescending(c => c.GetParameters().Length)
                    .First();
                var args = constructor.GetParameters()
                    .Select(p => Create(p.ParameterType))
                    .ToArray();
                return constructor.Invoke(args);
            }

            var fallbackInstance = Activator.CreateInstance(type, nonPublic: true);
            if (fallbackInstance is null)
                return null;

            PopulateWritableProperties(type, fallbackInstance);
            return fallbackInstance;
        }

        private static void PopulateWritableProperties(Type type, object instance)
        {
            foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (!property.CanWrite)
                    continue;

                var value = Create(property.PropertyType);
                property.SetValue(instance, value);
            }
        }
    }
}
