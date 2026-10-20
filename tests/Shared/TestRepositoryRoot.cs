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

    private static readonly Lazy<string> Located = new(Locate, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>The repository root; resolves only for an integration test or its class fixture.</summary>
    internal static string Resolve()
    {
        EnsureIntegration(TestContext.Current.TestCase?.Traits ?? TestContext.Current.TestClass?.Traits);
        return Located.Value;
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

    private static string Locate()
    {
        var configured = Environment.GetEnvironmentVariable("MCP_REPOSITORY_ROOT");
        if (!string.IsNullOrWhiteSpace(configured))
            return Validate(configured, "Configured");

        var buildRoot = ReadBuildMetadata();
        if (!string.IsNullOrWhiteSpace(buildRoot))
            return Validate(buildRoot, "Build-metadata");

        var startingPoints = new[]
        {
            Path.GetDirectoryName(typeof(TestRepositoryRoot).Assembly.Location),
            AppContext.BaseDirectory,
            Directory.GetCurrentDirectory(),
        };
        foreach (var startingPoint in startingPoints.Where(candidate => !string.IsNullOrWhiteSpace(candidate)))
        {
            for (var current = new DirectoryInfo(Path.GetFullPath(startingPoint!)); current is not null; current = current.Parent)
            {
                if (IsRepositoryRoot(current.FullName))
                    return current.FullName;
            }
        }

        throw new InvalidOperationException(
            "Repository root was not found from MCP_REPOSITORY_ROOT, build metadata, the test assembly, the test output, "
            + "or the current directory.");
    }

    private static string? ReadBuildMetadata() =>
        typeof(TestRepositoryRoot).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => string.Equals(attribute.Key, "McpRepositoryRoot", StringComparison.Ordinal))
            ?.Value;

    /// <summary>Validates an explicit root without falling back to another checkout.</summary>
    private static string Validate(string candidate, string source)
    {
        var root = Path.GetFullPath(candidate);
        if (!IsRepositoryRoot(root))
            throw new InvalidOperationException($"{source} repository root is invalid: {root}");

        return root;
    }

    private static bool IsRepositoryRoot(string root)
    {
        var gitMarker = Path.Combine(root, ".git");
        return (File.Exists(gitMarker) || Directory.Exists(gitMarker))
            && File.Exists(Path.Combine(root, "QBrainAi.sln"))
            && File.Exists(Path.Combine(root, "Directory.Build.props"));
    }
}
