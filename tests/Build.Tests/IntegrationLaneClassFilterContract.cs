namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-019 (AC-2): Contract cases and assertions for <see cref="IIntegrationLaneClassFilter"/>. The expected
/// filters come from the acceptance criterion (the lane runs, from QBrainAi.Support.Mcp.IntegrationTests, only the
/// Category=Integration cases of RequirementScopeLayerReplIntegrationTests and QuadBrainOllamaEndpointIntegrationTests,
/// selected by class name; operator 2026-10-29: "Run the 2 classes, AC first (Recommended)"; hostile validation finding
/// R8B-F2) and from the dotnet test filter grammar: '&amp;' binds tighter than '|', so the class alternative is
/// parenthesized, and each class match is bounded by dots so it cannot select a class whose name only contains it. The
/// same assertions run against the scripted filter (BDPv4 mock gate), wrong filters (negative controls), and the
/// production filter.
/// </summary>
internal static class IntegrationLaneClassFilterContract
{
    /// <summary>The Linux host filter.</summary>
    internal const string LinuxHost = "Category=Integration&Platform!=Windows";

    /// <summary>The Windows host filter.</summary>
    internal const string WindowsHost = "Category=Integration&Platform!=Linux";

    /// <summary>The two classes AC-2 names, in its order.</summary>
    internal static readonly string[] SupportClasses = ["RequirementScopeLayerReplIntegrationTests", "QuadBrainOllamaEndpointIntegrationTests"];

    /// <summary>The composition rows: a row name, the host filter, the classes, and the filter AC-2 requires.</summary>
    internal static readonly (string Name, string Host, string[] Classes, string Expected)[] Rows =
    [
        ("every class: the host filter alone", LinuxHost, [], LinuxHost),
        ("the two classes on Linux", LinuxHost, SupportClasses,
            LinuxHost + "&(FullyQualifiedName~.RequirementScopeLayerReplIntegrationTests.|FullyQualifiedName~.QuadBrainOllamaEndpointIntegrationTests.)"),
        ("the two classes on Windows", WindowsHost, SupportClasses,
            WindowsHost + "&(FullyQualifiedName~.RequirementScopeLayerReplIntegrationTests.|FullyQualifiedName~.QuadBrainOllamaEndpointIntegrationTests.)"),
        ("one class", LinuxHost, ["RequirementScopeLayerReplIntegrationTests"],
            LinuxHost + "&(FullyQualifiedName~.RequirementScopeLayerReplIntegrationTests.)"),
    ];

    /// <summary>The refusal rows: a row name, the host filter, and the classes the filter must refuse.</summary>
    internal static readonly (string Name, string Host, string[] Classes)[] Refusals =
    [
        ("a blank host filter", " ", SupportClasses),
        ("an empty class name", LinuxHost, [""]),
        ("a dotted class name", LinuxHost, ["Controllers.QuadBrainOllamaEndpointIntegrationTests"]),
        ("a filter operator in a class name", LinuxHost, ["RequirementScopeLayerReplIntegrationTests|FullyQualifiedName~Live"]),
        ("a parenthesis in a class name", LinuxHost, ["QuadBrainOllamaEndpointIntegrationTests)"]),
        ("whitespace in a class name", LinuxHost, ["Quad Brain"]),
        ("a class name starting with a digit", LinuxHost, ["9QuadBrain"]),
        ("a trailing line break in a class name", LinuxHost, ["QuadBrainOllamaEndpointIntegrationTests\n"]),
    ];

    /// <summary>
    /// Asserts that <paramref name="filter"/> composes the required filter on every row and refuses every refusal row
    /// with an <see cref="ArgumentException"/>.
    /// </summary>
    /// <param name="filter">Filter under test.</param>
    internal static void AssertContract(IIntegrationLaneClassFilter filter)
    {
        foreach (var (name, host, classes, expected) in Rows)
        {
            string? actual = null;
            var thrown = Record.Exception(() => actual = filter.Compose(host, classes));
            Assert.True(thrown is null, $"Row '{name}': threw {thrown?.GetType().Name}: {thrown?.Message}");
            Assert.True(string.Equals(expected, actual, StringComparison.Ordinal), $"Row '{name}': expected '{expected}' but got '{actual}'.");
        }

        foreach (var (name, host, classes) in Refusals)
        {
            var thrown = Record.Exception(() => filter.Compose(host, classes));
            Assert.True(thrown is ArgumentException, $"Refusal '{name}': expected an ArgumentException but got {(thrown is null ? "none" : thrown.GetType().Name)}.");
        }
    }
}
