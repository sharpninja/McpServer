using System.Reflection;

namespace NukeBuild.Tests;

/// <summary>TR-MCP-NUKE-019: verifies the actual InlineData row set, rather than only a contract's row count.</summary>
internal static class InlineTheoryCoverage
{
    /// <summary>Asserts that a one-string InlineData theory declares every expected row exactly once.</summary>
    /// <typeparam name="T">The test class declaring the theory.</typeparam>
    /// <param name="methodName">The theory's name.</param>
    /// <param name="expected">The complete contract row set.</param>
    internal static void AssertRows<T>(string methodName, IEnumerable<string> expected)
    {
        var method = typeof(T).GetMethod(methodName);
        Assert.NotNull(method);
        var actual = method.GetCustomAttributesData().Where(attribute => attribute.AttributeType == typeof(InlineDataAttribute))
            .Select(attribute =>
            {
                var values = Assert.IsAssignableFrom<IReadOnlyCollection<CustomAttributeTypedArgument>>(attribute.ConstructorArguments[0].Value);
                return Assert.IsType<string>(Assert.Single(values).Value);
            }).Order(StringComparer.Ordinal).ToArray();
        Assert.Equal(expected.Order(StringComparer.Ordinal), actual);
    }
}
