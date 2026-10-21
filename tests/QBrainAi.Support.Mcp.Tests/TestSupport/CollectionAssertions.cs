using System.Reflection;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.TestSupport;

/// <summary>
/// Assertions on xUnit collection membership and collection definitions, read through <see cref="CustomAttributeData"/>
/// so no test is run. Shared by the collection tests of TR-MCP-NUKE-019 AC-2 (<c>SqlServerLocalDbCollectionTests</c>)
/// and of TR-MCP-USECASE-018 AC-006 and TR-MCP-USECASE-019 AC-008 (<c>ProcessEnvironmentCollectionTests</c>).
/// </summary>
internal static class CollectionAssertions
{
    /// <summary>
    /// Asserts that <paramref name="type"/> carries exactly one xUnit collection attribute and that it names
    /// <paramref name="collection"/>.
    /// </summary>
    /// <param name="type">The test class.</param>
    /// <param name="collection">The required collection name.</param>
    internal static void AssertInCollection(Type type, string collection)
    {
        var names = type.GetCustomAttributesData()
            .Where(attribute => attribute.AttributeType == typeof(CollectionAttribute))
            .Select(attribute => attribute.ConstructorArguments.Count == 1 ? attribute.ConstructorArguments[0].Value as string : null)
            .ToList();
        var name = Assert.Single(names);
        Assert.Equal(collection, name);
    }

    /// <summary>
    /// Asserts that <paramref name="definition"/> carries exactly one xUnit collection definition, that it names
    /// <paramref name="collection"/>, and that it sets <c>DisableParallelization</c> to true, so the collection never
    /// runs beside another collection.
    /// </summary>
    /// <param name="definition">The collection definition class.</param>
    /// <param name="collection">The required collection name.</param>
    internal static void AssertSerializedDefinition(Type definition, string collection)
    {
        var attribute = Assert.Single(
            definition.GetCustomAttributesData(),
            data => data.AttributeType == typeof(CollectionDefinitionAttribute));
        var name = Assert.Single(attribute.ConstructorArguments);
        Assert.Equal(collection, name.Value as string);
        var flag = Assert.Single(
            attribute.NamedArguments,
            argument => argument.MemberName == nameof(CollectionDefinitionAttribute.DisableParallelization));
        Assert.True(flag.TypedValue.Value is true);
    }
}
