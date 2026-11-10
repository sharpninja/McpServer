using System.Reflection;

namespace QBrainAi.Tests.Shared;

/// <summary>
/// TR-MCP-USECASE-018 (AC-006), TR-MCP-USECASE-019 (AC-008): the one repository-root locator for the unit-test
/// projects, linked into each project that reads the checkout, so every test that reads repository files reaches
/// it. Reading the checkout is an environment dependence (operator 2026-10-19: "If it depends on the environment,
/// it's an integration test."), so the locator resolves only inside a test case, or a test-class fixture, tagged
/// <c>[Trait("Category", "Integration")]</c>; a unit test that reaches it fails with the reason, which makes the
/// full unit run the proof that no unit test reads the repository. The root comes from <c>MCP_REPOSITORY_ROOT</c>,
/// then the <c>McpRepositoryRoot</c> build metadata, then the first directory above the test assembly, the test
/// output or the current directory that holds the repository markers (<c>.git</c>, <c>QBrainAi.sln</c>,
/// <c>Directory.Build.props</c>).
/// </summary>
internal static class TestRepositoryRoot
{
    private const string RuleMessage =
        "A unit test may not read the repository checkout: it depends on the environment, so it is an integration "
        + "test. Tag the test method or its class [Trait(\"Category\", \"Integration\")], or read a fixture copied "
        + "to the test output.";

    /// <summary>The repository root; resolves only for an integration test or its class fixture.</summary>
    internal static string Resolve()
    {
        EnsureIntegration(TestContext.Current.TestCase?.Traits ?? TestContext.Current.TestClass?.Traits);
        return LocateUncached();
    }

    /// <summary>
    /// The <c>McpRepositoryRoot</c> build metadata of the test assembly (TR-MCP-USECASE-018: the deterministic
    /// repository-root anchor), or null when the project does not embed it; available only for an integration test
    /// or its class fixture, since the value names the checkout.
    /// </summary>
    internal static string? BuildMetadataRoot
    {
        get
        {
            EnsureIntegration(TestContext.Current.TestCase?.Traits ?? TestContext.Current.TestClass?.Traits);
            return ReadBuildMetadata();
        }
    }

    /// <summary>A path under the repository root; resolves only for an integration test or its class fixture.</summary>
    /// <param name="segments">Path segments below the root.</param>
    internal static string Combine(params string[] segments)
    {
        ArgumentNullException.ThrowIfNull(segments);
        return Path.Combine(new[] { Resolve() }.Concat(segments).ToArray());
    }

    /// <summary>
    /// The trait policy shared by <see cref="Resolve"/> and its mock tests: passes only when the traits hold
    /// <c>Category=Integration</c>; otherwise, including when there is no test context, throws naming the rule.
    /// </summary>
    /// <param name="traits">The running test case's or test class's traits, or null outside a test.</param>
    internal static void EnsureIntegration(IReadOnlyDictionary<string, IReadOnlyCollection<string>>? traits)
    {
        if (traits is not null
            && traits.TryGetValue("Category", out var categories)
            && categories.Contains("Integration", StringComparer.Ordinal))
        {
            return;
        }

        throw new InvalidOperationException(RuleMessage);
    }

    /// <summary>AC-005: performs a fresh native lookup, guarded as a physical integration-test operation.</summary>
    internal static string LocateUncached()
    {
        EnsureIntegration(TestContext.Current.TestCase?.Traits ?? TestContext.Current.TestClass?.Traits);
        return Locate(Environment.GetEnvironmentVariable("MCP_REPOSITORY_ROOT"), ReadBuildMetadata(),
            [Path.GetDirectoryName(typeof(TestRepositoryRoot).Assembly.Location), AppContext.BaseDirectory, Directory.GetCurrentDirectory()],
            Path.GetFullPath, IsRepositoryRoot, path => Directory.GetParent(path)?.FullName);
    }

    /// <summary>
    /// AC-005/AC-006: uncached selection over explicit roots, starting points and path predicates. Invalid explicit
    /// roots fail closed; only absent overrides fall back. This overload performs no ambient reads itself.
    /// </summary>
    /// <param name="configured">Optional configured root.</param>
    /// <param name="buildRoot">Optional metadata root.</param>
    /// <param name="startingPoints">Fallback starting points, searched in order.</param>
    /// <param name="normalize">Normalizes an explicit path.</param>
    /// <param name="isRepositoryRoot">Tests the repository markers.</param>
    /// <param name="parent">Returns a parent path or null at the root.</param>
    internal static string Locate(string? configured, string? buildRoot, IEnumerable<string?> startingPoints,
        Func<string, string> normalize, Func<string, bool> isRepositoryRoot, Func<string, string?> parent)
    {
        string ValidateExplicit(string candidate, string source)
        {
            var root = normalize(candidate);
            return isRepositoryRoot(root) ? root : throw new InvalidOperationException($"{source} repository root is invalid: {root}");
        }

        if (!string.IsNullOrWhiteSpace(configured)) return ValidateExplicit(configured, "Configured");
        if (!string.IsNullOrWhiteSpace(buildRoot)) return ValidateExplicit(buildRoot, "Build-metadata");
        foreach (var start in startingPoints.Where(candidate => !string.IsNullOrWhiteSpace(candidate)))
            for (var current = normalize(start!); current is not null; current = parent(current))
                if (isRepositoryRoot(current)) return current;
        throw new InvalidOperationException(
            "Repository root was not found from MCP_REPOSITORY_ROOT, build metadata, the test assembly, the test output, or the current directory.");
    }

    private static string? ReadBuildMetadata() =>
        typeof(TestRepositoryRoot).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => string.Equals(attribute.Key, "McpRepositoryRoot", StringComparison.Ordinal))
            ?.Value;

    private static bool IsRepositoryRoot(string root)
    {
        var gitMarker = Path.Combine(root, ".git");
        return (File.Exists(gitMarker) || Directory.Exists(gitMarker))
            && File.Exists(Path.Combine(root, "QBrainAi.sln"))
            && File.Exists(Path.Combine(root, "Directory.Build.props"));
    }
}
