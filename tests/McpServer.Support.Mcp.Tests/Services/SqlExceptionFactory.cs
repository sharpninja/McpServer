using System.Reflection;
using Microsoft.Data.SqlClient;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>TEST-HANDOFF-005: constructs provider-shaped SqlException instances without English message text.</summary>
internal static class SqlExceptionFactory
{
    /// <summary>Creates a SqlException whose Number is the supplied SQL Server error number.</summary>
    public static SqlException Create(int number)
    {
        var errorCtor = typeof(SqlError).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .OrderByDescending(ctor => ctor.GetParameters().Length)
            .First();
        var parameters = errorCtor.GetParameters();
        var args = new object?[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            var type = parameters[i].ParameterType;
            args[i] = type == typeof(int) && parameters[i].Name is "infoNumber" or "number" or "info"
                ? number
                : type == typeof(int)
                    ? 0
                    : type == typeof(byte)
                        ? (byte)0
                        : type == typeof(string)
                            ? string.Empty
                            : type == typeof(bool)
                                ? false
                                : type.IsValueType
                                    ? Activator.CreateInstance(type)
                                    : null;
        }

        var error = (SqlError)errorCtor.Invoke(args);
        var errors = (SqlErrorCollection)Activator.CreateInstance(typeof(SqlErrorCollection), nonPublic: true)!;
        typeof(SqlErrorCollection)
            .GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.Invoke(errors, [error]);
        var exceptionCtor = typeof(SqlException)
            .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .First(ctor => ctor.GetParameters().Any(p => p.ParameterType == typeof(SqlErrorCollection)));
        var exceptionArgs = exceptionCtor.GetParameters()
            .Select(p => p.ParameterType == typeof(SqlErrorCollection)
                ? errors
                : p.ParameterType == typeof(string)
                    ? string.Empty
                    : p.ParameterType.IsValueType
                        ? Activator.CreateInstance(p.ParameterType)
                        : null)
            .ToArray();
        return (SqlException)exceptionCtor.Invoke(exceptionArgs);
    }
}
