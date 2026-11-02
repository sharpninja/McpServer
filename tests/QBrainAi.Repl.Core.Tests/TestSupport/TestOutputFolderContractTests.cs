using Xunit.Sdk;

namespace QBrainAi.Repl.Core.Tests.TestSupport;

/// <summary>
/// FR-MCP-TESTRESOURCE-001 (second criterion: "Generated test artifacts are written only to test-owned disposable output
/// locations, not to repository fixture or documentation paths"), BDPv4 mock gate: a test's artifact folder is accepted
/// only when it is an absolute path with no '.' or '..' segment that lies outside the checkout (compared by path segment
/// with either separator); the checkout itself, any folder inside it, a relative path and a dot segment are refused.
/// Fixed rows and an independent scripted reference check; wrong checks (accepts anything, refuses anything, plain string
/// prefix, ignores dot segments, accepts the checkout root) are rejected by the same assertions inside this passing
/// harness. Strings only: no folder is created or read.
/// </summary>
/// <remarks>
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-02</term><description>FR-MCP-TESTRESOURCE-001 (second criterion, generated artifacts in test-owned locations): added the output-folder contract, because two memory-bench stub cases wrote into the checkout's docs/benchmarks/results and rewrote two tracked files on every lane run (operator 2026-11-02: "Fix in scope"; triage-report-32e6bc07).</description></item>
/// </list>
/// </remarks>
public sealed class TestOutputFolderContractTests
{
    /// <summary>A checkout root, a candidate artifact folder and whether the folder is acceptable.</summary>
    /// <param name="Name">Row name.</param>
    /// <param name="Checkout">The checkout root.</param>
    /// <param name="Folder">The candidate artifact folder.</param>
    /// <param name="Accepted">Whether the folder is a test-owned location outside the checkout.</param>
    internal sealed record OutputCase(string Name, string Checkout, string Folder, bool Accepted);

    /// <summary>The fixed rows.</summary>
    internal static readonly OutputCase[] Cases =
    [
        new("temporary folder", "/work/repo", "/tmp/memory-bench-stub-0123", true),
        new("sibling sharing the prefix", "/work/repo", "/work/repo-out/stub", true),
        new("checkout documentation folder", "/work/repo", "/work/repo/docs/benchmarks/results", false),
        new("checkout root itself", "/work/repo", "/work/repo", false),
        new("checkout root with trailing separator", "/work/repo", "/work/repo/", false),
        new("dot segment inside the checkout", "/work/repo", "/work/repo/./docs/out", false),
        new("parent segment", "/work/repo", "/work/repo/../tmp/out", false),
        new("parent segment escaping into the checkout", "/work/repo", "/work/other/../repo/docs", false),
        new("dot segment outside the checkout", "/work/repo", "/tmp/./out", false),
        new("relative folder", "/work/repo", "docs/benchmarks/results", false),
        new("Windows folder inside the checkout, other separator", "C:/GitHub/repo", @"C:\GitHub\repo\docs\out", false),
        new("Windows temporary folder", "C:/GitHub/repo", @"D:\Temp\memory-bench-stub-0123", true),
    ];

    /// <summary>The scripted reference check satisfies every row.</summary>
    [Fact]
    public void ScriptedCheck_SatisfiesEveryRow() => AssertCheck(ScriptedCheck);

    /// <summary>Each wrong check fails the same assertions.</summary>
    /// <param name="fault">The deliberate fault.</param>
    [Theory]
    [InlineData("accepts anything")]
    [InlineData("refuses anything")]
    [InlineData("string prefix")]
    [InlineData("ignores dot segments")]
    [InlineData("accepts the checkout root")]
    [InlineData("accepts a relative folder")]
    [InlineData("one separator only")]
    public void WrongChecks_AreRejected(string fault) =>
        Assert.ThrowsAny<XunitException>(() => AssertCheck((folder, checkout) => fault switch
        {
            "accepts anything" => null,
            "refuses anything" => Refusal(folder, checkout),
            "string prefix" => folder.Replace('\\', '/').StartsWith(checkout.Replace('\\', '/'), StringComparison.Ordinal) ? Refusal(folder, checkout) : null,
            "ignores dot segments" => IsAbsolute(folder) && IsInsideOrEqual(Segments(folder), Segments(checkout)) ? Refusal(folder, checkout) : IsAbsolute(folder) ? null : Refusal(folder, checkout),
            "accepts the checkout root" => Segments(folder).Length == Segments(checkout).Length ? null : ScriptedCheck(folder, checkout),
            "accepts a relative folder" => IsAbsolute(folder) ? ScriptedCheck(folder, checkout) : null,
            "one separator only" => !folder.Contains('/', StringComparison.Ordinal) && IsAbsolute(folder) ? null : ScriptedCheck(folder, checkout),
            _ => throw new ArgumentOutOfRangeException(nameof(fault)),
        }));

    /// <summary>Asserts a check against every row: null for an acceptable folder, a message naming the folder otherwise.</summary>
    /// <param name="check">The check under test: folder and checkout root in, null or a refusal out.</param>
    internal static void AssertCheck(Func<string, string, string?> check)
    {
        foreach (var row in Cases)
        {
            var verdict = check(row.Folder, row.Checkout);
            if (row.Accepted)
                Assert.True(verdict is null, $"{row.Name}: refused '{verdict}'");
            else
                Assert.True(verdict is not null && verdict.Contains(row.Folder, StringComparison.Ordinal), $"{row.Name}: '{verdict}'");
        }
    }

    private static string? ScriptedCheck(string folder, string checkout) =>
        Cases.Single(row => row.Folder == folder && row.Checkout == checkout).Accepted ? null : Refusal(folder, checkout);

    private static string Refusal(string folder, string checkout) =>
        $"The test artifact folder '{folder}' is not a test-owned location outside the checkout '{checkout}'.";

    private static bool IsInsideOrEqual(string[] path, string[] root) =>
        path.Length >= root.Length && root.Select((segment, index) => segment == path[index]).All(same => same);

    private static bool IsAbsolute(string path) =>
        path.StartsWith('/') || (path.Length >= 3 && char.IsAsciiLetter(path[0]) && path[1] == ':' && path[2] is '/' or '\\');

    private static string[] Segments(string path) => path.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
}
